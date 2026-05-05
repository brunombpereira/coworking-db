using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcEspacos : UserControl
    {
        // ── Espaços state ────────────────────────────────────────────────────
        private DataGridView dgvEspacos;
        private Panel pnlEspacosForm;
        private Button btnEspNovo, btnEspEditar, btnEspEliminar, btnEspGuardar, btnEspCancelar;
        private TextBox txtEspNome, txtEspMorada, txtEspTelefone, txtEspEmail;
        private TextBox txtEspAbertura, txtEspFecho;
        private int _editIdEsp = -1;

        // ── Salas state ──────────────────────────────────────────────────────
        private DataGridView dgvSalas;
        private Panel pnlSalasForm;
        private Button btnSalaNovo, btnSalaEditar, btnSalaEliminar, btnSalaGuardar, btnSalaCancelar;
        private ComboBox cmbSalasEspaco, cmbSalaEstado;
        private TextBox txtSalaNome, txtSalaCapacidade, txtSalaPreco;
        private int _editIdSala = -1;

        // ── Postos state ─────────────────────────────────────────────────────
        private DataGridView dgvPostos;
        private Panel pnlPostosForm;
        private Button btnPostoNovo, btnPostoEditar, btnPostoEliminar, btnPostoGuardar, btnPostoCancelar;
        private ComboBox cmbPostosEspaco, cmbPostoTipo, cmbPostoEstado;
        private TextBox txtPostoCodigo, txtPostoPreco;
        private int _editIdPosto = -1;

        public UcEspacos()
        {
            this.BackColor = Theme.ContentBg;
            this.Dock = DockStyle.Fill;
            BuildUI();

            LoadSalasEspacos();
            LoadPostosEspacos();
            LoadEspacosData();
            LoadSalasData();
            LoadPostosData();
        }

        private void BuildUI()
        {
            // ── Page title ───────────────────────────────────────────────────
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.ContentBg,
                Padding = new Padding(20, 12, 0, 0)
            };
            var lblTitle = new Label
            {
                Text = "Espaços & Recursos",
                Font = Theme.FontTitle,
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            pnlTitle.Controls.Add(lblTitle);

            // ── TabControl ───────────────────────────────────────────────────
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontBase
            };

            var tab1 = new TabPage("Espaços");
            var tab2 = new TabPage("Salas");
            var tab3 = new TabPage("Postos");

            BuildEspacosTab(tab1);
            BuildSalasTab(tab2);
            BuildPostosTab(tab3);

            tabControl.TabPages.Add(tab1);
            tabControl.TabPages.Add(tab2);
            tabControl.TabPages.Add(tab3);

            // ── Add to UserControl (CRITICAL ORDER) ──────────────────────────
            this.Controls.Add(tabControl);
            this.Controls.Add(pnlTitle);
        }

        // ════════════════════════════════════════════════════════════════════
        // TAB 1: ESPAÇOS
        // ════════════════════════════════════════════════════════════════════

        private void BuildEspacosTab(TabPage tab)
        {
            var pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ContentBg };

            // Toolbar
            var pnlEspacosToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();

            btnEspNovo     = Theme.BtnPrim("+ Novo");
            btnEspEditar   = Theme.BtnGray("Editar");
            btnEspEliminar = Theme.BtnRed("Eliminar");
            btnEspGuardar  = Theme.BtnPrim("Guardar");
            btnEspCancelar = Theme.BtnGray("Cancelar");

            btnEspEditar.Enabled   = false;
            btnEspEliminar.Enabled = false;
            btnEspGuardar.Visible  = false;
            btnEspCancelar.Visible = false;

            btnEspNovo.Click     += BtnEspNovo_Click;
            btnEspEditar.Click   += BtnEspEditar_Click;
            btnEspEliminar.Click += BtnEspEliminar_Click;
            btnEspGuardar.Click  += BtnEspGuardar_Click;
            btnEspCancelar.Click += BtnEspCancelar_Click;

            flow.Controls.Add(btnEspNovo);
            flow.Controls.Add(btnEspEditar);
            flow.Controls.Add(btnEspEliminar);
            flow.Controls.Add(btnEspGuardar);
            flow.Controls.Add(btnEspCancelar);
            pnlEspacosToolbar.Controls.Add(flow);

            // Grid
            dgvEspacos = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvEspacos);
            dgvEspacos.SelectionChanged += DgvEspacos_SelectionChanged;

            // Form — height=160, 3 cols x 2 rows
            pnlEspacosForm = Theme.FormPanel(160);

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 3,
                AutoSize    = false
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));

            // Row 0
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Nome *"),     txtEspNome      = Theme.Field()), 0, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Morada *"),    txtEspMorada    = Theme.Field()), 1, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Telefone"),    txtEspTelefone  = Theme.Field()), 2, 0);

            // Row 1
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Email"),       txtEspEmail     = Theme.Field()), 0, 1);

            txtEspAbertura = Theme.Field();
            txtEspAbertura.Text = "08:00";
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Hora Abertura *"), txtEspAbertura), 1, 1);

            txtEspFecho = Theme.Field();
            txtEspFecho.Text = "20:00";
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Hora Fecho *"),   txtEspFecho), 2, 1);

            // Row 2: buttons
            var flowBtns = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Padding       = new Padding(0, 4, 0, 0)
            };
            var btnSave   = Theme.BtnPrim("Guardar");
            var btnCancel = Theme.BtnGray("Cancelar");
            btnSave.Click   += BtnEspGuardar_Click;
            btnCancel.Click += BtnEspCancelar_Click;
            flowBtns.Controls.Add(btnSave);
            flowBtns.Controls.Add(btnCancel);
            tbl.SetColumnSpan(flowBtns, 3);
            tbl.Controls.Add(flowBtns, 0, 2);

            pnlEspacosForm.Controls.Add(tbl);

            // Add in correct order: form first (Bottom), then grid (Fill), then toolbar (Top)
            pnlContent.Controls.Add(pnlEspacosForm);
            pnlContent.Controls.Add(dgvEspacos);
            pnlContent.Controls.Add(pnlEspacosToolbar);

            tab.Controls.Add(pnlContent);
        }

        private void SetEspacosEditMode(bool editing)
        {
            btnEspNovo.Visible     = btnEspEditar.Visible     = btnEspEliminar.Visible = !editing;
            btnEspGuardar.Visible  = btnEspCancelar.Visible   = editing;
            dgvEspacos.Enabled     = !editing;
            pnlEspacosForm.Visible = editing;
            if (!editing)
                btnEspEditar.Enabled = btnEspEliminar.Enabled = dgvEspacos.SelectedRows.Count > 0;
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
                      FROM espaco
                      ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgvEspacos.DataSource = dt;
                    if (dgvEspacos.Columns.Contains("ID"))
                        dgvEspacos.Columns["ID"].Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvEspacos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvEspacos.SelectedRows.Count == 0) return;
            var row = dgvEspacos.SelectedRows[0];
            _editIdEsp = Convert.ToInt32(row.Cells["ID"].Value);

            txtEspNome.Text      = row.Cells["Nome"].Value?.ToString() ?? "";
            txtEspMorada.Text    = row.Cells["Morada"].Value?.ToString() ?? "";
            txtEspTelefone.Text  = row.Cells["Telefone"].Value?.ToString() ?? "";
            txtEspEmail.Text     = row.Cells["Email"].Value?.ToString() ?? "";
            txtEspAbertura.Text  = row.Cells["Abertura"].Value?.ToString() ?? "08:00";
            txtEspFecho.Text     = row.Cells["Fecho"].Value?.ToString() ?? "20:00";

            btnEspEditar.Enabled   = true;
            btnEspEliminar.Enabled = true;
        }

        private void BtnEspNovo_Click(object sender, EventArgs e)
        {
            _editIdEsp           = -1;
            txtEspNome.Text      = "";
            txtEspMorada.Text    = "";
            txtEspTelefone.Text  = "";
            txtEspEmail.Text     = "";
            txtEspAbertura.Text  = "08:00";
            txtEspFecho.Text     = "20:00";
            SetEspacosEditMode(true);
            txtEspNome.Focus();
        }

        private void BtnEspEditar_Click(object sender, EventArgs e)
        {
            if (_editIdEsp < 0) return;
            SetEspacosEditMode(true);
            txtEspNome.Focus();
        }

        private void BtnEspEliminar_Click(object sender, EventArgs e)
        {
            if (_editIdEsp < 0) return;
            var res = MessageBox.Show("Eliminar o espaço selecionado?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) return;

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM espaco WHERE espaco_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _editIdEsp);
                    cmd.ExecuteNonQuery();
                }
                _editIdEsp = -1;
                LoadEspacosData();
                LoadSalasEspacos();
                LoadPostosEspacos();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEspGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEspNome.Text))
            {
                MessageBox.Show("Nome é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtEspMorada.Text))
            {
                MessageBox.Show("Morada é obrigatória.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtEspAbertura.Text))
            {
                MessageBox.Show("Hora de abertura é obrigatória.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtEspFecho.Text))
            {
                MessageBox.Show("Hora de fecho é obrigatória.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TimeSpan.TryParse(txtEspAbertura.Text.Trim(), out TimeSpan abertura))
            {
                MessageBox.Show("Hora de abertura inválida. Use o formato HH:MM.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!TimeSpan.TryParse(txtEspFecho.Text.Trim(), out TimeSpan fecho))
            {
                MessageBox.Show("Hora de fecho inválida. Use o formato HH:MM.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            object telefoneVal = string.IsNullOrWhiteSpace(txtEspTelefone.Text)
                ? (object)DBNull.Value : txtEspTelefone.Text.Trim();
            object emailVal = string.IsNullOrWhiteSpace(txtEspEmail.Text)
                ? (object)DBNull.Value : txtEspEmail.Text.Trim();

            try
            {
                using (var conn = Database.GetConnection())
                {
                    SqlCommand cmd;
                    if (_editIdEsp < 0)
                    {
                        cmd = new SqlCommand(
                            "INSERT INTO espaco (nome,morada,telefone,email,hora_abertura,hora_fecho) " +
                            "VALUES (@n,@m,@t,@e,@ha,@hf)", conn);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            "UPDATE espaco SET nome=@n,morada=@m,telefone=@t,email=@e," +
                            "hora_abertura=@ha,hora_fecho=@hf WHERE espaco_id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", _editIdEsp);
                    }

                    cmd.Parameters.AddWithValue("@n",  txtEspNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@m",  txtEspMorada.Text.Trim());
                    cmd.Parameters.AddWithValue("@t",  telefoneVal);
                    cmd.Parameters.AddWithValue("@e",  emailVal);
                    cmd.Parameters.AddWithValue("@ha", abertura);
                    cmd.Parameters.AddWithValue("@hf", fecho);

                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }

                SetEspacosEditMode(false);
                LoadEspacosData();
                LoadSalasEspacos();
                LoadPostosEspacos();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEspCancelar_Click(object sender, EventArgs e)
        {
            SetEspacosEditMode(false);
        }

        // ════════════════════════════════════════════════════════════════════
        // TAB 2: SALAS
        // ════════════════════════════════════════════════════════════════════

        private void BuildSalasTab(TabPage tab)
        {
            var pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ContentBg };

            // Toolbar
            var pnlSalasToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();

            btnSalaNovo     = Theme.BtnPrim("+ Novo");
            btnSalaEditar   = Theme.BtnGray("Editar");
            btnSalaEliminar = Theme.BtnRed("Eliminar");
            btnSalaGuardar  = Theme.BtnPrim("Guardar");
            btnSalaCancelar = Theme.BtnGray("Cancelar");

            btnSalaEditar.Enabled   = false;
            btnSalaEliminar.Enabled = false;
            btnSalaGuardar.Visible  = false;
            btnSalaCancelar.Visible = false;

            btnSalaNovo.Click     += BtnSalaNovo_Click;
            btnSalaEditar.Click   += BtnSalaEditar_Click;
            btnSalaEliminar.Click += BtnSalaEliminar_Click;
            btnSalaGuardar.Click  += BtnSalaGuardar_Click;
            btnSalaCancelar.Click += BtnSalaCancelar_Click;

            flow.Controls.Add(btnSalaNovo);
            flow.Controls.Add(btnSalaEditar);
            flow.Controls.Add(btnSalaEliminar);
            flow.Controls.Add(btnSalaGuardar);
            flow.Controls.Add(btnSalaCancelar);
            pnlSalasToolbar.Controls.Add(flow);

            // Grid
            dgvSalas = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvSalas);
            dgvSalas.SelectionChanged += DgvSalas_SelectionChanged;
            dgvSalas.CellFormatting   += DgvSalas_CellFormatting;

            // Form — height=140, 4 cols x 3 rows
            pnlSalasForm = Theme.FormPanel(160);

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 3,
                AutoSize    = false
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));

            // Row 0
            cmbSalasEspaco = Theme.Combo();
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Espaço *"),       cmbSalasEspaco),        0, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Nome *"),         txtSalaNome       = Theme.Field()), 1, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Capacidade *"),   txtSalaCapacidade = Theme.Field()), 2, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Preço/Hora *"),   txtSalaPreco      = Theme.Field()), 3, 0);

            // Row 1 — Estado combo (col 0), rest empty
            cmbSalaEstado = Theme.Combo();
            cmbSalaEstado.Items.AddRange(new object[] { "Disponivel", "Indisponivel", "Manutencao", "Inativa" });
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Estado *"), cmbSalaEstado), 0, 1);

            // Row 2: buttons
            var flowBtns = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Padding       = new Padding(0, 4, 0, 0)
            };
            var btnSave   = Theme.BtnPrim("Guardar");
            var btnCancel = Theme.BtnGray("Cancelar");
            btnSave.Click   += BtnSalaGuardar_Click;
            btnCancel.Click += BtnSalaCancelar_Click;
            flowBtns.Controls.Add(btnSave);
            flowBtns.Controls.Add(btnCancel);
            tbl.SetColumnSpan(flowBtns, 4);
            tbl.Controls.Add(flowBtns, 0, 2);

            pnlSalasForm.Controls.Add(tbl);

            pnlContent.Controls.Add(pnlSalasForm);
            pnlContent.Controls.Add(dgvSalas);
            pnlContent.Controls.Add(pnlSalasToolbar);

            tab.Controls.Add(pnlContent);
        }

        private void LoadSalasEspacos()
        {
            if (cmbSalasEspaco == null) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT espaco_id, nome FROM espaco ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    cmbSalasEspaco.DataSource    = dt;
                    cmbSalasEspaco.DisplayMember = "nome";
                    cmbSalasEspaco.ValueMember   = "espaco_id";
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetSalasEditMode(bool editing)
        {
            btnSalaNovo.Visible     = btnSalaEditar.Visible     = btnSalaEliminar.Visible = !editing;
            btnSalaGuardar.Visible  = btnSalaCancelar.Visible   = editing;
            dgvSalas.Enabled        = !editing;
            pnlSalasForm.Visible    = editing;
            if (!editing)
                btnSalaEditar.Enabled = btnSalaEliminar.Enabled = dgvSalas.SelectedRows.Count > 0;
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
                      FROM sala s
                      JOIN espaco e ON s.espaco_id=e.espaco_id
                      ORDER BY e.nome, s.nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgvSalas.DataSource = dt;
                    if (dgvSalas.Columns.Contains("ID"))
                        dgvSalas.Columns["ID"].Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvSalas_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSalas.SelectedRows.Count == 0) return;
            var row = dgvSalas.SelectedRows[0];
            _editIdSala = Convert.ToInt32(row.Cells["ID"].Value);

            txtSalaNome.Text      = row.Cells["Nome"].Value?.ToString() ?? "";
            txtSalaCapacidade.Text = row.Cells["Capacidade"].Value?.ToString() ?? "";
            txtSalaPreco.Text     = row.Cells["Preço/Hora"].Value?.ToString() ?? "";

            var estado = row.Cells["Estado"].Value?.ToString() ?? "";
            var idx = cmbSalaEstado.Items.IndexOf(estado);
            cmbSalaEstado.SelectedIndex = idx >= 0 ? idx : 0;

            btnSalaEditar.Enabled   = true;
            btnSalaEliminar.Enabled = true;
        }

        private void DgvSalas_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
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
        }

        private void BtnSalaNovo_Click(object sender, EventArgs e)
        {
            _editIdSala               = -1;
            txtSalaNome.Text          = "";
            txtSalaCapacidade.Text    = "";
            txtSalaPreco.Text         = "";
            cmbSalaEstado.SelectedIndex  = 0;
            cmbSalasEspaco.Enabled    = true;
            if (cmbSalasEspaco.Items.Count > 0)
                cmbSalasEspaco.SelectedIndex = 0;
            SetSalasEditMode(true);
            txtSalaNome.Focus();
        }

        private void BtnSalaEditar_Click(object sender, EventArgs e)
        {
            if (_editIdSala < 0) return;
            cmbSalasEspaco.Enabled = false; // cannot change espaco of existing sala
            SetSalasEditMode(true);
            txtSalaNome.Focus();
        }

        private void BtnSalaEliminar_Click(object sender, EventArgs e)
        {
            if (_editIdSala < 0) return;
            var res = MessageBox.Show("Eliminar a sala selecionada?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) return;

            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand(
                        "SELECT COUNT(*) FROM reserva WHERE recurso_id=@id AND estado NOT IN ('Cancelada','Concluida')", conn))
                    {
                        chk.Parameters.AddWithValue("@id", _editIdSala);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — a sala tem reservas ativas.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM recurso WHERE recurso_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editIdSala);
                        cmd.ExecuteNonQuery();
                    }
                }
                _editIdSala = -1;
                LoadSalasData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSalaGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSalaNome.Text))
            {
                MessageBox.Show("Nome é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(txtSalaCapacidade.Text, out int cap) || cap <= 0)
            {
                MessageBox.Show("Capacidade inválida.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtSalaPreco.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal preco) || preco < 0)
            {
                MessageBox.Show("Preço inválido.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbSalaEstado.SelectedItem == null)
            {
                MessageBox.Show("Estado é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_editIdSala < 0 && cmbSalasEspaco.SelectedValue == null)
            {
                MessageBox.Show("Espaço é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var estado = cmbSalaEstado.SelectedItem.ToString();

            try
            {
                using (var conn = Database.GetConnection())
                {
                    SqlCommand cmd;
                    if (_editIdSala < 0)
                    {
                        var espacoId = Convert.ToInt32(cmbSalasEspaco.SelectedValue);
                        int newRecursoId;
                        using (var ins = new SqlCommand(
                            "INSERT INTO recurso (tipo) VALUES ('Sala'); SELECT SCOPE_IDENTITY()", conn))
                            newRecursoId = Convert.ToInt32(ins.ExecuteScalar());
                        cmd = new SqlCommand(
                            "INSERT INTO sala (recurso_id,nome,capacidade,preco_hora,estado,espaco_id) " +
                            "VALUES (@rid,@n,@c,@p,@e,@eid)", conn);
                        cmd.Parameters.AddWithValue("@rid", newRecursoId);
                        cmd.Parameters.AddWithValue("@eid", espacoId);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            "UPDATE sala SET nome=@n,capacidade=@c,preco_hora=@p,estado=@e " +
                            "WHERE recurso_id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", _editIdSala);
                    }

                    cmd.Parameters.AddWithValue("@n", txtSalaNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@c", cap);
                    cmd.Parameters.AddWithValue("@p", preco);
                    cmd.Parameters.AddWithValue("@e", estado);

                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }

                SetSalasEditMode(false);
                LoadSalasData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSalaCancelar_Click(object sender, EventArgs e)
        {
            SetSalasEditMode(false);
        }

        // ════════════════════════════════════════════════════════════════════
        // TAB 3: POSTOS
        // ════════════════════════════════════════════════════════════════════

        private void BuildPostosTab(TabPage tab)
        {
            var pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ContentBg };

            // Toolbar
            var pnlPostosToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();

            btnPostoNovo     = Theme.BtnPrim("+ Novo");
            btnPostoEditar   = Theme.BtnGray("Editar");
            btnPostoEliminar = Theme.BtnRed("Eliminar");
            btnPostoGuardar  = Theme.BtnPrim("Guardar");
            btnPostoCancelar = Theme.BtnGray("Cancelar");

            btnPostoEditar.Enabled   = false;
            btnPostoEliminar.Enabled = false;
            btnPostoGuardar.Visible  = false;
            btnPostoCancelar.Visible = false;

            btnPostoNovo.Click     += BtnPostoNovo_Click;
            btnPostoEditar.Click   += BtnPostoEditar_Click;
            btnPostoEliminar.Click += BtnPostoEliminar_Click;
            btnPostoGuardar.Click  += BtnPostoGuardar_Click;
            btnPostoCancelar.Click += BtnPostoCancelar_Click;

            flow.Controls.Add(btnPostoNovo);
            flow.Controls.Add(btnPostoEditar);
            flow.Controls.Add(btnPostoEliminar);
            flow.Controls.Add(btnPostoGuardar);
            flow.Controls.Add(btnPostoCancelar);
            pnlPostosToolbar.Controls.Add(flow);

            // Grid
            dgvPostos = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvPostos);
            dgvPostos.SelectionChanged += DgvPostos_SelectionChanged;
            dgvPostos.CellFormatting   += DgvPostos_CellFormatting;

            // Form — height=160, 4 cols x 3 rows
            pnlPostosForm = Theme.FormPanel(160);

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 3,
                AutoSize    = false
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));

            // Row 0
            cmbPostosEspaco = Theme.Combo();
            cmbPostoTipo    = Theme.Combo();
            cmbPostoTipo.Items.AddRange(new object[] { "Flex", "Fixo", "Privado" });

            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Espaço *"),      cmbPostosEspaco),       0, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Código *"),       txtPostoCodigo = Theme.Field()), 1, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Tipo *"),         cmbPostoTipo),          2, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Preço/Hora *"),   txtPostoPreco  = Theme.Field()), 3, 0);

            // Row 1 — Estado combo (col 0)
            cmbPostoEstado = Theme.Combo();
            cmbPostoEstado.Items.AddRange(new object[] { "Disponivel", "Indisponivel", "Manutencao", "Inativo" });
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Estado *"), cmbPostoEstado), 0, 1);

            // Row 2: buttons
            var flowBtns = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Padding       = new Padding(0, 4, 0, 0)
            };
            var btnSave   = Theme.BtnPrim("Guardar");
            var btnCancel = Theme.BtnGray("Cancelar");
            btnSave.Click   += BtnPostoGuardar_Click;
            btnCancel.Click += BtnPostoCancelar_Click;
            flowBtns.Controls.Add(btnSave);
            flowBtns.Controls.Add(btnCancel);
            tbl.SetColumnSpan(flowBtns, 4);
            tbl.Controls.Add(flowBtns, 0, 2);

            pnlPostosForm.Controls.Add(tbl);

            pnlContent.Controls.Add(pnlPostosForm);
            pnlContent.Controls.Add(dgvPostos);
            pnlContent.Controls.Add(pnlPostosToolbar);

            tab.Controls.Add(pnlContent);
        }

        private void LoadPostosEspacos()
        {
            if (cmbPostosEspaco == null) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT espaco_id, nome FROM espaco ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    cmbPostosEspaco.DataSource    = dt;
                    cmbPostosEspaco.DisplayMember = "nome";
                    cmbPostosEspaco.ValueMember   = "espaco_id";
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetPostosEditMode(bool editing)
        {
            btnPostoNovo.Visible     = btnPostoEditar.Visible     = btnPostoEliminar.Visible = !editing;
            btnPostoGuardar.Visible  = btnPostoCancelar.Visible   = editing;
            dgvPostos.Enabled        = !editing;
            pnlPostosForm.Visible    = editing;
            if (!editing)
                btnPostoEditar.Enabled = btnPostoEliminar.Enabled = dgvPostos.SelectedRows.Count > 0;
        }

        private void LoadPostosData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT p.recurso_id AS ID, e.nome AS Espaço, p.codigo AS Código,
                             p.tipo_posto AS Tipo, p.preco_hora AS [Preço/Hora], p.estado AS Estado
                      FROM posto_trabalho p
                      JOIN espaco e ON p.espaco_id=e.espaco_id
                      ORDER BY e.nome, p.codigo", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgvPostos.DataSource = dt;
                    if (dgvPostos.Columns.Contains("ID"))
                        dgvPostos.Columns["ID"].Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvPostos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPostos.SelectedRows.Count == 0) return;
            var row = dgvPostos.SelectedRows[0];
            _editIdPosto = Convert.ToInt32(row.Cells["ID"].Value);

            txtPostoCodigo.Text = row.Cells["Código"].Value?.ToString() ?? "";
            txtPostoPreco.Text  = row.Cells["Preço/Hora"].Value?.ToString() ?? "";

            var tipo = row.Cells["Tipo"].Value?.ToString() ?? "";
            var tipoIdx = cmbPostoTipo.Items.IndexOf(tipo);
            cmbPostoTipo.SelectedIndex = tipoIdx >= 0 ? tipoIdx : 0;

            var estado = row.Cells["Estado"].Value?.ToString() ?? "";
            var estadoIdx = cmbPostoEstado.Items.IndexOf(estado);
            cmbPostoEstado.SelectedIndex = estadoIdx >= 0 ? estadoIdx : 0;

            btnPostoEditar.Enabled   = true;
            btnPostoEliminar.Enabled = true;
        }

        private void DgvPostos_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvPostos.Columns.Count == 0 || e.Value == null) return;
            if (dgvPostos.Columns[e.ColumnIndex].Name == "Preço/Hora")
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal val))
                {
                    e.Value = Theme.FormatEuro(val);
                    e.FormattingApplied = true;
                }
            }
        }

        private void BtnPostoNovo_Click(object sender, EventArgs e)
        {
            _editIdPosto               = -1;
            txtPostoCodigo.Text        = "";
            txtPostoPreco.Text         = "";
            cmbPostoTipo.SelectedIndex  = 0;
            cmbPostoEstado.SelectedIndex = 0;
            cmbPostosEspaco.Enabled    = true;
            if (cmbPostosEspaco.Items.Count > 0)
                cmbPostosEspaco.SelectedIndex = 0;
            SetPostosEditMode(true);
            txtPostoCodigo.Focus();
        }

        private void BtnPostoEditar_Click(object sender, EventArgs e)
        {
            if (_editIdPosto < 0) return;
            cmbPostosEspaco.Enabled = false; // cannot change espaco of existing posto
            SetPostosEditMode(true);
            txtPostoCodigo.Focus();
        }

        private void BtnPostoEliminar_Click(object sender, EventArgs e)
        {
            if (_editIdPosto < 0) return;
            var res = MessageBox.Show("Eliminar o posto selecionado?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) return;

            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand(
                        "SELECT COUNT(*) FROM reserva WHERE recurso_id=@id AND estado NOT IN ('Cancelada','Concluida')", conn))
                    {
                        chk.Parameters.AddWithValue("@id", _editIdPosto);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — o posto tem reservas ativas.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM recurso WHERE recurso_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _editIdPosto);
                        cmd.ExecuteNonQuery();
                    }
                }
                _editIdPosto = -1;
                LoadPostosData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPostoGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPostoCodigo.Text))
            {
                MessageBox.Show("Código é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtPostoPreco.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal preco) || preco < 0)
            {
                MessageBox.Show("Preço inválido.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbPostoTipo.SelectedItem == null)
            {
                MessageBox.Show("Tipo é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbPostoEstado.SelectedItem == null)
            {
                MessageBox.Show("Estado é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_editIdPosto < 0 && cmbPostosEspaco.SelectedValue == null)
            {
                MessageBox.Show("Espaço é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tipo   = cmbPostoTipo.SelectedItem.ToString();
            var estado = cmbPostoEstado.SelectedItem.ToString();

            try
            {
                using (var conn = Database.GetConnection())
                {
                    SqlCommand cmd;
                    if (_editIdPosto < 0)
                    {
                        var espacoId = Convert.ToInt32(cmbPostosEspaco.SelectedValue);
                        int newRecursoId;
                        using (var ins = new SqlCommand(
                            "INSERT INTO recurso (tipo) VALUES ('Posto'); SELECT SCOPE_IDENTITY()", conn))
                            newRecursoId = Convert.ToInt32(ins.ExecuteScalar());
                        cmd = new SqlCommand(
                            "INSERT INTO posto_trabalho (recurso_id,codigo,tipo_posto,preco_hora,estado,espaco_id) " +
                            "VALUES (@rid,@c,@t,@p,@e,@eid)", conn);
                        cmd.Parameters.AddWithValue("@rid", newRecursoId);
                        cmd.Parameters.AddWithValue("@eid", espacoId);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            "UPDATE posto_trabalho SET codigo=@c,tipo_posto=@t,preco_hora=@p,estado=@e " +
                            "WHERE recurso_id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", _editIdPosto);
                    }

                    cmd.Parameters.AddWithValue("@c", txtPostoCodigo.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", tipo);
                    cmd.Parameters.AddWithValue("@p", preco);
                    cmd.Parameters.AddWithValue("@e", estado);

                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }

                SetPostosEditMode(false);
                LoadPostosData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPostoCancelar_Click(object sender, EventArgs e)
        {
            SetPostosEditMode(false);
        }

        // ════════════════════════════════════════════════════════════════════
        // SHARED HELPERS
        // ════════════════════════════════════════════════════════════════════

        private Panel MakeFieldCell(Label lbl, Control field)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0) };
            // Dock=Top: add field first so it sits below the label
            p.Controls.Add(field);
            p.Controls.Add(lbl);
            return p;
        }
    }
}
