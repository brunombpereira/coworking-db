using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Clientes redesenhado: stats row (3 KPIs) + toolbar com search +
    /// lista de cards (avatar + info + acções inline) em vez de DataGridView.
    /// </summary>
    public class UcClientes : UserControl
    {
        private Label _kpiTotal, _kpiNovos, _kpiComAdesao;
        private ModernInput _txtSearch;
        private ScrollableList _listContainer;
        private Panel _emptyState;

        public UcClientes()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadData();
        }

        // ── UI ──────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Header (title + Novo Cliente button à direita)
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 84, BackColor = Theme.PageBg,
                Padding = new Padding(Theme.PageHPad, 14, Theme.PageHPad, 0),
            };
            var titleArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var lblTitle = new Label
            {
                Text = "Clientes", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top, Height = 34, AutoSize = false,
            };
            var lblSub = new Label
            {
                Text = "Gestão de contas de cliente do coworking",
                Font = Theme.FontLabel, ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
                Padding = new Padding(0, 4, 0, 0),
            };
            titleArea.Controls.Add(lblSub);
            titleArea.Controls.Add(lblTitle);

            var btnNovo = new ModernButton
            {
                Text  = "+ Novo Cliente",
                Style = ModernButton.Variant.Primary,
                Dock  = DockStyle.Top, Width = 140, Height = 38,
                Margin = new Padding(0),
            };
            btnNovo.Click += (s, e) => OpenEditor(null);

            var btnHolder = new Panel { Dock = DockStyle.Right, Width = 140, BackColor = Theme.PageBg, Padding = new Padding(0, 14, 0, 0) };
            btnHolder.Controls.Add(btnNovo);

            _txtSearch = new ModernInput { Dock = DockStyle.Top, Height = 38 };
            _txtSearch.PlaceholderText = "Procurar nome, NIF, email…";
            _txtSearch.LeadingIcon     = IconChar.MagnifyingGlass;
            _txtSearch.TextChanged    += (s, e) => LoadData();

            // Holder da search com 20px gap à direita (entre search e botão)
            var searchHolder = new Panel { Dock = DockStyle.Right, Width = 240, BackColor = Theme.PageBg, Padding = new Padding(0, 14, 20, 0) };
            searchHolder.Controls.Add(_txtSearch);

            // Em WinForms o ÚLTIMO Dock=Right adicionado fica mais à direita.
            // Queremos [titleArea ......... search   button] → search PRIMEIRO,
            // botão DEPOIS (assim o botão fica rightmost).
            pnlTitle.Controls.Add(titleArea);
            pnlTitle.Controls.Add(searchHolder);
            pnlTitle.Controls.Add(btnHolder);

            // Content (stats + lista — search vai DENTRO do card da lista)
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                BackColor = Theme.PageBg, Padding = new Padding(Theme.PageHPad, 12, Theme.PageHPad, 16),
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));   // stats
            content.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));   // lista

            content.Controls.Add(BuildStatsRow(),  0, 0);
            content.Controls.Add(BuildListCard(),  0, 1);

            Controls.Add(content);
            Controls.Add(pnlTitle);
        }

        // ── Stats row ───────────────────────────────────────────────────
        private Control BuildStatsRow()
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.PageBg };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            var c1 = BuildKpi("Total de clientes",      IconChar.Users,        out _kpiTotal);
            var c2 = BuildKpi("Novos este mês",          IconChar.UserPlus,     out _kpiNovos);
            var c3 = BuildKpi("Com adesão activa",       IconChar.Star,         out _kpiComAdesao);
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
            };

            inner.Controls.Add(valueLbl);
            inner.Controls.Add(topLine);
            card.Controls.Add(inner);
            return card;
        }

        // ── List card ───────────────────────────────────────────────────
        private Control BuildListCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg, BorderColor = Theme.CardBorder,
                CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = Theme.ListInnerPadding };

            _listContainer = new ScrollableList
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg, Visible = false,
            };
            _listContainer.Content.BackColor = Theme.CardBg;

            _emptyState = BuildEmptyState("Nenhum cliente encontrado", IconChar.UserSlash);
            _emptyState.Dock = DockStyle.Fill;
            _emptyState.BackColor = Theme.CardBg;

            inner.Controls.Add(_listContainer);
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
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
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
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM cliente", conn))
                        _kpiTotal.Text = cmd.ExecuteScalar().ToString();
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM cliente
                          WHERE data_registo >= DATEADD(DAY, 1-DAY(GETDATE()), CAST(GETDATE() AS date))", conn))
                        _kpiNovos.Text = cmd.ExecuteScalar().ToString();
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(DISTINCT cliente_id) FROM adesao WHERE estado='Ativa'", conn))
                        _kpiComAdesao.Text = cmd.ExecuteScalar().ToString();

                    LoadClients(conn);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadClients(SqlConnection conn)
        {
            _listContainer.Content.SuspendLayout();
            _listContainer.Content.Controls.Clear();

            var cards = new System.Collections.Generic.List<Control>();
            int totalH = 0;
            int count = 0;
            using (var cmd = new SqlCommand(
                @"SELECT c.cliente_id, c.nome, c.nif, c.email, c.telefone, c.data_registo,
                        (SELECT COUNT(*) FROM reserva WHERE cliente_id = c.cliente_id)        AS num_reservas,
                        (SELECT MAX(data_reserva) FROM reserva WHERE cliente_id = c.cliente_id) AS ultima,
                        CASE WHEN EXISTS (SELECT 1 FROM adesao WHERE cliente_id=c.cliente_id AND estado='Ativa')
                             THEN 1 ELSE 0 END AS tem_adesao
                  FROM cliente c
                  WHERE @q='' OR c.nome LIKE '%'+@q+'%' OR c.nif LIKE '%'+@q+'%' OR c.email LIKE '%'+@q+'%'
                  ORDER BY c.nome", conn))
            {
                cmd.Parameters.AddWithValue("@q", _txtSearch?.Text?.Trim() ?? "");
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var card = BuildClientCard(
                            id:          Convert.ToInt32(r["cliente_id"]),
                            nome:        r["nome"].ToString(),
                            nif:         r["nif"].ToString(),
                            email:       r["email"].ToString(),
                            telefone:    r["telefone"] is DBNull ? null : r["telefone"].ToString(),
                            numReservas: Convert.ToInt32(r["num_reservas"]),
                            ultimaReserva: r["ultima"] is DBNull ? null : (DateTime?)r["ultima"],
                            temAdesao:   Convert.ToInt32(r["tem_adesao"]) == 1
                        );
                        card.Dock = DockStyle.Top;
                        cards.Add(card);
                        totalH += card.Height + 6;
                        count++;
                    }
                }
            }
            // Adicionar reverse — Dock=Top processa em reverse z-order.
            for (int i = cards.Count - 1; i >= 0; i--)
                _listContainer.Content.Controls.Add(cards[i]);
            _listContainer.Content.ResumeLayout();
            _listContainer.UpdateLayout(totalH);

            _listContainer.Visible = count > 0;
            _emptyState.Visible    = count == 0;
            _emptyState.Invalidate();
        }

        // ── Client card ─────────────────────────────────────────────────
        private Control BuildClientCard(int id, string nome, string nif, string email, string telefone,
                                        int numReservas, DateTime? ultimaReserva, bool temAdesao)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = MixColors(Theme.CardBg, Color.White, 0.05f);

            var row = new Panel
            {
                Height = 76, Margin = new Padding(0, 0, 0, 6),
                BackColor = idleBg, Cursor = Cursors.Hand,
            };

            // Avatar
            var avatarHolder = new Panel { Dock = DockStyle.Left, Width = 64, BackColor = idleBg };
            var avatar = new AvatarCircle
            {
                Initial = nome, CircleColor = Theme.Accent,
                Size = new Size(44, 44), Location = new Point(10, 16),
            };
            avatarHolder.Controls.Add(avatar);

            // Acções à direita (edit + delete) — bg NUNCA muda no hover; só
            // a cor do próprio ícone. MouseOverBackColor sincronizado com o
            // estado actual do row (idle ou hover) para evitar o "quadrado".
            var actions = new Panel { Dock = DockStyle.Right, Width = 100, BackColor = idleBg };
            var btnEdit = new IconButton
            {
                IconChar = IconChar.Pen, IconSize = 18, IconColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, BackColor = idleBg, ForeColor = Theme.TextSecondary,
                Size = new Size(40, 40), Location = new Point(8, 18), Cursor = Cursors.Hand,
                TabStop = false,
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.FlatAppearance.MouseOverBackColor = idleBg;
            btnEdit.MouseEnter += (s, e) => btnEdit.IconColor = Theme.Accent;
            btnEdit.MouseLeave += (s, e) => btnEdit.IconColor = Theme.TextSecondary;
            btnEdit.Click += (s, e) => OpenEditor(id);

            var btnDelete = new IconButton
            {
                IconChar = IconChar.TrashCan, IconSize = 18, IconColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, BackColor = idleBg, ForeColor = Theme.TextSecondary,
                Size = new Size(40, 40), Location = new Point(52, 18), Cursor = Cursors.Hand,
                TabStop = false,
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = idleBg;
            btnDelete.MouseEnter += (s, e) => btnDelete.IconColor = Theme.StatusDangerFg;
            btnDelete.MouseLeave += (s, e) => btnDelete.IconColor = Theme.TextSecondary;
            btnDelete.Click += (s, e) => DeleteCliente(id, nome);

            actions.Controls.Add(btnEdit);
            actions.Controls.Add(btnDelete);

            // Stats (reservas + última). Quando numReservas=0 mostramos só
            // 'Sem reservas' (uma linha), centrado verticalmente. Caso
            // contrário 'N reservas' + 'Última dd/MM/yyyy'.
            var statsPanel = new Panel
            {
                Dock = DockStyle.Right, Width = 200, BackColor = idleBg,
            };
            if (numReservas == 0)
            {
                statsPanel.Padding = new Padding(0, 26, 12, 0);  // centrar 1 linha numa altura 76
                var lblNone = new Label
                {
                    Text = "Sem reservas", Font = Theme.FontSub, ForeColor = Theme.TextMuted,
                    Dock = DockStyle.Top, Height = 24, AutoSize = false,
                    TextAlign = ContentAlignment.MiddleRight, BackColor = idleBg,
                };
                statsPanel.Controls.Add(lblNone);
            }
            else
            {
                statsPanel.Padding = new Padding(0, 16, 12, 0);  // centrar 2 linhas (24+20=44)
                var lblStats1 = new Label
                {
                    Text = numReservas + " reserva" + (numReservas == 1 ? "" : "s"),
                    Font = Theme.FontBold, ForeColor = Theme.TextPrimary,
                    Dock = DockStyle.Top, Height = 24, AutoSize = false,
                    TextAlign = ContentAlignment.MiddleRight, BackColor = idleBg,
                };
                var lblStats2 = new Label
                {
                    Text = ultimaReserva.HasValue
                              ? "Última: " + ultimaReserva.Value.ToString("dd/MM/yyyy")
                              : "—",
                    Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                    Dock = DockStyle.Top, Height = 20, AutoSize = false,
                    TextAlign = ContentAlignment.MiddleRight, BackColor = idleBg,
                };
                statsPanel.Controls.Add(lblStats2);
                statsPanel.Controls.Add(lblStats1);
            }

            // Texto principal (nome + badge inline + email + nif/telefone) — Fill
            var pnlText = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(0, 6, 0, 0) };

            // Linha do nome — FlowLayoutPanel para meter o badge ao lado
            var nomeRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 26, AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = idleBg, Padding = new Padding(0),
            };
            var lblNome = new Label
            {
                Text = nome, Font = new Font(Theme.FontBase.FontFamily, 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                AutoSize = true, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 0, 0),
            };
            nomeRow.Controls.Add(lblNome);

            if (temAdesao)
            {
                var badge = new StatusPill
                {
                    Text  = "Com adesão",
                    Width = 140, Height = 22,
                    BackColor = idleBg,
                    AutoWidthFromText = true,
                    HorizontalPadding = 14,            // mais ar à volta do texto
                    Margin = new Padding(10, 3, 0, 0), // 10px gap + alinhamento vertical c/ nome
                };
                badge.SetColors(Theme.StatusSuccessBg, Theme.StatusSuccessFg);
                nomeRow.Controls.Add(badge);
            }

            var lblEmail = new Label
            {
                Text = email, Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblMeta = new Label
            {
                Text = $"NIF {nif}" + (string.IsNullOrEmpty(telefone) ? "" : "  ·  " + telefone),
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            pnlText.Controls.Add(lblMeta);
            pnlText.Controls.Add(lblEmail);
            pnlText.Controls.Add(nomeRow);

            // Em WinForms, o ÚLTIMO Dock=Right adicionado fica mais à direita.
            // Queremos [avatar] [text] [stats] [actions] — actions à direita,
            // logo é adicionado por ÚLTIMO entre os Right-docked.
            row.Controls.Add(pnlText);      // Fill
            row.Controls.Add(statsPanel);   // Right (entre text e actions)
            row.Controls.Add(actions);      // Right (rightmost — adicionado por último)
            row.Controls.Add(avatarHolder); // Left

            // Hover unificado — sincroniza também o MouseOverBackColor dos
            // icon buttons para evitar o flash de "quadrado" quando o cursor
            // passa sobre o lápis/lixeira (queremos só ver a cor do ícone).
            void SetHover(bool on)
            {
                Color bg = on ? hoverBg : idleBg;
                row.BackColor = bg;
                foreach (Control c in row.Controls) PaintChildren(c, bg);
                btnEdit.FlatAppearance.MouseOverBackColor   = bg;
                btnDelete.FlatAppearance.MouseOverBackColor = bg;
            }
            void Hook(Control c)
            {
                c.MouseEnter += (s, e) => SetHover(true);
                c.MouseLeave += (s, e) =>
                {
                    var p = row.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!row.ClientRectangle.Contains(p)) SetHover(false);
                };
                foreach (Control child in c.Controls) Hook(child);
            }
            Hook(row);

            // Click no row (fora dos action buttons) → abre detalhe (read-only)
            void HookRowClick(Control c)
            {
                if (c == btnEdit || c == btnDelete) return;
                c.Click += (s, e) => OpenDetail(id, nome, nif, email, telefone,
                                                 numReservas, ultimaReserva, temAdesao);
                foreach (Control child in c.Controls) HookRowClick(child);
            }
            HookRowClick(row);

            return row;
        }

        // ── Detail (read-only) ──────────────────────────────────────────
        private void OpenDetail(int id, string nome, string nif, string email, string telefone,
                                int numReservas, DateTime? ultimaReserva, bool temAdesao)
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };

            // Avatar circle grande (60) com inicial
            var avatarRow = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Theme.CardBg };
            string initial = string.IsNullOrEmpty(nome) ? "?" : nome.Substring(0, 1).ToUpper();
            avatarRow.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                int diam = 60;
                int cx = 0, cy = (avatarRow.Height - diam) / 2;
                using (var br = new SolidBrush(Theme.Accent))
                    g.FillEllipse(br, cx, cy, diam, diam);
                using (var f = new Font(Theme.FontBase.FontFamily, 22f, FontStyle.Bold))
                {
                    var ts = TextRenderer.MeasureText(g, initial, f, Size.Empty, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, initial, f,
                        new Point(cx + (diam - ts.Width) / 2, cy + (diam - ts.Height) / 2),
                        Color.White, TextFormatFlags.NoPadding);
                }
                // Nome ao lado
                using (var f = new Font(Theme.FontBase.FontFamily, 16f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, nome, f, new Point(diam + 14, cy + 6),
                        Theme.TextPrimary, TextFormatFlags.NoPadding);
                }
                // Chip 'Com adesão' se aplicável
                if (temAdesao)
                {
                    int chipX = diam + 14, chipY = cy + 38;
                    var pillRect = new Rectangle(chipX, chipY, 90, 22);
                    using (var path = ModernCard.RoundedRect(pillRect, 11))
                    using (var br = new SolidBrush(Theme.StatusSuccessBg))
                        g.FillPath(br, path);
                    TextRenderer.DrawText(g, "Com adesão", Theme.FontMicro, pillRect,
                        Theme.StatusSuccessFg,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            };

            // Adicionar fields (Dock=Top, reverse z-order → adicionar último
            // = topo. Aqui queremos avatar em cima → adicionar último.)
            body.Controls.Add(BuildDetailField("Última reserva",
                ultimaReserva.HasValue ? ultimaReserva.Value.ToString("dd/MM/yyyy") : "—"));
            body.Controls.Add(BuildDetailField("Nº reservas",  numReservas.ToString()));
            body.Controls.Add(BuildDetailField("Telefone",     telefone ?? "—"));
            body.Controls.Add(BuildDetailField("Email",        email));
            body.Controls.Add(BuildDetailField("NIF",          nif));
            body.Controls.Add(avatarRow);

            // Modo read-only (onSave=null) → FormDialog mostra só 'Fechar'.
            // Acrescentamos 'Editar dados' no Footer exposto.
            using (var dlg = new CoworkingApp.FormDialog($"Detalhes — {nome}", body, 460, onSave: null))
            {
                var btnEditar = new ModernButton
                {
                    Text = "Editar dados", Style = ModernButton.Variant.Primary,
                    Font = Theme.FontBold, Size = new Size(150, 36),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                };
                // Posicionar à esquerda do botão Fechar dentro do footer.
                btnEditar.Click += (s, e) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };
                if (dlg.Footer != null)
                {
                    // Inserir o botão à esquerda dentro do FlowLayoutPanel do footer.
                    foreach (Control flow in dlg.Footer.Controls)
                    {
                        if (flow is FlowLayoutPanel flp)
                        {
                            flp.Controls.Add(btnEditar);
                            break;
                        }
                    }
                }
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
                    OpenEditor(id);
            }
        }

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

        private static void PaintChildren(Control c, Color bg)
        {
            c.BackColor = bg;
            foreach (Control child in c.Controls) PaintChildren(child, bg);
        }

        private static Color MixColors(Color a, Color b, float t)
            => Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));

        // ── Edit / Delete ───────────────────────────────────────────────
        private void OpenEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var txtNome     = AddField(tbl, "Nome *",     IconChar.User,         placeholder: "Ana Silva");
            var txtNif      = AddField(tbl, "NIF *",      IconChar.IdCard,       placeholder: "123456789");
            var txtEmail    = AddField(tbl, "Email *",    IconChar.Envelope,     placeholder: "ana@example.com");
            var txtTelefone = AddField(tbl, "Telefone",   IconChar.Phone,        placeholder: "912 345 678");

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT nome, nif, email, telefone FROM cliente WHERE cliente_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtNome.Text = r["nome"]?.ToString() ?? "";
                            txtNif.Text = r["nif"]?.ToString() ?? "";
                            txtEmail.Text = r["email"]?.ToString() ?? "";
                            txtTelefone.Text = r["telefone"] is DBNull ? "" : r["telefone"].ToString();
                        }
                    }
                }
            }

            using (var dlg = new CoworkingApp.FormDialog(id.HasValue ? "Editar Cliente" : "Novo Cliente", tbl, 480, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (!Regex.IsMatch(txtNif.Text.Trim(), @"^\d{9}$")) throw new ApplicationException("NIF inválido (9 dígitos).");
                try { _ = new MailAddress(txtEmail.Text.Trim()); } catch { throw new ApplicationException("Email inválido."); }

                var sql = id.HasValue
                    ? "UPDATE cliente SET nome=@n, nif=@nif, email=@e, telefone=@t WHERE cliente_id=@id"
                    : "INSERT INTO cliente (nome, nif, email, telefone) VALUES (@n,@nif,@e,@t)";
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@nif", txtNif.Text.Trim());
                    cmd.Parameters.AddWithValue("@e", txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", string.IsNullOrWhiteSpace(txtTelefone.Text) ? (object)DBNull.Value : txtTelefone.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void DeleteCliente(int id, string nome)
        {
            if (MessageBox.Show($"Eliminar o cliente \"{nome}\"?", "Confirmar",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM pagamento WHERE cliente_id=@id", conn))
                    {
                        chk.Parameters.AddWithValue("@id", id);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — cliente tem pagamentos.", "Aviso",
                                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM cliente WHERE cliente_id=@id", conn))
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

        // ── Form helpers (template partilhado pelos outros UCs) ──────────
        // Cada helper cria um Panel Dock=Top com label+control e devolve o controlo.
        // Para acesso ao Panel wrapper (ex: esconder a row inteira), usar control.Parent:
        //   var cmb = AddCombo(tbl, "Opcional", new[]{"A","B"});
        //   cmb.Parent.Visible = false;
        internal static ModernInput AddField(TableLayoutPanel tbl, string label)
            => AddField(tbl, label, IconChar.None, placeholder: null);

        /// <summary>AddField com ícone leading + placeholder opcional.</summary>
        internal static ModernInput AddField(TableLayoutPanel tbl, string label,
                                              IconChar icon, string placeholder = null)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 12) };
            var input = new ModernInput { Dock = DockStyle.Top, Height = 42 };
            if (icon != IconChar.None) input.LeadingIcon = icon;
            if (placeholder != null)  input.PlaceholderText = placeholder;
            pnl.Controls.Add(input);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return input;
        }

        internal static ComboBox AddCombo(TableLayoutPanel tbl, string label, string[] items)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var cmb = Theme.Combo();
            if (items != null && items.Length > 0) cmb.Items.AddRange(items);
            pnl.Controls.Add(cmb);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return cmb;
        }

        /// <summary>AddModernSelect — picker custom dark-themed (substitui o
        /// ComboBox nativo). Aceita string[] e selecciona o primeiro item.</summary>
        internal static ModernSelect AddModernSelect(TableLayoutPanel tbl, string label, string[] items)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 12) };
            var sel = new ModernSelect { Dock = DockStyle.Top, Height = 42 };
            if (items != null && items.Length > 0) sel.AddItems(items);
            pnl.Controls.Add(sel);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return sel;
        }

        /// <summary>AddModernSelect com DataTable (display/value cols).</summary>
        internal static ModernSelect AddModernSelectDataSource(TableLayoutPanel tbl, string label,
                                                                DataTable dt, string displayCol, string valueCol)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 12) };
            var sel = new ModernSelect { Dock = DockStyle.Top, Height = 42 };
            if (dt != null) sel.BindDataTable(dt, displayCol, valueCol);
            pnl.Controls.Add(sel);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return sel;
        }

        /// <summary>AddModernDateField — campo data dark consistente com
        /// o filtro 'Período' do toolbar (substitui o DateTimePicker
        /// nativo Windows-style).</summary>
        internal static ModernDateField AddModernDateField(TableLayoutPanel tbl, string label)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 12) };
            var dt  = new ModernDateField { Dock = DockStyle.Top, Height = 42, Value = DateTime.Today };
            pnl.Controls.Add(dt);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return dt;
        }

        internal static ComboBox AddComboDataSource(TableLayoutPanel tbl, string label, object dataSource, string display, string value)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var cmb = Theme.Combo();
            // Order matters: assign DisplayMember/ValueMember before DataSource to avoid late binding races.
            cmb.DisplayMember = display;
            cmb.ValueMember   = value;
            cmb.DataSource    = dataSource;
            pnl.Controls.Add(cmb);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return cmb;
        }

        internal static DateTimePicker AddDate(TableLayoutPanel tbl, string label)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var dt = Theme.DatePicker();
            pnl.Controls.Add(dt);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return dt;
        }
    }
}
