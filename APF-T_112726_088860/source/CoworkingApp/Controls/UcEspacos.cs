using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcEspacos : UserControl
    {
        // ── Espaços ──────────────────────────────────────────────────────────
        private DataGridView dgvEspacos;
        private Button btnEspNovo, btnEspEditar, btnEspEliminar;
        private int _selEspaco = -1;

        // ── Salas ────────────────────────────────────────────────────────────
        private DataGridView dgvSalas;
        private Button btnSalaNovo, btnSalaEditar, btnSalaEliminar;
        private int _selSala = -1;

        // ── Postos ───────────────────────────────────────────────────────────
        private DataGridView dgvPostos;
        private Button btnPostoNovo, btnPostoEditar, btnPostoEliminar;
        private int _selPosto = -1;

        public UcEspacos()
        {
            this.BackColor = Theme.PageBg;
            this.Dock = DockStyle.Fill;
            BuildUI();
            LoadEspacosData();
            LoadSalasData();
            LoadPostosData();
        }

        private void BuildUI()
        {
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.PageBg, Padding = new Padding(20, 14, 20, 0) };
            pnlTitle.Controls.Add(new Label { Text = "Espaços & Recursos", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill, AutoSize = false });

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.FontBase };

            var tEsp = new TabPage("Espaços");
            BuildEspacosTab(tEsp);
            tabs.TabPages.Add(tEsp);

            var tSala = new TabPage("Salas");
            BuildSalasTab(tSala);
            tabs.TabPages.Add(tSala);

            var tPosto = new TabPage("Postos");
            BuildPostosTab(tPosto);
            tabs.TabPages.Add(tPosto);

            this.Controls.Add(tabs);
            this.Controls.Add(pnlTitle);
        }

        // ════════════════════════════════════════════════════════════════════
        // ESPAÇOS
        // ════════════════════════════════════════════════════════════════════
        private void BuildEspacosTab(TabPage tab)
        {
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var pnlToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();

            btnEspNovo = Theme.BtnPrim("+ Novo");
            btnEspEditar = Theme.BtnGray("Editar");
            btnEspEliminar = Theme.BtnRed("Eliminar");
            btnEspEditar.Enabled = false;
            btnEspEliminar.Enabled = false;
            btnEspNovo.Click += (s, e) => OpenEspacoEditor(null);
            btnEspEditar.Click += (s, e) => OpenEspacoEditor(_selEspaco);
            btnEspEliminar.Click += BtnEspEliminar_Click;
            flow.Controls.Add(btnEspNovo);
            flow.Controls.Add(btnEspEditar);
            flow.Controls.Add(btnEspEliminar);
            pnlToolbar.Controls.Add(flow);

            dgvEspacos = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvEspacos);
            dgvEspacos.SelectionChanged += (s, e) =>
            {
                if (dgvEspacos.SelectedRows.Count == 0) { _selEspaco = -1; btnEspEditar.Enabled = btnEspEliminar.Enabled = false; return; }
                _selEspaco = Convert.ToInt32(dgvEspacos.SelectedRows[0].Cells["ID"].Value);
                btnEspEditar.Enabled = btnEspEliminar.Enabled = true;
            };

            content.Controls.Add(dgvEspacos);
            content.Controls.Add(pnlToolbar);
            tab.Controls.Add(content);
        }

        private void LoadEspacosData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT espaco_id AS ID, nome AS Nome, morada AS Morada,
                             telefone AS Telefone, email AS Email,
                             CONVERT(varchar,hora_abertura,108) AS Abertura,
                             CONVERT(varchar,hora_fecho,108) AS Fecho
                      FROM espaco ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgvEspacos.DataSource = dt;
                    if (dgvEspacos.Columns.Contains("ID")) dgvEspacos.Columns["ID"].Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEspacoEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var txtNome     = UcClientes.AddField(tbl, "Nome *");
            var txtMorada   = UcClientes.AddField(tbl, "Morada *");
            var txtTelefone = UcClientes.AddField(tbl, "Telefone");
            var txtEmail    = UcClientes.AddField(tbl, "Email");
            var txtAbertura = UcClientes.AddField(tbl, "Hora abertura (HH:MM) *");
            var txtFecho    = UcClientes.AddField(tbl, "Hora fecho (HH:MM) *");
            txtAbertura.Text = "08:00";
            txtFecho.Text = "20:00";

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT nome, morada, telefone, email, hora_abertura, hora_fecho FROM espaco WHERE espaco_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtNome.Text = r["nome"]?.ToString() ?? "";
                            txtMorada.Text = r["morada"]?.ToString() ?? "";
                            txtTelefone.Text = r["telefone"] is DBNull ? "" : r["telefone"].ToString();
                            txtEmail.Text = r["email"] is DBNull ? "" : r["email"].ToString();
                            txtAbertura.Text = ((TimeSpan)r["hora_abertura"]).ToString(@"hh\:mm");
                            txtFecho.Text = ((TimeSpan)r["hora_fecho"]).ToString(@"hh\:mm");
                        }
                    }
                }
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Espaço" : "Novo Espaço", tbl, 420, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (string.IsNullOrWhiteSpace(txtMorada.Text)) throw new ApplicationException("Morada é obrigatória.");
                if (!TimeSpan.TryParse(txtAbertura.Text.Trim(), out TimeSpan abertura)) throw new ApplicationException("Hora abertura inválida (HH:MM).");
                if (!TimeSpan.TryParse(txtFecho.Text.Trim(), out TimeSpan fecho)) throw new ApplicationException("Hora fecho inválida (HH:MM).");
                if (fecho <= abertura) throw new ApplicationException("Hora fecho deve ser depois de hora abertura.");

                var sql = id.HasValue
                    ? "UPDATE espaco SET nome=@n, morada=@m, telefone=@t, email=@e, hora_abertura=@ha, hora_fecho=@hf WHERE espaco_id=@id"
                    : "INSERT INTO espaco (nome, morada, telefone, email, hora_abertura, hora_fecho) VALUES (@n,@m,@t,@e,@ha,@hf)";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@m", txtMorada.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", string.IsNullOrWhiteSpace(txtTelefone.Text) ? (object)DBNull.Value : txtTelefone.Text.Trim());
                    cmd.Parameters.AddWithValue("@e", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@ha", abertura);
                    cmd.Parameters.AddWithValue("@hf", fecho);
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) { LoadEspacosData(); LoadSalasData(); LoadPostosData(); }
            }
        }

        private void BtnEspEliminar_Click(object sender, EventArgs e)
        {
            if (_selEspaco < 0) return;
            if (MessageBox.Show("Eliminar espaço?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM espaco WHERE espaco_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _selEspaco);
                    cmd.ExecuteNonQuery();
                }
                LoadEspacosData(); LoadSalasData(); LoadPostosData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // SALAS
        // ════════════════════════════════════════════════════════════════════
        private void BuildSalasTab(TabPage tab)
        {
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var pnlToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();

            btnSalaNovo = Theme.BtnPrim("+ Novo");
            btnSalaEditar = Theme.BtnGray("Editar");
            btnSalaEliminar = Theme.BtnRed("Eliminar");
            btnSalaEditar.Enabled = false;
            btnSalaEliminar.Enabled = false;
            btnSalaNovo.Click += (s, e) => OpenSalaEditor(null);
            btnSalaEditar.Click += (s, e) => OpenSalaEditor(_selSala);
            btnSalaEliminar.Click += BtnSalaEliminar_Click;
            flow.Controls.Add(btnSalaNovo);
            flow.Controls.Add(btnSalaEditar);
            flow.Controls.Add(btnSalaEliminar);
            pnlToolbar.Controls.Add(flow);

            dgvSalas = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvSalas);
            dgvSalas.SelectionChanged += (s, e) =>
            {
                if (dgvSalas.SelectedRows.Count == 0) { _selSala = -1; btnSalaEditar.Enabled = btnSalaEliminar.Enabled = false; return; }
                _selSala = Convert.ToInt32(dgvSalas.SelectedRows[0].Cells["ID"].Value);
                btnSalaEditar.Enabled = btnSalaEliminar.Enabled = true;
            };
            dgvSalas.CellFormatting += (s, e) =>
            {
                if (dgvSalas.Columns.Count == 0 || e.Value == null) return;
                if (dgvSalas.Columns[e.ColumnIndex].Name == "Preço/Hora")
                {
                    if (decimal.TryParse(e.Value.ToString(), out decimal val))
                    {
                        e.Value = Theme.FormatEuro(val);
                        e.FormattingApplied = true;
                    }
                }
            };

            content.Controls.Add(dgvSalas);
            content.Controls.Add(pnlToolbar);
            tab.Controls.Add(content);
        }

        private void LoadSalasData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT s.recurso_id AS ID, e.nome AS Espaço, s.nome AS Nome,
                             s.capacidade AS Capacidade,
                             s.preco_hora AS [Preço/Hora],
                             s.estado AS Estado
                      FROM sala s JOIN espaco e ON s.espaco_id=e.espaco_id
                      ORDER BY e.nome, s.nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgvSalas.DataSource = dt;
                    if (dgvSalas.Columns.Contains("ID")) dgvSalas.Columns["ID"].Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable LoadEspacosForCombo()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand("SELECT espaco_id, nome FROM espaco ORDER BY nome", conn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        private void OpenSalaEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var dsEspacos = LoadEspacosForCombo();
            var cmbEspaco = UcClientes.AddComboDataSource(tbl, "Espaço *", dsEspacos, "nome", "espaco_id");
            var txtNome   = UcClientes.AddField(tbl, "Nome *");
            var txtCap    = UcClientes.AddField(tbl, "Capacidade *");
            var txtPreco  = UcClientes.AddField(tbl, "Preço/Hora *");
            var cmbEstado = UcClientes.AddCombo(tbl, "Estado *", new[] { "Disponivel", "Indisponivel", "Manutencao", "Inativo" });
            cmbEstado.SelectedIndex = 0;

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT espaco_id, nome, capacidade, preco_hora, estado FROM sala WHERE recurso_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            cmbEspaco.SelectedValue = r["espaco_id"];
                            cmbEspaco.Enabled = false;  // não trocar espaco em edição
                            txtNome.Text = r["nome"]?.ToString() ?? "";
                            txtCap.Text = r["capacidade"]?.ToString() ?? "";
                            txtPreco.Text = Convert.ToDecimal(r["preco_hora"]).ToString(CultureInfo.InvariantCulture);
                            var estado = r["estado"]?.ToString() ?? "Disponivel";
                            var idx = cmbEstado.Items.IndexOf(estado);
                            cmbEstado.SelectedIndex = idx >= 0 ? idx : 0;
                        }
                    }
                }
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Sala" : "Nova Sala", tbl, 420, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (!int.TryParse(txtCap.Text, out int cap) || cap <= 0) throw new ApplicationException("Capacidade inválida (inteiro > 0).");
                if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0) throw new ApplicationException("Preço inválido.");
                if (cmbEspaco.SelectedValue == null) throw new ApplicationException("Espaço é obrigatório.");
                if (cmbEstado.SelectedItem == null) throw new ApplicationException("Estado é obrigatório.");

                using (var conn = Database.GetConnection())
                {
                    if (id.HasValue)
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE sala SET nome=@n, capacidade=@c, preco_hora=@p, estado=@e WHERE recurso_id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id.Value);
                            cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                            cmd.Parameters.AddWithValue("@c", cap);
                            cmd.Parameters.AddWithValue("@p", preco);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedItem.ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Two-step: INSERT recurso, then INSERT sala
                        int newRid;
                        using (var ins = new SqlCommand("INSERT INTO recurso (tipo) VALUES ('Sala'); SELECT SCOPE_IDENTITY()", conn))
                            newRid = Convert.ToInt32(ins.ExecuteScalar());
                        using (var cmd = new SqlCommand(
                            "INSERT INTO sala (recurso_id, espaco_id, nome, capacidade, preco_hora, estado) VALUES (@rid,@eid,@n,@c,@p,@e)", conn))
                        {
                            cmd.Parameters.AddWithValue("@rid", newRid);
                            cmd.Parameters.AddWithValue("@eid", Convert.ToInt32(cmbEspaco.SelectedValue));
                            cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                            cmd.Parameters.AddWithValue("@c", cap);
                            cmd.Parameters.AddWithValue("@p", preco);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedItem.ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadSalasData();
            }
        }

        private void BtnSalaEliminar_Click(object sender, EventArgs e)
        {
            if (_selSala < 0) return;
            if (MessageBox.Show("Eliminar sala?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM reserva WHERE recurso_id=@id AND estado NOT IN ('Cancelada','Concluida')", conn))
                    {
                        chk.Parameters.AddWithValue("@id", _selSala);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — sala tem reservas activas.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM recurso WHERE recurso_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _selSala);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadSalasData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // POSTOS
        // ════════════════════════════════════════════════════════════════════
        private void BuildPostosTab(TabPage tab)
        {
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var pnlToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();

            btnPostoNovo = Theme.BtnPrim("+ Novo");
            btnPostoEditar = Theme.BtnGray("Editar");
            btnPostoEliminar = Theme.BtnRed("Eliminar");
            btnPostoEditar.Enabled = false;
            btnPostoEliminar.Enabled = false;
            btnPostoNovo.Click += (s, e) => OpenPostoEditor(null);
            btnPostoEditar.Click += (s, e) => OpenPostoEditor(_selPosto);
            btnPostoEliminar.Click += BtnPostoEliminar_Click;
            flow.Controls.Add(btnPostoNovo);
            flow.Controls.Add(btnPostoEditar);
            flow.Controls.Add(btnPostoEliminar);
            pnlToolbar.Controls.Add(flow);

            dgvPostos = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvPostos);
            dgvPostos.SelectionChanged += (s, e) =>
            {
                if (dgvPostos.SelectedRows.Count == 0) { _selPosto = -1; btnPostoEditar.Enabled = btnPostoEliminar.Enabled = false; return; }
                _selPosto = Convert.ToInt32(dgvPostos.SelectedRows[0].Cells["ID"].Value);
                btnPostoEditar.Enabled = btnPostoEliminar.Enabled = true;
            };
            dgvPostos.CellFormatting += (s, e) =>
            {
                if (dgvPostos.Columns.Count == 0 || e.Value == null) return;
                if (dgvPostos.Columns[e.ColumnIndex].Name == "Preço/Dia")
                {
                    if (decimal.TryParse(e.Value.ToString(), out decimal val))
                    {
                        e.Value = Theme.FormatEuro(val);
                        e.FormattingApplied = true;
                    }
                }
            };

            content.Controls.Add(dgvPostos);
            content.Controls.Add(pnlToolbar);
            tab.Controls.Add(content);
        }

        private void LoadPostosData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT p.recurso_id AS ID, e.nome AS Espaço, p.codigo AS Código,
                             p.tipo_posto AS Tipo, p.preco_dia AS [Preço/Dia], p.estado AS Estado
                      FROM posto p JOIN espaco e ON p.espaco_id=e.espaco_id
                      ORDER BY e.nome, p.codigo", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgvPostos.DataSource = dt;
                    if (dgvPostos.Columns.Contains("ID")) dgvPostos.Columns["ID"].Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenPostoEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var dsEspacos = LoadEspacosForCombo();
            var cmbEspaco = UcClientes.AddComboDataSource(tbl, "Espaço *", dsEspacos, "nome", "espaco_id");
            var txtCodigo = UcClientes.AddField(tbl, "Código *");
            var cmbTipo   = UcClientes.AddCombo(tbl, "Tipo *", new[] { "Flex", "Fixo", "Privado" });
            var txtPreco  = UcClientes.AddField(tbl, "Preço/Dia *");
            var cmbEstado = UcClientes.AddCombo(tbl, "Estado *", new[] { "Disponivel", "Indisponivel", "Manutencao", "Inativo" });
            cmbTipo.SelectedIndex = 0;
            cmbEstado.SelectedIndex = 0;

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT espaco_id, codigo, tipo_posto, preco_dia, estado FROM posto WHERE recurso_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            cmbEspaco.SelectedValue = r["espaco_id"];
                            cmbEspaco.Enabled = false;
                            txtCodigo.Text = r["codigo"]?.ToString() ?? "";
                            var tipo = r["tipo_posto"]?.ToString() ?? "Flex";
                            var idxT = cmbTipo.Items.IndexOf(tipo);
                            cmbTipo.SelectedIndex = idxT >= 0 ? idxT : 0;
                            txtPreco.Text = Convert.ToDecimal(r["preco_dia"]).ToString(CultureInfo.InvariantCulture);
                            var estado = r["estado"]?.ToString() ?? "Disponivel";
                            var idxE = cmbEstado.Items.IndexOf(estado);
                            cmbEstado.SelectedIndex = idxE >= 0 ? idxE : 0;
                        }
                    }
                }
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Posto" : "Novo Posto", tbl, 420, () =>
            {
                if (string.IsNullOrWhiteSpace(txtCodigo.Text)) throw new ApplicationException("Código é obrigatório.");
                if (cmbTipo.SelectedItem == null) throw new ApplicationException("Tipo é obrigatório.");
                if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0) throw new ApplicationException("Preço inválido.");
                if (cmbEspaco.SelectedValue == null) throw new ApplicationException("Espaço é obrigatório.");
                if (cmbEstado.SelectedItem == null) throw new ApplicationException("Estado é obrigatório.");

                using (var conn = Database.GetConnection())
                {
                    if (id.HasValue)
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE posto SET codigo=@c, tipo_posto=@t, preco_dia=@p, estado=@e WHERE recurso_id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id.Value);
                            cmd.Parameters.AddWithValue("@c", txtCodigo.Text.Trim());
                            cmd.Parameters.AddWithValue("@t", cmbTipo.SelectedItem.ToString());
                            cmd.Parameters.AddWithValue("@p", preco);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedItem.ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        int newRid;
                        using (var ins = new SqlCommand("INSERT INTO recurso (tipo) VALUES ('Posto'); SELECT SCOPE_IDENTITY()", conn))
                            newRid = Convert.ToInt32(ins.ExecuteScalar());
                        using (var cmd = new SqlCommand(
                            "INSERT INTO posto (recurso_id, espaco_id, codigo, tipo_posto, preco_dia, estado) VALUES (@rid,@eid,@c,@t,@p,@e)", conn))
                        {
                            cmd.Parameters.AddWithValue("@rid", newRid);
                            cmd.Parameters.AddWithValue("@eid", Convert.ToInt32(cmbEspaco.SelectedValue));
                            cmd.Parameters.AddWithValue("@c", txtCodigo.Text.Trim());
                            cmd.Parameters.AddWithValue("@t", cmbTipo.SelectedItem.ToString());
                            cmd.Parameters.AddWithValue("@p", preco);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedItem.ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadPostosData();
            }
        }

        private void BtnPostoEliminar_Click(object sender, EventArgs e)
        {
            if (_selPosto < 0) return;
            if (MessageBox.Show("Eliminar posto?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM reserva WHERE recurso_id=@id AND estado NOT IN ('Cancelada','Concluida')", conn))
                    {
                        chk.Parameters.AddWithValue("@id", _selPosto);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — posto tem reservas activas.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM recurso WHERE recurso_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _selPosto);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadPostosData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
