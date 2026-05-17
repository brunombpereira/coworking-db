using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FontAwesome.Sharp;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Dashboard redesenhada: 4 KPI cards no topo, linha + doughnut no meio,
    /// próximas reservas no fundo. Todos os cards usam ModernCard (rounded).
    /// </summary>
    public class UcDashboard : UserControl
    {
        // KPI labels
        private Label _lblReceitaValue, _lblReceitaDelta;
        private Label _lblReservasValue, _lblReservasDelta;
        private Label _lblAdesoesValue, _lblAdesoesDelta;
        private Label _lblOcupValue,     _lblOcupDelta;

        private Chart _heroSparkline;
        private Chart _chartReceita;
        private Chart _chartMetodos;
        private FlowLayoutPanel _flpProximas;

        // Paleta modern (indigo family + emerald accent)
        private static readonly Color[] Palette =
        {
            ColorTranslator.FromHtml("#6366f1"),  // indigo
            ColorTranslator.FromHtml("#8b5cf6"),  // violet
            ColorTranslator.FromHtml("#06b6d4"),  // cyan
            ColorTranslator.FromHtml("#10b981"),  // emerald
            ColorTranslator.FromHtml("#f59e0b"),  // amber
            ColorTranslator.FromHtml("#ec4899"),  // pink
        };

        public UcDashboard()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadData();
        }

        // ── UI ──────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Header
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 72, BackColor = Theme.PageBg,
                Padding = new Padding(24, 18, 24, 0),
            };
            var lblTitle = new Label
            {
                Text = "Dashboard", Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary, Dock = DockStyle.Top,
                Height = 30, AutoSize = false,
            };
            var lblSub = new Label
            {
                Text      = "Visão geral · " + DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy"),
                Font      = Theme.FontLabel,
                ForeColor = Theme.TextSecondary,
                Dock      = DockStyle.Top, Height = 18, AutoSize = false,
            };
            pnlTitle.Controls.Add(lblSub);
            pnlTitle.Controls.Add(lblTitle);

            // Content grid: 3 rows (KPIs / Charts / Próximas)
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.PageBg,
                Padding   = new Padding(24, 12, 24, 24),
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            // Row 1: 4 KPI cards
            var row1 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Theme.PageBg };
            for (int i = 0; i < 4; i++)
                row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            row1.Controls.Add(BuildHeroKpi("Receita do mês", IconChar.SackDollar,  out _lblReceitaValue,  out _lblReceitaDelta, isAccent: true), 0, 0);
            row1.Controls.Add(BuildKpi    ("Reservas hoje",  IconChar.CalendarDay, out _lblReservasValue, out _lblReservasDelta),               1, 0);
            row1.Controls.Add(BuildKpi    ("Adesões ativas", IconChar.Star,        out _lblAdesoesValue,  out _lblAdesoesDelta),                2, 0);
            row1.Controls.Add(BuildKpi    ("Ocupação hoje",  IconChar.ChartPie,    out _lblOcupValue,     out _lblOcupDelta),                   3, 0);

            // Row 2: line chart + doughnut
            var row2 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.PageBg };
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            row2.Controls.Add(BuildChartCard("Receita — últimos 6 meses", IconChar.ChartLine, out _chartReceita, isLine: true),  0, 0);
            row2.Controls.Add(BuildChartCard("Métodos de pagamento",      IconChar.CreditCard, out _chartMetodos, isLine: false), 1, 0);

            // Row 3: próximas reservas
            content.Controls.Add(row1, 0, 0);
            content.Controls.Add(row2, 0, 1);
            content.Controls.Add(BuildProximasCard(), 0, 2);

            Controls.Add(content);
            Controls.Add(pnlTitle);
        }

        // ── KPI cards ───────────────────────────────────────────────────
        private Control BuildHeroKpi(string title, IconChar icon, out Label valueLbl, out Label deltaLbl, bool isAccent)
        {
            var card = new ModernCard
            {
                Dock         = DockStyle.Fill,
                BackColor    = isAccent ? Theme.Accent : Theme.CardBg,
                BorderColor  = isAccent ? Color.Empty  : Theme.CardBorder,
                CornerRadius = 12,
                ShowShadow   = false,
                Margin       = new Padding(0, 0, 8, 0),
            };

            var inner = new Panel { Dock = DockStyle.Fill, BackColor = card.BackColor, Padding = new Padding(18, 14, 18, 12) };

            var iconLbl = new IconPictureBox
            {
                IconChar = icon, IconSize = 16,
                IconColor = isAccent ? Color.FromArgb(230, 255, 255, 255) : Theme.TextSecondary,
                BackColor = card.BackColor, Dock = DockStyle.Right, SizeMode = PictureBoxSizeMode.CenterImage,
                Size = new Size(28, 24),
            };
            var lbl = new Label
            {
                Text = title.ToUpperInvariant(), Font = Theme.FontMicro,
                ForeColor = isAccent ? Color.FromArgb(220, 255, 255, 255) : Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
                BackColor = card.BackColor,
            };
            var topLine = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = card.BackColor };
            topLine.Controls.Add(iconLbl);
            topLine.Controls.Add(lbl);

            valueLbl = new Label
            {
                Text = "—", Font = new Font(Theme.FontBase.FontFamily, 22f, FontStyle.Bold),
                ForeColor = isAccent ? Color.White : Theme.TextPrimary,
                Dock = DockStyle.Top, Height = 38, AutoSize = false,
                BackColor = card.BackColor,
            };

            // Sparkline para o hero (mini bar chart) — Dock=Bottom evita
            // o "Height must be > 0" do Chart durante layout inicial.
            _heroSparkline = BuildChartControl(transparent: true);
            _heroSparkline.Dock   = DockStyle.Bottom;
            _heroSparkline.Height = 40;

            deltaLbl = new Label
            {
                Text = "", Font = Theme.FontLabel,
                ForeColor = isAccent ? Color.FromArgb(220, 255, 255, 255) : Theme.TextSecondary,
                Dock = DockStyle.Bottom, Height = 18, AutoSize = false,
                BackColor = card.BackColor,
            };

            if (isAccent)
            {
                inner.Controls.Add(_heroSparkline);
                inner.Controls.Add(deltaLbl);
                inner.Controls.Add(valueLbl);
                inner.Controls.Add(topLine);
            }
            else
            {
                inner.Controls.Add(deltaLbl);
                inner.Controls.Add(valueLbl);
                inner.Controls.Add(topLine);
            }
            card.Controls.Add(inner);
            return card;
        }

        private Control BuildKpi(string title, IconChar icon, out Label valueLbl, out Label deltaLbl)
            => BuildHeroKpi(title, icon, out valueLbl, out deltaLbl, isAccent: false);

        // ── Chart cards ─────────────────────────────────────────────────
        private Control BuildChartCard(string title, IconChar icon, out Chart chart, bool isLine)
        {
            var card = new ModernCard
            {
                Dock         = DockStyle.Fill,
                BackColor    = Theme.CardBg,
                BorderColor  = Theme.CardBorder,
                CornerRadius = 12,
                ShowShadow   = false,
                Margin       = new Padding(4, 8, 4, 8),
            };

            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(18, 14, 18, 14) };

            var header = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Theme.CardBg };
            var iconLbl = new IconPictureBox
            {
                IconChar = icon, IconSize = 14, IconColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Left,
                Size = new Size(24, 24), SizeMode = PictureBoxSizeMode.CenterImage,
            };
            var lbl = new Label
            {
                Text = title, Font = Theme.FontSection, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg, Padding = new Padding(6, 0, 0, 0),
            };
            header.Controls.Add(lbl);
            header.Controls.Add(iconLbl);

            chart = BuildChartControl(transparent: false);
            chart.Dock = DockStyle.Fill;
            chart.BackColor = Theme.CardBg;

            inner.Controls.Add(chart);
            inner.Controls.Add(header);
            card.Controls.Add(inner);
            return card;
        }

        private static Chart BuildChartControl(bool transparent)
        {
            // MinimumSize evita ArgumentException "Height must be > 0" se o
            // chart for adicionado a um parent ainda sem tamanho (caso comum
            // em containers TableLayoutPanel/ModernCard durante BuildUI).
            var c = new Chart
            {
                BackColor   = transparent ? Color.Transparent : Theme.CardBg,
                MinimumSize = new Size(1, 1),
            };
            var area = new ChartArea
            {
                BackColor = Color.Transparent,
                Position  = { Auto = true },
            };
            area.AxisX.LineColor              = Theme.CardBorder;
            area.AxisY.LineColor              = Theme.CardBorder;
            area.AxisX.LabelStyle.ForeColor   = Theme.TextMuted;
            area.AxisY.LabelStyle.ForeColor   = Theme.TextMuted;
            area.AxisX.LabelStyle.Font        = Theme.FontSub;
            area.AxisY.LabelStyle.Font        = Theme.FontSub;
            area.AxisX.MajorGrid.LineColor    = Color.Transparent;
            area.AxisY.MajorGrid.LineColor    = Color.FromArgb(40, Theme.TextMuted);
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisX.MajorTickMark.LineColor = Color.Transparent;
            area.AxisY.MajorTickMark.LineColor = Color.Transparent;
            c.ChartAreas.Add(area);
            return c;
        }

        // ── Próximas reservas ───────────────────────────────────────────
        private Control BuildProximasCard()
        {
            var card = new ModernCard
            {
                Dock         = DockStyle.Fill,
                BackColor    = Theme.CardBg,
                BorderColor  = Theme.CardBorder,
                CornerRadius = 12,
                ShowShadow   = false,
                Margin       = new Padding(0, 8, 0, 0),
            };

            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(18, 14, 18, 14) };

            var header = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Theme.CardBg };
            var iconLbl = new IconPictureBox
            {
                IconChar = IconChar.Clock, IconSize = 14, IconColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Left,
                Size = new Size(24, 24), SizeMode = PictureBoxSizeMode.CenterImage,
            };
            var lbl = new Label
            {
                Text = "Próximas reservas", Font = Theme.FontSection, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg, Padding = new Padding(6, 0, 0, 0),
            };
            header.Controls.Add(lbl);
            header.Controls.Add(iconLbl);

            _flpProximas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true, BackColor = Theme.CardBg,
            };

            inner.Controls.Add(_flpProximas);
            inner.Controls.Add(header);
            card.Controls.Add(inner);
            return card;
        }

        // ── Data loading ────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    // Hero — receita do mês corrente + delta
                    decimal curr;
                    using (var cmd = new SqlCommand(
                        @"SELECT ISNULL(SUM(valor),0) FROM pagamento
                          WHERE estado='Pago'
                            AND YEAR(data_pagamento)=YEAR(GETDATE())
                            AND MONTH(data_pagamento)=MONTH(GETDATE())", conn))
                    {
                        curr = Convert.ToDecimal(cmd.ExecuteScalar());
                        _lblReceitaValue.Text = Theme.FormatEuro(curr);
                    }
                    using (var cmd = new SqlCommand(
                        @"SELECT ISNULL(SUM(valor),0) FROM pagamento
                          WHERE estado='Pago'
                            AND data_pagamento >= DATEADD(MONTH,-1,DATEADD(DAY,1-DAY(GETDATE()),CAST(GETDATE() AS date)))
                            AND data_pagamento <  DATEADD(DAY,1-DAY(GETDATE()),CAST(GETDATE() AS date))", conn))
                    {
                        var prev = Convert.ToDecimal(cmd.ExecuteScalar());
                        _lblReceitaDelta.Text = prev > 0
                            ? $"{(curr-prev)/prev:+0%;-0%;0%} vs mês anterior"
                            : "sem histórico";
                    }
                    LoadSparkline(conn);

                    // Reservas hoje
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM reserva
                          WHERE data_reserva = CAST(GETDATE() AS date)
                            AND estado IN ('Confirmada','Pendente')", conn))
                    {
                        _lblReservasValue.Text = cmd.ExecuteScalar().ToString();
                        _lblReservasDelta.Text = "agendadas para hoje";
                    }

                    // Adesões ativas
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM adesao WHERE estado='Ativa'", conn))
                        _lblAdesoesValue.Text = cmd.ExecuteScalar().ToString();
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM cliente", conn))
                        _lblAdesoesDelta.Text = "de " + cmd.ExecuteScalar() + " clientes";

                    // Ocupação hoje (% recursos com reserva hoje)
                    int totalRec, ocupRec;
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM recurso", conn))
                        totalRec = Convert.ToInt32(cmd.ExecuteScalar());
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(DISTINCT recurso_id) FROM reserva
                          WHERE data_reserva = CAST(GETDATE() AS date)
                            AND estado IN ('Confirmada','Pendente','Concluida')", conn))
                        ocupRec = Convert.ToInt32(cmd.ExecuteScalar());

                    _lblOcupValue.Text = totalRec > 0 ? $"{ocupRec * 100 / totalRec}%" : "—";
                    _lblOcupDelta.Text = $"{ocupRec} de {totalRec} recursos";

                    LoadReceitaMensal(conn);
                    LoadMetodos(conn);
                    LoadProximas(conn);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSparkline(SqlConnection conn)
        {
            _heroSparkline.Series.Clear();
            var s = new Series
            {
                ChartType           = SeriesChartType.Column,
                Color               = Color.FromArgb(220, 255, 255, 255),
                IsValueShownAsLabel = false,
                BorderWidth         = 0,
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
            // Esconder todos os eixos no sparkline
            var area = _heroSparkline.ChartAreas[0];
            area.BackColor                     = Color.Transparent;
            area.AxisX.LabelStyle.Enabled      = false;
            area.AxisY.LabelStyle.Enabled      = false;
            area.AxisX.LineColor               = Color.Transparent;
            area.AxisY.LineColor               = Color.Transparent;
            area.AxisX.MajorGrid.LineColor     = Color.Transparent;
            area.AxisY.MajorGrid.LineColor     = Color.Transparent;
            area.AxisX.MajorTickMark.LineColor = Color.Transparent;
            area.AxisY.MajorTickMark.LineColor = Color.Transparent;
            area.Position.Auto                 = false;
            area.Position.X      = 0;
            area.Position.Y      = 0;
            area.Position.Width  = 100;
            area.Position.Height = 100;
        }

        private void LoadReceitaMensal(SqlConnection conn)
        {
            _chartReceita.Series.Clear();
            var s = new Series
            {
                ChartType   = SeriesChartType.SplineArea,
                Color       = Color.FromArgb(120, 99, 102, 241),  // indigo translúcido
                BorderColor = Theme.Accent,
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize  = 7,
                MarkerColor = Theme.Accent,
                MarkerBorderColor = Color.White,
                MarkerBorderWidth = 2,
            };
            using (var cmd = new SqlCommand(
                @"SELECT FORMAT(data_pagamento,'yyyy-MM') AS Mes, SUM(valor) AS Total
                  FROM pagamento
                  WHERE estado='Pago' AND data_pagamento >= DATEADD(MONTH,-6,GETDATE())
                  GROUP BY FORMAT(data_pagamento,'yyyy-MM')
                  ORDER BY Mes", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string mes = reader.GetString(0);
                    decimal total = reader.GetDecimal(1);
                    s.Points.AddXY(mes, total);
                }
            }
            _chartReceita.Series.Add(s);
        }

        private void LoadMetodos(SqlConnection conn)
        {
            _chartMetodos.Series.Clear();
            _chartMetodos.Legends.Clear();
            var s = new Series { ChartType = SeriesChartType.Doughnut, BorderWidth = 0 };
            int i = 0;
            using (var cmd = new SqlCommand(
                @"SELECT metodo_pagamento, COUNT(*) FROM pagamento WHERE estado='Pago'
                  GROUP BY metodo_pagamento ORDER BY COUNT(*) DESC", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int idx = s.Points.AddXY(reader.GetString(0), reader.GetInt32(1));
                    s.Points[idx].Color      = Palette[i % Palette.Length];
                    s.Points[idx].Label      = "#PERCENT{P0}";
                    s.Points[idx].LabelForeColor = Color.White;
                    s.Points[idx].LegendText = reader.GetString(0);
                    i++;
                }
            }
            // Doughnut hole maior para look modern
            s["DoughnutRadius"] = "55";
            _chartMetodos.Series.Add(s);

            var legend = new Legend
            {
                Docking      = Docking.Bottom,
                Alignment    = StringAlignment.Center,
                BackColor    = Color.Transparent,
                ForeColor    = Theme.TextSecondary,
                Font         = Theme.FontSub,
                LegendStyle  = LegendStyle.Row,
            };
            _chartMetodos.Legends.Add(legend);
        }

        private void LoadProximas(SqlConnection conn)
        {
            _flpProximas.Controls.Clear();
            int count = 0;
            using (var cmd = new SqlCommand(
                @"SELECT TOP 6
                       c.nome AS Cliente,
                       CASE WHEN s.recurso_id IS NOT NULL THEN s.nome ELSE p.codigo END AS Recurso,
                       rc.tipo,
                       r.data_reserva,
                       r.hora_inicio,
                       r.estado
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
                    _flpProximas.Controls.Add(BuildProximaItem(
                        cliente: reader["Cliente"].ToString(),
                        recurso: reader["Recurso"].ToString(),
                        tipo:    reader["tipo"].ToString(),
                        data:    (DateTime)reader["data_reserva"],
                        hora:    reader["hora_inicio"] is DBNull ? null : (TimeSpan?)reader["hora_inicio"],
                        estado:  reader["estado"].ToString()
                    ));
                    count++;
                }
            }
            if (count == 0)
            {
                _flpProximas.Controls.Add(new Label
                {
                    Text = "Não há reservas agendadas.",
                    Font = Theme.FontBase, ForeColor = Theme.TextMuted,
                    AutoSize = false, Width = 600, Height = 32,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 6, 0, 0),
                    BackColor = Theme.CardBg,
                });
            }
        }

        private Control BuildProximaItem(string cliente, string recurso, string tipo,
                                         DateTime data, TimeSpan? hora, string estado)
        {
            var row = new Panel
            {
                Width = 920, Height = 56, Margin = new Padding(0, 0, 0, 6),
                BackColor = Theme.CardBg,
            };
            // Border-bottom subtil
            row.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(30, Theme.TextMuted), 1))
                    e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
            };

            // Avatar inicial
            var avatar = new AvatarCircle
            {
                Initial     = cliente,
                CircleColor = Theme.Accent,
                Size        = new Size(36, 36),
                Location    = new Point(0, (row.Height - 36) / 2),
            };

            // Cliente + recurso
            var lblCliente = new Label
            {
                Text = cliente, Font = Theme.FontBold, ForeColor = Theme.TextPrimary,
                AutoSize = false, Location = new Point(48, 8),
                Size = new Size(360, 20), TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg,
            };
            var lblRecurso = new Label
            {
                Text = $"{tipo} · {recurso}",
                Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                AutoSize = false, Location = new Point(48, 30),
                Size = new Size(360, 18), TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg,
            };

            // Data + hora
            string horaTxt = hora.HasValue ? hora.Value.ToString(@"hh\:mm") : "Dia inteiro";
            var lblData = new Label
            {
                Text = data.ToString("dd/MM/yyyy"),
                Font = Theme.FontBase, ForeColor = Theme.TextPrimary,
                AutoSize = false, Location = new Point(550, 8),
                Size = new Size(160, 20), TextAlign = ContentAlignment.MiddleRight,
                BackColor = Theme.CardBg,
            };
            var lblHora = new Label
            {
                Text = horaTxt,
                Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                AutoSize = false, Location = new Point(550, 30),
                Size = new Size(160, 18), TextAlign = ContentAlignment.MiddleRight,
                BackColor = Theme.CardBg,
            };

            // Estado pill
            var pill = new Label
            {
                Text = estado,
                Font = Theme.FontMicro,
                ForeColor = estado == "Confirmada" ? Theme.StatusSuccessFg : Theme.StatusWarningFg,
                BackColor = estado == "Confirmada" ? Theme.StatusSuccessBg : Theme.StatusWarningBg,
                AutoSize = false, Size = new Size(90, 22),
                Location = new Point(730, (row.Height - 22) / 2),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0),
            };

            row.Controls.Add(avatar);
            row.Controls.Add(lblCliente);
            row.Controls.Add(lblRecurso);
            row.Controls.Add(lblData);
            row.Controls.Add(lblHora);
            row.Controls.Add(pill);
            return row;
        }
    }
}
