using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;

namespace CoworkingApp.Controls
{
    public class UcNotificacoes : UserControl
    {
        // KPIs
        private Label _kpiTotal, _kpiPorLer, _kpiLidas;

        // Toolbar
        private SegmentedControl _segFiltro;
        private IconButton _btnRefresh;
        private ModernButton _btnMarcarTodas;

        // Lista
        private ScrollableList _list;
        private Panel _empty;

        // Data cache
        private DataTable _allRows;

        public UcNotificacoes()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            Carregar();
        }

        // ── BUILD UI ────────────────────────────────────────────────────
        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                BackColor = Theme.PageBg,
                Padding = new Padding(20, 16, 20, 16),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // title
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));   // toolbar
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));  // KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // list

            root.Controls.Add(BuildTitle(),   0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);
            root.Controls.Add(BuildKpis(),    0, 2);
            root.Controls.Add(BuildList(),    0, 3);

            Controls.Add(root);
        }

        private Control BuildTitle()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            pnl.Controls.Add(new Label
            {
                Text = "Notificações", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        // ── Toolbar ─────────────────────────────────────────────────────
        private Control BuildToolbar()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
                Margin = new Padding(0, 0, 0, 12),
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(16, 12, 16, 12) };

            // Esquerda: SegmentedControl "Só por ler / Todas"
            _segFiltro = new SegmentedControl
            {
                Segments      = new[] { "Só por ler", "Todas" },
                SelectedIndex = 0,
                Width         = 220, Height = 36,
                Anchor        = AnchorStyles.Top | AnchorStyles.Left,
            };
            _segFiltro.SelectedIndexChanged += (s, e) => RenderRows();

            // Direita: Refresh (só ícone, sem border/bg) + Marcar todas compacto
            _btnRefresh = new IconButton
            {
                IconChar  = IconChar.RotateRight, IconSize = 18,
                IconColor = Theme.TextSecondary, ForeColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, Size = new Size(36, 36),
                BackColor = Theme.CardBg, Cursor = Cursors.Hand, TabStop = false,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnRefresh.FlatAppearance.BorderSize         = 0;
            _btnRefresh.FlatAppearance.MouseOverBackColor = Theme.CardBg;
            _btnRefresh.MouseEnter += (s, e) => _btnRefresh.IconColor = Theme.Accent;
            _btnRefresh.MouseLeave += (s, e) => _btnRefresh.IconColor = Theme.TextSecondary;
            _btnRefresh.Click      += (s, e) => Carregar();

            _btnMarcarTodas = new ModernButton
            {
                Text = "Marcar todas lidas", Style = ModernButton.Variant.Primary,
                Font = Theme.FontBold, Size = new Size(160, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnMarcarTodas.Click += (s, e) => MarcarTodasLidas();

            inner.Controls.Add(_segFiltro);
            inner.Controls.Add(_btnMarcarTodas);
            inner.Controls.Add(_btnRefresh);
            card.Controls.Add(inner);

            void Relayout()
            {
                var dr = inner.DisplayRectangle;
                int cy = dr.Y + (dr.Height - _btnMarcarTodas.Height) / 2;
                _btnMarcarTodas.Location = new Point(dr.Right - _btnMarcarTodas.Width, cy);
                _btnRefresh    .Location = new Point(_btnMarcarTodas.Location.X - _btnRefresh.Width - 8, cy);
                int segY = dr.Y + (dr.Height - _segFiltro.Height) / 2;
                _segFiltro.Location = new Point(dr.X, segY);
            }
            inner.SizeChanged   += (s, e) => Relayout();
            inner.HandleCreated += (s, e) => Relayout();

            return card;
        }

        // ── KPIs ────────────────────────────────────────────────────────
        private Control BuildKpis()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Theme.PageBg,
                Margin = new Padding(0, 0, 0, 12),
            };
            for (int i = 0; i < 3; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var k1 = BuildKpi("Total",     IconChar.Bell,        Theme.Accent,          out _kpiTotal);
            var k2 = BuildKpi("Por ler",   IconChar.Envelope,    Theme.StatusWarningFg, out _kpiPorLer);
            var k3 = BuildKpi("Lidas",     IconChar.CircleCheck, Theme.StatusSuccessFg, out _kpiLidas);
            k3.Margin = new Padding(0);
            grid.Controls.Add(k1, 0, 0);
            grid.Controls.Add(k2, 1, 0);
            grid.Controls.Add(k3, 2, 0);
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

        // ── Lista ───────────────────────────────────────────────────────
        private Control BuildList()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(10) };

            _list = new ScrollableList { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Visible = false };
            _list.Content.BackColor = Theme.CardBg;
            _list.Resize += (s, e) => RenderRows();

            _empty = BuildEmptyState("Sem notificações", IconChar.Bell);
            _empty.Dock = DockStyle.Fill;

            inner.Controls.Add(_list);
            inner.Controls.Add(_empty);
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

        // ── Data ────────────────────────────────────────────────────────
        private void Carregar()
        {
            // SQL: carrega TUDO (filtro por-ler é client-side, para KPIs ficarem corretos)
            string whereCliente = Session.IsCliente ? "AND n.cliente_id = @cid" : "";
            string sql = $@"
                SELECT n.notificacao_id AS id, c.nome AS cliente, n.tipo AS tipo,
                       n.assunto AS assunto, n.mensagem AS mensagem,
                       n.data_criacao AS data, ISNULL(n.lida, 0) AS lida
                FROM notificacao n
                JOIN cliente c ON n.cliente_id = c.cliente_id
                WHERE 1 = 1 {whereCliente}
                ORDER BY n.data_criacao DESC";
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (Session.IsCliente && Session.ClienteId.HasValue)
                        cmd.Parameters.AddWithValue("@cid", Session.ClienteId.Value);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        _allRows = new DataTable();
                        da.Fill(_allRows);
                    }
                }
                RenderRows();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRows()
        {
            if (_allRows == null || _list == null) return;

            _list.Content.SuspendLayout();
            _list.Content.Controls.Clear();

            int total = _allRows.Rows.Count;
            int lidas = 0, porLer = 0;
            foreach (DataRow r in _allRows.Rows)
            {
                bool lida = Convert.ToBoolean(r["lida"]);
                if (lida) lidas++; else porLer++;
            }
            _kpiTotal .Text = total.ToString();
            _kpiPorLer.Text = porLer.ToString();
            _kpiLidas .Text = lidas.ToString();

            // Filter: 0 = Só por ler, 1 = Todas
            bool soPorLer = (_segFiltro.SelectedIndex == 0);
            var rowsView = new List<DataRow>();
            foreach (DataRow r in _allRows.Rows)
            {
                bool lida = Convert.ToBoolean(r["lida"]);
                if (soPorLer && lida) continue;
                rowsView.Add(r);
            }

            int y = 0;
            int width = Math.Max(600, _list.ClientSize.Width - 20);
            foreach (var r in rowsView)
            {
                int id        = Convert.ToInt32(r["id"]);
                string cli    = r["cliente"].ToString();
                string tipo   = r["tipo"].ToString();
                string assun  = r["assunto"].ToString();
                string msg    = r["mensagem"].ToString();
                DateTime data = Convert.ToDateTime(r["data"]);
                bool lida     = Convert.ToBoolean(r["lida"]);

                var card = BuildNotifCard(id, cli, tipo, assun, msg, data, lida);
                card.Location = new Point(0, y);
                card.Width    = width;
                card.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _list.Content.Controls.Add(card);
                y += card.Height + 8;
            }
            _list.Content.ResumeLayout();
            _list.UpdateLayout(y);

            _list .Visible = rowsView.Count > 0;
            _empty.Visible = rowsView.Count == 0;
            _empty.Invalidate();
        }

        // ── Card ────────────────────────────────────────────────────────
        private Control BuildNotifCard(int id, string cliente, string tipo,
                                        string assunto, string mensagem,
                                        DateTime data, bool lida)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = UcEspacos.MixColors(Theme.CardBg, Color.White, 0.05f);

            var row = new Panel
            {
                Height = 84, BackColor = idleBg, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 6),
            };

            // ─── Esquerda: bloco ícone tipo ───────────────────────────
            Color tipoColor = TipoColor(tipo);
            IconChar tipoIcon = TipoIcon(tipo);

            var iconBlock = new Panel { Dock = DockStyle.Left, Width = 72, BackColor = idleBg };
            Image img = null;
            using (var pb = new IconPictureBox
                   { IconChar = tipoIcon, IconSize = 22, IconColor = tipoColor })
                if (pb.Image != null) img = (Image)pb.Image.Clone();
            iconBlock.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int diam = 44;
                int cx = (iconBlock.Width - diam) / 2;
                int cy = (iconBlock.Height - diam) / 2;
                using (var br = new SolidBrush(Color.FromArgb(40, tipoColor)))
                    g.FillEllipse(br, cx, cy, diam, diam);
                if (img != null)
                {
                    int s2 = 22;
                    g.DrawImage(img,
                        cx + (diam - s2) / 2,
                        cy + (diam - s2) / 2 + 1,
                        s2, s2);
                }
            };
            iconBlock.Resize += (s, e) => iconBlock.Invalidate();

            // ─── Direita: data + acção ────────────────────────────────
            var rightInfo = new Panel { Dock = DockStyle.Right, Width = 140, BackColor = idleBg, Padding = new Padding(0, 16, 16, 0) };
            var lblData = new Label
            {
                Text = FormatRelative(data),
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            };
            var pnlAction = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = idleBg };
            if (!lida)
            {
                var btnLer = UcEspacos.MakeIconBtn(IconChar.Check, Theme.Accent, idleBg, () => MarcarLida(id));
                btnLer.Dock = DockStyle.Right;
                btnLer.Size = new Size(32, 28);
                pnlAction.Controls.Add(btnLer);
            }
            rightInfo.Controls.Add(pnlAction);
            rightInfo.Controls.Add(lblData);

            // ─── Centro: assunto + mensagem + cliente ─────────────────
            var middle = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(8, 14, 8, 0) };

            // Linha 1: assunto bold + (dot por-ler)
            var headerLine = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = idleBg };
            var lblAssunto = new Label
            {
                Text = assunto, Font = new Font(Theme.FontBase.FontFamily, 12f, FontStyle.Bold),
                ForeColor = lida ? Theme.TextSecondary : Theme.TextPrimary,
                BackColor = idleBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0),
            };
            if (!lida)
            {
                // Dot indicator à esquerda do assunto
                var dot = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = idleBg };
                dot.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var br = new SolidBrush(Theme.Accent))
                        g.FillEllipse(br, 2, (dot.Height - 8) / 2, 8, 8);
                };
                headerLine.Controls.Add(lblAssunto);
                headerLine.Controls.Add(dot);
            }
            else
            {
                headerLine.Controls.Add(lblAssunto);
            }

            var lblMsg = new Label
            {
                Text = mensagem ?? "", Font = Theme.FontSub,
                ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 18, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
            };
            var lblMeta = new Label
            {
                Text = cliente, Font = Theme.FontSub, ForeColor = Theme.TextMuted,
                BackColor = idleBg, Dock = DockStyle.Top, Height = 16,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            middle.Controls.Add(lblMeta);
            middle.Controls.Add(lblMsg);
            middle.Controls.Add(headerLine);

            row.Controls.Add(middle);
            row.Controls.Add(rightInfo);
            row.Controls.Add(iconBlock);

            // Hover + click → popup detalhe (com auto-mark-read).
            void Hook(Control c)
            {
                c.MouseEnter += (s, e) => SetBg(true);
                c.MouseLeave += (s, e) =>
                {
                    var p = row.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!row.ClientRectangle.Contains(p)) SetBg(false);
                };
                // O botão "marcar lida" tem o seu próprio handler → não propagar click.
                if (!(c is IconButton))
                    c.Click += (s, e) => OpenDetailPopup(id, cliente, tipo, assunto, mensagem, data, lida);
                foreach (Control x in c.Controls) Hook(x);
            }
            void SetBg(bool on)
            {
                Color bg = on ? hoverBg : idleBg;
                void Recurse(Control c) { c.BackColor = bg; foreach (Control x in c.Controls) Recurse(x); }
                Recurse(row);
            }
            Hook(row);

            return row;
        }

        private void OpenDetailPopup(int id, string cliente, string tipo, string assunto,
                                      string mensagem, DateTime data, bool jaLida)
        {
            const int PopupW    = 520;
            const int PadBody   = 24;
            const int HeaderH   = 56;
            const int MetaH     = 22;
            const int SpacerH   = 14;
            int contentW = PopupW - PadBody * 2 - 2;

            var assuntoFont = new Font(Theme.FontBase.FontFamily, 15f, FontStyle.Bold);
            int assuntoH = TextRenderer.MeasureText(assunto ?? "—", assuntoFont,
                new Size(contentW, 1000), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
            int msgH = TextRenderer.MeasureText(mensagem ?? "—", Theme.FontBase,
                new Size(contentW, 1000), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
            // Buffer extra para descenders e wrap edge cases.
            assuntoH = Math.Max(32, assuntoH + 8);
            msgH     = Math.Max(20, msgH + 6);

            int popupH = HeaderH + assuntoH + MetaH + SpacerH + msgH + 4 /*top body padding*/ + PadBody /*bottom*/ + 2 /*border*/;

            var dlg = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Theme.CardBorder,
                Padding         = new Padding(1),
                Size            = new Size(PopupW, popupH),
                ShowInTaskbar   = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg };

            // ─── Header (Dock=Top) com tipo pill + botão X ──────────
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.CardBg };
            var tipoPill = new StatusPill
            {
                Text = tipo, Height = 24, Width = 200, BackColor = Theme.CardBg,
                Style = StatusPill.PillStyle.Dot, Font = Theme.FontSub,
            };
            tipoPill.SetColors(Color.FromArgb(40, TipoColor(tipo)), TipoColor(tipo));

            var btnClose = new IconButton
            {
                IconChar = IconChar.Xmark, IconSize = 22, IconColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, Size = new Size(40, 40),
                BackColor = Theme.CardBg, Cursor = Cursors.Hand, TabStop = false,
            };
            btnClose.FlatAppearance.BorderSize         = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Theme.CardBg;
            btnClose.MouseEnter += (s, e) => btnClose.IconColor = Theme.StatusDangerFg;
            btnClose.MouseLeave += (s, e) => btnClose.IconColor = Theme.TextSecondary;
            btnClose.Click      += (s, e) => dlg.Close();

            header.Controls.Add(tipoPill);
            header.Controls.Add(btnClose);

            void LayoutHeader()
            {
                tipoPill.Location = new Point(24, (header.Height - tipoPill.Height) / 2);
                btnClose.Location = new Point(header.Width - btnClose.Width - 12, (header.Height - btnClose.Height) / 2);
            }
            header.Resize       += (s, e) => LayoutHeader();
            header.HandleCreated += (s, e) => LayoutHeader();

            // ─── Corpo (Dock=Fill) com assunto + meta + mensagem ────
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(PadBody, 4, PadBody, PadBody) };
            var lblAssunto = new Label
            {
                Text = assunto, Font = assuntoFont,
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = assuntoH, AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
            };
            var lblMeta = new Label
            {
                Text = $"{cliente} · {data:dd/MM/yyyy HH:mm}",
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = MetaH, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var spacer = new Panel { Dock = DockStyle.Top, Height = SpacerH, BackColor = Theme.CardBg };
            var lblMsg = new Label
            {
                Text = mensagem ?? "", Font = Theme.FontBase,
                ForeColor = Theme.TextSecondary, BackColor = Theme.CardBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.TopLeft,
            };
            body.Controls.Add(lblMsg);
            body.Controls.Add(spacer);
            body.Controls.Add(lblMeta);
            body.Controls.Add(lblAssunto);

            inner.Controls.Add(body);
            inner.Controls.Add(header);
            dlg.Controls.Add(inner);

            // Auto-mark-read ao fechar (botão X, click fora, ESC).
            dlg.FormClosed += (s, e) =>
            {
                if (!jaLida) MarcarLida(id);
                dlg.Dispose();
            };
            dlg.Deactivate += (s, e) => dlg.Close();
            dlg.KeyPreview = true;
            dlg.KeyDown   += (s, e) => { if (e.KeyCode == Keys.Escape) dlg.Close(); };

            dlg.Show(FindForm());
        }

        private static Color TipoColor(string tipo)
        {
            if (string.IsNullOrEmpty(tipo)) return Theme.Accent;
            if (tipo.StartsWith("Pagamento", StringComparison.OrdinalIgnoreCase)) return Theme.StatusSuccessFg;
            if (tipo.StartsWith("Reserva",   StringComparison.OrdinalIgnoreCase)) return Theme.Accent;
            if (tipo.StartsWith("Adesao",    StringComparison.OrdinalIgnoreCase)) return ColorTranslator.FromHtml("#8b5cf6");
            return Theme.Accent;
        }

        private static IconChar TipoIcon(string tipo)
        {
            if (string.IsNullOrEmpty(tipo)) return IconChar.Bell;
            if (tipo.StartsWith("Pagamento", StringComparison.OrdinalIgnoreCase)) return IconChar.EuroSign;
            if (tipo.Equals("ReservaCancelada", StringComparison.OrdinalIgnoreCase)) return IconChar.CalendarXmark;
            if (tipo.StartsWith("Reserva",   StringComparison.OrdinalIgnoreCase)) return IconChar.CalendarCheck;
            if (tipo.StartsWith("Adesao",    StringComparison.OrdinalIgnoreCase)) return IconChar.Star;
            return IconChar.Bell;
        }

        private static string FormatRelative(DateTime data)
        {
            var now  = DateTime.Now;
            var diff = now - data;
            if (diff.TotalMinutes < 1)  return "agora";
            if (diff.TotalMinutes < 60) return $"há {(int)diff.TotalMinutes} min";
            if (diff.TotalHours   < 24) return $"há {(int)diff.TotalHours} h";
            if (diff.TotalDays    < 7)  return $"há {(int)diff.TotalDays} d";
            return data.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-PT"));
        }

        // ── Acções ─────────────────────────────────────────────────────
        private void MarcarLida(int id)
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("sp_marcar_notificacao_lida", conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@notificacao_id", id);
                    cmd.ExecuteNonQuery();
                }
                Carregar();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MarcarTodasLidas()
        {
            if (_allRows == null || _allRows.Rows.Count == 0) return;
            int porLer = 0;
            foreach (DataRow r in _allRows.Rows)
                if (!Convert.ToBoolean(r["lida"])) porLer++;
            if (porLer == 0) return;

            if (MessageBox.Show($"Marcar {porLer} notificações como lidas?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("sp_marcar_notificacao_lida", conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.Add("@notificacao_id", SqlDbType.Int);
                    foreach (DataRow r in _allRows.Rows)
                    {
                        if (Convert.ToBoolean(r["lida"])) continue;
                        cmd.Parameters["@notificacao_id"].Value = Convert.ToInt32(r["id"]);
                        cmd.ExecuteNonQuery();
                    }
                }
                Carregar();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
