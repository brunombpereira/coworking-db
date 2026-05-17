using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    /// <summary>
    /// Popup borderless que substitui ContextMenuStrip — estilo modern dark
    /// com cantos arredondados, sombra subtil e items hover/click consistentes.
    /// Fecha-se ao perder foco (clicar fora) ou ao escolher um item.
    /// </summary>
    public class ProfilePopup : Form
    {
        public class MenuItemDef
        {
            public string   Text;
            public IconChar Icon;
            public Action   OnClick;
            public bool     IsSeparator;
            public bool     IsDanger;       // tinta ícone+texto em vermelho
        }

        private readonly List<MenuItemDef> _items;

        public ProfilePopup(List<MenuItemDef> items)
        {
            _items = items;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.Manual;
            ShowInTaskbar   = false;
            BackColor       = Theme.SidebarBg;          // match sidebar — visual continuity
            DoubleBuffered  = true;
            TopMost         = true;
            Padding         = new Padding(6, 8, 6, 8);
            Width           = 230;

            // Altura calculada (cada item 40, separador 14)
            int h = Padding.Vertical;
            foreach (var i in items) h += i.IsSeparator ? 14 : 40;
            Height = h;

            BuildItems();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Cantos arredondados via Region (corners 10px). Edges ligeiramente
            // serrilhados mas o popup é pequeno e o raio também — aceitável.
            using (var path = ModernCard.RoundedRect(new Rectangle(0, 0, Width, Height), 10))
            {
                Region = new Region(path);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();  // fecha ao perder foco (clique fora)
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;  // CS_DROPSHADOW (sombra leve do SO)
                return cp;
            }
        }

        // Sem OnPaint override — BackColor + Region tratam de tudo. Nada de
        // border (a sombra do SO via CS_DROPSHADOW já separa do que está atrás).

        private void BuildItems()
        {
            int y = Padding.Top;

            foreach (var def in _items)
            {
                if (def.IsSeparator)
                {
                    var sep = new Panel
                    {
                        Location  = new Point(Padding.Left + 8, y + 6),
                        Size      = new Size(Width - Padding.Horizontal - 16, 1),
                        BackColor = Color.FromArgb(28, Color.White),
                    };
                    Controls.Add(sep);
                    y += 14;
                    continue;
                }

                var item = BuildItem(def, y);
                Controls.Add(item);
                y += 40;
            }
        }

        private Control BuildItem(MenuItemDef def, int y)
        {
            // Danger (Sair) mantém-se vermelho idle e hover; outros items
            // usam SidebarText idle e SidebarTextActive hover — alinhamento
            // 1:1 com os nav items da sidebar.
            Color danger    = Theme.StatusDangerFg;
            Color iconIdle  = def.IsDanger ? danger : Theme.SidebarText;
            Color iconHover = def.IsDanger ? danger : Theme.SidebarTextActive;
            Color textIdle  = def.IsDanger ? danger : Theme.SidebarText;
            Color textHover = def.IsDanger ? danger : Theme.SidebarTextActive;

            var item = new ItemControl
            {
                Location       = new Point(Padding.Left, y),
                Size           = new Size(Width - Padding.Horizontal, 40),
                IconChar       = def.Icon,
                IconColorIdle  = iconIdle,
                IconColorHover = iconHover,
                Text           = def.Text,
                ForeColorIdle  = textIdle,
                ForeColorHover = textHover,
                HoverColor     = Theme.SidebarBgActive,
                BackColorIdle  = Theme.SidebarBg,
            };
            item.Click += (s, e) =>
            {
                Close();
                def.OnClick?.Invoke();
            };
            return item;
        }

        // ── Item interno custom-painted ─────────────────────────────────
        private class ItemControl : Control
        {
            public IconChar IconChar       { get; set; }
            public Color    IconColorIdle  { get; set; }
            public Color    IconColorHover { get; set; }
            public Color    ForeColorIdle  { get; set; }
            public Color    ForeColorHover { get; set; }
            public Color    HoverColor     { get; set; }
            public Color    BackColorIdle  { get; set; }

            private bool _hover;

            public ItemControl()
            {
                SetStyle(ControlStyles.OptimizedDoubleBuffer
                       | ControlStyles.AllPaintingInWmPaint
                       | ControlStyles.ResizeRedraw
                       | ControlStyles.UserPaint, true);
                Cursor = Cursors.Hand;
                // Mesma font que os nav items da sidebar
                Font   = new Font(Theme.FontBase.FontFamily, 9.5f);
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Bg com cantos arredondados em hover
                if (_hover)
                {
                    var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                    using (var path  = ModernCard.RoundedRect(rect, 6))
                    using (var brush = new SolidBrush(HoverColor))
                        g.FillPath(brush, path);
                }
                else
                {
                    using (var brush = new SolidBrush(BackColorIdle))
                        g.FillRectangle(brush, ClientRectangle);
                }

                Color iconColor = _hover ? IconColorHover : IconColorIdle;
                Color textColor = _hover ? ForeColorHover : ForeColorIdle;

                // Icon 18px (igual aos nav items da sidebar)
                const int iconSize = 18;
                int iconX = 14;
                int iconY = (Height - iconSize) / 2;
                var img   = IconRenderer.Render(IconChar, iconSize, iconColor);
                if (img != null) g.DrawImage(img, iconX, iconY, iconSize, iconSize);

                // Texto
                int textX    = iconX + iconSize + 10;
                var textRect = new Rectangle(textX, 0, Width - textX - 8, Height);
                TextRenderer.DrawText(g, Text, Font, textRect, textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        // ── Render de ícones FontAwesome com cache (idle + hover colors) ─
        internal static class IconRenderer
        {
            private static readonly Dictionary<(IconChar, int, int), Image> _cache
                = new Dictionary<(IconChar, int, int), Image>();

            public static Image Render(IconChar c, int size, Color color)
            {
                var key = (c, size, color.ToArgb());
                if (_cache.TryGetValue(key, out var cached)) return cached;

                var bmp = new Bitmap(size, size);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode     = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                    // FontAwesome.Sharp renderiza via IconButton/IconPictureBox.
                    // Recriamos o mesmo render: DrawString do char com a font FA.
                    using (var fa = new IconPictureBox
                           {
                               IconChar  = c,
                               IconSize  = size,
                               IconColor = color,
                               Size      = new Size(size, size),
                               BackColor = Color.Transparent,
                               SizeMode  = PictureBoxSizeMode.AutoSize,
                           })
                    {
                        fa.DrawToBitmap(bmp, new Rectangle(0, 0, size, size));
                    }
                }
                _cache[key] = bmp;
                return bmp;
            }
        }
    }
}
