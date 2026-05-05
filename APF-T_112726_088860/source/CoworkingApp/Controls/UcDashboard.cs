using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CoworkingApp.Controls
{
    public class UcDashboard : UserControl
    {
        private Label _lblHeroValue, _lblHeroDelta;
        private Label _lblKpi1Value, _lblKpi1Delta;
        private Label _lblKpi2Value, _lblKpi2Delta;
        private FlowLayoutPanel _flpProximas;
        private Chart _chartMetodos;
        private Chart _heroSparkline;

        public UcDashboard()
        {
            this.BackColor = Theme.PageBg;
            this.Dock = DockStyle.Fill;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Theme.PageBg, Padding = new Padding(20, 14, 20, 0) };
            var lblTitle = new Label { Text = "Dashboard", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, Dock = DockStyle.Top, Height = 28, AutoSize = false };
            var lblSub   = new Label { Text = "Visão geral · " + DateTime.Now.ToString("dd 'de' MMMM yyyy"), Font = Theme.FontLabel, ForeColor = Theme.TextSecondary, Dock = DockStyle.Top, Height = 18, AutoSize = false };
            pnlTitle.Controls.Add(lblSub);
            pnlTitle.Controls.Add(lblTitle);

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.PageBg,
                Padding = new Padding(20, 8, 20, 20)
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ── Row 1: Hero + 2 KPIs ─────────────────────────────────────────
            var row1 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.PageBg };
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1.6f));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1.0f));
            row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1.0f));

            row1.Controls.Add(BuildHeroCard(), 0, 0);
            row1.Controls.Add(BuildKpiCard("Reservas hoje", out _lblKpi1Value, out _lblKpi1Delta), 1, 0);
            row1.Controls.Add(BuildKpiCard("Adesões ativas", out _lblKpi2Value, out _lblKpi2Delta), 2, 0);

            // ── Row 2: Próximas + Pie ────────────────────────────────────────
            var row2 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.PageBg };
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            row2.Controls.Add(BuildProximasCard(), 0, 0);
            row2.Controls.Add(BuildMetodosCard(),  1, 0);

            content.Controls.Add(row1, 0, 0);
            content.Controls.Add(row2, 0, 1);

            this.Controls.Add(content);
            this.Controls.Add(pnlTitle);
        }

        private Panel BuildHeroCard()
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Accent, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(16) };
            card.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(card.ClientRectangle, Theme.Accent, Theme.AccentHover, 135f))
                    e.Graphics.FillRectangle(brush, card.ClientRectangle);
            };

            var lbl = new Label { Text = "RECEITA DO MÊS", Font = Theme.FontMicro, ForeColor = Color.FromArgb(220, 255, 255, 255), Dock = DockStyle.Top, AutoSize = false, Height = 18 };
            _lblHeroValue = new Label { Text = "—", Font = Theme.FontHero, ForeColor = Color.White, Dock = DockStyle.Top, AutoSize = false, Height = 44 };
            _lblHeroDelta = new Label { Text = "", Font = Theme.FontLabel, ForeColor = Color.FromArgb(220, 255, 255, 255), Dock = DockStyle.Top, AutoSize = false, Height = 18 };

            _heroSparkline = BuildChart(transparent: true);
            _heroSparkline.Dock = DockStyle.Bottom;
            _heroSparkline.Height = 50;

            card.Controls.Add(_heroSparkline);
            card.Controls.Add(_lblHeroDelta);
            card.Controls.Add(_lblHeroValue);
            card.Controls.Add(lbl);
            return card;
        }

        private Panel BuildKpiCard(string title, out Label valueLbl, out Label deltaLbl)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(14) };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            var lblTitle = new Label { Text = title.ToUpperInvariant(), Font = Theme.FontMicro, ForeColor = Theme.TextSecondary, Dock = DockStyle.Top, AutoSize = false, Height = 18 };
            valueLbl = new Label { Text = "—", Font = new Font(Theme.FontBase.FontFamily, 22f, FontStyle.Bold), ForeColor = Theme.TextPrimary, Dock = DockStyle.Top, AutoSize = false, Height = 36 };
            deltaLbl = new Label { Text = "", Font = Theme.FontLabel, ForeColor = Theme.TextSecondary, Dock = DockStyle.Top, AutoSize = false, Height = 18 };
            card.Controls.Add(deltaLbl);
            card.Controls.Add(valueLbl);
            card.Controls.Add(lblTitle);
            return card;
        }

        private Panel BuildProximasCard()
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Margin = new Padding(0, 8, 8, 0), Padding = new Padding(14) };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            var lbl = new Label { Text = "Próximas reservas", Font = Theme.FontSection, ForeColor = Theme.TextPrimary, Dock = DockStyle.Top, AutoSize = false, Height = 24 };
            _flpProximas = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
            card.Controls.Add(_flpProximas);
            card.Controls.Add(lbl);
            return card;
        }

        private Panel BuildMetodosCard()
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Margin = new Padding(8, 8, 0, 0), Padding = new Padding(14) };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            var lbl = new Label { Text = "Métodos de pagamento", Font = Theme.FontSection, ForeColor = Theme.TextPrimary, Dock = DockStyle.Top, AutoSize = false, Height = 24 };
            _chartMetodos = BuildChart(transparent: false);
            _chartMetodos.Dock = DockStyle.Fill;
            card.Controls.Add(_chartMetodos);
            card.Controls.Add(lbl);
            return card;
        }

        private static Chart BuildChart(bool transparent)
        {
            var c = new Chart { BackColor = transparent ? Color.Transparent : Theme.CardBg };
            var area = new ChartArea { BackColor = Color.Transparent };
            area.AxisX.LineColor = Theme.CardBorder;
            area.AxisY.LineColor = Theme.CardBorder;
            area.AxisX.LabelStyle.ForeColor = Theme.TextMuted;
            area.AxisY.LabelStyle.ForeColor = Theme.TextMuted;
            area.AxisX.MajorGrid.LineColor = Color.Transparent;
            area.AxisY.MajorGrid.LineColor = Theme.CardBorder;
            c.ChartAreas.Add(area);
            return c;
        }

        private void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    // Hero — receita do mês corrente
                    using (var cmd = new SqlCommand(
                        @"SELECT ISNULL(SUM(valor),0) FROM pagamento
                          WHERE estado='Pago'
                            AND YEAR(data_pagamento)=YEAR(GETDATE())
                            AND MONTH(data_pagamento)=MONTH(GETDATE())", conn))
                    {
                        var v = Convert.ToDecimal(cmd.ExecuteScalar());
                        _lblHeroValue.Text = Theme.FormatEuro(v);
                    }
                    using (var cmd = new SqlCommand(
                        @"SELECT ISNULL(SUM(valor),0) FROM pagamento
                          WHERE estado='Pago'
                            AND data_pagamento >= DATEADD(MONTH,-1,DATEADD(DAY,1-DAY(GETDATE()),CAST(GETDATE() AS date)))
                            AND data_pagamento <  DATEADD(DAY,1-DAY(GETDATE()),CAST(GETDATE() AS date))", conn))
                    {
                        var prev = Convert.ToDecimal(cmd.ExecuteScalar());
                        var curr = decimal.Parse(_lblHeroValue.Text.Replace(" €",""), new System.Globalization.CultureInfo("pt-PT"));
                        _lblHeroDelta.Text = prev > 0
                            ? $"{(curr-prev)/prev:+0%;-0%;0%} vs mês anterior"
                            : "sem histórico";
                    }

                    // Sparkline — receita por mês últimos 6
                    LoadSparkline(conn);

                    // KPI 1 — reservas hoje
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM reserva
                          WHERE data_reserva = CAST(GETDATE() AS date)
                            AND estado IN ('Confirmada','Pendente')", conn))
                    {
                        _lblKpi1Value.Text = cmd.ExecuteScalar().ToString();
                        _lblKpi1Delta.Text = "para hoje";
                    }

                    // KPI 2 — adesões ativas
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM adesao WHERE estado='Ativa'", conn))
                    {
                        _lblKpi2Value.Text = cmd.ExecuteScalar().ToString();
                    }
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM cliente", conn))
                    {
                        _lblKpi2Delta.Text = "de " + cmd.ExecuteScalar() + " clientes";
                    }

                    LoadProximas(conn);
                    LoadMetodos(conn);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSparkline(SqlConnection conn)
        {
            _heroSparkline.Series.Clear();
            var s = new Series
            {
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(220, 255, 255, 255),
                IsValueShownAsLabel = false,
                BorderWidth = 0
            };
            using (var cmd = new SqlCommand(
                @"SELECT TOP 6 FORMAT(data_pagamento,'yyyy-MM') AS Mes, SUM(valor) AS Total
                  FROM pagamento
                  WHERE estado='Pago' AND data_pagamento >= DATEADD(MONTH,-6,GETDATE())
                  GROUP BY FORMAT(data_pagamento,'yyyy-MM')
                  ORDER BY Mes", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    s.Points.AddXY(reader.GetString(0), reader.GetDecimal(1));
            }
            _heroSparkline.Series.Add(s);
            _heroSparkline.ChartAreas[0].AxisX.LabelStyle.Enabled = false;
            _heroSparkline.ChartAreas[0].AxisY.LabelStyle.Enabled = false;
            _heroSparkline.ChartAreas[0].AxisX.LineColor = Color.Transparent;
            _heroSparkline.ChartAreas[0].AxisY.LineColor = Color.Transparent;
            _heroSparkline.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Transparent;
            _heroSparkline.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Transparent;
            _heroSparkline.ChartAreas[0].AxisX.MajorTickMark.LineColor = Color.Transparent;
            _heroSparkline.ChartAreas[0].AxisY.MajorTickMark.LineColor = Color.Transparent;
        }

        private void LoadProximas(SqlConnection conn)
        {
            _flpProximas.Controls.Clear();
            using (var cmd = new SqlCommand(
                @"SELECT TOP 5
                       c.nome AS Cliente,
                       CASE WHEN s.recurso_id IS NOT NULL THEN s.nome ELSE p.codigo END AS Recurso,
                       r.data_reserva,
                       r.hora_inicio
                  FROM reserva r
                  JOIN cliente c ON r.cliente_id = c.cliente_id
                  JOIN recurso rc ON r.recurso_id = rc.recurso_id
                  LEFT JOIN sala s ON rc.recurso_id = s.recurso_id
                  LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
                  WHERE r.data_reserva >= CAST(GETDATE() AS date)
                    AND r.estado IN ('Confirmada','Pendente')
                  ORDER BY r.data_reserva, r.hora_inicio", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var line = new Panel { Width = 380, Height = 26, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 0) };
                    var lblL = new Label
                    {
                        Text = reader["Cliente"] + " · " + reader["Recurso"],
                        Font = Theme.FontBase, ForeColor = Theme.TextPrimary,
                        Dock = DockStyle.Left, AutoSize = false, Width = 240, TextAlign = ContentAlignment.MiddleLeft
                    };
                    var hora = reader["hora_inicio"] == DBNull.Value
                        ? "(dia)"
                        : ((TimeSpan)reader["hora_inicio"]).ToString(@"hh\:mm");
                    var lblR = new Label
                    {
                        Text = ((DateTime)reader["data_reserva"]).ToString("dd/MM") + "  " + hora,
                        Font = Theme.FontBase, ForeColor = Theme.TextSecondary,
                        Dock = DockStyle.Right, AutoSize = false, Width = 130, TextAlign = ContentAlignment.MiddleRight
                    };
                    line.Controls.Add(lblL);
                    line.Controls.Add(lblR);
                    _flpProximas.Controls.Add(line);
                }
            }
        }

        private void LoadMetodos(SqlConnection conn)
        {
            _chartMetodos.Series.Clear();
            _chartMetodos.Legends.Clear();
            var s = new Series { ChartType = SeriesChartType.Doughnut, BorderWidth = 0 };
            var palette = new[] {
                ColorTranslator.FromHtml("#6366f1"),
                ColorTranslator.FromHtml("#8b5cf6"),
                ColorTranslator.FromHtml("#10b981"),
                ColorTranslator.FromHtml("#f59e0b"),
                ColorTranslator.FromHtml("#ef4444")
            };
            int i = 0;
            using (var cmd = new SqlCommand(
                @"SELECT metodo_pagamento, COUNT(*) FROM pagamento WHERE estado='Pago'
                  GROUP BY metodo_pagamento", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int idx = s.Points.AddXY(reader.GetString(0), reader.GetInt32(1));
                    s.Points[idx].Color = palette[i % palette.Length];
                    s.Points[idx].Label = "#PERCENT{P0}";
                    s.Points[idx].LegendText = reader.GetString(0);
                    i++;
                }
            }
            _chartMetodos.Series.Add(s);
            var legend = new Legend { Docking = Docking.Bottom, Alignment = StringAlignment.Center, BackColor = Color.Transparent, ForeColor = Theme.TextSecondary };
            _chartMetodos.Legends.Add(legend);
        }
    }
}
