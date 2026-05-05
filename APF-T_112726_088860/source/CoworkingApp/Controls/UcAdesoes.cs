using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcAdesoes : UserControl
    {
        // ── Grid & form panel ────────────────────────────────────────────────
        private DataGridView dgv;
        private Panel pnlForm;

        // ── Toolbar buttons ──────────────────────────────────────────────────
        private Button btnNovo, btnEditar, btnEliminar, btnGuardar, btnCancelar;

        // ── Form fields ──────────────────────────────────────────────────────
        private ComboBox cmbCliente, cmbPlano, cmbEstado;
        private DateTimePicker dtpDataInicio;
        private Label lblDataFimInfo;

        // ── State ────────────────────────────────────────────────────────────
        private int _editId = -1;
        private ComboBox cmbFiltroCliente, cmbFiltroEstado;
        private Label _lblStatus;

        // ────────────────────────────────────────────────────────────────────
        public UcAdesoes()
        {
            this.BackColor = Theme.ContentBg;
            this.Dock = DockStyle.Fill;

            InitUI();
            LoadCombos();
            LoadData();
        }

        // ────────────────────────────────────────────────────────────────────
        // UI construction
        // ────────────────────────────────────────────────────────────────────

        private void InitUI()
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
                Text = "Adesões",
                Font = Theme.FontTitle,
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            pnlTitle.Controls.Add(lblTitle);

            // ── Toolbar (Dock=Top) ───────────────────────────────────────────
            var pnlToolbar = Theme.Toolbar();
            var flpToolbar = Theme.ToolbarFlow();

            btnNovo     = Theme.BtnPrim("+ Novo");
            btnEditar   = Theme.BtnGray("Editar");
            btnEliminar = Theme.BtnRed("Eliminar");
            btnGuardar  = Theme.BtnPrim("Guardar");
            btnCancelar = Theme.BtnGray("Cancelar");

            btnEditar.Enabled   = false;
            btnEliminar.Enabled = false;
            btnGuardar.Visible  = false;
            btnCancelar.Visible = false;

            btnNovo.Click     += BtnNovo_Click;
            btnEditar.Click   += BtnEditar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnGuardar.Click  += BtnGuardar_Click;
            btnCancelar.Click += BtnCancelar_Click;

            _lblStatus = new Label { AutoSize = true, Visible = false, Margin = new Padding(16, 8, 0, 0), Font = Theme.FontSub };
            flpToolbar.Controls.AddRange(new Control[]
            {
                btnNovo, btnEditar, btnEliminar, btnGuardar, btnCancelar, _lblStatus
            });
            pnlToolbar.Controls.Add(flpToolbar);

            // ── DataGridView (Dock=Fill) ─────────────────────────────────────
            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);
            dgv.SelectionChanged += Dgv_SelectionChanged;
            dgv.CellFormatting   += (s, e) => Theme.ApplyStatusColor(e, "Estado", dgv);

            // ── Form panel (Dock=Bottom, height=160) ─────────────────────────
            pnlForm = Theme.FormPanel(160);

            // TableLayoutPanel: 2 rows x 3 columns
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,       // row0: fields, row1: info+calc, row2: buttons
                BackColor = Color.White
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));   // row 0: combos
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));   // row 1: estado + info
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // row 2: buttons

            // ── Row 0: Cliente | Plano | Data Início ────────────────────────
            cmbCliente    = Theme.Combo();
            cmbPlano      = Theme.Combo();
            dtpDataInicio = Theme.DatePicker();

            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Cliente *"),    cmbCliente),    0, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Plano *"),      cmbPlano),      1, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Data Início *"), dtpDataInicio), 2, 0);

            // ── Row 1: Estado | Data Fim info (spanning 2 cols) ──────────────
            cmbEstado = Theme.Combo();
            cmbEstado.Items.AddRange(new object[]
            {
                "Pendente", "Ativa", "Suspensa", "Cancelada", "Terminada"
            });

            lblDataFimInfo = new Label
            {
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                Font = new Font("Segoe UI", 9f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Data fim calculada: —"
            };

            var pnlDataFim = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 20, 0, 0) };
            pnlDataFim.Controls.Add(lblDataFimInfo);

            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Estado *"), cmbEstado), 0, 1);
            tbl.Controls.Add(pnlDataFim, 1, 1);
            tbl.SetColumnSpan(pnlDataFim, 2);

            // ── Row 2: Guardar / Cancelar ────────────────────────────────────
            var flpForm = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0),
                BackColor = Color.White
            };
            var btnSave   = Theme.BtnPrim("Guardar");
            var btnCancel = Theme.BtnGray("Cancelar");
            btnSave.Click   += BtnGuardar_Click;
            btnCancel.Click += BtnCancelar_Click;
            flpForm.Controls.Add(btnSave);
            flpForm.Controls.Add(btnCancel);
            tbl.SetColumnSpan(flpForm, 3);
            tbl.Controls.Add(flpForm, 0, 2);

            pnlForm.Controls.Add(tbl);

            // ── Filter panel (Dock=Top) ──────────────────────────────────────
            var pnlFiltros = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.White,
                Padding   = new Padding(12, 8, 12, 4)
            };
            var flpFiltros = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false
            };

            var lblCli = new Label { Text = "Cliente:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = Theme.FontLabel, ForeColor = ColorTranslator.FromHtml("#64748b") };
            cmbFiltroCliente = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Font = Theme.FontBase, Margin = new Padding(0, 3, 8, 0) };
            var lblEst = new Label { Text = "Estado:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = Theme.FontLabel, ForeColor = ColorTranslator.FromHtml("#64748b") };
            cmbFiltroEstado = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Font = Theme.FontBase, Margin = new Padding(0, 3, 8, 0) };
            cmbFiltroEstado.Items.AddRange(new object[] { "(Todos)", "Pendente", "Ativa", "Suspensa", "Cancelada", "Terminada" });
            cmbFiltroEstado.SelectedIndex = 0;

            var btnFiltrar = Theme.BtnPrim("Filtrar");
            var btnLimpar  = Theme.BtnGray("Limpar");
            btnFiltrar.Margin = new Padding(0, 3, 4, 0);
            btnLimpar.Margin  = new Padding(0, 3, 0, 0);
            btnFiltrar.Click += (s, e) => LoadData();
            btnLimpar.Click  += (s, e) =>
            {
                cmbFiltroCliente.SelectedIndex = 0;
                cmbFiltroEstado.SelectedIndex  = 0;
                LoadData();
            };

            flpFiltros.Controls.AddRange(new Control[] { lblCli, cmbFiltroCliente, lblEst, cmbFiltroEstado, btnFiltrar, btnLimpar });
            pnlFiltros.Controls.Add(flpFiltros);

            this.Controls.Add(pnlForm);     // Dock=Bottom  — added first
            this.Controls.Add(dgv);         // Dock=Fill
            this.Controls.Add(pnlFiltros);  // Dock=Top
            this.Controls.Add(pnlToolbar);  // Dock=Top
            this.Controls.Add(pnlTitle);    // Dock=Top — added last → appears at very top
        }

        // Helper: wraps a label + control in a fill panel (label on top)
        private Panel MakeFieldCell(Label lbl, Control field)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0), BackColor = Color.White };
            // Controls added in reverse dock order: field (Top) first, then label (Top)
            p.Controls.Add(field);
            p.Controls.Add(lbl);
            return p;
        }

        // ────────────────────────────────────────────────────────────────────
        // Data loading
        // ────────────────────────────────────────────────────────────────────

        private void LoadCombos()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    // Load clientes
                    using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        cmbCliente.DataSource    = dt;
                        cmbCliente.DisplayMember = "nome";
                        cmbCliente.ValueMember   = "cliente_id";
                    }

                    // Load planos
                    using (var cmd = new SqlCommand("SELECT plano_id, nome_plano FROM plano ORDER BY nome_plano", conn))
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        cmbPlano.DataSource    = dt;
                        cmbPlano.DisplayMember = "nome_plano";
                        cmbPlano.ValueMember   = "plano_id";
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Wire up live data-fim preview after datasource is set
            cmbPlano.SelectedIndexChanged      += (s, e) => UpdateDataFimLabel();
            dtpDataInicio.ValueChanged         += (s, e) => UpdateDataFimLabel();

            // Populate filter client combo
            try
            {
                using (var conn2 = Database.GetConnection())
                using (var cmdFilt = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn2))
                using (var adFilt = new SqlDataAdapter(cmdFilt))
                {
                    var dtFilt = new DataTable();
                    adFilt.Fill(dtFilt);
                    var rowTodos = dtFilt.NewRow();
                    rowTodos["cliente_id"] = DBNull.Value;
                    rowTodos["nome"]       = "(Todos)";
                    dtFilt.Rows.InsertAt(rowTodos, 0);
                    cmbFiltroCliente.DataSource    = dtFilt;
                    cmbFiltroCliente.DisplayMember = "nome";
                    cmbFiltroCliente.ValueMember   = "cliente_id";
                    cmbFiltroCliente.SelectedIndex = 0;
                }
            }
            catch (SqlException ex) { System.Diagnostics.Debug.WriteLine("LoadFiltroClientes: " + ex.Message); }
        }

        private void LoadData()
        {
            try
            {
                var whereParts = new System.Collections.Generic.List<string>();

                bool filtraPorCliente = cmbFiltroCliente?.SelectedValue != null
                    && !(cmbFiltroCliente.SelectedValue is System.DBNull)
                    && cmbFiltroCliente.SelectedIndex > 0;
                if (filtraPorCliente) whereParts.Add("a.cliente_id = @c");

                bool filtraPorEstado = cmbFiltroEstado?.SelectedIndex > 0;
                if (filtraPorEstado) whereParts.Add("a.estado = @e");

                string where = whereParts.Count > 0
                    ? "WHERE " + string.Join(" AND ", whereParts)
                    : "";

                string sql = $@"
                    SELECT a.adesao_id AS ID,
                           c.nome AS Cliente,
                           p.nome_plano AS Plano,
                           CONVERT(varchar,a.data_inicio,103) AS [Início],
                           CONVERT(varchar,a.data_fim,103) AS [Fim],
                           a.estado AS Estado
                    FROM adesao a
                    JOIN cliente c ON a.cliente_id = c.cliente_id
                    JOIN plano   p ON a.plano_id   = p.plano_id
                    {where}
                    ORDER BY a.data_inicio DESC";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (filtraPorCliente) cmd.Parameters.AddWithValue("@c", Convert.ToInt32(cmbFiltroCliente.SelectedValue));
                    if (filtraPorEstado)  cmd.Parameters.AddWithValue("@e", cmbFiltroEstado.Text);

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dgv.DataSource = dt;
                    }
                }

                if (dgv.Columns.Contains("ID"))
                    dgv.Columns["ID"].Visible = false;

                UpdateButtonState();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Live data-fim calculation
        // ────────────────────────────────────────────────────────────────────

        private void UpdateDataFimLabel()
        {
            if (!pnlForm.Visible) return;
            if (cmbPlano.SelectedValue == null) return;

            try
            {
                int planoId = Convert.ToInt32(cmbPlano.SelectedValue);
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    "SELECT duracao_meses FROM plano WHERE plano_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", planoId);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return;

                    int dur = Convert.ToInt32(result);
                    DateTime dataFim = dtpDataInicio.Value.AddMonths(dur);
                    lblDataFimInfo.Text = "Data fim calculada: " + dataFim.ToString("dd/MM/yyyy");
                }
            }
            catch
            {
                // Silent — live preview only
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Edit mode
        // ────────────────────────────────────────────────────────────────────

        private void SetEditMode(bool editing)
        {
            btnNovo.Visible     = btnEditar.Visible     = btnEliminar.Visible = !editing;
            btnGuardar.Visible  = btnCancelar.Visible   = editing;
            dgv.Enabled         = !editing;
            pnlForm.Visible     = editing;

            if (!editing)
                btnEditar.Enabled = btnEliminar.Enabled = dgv.SelectedRows.Count > 0;
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

            // Reflect estado from grid
            string estado = row.Cells["Estado"].Value?.ToString() ?? "";
            int idx = cmbEstado.Items.IndexOf(estado);
            if (idx >= 0)
                cmbEstado.SelectedIndex = idx;
        }

        // ────────────────────────────────────────────────────────────────────
        // Toolbar button handlers
        // ────────────────────────────────────────────────────────────────────

        private void BtnNovo_Click(object sender, EventArgs e)
        {
            _editId = -1;

            // Enable all fields for new record
            cmbCliente.Enabled    = true;
            cmbPlano.Enabled      = true;
            dtpDataInicio.Enabled = true;

            // Reset fields
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
            if (cmbPlano.Items.Count > 0)   cmbPlano.SelectedIndex   = 0;
            dtpDataInicio.Value = DateTime.Today;
            cmbEstado.SelectedIndex = cmbEstado.Items.IndexOf("Pendente") >= 0
                ? cmbEstado.Items.IndexOf("Pendente") : 0;

            lblDataFimInfo.Text = "Data fim calculada: —";

            SetEditMode(true);
            UpdateDataFimLabel();
        }

        private void BtnEditar_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;

            var row = dgv.SelectedRows[0];

            // Reflect estado from selected row
            string estado = row.Cells["Estado"].Value?.ToString() ?? "";
            int idx = cmbEstado.Items.IndexOf(estado);
            if (idx >= 0) cmbEstado.SelectedIndex = idx;

            // Disable Cliente, Plano, DataInício — only Estado is editable
            cmbCliente.Enabled    = false;
            cmbPlano.Enabled      = false;
            dtpDataInicio.Enabled = false;

            SetEditMode(true);
            cmbEstado.Focus();
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;

            var res = MessageBox.Show(
                "Eliminar a adesão selecionada?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res != DialogResult.Yes) return;

            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM adesao WHERE adesao_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                _editId = -1;
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
            if (cmbEstado.SelectedIndex < 0)
            {
                MessageBox.Show("Selecione um estado.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_editId == -1 && cmbCliente.SelectedValue == null)
            {
                MessageBox.Show("Selecione um cliente.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_editId == -1 && cmbPlano.SelectedValue == null)
            {
                MessageBox.Show("Selecione um plano.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = Database.GetConnection())
                {
                    if (_editId == -1)
                    {
                        // INSERT
                        const string sql =
                            "INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) " +
                            "VALUES (@c, @p, @d, @e)";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@c", Convert.ToInt32(cmbCliente.SelectedValue));
                            cmd.Parameters.AddWithValue("@p", Convert.ToInt32(cmbPlano.SelectedValue));
                            cmd.Parameters.AddWithValue("@d", dtpDataInicio.Value.Date);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.Text);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // UPDATE — only estado can change
                        const string sql =
                            "UPDATE adesao SET estado=@e WHERE adesao_id=@id";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@e",  cmbEstado.Text);
                            cmd.Parameters.AddWithValue("@id", _editId);
                            cmd.ExecuteNonQuery();
                        }
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
    }
}
