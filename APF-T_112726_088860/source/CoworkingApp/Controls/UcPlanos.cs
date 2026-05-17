using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcPlanos : UserControl
    {
        private DataGridView dgv;
        private Button btnNovo, btnEditar, btnEliminar;
        private int _selectedId = -1;

        public UcPlanos()
        {
            this.BackColor = Theme.PageBg;
            this.Dock = DockStyle.Fill;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.PageBg, Padding = new Padding(20, 14, 20, 0) };
            pnlTitle.Controls.Add(new Label { Text = "Planos", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill, AutoSize = false });

            var pnlToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();
            btnNovo     = Theme.BtnPrim("+ Novo");
            btnEditar   = Theme.BtnGray("Editar");
            btnEliminar = Theme.BtnRed("Eliminar");
            btnEditar.Enabled = false;
            btnEliminar.Enabled = false;
            btnNovo.Click     += (s, e) => OpenEditor(null);
            btnEditar.Click   += (s, e) => OpenEditor(_selectedId);
            btnEliminar.Click += BtnEliminar_Click;
            flow.Controls.Add(btnNovo);
            flow.Controls.Add(btnEditar);
            flow.Controls.Add(btnEliminar);
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
                if (dgv.Columns[e.ColumnIndex].Name == "Preço/Mês")
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

        private void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT plano_id AS ID, nome_plano AS Nome, tipo_plano AS Tipo,
                             preco_mensal AS [Preço/Mês],
                             duracao_meses AS [Duração (meses)],
                             descricao AS Descrição
                      FROM plano
                      ORDER BY nome_plano", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgv.DataSource = dt;
                    if (dgv.Columns.Contains("ID")) dgv.Columns["ID"].Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var txtNome    = UcClientes.AddField(tbl, "Nome *");
            var cmbTipo    = UcClientes.AddCombo(tbl, "Tipo *", new[] { "Flex", "Fixo", "Privado" });
            var txtPreco   = UcClientes.AddField(tbl, "Preço mensal *");
            var txtDuracao = UcClientes.AddField(tbl, "Duração (meses) *");
            var txtDesc    = UcClientes.AddField(tbl, "Descrição");

            cmbTipo.SelectedIndex = 0;

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT nome_plano, tipo_plano, preco_mensal, duracao_meses, descricao FROM plano WHERE plano_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtNome.Text = r["nome_plano"]?.ToString() ?? "";
                            var tipo = r["tipo_plano"]?.ToString() ?? "Flex";
                            var idx = cmbTipo.Items.IndexOf(tipo);
                            cmbTipo.SelectedIndex = idx >= 0 ? idx : 0;
                            txtPreco.Text = Convert.ToDecimal(r["preco_mensal"]).ToString(CultureInfo.InvariantCulture);
                            txtDuracao.Text = r["duracao_meses"]?.ToString() ?? "";
                            txtDesc.Text = r["descricao"] is DBNull ? "" : r["descricao"].ToString();
                        }
                    }
                }
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Plano" : "Novo Plano", tbl, 380, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (cmbTipo.SelectedItem == null) throw new ApplicationException("Tipo é obrigatório.");
                if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0)
                    throw new ApplicationException("Preço inválido.");
                if (!int.TryParse(txtDuracao.Text, out int dur) || dur <= 0)
                    throw new ApplicationException("Duração inválida (inteiro > 0).");

                var sql = id.HasValue
                    ? "UPDATE plano SET nome_plano=@n, tipo_plano=@tp, preco_mensal=@p, duracao_meses=@d, descricao=@desc WHERE plano_id=@id"
                    : "INSERT INTO plano (nome_plano, tipo_plano, preco_mensal, duracao_meses, descricao) VALUES (@n,@tp,@p,@d,@desc)";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@tp", cmbTipo.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@p", preco);
                    cmd.Parameters.AddWithValue("@d", dur);
                    cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(txtDesc.Text) ? (object)DBNull.Value : txtDesc.Text.Trim());
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
            if (MessageBox.Show("Eliminar plano?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM adesao WHERE plano_id=@id", conn))
                    {
                        chk.Parameters.AddWithValue("@id", _selectedId);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — plano tem adesões.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM plano WHERE plano_id=@id", conn))
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
