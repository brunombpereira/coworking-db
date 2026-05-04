using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcReservas : UserControl
    {
        // ── Grid ─────────────────────────────────────────────────────────────
        private DataGridView dgv;

        // ── Toolbar controls ─────────────────────────────────────────────────
        private Button btnNova, btnCancelarRes, btnAtualizar;
        private ComboBox cmbFiltro;
        private DateTimePicker dtpFiltroDE, dtpFiltroAte;
        private ComboBox cmbFiltroCliente;

        // ── Nova reserva panel ───────────────────────────────────────────────
        private Panel pnlNovaReserva;

        // ── Nova reserva form fields ─────────────────────────────────────────
        private ComboBox cmbNovaCliente, cmbNovaRecursoTipo, cmbNovaRecurso;
        private DateTimePicker dtpNovaData, dtpHoraInicio, dtpHoraFim;
        private TextBox txtParticipantes, txtNotas;
        private Label lblValorCalc;
        private Button btnCalcular, btnConfirmar, btnFecharForm;

        // ── State ────────────────────────────────────────────────────────────
        private decimal _valorCalculado = 0;

        // ────────────────────────────────────────────────────────────────────
        public UcReservas()
        {
            this.BackColor = Theme.ContentBg;
            this.Dock = DockStyle.Fill;

            InitUI();
            LoadClientes();
            LoadRecursos();
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
                Padding = new Padding(20, 8, 0, 0)
            };
            var lblTitle = new Label
            {
                Text = "Reservas",
                Font = Theme.FontTitle,
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                AutoSize = false,
                Bounds = new Rectangle(0, 4, 200, 28)
            };
            var lblSubtitle = new Label
            {
                Text = "Gestão de reservas de salas e postos de trabalho",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                AutoSize = false,
                Bounds = new Rectangle(0, 32, 400, 18)
            };
            pnlTitle.Controls.Add(lblSubtitle);
            pnlTitle.Controls.Add(lblTitle);

            // ── Toolbar (Dock=Top) ───────────────────────────────────────────
            var pnlToolbar = Theme.Toolbar();
            var flpToolbar = Theme.ToolbarFlow();

            btnNova       = Theme.BtnPrim("+ Nova Reserva");
            btnCancelarRes = Theme.BtnGray("Cancelar Reserva");
            btnAtualizar  = Theme.BtnGray("⟳  Atualizar");

            btnNova.Click       += BtnNova_Click;
            btnCancelarRes.Click += BtnCancelarRes_Click;
            btnAtualizar.Click  += (s, e) => LoadData();

            flpToolbar.Controls.AddRange(new Control[]
            {
                btnNova, btnCancelarRes, btnAtualizar
            });
            pnlToolbar.Controls.Add(flpToolbar);

            // ── DataGridView (Dock=Fill) ─────────────────────────────────────
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false
            };
            Theme.StyleGrid(dgv);
            dgv.SelectionChanged += Dgv_SelectionChanged;
            dgv.CellFormatting   += Dgv_CellFormatting;

            // Define columns explicitly
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID",       HeaderText = "ID",        DataPropertyName = "ID",       Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente",  HeaderText = "Cliente",   DataPropertyName = "Cliente" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Recurso",  HeaderText = "Recurso",   DataPropertyName = "Recurso" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo",     HeaderText = "Tipo",      DataPropertyName = "Tipo",     FillWeight = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Data",     HeaderText = "Data",      DataPropertyName = "Data",     FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "HInicio",  HeaderText = "H.Início",  DataPropertyName = "H.Início", FillWeight = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "HFim",     HeaderText = "H.Fim",     DataPropertyName = "H.Fim",    FillWeight = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Valor",    HeaderText = "Valor",     DataPropertyName = "Valor",    FillWeight = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado",   HeaderText = "Estado",    DataPropertyName = "Estado",   FillWeight = 80 });

            // ── Nova Reserva panel (Dock=Bottom) ─────────────────────────────
            pnlNovaReserva = BuildNovaReservaPanel();

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

            var lblDE = new Label { Text = "De:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = Theme.FontLabel, ForeColor = ColorTranslator.FromHtml("#64748b") };
            dtpFiltroDE = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width  = 96,
                Value  = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                Margin = new Padding(0, 3, 8, 0)
            };
            var lblAte = new Label { Text = "Até:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = Theme.FontLabel, ForeColor = ColorTranslator.FromHtml("#64748b") };
            dtpFiltroAte = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width  = 96,
                Value  = DateTime.Today,
                Margin = new Padding(0, 3, 8, 0)
            };
            var lblCli = new Label { Text = "Cliente:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = Theme.FontLabel, ForeColor = ColorTranslator.FromHtml("#64748b") };
            cmbFiltroCliente = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 160,
                Font          = Theme.FontBase,
                Margin        = new Padding(0, 3, 8, 0)
            };
            var lblEstado = new Label { Text = "Estado:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = Theme.FontLabel, ForeColor = ColorTranslator.FromHtml("#64748b") };
            cmbFiltro = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 110,
                Font          = Theme.FontBase,
                Margin        = new Padding(0, 3, 8, 0)
            };
            cmbFiltro.Items.AddRange(new object[] { "(Todos)", "Pendente", "Confirmada", "Cancelada", "Concluida" });
            cmbFiltro.SelectedIndex = 0;

            var btnFiltrar = Theme.BtnPrim("Filtrar");
            var btnLimpar  = Theme.BtnGray("Limpar");
            btnFiltrar.Margin = new Padding(0, 3, 4, 0);
            btnLimpar.Margin  = new Padding(0, 3, 0, 0);
            btnFiltrar.Click += (s, e) => LoadData();
            btnLimpar.Click  += (s, e) =>
            {
                dtpFiltroDE.Value   = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                dtpFiltroAte.Value  = DateTime.Today;
                cmbFiltroCliente.SelectedIndex = 0;
                cmbFiltro.SelectedIndex        = 0;
                LoadData();
            };

            flpFiltros.Controls.AddRange(new Control[]
            {
                lblDE, dtpFiltroDE, lblAte, dtpFiltroAte,
                lblCli, cmbFiltroCliente, lblEstado, cmbFiltro,
                btnFiltrar, btnLimpar
            });
            pnlFiltros.Controls.Add(flpFiltros);

            this.Controls.Add(pnlNovaReserva);  // Dock=Bottom — added first
            this.Controls.Add(dgv);              // Dock=Fill
            this.Controls.Add(pnlFiltros);       // Dock=Top
            this.Controls.Add(pnlToolbar);       // Dock=Top
            this.Controls.Add(pnlTitle);         // Dock=Top — appears at very top
        }

        private Panel BuildNovaReservaPanel()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 220,
                BackColor = Color.White,
                Visible = false
            };
            // Top border paint
            pnl.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(ColorTranslator.FromHtml("#bfdbfe"), 1))
                    e.Graphics.DrawLine(pen, 0, 0, pnl.Width, 0);
            };

            // ── Header strip (Dock=Top, Height=24) ──────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.White
            };
            var lblHeader = new Label
            {
                Text = "NOVA RESERVA",
                ForeColor = ColorTranslator.FromHtml("#1d4ed8"),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 4, 0, 0)
            };
            pnlHeader.Controls.Add(lblHeader);

            // ── Content panel (Dock=Fill) ────────────────────────────────────
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14, 0, 14, 10)
            };

            // Row 1: 5-column TableLayoutPanel (height 80)
            var tblRow1 = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.White
            };
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tblRow1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            tblRow1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            cmbNovaCliente = Theme.Combo();

            cmbNovaRecursoTipo = Theme.Combo();
            cmbNovaRecursoTipo.Items.AddRange(new object[] { "Sala", "Posto" });
            cmbNovaRecursoTipo.SelectedIndex = 0;
            cmbNovaRecursoTipo.SelectedIndexChanged += CmbNovaRecursoTipo_Changed;

            cmbNovaRecurso = Theme.Combo();

            dtpNovaData = Theme.DatePicker();
            dtpNovaData.Value = DateTime.Today;

            dtpHoraInicio = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(9),
                Font = new Font("Segoe UI", 9.5f),
                Dock = DockStyle.Top
            };

            tblRow1.Controls.Add(MakeFormCell(Theme.FieldLabel("Cliente *"),      cmbNovaCliente),    0, 0);
            tblRow1.Controls.Add(MakeFormCell(Theme.FieldLabel("Tipo"),           cmbNovaRecursoTipo), 1, 0);
            tblRow1.Controls.Add(MakeFormCell(Theme.FieldLabel("Sala / Posto *"), cmbNovaRecurso),    2, 0);
            tblRow1.Controls.Add(MakeFormCell(Theme.FieldLabel("Data *"),         dtpNovaData),       3, 0);
            tblRow1.Controls.Add(MakeFormCell(Theme.FieldLabel("H.Início"),       dtpHoraInicio),     4, 0);

            // Row 2: 4-column TableLayoutPanel
            var tblRow2 = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 70,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.White
            };
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            tblRow2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            tblRow2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            dtpHoraFim = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(10),
                Font = new Font("Segoe UI", 9.5f),
                Dock = DockStyle.Top
            };

            txtParticipantes = Theme.Field();
            txtNotas         = Theme.Field();

            // Valor calc + Calcular button cell
            lblValorCalc = new Label
            {
                Text = "Valor: —",
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Top
            };
            btnCalcular  = Theme.BtnGray("Calcular");
            btnCalcular.Click += BtnCalcular_Click;

            var pnlValor = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 0, 6, 0) };
            pnlValor.Controls.Add(btnCalcular);
            pnlValor.Controls.Add(lblValorCalc);
            pnlValor.Controls.Add(Theme.FieldLabel("Valor"));

            tblRow2.Controls.Add(MakeFormCell(Theme.FieldLabel("H.Fim"),          dtpHoraFim),        0, 0);
            tblRow2.Controls.Add(MakeFormCell(Theme.FieldLabel("Participantes"),  txtParticipantes),   1, 0);
            tblRow2.Controls.Add(MakeFormCell(Theme.FieldLabel("Notas"),          txtNotas),           2, 0);
            tblRow2.Controls.Add(pnlValor,                                                             3, 0);

            // Bottom FlowLayoutPanel: Confirmar + Fechar
            var flpBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(0, 4, 0, 0)
            };
            btnConfirmar   = Theme.BtnPrim("Confirmar");
            btnFecharForm  = Theme.BtnGray("Fechar");
            btnConfirmar.Enabled = false;
            btnConfirmar.Click  += BtnConfirmar_Click;
            btnFecharForm.Click += (s, e) => pnlNovaReserva.Visible = false;
            flpBottom.Controls.Add(btnConfirmar);
            flpBottom.Controls.Add(btnFecharForm);

            // Add to content panel (dock order: bottom → top)
            pnlContent.Controls.Add(flpBottom);
            pnlContent.Controls.Add(tblRow2);
            pnlContent.Controls.Add(tblRow1);

            // Add to outer panel (dock order: bottom → top within pnl not needed here)
            pnl.Controls.Add(pnlContent);
            pnl.Controls.Add(pnlHeader);

            return pnl;
        }

        // Helper: field cell with label stacked on top of control
        private Panel MakeFormCell(Label lbl, Control field)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0), BackColor = Color.White };
            // Dock=Top: field added first (goes below), label added last (goes on top)
            p.Controls.Add(field);
            p.Controls.Add(lbl);
            return p;
        }

        // ────────────────────────────────────────────────────────────────────
        // Data loading
        // ────────────────────────────────────────────────────────────────────

        private void LoadClientes()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    "SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    cmbNovaCliente.DataSource    = dt;
                    cmbNovaCliente.DisplayMember = "nome";
                    cmbNovaCliente.ValueMember   = "cliente_id";
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Populate filter client combo (with "Todos os clientes" as first item)
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
                    rowTodos["nome"]       = "(Todos os clientes)";
                    dt.Rows.InsertAt(rowTodos, 0);
                    cmbFiltroCliente.DataSource    = dt;
                    cmbFiltroCliente.DisplayMember = "nome";
                    cmbFiltroCliente.ValueMember   = "cliente_id";
                    cmbFiltroCliente.SelectedIndex = 0;
                }
            }
            catch { /* filter combo is optional */ }
        }

        private void LoadRecursos()
        {
            string sql;

            if (cmbNovaRecursoTipo.Text == "Sala")
            {
                sql = @"
                    SELECT s.sala_id AS id,
                           e.nome + ' — ' + s.nome + ' (cap:' + CAST(s.capacidade AS varchar) + ')' AS descricao,
                           s.preco_hora
                    FROM sala s
                    JOIN espaco e ON s.espaco_id = e.espaco_id
                    WHERE s.estado = 'Disponivel'
                    ORDER BY descricao";
            }
            else
            {
                sql = @"
                    SELECT p.posto_id AS id,
                           e.nome + ' — ' + p.codigo + ' (' + p.tipo + ')' AS descricao,
                           p.preco_hora
                    FROM posto_trabalho p
                    JOIN espaco e ON p.espaco_id = e.espaco_id
                    WHERE p.estado = 'Disponivel'
                    ORDER BY descricao";
            }

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    cmbNovaRecurso.DataSource    = dt;
                    cmbNovaRecurso.DisplayMember = "descricao";
                    cmbNovaRecurso.ValueMember   = "id";
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao carregar recursos: " + ex.Message, "Erro BD",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Toggle participantes visibility based on tipo
            if (txtParticipantes != null)
                txtParticipantes.Visible = (cmbNovaRecursoTipo.Text == "Sala");

            // Reset calculated value when recurso type changes
            _valorCalculado = 0;
            if (lblValorCalc != null)  lblValorCalc.Text = "Valor: —";
            if (btnConfirmar != null)  btnConfirmar.Enabled = false;
        }

        private void LoadData()
        {
            try
            {
                var whereParts = new System.Collections.Generic.List<string>();
                whereParts.Add("r.data_reserva BETWEEN @de AND @ate");

                string estadoFiltro = cmbFiltro?.SelectedIndex > 0 ? cmbFiltro.Text : null;
                if (estadoFiltro != null) whereParts.Add("r.estado = @e");

                bool filtraPorCliente = cmbFiltroCliente?.SelectedValue != null
                    && !(cmbFiltroCliente.SelectedValue is System.DBNull)
                    && cmbFiltroCliente.SelectedIndex > 0;
                if (filtraPorCliente) whereParts.Add("r.cliente_id = @c");

                string where = "WHERE " + string.Join(" AND ", whereParts);

                string sql = $@"
                    SELECT r.reserva_id AS ID,
                           c.nome AS Cliente,
                           ISNULL(s.nome, p.codigo) AS Recurso,
                           CASE WHEN r.sala_id IS NOT NULL THEN 'Sala' ELSE 'Posto' END AS Tipo,
                           CONVERT(varchar,r.data_reserva,103) AS Data,
                           CONVERT(varchar,r.hora_inicio,108) AS [H.Início],
                           CONVERT(varchar,r.hora_fim,108) AS [H.Fim],
                           r.valor AS Valor,
                           r.estado AS Estado
                    FROM reserva r
                    JOIN cliente c ON r.cliente_id = c.cliente_id
                    LEFT JOIN sala s ON r.sala_id = s.sala_id
                    LEFT JOIN posto_trabalho p ON r.posto_id = p.posto_id
                    {where}
                    ORDER BY r.data_reserva DESC, r.hora_inicio";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@de",  dtpFiltroDE?.Value.Date  ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
                    cmd.Parameters.AddWithValue("@ate", dtpFiltroAte?.Value.Date ?? DateTime.Today);
                    if (estadoFiltro != null)    cmd.Parameters.AddWithValue("@e", estadoFiltro);
                    if (filtraPorCliente)        cmd.Parameters.AddWithValue("@c", Convert.ToInt32(cmbFiltroCliente.SelectedValue));

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dgv.DataSource = dt;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Grid events
        // ────────────────────────────────────────────────────────────────────

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            // No form fill needed — reservas are read-only (only cancel allowed)
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns.Count == 0 || e.Value == null) return;
            if (dgv.Columns[e.ColumnIndex].Name == "Valor")
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal val))
                {
                    e.Value = Theme.FormatEuro(val);
                    e.FormattingApplied = true;
                }
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Toolbar button handlers
        // ────────────────────────────────────────────────────────────────────

        private void BtnNova_Click(object sender, EventArgs e)
        {
            // Toggle form visibility
            pnlNovaReserva.Visible = !pnlNovaReserva.Visible;

            if (pnlNovaReserva.Visible)
            {
                // Reset the form state
                _valorCalculado = 0;
                lblValorCalc.Text = "Valor: —";
                btnConfirmar.Enabled = false;
                dtpNovaData.Value = DateTime.Today;
                dtpHoraInicio.Value = DateTime.Today.AddHours(9);
                dtpHoraFim.Value    = DateTime.Today.AddHours(10);
                txtParticipantes.Text = "";
                txtNotas.Text = "";
                if (cmbNovaCliente.Items.Count > 0)  cmbNovaCliente.SelectedIndex  = 0;
                if (cmbNovaRecurso.Items.Count > 0)   cmbNovaRecurso.SelectedIndex   = 0;
                if (cmbNovaRecursoTipo.SelectedIndex < 0) cmbNovaRecursoTipo.SelectedIndex = 0;
            }
        }

        private void BtnCancelarRes_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione uma reserva para cancelar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgv.SelectedRows[0];
            string estadoAtual = row.Cells["Estado"].Value?.ToString() ?? "";

            if (estadoAtual == "Cancelada")
            {
                MessageBox.Show("Esta reserva já está cancelada.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (estadoAtual == "Concluida")
            {
                MessageBox.Show("Não é possível cancelar uma reserva concluída.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var res = MessageBox.Show(
                "Cancelar a reserva selecionada?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res != DialogResult.Yes) return;

            int id = Convert.ToInt32(row.Cells["ID"].Value);

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    "UPDATE reserva SET estado='Cancelada' WHERE reserva_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Nova Reserva form handlers
        // ────────────────────────────────────────────────────────────────────

        private void CmbNovaRecursoTipo_Changed(object sender, EventArgs e)
        {
            LoadRecursos();
        }

        private void BtnCalcular_Click(object sender, EventArgs e)
        {
            if (cmbNovaRecurso.SelectedValue == null)
            {
                MessageBox.Show("Selecione um recurso.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var inicio = dtpHoraInicio.Value.TimeOfDay;
            var fim    = dtpHoraFim.Value.TimeOfDay;

            if (fim <= inicio)
            {
                MessageBox.Show("Hora fim deve ser posterior à hora início.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string sql = cmbNovaRecursoTipo.Text == "Sala"
                    ? "SELECT preco_hora FROM sala WHERE sala_id=@id"
                    : "SELECT preco_hora FROM posto_trabalho WHERE posto_id=@id";

                int recursoId = Convert.ToInt32(cmbNovaRecurso.SelectedValue);

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", recursoId);
                    var result = cmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        MessageBox.Show("Preço/hora não definido para este recurso.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    decimal precoHora = Convert.ToDecimal(result);
                    double horas      = (fim - inicio).TotalHours;
                    _valorCalculado   = (decimal)horas * precoHora;

                    lblValorCalc.Text    = "Valor: " + Theme.FormatEuro(_valorCalculado);
                    btnConfirmar.Enabled = true;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao calcular valor: " + ex.Message, "Erro BD",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            // Basic validation
            if (cmbNovaCliente.SelectedValue == null)
            {
                MessageBox.Show("Selecione um cliente.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbNovaRecurso.SelectedValue == null)
            {
                MessageBox.Show("Selecione um recurso.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var inicio = dtpHoraInicio.Value.TimeOfDay;
            var fim    = dtpHoraFim.Value.TimeOfDay;

            if (fim <= inicio)
            {
                MessageBox.Show("Hora fim deve ser posterior à hora início.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int clienteId  = Convert.ToInt32(cmbNovaCliente.SelectedValue);
            int recursoId  = Convert.ToInt32(cmbNovaRecurso.SelectedValue);
            bool isSala    = cmbNovaRecursoTipo.Text == "Sala";
            string horaIni = inicio.ToString(@"hh\:mm");
            string horaFim = fim.ToString(@"hh\:mm");

            // Notas
            object notasVal = string.IsNullOrWhiteSpace(txtNotas.Text)
                ? (object)DBNull.Value
                : txtNotas.Text.Trim();

            try
            {
                using (var conn = Database.GetConnection())
                {
                    SqlCommand cmd;

                    if (isSala)
                    {
                        // Parse participantes
                        object npVal;
                        if (int.TryParse(txtParticipantes.Text.Trim(), out int np) && np > 0)
                            npVal = np;
                        else
                            npVal = DBNull.Value;

                        cmd = new SqlCommand(
                            "INSERT INTO reserva " +
                            "(cliente_id, sala_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas) " +
                            "VALUES (@c, @r, @d, @hi, @hf, 'Pendente', @v, @np, @n)",
                            conn);
                        cmd.Parameters.AddWithValue("@c",  clienteId);
                        cmd.Parameters.AddWithValue("@r",  recursoId);
                        cmd.Parameters.AddWithValue("@d",  dtpNovaData.Value.Date);
                        cmd.Parameters.AddWithValue("@hi", horaIni);
                        cmd.Parameters.AddWithValue("@hf", horaFim);
                        cmd.Parameters.AddWithValue("@v",  _valorCalculado);
                        cmd.Parameters.AddWithValue("@np", npVal);
                        cmd.Parameters.AddWithValue("@n",  notasVal);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            "INSERT INTO reserva " +
                            "(cliente_id, posto_id, data_reserva, hora_inicio, hora_fim, estado, valor, notas) " +
                            "VALUES (@c, @r, @d, @hi, @hf, 'Pendente', @v, @n)",
                            conn);
                        cmd.Parameters.AddWithValue("@c",  clienteId);
                        cmd.Parameters.AddWithValue("@r",  recursoId);
                        cmd.Parameters.AddWithValue("@d",  dtpNovaData.Value.Date);
                        cmd.Parameters.AddWithValue("@hi", horaIni);
                        cmd.Parameters.AddWithValue("@hf", horaFim);
                        cmd.Parameters.AddWithValue("@v",  _valorCalculado);
                        cmd.Parameters.AddWithValue("@n",  notasVal);
                    }

                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }

                MessageBox.Show("Reserva criada com sucesso!", "Sucesso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reset form
                _valorCalculado = 0;
                lblValorCalc.Text    = "Valor: —";
                btnConfirmar.Enabled = false;
                txtParticipantes.Text = "";
                txtNotas.Text = "";
                dtpNovaData.Value   = DateTime.Today;
                dtpHoraInicio.Value = DateTime.Today.AddHours(9);
                dtpHoraFim.Value    = DateTime.Today.AddHours(10);

                pnlNovaReserva.Visible = false;
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao criar reserva: " + ex.Message, "Erro BD",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
