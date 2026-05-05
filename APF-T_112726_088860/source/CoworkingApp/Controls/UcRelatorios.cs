using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CoworkingApp.Controls
{
    public class UcRelatorios : UserControl
    {
        // Tab 1
        private RadioButton rbSala;
        private RadioButton rbPosto;
        private DateTimePicker dtpDisp;
        private DateTimePicker dtpDispHI;
        private DateTimePicker dtpDispHF;
        private Button btnPesquisarDisp;
        private DataGridView dgvDisponibilidade;

        // Tab 2
        private ComboBox cmbClienteHist;
        private Button btnPesquisarHist;
        private DataGridView dgvHistCliente;

        // Tab 3
        private ComboBox cmbClientePag;
        private Button btnPesquisarPag;
        private DataGridView dgvHistPag;

        // Tab 4
        private DateTimePicker dtpOcupIni;
        private DateTimePicker dtpOcupFim;
        private Button btnPesquisarOcup;
        private DataGridView dgvOcupacao;
        private DateTimePicker dtpRecIni;
        private DateTimePicker dtpRecFim;
        private Button btnPesquisarRec;
        private Label lblTotalReceita;
        private DataGridView dgvReceita;
        private Chart _chartReceita;
        private Chart _chartMetodosPag;

        public UcRelatorios()
        {
            InitUI();
            LoadClientesCombos();
        }

        private static void StyleChart(System.Windows.Forms.DataVisualization.Charting.Chart c)
        {
            c.BackColor = Theme.CardBg;
            foreach (var area in c.ChartAreas)
            {
                area.BackColor = System.Drawing.Color.Transparent;
                area.AxisX.LineColor = Theme.CardBorder;
                area.AxisY.LineColor = Theme.CardBorder;
                area.AxisX.LabelStyle.ForeColor = Theme.TextMuted;
                area.AxisY.LabelStyle.ForeColor = Theme.TextMuted;
                area.AxisX.MajorGrid.LineColor = System.Drawing.Color.Transparent;
                area.AxisY.MajorGrid.LineColor = Theme.CardBorder;
            }
            foreach (var legend in c.Legends)
            {
                legend.BackColor = System.Drawing.Color.Transparent;
                legend.ForeColor = Theme.TextSecondary;
            }
            foreach (var title in c.Titles)
            {
                title.ForeColor = Theme.TextPrimary;
            }
        }

        private static readonly System.Drawing.Color[] ChartPalette = new[]
        {
            ColorTranslator.FromHtml("#6366f1"),
            ColorTranslator.FromHtml("#8b5cf6"),
            ColorTranslator.FromHtml("#10b981"),
            ColorTranslator.FromHtml("#f59e0b"),
            ColorTranslator.FromHtml("#ef4444"),
            ColorTranslator.FromHtml("#3b82f6")
        };

        private void InitUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.PageBg;

            // --- pnlTitle (Dock=Top) ---
            var pnlTitle = new Panel();
            pnlTitle.Dock = DockStyle.Top;
            pnlTitle.Height = 56;
            pnlTitle.BackColor = Theme.PageBg;

            var lblTitle = new Label();
            lblTitle.Text = "Relatórios";
            lblTitle.Font = Theme.FontTitle;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(18, 14);
            pnlTitle.Controls.Add(lblTitle);

            // --- tabControl (Dock=Fill) ---
            var tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = Theme.FontBase;

            tabControl.TabPages.Add(BuildTabDisponibilidade());
            tabControl.TabPages.Add(BuildTabHistorialCliente());
            tabControl.TabPages.Add(BuildTabHistorialPagamentos());
            tabControl.TabPages.Add(BuildTabOcupacaoReceita());

            // Add order: Fill first, then Top panels (title last = visually topmost)
            this.Controls.Add(tabControl);   // Dock=Fill
            this.Controls.Add(pnlTitle);     // Dock=Top — added last = visually topmost
        }

        // ─── Tab 1: Disponibilidade ─────────────────────────────────────────────

        private TabPage BuildTabDisponibilidade()
        {
            var tab = new TabPage("Disponibilidade");
            tab.BackColor = Color.White;

            // FlowLayoutPanel (Dock=Top)
            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 56;
            flw.BackColor = Color.White;
            flw.Padding = new Padding(10, 8, 0, 8);
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;

            rbSala = new RadioButton();
            rbSala.Text = "Sala";
            rbSala.Checked = true;
            rbSala.AutoSize = true;
            rbSala.Margin = new Padding(0, 4, 8, 0);

            rbPosto = new RadioButton();
            rbPosto.Text = "Posto";
            rbPosto.AutoSize = true;
            rbPosto.Margin = new Padding(0, 4, 12, 0);

            var lblData = new Label();
            lblData.Text = "Data:";
            lblData.AutoSize = true;
            lblData.Margin = new Padding(0, 7, 4, 0);

            dtpDisp = new DateTimePicker();
            dtpDisp.Format = DateTimePickerFormat.Short;
            dtpDisp.Width = 100;
            dtpDisp.Margin = new Padding(0, 3, 12, 0);

            var lblHI = new Label();
            lblHI.Text = "H.Início:";
            lblHI.AutoSize = true;
            lblHI.Margin = new Padding(0, 7, 4, 0);

            dtpDispHI = new DateTimePicker();
            dtpDispHI.Format = DateTimePickerFormat.Time;
            dtpDispHI.ShowUpDown = true;
            dtpDispHI.Width = 80;
            dtpDispHI.Value = DateTime.Today.AddHours(9);
            dtpDispHI.Margin = new Padding(0, 3, 12, 0);

            var lblHF = new Label();
            lblHF.Text = "H.Fim:";
            lblHF.AutoSize = true;
            lblHF.Margin = new Padding(0, 7, 4, 0);

            dtpDispHF = new DateTimePicker();
            dtpDispHF.Format = DateTimePickerFormat.Time;
            dtpDispHF.ShowUpDown = true;
            dtpDispHF.Width = 80;
            dtpDispHF.Value = DateTime.Today.AddHours(10);
            dtpDispHF.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarDisp = Theme.BtnPrim("Pesquisar");
            btnPesquisarDisp.Margin = new Padding(0, 3, 0, 0);
            btnPesquisarDisp.Click += BtnPesquisarDisp_Click;

            flw.Controls.Add(rbSala);
            flw.Controls.Add(rbPosto);
            flw.Controls.Add(lblData);
            flw.Controls.Add(dtpDisp);
            flw.Controls.Add(lblHI);
            flw.Controls.Add(dtpDispHI);
            flw.Controls.Add(lblHF);
            flw.Controls.Add(dtpDispHF);
            flw.Controls.Add(btnPesquisarDisp);

            dgvDisponibilidade = new DataGridView();
            dgvDisponibilidade.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvDisponibilidade);
            dgvDisponibilidade.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && (dgvDisponibilidade.Columns[e.ColumnIndex].HeaderText == "€/Hora" ||
                    dgvDisponibilidade.Columns[e.ColumnIndex].HeaderText == "€/Dia") &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            tab.Controls.Add(dgvDisponibilidade);  // Fill
            tab.Controls.Add(flw);                 // Top — added last = visually topmost
            return tab;
        }

        private void BtnPesquisarDisp_Click(object sender, EventArgs e)
        {
            if (dtpDispHI.Value.TimeOfDay >= dtpDispHF.Value.TimeOfDay)
            {
                MessageBox.Show("Hora fim deve ser posterior à hora início.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string hi = dtpDispHI.Value.TimeOfDay.ToString(@"hh\:mm");
            string hf = dtpDispHF.Value.TimeOfDay.ToString(@"hh\:mm");
            DateTime data = dtpDisp.Value.Date;

            string sql;
            if (rbSala.Checked)
            {
                sql = @"
SELECT s.recurso_id AS ID, e.nome AS Espaço, s.nome AS Sala,
  s.capacidade AS Capacidade, s.preco_hora AS [€/Hora]
FROM sala s JOIN espaco e ON s.espaco_id=e.espaco_id
WHERE s.estado='Disponivel'
AND NOT EXISTS (
  SELECT 1 FROM reserva r WHERE r.recurso_id=s.recurso_id
  AND r.data_reserva=@d AND r.estado<>'Cancelada'
  AND r.hora_inicio < @hf AND r.hora_fim > @hi)
ORDER BY e.nome, s.nome";
            }
            else
            {
                sql = @"
SELECT p.recurso_id AS ID, e.nome AS Espaço, p.codigo AS Código,
  p.tipo_posto AS Tipo, p.preco_dia AS [€/Dia]
FROM posto p JOIN espaco e ON p.espaco_id=e.espaco_id
WHERE p.estado='Disponivel'
AND NOT EXISTS (
  SELECT 1 FROM reserva r WHERE r.recurso_id=p.recurso_id
  AND r.data_reserva=@d AND r.estado<>'Cancelada')
ORDER BY e.nome, p.codigo";
            }

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@d", data);
                    cmd.Parameters.AddWithValue("@hi", hi);
                    cmd.Parameters.AddWithValue("@hf", hf);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        dgvDisponibilidade.DataSource = dt;
                    }
                }

                if (dgvDisponibilidade.Columns.Contains("ID"))
                    dgvDisponibilidade.Columns["ID"].Visible = false;
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Tab 2: Historial por Cliente ───────────────────────────────────────

        private TabPage BuildTabHistorialCliente()
        {
            var tab = new TabPage("Historial por Cliente");
            tab.BackColor = Color.White;

            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 50;
            flw.BackColor = Color.White;
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;
            flw.Padding = new Padding(10, 8, 0, 8);

            var lblCliente = new Label();
            lblCliente.Text = "Cliente:";
            lblCliente.AutoSize = true;
            lblCliente.Margin = new Padding(0, 7, 4, 0);

            cmbClienteHist = new ComboBox();
            cmbClienteHist.Width = 220;
            cmbClienteHist.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbClienteHist.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarHist = Theme.BtnPrim("Pesquisar");
            btnPesquisarHist.Margin = new Padding(0, 3, 0, 0);
            btnPesquisarHist.Click += BtnPesquisarHist_Click;

            flw.Controls.Add(lblCliente);
            flw.Controls.Add(cmbClienteHist);
            flw.Controls.Add(btnPesquisarHist);

            dgvHistCliente = new DataGridView();
            dgvHistCliente.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvHistCliente);
            dgvHistCliente.CellFormatting += (s, e) =>
            {
                Theme.ApplyStatusColor(e, "Estado", dgvHistCliente);
                if (e.ColumnIndex >= 0 && dgvHistCliente.Columns[e.ColumnIndex].HeaderText == "Valor" &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            tab.Controls.Add(dgvHistCliente);  // Fill
            tab.Controls.Add(flw);             // Top — added last = visually topmost
            return tab;
        }

        private void BtnPesquisarHist_Click(object sender, EventArgs e)
        {
            if (cmbClienteHist.SelectedValue == null) return;
            int clienteId = Convert.ToInt32(cmbClienteHist.SelectedValue);

            string sql = @"
SELECT ISNULL(s.nome, p.codigo) AS Recurso,
  rc.tipo AS Tipo,
  CONVERT(varchar,r.data_reserva,103) AS Data,
  CONVERT(varchar,r.hora_inicio,108)+'-'+CONVERT(varchar,r.hora_fim,108) AS Horário,
  r.valor AS Valor, r.estado AS Estado
FROM reserva r
JOIN recurso rc ON r.recurso_id = rc.recurso_id
LEFT JOIN sala s ON rc.recurso_id = s.recurso_id
LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
WHERE r.cliente_id=@c ORDER BY r.data_reserva DESC";

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@c", clienteId);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        dgvHistCliente.DataSource = dt;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Tab 3: Historial de Pagamentos ─────────────────────────────────────

        private TabPage BuildTabHistorialPagamentos()
        {
            var tab = new TabPage("Historial de Pagamentos");
            tab.BackColor = Color.White;

            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 50;
            flw.BackColor = Color.White;
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;
            flw.Padding = new Padding(10, 8, 0, 8);

            var lblCliente = new Label();
            lblCliente.Text = "Cliente:";
            lblCliente.AutoSize = true;
            lblCliente.Margin = new Padding(0, 7, 4, 0);

            cmbClientePag = new ComboBox();
            cmbClientePag.Width = 220;
            cmbClientePag.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbClientePag.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarPag = Theme.BtnPrim("Pesquisar");
            btnPesquisarPag.Margin = new Padding(0, 3, 0, 0);
            btnPesquisarPag.Click += BtnPesquisarPag_Click;

            flw.Controls.Add(lblCliente);
            flw.Controls.Add(cmbClientePag);
            flw.Controls.Add(btnPesquisarPag);

            dgvHistPag = new DataGridView();
            dgvHistPag.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvHistPag);
            dgvHistPag.CellFormatting += (s, e) =>
            {
                Theme.ApplyStatusColor(e, "Estado", dgvHistPag);
                if (e.ColumnIndex >= 0 && dgvHistPag.Columns[e.ColumnIndex].HeaderText == "Valor" &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            // ── Pie chart: métodos de pagamento por cliente (Dock=Bottom, Height=160)
            _chartMetodosPag = new Chart { Dock = DockStyle.Bottom, Height = 160, Visible = false, BackColor = Theme.CardBg };
            var areaMet = new ChartArea("main") { BackColor = Color.Transparent };
            _chartMetodosPag.ChartAreas.Add(areaMet);
            var serMet = new Series("met") { ChartType = SeriesChartType.Pie };
            _chartMetodosPag.Series.Add(serMet);
            var legendMet = new Legend("leg") { Docking = Docking.Right, Font = new Font("Segoe UI", 8f), BackColor = Color.Transparent };
            _chartMetodosPag.Legends.Add(legendMet);
            serMet.Legend = "leg";
            StyleChart(_chartMetodosPag);

            tab.Controls.Add(dgvHistPag);         // Fill
            tab.Controls.Add(_chartMetodosPag);   // Bottom
            tab.Controls.Add(flw);                // Top — added last = visually topmost
            return tab;
        }

        private void BtnPesquisarPag_Click(object sender, EventArgs e)
        {
            if (cmbClientePag.SelectedValue == null) return;
            int clienteId = Convert.ToInt32(cmbClientePag.SelectedValue);

            string sqlHist = @"
SELECT CONVERT(varchar,p.data_pagamento,103) AS Data,
  p.valor AS Valor, p.metodo_pagamento AS Método, p.estado AS Estado,
  CASE WHEN p.reserva_id IS NOT NULL THEN 'Reserva #'+CAST(p.reserva_id AS varchar)
       ELSE 'Adesão #'+CAST(p.adesao_id AS varchar) END AS Referência
FROM pagamento p WHERE p.cliente_id=@c ORDER BY p.data_pagamento DESC";

            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var cmd = new SqlCommand(sqlHist, conn))
                    {
                        cmd.Parameters.AddWithValue("@c", clienteId);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);
                            dgvHistPag.DataSource = dt;
                        }
                    }

                    var serMet = _chartMetodosPag.Series["met"];
                    serMet.Points.Clear();
                    string sqlPie = "SELECT metodo_pagamento, COUNT(*) FROM pagamento WHERE cliente_id=@c GROUP BY metodo_pagamento";
                    using (var cmdPie = new SqlCommand(sqlPie, conn))
                    {
                        cmdPie.Parameters.AddWithValue("@c", clienteId);
                        using (var rdr = cmdPie.ExecuteReader())
                        {
                            while (rdr.Read())
                                serMet.Points.AddXY(rdr.GetString(0), rdr.GetInt32(1));
                        }
                    }
                    for (int i = 0; i < serMet.Points.Count; i++)
                        serMet.Points[i].Color = ChartPalette[i % ChartPalette.Length];
                    _chartMetodosPag.Visible = serMet.Points.Count > 0;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Tab 4: Ocupação e Receita ──────────────────────────────────────────

        private TabPage BuildTabOcupacaoReceita()
        {
            var tab = new TabPage("Ocupação e Receita");
            tab.BackColor = Color.White;

            // Sub-section B (Dock=Fill) — added first so A (Dock=Top) sits above it
            var pnlReceita = new Panel();
            pnlReceita.Dock = DockStyle.Fill;
            pnlReceita.BackColor = Color.White;
            BuildReceitaPanel(pnlReceita);

            // Sub-section A (Dock=Top, Height=200)
            var pnlOcupacao = new Panel();
            pnlOcupacao.Dock = DockStyle.Top;
            pnlOcupacao.Height = 200;
            pnlOcupacao.BackColor = Color.White;
            BuildOcupacaoPanel(pnlOcupacao);

            tab.Controls.Add(pnlReceita);   // Fill — added first
            tab.Controls.Add(pnlOcupacao);  // Top — added last = visually topmost
            return tab;
        }

        private void BuildOcupacaoPanel(Panel pnl)
        {
            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 50;
            flw.BackColor = Color.White;
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;
            flw.Padding = new Padding(10, 8, 0, 8);

            var lblDe = new Label();
            lblDe.Text = "De:";
            lblDe.AutoSize = true;
            lblDe.Margin = new Padding(0, 7, 4, 0);

            dtpOcupIni = new DateTimePicker();
            dtpOcupIni.Format = DateTimePickerFormat.Short;
            dtpOcupIni.Width = 100;
            dtpOcupIni.Value = DateTime.Today.AddDays(-30);
            dtpOcupIni.Margin = new Padding(0, 3, 12, 0);

            var lblAte = new Label();
            lblAte.Text = "Até:";
            lblAte.AutoSize = true;
            lblAte.Margin = new Padding(0, 7, 4, 0);

            dtpOcupFim = new DateTimePicker();
            dtpOcupFim.Format = DateTimePickerFormat.Short;
            dtpOcupFim.Width = 100;
            dtpOcupFim.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarOcup = Theme.BtnPrim("Ver Ocupação");
            btnPesquisarOcup.Margin = new Padding(0, 3, 0, 0);
            btnPesquisarOcup.Click += BtnPesquisarOcup_Click;

            flw.Controls.Add(lblDe);
            flw.Controls.Add(dtpOcupIni);
            flw.Controls.Add(lblAte);
            flw.Controls.Add(dtpOcupFim);
            flw.Controls.Add(btnPesquisarOcup);

            var lblOcupHeader = new Label();
            lblOcupHeader.Text = "Ocupação por Espaço";
            lblOcupHeader.Dock = DockStyle.Top;
            lblOcupHeader.Height = 24;
            lblOcupHeader.Padding = new Padding(10, 4, 0, 0);
            lblOcupHeader.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            dgvOcupacao = new DataGridView();
            dgvOcupacao.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvOcupacao);

            pnl.Controls.Add(dgvOcupacao);      // Fill
            pnl.Controls.Add(lblOcupHeader);     // Top
            pnl.Controls.Add(flw);              // Top — added last = visually topmost
        }

        private void BuildReceitaPanel(Panel pnl)
        {
            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 50;
            flw.BackColor = Color.White;
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;
            flw.Padding = new Padding(10, 8, 0, 8);

            var lblDe = new Label();
            lblDe.Text = "De:";
            lblDe.AutoSize = true;
            lblDe.Margin = new Padding(0, 7, 4, 0);

            dtpRecIni = new DateTimePicker();
            dtpRecIni.Format = DateTimePickerFormat.Short;
            dtpRecIni.Width = 100;
            dtpRecIni.Value = DateTime.Today.AddDays(-30);
            dtpRecIni.Margin = new Padding(0, 3, 12, 0);

            var lblAte = new Label();
            lblAte.Text = "Até:";
            lblAte.AutoSize = true;
            lblAte.Margin = new Padding(0, 7, 4, 0);

            dtpRecFim = new DateTimePicker();
            dtpRecFim.Format = DateTimePickerFormat.Short;
            dtpRecFim.Width = 100;
            dtpRecFim.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarRec = Theme.BtnPrim("Ver Receita");
            btnPesquisarRec.Margin = new Padding(0, 3, 12, 0);
            btnPesquisarRec.Click += BtnPesquisarRec_Click;

            lblTotalReceita = new Label();
            lblTotalReceita.AutoSize = true;
            lblTotalReceita.ForeColor = Theme.TextPrimary;
            lblTotalReceita.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblTotalReceita.Margin = new Padding(8, 7, 0, 0);

            flw.Controls.Add(lblDe);
            flw.Controls.Add(dtpRecIni);
            flw.Controls.Add(lblAte);
            flw.Controls.Add(dtpRecFim);
            flw.Controls.Add(btnPesquisarRec);
            flw.Controls.Add(lblTotalReceita);

            var lblRecHeader = new Label();
            lblRecHeader.Text = "Receita por Método de Pagamento";
            lblRecHeader.Dock = DockStyle.Top;
            lblRecHeader.Height = 24;
            lblRecHeader.Padding = new Padding(10, 4, 0, 0);
            lblRecHeader.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            dgvReceita = new DataGridView();
            dgvReceita.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvReceita);
            dgvReceita.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && dgvReceita.Columns[e.ColumnIndex].HeaderText == "Total" &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            // ── Receita mensal chart (Dock=Top, Height=160) ──────────────
            _chartReceita = new Chart { Dock = DockStyle.Top, Height = 160, BackColor = Theme.CardBg };
            var areaRec = new ChartArea("main");
            areaRec.AxisX.MajorGrid.Enabled   = false;
            areaRec.AxisY.LabelStyle.Format   = "€ #,##0";
            areaRec.AxisY.MajorGrid.LineColor = Theme.CardBorder;
            areaRec.BackColor = Color.Transparent;
            _chartReceita.ChartAreas.Add(areaRec);
            var serRec = new Series("rec") { ChartType = SeriesChartType.Column, Color = Theme.Accent, BorderWidth = 0 };
            _chartReceita.Series.Add(serRec);
            _chartReceita.Titles.Add(new Title("Receita Mensal") { Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Theme.TextPrimary, Docking = Docking.Top });
            StyleChart(_chartReceita);

            pnl.Controls.Add(dgvReceita);      // Fill
            pnl.Controls.Add(lblRecHeader);    // Top
            pnl.Controls.Add(_chartReceita);   // Top
            pnl.Controls.Add(flw);             // Top — added last = visually topmost
        }

        private void BtnPesquisarOcup_Click(object sender, EventArgs e)
        {
            if (dtpOcupIni.Value.Date > dtpOcupFim.Value.Date)
            {
                MessageBox.Show("A data inicial não pode ser posterior à data final.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string sql = @"
SELECT e.nome AS Espaço,
  (SELECT COUNT(*) FROM sala         WHERE espaco_id = e.espaco_id) AS Salas,
  (SELECT COUNT(*) FROM posto WHERE espaco_id = e.espaco_id) AS Postos,
  (SELECT COUNT(*) FROM reserva r
     JOIN sala s ON r.recurso_id = s.recurso_id
     WHERE s.espaco_id = e.espaco_id
       AND r.estado <> 'Cancelada'
       AND r.data_reserva BETWEEN @ini AND @fim) AS [Reservas Sala],
  (SELECT COUNT(*) FROM reserva r
     JOIN posto p ON r.recurso_id = p.recurso_id
     WHERE p.espaco_id = e.espaco_id
       AND r.estado <> 'Cancelada'
       AND r.data_reserva BETWEEN @ini AND @fim) AS [Reservas Posto]
FROM espaco e
ORDER BY e.nome";

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ini", dtpOcupIni.Value.Date);
                    cmd.Parameters.AddWithValue("@fim", dtpOcupFim.Value.Date);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        dgvOcupacao.DataSource = dt;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPesquisarRec_Click(object sender, EventArgs e)
        {
            if (dtpRecIni.Value.Date > dtpRecFim.Value.Date)
            {
                MessageBox.Show("A data inicial não pode ser posterior à data final.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string sqlReceita = @"
SELECT metodo_pagamento AS Método, COUNT(*) AS [Nº Pagamentos], SUM(valor) AS Total
FROM pagamento
WHERE estado='Pago' AND data_pagamento BETWEEN @ini AND @fim
GROUP BY metodo_pagamento ORDER BY Total DESC";

            string sqlTotal = @"
SELECT ISNULL(SUM(valor),0) FROM pagamento WHERE estado='Pago' AND data_pagamento BETWEEN @ini AND @fim";

            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var cmd = new SqlCommand(sqlReceita, conn))
                    {
                        cmd.Parameters.AddWithValue("@ini", dtpRecIni.Value.Date);
                        cmd.Parameters.AddWithValue("@fim", dtpRecFim.Value.Date);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);
                            dgvReceita.DataSource = dt;
                        }
                    }

                    using (var cmdTotal = new SqlCommand(sqlTotal, conn))
                    {
                        cmdTotal.Parameters.AddWithValue("@ini", dtpRecIni.Value.Date);
                        cmdTotal.Parameters.AddWithValue("@fim", dtpRecFim.Value.Date);
                        decimal total = (decimal)cmdTotal.ExecuteScalar();
                        lblTotalReceita.Text = "Total: " + Theme.FormatEuro(total);
                    }

                    // Update bar chart
                    _chartReceita.Series["rec"].Points.Clear();
                    string sqlMensal = @"
SELECT CAST(YEAR(data_pagamento) AS varchar) + '/' +
       RIGHT('0' + CAST(MONTH(data_pagamento) AS varchar), 2) AS Mes,
       SUM(valor) AS Total
FROM pagamento
WHERE estado='Pago' AND data_pagamento BETWEEN @ini AND @fim
GROUP BY YEAR(data_pagamento), MONTH(data_pagamento)
ORDER BY YEAR(data_pagamento), MONTH(data_pagamento)";
                    using (var cmdMensal = new SqlCommand(sqlMensal, conn))
                    {
                        cmdMensal.Parameters.AddWithValue("@ini", dtpRecIni.Value.Date);
                        cmdMensal.Parameters.AddWithValue("@fim", dtpRecFim.Value.Date);
                        using (var rdr = cmdMensal.ExecuteReader())
                        {
                            while (rdr.Read())
                                _chartReceita.Series["rec"].Points.AddXY(
                                    rdr.GetString(0), Convert.ToDouble(rdr.GetDecimal(1)));
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Shared helpers ─────────────────────────────────────────────────────

        private void LoadClientesCombos()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cmbClienteHist.DisplayMember = "nome";
                    cmbClienteHist.ValueMember = "cliente_id";
                    cmbClienteHist.DataSource = dt.Copy();

                    cmbClientePag.DisplayMember = "nome";
                    cmbClientePag.ValueMember = "cliente_id";
                    cmbClientePag.DataSource = dt.Copy();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
