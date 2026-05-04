using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcClientes : UserControl
    {
        private DataGridView dgv;
        private Panel pnlForm;

        private Button btnNovo, btnEditar, btnEliminar, btnGuardar, btnCancelar;

        private TextBox txtNome, txtNif, txtEmail, txtTelefone;

        private int _editId = -1;
        private TextBox txtSearch;
        private Label   lblCount;
        private Label   _lblStatus;

        public UcClientes()
        {
            this.BackColor = Theme.ContentBg;
            this.Dock = DockStyle.Fill;

            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            // ── Title panel (Dock=Top) ───────────────────────────────────────
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.ContentBg,
                Padding = new Padding(20, 12, 0, 0)
            };
            var lblTitle = new Label
            {
                Text = "Clientes",
                Font = Theme.FontTitle,
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            pnlTitle.Controls.Add(lblTitle);

            // ── Toolbar (Dock=Top) ───────────────────────────────────────────
            var pnlToolbar = Theme.Toolbar();
            var flp = Theme.ToolbarFlow();

            btnNovo     = Theme.BtnPrim("+ Novo");
            btnEditar   = Theme.BtnGray("Editar");
            btnEliminar = Theme.BtnRed("Eliminar");
            btnGuardar  = Theme.BtnPrim("Guardar");
            btnCancelar = Theme.BtnGray("Cancelar");

            btnGuardar.Visible  = false;
            btnCancelar.Visible = false;

            _lblStatus = new Label
            {
                AutoSize  = true,
                Visible   = false,
                Margin    = new Padding(16, 8, 0, 0),
                Font      = Theme.FontSub
            };
            flp.Controls.AddRange(new Control[]
            {
                btnNovo, btnEditar, btnEliminar, btnGuardar, btnCancelar, _lblStatus
            });
            pnlToolbar.Controls.Add(flp);

            // ── DataGridView (Dock=Fill) ─────────────────────────────────────
            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);

            // ── Form panel (Dock=Bottom) ─────────────────────────────────────
            pnlForm = Theme.FormPanel(120);

            // TableLayoutPanel: 4 columns, 3 rows (label row, field row, button row)
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3,
                BackColor = Color.White
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));  // labels
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));  // fields
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // buttons

            // Row 0 — labels
            tbl.Controls.Add(Theme.FieldLabel("Nome *"),     0, 0);
            tbl.Controls.Add(Theme.FieldLabel("NIF *"),      1, 0);
            tbl.Controls.Add(Theme.FieldLabel("Email *"),    2, 0);
            tbl.Controls.Add(Theme.FieldLabel("Telefone"),   3, 0);

            // Row 1 — fields
            txtNome     = Theme.Field();
            txtNif      = Theme.Field();
            txtEmail    = Theme.Field();
            txtTelefone = Theme.Field();

            tbl.Controls.Add(txtNome,     0, 1);
            tbl.Controls.Add(txtNif,      1, 1);
            tbl.Controls.Add(txtEmail,    2, 1);
            tbl.Controls.Add(txtTelefone, 3, 1);

            // Row 2 — save/cancel buttons
            var flpForm = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Color.White
            };
            var btnGuardarForm  = Theme.BtnPrim("Guardar");
            var btnCancelarForm = Theme.BtnGray("Cancelar");
            btnGuardarForm.Click  += BtnGuardar_Click;
            btnCancelarForm.Click += BtnCancelar_Click;
            flpForm.Controls.Add(btnGuardarForm);
            flpForm.Controls.Add(btnCancelarForm);
            tbl.Controls.Add(flpForm, 0, 2);
            tbl.SetColumnSpan(flpForm, 4);

            pnlForm.Controls.Add(tbl);

            // ── Wire up toolbar buttons ──────────────────────────────────────
            btnNovo.Click     += BtnNovo_Click;
            btnEditar.Click   += BtnEditar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnGuardar.Click  += BtnGuardar_Click;
            btnCancelar.Click += BtnCancelar_Click;

            // ── Wire up grid selection ───────────────────────────────────────
            dgv.SelectionChanged += Dgv_SelectionChanged;

            // ── Add controls in correct dock order ───────────────────────────
            // WinForms processes Dock in reverse add order for Top/Bottom.
            // Add Bottom first, then Fill, then Top panels last — so Top panels
            // claim their space first (last added = first processed for Top).
            // ── Search bar panel (Dock=Top) ──────────────────────────────────
            var pnlSearch = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                BackColor = Color.White,
                Padding   = new Padding(12, 6, 12, 4)
            };
            var flpSearch = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false
            };
            var lblSearchLabel = new Label
            {
                Text      = "Pesquisar:",
                Font      = Theme.FontLabel,
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                AutoSize  = true,
                Margin    = new Padding(0, 6, 6, 0)
            };
            txtSearch = new TextBox
            {
                Width       = 320,
                Font        = Theme.FontBase,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = ColorTranslator.FromHtml("#f8fafc"),
                Margin      = new Padding(0, 2, 12, 0)
            };
            lblCount = new Label
            {
                AutoSize  = true,
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                Font      = Theme.FontSub,
                Margin    = new Padding(0, 7, 0, 0)
            };
            txtSearch.TextChanged += (s, e) => LoadData(txtSearch.Text.Trim());
            flpSearch.Controls.Add(lblSearchLabel);
            flpSearch.Controls.Add(txtSearch);
            flpSearch.Controls.Add(lblCount);
            pnlSearch.Controls.Add(flpSearch);

            this.Controls.Add(pnlForm);     // Bottom
            this.Controls.Add(dgv);         // Fill
            this.Controls.Add(pnlSearch);   // Top (third)
            this.Controls.Add(pnlToolbar);  // Top (second)
            this.Controls.Add(pnlTitle);    // Top (first — outermost)

            // Initial button state
            UpdateButtonState();
        }

        // ────────────────────────────────────────────────────────────────────
        // Button state helpers
        // ────────────────────────────────────────────────────────────────────

        private void SetEditMode(bool editing)
        {
            btnNovo.Visible     = !editing;
            btnEditar.Visible   = !editing;
            btnEliminar.Visible = !editing;
            btnGuardar.Visible  = editing;
            btnCancelar.Visible = editing;
            dgv.Enabled   = !editing;
            pnlForm.Visible = editing;
        }

        private void UpdateButtonState()
        {
            bool hasRow = dgv.SelectedRows.Count > 0 && dgv.Enabled;
            btnEditar.Enabled   = hasRow;
            btnEliminar.Enabled = hasRow;
        }

        // ────────────────────────────────────────────────────────────────────
        // Grid events
        // ────────────────────────────────────────────────────────────────────

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonState();

            if (dgv.SelectedRows.Count == 0) return;

            var row = dgv.SelectedRows[0];
            _editId = Convert.ToInt32(row.Cells["ID"].Value);
            txtNome.Text     = row.Cells["Nome"].Value?.ToString()     ?? "";
            txtNif.Text      = row.Cells["NIF"].Value?.ToString()      ?? "";
            txtEmail.Text    = row.Cells["Email"].Value?.ToString()    ?? "";
            txtTelefone.Text = row.Cells["Telefone"].Value?.ToString() ?? "";
        }

        // ────────────────────────────────────────────────────────────────────
        // Toolbar button handlers
        // ────────────────────────────────────────────────────────────────────

        private void BtnNovo_Click(object sender, EventArgs e)
        {
            _editId = -1;
            txtNome.Text     = "";
            txtNif.Text      = "";
            txtEmail.Text    = "";
            txtTelefone.Text = "";
            SetEditMode(true);
            txtNome.Focus();
        }

        private void BtnEditar_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;
            SetEditMode(true);
            txtNome.Focus();
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;

            var result = MessageBox.Show(
                "Eliminar o cliente selecionado?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    "DELETE FROM cliente WHERE cliente_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtNome.Text) ||
                string.IsNullOrWhiteSpace(txtNif.Text)  ||
                string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Nome, NIF e Email são obrigatórios.", "Validação",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = Database.GetConnection())
                {
                    string sql;
                    if (_editId == -1)
                        sql = "INSERT INTO cliente (nome,nif,email,telefone) VALUES (@n,@nif,@e,@t)";
                    else
                        sql = "UPDATE cliente SET nome=@n,nif=@nif,email=@e,telefone=@t WHERE cliente_id=@id";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n",   txtNome.Text.Trim());
                        cmd.Parameters.AddWithValue("@nif", txtNif.Text.Trim());
                        cmd.Parameters.AddWithValue("@e",   txtEmail.Text.Trim());

                        // Telefone is optional
                        if (string.IsNullOrWhiteSpace(txtTelefone.Text))
                            cmd.Parameters.AddWithValue("@t", (object)DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@t", txtTelefone.Text.Trim());

                        if (_editId > 0)
                            cmd.Parameters.AddWithValue("@id", _editId);

                        cmd.ExecuteNonQuery();
                    }
                }

                Theme.ShowSuccess(_lblStatus);
                SetEditMode(false);
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            SetEditMode(false);
            UpdateButtonState();
        }

        // ────────────────────────────────────────────────────────────────────
        // Data
        // ────────────────────────────────────────────────────────────────────

        private void LoadData(string filter = "")
        {
            try
            {
                string where = string.IsNullOrWhiteSpace(filter)
                    ? ""
                    : "WHERE nome LIKE @q OR nif LIKE @q OR email LIKE @q";

                var sql = $@"SELECT cliente_id AS ID,
                                   nome        AS Nome,
                                   nif         AS NIF,
                                   email       AS Email,
                                   telefone    AS Telefone,
                                   CONVERT(varchar,data_registo,103) AS [Data Registo]
                            FROM cliente
                            {where}
                            ORDER BY nome";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(filter))
                        cmd.Parameters.AddWithValue("@q", "%" + filter + "%");

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dgv.DataSource = dt;
                    }
                }

                if (dgv.Columns.Contains("ID"))
                    dgv.Columns["ID"].Visible = false;

                int count = dgv.Rows.Count;
                if (lblCount != null)
                    lblCount.Text = count + " cliente" + (count != 1 ? "s" : "");

                UpdateButtonState();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
