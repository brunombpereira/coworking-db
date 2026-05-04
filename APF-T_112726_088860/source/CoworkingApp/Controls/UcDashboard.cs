using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CoworkingApp.Controls
{
    public class UcDashboard : UserControl
    {
        private Label lblClientes, lblClientesSub;
        private Label lblReservas, lblReservasSub;
        private Label lblAdesoes, lblAdoesSub;
        private Label lblReceita, lblReceitaSub;
        private DataGridView dgvRecent;
        private Chart _chartReservas;
        private Chart _chartMetodos;

        public UcDashboard()
        {
            this.BackColor = Theme.ContentBg;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);

            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            // ── Header panel (Dock=Top) ──────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.ContentBg,
                Padding = new Padding(20, 16, 20, 0)
            };

            var btnAtualizar = Theme.BtnGray("⟳  Atualizar");
            btnAtualizar.Dock = DockStyle.Right;
            btnAtualizar.AutoSize = true;
            btnAtualizar.Click += (s, e) => LoadData();

            var lblSub = new Label
            {
                Text = "Resumo do sistema de coworking",
                Font = new Font("Segoe UI", 9f),
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 22
            };

            var lblTitle = new Label
            {
                Text = "Dashboard",
                Font = Theme.FontTitle,
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 36
            };

            pnlHeader.Controls.Add(btnAtualizar);
            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // ── Stat cards panel (Dock=Top) ──────────────────────────────────
            var pnlCards = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Theme.ContentBg,
                Padding = new Padding(20, 12, 20, 0)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Card 1 — Clientes (#1d4ed8 blue)
            lblClientes = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 34
            };
            lblClientesSub = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 18
            };
            tbl.Controls.Add(
                MakeCard("CLIENTES", ColorTranslator.FromHtml("#1d4ed8"),
                         lblClientes, lblClientesSub),
                0, 0);

            // Card 2 — Reservas Ativas (#0ea5e9 sky)
            lblReservas = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 34
            };
            lblReservasSub = new Label
            {
                Text = "salas + postos",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 18
            };
            tbl.Controls.Add(
                MakeCard("RESERVAS ATIVAS", ColorTranslator.FromHtml("#0ea5e9"),
                         lblReservas, lblReservasSub),
                1, 0);

            // Card 3 — Adesões Ativas (#8b5cf6 violet)
            lblAdesoes = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 34
            };
            lblAdoesSub = new Label
            {
                Text = "planos ativos",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 18
            };
            tbl.Controls.Add(
                MakeCard("ADESÕES ATIVAS", ColorTranslator.FromHtml("#8b5cf6"),
                         lblAdesoes, lblAdoesSub),
                2, 0);

            // Card 4 — Receita Mês (#22c55e green)
            lblReceita = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 34
            };
            lblReceitaSub = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8f),
                ForeColor = ColorTranslator.FromHtml("#22c55e"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 18
            };
            tbl.Controls.Add(
                MakeCard("RECEITA MÊS", ColorTranslator.FromHtml("#22c55e"),
                         lblReceita, lblReceitaSub),
                3, 0);

            pnlCards.Controls.Add(tbl);

            // ── "Pagamentos Recentes" header (Dock=Top) ──────────────────────
            var pnlRecentHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Theme.ContentBg,
                Padding = new Padding(20, 8, 0, 0)
            };
            var lblRecentTitle = new Label
            {
                Text = "Pagamentos Recentes",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            pnlRecentHeader.Controls.Add(lblRecentTitle);

            // ── Grid panel (Dock=Fill) ────────────────────────────────────────
            var pnlGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.ContentBg,
                Padding = new Padding(20, 4, 20, 16)
            };

            dgvRecent = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvRecent);
            dgvRecent.CellFormatting += DgvRecent_CellFormatting;
            pnlGrid.Controls.Add(dgvRecent);

            // ── Charts panel (Dock=Top, Height=180) ─────────────────────────
            var pnlCharts = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 180,
                BackColor = Theme.ContentBg,
                Padding   = new Padding(20, 8, 20, 0)
            };
            var tblCharts = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = Color.Transparent
            };
            tblCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            tblCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tblCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _chartReservas = BuildBarChart("Reservas por Mês", ColorTranslator.FromHtml("#1d4ed8"));
            _chartMetodos  = BuildPieChart("Pagamentos por Método");
            tblCharts.Controls.Add(_chartReservas, 0, 0);
            tblCharts.Controls.Add(_chartMetodos,  1, 0);
            pnlCharts.Controls.Add(tblCharts);

            // ── Add controls in reverse dock order ───────────────────────────
            // Fill must be added before Top panels so it occupies remaining space.
            this.Controls.Add(pnlGrid);
            this.Controls.Add(pnlRecentHeader);
            this.Controls.Add(pnlCharts);
            this.Controls.Add(pnlCards);
            this.Controls.Add(pnlHeader);
        }

        private Panel MakeCard(string title, Color borderColor, Label lblValue, Label lblSub)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 14, 12, 8)
            };

            var localBorder = borderColor; // capture for lambda
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(localBorder, 3))
                    e.Graphics.DrawLine(pen, 0, 0, card.Width, 0);
            };

            var lblCardTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 18
            };

            // Add in reverse (Dock=Top: last added appears at bottom)
            card.Controls.Add(lblSub);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblCardTitle);

            return card;
        }

        private Chart BuildBarChart(string title, Color color)
        {
            var chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            var area  = new ChartArea("main");
            area.AxisX.MajorGrid.Enabled   = false;
            area.AxisY.MajorGrid.LineColor = ColorTranslator.FromHtml("#e2e8f0");
            area.AxisY.LabelStyle.Format   = "#,##0";
            area.BackColor = Color.White;
            chart.ChartAreas.Add(area);

            var series = new Series("data")
            {
                ChartType   = SeriesChartType.Column,
                Color       = color,
                BorderWidth = 0
            };
            chart.Series.Add(series);

            chart.Titles.Add(new Title(title)
            {
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Docking   = Docking.Top
            });
            return chart;
        }

        private Chart BuildPieChart(string title)
        {
            var chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            chart.ChartAreas.Add(new ChartArea("main") { BackColor = Color.White });

            var series = new Series("data") { ChartType = SeriesChartType.Pie };
            chart.Series.Add(series);

            var legend = new Legend("leg")
            {
                Docking   = Docking.Right,
                Font      = new Font("Segoe UI", 8f),
                BackColor = Color.Transparent
            };
            chart.Legends.Add(legend);
            series.Legend = "leg";

            chart.Titles.Add(new Title(title)
            {
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Docking   = Docking.Top
            });
            chart.Palette = ChartColorPalette.Bright;
            return chart;
        }

        private void DgvRecent_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvRecent.Columns.Count == 0) return;
            Theme.ApplyStatusColor(e, "Estado", dgvRecent);
            if (dgvRecent.Columns[e.ColumnIndex].Name == "Valor" && e.Value != null)
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal val))
                {
                    e.Value = Theme.FormatEuro(val);
                    e.FormattingApplied = true;
                }
            }
        }

        public void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    // ── Clients count ────────────────────────────────────────
                    int clientes = 0;
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM cliente", conn))
                        clientes = (int)cmd.ExecuteScalar();
                    lblClientes.Text = clientes.ToString();
                    lblClientesSub.Text = clientes + " registado" + (clientes != 1 ? "s" : "");

                    // ── Active reservations ──────────────────────────────────
                    int reservas = 0;
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM reserva WHERE estado IN ('Pendente','Confirmada')", conn))
                        reservas = (int)cmd.ExecuteScalar();
                    lblReservas.Text = reservas.ToString();
                    // lblReservasSub is static ("salas + postos"), already set

                    // ── Active memberships ───────────────────────────────────
                    int adesoes = 0;
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM adesao WHERE estado='Ativa'", conn))
                        adesoes = (int)cmd.ExecuteScalar();
                    lblAdesoes.Text = adesoes.ToString();
                    // lblAdoesSub is static ("planos ativos"), already set

                    // ── Revenue this month ───────────────────────────────────
                    decimal receita = 0;
                    using (var cmd = new SqlCommand(
                        @"SELECT ISNULL(SUM(valor),0) FROM pagamento
                          WHERE estado='Pago'
                            AND MONTH(data_pagamento)=MONTH(GETDATE())
                            AND YEAR(data_pagamento)=YEAR(GETDATE())", conn))
                        receita = (decimal)cmd.ExecuteScalar();
                    lblReceita.Text = Theme.FormatEuro(receita);
                    lblReceitaSub.Text = "mês atual";

                    // ── Recent payments TOP 10 ───────────────────────────────
                    var sql = @"SELECT TOP 10
                                  c.nome AS Cliente,
                                  CASE WHEN p.reserva_id IS NOT NULL THEN 'Reserva' ELSE 'Adesão' END AS Tipo,
                                  p.valor AS Valor,
                                  p.estado AS Estado,
                                  CONVERT(varchar,p.data_pagamento,103) AS Data
                                FROM pagamento p
                                JOIN cliente c ON p.cliente_id=c.cliente_id
                                ORDER BY p.data_pagamento DESC";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dgvRecent.DataSource = dt;
                    }

                    // ── Bar chart: reservas por mês (últimos 6 meses) ────────
                    var sqlBar = @"
                        SELECT CAST(YEAR(data_reserva) AS varchar) + '/' +
                               RIGHT('0' + CAST(MONTH(data_reserva) AS varchar), 2) AS Mes,
                               COUNT(*) AS Total
                        FROM reserva
                        WHERE data_reserva >= DATEADD(MONTH, -5, GETDATE())
                        GROUP BY YEAR(data_reserva), MONTH(data_reserva)
                        ORDER BY YEAR(data_reserva), MONTH(data_reserva)";

                    _chartReservas.Series["data"].Points.Clear();
                    using (var cmdBar = new SqlCommand(sqlBar, conn))
                    using (var rdrBar = cmdBar.ExecuteReader())
                    {
                        while (rdrBar.Read())
                            _chartReservas.Series["data"].Points.AddXY(
                                rdrBar.GetString(0), rdrBar.GetInt32(1));
                    }

                    // ── Pie chart: pagamentos por método ─────────────────────
                    var sqlPie = @"
                        SELECT metodo_pagamento, COUNT(*)
                        FROM pagamento WHERE estado='Pago'
                        GROUP BY metodo_pagamento";

                    _chartMetodos.Series["data"].Points.Clear();
                    using (var cmdPie = new SqlCommand(sqlPie, conn))
                    using (var rdrPie = cmdPie.ExecuteReader())
                    {
                        while (rdrPie.Read())
                            _chartMetodos.Series["data"].Points.AddXY(
                                rdrPie.GetString(0), rdrPie.GetInt32(1));
                    }
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
