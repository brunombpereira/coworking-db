using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;

namespace CoworkingApp.Controls
{
    public class UcEstatisticas : UserControl
    {
        private Chart _chartReceitaMensal;
        private Chart _chartReceitaMetodo;
        private Chart _chartTopClientes;
        private ScrollableList _listAdesoes;
        private Panel _adesoesEmpty;
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
            ColorTranslator.FromHtml("#ec4899"),
        };

        public UcEstatisticas()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.PageBg;
            BuildUI();
            HandleCreated += (s, e) => { try { CarregarDados(); } catch { /* sem BD */ } };
        }

        // ── BUILD UI ────────────────────────────────────────────────────
        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.PageBg,
                Padding = new Padding(20, 16, 20, 16),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // title
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));  // KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // 2×2 charts/lista

            root.Controls.Add(BuildTitle(), 0, 0);
            root.Controls.Add(BuildKpis(),  0, 1);
            root.Controls.Add(BuildMain(),  0, 2);
            Controls.Add(root);
        }

        private Control BuildTitle()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            pnl.Controls.Add(new Label
            {
                Text = "Estatísticas", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        // ── KPIs ────────────────────────────────────────────────────────
        private Control BuildKpis()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Theme.PageBg, Margin = new Padding(0, 0, 0, 12),
            };
            for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var k1 = BuildKpi("Clientes ativos",  IconChar.Users,        Theme.Accent,          out _lblTotalClientes);
            var k2 = BuildKpi("Receita YTD",       IconChar.EuroSign,     Theme.StatusSuccessFg, out _lblTotalReceita);
            var k3 = BuildKpi("Reservas (mês)",    IconChar.CalendarDays, Theme.Accent,          out _lblReservasMes);
            var k4 = BuildKpi("Ocupação média",    IconChar.ChartPie,     Theme.StatusOrangeFg,  out _lblOcupacao);
            k4.Margin = new Padding(0);
            grid.Controls.Add(k1, 0, 0);
            grid.Controls.Add(k2, 1, 0);
            grid.Controls.Add(k3, 2, 0);
            grid.Controls.Add(k4, 3, 0);
            return grid;
        }

        private Control BuildKpi(string label, IconChar icon, Color iconColor, out Label valueLbl)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
                Margin = new Padding(0, 0, 12, 0),
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(18, 14, 18, 14) };

            var topLine = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Theme.CardBg };
            topLine.Controls.Add(new Label
            {
                Text = label, Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0),
            });
            topLine.Controls.Add(new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = iconColor,
                BackColor = Theme.CardBg, Dock = DockStyle.Left, Width = 22,
                SizeMode = PictureBoxSizeMode.CenterImage,
            });

            valueLbl = new Label
            {
                Text = "—", Font = new Font(Theme.FontBase.FontFamily, 24f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 4, 0, 0),
            };
            inner.Controls.Add(valueLbl);
            inner.Controls.Add(topLine);
            card.Controls.Add(inner);
            return card;
        }

        // ── Main: 2×2 grid ──────────────────────────────────────────────
        private Control BuildMain()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Theme.PageBg,
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _chartReceitaMensal = MakeChart("rec", SeriesChartType.Column);
            _chartReceitaMetodo = MakeChart("met", SeriesChartType.Doughnut);
            _chartTopClientes   = MakeChart("top", SeriesChartType.Bar);

            var cRec  = BuildChartCard("Receita mensal",      IconChar.ChartBar,    _chartReceitaMensal);
            var cMet  = BuildChartCard("Receita por método",  IconChar.CreditCard,  _chartReceitaMetodo);
            var cTop  = BuildChartCard("Top clientes",         IconChar.Star,        _chartTopClientes);
            var cAde  = BuildAdesoesCard();

            cRec.Margin = new Padding(0, 0, 6, 6);
            cMet.Margin = new Padding(6, 0, 0, 6);
            cTop.Margin = new Padding(0, 6, 6, 0);
            cAde.Margin = new Padding(6, 6, 0, 0);

            grid.Controls.Add(cRec, 0, 0);
            grid.Controls.Add(cMet, 1, 0);
            grid.Controls.Add(cTop, 0, 1);
            grid.Controls.Add(cAde, 1, 1);
            return grid;
        }

        private Control BuildChartCard(string title, IconChar icon, Chart chart)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 10, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(14, 10, 14, 14) };

            var header = BuildSectionHeader(title, icon);
            chart.Dock = DockStyle.Fill;
            chart.BackColor = Theme.CardBg;

            inner.Controls.Add(chart);
            inner.Controls.Add(header);
            card.Controls.Add(inner);
            return card;
        }

        private Control BuildAdesoesCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 10, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(14, 10, 14, 14) };

            var header = BuildSectionHeader("Adesões a expirar (30 dias)", IconChar.Clock);

            _listAdesoes = new ScrollableList { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            _listAdesoes.Content.BackColor = Theme.CardBg;
            _adesoesEmpty = BuildEmptyState("Nenhuma adesão a expirar", IconChar.CircleCheck);
            _adesoesEmpty.Dock = DockStyle.Fill;
            _adesoesEmpty.Visible = false;

            var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            body.Controls.Add(_listAdesoes);
            body.Controls.Add(_adesoesEmpty);

            inner.Controls.Add(body);
            inner.Controls.Add(header);
            card.Controls.Add(inner);
            return card;
        }

        private Panel BuildSectionHeader(string title, IconChar icon)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Theme.CardBg };
            pnl.Controls.Add(new Label
            {
                Text = title, Font = Theme.FontSection, ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0),
            });
            pnl.Controls.Add(new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = Theme.Accent,
                BackColor = Theme.CardBg, Dock = DockStyle.Left, Width = 22,
                SizeMode = PictureBoxSizeMode.CenterImage,
            });
            return pnl;
        }

        private Panel BuildEmptyState(string text, IconChar icon)
        {
            var pnl = new Panel { BackColor = Theme.CardBg };
            Image iconImg = null;
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (iconImg == null)
                {
                    using (var pb = new IconPictureBox { IconChar = icon, IconSize = 44, IconColor = Theme.TextMuted })
                        if (pb.Image != null) iconImg = (Image)pb.Image.Clone();
                }
                var ts = TextRenderer.MeasureText(g, text, Theme.FontBase, Size.Empty, TextFormatFlags.NoPadding);
                int iconSize = 44, gap = 14, totalH = iconSize + gap + ts.Height;
                int startY = Math.Max(8, (pnl.Height - totalH) / 2);
                int iconX = (pnl.Width - iconSize) / 2;
                int textX = (pnl.Width - ts.Width) / 2;
                if (iconImg != null) g.DrawImage(iconImg, iconX, startY, iconSize, iconSize);
                TextRenderer.DrawText(g, text, Theme.FontBase, new Point(textX, startY + iconSize + gap),
                    Theme.TextMuted, TextFormatFlags.NoPadding);
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
            return pnl;
        }

        private Chart MakeChart(string seriesName, SeriesChartType type)
        {
            var c = new Chart
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                MinimumSize = new Size(1, 1),
            };
            var area = new ChartArea("main") { BackColor = Color.Transparent };
            area.AxisX.MajorGrid.Enabled    = false;
            area.AxisX.LineColor            = Theme.CardBorder;
            area.AxisY.LineColor            = Theme.CardBorder;
            area.AxisX.LabelStyle.ForeColor = Theme.TextMuted;
            area.AxisY.LabelStyle.ForeColor = Theme.TextMuted;
            area.AxisY.MajorGrid.LineColor  = Theme.CardBorder;
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
                    Font = Theme.FontSub,
                };
                c.Legends.Add(legend);
                s.Legend = "leg";
                s["DoughnutRadius"] = "40";
            }

            return c;
        }

        // ── Data load ───────────────────────────────────────────────────
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
            using (var cmd = new SqlCommand(
                "SELECT COUNT(DISTINCT cliente_id) FROM adesao WHERE estado='Ativa'", conn))
                _lblTotalClientes.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

            using (var cmd = new SqlCommand(
                "SELECT COALESCE(SUM(valor),0) FROM pagamento WHERE estado='Pago' AND YEAR(data_pagamento)=YEAR(GETDATE())", conn))
                _lblTotalReceita.Text = Theme.FormatEuro(Convert.ToDecimal(cmd.ExecuteScalar()));

            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM reserva WHERE YEAR(data_reserva)=YEAR(GETDATE()) AND MONTH(data_reserva)=MONTH(GETDATE())", conn))
                _lblReservasMes.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString();

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
                    s.Points[idx].Color          = Palette[i % Palette.Length];
                    s.Points[idx].Label          = "#PERCENT{P1}";
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
                SELECT cliente_nome AS cliente, nome_plano AS plano,
                       CONVERT(varchar, data_fim, 103) AS data_fim,
                       dias_restantes AS dias
                FROM vw_adesoes_a_expirar
                ORDER BY dias_restantes", conn))
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                da.Fill(dt);
                RenderAdesoes(dt);
            }
        }

        private void RenderAdesoes(DataTable dt)
        {
            _listAdesoes.Content.SuspendLayout();
            _listAdesoes.Content.Controls.Clear();

            var cards = new List<Control>();
            int totalH = 0;
            foreach (DataRow r in dt.Rows)
            {
                string cli   = r["cliente"].ToString();
                string plano = r["plano"].ToString();
                string dataF = r["data_fim"].ToString();
                int    dias  = Convert.ToInt32(r["dias"]);
                var card = BuildAdesaoExpirarCard(cli, plano, dataF, dias);
                card.Dock = DockStyle.Top;
                cards.Add(card);
                totalH += card.Height;
            }
            // Reverse para Dock=Top processar correctamente.
            for (int i = cards.Count - 1; i >= 0; i--)
                _listAdesoes.Content.Controls.Add(cards[i]);

            _listAdesoes.Content.ResumeLayout();
            _listAdesoes.UpdateLayout(totalH);
            _listAdesoes.Visible   = dt.Rows.Count > 0;
            _adesoesEmpty.Visible  = dt.Rows.Count == 0;
            _adesoesEmpty.Invalidate();
        }

        private Control BuildAdesaoExpirarCard(string cliente, string plano, string dataFim, int dias)
        {
            Color idleBg = Theme.CardBg;
            var wrap = new Panel { Height = 66, BackColor = idleBg, Padding = new Padding(0, 0, 0, 6) };
            var row  = new Panel { Dock = DockStyle.Fill, BackColor = idleBg };

            // Direita: chip dias restantes
            var rightInfo = new Panel { Dock = DockStyle.Right, Width = 130, BackColor = idleBg, Padding = new Padding(0, 14, 12, 0) };
            string diasTxt = dias == 1 ? "1 dia" : $"{dias} dias";
            var (bg, fg) = DiasColor(dias);
            var pillHolder = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = idleBg };
            var pill = new StatusPill
            {
                Text = diasTxt, Height = 22, Style = StatusPill.PillStyle.Dot,
                Font = Theme.FontSub, BackColor = idleBg,
            };
            pill.SetColors(bg, fg);
            pill.Dock = DockStyle.Right;
            pill.Width = StatusPill.MeasureDotWidth(diasTxt, Theme.FontSub);
            pillHolder.Controls.Add(pill);
            rightInfo.Controls.Add(pillHolder);

            // Centro: cliente bold + plano sub + data
            var middle = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(12, 10, 8, 0) };
            var lblData = new Label
            {
                Text = "Termina em " + dataFim, Font = Theme.FontSub,
                ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 18, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblPlano = new Label
            {
                Text = plano, Font = Theme.FontSub,
                ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblCli = new Label
            {
                Text = cliente, Font = new Font(Theme.FontBase.FontFamily, 11f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 22, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            // Adicionar em reverse: Top docks processam em reverse z order.
            middle.Controls.Add(lblData);
            middle.Controls.Add(lblPlano);
            middle.Controls.Add(lblCli);

            row.Controls.Add(middle);
            row.Controls.Add(rightInfo);
            wrap.Controls.Add(row);
            return wrap;
        }

        private static (Color bg, Color fg) DiasColor(int dias)
        {
            if (dias <= 7)  return (Theme.StatusDangerBg,  Theme.StatusDangerFg);
            if (dias <= 14) return (Theme.StatusWarningBg, Theme.StatusWarningFg);
            return (Theme.StatusNeutralBg, Theme.StatusNeutralFg);
        }
    }
}
