using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcClientes : UserControl
    {
        private DataGridView dgv;
        private Button btnNovo, btnEditar, btnEliminar;
        private TextBox txtSearch;
        private Label lblCount;
        private int _selectedId = -1;

        public UcClientes()
        {
            this.BackColor = Theme.PageBg;
            this.Dock = DockStyle.Fill;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.PageBg, Padding = new Padding(20, 14, 20, 0) };
            pnlTitle.Controls.Add(new Label { Text = "Clientes", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill, AutoSize = false });

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

            txtSearch = new TextBox { Width = 200, Font = Theme.FontBase, BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary, Margin = new Padding(12, 4, 0, 0) };
            txtSearch.TextChanged += (s, e) => LoadData();
            lblCount = new Label { Font = Theme.FontLabel, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(8, 10, 0, 0) };

            flow.Controls.Add(btnNovo);
            flow.Controls.Add(btnEditar);
            flow.Controls.Add(btnEliminar);
            flow.Controls.Add(txtSearch);
            flow.Controls.Add(lblCount);
            pnlToolbar.Controls.Add(flow);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) { _selectedId = -1; btnEditar.Enabled = btnEliminar.Enabled = false; return; }
                _selectedId = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);
                btnEditar.Enabled = btnEliminar.Enabled = true;
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
                    @"SELECT cliente_id AS ID, nome AS Nome, nif AS NIF, email AS Email,
                             telefone AS Telefone, data_registo AS [Registo]
                      FROM cliente
                      WHERE @q='' OR nome LIKE '%'+@q+'%' OR nif LIKE '%'+@q+'%' OR email LIKE '%'+@q+'%'
                      ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    cmd.Parameters.AddWithValue("@q", txtSearch?.Text?.Trim() ?? "");
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgv.DataSource = dt;
                    if (dgv.Columns.Contains("ID")) dgv.Columns["ID"].Visible = false;
                    lblCount.Text = dt.Rows.Count + " resultados";
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
            var txtNome = AddField(tbl, "Nome *");
            var txtNif  = AddField(tbl, "NIF *");
            var txtEmail = AddField(tbl, "Email *");
            var txtTelefone = AddField(tbl, "Telefone");

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT nome, nif, email, telefone FROM cliente WHERE cliente_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtNome.Text = r["nome"]?.ToString() ?? "";
                            txtNif.Text = r["nif"]?.ToString() ?? "";
                            txtEmail.Text = r["email"]?.ToString() ?? "";
                            txtTelefone.Text = r["telefone"] is DBNull ? "" : r["telefone"].ToString();
                        }
                    }
                }
            }

            using (var dlg = new CoworkingApp.FormDialog(id.HasValue ? "Editar Cliente" : "Novo Cliente", tbl, 380, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (!Regex.IsMatch(txtNif.Text.Trim(), @"^\d{9}$")) throw new ApplicationException("NIF inválido (9 dígitos).");
                try { _ = new MailAddress(txtEmail.Text.Trim()); } catch { throw new ApplicationException("Email inválido."); }

                var sql = id.HasValue
                    ? "UPDATE cliente SET nome=@n, nif=@nif, email=@e, telefone=@t WHERE cliente_id=@id"
                    : "INSERT INTO cliente (nome, nif, email, telefone) VALUES (@n,@nif,@e,@t)";
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@nif", txtNif.Text.Trim());
                    cmd.Parameters.AddWithValue("@e", txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", string.IsNullOrWhiteSpace(txtTelefone.Text) ? (object)DBNull.Value : txtTelefone.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadData();
            }
        }

        // ── Form helpers (template para Tasks 9-13) ──────────────────────────
        // Cada helper cria um Panel Dock=Top com label+control e devolve o controlo.
        // Para acesso ao Panel wrapper (ex: esconder a row inteira), usar control.Parent:
        //   var cmb = AddCombo(tbl, "Opcional", new[]{"A","B"});
        //   cmb.Parent.Visible = false;
        internal static TextBox AddField(TableLayoutPanel tbl, string label)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var txt = Theme.Field();
            pnl.Controls.Add(txt);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return txt;
        }

        internal static ComboBox AddCombo(TableLayoutPanel tbl, string label, string[] items)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var cmb = Theme.Combo();
            if (items != null && items.Length > 0) cmb.Items.AddRange(items);
            pnl.Controls.Add(cmb);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return cmb;
        }

        internal static ComboBox AddComboDataSource(TableLayoutPanel tbl, string label, object dataSource, string display, string value)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var cmb = Theme.Combo();
            // Order matters: assign DisplayMember/ValueMember before DataSource to avoid late binding races.
            cmb.DisplayMember = display;
            cmb.ValueMember   = value;
            cmb.DataSource    = dataSource;
            pnl.Controls.Add(cmb);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return cmb;
        }

        internal static DateTimePicker AddDate(TableLayoutPanel tbl, string label)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var dt = Theme.DatePicker();
            pnl.Controls.Add(dt);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return dt;
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (_selectedId < 0) return;
            if (MessageBox.Show("Eliminar cliente?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM pagamento WHERE cliente_id=@id", conn))
                    {
                        chk.Parameters.AddWithValue("@id", _selectedId);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — cliente tem pagamentos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM cliente WHERE cliente_id=@id", conn))
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
