using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcAdesoes : UserControl
    {
        private DataGridView dgv;
        private Button btnNovo, btnEditar, btnEliminar;
        private ComboBox cmbFiltroCliente, cmbFiltroEstado;
        private int _selectedId = -1;

        public UcAdesoes()
        {
            this.BackColor = Theme.PageBg;
            this.Dock = DockStyle.Fill;
            BuildUI();
            LoadFiltroClientes();
            LoadData();
        }

        private void BuildUI()
        {
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.PageBg, Padding = new Padding(20, 14, 20, 0) };
            pnlTitle.Controls.Add(new Label { Text = "Adesões", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill, AutoSize = false });

            var pnlToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();
            btnNovo = Theme.BtnPrim("+ Novo");
            btnEditar = Theme.BtnGray("Editar");
            btnEliminar = Theme.BtnRed("Eliminar");
            btnEditar.Enabled = false;
            btnEliminar.Enabled = false;
            btnNovo.Click += (s, e) => OpenEditor(null);
            btnEditar.Click += (s, e) => OpenEditor(_selectedId);
            btnEliminar.Click += BtnEliminar_Click;

            cmbFiltroCliente = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 4, 0, 0), BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary };
            cmbFiltroEstado = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 4, 0, 0), BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary };
            cmbFiltroEstado.Items.AddRange(new object[] { "(Todos)", "Pendente", "Ativa", "Suspensa", "Cancelada", "Terminada" });
            cmbFiltroEstado.SelectedIndex = 0;
            cmbFiltroCliente.SelectedIndexChanged += (s, e) => LoadData();
            cmbFiltroEstado.SelectedIndexChanged += (s, e) => LoadData();

            flow.Controls.Add(btnNovo);
            flow.Controls.Add(btnEditar);
            flow.Controls.Add(btnEliminar);
            flow.Controls.Add(cmbFiltroCliente);
            flow.Controls.Add(cmbFiltroEstado);
            pnlToolbar.Controls.Add(flow);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) { _selectedId = -1; btnEditar.Enabled = btnEliminar.Enabled = false; return; }
                _selectedId = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);
                btnEditar.Enabled = btnEliminar.Enabled = true;
            };
            dgv.CellFormatting += (s, e) =>
            {
                if (dgv.Columns.Count == 0 || e.Value == null) return;
                Theme.ApplyStatusColor(e, "Estado", dgv);
                if (dgv.Columns[e.ColumnIndex].Name == "Preço Acordado")
                {
                    if (decimal.TryParse(e.Value.ToString(), out decimal val))
                    {
                        e.Value = Theme.FormatEuro(val);
                        e.FormattingApplied = true;
                    }
                }
            };

            this.Controls.Add(dgv);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlTitle);
        }

        private void LoadFiltroClientes()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    var rowTodos = dt.NewRow();
                    rowTodos["cliente_id"] = DBNull.Value;
                    rowTodos["nome"] = "(Todos)";
                    dt.Rows.InsertAt(rowTodos, 0);
                    cmbFiltroCliente.DataSource = dt;
                    cmbFiltroCliente.DisplayMember = "nome";
                    cmbFiltroCliente.ValueMember = "cliente_id";
                    cmbFiltroCliente.SelectedIndex = 0;
                }
            }
            catch (SqlException) { /* ignore */ }
        }

        private void LoadData()
        {
            try
            {
                var whereParts = new System.Collections.Generic.List<string>();
                string estadoFiltro = cmbFiltroEstado?.SelectedIndex > 0 ? cmbFiltroEstado.Text : null;
                if (estadoFiltro != null) whereParts.Add("a.estado = @e");
                bool filtraCliente = cmbFiltroCliente?.SelectedIndex > 0
                    && cmbFiltroCliente.SelectedValue != null
                    && !(cmbFiltroCliente.SelectedValue is DBNull);
                if (filtraCliente) whereParts.Add("a.cliente_id = @c");
                string where = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                string sql = $@"
                    SELECT a.adesao_id AS ID, c.nome AS Cliente, p.nome_plano AS Plano,
                           p.tipo_plano AS Tipo,
                           CASE WHEN a.recurso_id IS NULL THEN '—' ELSE COALESCE(po.codigo, 'Sala '+s.nome) END AS Posto,
                           CONVERT(varchar,a.data_inicio,103) AS [Data Início],
                           CONVERT(varchar,a.data_fim,103) AS [Data Fim],
                           a.preco_acordado AS [Preço Acordado],
                           a.estado AS Estado
                    FROM adesao a
                    JOIN cliente c ON a.cliente_id=c.cliente_id
                    JOIN plano p ON a.plano_id=p.plano_id
                    LEFT JOIN posto po ON a.recurso_id=po.recurso_id
                    LEFT JOIN sala s ON a.recurso_id=s.recurso_id
                    {where}
                    ORDER BY a.data_inicio DESC";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (estadoFiltro != null) cmd.Parameters.AddWithValue("@e", estadoFiltro);
                    if (filtraCliente) cmd.Parameters.AddWithValue("@c", Convert.ToInt32(cmbFiltroCliente.SelectedValue));
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dgv.DataSource = dt;
                        if (dgv.Columns.Contains("ID")) dgv.Columns["ID"].Visible = false;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEditor(int? id)
        {
            // Carregar combos data
            DataTable dsClientes, dsPlanos;
            using (var conn = Database.GetConnection())
            {
                using (var c = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var a = new SqlDataAdapter(c)) { dsClientes = new DataTable(); a.Fill(dsClientes); }
                using (var c = new SqlCommand("SELECT plano_id, nome_plano, tipo_plano, preco_mensal FROM plano ORDER BY nome_plano", conn))
                using (var a = new SqlDataAdapter(c)) { dsPlanos = new DataTable(); a.Fill(dsPlanos); }
            }

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var cmbCliente = UcClientes.AddComboDataSource(tbl, "Cliente *", dsClientes, "nome", "cliente_id");
            var cmbPlano   = UcClientes.AddComboDataSource(tbl, "Plano *", dsPlanos, "nome_plano", "plano_id");
            var dtInicio   = UcClientes.AddDate(tbl, "Data início *");
            var cmbPosto   = UcClientes.AddCombo(tbl, "Posto atribuído *", new string[0]);
            var txtPreco   = UcClientes.AddField(tbl, "Preço acordado *");
            var cmbEstado  = UcClientes.AddCombo(tbl, "Estado *", new[] { "Pendente", "Ativa", "Suspensa", "Cancelada", "Terminada" });
            cmbEstado.SelectedIndex = 0;

            // ValueMember para cmbPosto
            cmbPosto.DisplayMember = "label";
            cmbPosto.ValueMember = "recurso_id";

            // Helper para carregar postos de um tipo
            Action<string> loadPostos = (tipo) =>
            {
                using (var conn = Database.GetConnection())
                using (var c = new SqlCommand(
                    "SELECT p.recurso_id, e.nome + ' / ' + p.codigo AS label FROM posto p JOIN espaco e ON p.espaco_id=e.espaco_id WHERE p.tipo_posto=@t AND p.estado='Disponivel' ORDER BY label", conn))
                using (var a = new SqlDataAdapter(c))
                {
                    c.Parameters.AddWithValue("@t", tipo);
                    var dt = new DataTable();
                    a.Fill(dt);
                    cmbPosto.DataSource = dt;
                }
            };

            string tipoPlanoSel = "Flex";
            decimal precoMensalSel = 0;

            cmbPlano.SelectedIndexChanged += (s, e) =>
            {
                if (cmbPlano.SelectedItem == null) return;
                var rv = (DataRowView)cmbPlano.SelectedItem;
                tipoPlanoSel = rv["tipo_plano"].ToString();
                precoMensalSel = Convert.ToDecimal(rv["preco_mensal"]);
                txtPreco.Text = precoMensalSel.ToString(CultureInfo.InvariantCulture);
                if (tipoPlanoSel == "Flex")
                {
                    cmbPosto.Parent.Visible = false;
                    cmbPosto.DataSource = null;
                }
                else
                {
                    cmbPosto.Parent.Visible = true;
                    loadPostos(tipoPlanoSel);
                }
            };

            // Pre-load se editing
            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT a.cliente_id, a.plano_id, a.recurso_id, a.data_inicio, a.preco_acordado, a.estado, p.tipo_plano, p.preco_mensal
                      FROM adesao a JOIN plano p ON a.plano_id=p.plano_id WHERE a.adesao_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            cmbCliente.SelectedValue = r["cliente_id"];
                            cmbPlano.SelectedValue = r["plano_id"];
                            // SelectedIndexChanged já triggered — carrega postos se necessário
                            tipoPlanoSel = r["tipo_plano"].ToString();
                            if (tipoPlanoSel != "Flex" && r["recurso_id"] != DBNull.Value)
                            {
                                cmbPosto.SelectedValue = r["recurso_id"];
                            }
                            dtInicio.Value = Convert.ToDateTime(r["data_inicio"]);
                            txtPreco.Text = Convert.ToDecimal(r["preco_acordado"]).ToString(CultureInfo.InvariantCulture);
                            var estado = r["estado"].ToString();
                            var idx = cmbEstado.Items.IndexOf(estado);
                            cmbEstado.SelectedIndex = idx >= 0 ? idx : 0;
                        }
                    }
                }
            }
            else
            {
                // Force SelectedIndexChanged to fire even if SelectedIndex was already 0
                if (cmbPlano.Items.Count > 0)
                {
                    cmbPlano.SelectedIndex = -1;
                    cmbPlano.SelectedIndex = 0;
                }
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Adesão" : "Nova Adesão", tbl, 460, () =>
            {
                if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull) throw new ApplicationException("Cliente é obrigatório.");
                if (cmbPlano.SelectedValue == null || cmbPlano.SelectedValue is DBNull) throw new ApplicationException("Plano é obrigatório.");
                if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0)
                    throw new ApplicationException("Preço acordado inválido.");

                object recursoVal = DBNull.Value;
                if (tipoPlanoSel != "Flex")
                {
                    if (cmbPosto.SelectedValue == null || cmbPosto.SelectedValue is DBNull)
                        throw new ApplicationException("Posto atribuído é obrigatório para planos Fixo/Privado.");
                    recursoVal = Convert.ToInt32(cmbPosto.SelectedValue);
                }

                var sql = id.HasValue
                    ? "UPDATE adesao SET cliente_id=@c, plano_id=@p, recurso_id=@r, data_inicio=@d, preco_acordado=@pr, estado=@e WHERE adesao_id=@id"
                    : "INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado) VALUES (@c,@p,@r,@d,@pr,@e)";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@c", Convert.ToInt32(cmbCliente.SelectedValue));
                    cmd.Parameters.AddWithValue("@p", Convert.ToInt32(cmbPlano.SelectedValue));
                    cmd.Parameters.AddWithValue("@r", recursoVal);
                    cmd.Parameters.AddWithValue("@d", dtInicio.Value.Date);
                    cmd.Parameters.AddWithValue("@pr", preco);
                    cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedItem.ToString());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (_selectedId < 0) return;
            if (MessageBox.Show("Eliminar adesão?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM pagamento WHERE adesao_id=@id", conn))
                    {
                        chk.Parameters.AddWithValue("@id", _selectedId);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — adesão tem pagamentos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM adesao WHERE adesao_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
