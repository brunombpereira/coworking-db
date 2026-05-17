using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CoworkingApp.Controls
{
    public class UcEstatisticas : UserControl
    {
        private Chart _chartTopClientes;
        private Chart _chartReceitaMensal;
        private Chart _chartReceitaMetodo;
        private DataGridView _dgvAdesoesExpirar;
        private DataGridView _dgvTopClientes;
        private Label _lblTotalClientes;
        private Label _lblTotalReceita;
        private Label _lblReservasMes;
        private Label _lblOcupacao;

        private static readonly Color[] Palette =
        {
            ColorTranslator.FromHtml("#6366f1"),
            ColorTranslator.FromHtml("#8b5cf6"),
            ColorTranslator.FromHtml("#10b981"),
            ColorTranslator.FromHtml("#f59e0b"),
            ColorTranslator.FromHtml("#ef4444"),
            ColorTranslator.FromHtml("#3b82f6"),
            ColorTranslator.FromHtml("#ec4899")
        };

        public UcEstatisticas()
        {
            InitUI();
            CarregarDados();
        }

        // ─── UI ──────────────────────────────────────────────────────────────

        private void InitUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.PageBg;

            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.PageBg };
            pnlTitle.Controls.Add(new Label
            {
                Text = "Estatísticas Avançadas",
                Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(18, 14)
            });

            // KPI row
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 96,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Theme.PageBg,
                Padding = new Padding(10, 4, 10, 8)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            _lblTotalClientes = KpiCard(pnlKpis, "Clientes ativos", "—", 0);
            _lblTotalReceita  = KpiCard(pnlKpis, "Receita YTD",     "—", 1);
            _lblReservasMes   = KpiCard(pnlKpis, "Reservas (mês)",  "—", 2);
            _lblOcupacao      = KpiCard(pnlKpis, "Ocupação média",  "—", 3);

            // Main grid: 2x2 area de charts/grids
            var pnlMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Theme.PageBg,
                Padding = new Padding(10, 0, 10, 10)
            };
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            _chartReceitaMensal = MakeChart("Receita Mensal", SeriesChartType.Column);
            _chartReceitaMetodo = MakeChart("Receita por Método", SeriesChartType.Doughnut);
            _chartTopClientes   = MakeChart("Top 5 Clientes", SeriesChartType.Bar);

            var pnlAdesoes = MakeCard("Adesões a expirar (30 dias)");
            _dgvAdesoesExpirar = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(_dgvAdesoesExpirar);
            pnlAdesoes.Controls.Add(_dgvAdesoesExpirar);
            pnlAdesoes.Controls.SetChildIndex(_dgvAdesoesExpirar, 0);

            pnlMain.Controls.Add(WrapChart("Receita Mensal", _chartReceitaMensal), 0, 0);
            pnlMain.Controls.Add(WrapChart("Receita por Método", _chartReceitaMetodo), 1, 0);
            pnlMain.Controls.Add(WrapChart("Top Clientes por Receita", _chartTopClientes), 0, 1);
            pnlMain.Controls.Add(pnlAdesoes, 1, 1);

            // Order matters: Fill first, then Top (last added sits topmost)
            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlKpis);
            this.Controls.Add(pnlTitle);
        }

        private Label KpiCard(TableLayoutPanel host, string title, string value, int col)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = Theme.CardBg,
                Padding = new Padding(14, 10, 14, 10)
            };
            var lblT = new Label
            {
                Text = title,
                Font = Theme.FontSub,
                ForeColor = Theme.TextSecondary,
                AutoSize = true
            };
            var lblV = new Label
            {
                Text = value,
                Font = new Font(Theme.FontBase.FontFamily, 18f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 24)
            };
            card.Controls.Add(lblT);
            card.Controls.Add(lblV);
            host.Controls.Add(card, col, 0);
            return lblV;
        }

        private Panel MakeCard(string title)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = Theme.CardBg,
                Padding = new Padding(10, 6, 10, 6)
            };
            var lbl = new Label
            {
                Text = title,
                Font = Theme.FontBold,
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(0, 0, 0, 4)
            };
            pnl.Controls.Add(lbl);
            return pnl;
        }

        private Panel WrapChart(string title, Chart chart)
        {
            var card = MakeCard(title);
            chart.Dock = DockStyle.Fill;
            chart.BackColor = Theme.CardBg;
            card.Controls.Add(chart);
            card.Controls.SetChildIndex(chart, 0);
            return card;
        }

        private Chart MakeChart(string seriesName, SeriesChartType type)
        {
            var c = new Chart { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            var area = new ChartArea("main") { BackColor = Color.Transparent };
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LineColor = Theme.CardBorder;
            area.AxisY.LineColor = Theme.CardBorder;
            area.AxisX.LabelStyle.ForeColor = Theme.TextMuted;
            area.AxisY.LabelStyle.ForeColor = Theme.TextMuted;
            area.AxisY.MajorGrid.LineColor = Theme.CardBorder;
            c.ChartAreas.Add(area);

            var s = new Series(seriesName) { ChartType = type, BorderWidth = 0 };
            if (type == SeriesChartType.Column || type == SeriesChartType.Bar)
                s.Color = Theme.Accent;
            c.Series.Add(s);

            if (type == SeriesChartType.Doughnut || type == SeriesChartType.Pie)
            {
                var legend = new Legend("leg")
                {
                    Docking = Docking.Right,
                    BackColor = Color.Transparent,
                    ForeColor = Theme.TextSecondary,
                    Font = new Font("Segoe UI", 8f)
                };
                c.Legends.Add(legend);
                s.Legend = "leg";
                s["DoughnutRadius"] = "40";
            }

            return c;
        }

        // ─── Data load ──────────────────────────────────────────────────────

        private void CarregarDados()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    LoadKpis(conn);
                    LoadReceitaMensal(conn);
                    LoadReceitaMetodo(conn);
                    LoadTopClientes(conn);
                    LoadAdesoesExpirar(conn);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadKpis(SqlConnection conn)
        {
            // Clientes ativos (com adesão Ativa)
            using (var cmd = new SqlCommand(
                "SELECT COUNT(DISTINCT cliente_id) FROM adesao WHERE estado='Ativa'", conn))
            {
                _lblTotalClientes.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString();
            }
            // Receita YTD
            using (var cmd = new SqlCommand(
                "SELECT COALESCE(SUM(valor),0) FROM pagamento WHERE estado='Pago' AND YEAR(data_pagamento)=YEAR(GETDATE())", conn))
            {
                decimal r = Convert.ToDecimal(cmd.ExecuteScalar());
                _lblTotalReceita.Text = Theme.FormatEuro(r);
            }
            // Reservas do mês corrente
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM reserva WHERE YEAR(data_reserva)=YEAR(GETDATE()) AND MONTH(data_reserva)=MONTH(GETDATE())", conn))
            {
                _lblReservasMes.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString();
            }
            // Ocupação média do mês — usa fn_taxa_ocupacao_espaco por espaço, médias.
            using (var cmd = new SqlCommand(@"
SELECT AVG(dbo.fn_taxa_ocupacao_espaco(e.espaco_id, CAST(GETDATE() AS DATE)))
FROM espaco e", conn))
            {
                var obj = cmd.ExecuteScalar();
                decimal pct = (obj == null || obj == DBNull.Value) ? 0 : Convert.ToDecimal(obj);
                _lblOcupacao.Text = $"{pct:0.0} %";
            }
        }

        private void LoadReceitaMensal(SqlConnection conn)
        {
            var s = _chartReceitaMensal.Series[0];
            s.Points.Clear();
            using (var cmd = new SqlCommand(@"
SELECT TOP 12 CAST(ano AS varchar) + '/' + RIGHT('0'+CAST(mes AS varchar),2) AS m,
       receita_total
FROM vw_receita_mensal
ORDER BY ano, mes", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                    s.Points.AddXY(rdr.GetString(0), Convert.ToDouble(rdr.GetDecimal(1)));
            }
        }

        private void LoadReceitaMetodo(SqlConnection conn)
        {
            var s = _chartReceitaMetodo.Series[0];
            s.Points.Clear();
            int i = 0;
            using (var cmd = new SqlCommand(
                "SELECT metodo_pagamento, receita FROM vw_receita_por_metodo ORDER BY receita DESC", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    int idx = s.Points.AddXY(rdr.GetString(0), Convert.ToDouble(rdr.GetDecimal(1)));
                    s.Points[idx].Color = Palette[i % Palette.Length];
                    s.Points[idx].Label = "#PERCENT{P1}";
                    s.Points[idx].LabelForeColor = Theme.TextOnAccent;
                    i++;
                }
            }
        }

        private void LoadTopClientes(SqlConnection conn)
        {
            var s = _chartTopClientes.Series[0];
            s.Points.Clear();
            int i = 0;
            using (var cmd = new SqlCommand(
                "SELECT TOP 5 nome, receita FROM vw_top_clientes_receita ORDER BY receita DESC", conn))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    int idx = s.Points.AddXY(rdr.GetString(0), Convert.ToDouble(rdr.GetDecimal(1)));
                    s.Points[idx].Color = Palette[i % Palette.Length];
                    s.Points[idx].Label = "€ #VALY{N0}";
                    i++;
                }
            }
        }

        private void LoadAdesoesExpirar(SqlConnection conn)
        {
            using (var cmd = new SqlCommand(@"
SELECT cliente_nome AS Cliente, nome_plano AS Plano,
       CONVERT(varchar, data_fim, 103) AS [Data Fim],
       dias_restantes AS [Dias]
FROM vw_adesoes_a_expirar
ORDER BY dias_restantes", conn))
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                da.Fill(dt);
                _dgvAdesoesExpirar.DataSource = dt;
            }
        }
    }
}
