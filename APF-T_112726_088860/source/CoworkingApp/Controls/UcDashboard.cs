using System;
using System.Data;
using System.Globalization;
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

        private Chart _chartReceita;
        private Chart _chartMetodos;
        private Panel _proximasList;    // scrollable items (visível só se count > 0)
        private Panel _proximasEmpty;   // empty state custom-painted (visível só se count = 0)

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
                Padding = new Padding(Theme.PageHPad, 14, Theme.PageHPad, 0),
            };
            var lblTitle = new Label
            {
                Text = "Dashboard", Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary, Dock = DockStyle.Top,
                Height = 34, AutoSize = false,
            };
            var lblSub = new Label
            {
                Text      = "Visão geral · " + DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy"),
                Font      = Theme.FontLabel,
                ForeColor = Theme.TextSecondary,
                Dock      = DockStyle.Top, Height = 22, AutoSize = false,
                Padding   = new Padding(0, 4, 0, 0),
            };
            pnlTitle.Controls.Add(lblSub);
            pnlTitle.Controls.Add(lblTitle);

            // Content grid: 3 rows (KPIs / Charts / Próximas)
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.PageBg,
                Padding   = new Padding(Theme.PageHPad, 12, Theme.PageHPad, 16),
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));

            // Row 1: 4 KPI cards
            var row1 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Theme.PageBg };
            for (int i = 0; i < 4; i++)
                row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            var hero  = BuildHeroKpi("Receita do mês", IconChar.SackDollar,  out _lblReceitaValue,  out _lblReceitaDelta, isAccent: true);
            var kpi1  = BuildKpi    ("Reservas hoje",  IconChar.CalendarDay, out _lblReservasValue, out _lblReservasDelta);
            var kpi2  = BuildKpi    ("Adesões ativas", IconChar.Star,        out _lblAdesoesValue,  out _lblAdesoesDelta);
            var kpi3  = BuildKpi    ("Ocupação hoje",  IconChar.ChartPie,    out _lblOcupValue,     out _lblOcupDelta);
            // Sobrepor margins para alinhar outer edges com as outras rows.
            hero.Margin = new Padding(0, 0, 4, 8);
            kpi1.Margin = new Padding(4, 0, 4, 8);
            kpi2.Margin = new Padding(4, 0, 4, 8);
            kpi3.Margin = new Padding(4, 0, 0, 8);
            row1.Controls.Add(hero, 0, 0);
            row1.Controls.Add(kpi1, 1, 0);
            row1.Controls.Add(kpi2, 2, 0);
            row1.Controls.Add(kpi3, 3, 0);

            // Row 2: line chart + doughnut
            var row2 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.PageBg };
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            var cardReceita = BuildChartCard("Receita — últimos 6 meses", IconChar.ChartLine, out _chartReceita, isLine: true);
            var cardMetodos = BuildChartCard("Métodos de pagamento",      IconChar.CreditCard, out _chartMetodos, isLine: false);
            cardReceita.Margin = new Padding(0, 0, 4, 8);
            cardMetodos.Margin = new Padding(4, 0, 0, 8);
            row2.Controls.Add(cardReceita, 0, 0);
            row2.Controls.Add(cardMetodos, 1, 0);

            // Row 3: próximas reservas (full-width, sem margins)
            var cardProx = BuildProximasCard();
            cardProx.Margin = new Padding(0, 0, 0, 0);

            content.Controls.Add(row1, 0, 0);
            content.Controls.Add(row2, 0, 1);
            content.Controls.Add(cardProx, 0, 2);

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
                // Margin é definida pelo caller (BuildUI) para alinhamento
                // consistente entre rows.
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
                Dock = DockStyle.Top, Height = 46, AutoSize = false,
                BackColor = card.BackColor,
            };

            deltaLbl = new Label
            {
                Text = "", Font = Theme.FontLabel,
                ForeColor = isAccent ? Color.FromArgb(220, 255, 255, 255) : Theme.TextSecondary,
                Dock = DockStyle.Bottom, Height = 18, AutoSize = false,
                BackColor = card.BackColor,
            };

            inner.Controls.Add(deltaLbl);
            inner.Controls.Add(valueLbl);
            inner.Controls.Add(topLine);
            card.Controls.Add(inner);

            // Capturar out params em locais — não se pode ler 'out' dentro de
            // lambdas/local functions (CS1628).
            Label vLbl = valueLbl;
            Label dLbl = deltaLbl;

            // Hover: bg ligeiramente mais claro (acende) + cursor hand.
            Color idleBg  = card.BackColor;
            Color hoverBg = isAccent
                ? MixColors(idleBg, Color.White, 0.05f)
                : MixColors(idleBg, Color.White, 0.06f);

            Color idleBorder = card.BorderColor;
            void SetCardHover(bool on)
            {
                Color bg = on ? hoverBg : idleBg;
                card.BackColor    = bg;
                inner.BackColor   = bg;
                topLine.BackColor = bg;
                iconLbl.BackColor = bg;
                lbl.BackColor     = bg;
                vLbl.BackColor    = bg;
                dLbl.BackColor    = bg;
                // Hover: border accent indigo (excepto no card hero accent
                // que já tem cor própria).
                if (!isAccent)
                {
                    card.BorderColor = on ? Theme.Accent : idleBorder;
                    card.Invalidate();
                }
            }
            void HookCard(Control c)
            {
                c.Cursor      = Cursors.Hand;
                c.MouseEnter += (s, e) => SetCardHover(true);
                c.MouseLeave += (s, e) =>
                {
                    var p = card.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!card.ClientRectangle.Contains(p)) SetCardHover(false);
                };
                foreach (Control child in c.Controls) HookCard(child);
            }
            HookCard(card);
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
                // Margin definida no caller.
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
                // Margin definida no caller.
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

            // Container que alterna list / empty conforme há ou não dados.
            // Usar dois Panels separados (em vez de um FLP) garante que a
            // scrollbar só aparece quando há items que excedem o espaço.
            _proximasList = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Theme.CardBg,
                Visible    = false,
            };
            _proximasList.Resize += (s, e) => ResizeProximasItems();

            _proximasEmpty = BuildEmptyState("Não há reservas agendadas", IconChar.CalendarCheck);
            _proximasEmpty.Dock      = DockStyle.Fill;
            _proximasEmpty.BackColor = Theme.CardBg;
            _proximasEmpty.Visible   = true;

            inner.Controls.Add(_proximasList);
            inner.Controls.Add(_proximasEmpty);
            inner.Controls.Add(header);
            card.Controls.Add(inner);
            return card;
        }

        private Panel BuildEmptyState(string text, IconChar icon)
        {
            var pnl = new Panel { BackColor = Theme.CardBg };
            Image iconImg = null;
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Render do icon lazy (precisa de Parent setado)
                if (iconImg == null)
                {
                    using (var pb = new IconPictureBox
                           { IconChar = icon, IconSize = 44, IconColor = Theme.TextMuted })
                    {
                        if (pb.Image != null) iconImg = (Image)pb.Image.Clone();
                    }
                }

                var textSize = TextRenderer.MeasureText(g, text, Theme.FontBase,
                    Size.Empty, TextFormatFlags.NoPadding);
                int iconSize = 44, gap = 14;
                int totalH   = iconSize + gap + textSize.Height;
                int startY   = Math.Max(8, (pnl.Height - totalH) / 2);
                int iconX    = (pnl.Width - iconSize) / 2;
                int textX    = (pnl.Width - textSize.Width) / 2;

                if (iconImg != null)
                    g.DrawImage(iconImg, iconX, startY, iconSize, iconSize);
                TextRenderer.DrawText(g, text, Theme.FontBase,
                    new Point(textX, startY + iconSize + gap), Theme.TextMuted,
                    TextFormatFlags.NoPadding);
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
            return pnl;
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

        private void LoadReceitaMensal(SqlConnection conn)
        {
            _chartReceita.Series.Clear();
            // Line (não SplineArea) — renderiza bem mesmo com 1-2 pontos.
            // SplineArea com poucos pontos produzia "bars" verticais soltas.
            var s = new Series
            {
                ChartType         = SeriesChartType.Line,
                Color             = Theme.Accent,
                BorderWidth       = 3,
                MarkerStyle       = MarkerStyle.Circle,
                MarkerSize        = 9,
                MarkerColor       = Theme.Accent,
                MarkerBorderColor = Color.White,
                MarkerBorderWidth = 2,
                IsValueShownAsLabel = false,
                // CRÍTICO: 1 categoria por ponto. Sem isto, o framework
                // tenta parsear '2026-02' como número e falha → todos os
                // pontos ficam em X=0 (empilhados na mesma vertical).
                IsXValueIndexed   = true,
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
                    int idx = s.Points.AddXY(mes, total);
                    s.Points[idx].ToolTip = $"{mes}: {Theme.FormatEuro(total)}";
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
                    string metodo = reader.GetString(0);
                    int count = reader.GetInt32(1);
                    int idx = s.Points.AddXY(metodo, count);
                    s.Points[idx].Color      = Palette[i % Palette.Length];
                    s.Points[idx].Label      = "#PERCENT{P0}";
                    s.Points[idx].LabelForeColor = Color.White;
                    s.Points[idx].LegendText = metodo;
                    s.Points[idx].ToolTip    = $"{metodo}: {count} pagamento" + (count == 1 ? "" : "s") + " (#PERCENT{P1})";
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
                Font         = new Font(Theme.FontBase.FontFamily, 8.5f),
                LegendStyle  = LegendStyle.Table,
                TableStyle   = LegendTableStyle.Wide,   // wraps to multiple rows se preciso
                MaximumAutoSize = 35,                   // até 35% da chart area
            };
            _chartMetodos.Legends.Add(legend);
        }

        private void ResizeProximasItems()
        {
            if (_proximasList == null) return;
            int w = _proximasList.ClientSize.Width;
            if (w < 100) return;
            foreach (Control c in _proximasList.Controls)
                c.Width = w;
        }

        private void LoadProximas(SqlConnection conn, int? cliId = null)
        {
            _proximasList.Controls.Clear();
            int count = 0;
            int y     = 0;
            string filtroCli = cliId.HasValue ? " AND r.cliente_id = @cli " : "";
            using (var cmd = new SqlCommand(
                $@"SELECT TOP 6
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
                    {filtroCli}
                  ORDER BY r.data_reserva, r.hora_inicio", conn))
            {
                if (cliId.HasValue) cmd.Parameters.AddWithValue("@cli", cliId.Value);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var item = BuildProximaItem(
                        cliente: reader["Cliente"].ToString(),
                        recurso: reader["Recurso"].ToString(),
                        tipo:    reader["tipo"].ToString(),
                        data:    (DateTime)reader["data_reserva"],
                        hora:    reader["hora_inicio"] is DBNull ? null : (TimeSpan?)reader["hora_inicio"],
                        estado:  reader["estado"].ToString()
                    );
                    item.Location = new Point(0, y);
                    item.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    item.Width    = _proximasList.ClientSize.Width;
                    _proximasList.Controls.Add(item);
                    y += item.Height + 4;
                    count++;
                }
            }
            } // fecha o bloco do `using cmd`

            // Toggle visibility: lista OU empty state — nunca os dois.
            _proximasList.Visible  = count > 0;
            _proximasEmpty.Visible = count == 0;
            _proximasEmpty.Invalidate();   // forçar repaint do empty state

            ResizeProximasItems();
        }

        private Control BuildProximaItem(string cliente, string recurso, string tipo,
                                         DateTime data, TimeSpan? hora, string estado)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = MixColors(Theme.CardBg, Color.White, 0.06f);

            var row = new Panel
            {
                Height    = 60,
                Margin    = new Padding(0, 0, 0, 4),
                BackColor = idleBg,
                Cursor    = Cursors.Hand,
            };

            // Avatar à esquerda (dock left, fixo)
            var avatarHolder = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 56,
                BackColor = idleBg,
            };
            var avatar = new AvatarCircle
            {
                Initial     = cliente,
                CircleColor = Theme.Accent,
                Size        = new Size(40, 40),
                Location    = new Point(4, 10),
            };
            avatarHolder.Controls.Add(avatar);

            // Pill (estado) à direita
            var pill = new StatusPill
            {
                Text      = estado,
                Dock      = DockStyle.Right,
                Width     = 110,
                Margin    = new Padding(0),
                BackColor = idleBg,
            };
            switch (estado)
            {
                case "Confirmada": pill.SetColors(Theme.StatusSuccessBg, Theme.StatusSuccessFg); break;
                case "Pendente":   pill.SetColors(Theme.StatusWarningBg, Theme.StatusWarningFg); break;
                case "Concluida":  pill.SetColors(Theme.StatusNeutralBg, Theme.StatusNeutralFg); break;
                default:           pill.SetColors(Theme.StatusNeutralBg, Theme.StatusNeutralFg); break;
            }

            // Data + hora — dock right depois da pill
            string horaTxt = hora.HasValue ? hora.Value.ToString(@"hh\:mm") : "Dia inteiro";
            var pnlDate = new Panel { Dock = DockStyle.Right, Width = 150, BackColor = idleBg };
            var lblHora = new Label
            {
                Text = horaTxt, Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight, BackColor = idleBg,
            };
            var lblData = new Label
            {
                Text = data.ToString("dd/MM/yyyy"), Font = Theme.FontBase, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top, Height = 24, AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight, BackColor = idleBg,
                Padding = new Padding(0, 8, 4, 0),
            };
            pnlDate.Controls.Add(lblHora);
            pnlDate.Controls.Add(lblData);

            // Texto no meio (fill)
            var pnlText = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(4, 0, 0, 0) };
            var lblRecurso = new Label
            {
                Text = $"{tipo} · {recurso}", Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 20, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, BackColor = idleBg,
            };
            var lblCliente = new Label
            {
                Text = cliente, Font = Theme.FontBold, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top, Height = 24, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, BackColor = idleBg,
                Padding = new Padding(0, 8, 0, 0),
            };
            pnlText.Controls.Add(lblRecurso);
            pnlText.Controls.Add(lblCliente);

            // Ordem importa para o dock stacking
            row.Controls.Add(pnlText);    // Fill — adicionado primeiro, ocupa o resto
            row.Controls.Add(pill);       // Right — adicionado depois, fica mais à direita
            row.Controls.Add(pnlDate);    // Right — adicionado por último, fica mais à esquerda das outras Right
            row.Controls.Add(avatarHolder); // Left

            // Hover — actualiza bg de tudo
            void SetHover(bool on)
            {
                Color bg = on ? hoverBg : idleBg;
                row.BackColor          = bg;
                avatarHolder.BackColor = bg;
                pnlDate.BackColor      = bg;
                pnlText.BackColor      = bg;
                pill.BackColor         = bg;
                lblHora.BackColor      = bg;
                lblData.BackColor      = bg;
                lblRecurso.BackColor   = bg;
                lblCliente.BackColor   = bg;
            }
            void Hook(Control c)
            {
                c.MouseEnter += (s, e) => SetHover(true);
                c.MouseLeave += (s, e) =>
                {
                    // Só remove hover se o cursor saiu mesmo do row.
                    // Qualificar System.Windows.Forms.Cursor — Control.Cursor
                    // é uma instance property que faria sombra à classe estática.
                    var p = row.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!row.ClientRectangle.Contains(p)) SetHover(false);
                };
                foreach (Control child in c.Controls) Hook(child);
            }
            Hook(row);

            return row;
        }

        // Mistura linear de duas cores (0 = a, 1 = b).
        private static Color MixColors(Color a, Color b, float t)
            => Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
    }
}
