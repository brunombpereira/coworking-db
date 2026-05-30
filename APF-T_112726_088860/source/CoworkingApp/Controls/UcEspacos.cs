using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using CoworkingApp;
using FontAwesome.Sharp;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Gestão de Espaços, Salas e Postos. Pill tabs custom + stats row +
    /// listas de cards (espaços = vertical, salas/postos = grid horizontal).
    /// </summary>
    public class UcEspacos : UserControl
    {
        private enum Tab { Espacos, Salas, Postos }
        private Tab _active = Tab.Espacos;

        private Label _kpiEspacos, _kpiSalas, _kpiPostos;
        private ModernButton _btnNovo;
        private TabButton _tabEspacos, _tabSalas, _tabPostos;

        // Espaços — vertical list scrollable
        private Panel _espacosList;
        private Panel _espacosEmpty;

        // Salas — grid de cards
        private Panel _salasHost;
        private TableLayoutPanel _salasGrid;
        private Panel _salasEmpty;
        private readonly List<Control> _salasCards = new List<Control>();

        // Postos — grid de cards
        private Panel _postosHost;
        private TableLayoutPanel _postosGrid;
        private Panel _postosEmpty;
        private readonly List<Control> _postosCards = new List<Control>();

        private const int SmallCardW = 280;
        private const int SmallCardH = 250;
        private const int CardGap    = 14;

        public UcEspacos()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadAll();
        }

        // ── UI ──────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Header ──────────────────────────────────────────────────
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 84, BackColor = Theme.PageBg,
                Padding = new Padding(24, 18, 24, 0),
            };
            var titleArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            titleArea.Controls.Add(new Label
            {
                Text = "Gestão dos espaços físicos, salas e postos de trabalho",
                Font = Theme.FontLabel, ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
                Padding = new Padding(0, 4, 0, 0),
            });
            titleArea.Controls.Add(new Label
            {
                // && para escapar — & sozinho é interpretado como mnemonic
                // accelerator e sublinha a próxima letra (e não aparece).
                Text = "Espaços && Recursos", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top, Height = 34, AutoSize = false,
            });

            _btnNovo = new ModernButton
            {
                Text  = "+ Novo Espaço",
                Style = ModernButton.Variant.Primary,
                Dock  = DockStyle.Top, Width = 160, Height = 38,
            };
            _btnNovo.Click += (s, e) => OnNovoClick();
            var btnHolder = new Panel { Dock = DockStyle.Right, Width = 160, BackColor = Theme.PageBg, Padding = new Padding(0, 14, 0, 0) };
            btnHolder.Controls.Add(_btnNovo);

            pnlTitle.Controls.Add(titleArea);
            pnlTitle.Controls.Add(btnHolder);

            // ── Content stack ───────────────────────────────────────────
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.PageBg, Padding = new Padding(24, 12, 24, 24),
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute,  54f));  // tabs
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));  // stats
            content.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));  // tab content

            content.Controls.Add(BuildTabBar(),    0, 0);
            content.Controls.Add(BuildStatsRow(),  0, 1);
            content.Controls.Add(BuildContent(),   0, 2);

            Controls.Add(content);
            Controls.Add(pnlTitle);
            SwitchTab(Tab.Espacos);
        }

        // ── Underline tab bar (estilo Linear/GitHub) ────────────────────
        private Control BuildTabBar()
        {
            var bar = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            // Linha subtil ao longo do fundo do bar — active tab destaca-se com
            // o seu próprio underline em accent que sobrepõe esta linha.
            bar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            };
            _tabEspacos = new TabButton { Text = "Espaços", Icon = IconChar.Building, Margin = new Padding(0, 6, 4, 0) };
            _tabSalas   = new TabButton { Text = "Salas",   Icon = IconChar.DoorOpen, Margin = new Padding(0, 6, 4, 0) };
            _tabPostos  = new TabButton { Text = "Postos",  Icon = IconChar.Chair,    Margin = new Padding(0, 6, 0, 0) };
            _tabEspacos.Click += (s, e) => SwitchTab(Tab.Espacos);
            _tabSalas.Click   += (s, e) => SwitchTab(Tab.Salas);
            _tabPostos.Click  += (s, e) => SwitchTab(Tab.Postos);

            // FlowLayoutPanel respeita o AutoSize de cada TabButton (calculado
            // por texto). Antes tinha Locations hardcoded (0, 120, 240) que
            // criavam gaps/overlaps quando o RecalcSize por texto mudou os
            // widths.
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, BackColor = Theme.PageBg,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                AutoSize = false, Padding = new Padding(0),
            };
            flow.Controls.Add(_tabEspacos);
            flow.Controls.Add(_tabSalas);
            flow.Controls.Add(_tabPostos);
            bar.Controls.Add(flow);
            return bar;
        }

        // ── Stats row ───────────────────────────────────────────────────
        private Control BuildStatsRow()
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.PageBg };
            for (int i = 0; i < 3; i++) row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            var c1 = BuildKpi("Espaços físicos", IconChar.Building,       out _kpiEspacos);
            var c2 = BuildKpi("Salas",           IconChar.DoorOpen,       out _kpiSalas);
            var c3 = BuildKpi("Postos",          IconChar.Chair,          out _kpiPostos);
            c1.Margin = new Padding(0, 0, 4, 8);
            c2.Margin = new Padding(4, 0, 4, 8);
            c3.Margin = new Padding(4, 0, 0, 8);
            row.Controls.Add(c1, 0, 0); row.Controls.Add(c2, 1, 0); row.Controls.Add(c3, 2, 0);
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

        // ── Content (3 sub-cards, alternam por visibilidade) ────────────
        private Control BuildContent()
        {
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };

            // Espaços tab content
            var cardEsp = new ModernCard { Dock = DockStyle.Fill, BackColor = Theme.CardBg, BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false };
            var innerEsp = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(8, 8, 8, 8) };
            _espacosList = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.CardBg, Visible = false };
            _espacosList.Resize += (s, e) => ResizeEspacosItems();
            _espacosEmpty = BuildEmptyState("Nenhum espaço definido", IconChar.Building);
            _espacosEmpty.Dock = DockStyle.Fill;
            innerEsp.Controls.Add(_espacosList);
            innerEsp.Controls.Add(_espacosEmpty);
            cardEsp.Controls.Add(innerEsp);
            cardEsp.Name = "tabEspacos";
            cardEsp.Dock = DockStyle.Fill;

            // Salas tab content
            var cardSalas = BuildGridCardOuter(out _salasHost, out _salasGrid, out _salasEmpty,
                                                "Nenhuma sala definida", IconChar.DoorClosed);
            cardSalas.Name = "tabSalas";
            _salasHost.SizeChanged += (s, e) => RebuildSalasGrid();

            // Postos tab content
            var cardPostos = BuildGridCardOuter(out _postosHost, out _postosGrid, out _postosEmpty,
                                                 "Nenhum posto definido", IconChar.Chair);
            cardPostos.Name = "tabPostos";
            _postosHost.SizeChanged += (s, e) => RebuildPostosGrid();

            wrapper.Controls.Add(cardEsp);
            wrapper.Controls.Add(cardSalas);
            wrapper.Controls.Add(cardPostos);
            return wrapper;
        }

        private Control BuildGridCardOuter(out Panel host, out TableLayoutPanel grid, out Panel empty,
                                            string emptyText, IconChar emptyIcon)
        {
            var card = new ModernCard { Dock = DockStyle.Fill, BackColor = Theme.CardBg, BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(8, 8, 8, 8) };
            host = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.CardBg, Visible = false };
            grid = new TableLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.CardBg, Dock = DockStyle.Top,
            };
            host.Controls.Add(grid);
            empty = BuildEmptyState(emptyText, emptyIcon);
            empty.Dock = DockStyle.Fill;
            inner.Controls.Add(host);
            inner.Controls.Add(empty);
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
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
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

        // ── Tab switching ───────────────────────────────────────────────
        private void SwitchTab(Tab t)
        {
            _active = t;
            _tabEspacos.Active = (t == Tab.Espacos);
            _tabSalas.Active   = (t == Tab.Salas);
            _tabPostos.Active  = (t == Tab.Postos);
            _btnNovo.Text = t == Tab.Espacos ? "+ Novo Espaço"
                          : t == Tab.Salas    ? "+ Nova Sala"
                                              : "+ Novo Posto";
            foreach (Control c in ((TableLayoutPanel)Controls[0]).Controls)
            {
                if (c is TableLayoutPanel) continue;  // skip stats/tabs rows
                // wrapper panel: alterna visibilidade dos 3 cards
                foreach (Control card in c.Controls)
                {
                    if (card.Name == "tabEspacos") card.Visible = (t == Tab.Espacos);
                    if (card.Name == "tabSalas")   card.Visible = (t == Tab.Salas);
                    if (card.Name == "tabPostos")  card.Visible = (t == Tab.Postos);
                }
            }
        }

        private void OnNovoClick()
        {
            switch (_active)
            {
                case Tab.Espacos: OpenEspacoEditor(null); break;
                case Tab.Salas:   OpenSalaEditor(null);   break;
                case Tab.Postos:  OpenPostoEditor(null);  break;
            }
        }

        // ── Data loading ────────────────────────────────────────────────
        private void LoadAll()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM espaco", conn))
                        _kpiEspacos.Text = cmd.ExecuteScalar().ToString();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM sala", conn))
                        _kpiSalas.Text = cmd.ExecuteScalar().ToString();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM posto", conn))
                        _kpiPostos.Text = cmd.ExecuteScalar().ToString();

                    LoadEspacos(conn);
                    LoadSalas(conn);
                    LoadPostos(conn);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── ESPAÇOS list ────────────────────────────────────────────────
        private void LoadEspacos(SqlConnection conn)
        {
            _espacosList.Controls.Clear();
            int y = 0, count = 0;
            using (var cmd = new SqlCommand(
                @"SELECT e.espaco_id, e.nome, e.morada, e.telefone, e.email,
                         e.hora_abertura, e.hora_fecho,
                         (SELECT COUNT(*) FROM sala  WHERE espaco_id = e.espaco_id) AS num_salas,
                         (SELECT COUNT(*) FROM posto WHERE espaco_id = e.espaco_id) AS num_postos
                  FROM espaco e ORDER BY e.nome", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var card = BuildEspacoCard(
                        id:        Convert.ToInt32(r["espaco_id"]),
                        nome:      r["nome"].ToString(),
                        morada:    r["morada"].ToString(),
                        telefone:  r["telefone"] is DBNull ? null : r["telefone"].ToString(),
                        email:     r["email"] is DBNull ? null : r["email"].ToString(),
                        abertura:  (TimeSpan)r["hora_abertura"],
                        fecho:     (TimeSpan)r["hora_fecho"],
                        numSalas:  Convert.ToInt32(r["num_salas"]),
                        numPostos: Convert.ToInt32(r["num_postos"])
                    );
                    card.Location = new Point(0, y);
                    card.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    card.Width    = _espacosList.ClientSize.Width;
                    _espacosList.Controls.Add(card);
                    y += card.Height + 6;
                    count++;
                }
            }
            _espacosList.Visible = count > 0;
            _espacosEmpty.Visible = count == 0;
            _espacosEmpty.Invalidate();
            ResizeEspacosItems();
        }

        private void ResizeEspacosItems()
        {
            if (_espacosList == null) return;
            int w = _espacosList.ClientSize.Width;
            if (w < 100) return;
            foreach (Control c in _espacosList.Controls) c.Width = w;
        }

        private Control BuildEspacoCard(int id, string nome, string morada, string telefone, string email,
                                         TimeSpan abertura, TimeSpan fecho, int numSalas, int numPostos)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = MixColors(Theme.CardBg, Color.White, 0.05f);

            var row = new Panel { Height = 96, Margin = new Padding(0, 0, 0, 6), BackColor = idleBg, Cursor = Cursors.Hand };

            // Só ícone Building branco — sem circle/fundo. Pintado no Paint
            // para o HookHover (PaintAll de BackColor) não interferir.
            var avatarHolder = new Panel { Dock = DockStyle.Left, Width = 72, BackColor = idleBg };
            Image buildingImg = null;
            const int iconSz = 36;
            using (var pb = new IconPictureBox
                   { IconChar = IconChar.Building, IconSize = iconSz, IconColor = Color.White })
                if (pb.Image != null) buildingImg = (Image)pb.Image.Clone();
            avatarHolder.Paint += (s, e) =>
            {
                if (buildingImg == null) return;
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                int x = (avatarHolder.Width  - iconSz) / 2;
                int y = (avatarHolder.Height - iconSz) / 2;
                g.DrawImage(buildingImg, x, y, iconSz, iconSz);
            };
            avatarHolder.Resize += (s, e) => avatarHolder.Invalidate();

            // Actions
            var actions = new Panel { Dock = DockStyle.Right, Width = 100, BackColor = idleBg };
            var btnEdit = MakeIconBtn(IconChar.Pen, Theme.Accent, idleBg, () => OpenEspacoEditor(id));
            var btnDel  = MakeIconBtn(IconChar.TrashCan, Theme.StatusDangerFg, idleBg, () => DeleteEspaco(id, nome));
            btnEdit.Location = new Point(8, 28);
            btnDel.Location  = new Point(52, 28);
            actions.Controls.Add(btnEdit);
            actions.Controls.Add(btnDel);

            // Stats (numSalas/numPostos)
            var stats = new Panel { Dock = DockStyle.Right, Width = 180, BackColor = idleBg, Padding = new Padding(0, 24, 12, 0) };
            stats.Controls.Add(new Label
            {
                Text = $"{numSalas} sala" + (numSalas == 1 ? "" : "s") + $" · {numPostos} posto" + (numPostos == 1 ? "" : "s"),
                Font = Theme.FontBold, ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            });
            stats.Controls.Add(new Label
            {
                Text = $"{abertura:hh\\:mm} — {fecho:hh\\:mm}",
                Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            });

            // Text (nome + morada + contactos)
            var pnlText = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(8, 18, 0, 0) };
            var lblNome = new Label
            {
                Text = nome, Font = new Font(Theme.FontBase.FontFamily, 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 26, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblMorada = new Label
            {
                Text = morada, Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 18, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var contactos = new List<string>();
            if (!string.IsNullOrEmpty(telefone)) contactos.Add(telefone);
            if (!string.IsNullOrEmpty(email))    contactos.Add(email);
            var lblContactos = new Label
            {
                Text = contactos.Count > 0 ? string.Join("  ·  ", contactos) : "—",
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 18, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            pnlText.Controls.Add(lblContactos);
            pnlText.Controls.Add(lblMorada);
            pnlText.Controls.Add(lblNome);

            row.Controls.Add(pnlText);
            row.Controls.Add(stats);
            row.Controls.Add(actions);
            row.Controls.Add(avatarHolder);

            HookHover(row, idleBg, hoverBg, btnEdit, btnDel);
            HookClick(row, btnEdit, btnDel, () => OpenEspacoDetail(id, nome, morada, telefone, email, abertura, fecho, numSalas, numPostos));
            return row;
        }

        // ── SALAS grid ──────────────────────────────────────────────────
        private bool _rebuildingSalas;
        private void LoadSalas(SqlConnection conn)
        {
            _salasCards.Clear();
            using (var cmd = new SqlCommand(
                @"SELECT s.recurso_id, e.nome AS espaco, s.nome, s.capacidade,
                         s.preco_hora, s.estado
                  FROM sala s JOIN espaco e ON s.espaco_id = e.espaco_id
                  ORDER BY e.nome, s.nome", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    _salasCards.Add(BuildSalaCard(
                        id:       Convert.ToInt32(r["recurso_id"]),
                        espaco:   r["espaco"].ToString(),
                        nome:     r["nome"].ToString(),
                        capacidade: Convert.ToInt32(r["capacidade"]),
                        preco:    Convert.ToDecimal(r["preco_hora"]),
                        estado:   r["estado"].ToString()
                    ));
                }
            }
            _salasHost.Visible = _salasCards.Count > 0;
            _salasEmpty.Visible = _salasCards.Count == 0;
            _salasEmpty.Invalidate();
            RebuildSalasGrid();
        }

        private void RebuildSalasGrid()
        {
            if (_rebuildingSalas || _salasGrid == null || _salasHost == null) return;
            int avail = _salasHost.ClientSize.Width;
            if (avail < SmallCardW + CardGap) return;
            _rebuildingSalas = true;
            try
            {
                _salasGrid.SuspendLayout();
                _salasGrid.Controls.Clear();
                _salasGrid.RowStyles.Clear();
                _salasGrid.ColumnStyles.Clear();
                int cols = Math.Max(1, (avail - CardGap) / (SmallCardW + CardGap));
                _salasGrid.ColumnCount = cols;
                for (int c = 0; c < cols; c++)
                    _salasGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SmallCardW + CardGap));
                int rows = (_salasCards.Count + cols - 1) / cols;
                _salasGrid.RowCount = Math.Max(1, rows);
                for (int r = 0; r < _salasGrid.RowCount; r++)
                    _salasGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, SmallCardH + CardGap));
                for (int i = 0; i < _salasCards.Count; i++)
                {
                    var card = _salasCards[i];
                    card.Margin = new Padding(0, 0, CardGap, CardGap);
                    _salasGrid.Controls.Add(card, i % cols, i / cols);
                }
                _salasGrid.ResumeLayout(true);
            }
            finally { _rebuildingSalas = false; }
        }

        private Control BuildSalaCard(int id, string espaco, string nome, int capacidade, decimal preco, string estado)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = MixColors(Theme.CardBg, Color.White, 0.05f);
            var card = new ModernCard
            {
                Width = SmallCardW, Height = SmallCardH,
                BackColor = idleBg, BorderColor = Theme.CardBorder,
                CornerRadius = 14, ShowShadow = false, Cursor = Cursors.Hand,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(22, 18, 22, 16) };

            var estadoPill = new StatusPill
            {
                Text = estado, Dock = DockStyle.Top,
                Height = 22, BackColor = idleBg,
                Style = StatusPill.PillStyle.Dot,
                Font = Theme.FontSub,
            };
            estadoPill.SetColors(EstadoBg(estado), EstadoFg(estado));
            var spAfterPill = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = idleBg };

            var lblNome = new Label
            {
                Text = nome, Font = new Font(Theme.FontBase.FontFamily, 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 28, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var spNome = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = idleBg };
            var lblEspaco = new Label
            {
                Text = espaco, Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var spDur = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = idleBg };
            var lblPreco = new Label
            {
                Text = Theme.FormatEuro(preco) + " /hora",
                Font = new Font(Theme.FontBase.FontFamily, 18f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 34, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var spPrice = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = idleBg };
            var lblCap = new Label
            {
                Text = $"{capacidade} lugar" + (capacidade == 1 ? "" : "es"),
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = idleBg };
            var btnEdit = MakeIconBtn(IconChar.Pen, Theme.Accent, idleBg, () => OpenSalaEditor(id));
            var btnDel  = MakeIconBtn(IconChar.TrashCan, Theme.StatusDangerFg, idleBg, () => DeleteSala(id, nome));
            btnEdit.Dock = DockStyle.Right;
            btnDel.Dock  = DockStyle.Right;
            footer.Controls.Add(btnEdit);  // adicionado primeiro = mais à esquerda dos Right
            footer.Controls.Add(btnDel);   // adicionado depois = rightmost

            inner.Controls.Add(footer);
            inner.Controls.Add(lblCap);
            inner.Controls.Add(spPrice);
            inner.Controls.Add(lblPreco);
            inner.Controls.Add(spDur);
            inner.Controls.Add(lblEspaco);
            inner.Controls.Add(spNome);
            inner.Controls.Add(lblNome);
            inner.Controls.Add(spAfterPill);
            inner.Controls.Add(estadoPill);
            card.Controls.Add(inner);

            HookHover(card, idleBg, hoverBg, btnEdit, btnDel);
            HookClick(card, btnEdit, btnDel, () => OpenSalaDetail(id, nome, espaco, capacidade, preco, estado));
            return card;
        }

        // ── POSTOS grid ─────────────────────────────────────────────────
        private bool _rebuildingPostos;
        private void LoadPostos(SqlConnection conn)
        {
            _postosCards.Clear();
            using (var cmd = new SqlCommand(
                @"SELECT p.recurso_id, e.nome AS espaco, p.codigo, p.tipo_posto,
                         p.preco_dia, p.estado
                  FROM posto p JOIN espaco e ON p.espaco_id = e.espaco_id
                  ORDER BY e.nome, p.codigo", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    _postosCards.Add(BuildPostoCard(
                        id:     Convert.ToInt32(r["recurso_id"]),
                        espaco: r["espaco"].ToString(),
                        codigo: r["codigo"].ToString(),
                        tipo:   r["tipo_posto"].ToString(),
                        preco:  Convert.ToDecimal(r["preco_dia"]),
                        estado: r["estado"].ToString()
                    ));
                }
            }
            _postosHost.Visible = _postosCards.Count > 0;
            _postosEmpty.Visible = _postosCards.Count == 0;
            _postosEmpty.Invalidate();
            RebuildPostosGrid();
        }

        private void RebuildPostosGrid()
        {
            if (_rebuildingPostos || _postosGrid == null || _postosHost == null) return;
            int avail = _postosHost.ClientSize.Width;
            if (avail < SmallCardW + CardGap) return;
            _rebuildingPostos = true;
            try
            {
                _postosGrid.SuspendLayout();
                _postosGrid.Controls.Clear();
                _postosGrid.RowStyles.Clear();
                _postosGrid.ColumnStyles.Clear();
                int cols = Math.Max(1, (avail - CardGap) / (SmallCardW + CardGap));
                _postosGrid.ColumnCount = cols;
                for (int c = 0; c < cols; c++)
                    _postosGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SmallCardW + CardGap));
                int rows = (_postosCards.Count + cols - 1) / cols;
                _postosGrid.RowCount = Math.Max(1, rows);
                for (int r = 0; r < _postosGrid.RowCount; r++)
                    _postosGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, SmallCardH + CardGap));
                for (int i = 0; i < _postosCards.Count; i++)
                {
                    var card = _postosCards[i];
                    card.Margin = new Padding(0, 0, CardGap, CardGap);
                    _postosGrid.Controls.Add(card, i % cols, i / cols);
                }
                _postosGrid.ResumeLayout(true);
            }
            finally { _rebuildingPostos = false; }
        }

        private Control BuildPostoCard(int id, string espaco, string codigo, string tipo, decimal preco, string estado)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = MixColors(Theme.CardBg, Color.White, 0.05f);
            var card = new ModernCard
            {
                Width = SmallCardW, Height = SmallCardH,
                BackColor = idleBg, BorderColor = Theme.CardBorder,
                CornerRadius = 14, ShowShadow = false, Cursor = Cursors.Hand,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(22, 18, 22, 16) };

            // Tipo badge colorido + Estado pill
            Color cyan   = ColorTranslator.FromHtml("#06b6d4");
            Color indigo = Theme.Accent;
            Color violet = ColorTranslator.FromHtml("#8b5cf6");
            Color tipoColor = tipo == "Flex" ? cyan : tipo == "Fixo" ? indigo : violet;

            // Header: tipo + estado lado-a-lado.
            var tipoPill = new StatusPill
            {
                Text = tipo, Height = 22, BackColor = idleBg,
                Style = StatusPill.PillStyle.Dot, Font = Theme.FontSub,
            };
            tipoPill.SetColors(Color.FromArgb(40, tipoColor), tipoColor);
            var estadoPill = new StatusPill
            {
                Text = estado, Height = 22, BackColor = idleBg,
                Style = StatusPill.PillStyle.Dot, Font = Theme.FontSub,
            };
            estadoPill.SetColors(EstadoBg(estado), EstadoFg(estado));

            // Widths dinâmicos pelo texto (dot 8 + gap 8 + texto + 4 margem).
            int tipoW   = StatusPill.MeasureDotWidth(tipo,   Theme.FontSub);
            int estadoW = StatusPill.MeasureDotWidth(estado, Theme.FontSub);
            tipoPill.Dock   = DockStyle.Left; tipoPill.Width   = tipoW;
            var spBetween   = new Panel { Dock = DockStyle.Left, Width = 16, BackColor = idleBg };
            estadoPill.Dock = DockStyle.Left; estadoPill.Width = estadoW;

            var headerRow = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = idleBg };
            headerRow.Controls.Add(estadoPill); // primeiro adicionado = leftmost dos Dock=Left
            headerRow.Controls.Add(spBetween);
            headerRow.Controls.Add(tipoPill);

            var spAfterHeader = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = idleBg };

            var lblCodigo = new Label
            {
                Text = codigo, Font = new Font(Theme.FontBase.FontFamily, 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 28, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var spNome = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = idleBg };
            var lblEspaco = new Label
            {
                Text = espaco, Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var spDur = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = idleBg };
            var lblPreco = new Label
            {
                Text = Theme.FormatEuro(preco) + " /dia",
                Font = new Font(Theme.FontBase.FontFamily, 18f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 34, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = idleBg };
            var btnEdit = MakeIconBtn(IconChar.Pen, Theme.Accent, idleBg, () => OpenPostoEditor(id));
            var btnDel  = MakeIconBtn(IconChar.TrashCan, Theme.StatusDangerFg, idleBg, () => DeletePosto(id, codigo));
            btnEdit.Dock = DockStyle.Right;
            btnDel.Dock  = DockStyle.Right;
            footer.Controls.Add(btnEdit);  // primeiro = à esquerda dos Right
            footer.Controls.Add(btnDel);   // depois = rightmost

            inner.Controls.Add(footer);
            inner.Controls.Add(lblPreco);
            inner.Controls.Add(spDur);
            inner.Controls.Add(lblEspaco);
            inner.Controls.Add(spNome);
            inner.Controls.Add(lblCodigo);
            inner.Controls.Add(spAfterHeader);
            inner.Controls.Add(headerRow);
            card.Controls.Add(inner);

            HookHover(card, idleBg, hoverBg, btnEdit, btnDel);
            HookClick(card, btnEdit, btnDel, () => OpenPostoDetail(id, codigo, espaco, tipo, preco, estado));
            return card;
        }

        // ── Detail helpers (read-only modals) ──────────────────────────
        private Panel BuildDetailHeader(IconChar icon, Color color, string title, string subtitle)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Theme.CardBg };
            Image img = null;
            using (var pb = new IconPictureBox { IconChar = icon, IconSize = 26, IconColor = Color.White })
                if (pb.Image != null) img = (Image)pb.Image.Clone();
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                int diam = 56;
                int cx = 0, cy = (pnl.Height - diam) / 2;
                using (var br = new SolidBrush(color)) g.FillEllipse(br, cx, cy, diam, diam);
                if (img != null) g.DrawImage(img, cx + (diam - 26) / 2, cy + (diam - 26) / 2 + 1, 26, 26);
                using (var f = new Font(Theme.FontBase.FontFamily, 16f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, title, f, new Point(diam + 14, cy + 4),
                        Theme.TextPrimary, TextFormatFlags.NoPadding);
                }
                if (!string.IsNullOrEmpty(subtitle))
                {
                    TextRenderer.DrawText(g, subtitle, Theme.FontSub, new Point(diam + 14, cy + 34),
                        Theme.TextSecondary, TextFormatFlags.NoPadding);
                }
            };
            return pnl;
        }

        private static Panel BuildDetailField(string label, string value)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Theme.CardBg, Padding = new Padding(0, 8, 0, 0) };
            pnl.Controls.Add(new Label
            {
                Text = value, Font = Theme.FontBase, ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            pnl.Controls.Add(new Label
            {
                Text = label.ToUpper(), Font = Theme.FontMicro, ForeColor = Theme.TextMuted, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = 16, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        private void OpenEspacoDetail(int id, string nome, string morada, string telefone, string email,
                                       TimeSpan abertura, TimeSpan fecho, int numSalas, int numPostos)
        {
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            body.Controls.Add(BuildDetailField("Postos",   numPostos.ToString()));
            body.Controls.Add(BuildDetailField("Salas",    numSalas.ToString()));
            body.Controls.Add(BuildDetailField("Horário",  $"{abertura:hh\\:mm} – {fecho:hh\\:mm}"));
            body.Controls.Add(BuildDetailField("Email",    string.IsNullOrEmpty(email) ? "—" : email));
            body.Controls.Add(BuildDetailField("Telefone", string.IsNullOrEmpty(telefone) ? "—" : telefone));
            body.Controls.Add(BuildDetailField("Morada",   morada));
            body.Controls.Add(BuildDetailHeader(IconChar.Building, Theme.Accent, nome, null));
            using (var dlg = new FormDialog($"Espaço — {nome}", body, 480, onSave: null))
                dlg.ShowDialog(FindForm());
        }

        private void OpenSalaDetail(int id, string nome, string espaco, int capacidade, decimal preco, string estado)
        {
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            body.Controls.Add(BuildDetailField("Estado",     estado));
            body.Controls.Add(BuildDetailField("Preço/Hora", Theme.FormatEuro(preco)));
            body.Controls.Add(BuildDetailField("Capacidade", capacidade + (capacidade == 1 ? " lugar" : " lugares")));
            body.Controls.Add(BuildDetailField("Espaço",     espaco));
            body.Controls.Add(BuildDetailHeader(IconChar.DoorClosed, Theme.Accent, nome, "Sala"));
            using (var dlg = new FormDialog($"Sala — {nome}", body, 480, onSave: null))
                dlg.ShowDialog(FindForm());
        }

        private void OpenPostoDetail(int id, string codigo, string espaco, string tipo, decimal preco, string estado)
        {
            Color tipoColor = tipo == "Flex" ? ColorTranslator.FromHtml("#06b6d4")
                            : tipo == "Fixo" ? Theme.Accent
                            : ColorTranslator.FromHtml("#8b5cf6");
            IconChar tipoIcon = tipo == "Flex" ? IconChar.PersonRunning
                              : tipo == "Fixo" ? IconChar.Chair
                              : IconChar.DoorClosed;
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            body.Controls.Add(BuildDetailField("Estado",    estado));
            body.Controls.Add(BuildDetailField("Preço/Dia", Theme.FormatEuro(preco)));
            body.Controls.Add(BuildDetailField("Tipo",      tipo));
            body.Controls.Add(BuildDetailField("Espaço",    espaco));
            body.Controls.Add(BuildDetailHeader(tipoIcon, tipoColor, codigo, "Posto " + tipo));
            using (var dlg = new FormDialog($"Posto — {codigo}", body, 480, onSave: null))
                dlg.ShowDialog(FindForm());
        }

        // ── Helpers ─────────────────────────────────────────────────────
        public static Color MixColors(Color a, Color b, float t)
            => Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));

        private static Color EstadoBg(string estado)
        {
            switch (estado)
            {
                case "Disponivel":   return Theme.StatusSuccessBg;
                case "Indisponivel": return Theme.StatusWarningBg;
                case "Manutencao":   return Theme.StatusOrangeBg;
                case "Inativo":      return Theme.StatusNeutralBg;
                default:              return Theme.StatusNeutralBg;
            }
        }
        private static Color EstadoFg(string estado)
        {
            switch (estado)
            {
                case "Disponivel":   return Theme.StatusSuccessFg;
                case "Indisponivel": return Theme.StatusWarningFg;
                case "Manutencao":   return Theme.StatusOrangeFg;
                case "Inativo":      return Theme.StatusNeutralFg;
                default:              return Theme.StatusNeutralFg;
            }
        }

        public static IconButton MakeIconBtn(IconChar icon, Color hoverColor, Color bg, Action onClick)
        {
            var btn = new IconButton
            {
                IconChar = icon, IconSize = 16, IconColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Theme.TextSecondary,
                Size = new Size(36, 36), Cursor = Cursors.Hand, TabStop = false,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = bg;
            btn.MouseEnter += (s, e) => btn.IconColor = hoverColor;
            btn.MouseLeave += (s, e) => btn.IconColor = Theme.TextSecondary;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        public static void HookHover(Control root, Color idleBg, Color hoverBg,
                                       IconButton btnEdit, IconButton btnDel)
        {
            void PaintAll(Control c, Color bg) { c.BackColor = bg; foreach (Control x in c.Controls) PaintAll(x, bg); }
            void SetH(bool on)
            {
                Color bg = on ? hoverBg : idleBg;
                if (root is ModernCard mc) mc.BackColor = bg;
                else root.BackColor = bg;
                PaintAll(root, bg);
                btnEdit.FlatAppearance.MouseOverBackColor = bg;
                btnDel .FlatAppearance.MouseOverBackColor = bg;
                if (root is ModernCard) root.Invalidate();
            }
            void Hook(Control c)
            {
                c.MouseEnter += (s, e) => SetH(true);
                c.MouseLeave += (s, e) =>
                {
                    var p = root.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!root.ClientRectangle.Contains(p)) SetH(false);
                };
                foreach (Control x in c.Controls) Hook(x);
            }
            Hook(root);
        }

        public static void HookClick(Control root, Control btnEdit, Control btnDel, Action onClick)
        {
            void Hook(Control c)
            {
                if (c == btnEdit || c == btnDel) return;
                c.Click += (s, e) => onClick();
                foreach (Control x in c.Controls) Hook(x);
            }
            Hook(root);
        }

        // ── Editors (preservados, apenas refactor cosmético) ────────────
        private void OpenEspacoEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var txtNome     = UcClientes.AddField(tbl, "Nome *",     IconChar.Building,  placeholder: "Coworking Aveiro Centro");
            var txtMorada   = UcClientes.AddField(tbl, "Morada *",   IconChar.LocationDot, placeholder: "Rua Direita 100, Aveiro");
            var txtTelefone = UcClientes.AddField(tbl, "Telefone",   IconChar.Phone,     placeholder: "234 111 222");
            var txtEmail    = UcClientes.AddField(tbl, "Email",      IconChar.Envelope,  placeholder: "aveiro@cowork.pt");
            var txtAbertura = UcClientes.AddField(tbl, "Hora abertura (HH:MM) *", IconChar.Sun, placeholder: "08:00");
            var txtFecho    = UcClientes.AddField(tbl, "Hora fecho (HH:MM) *",    IconChar.Moon,      placeholder: "20:00");
            txtAbertura.Text = "08:00";
            txtFecho.Text    = "20:00";

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT nome, morada, telefone, email, hora_abertura, hora_fecho FROM espaco WHERE espaco_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtNome.Text = r["nome"]?.ToString() ?? "";
                            txtMorada.Text = r["morada"]?.ToString() ?? "";
                            txtTelefone.Text = r["telefone"] is DBNull ? "" : r["telefone"].ToString();
                            txtEmail.Text = r["email"] is DBNull ? "" : r["email"].ToString();
                            txtAbertura.Text = ((TimeSpan)r["hora_abertura"]).ToString(@"hh\:mm");
                            txtFecho.Text = ((TimeSpan)r["hora_fecho"]).ToString(@"hh\:mm");
                        }
                    }
                }
            }
            using (var dlg = new FormDialog(id.HasValue ? "Editar Espaço" : "Novo Espaço", tbl, 500, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (string.IsNullOrWhiteSpace(txtMorada.Text)) throw new ApplicationException("Morada é obrigatória.");
                if (!TimeSpan.TryParse(txtAbertura.Text.Trim(), out TimeSpan abertura)) throw new ApplicationException("Hora abertura inválida (HH:MM).");
                if (!TimeSpan.TryParse(txtFecho.Text.Trim(), out TimeSpan fecho)) throw new ApplicationException("Hora fecho inválida (HH:MM).");
                if (fecho <= abertura) throw new ApplicationException("Hora fecho deve ser depois de hora abertura.");

                var sql = id.HasValue
                    ? "UPDATE espaco SET nome=@n, morada=@m, telefone=@t, email=@e, hora_abertura=@ha, hora_fecho=@hf WHERE espaco_id=@id"
                    : "INSERT INTO espaco (nome, morada, telefone, email, hora_abertura, hora_fecho) VALUES (@n,@m,@t,@e,@ha,@hf)";
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@m", txtMorada.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", string.IsNullOrWhiteSpace(txtTelefone.Text) ? (object)DBNull.Value : txtTelefone.Text.Trim());
                    cmd.Parameters.AddWithValue("@e", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@ha", abertura);
                    cmd.Parameters.AddWithValue("@hf", fecho);
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadAll();
            }
        }

        private void DeleteEspaco(int id, string nome)
        {
            if (MessageBox.Show($"Eliminar o espaço \"{nome}\"?", "Confirmar",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM espaco WHERE espaco_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LoadAll();
            }
            catch (SqlException ex) { MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private DataTable LoadEspacosForCombo()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand("SELECT espaco_id, nome FROM espaco ORDER BY nome", conn))
            using (var ad = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable(); ad.Fill(dt); return dt;
            }
        }

        private void OpenSalaEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var cmbEspaco = UcClientes.AddModernSelectDataSource(tbl, "Espaço *", LoadEspacosForCombo(), "nome", "espaco_id");
            var txtNome   = UcClientes.AddField(tbl, "Nome *",       IconChar.DoorClosed, placeholder: "Sala A");
            var txtCap    = UcClientes.AddField(tbl, "Capacidade *", IconChar.Users,      placeholder: "8");
            var txtPreco  = UcClientes.AddField(tbl, "Preço/Hora *", IconChar.EuroSign,   placeholder: "15.00");
            var cmbEstado = UcClientes.AddModernSelect(tbl, "Estado *", new[] { "Disponivel", "Indisponivel", "Manutencao", "Inativo" });

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT espaco_id, nome, capacidade, preco_hora, estado FROM sala WHERE recurso_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            cmbEspaco.SelectedValue = r["espaco_id"];
                            cmbEspaco.Enabled = false;
                            txtNome.Text = r["nome"]?.ToString() ?? "";
                            txtCap.Text = r["capacidade"]?.ToString() ?? "";
                            txtPreco.Text = Convert.ToDecimal(r["preco_hora"]).ToString(CultureInfo.InvariantCulture);
                            cmbEstado.SelectByDisplay(r["estado"]?.ToString() ?? "Disponivel");
                        }
                    }
                }
            }
            using (var dlg = new FormDialog(id.HasValue ? "Editar Sala" : "Nova Sala", tbl, 500, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (!int.TryParse(txtCap.Text, out int cap) || cap <= 0) throw new ApplicationException("Capacidade inválida (> 0).");
                if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0) throw new ApplicationException("Preço inválido.");
                if (cmbEspaco.SelectedValue == null) throw new ApplicationException("Espaço é obrigatório.");

                using (var conn = Database.GetConnection())
                {
                    if (id.HasValue)
                    {
                        using (var cmd = new SqlCommand("UPDATE sala SET nome=@n, capacidade=@c, preco_hora=@p, estado=@e WHERE recurso_id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id.Value);
                            cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                            cmd.Parameters.AddWithValue("@c", cap);
                            cmd.Parameters.AddWithValue("@p", preco);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedText);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        int newRid;
                        using (var ins = new SqlCommand("INSERT INTO recurso (tipo) VALUES ('Sala'); SELECT SCOPE_IDENTITY()", conn))
                            newRid = Convert.ToInt32(ins.ExecuteScalar());
                        using (var cmd = new SqlCommand("INSERT INTO sala (recurso_id, espaco_id, nome, capacidade, preco_hora, estado) VALUES (@rid,@eid,@n,@c,@p,@e)", conn))
                        {
                            cmd.Parameters.AddWithValue("@rid", newRid);
                            cmd.Parameters.AddWithValue("@eid", Convert.ToInt32(cmbEspaco.SelectedValue));
                            cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                            cmd.Parameters.AddWithValue("@c", cap);
                            cmd.Parameters.AddWithValue("@p", preco);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedText);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadAll();
            }
        }

        private void DeleteSala(int id, string nome)
        {
            if (MessageBox.Show($"Eliminar a sala \"{nome}\"?", "Confirmar",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM reserva WHERE recurso_id=@id AND estado NOT IN ('Cancelada','Concluida')", conn))
                    {
                        chk.Parameters.AddWithValue("@id", id);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Sala tem reservas activas.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM recurso WHERE recurso_id=@id", conn))
                    { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
                }
                LoadAll();
            }
            catch (SqlException ex) { MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void OpenPostoEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var cmbEspaco = UcClientes.AddModernSelectDataSource(tbl, "Espaço *", LoadEspacosForCombo(), "nome", "espaco_id");
            var txtCodigo = UcClientes.AddField(tbl, "Código *",    IconChar.Hashtag,  placeholder: "AV-F01");
            var cmbTipo   = UcClientes.AddModernSelect(tbl, "Tipo *", new[] { "Flex", "Fixo", "Privado" });
            var txtPreco  = UcClientes.AddField(tbl, "Preço/Dia *", IconChar.EuroSign, placeholder: "12.00");
            var cmbEstado = UcClientes.AddModernSelect(tbl, "Estado *", new[] { "Disponivel", "Indisponivel", "Manutencao", "Inativo" });

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT espaco_id, codigo, tipo_posto, preco_dia, estado FROM posto WHERE recurso_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            cmbEspaco.SelectedValue = r["espaco_id"];
                            cmbEspaco.Enabled = false;
                            txtCodigo.Text = r["codigo"]?.ToString() ?? "";
                            cmbTipo.SelectByDisplay(r["tipo_posto"]?.ToString() ?? "Flex");
                            txtPreco.Text = Convert.ToDecimal(r["preco_dia"]).ToString(CultureInfo.InvariantCulture);
                            cmbEstado.SelectByDisplay(r["estado"]?.ToString() ?? "Disponivel");
                        }
                    }
                }
            }
            using (var dlg = new FormDialog(id.HasValue ? "Editar Posto" : "Novo Posto", tbl, 500, () =>
            {
                if (string.IsNullOrWhiteSpace(txtCodigo.Text)) throw new ApplicationException("Código é obrigatório.");
                if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0) throw new ApplicationException("Preço inválido.");
                if (cmbEspaco.SelectedValue == null) throw new ApplicationException("Espaço é obrigatório.");

                using (var conn = Database.GetConnection())
                {
                    if (id.HasValue)
                    {
                        using (var cmd = new SqlCommand("UPDATE posto SET codigo=@c, tipo_posto=@t, preco_dia=@p, estado=@e WHERE recurso_id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id.Value);
                            cmd.Parameters.AddWithValue("@c", txtCodigo.Text.Trim());
                            cmd.Parameters.AddWithValue("@t", cmbTipo.SelectedText);
                            cmd.Parameters.AddWithValue("@p", preco);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedText);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        int newRid;
                        using (var ins = new SqlCommand("INSERT INTO recurso (tipo) VALUES ('Posto'); SELECT SCOPE_IDENTITY()", conn))
                            newRid = Convert.ToInt32(ins.ExecuteScalar());
                        using (var cmd = new SqlCommand("INSERT INTO posto (recurso_id, espaco_id, codigo, tipo_posto, preco_dia, estado) VALUES (@rid,@eid,@c,@t,@p,@e)", conn))
                        {
                            cmd.Parameters.AddWithValue("@rid", newRid);
                            cmd.Parameters.AddWithValue("@eid", Convert.ToInt32(cmbEspaco.SelectedValue));
                            cmd.Parameters.AddWithValue("@c", txtCodigo.Text.Trim());
                            cmd.Parameters.AddWithValue("@t", cmbTipo.SelectedText);
                            cmd.Parameters.AddWithValue("@p", preco);
                            cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedText);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadAll();
            }
        }

        private void DeletePosto(int id, string codigo)
        {
            if (MessageBox.Show($"Eliminar o posto \"{codigo}\"?", "Confirmar",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM reserva WHERE recurso_id=@id AND estado NOT IN ('Cancelada','Concluida')", conn))
                    {
                        chk.Parameters.AddWithValue("@id", id);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Posto tem reservas activas.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM recurso WHERE recurso_id=@id", conn))
                    { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
                }
                LoadAll();
            }
            catch (SqlException ex) { MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── TabButton (pill button para tab bar) ────────────────────────
    }
}
