using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Catálogo de planos — pricing cards estilo SaaS (Stripe-like) em vez de
    /// DataGridView. Stats no topo, grid de cards por baixo.
    /// </summary>
    public class UcPlanos : UserControl
    {
        private Label _kpiTotal, _kpiSubscritores, _kpiMrr;
        private Panel           _gridHost;     // container scrollable
        private TableLayoutPanel _grid;        // cells × cards
        private Panel           _emptyState;
        private System.Collections.Generic.List<Control> _pendingCards
            = new System.Collections.Generic.List<Control>();

        private const int CardWidth   = 280;
        private const int CardHeight  = 270;
        private const int CardGap     = 12;

        public UcPlanos()
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
                Dock = DockStyle.Top, Height = 84, BackColor = Theme.PageBg,
                Padding = new Padding(24, 18, 24, 0),
            };
            var titleArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            titleArea.Controls.Add(new Label
            {
                Text = "Catálogo de planos de adesão",
                Font = Theme.FontLabel, ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
                Padding = new Padding(0, 4, 0, 0),
            });
            titleArea.Controls.Add(new Label
            {
                Text = "Planos", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top, Height = 34, AutoSize = false,
            });

            var btnNovo = new ModernButton
            {
                Text  = "+ Novo Plano",
                Style = ModernButton.Variant.Primary,
                Dock  = DockStyle.Top, Width = 140, Height = 38,
            };
            btnNovo.Click += (s, e) => OpenEditor(null);
            var btnHolder = new Panel { Dock = DockStyle.Right, Width = 140, BackColor = Theme.PageBg, Padding = new Padding(0, 14, 0, 0) };
            btnHolder.Controls.Add(btnNovo);

            pnlTitle.Controls.Add(titleArea);
            pnlTitle.Controls.Add(btnHolder);

            // Content
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                BackColor = Theme.PageBg, Padding = new Padding(24, 12, 24, 24),
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));   // stats
            content.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));   // grid

            content.Controls.Add(BuildStatsRow(),  0, 0);
            content.Controls.Add(BuildGridCard(),  0, 1);

            Controls.Add(content);
            Controls.Add(pnlTitle);
        }

        // ── Stats row ───────────────────────────────────────────────────
        private Control BuildStatsRow()
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.PageBg };
            for (int i = 0; i < 3; i++)
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            var c1 = BuildKpi("Total de planos",       IconChar.ClipboardList, out _kpiTotal);
            var c2 = BuildKpi("Subscritores activos",  IconChar.Users,         out _kpiSubscritores);
            var c3 = BuildKpi("Receita mensal recorrente", IconChar.SackDollar, out _kpiMrr);
            c1.Margin = new Padding(0, 0, 4, 8);
            c2.Margin = new Padding(4, 0, 4, 8);
            c3.Margin = new Padding(4, 0, 0, 8);
            row.Controls.Add(c1, 0, 0);
            row.Controls.Add(c2, 1, 0);
            row.Controls.Add(c3, 2, 0);
            return row;
        }

        private Control BuildKpi(string title, IconChar icon, out Label valueLbl)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg, BorderColor = Theme.CardBorder,
                CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(18, 14, 18, 12) };

            var iconLbl = new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Right,
                SizeMode = PictureBoxSizeMode.CenterImage, Size = new Size(28, 24),
            };
            var lbl = new Label
            {
                Text = title.ToUpperInvariant(), Font = Theme.FontMicro,
                ForeColor = Theme.TextSecondary, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
            };
            var topLine = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Theme.CardBg };
            topLine.Controls.Add(iconLbl);
            topLine.Controls.Add(lbl);

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

        // ── Grid de cards ───────────────────────────────────────────────
        private Control BuildGridCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg, BorderColor = Theme.CardBorder,
                CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(16, 16, 16, 16) };

            // Host = panel scrollable. Grid = TableLayoutPanel auto-sized
            // que cresce em altura conforme se adicionam linhas — scrollbar
            // do host só aparece quando totalHeight > host.Height.
            _gridHost = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Theme.CardBg,
                Visible    = false,
            };
            _grid = new TableLayoutPanel
            {
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor    = Theme.CardBg,
                Dock         = DockStyle.Top,   // largura = host width, altura ajusta
            };
            _gridHost.Controls.Add(_grid);
            _gridHost.SizeChanged += (s, e) => RebuildGridColumns();

            _emptyState = BuildEmptyState("Nenhum plano definido", IconChar.ClipboardList);
            _emptyState.Dock = DockStyle.Fill;

            inner.Controls.Add(_gridHost);
            inner.Controls.Add(_emptyState);
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
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                if (iconImg == null)
                {
                    using (var pb = new IconPictureBox { IconChar = icon, IconSize = 44, IconColor = Theme.TextMuted })
                        if (pb.Image != null) iconImg = (Image)pb.Image.Clone();
                }
                var textSize = TextRenderer.MeasureText(g, text, Theme.FontBase, Size.Empty, TextFormatFlags.NoPadding);
                int iconSize = 44, gap = 14;
                int totalH = iconSize + gap + textSize.Height;
                int startY = Math.Max(8, (pnl.Height - totalH) / 2);
                int iconX  = (pnl.Width - iconSize) / 2;
                int textX  = (pnl.Width - textSize.Width) / 2;
                if (iconImg != null) g.DrawImage(iconImg, iconX, startY, iconSize, iconSize);
                TextRenderer.DrawText(g, text, Theme.FontBase, new Point(textX, startY + iconSize + gap),
                    Theme.TextMuted, TextFormatFlags.NoPadding);
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
            return pnl;
        }

        // ── Data ────────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    // Stats
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM plano", conn))
                        _kpiTotal.Text = cmd.ExecuteScalar().ToString();

                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(DISTINCT cliente_id) FROM adesao WHERE estado='Ativa'", conn))
                        _kpiSubscritores.Text = cmd.ExecuteScalar().ToString();

                    // MRR — soma de preco_acordado de adesões activas, normalizado a /mês via plano.duracao_meses
                    using (var cmd = new SqlCommand(
                        @"SELECT ISNULL(SUM(a.preco_acordado / NULLIF(p.duracao_meses,0)),0)
                          FROM adesao a JOIN plano p ON a.plano_id = p.plano_id
                          WHERE a.estado='Ativa'", conn))
                    {
                        var mrr = Convert.ToDecimal(cmd.ExecuteScalar());
                        _kpiMrr.Text = Theme.FormatEuro(mrr);
                    }

                    LoadPlanos(conn);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPlanos(SqlConnection conn)
        {
            _pendingCards.Clear();
            int count = 0;
            using (var cmd = new SqlCommand(
                @"SELECT p.plano_id, p.nome_plano, p.tipo_plano, p.preco_mensal,
                        p.duracao_meses, p.descricao,
                        (SELECT COUNT(*) FROM adesao WHERE plano_id = p.plano_id AND estado='Ativa') AS subs
                  FROM plano p
                  ORDER BY p.preco_mensal", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    _pendingCards.Add(BuildPlanoCard(
                        id:        Convert.ToInt32(r["plano_id"]),
                        nome:      r["nome_plano"].ToString(),
                        tipo:      r["tipo_plano"].ToString(),
                        preco:     Convert.ToDecimal(r["preco_mensal"]),
                        duracao:   Convert.ToInt32(r["duracao_meses"]),
                        descricao: r["descricao"] is DBNull ? null : r["descricao"].ToString(),
                        subs:      Convert.ToInt32(r["subs"])
                    ));
                    count++;
                }
            }
            _gridHost.Visible    = count > 0;
            _emptyState.Visible  = count == 0;
            _emptyState.Invalidate();
            RebuildGridColumns();
        }

        // Reconstrói as colunas do TableLayoutPanel baseado na largura
        // disponível e re-distribui os cards. Cada cell tem AbsoluteSize
        // do card → posições deterministicas, sem race conditions.
        private bool _rebuilding;
        private void RebuildGridColumns()
        {
            if (_rebuilding || _grid == null || _gridHost == null) return;
            int avail = _gridHost.ClientSize.Width;
            if (avail < CardWidth + CardGap) return;
            _rebuilding = true;
            try
            {
                _grid.SuspendLayout();
                _grid.Controls.Clear();
                _grid.RowStyles.Clear();
                _grid.ColumnStyles.Clear();
                _grid.RowCount = 0;
                _grid.ColumnCount = 0;

                int cols = Math.Max(1, (avail - CardGap) / (CardWidth + CardGap));
                _grid.ColumnCount = cols;
                for (int c = 0; c < cols; c++)
                    _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, CardWidth + CardGap));

                int rows = (_pendingCards.Count + cols - 1) / cols;
                _grid.RowCount = Math.Max(1, rows);
                for (int r = 0; r < _grid.RowCount; r++)
                    _grid.RowStyles.Add(new RowStyle(SizeType.Absolute, CardHeight + CardGap));

                for (int i = 0; i < _pendingCards.Count; i++)
                {
                    int row = i / cols;
                    int col = i % cols;
                    var card = _pendingCards[i];
                    card.Margin = new Padding(0, 0, CardGap, CardGap);
                    _grid.Controls.Add(card, col, row);
                }
                _grid.ResumeLayout(true);
            }
            finally { _rebuilding = false; }
        }

        // ── Plano card (pricing) ────────────────────────────────────────
        private Control BuildPlanoCard(int id, string nome, string tipo,
                                       decimal preco, int duracao, string descricao, int subs)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = MixColors(Theme.CardBg, Color.White, 0.04f);
            Color borderIdle = Theme.CardBorder;
            Color borderHover = Theme.Accent;

            // Tipo → cor do badge (paleta indigo family)
            Color cyan    = ColorTranslator.FromHtml("#06b6d4");
            Color indigo  = Theme.Accent;
            Color violet  = ColorTranslator.FromHtml("#8b5cf6");
            Color badgeBg, badgeFg;
            switch (tipo)
            {
                case "Flex":    badgeBg = Color.FromArgb(40, cyan);   badgeFg = cyan;   break;
                case "Fixo":    badgeBg = Color.FromArgb(40, indigo); badgeFg = indigo; break;
                case "Privado": badgeBg = Color.FromArgb(40, violet); badgeFg = violet; break;
                default:        badgeBg = Theme.StatusNeutralBg; badgeFg = Theme.StatusNeutralFg; break;
            }

            var card = new ModernCard
            {
                Width = CardWidth, Height = CardHeight,
                BackColor = idleBg, BorderColor = borderIdle,
                CornerRadius = 14, ShowShadow = false,
                Margin = new Padding(0),  // posição é calculada em RelayoutCards
                Cursor = Cursors.Hand,
            };

            // Inner com padding generoso
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(20, 18, 20, 16) };

            // Badge tipo (top)
            var badge = new StatusPill
            {
                Text  = tipo,
                Width = 90, Height = 24,
                Dock  = DockStyle.Top, BackColor = idleBg,
                Margin = new Padding(0),
            };
            badge.SetColors(badgeBg, badgeFg);

            // Spacer entre badge e nome (8px)
            var spBadge = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = idleBg };

            // Nome do plano (Height generoso, sem Padding interno)
            var lblNome = new Label
            {
                Text = nome, Font = new Font(Theme.FontBase.FontFamily, 14f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 28, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            // Spacer entre nome e preço
            var spNome = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = idleBg };

            // Preço grande + /mês — usar TableLayoutPanel para alinhar baseline
            var pricePanel = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = idleBg };
            var lblPrice = new Label
            {
                Text = preco.ToString("#,##0", new CultureInfo("pt-PT")) + " €",
                Font = new Font(Theme.FontBase.FontFamily, 26f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Left, AutoSize = true,
            };
            var lblPeriod = new Label
            {
                Text = "/mês", Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Left, AutoSize = false,
                Width = 60,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(4, 0, 0, 8),
            };
            pricePanel.Controls.Add(lblPeriod);
            pricePanel.Controls.Add(lblPrice);

            // Duração + descrição
            var lblDuracao = new Label
            {
                Text = duracao == 1 ? "Compromisso mensal" : $"Compromisso {duracao} meses",
                Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var spDur = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = idleBg };
            var lblDesc = new Label
            {
                Text = descricao ?? "Sem descrição",
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 36, AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
            };

            // Footer: # subscritores + acções
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = idleBg };
            var lblSubs = new Label
            {
                Text = subs == 0 ? "Sem adesões"
                                 : subs + " adesõe" + (subs == 1 ? "" : "s") + " activa" + (subs == 1 ? "" : "s"),
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Left, AutoSize = false, Width = 140,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var btnDelete = new IconButton
            {
                IconChar = IconChar.TrashCan, IconSize = 16, IconColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, BackColor = idleBg, ForeColor = Theme.TextSecondary,
                Size = new Size(32, 32), Dock = DockStyle.Right, Cursor = Cursors.Hand,
                TabStop = false,
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = idleBg;
            btnDelete.MouseEnter += (s, e) => btnDelete.IconColor = Theme.StatusDangerFg;
            btnDelete.MouseLeave += (s, e) => btnDelete.IconColor = Theme.TextSecondary;
            btnDelete.Click += (s, e) => DeletePlano(id, nome);

            var btnEdit = new IconButton
            {
                IconChar = IconChar.Pen, IconSize = 16, IconColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, BackColor = idleBg, ForeColor = Theme.TextSecondary,
                Size = new Size(32, 32), Dock = DockStyle.Right, Cursor = Cursors.Hand,
                TabStop = false,
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.FlatAppearance.MouseOverBackColor = idleBg;
            btnEdit.MouseEnter += (s, e) => btnEdit.IconColor = Theme.Accent;
            btnEdit.MouseLeave += (s, e) => btnEdit.IconColor = Theme.TextSecondary;
            btnEdit.Click += (s, e) => OpenEditor(id);

            footer.Controls.Add(btnDelete);
            footer.Controls.Add(btnEdit);
            footer.Controls.Add(lblSubs);

            // Ordem de adição (Dock=Top em ordem INVERSA visual):
            // último adicionado fica no topo do stack.
            inner.Controls.Add(footer);    // Bottom — fica no fundo
            inner.Controls.Add(lblDesc);
            inner.Controls.Add(spDur);
            inner.Controls.Add(lblDuracao);
            inner.Controls.Add(pricePanel);
            inner.Controls.Add(spNome);
            inner.Controls.Add(lblNome);
            inner.Controls.Add(spBadge);
            inner.Controls.Add(badge);     // último Top = no topo visual
            card.Controls.Add(inner);

            // Hover: bg lighten + border accent. Recursivo apanha todos os
            // descendentes (incluindo spacers).
            void PaintAll(Control c, Color bg)
            {
                c.BackColor = bg;
                foreach (Control child in c.Controls) PaintAll(child, bg);
            }
            void SetHover(bool on)
            {
                Color bg = on ? hoverBg : idleBg;
                card.BackColor   = bg;
                card.BorderColor = on ? borderHover : borderIdle;
                PaintAll(inner, bg);
                btnEdit.FlatAppearance.MouseOverBackColor   = bg;
                btnDelete.FlatAppearance.MouseOverBackColor = bg;
                card.Invalidate();
            }
            void Hook(Control c)
            {
                c.MouseEnter += (s, e) => SetHover(true);
                c.MouseLeave += (s, e) =>
                {
                    var p = card.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!card.ClientRectangle.Contains(p)) SetHover(false);
                };
                foreach (Control child in c.Controls) Hook(child);
            }
            Hook(card);

            // Click no card (fora dos icons) → detalhe read-only
            void HookClick(Control c)
            {
                if (c == btnEdit || c == btnDelete) return;
                c.Click += (s, e) => OpenDetail(id, nome, tipo, preco, duracao, descricao, subs);
                foreach (Control child in c.Controls) HookClick(child);
            }
            HookClick(card);

            return card;
        }

        // ── Detail (read-only) ──────────────────────────────────────────
        private void OpenDetail(int id, string nome, string tipo, decimal preco,
                                 int duracao, string descricao, int subs)
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };

            // Header com avatar tipo + nome
            Color tipoColor = TipoColor(tipo);
            var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Theme.CardBg };
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                int diam = 60;
                int cx = 0, cy = (header.Height - diam) / 2;
                using (var br = new SolidBrush(tipoColor)) g.FillEllipse(br, cx, cy, diam, diam);
                using (var pb = new IconPictureBox { IconChar = TipoIcon(tipo), IconSize = 28, IconColor = Color.White })
                {
                    if (pb.Image != null)
                        g.DrawImage(pb.Image, cx + (diam - 28) / 2, cy + (diam - 28) / 2 + 1, 28, 28);
                }
                using (var f = new Font(Theme.FontBase.FontFamily, 16f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, nome, f, new Point(diam + 14, cy + 6),
                        Theme.TextPrimary, TextFormatFlags.NoPadding);
                }
                // chip tipo
                var pillRect = new Rectangle(diam + 14, cy + 38, 80, 22);
                using (var path = ModernCard.RoundedRect(pillRect, 11))
                using (var br = new SolidBrush(Color.FromArgb(40, tipoColor)))
                    g.FillPath(br, path);
                TextRenderer.DrawText(g, tipo, Theme.FontMicro, pillRect, tipoColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            };

            // Fields (Dock=Top reverse z → último adicionado = topo).
            string descTxt = string.IsNullOrEmpty(descricao) ? "—" : descricao;
            body.Controls.Add(BuildDetailFieldMulti("Descrição", descTxt));
            body.Controls.Add(BuildDetailField("Subscrições activas", subs.ToString()));
            body.Controls.Add(BuildDetailField("Duração", duracao == 1 ? "1 mês" : $"{duracao} meses"));
            body.Controls.Add(BuildDetailField("Preço mensal", Theme.FormatEuro(preco)));
            body.Controls.Add(header);

            using (var dlg = new FormDialog($"Plano — {nome}", body, 460, onSave: null))
            {
                var btnEditar = new ModernButton
                {
                    Text = "Editar plano", Style = ModernButton.Variant.Primary,
                    Font = Theme.FontBold, Size = new Size(150, 36),
                };
                btnEditar.Click += (s, e) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };
                if (dlg.Footer != null)
                {
                    foreach (Control flow in dlg.Footer.Controls)
                    {
                        if (flow is FlowLayoutPanel flp) { flp.Controls.Add(btnEditar); break; }
                    }
                }
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
                    OpenEditor(id);
            }
        }

        private static Color TipoColor(string tipo) =>
            tipo == "Flex"    ? ColorTranslator.FromHtml("#06b6d4") :
            tipo == "Fixo"    ? Theme.Accent :
            tipo == "Privado" ? ColorTranslator.FromHtml("#8b5cf6") :
                                Theme.Accent;
        private static IconChar TipoIcon(string tipo) =>
            tipo == "Flex"    ? IconChar.PersonRunning :
            tipo == "Fixo"    ? IconChar.Chair :
            tipo == "Privado" ? IconChar.DoorClosed :
                                IconChar.Star;

        private static Panel BuildDetailField(string label, string value)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Theme.CardBg, Padding = new Padding(0, 8, 0, 0) };
            pnl.Controls.Add(new Label
            {
                Text = value, Font = Theme.FontBase, ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = 24,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            pnl.Controls.Add(new Label
            {
                Text = label.ToUpper(), Font = Theme.FontMicro, ForeColor = Theme.TextMuted,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = 16,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        private static Panel BuildDetailFieldMulti(string label, string value)
        {
            int valH = TextRenderer.MeasureText(value, Theme.FontBase,
                new Size(420, 1000), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
            valH = Math.Max(24, valH + 8);
            var pnl = new Panel { Dock = DockStyle.Top, Height = valH + 24, BackColor = Theme.CardBg, Padding = new Padding(0, 8, 0, 0) };
            pnl.Controls.Add(new Label
            {
                Text = value, Font = Theme.FontBase, ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = valH,
                AutoSize = false, TextAlign = ContentAlignment.TopLeft,
            });
            pnl.Controls.Add(new Label
            {
                Text = label.ToUpper(), Font = Theme.FontMicro, ForeColor = Theme.TextMuted,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = 16,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        private static Color MixColors(Color a, Color b, float t)
            => Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));

        // ── Editor / Delete ─────────────────────────────────────────────
        private void OpenEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var txtNome    = UcClientes.AddField(tbl, "Nome *",            IconChar.Star,      placeholder: "Flex Mensal");
            var cmbTipo    = UcClientes.AddCombo(tbl, "Tipo *",            new[] { "Flex", "Fixo", "Privado" });
            var txtPreco   = UcClientes.AddField(tbl, "Preço mensal *",    IconChar.EuroSign,   placeholder: "120.00");
            var txtDuracao = UcClientes.AddField(tbl, "Duração (meses) *", IconChar.CalendarDays, placeholder: "1");
            var txtDesc    = UcClientes.AddField(tbl, "Descrição",         IconChar.AlignLeft, placeholder: "Acesso livre a postos Flex");

            cmbTipo.SelectedIndex = 0;

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT nome_plano, tipo_plano, preco_mensal, duracao_meses, descricao FROM plano WHERE plano_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtNome.Text = r["nome_plano"]?.ToString() ?? "";
                            var tipo = r["tipo_plano"]?.ToString() ?? "Flex";
                            var idx = cmbTipo.Items.IndexOf(tipo);
                            cmbTipo.SelectedIndex = idx >= 0 ? idx : 0;
                            txtPreco.Text = Convert.ToDecimal(r["preco_mensal"]).ToString(CultureInfo.InvariantCulture);
                            txtDuracao.Text = r["duracao_meses"]?.ToString() ?? "";
                            txtDesc.Text = r["descricao"] is DBNull ? "" : r["descricao"].ToString();
                        }
                    }
                }
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Plano" : "Novo Plano", tbl, 480, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (cmbTipo.SelectedItem == null) throw new ApplicationException("Tipo é obrigatório.");
                if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0)
                    throw new ApplicationException("Preço inválido.");
                if (!int.TryParse(txtDuracao.Text, out int dur) || dur <= 0)
                    throw new ApplicationException("Duração inválida (inteiro > 0).");

                var sql = id.HasValue
                    ? "UPDATE plano SET nome_plano=@n, tipo_plano=@tp, preco_mensal=@p, duracao_meses=@d, descricao=@desc WHERE plano_id=@id"
                    : "INSERT INTO plano (nome_plano, tipo_plano, preco_mensal, duracao_meses, descricao) VALUES (@n,@tp,@p,@d,@desc)";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@tp", cmbTipo.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@p", preco);
                    cmd.Parameters.AddWithValue("@d", dur);
                    cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(txtDesc.Text) ? (object)DBNull.Value : txtDesc.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void DeletePlano(int id, string nome)
        {
            if (MessageBox.Show($"Eliminar o plano \"{nome}\"?", "Confirmar",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM adesao WHERE plano_id=@id", conn))
                    {
                        chk.Parameters.AddWithValue("@id", id);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — plano tem adesões.", "Aviso",
                                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM plano WHERE plano_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
