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
            public Color    IconColor;
            public Action   OnClick;
            public bool     IsSeparator;
        }

        private readonly List<MenuItemDef> _items;

        public ProfilePopup(List<MenuItemDef> items)
        {
            _items = items;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.Manual;
            ShowInTaskbar   = false;
            BackColor       = Theme.CardBg;
            DoubleBuffered  = true;
            TopMost         = true;
            Padding         = new Padding(6);
            Width           = 220;

            // Altura calculada (cada item 36, separador 12)
            int h = Padding.Vertical;
            foreach (var i in items) h += i.IsSeparator ? 12 : 36;
            Height = h;

            BuildItems();
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Border subtil à volta do popup
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var pen = new Pen(Theme.CardBorder, 1))
                e.Graphics.DrawRectangle(pen, rect);
        }

        private void BuildItems()
        {
            int y = Padding.Top;

            foreach (var def in _items)
            {
                if (def.IsSeparator)
                {
                    var sep = new Panel
                    {
                        Location  = new Point(Padding.Left + 6, y + 5),
                        Size      = new Size(Width - Padding.Horizontal - 12, 1),
                        BackColor = Theme.CardBorder,
                    };
                    Controls.Add(sep);
                    y += 12;
                    continue;
                }

                var item = BuildItem(def, y);
                Controls.Add(item);
                y += 36;
            }
        }

        private Control BuildItem(MenuItemDef def, int y)
        {
            var item = new ItemControl
            {
                Location      = new Point(Padding.Left, y),
                Size          = new Size(Width - Padding.Horizontal, 36),
                IconChar      = def.Icon,
                IconColor     = def.IconColor,
                Text          = def.Text,
                ForeColor     = Theme.TextPrimary,
                HoverColor    = Theme.SidebarBgActive,
                BackColorIdle = Theme.CardBg,
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
            public IconChar IconChar      { get; set; }
            public Color    IconColor     { get; set; }
            public Color    HoverColor    { get; set; }
            public Color    BackColorIdle { get; set; }

            private bool _hover;
            private static readonly Dictionary<(IconChar, int, Color), Image> _iconCache
                = new Dictionary<(IconChar, int, Color), Image>();

            public ItemControl()
            {
                SetStyle(ControlStyles.OptimizedDoubleBuffer
                       | ControlStyles.AllPaintingInWmPaint
                       | ControlStyles.ResizeRedraw
                       | ControlStyles.UserPaint, true);
                Cursor = Cursors.Hand;
                Font   = new Font("Segoe UI", 9.5f);
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Background com cantos arredondados em hover
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

                // Icon à esquerda
                int iconSize = 16;
                int iconX    = 12;
                int iconY    = (Height - iconSize) / 2;
                var img      = GetIcon(IconChar, iconSize, IconColor);
                if (img != null) g.DrawImage(img, iconX, iconY, iconSize, iconSize);

                // Texto
                int textX = iconX + iconSize + 12;
                var textRect = new Rectangle(textX, 0, Width - textX - 8, Height);
                TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            private static Image GetIcon(IconChar c, int size, Color color)
            {
                var key = (c, size, color);
                if (_iconCache.TryGetValue(key, out var cached)) return cached;

                using (var pb = new IconPictureBox
                       {
                           IconChar  = c,
                           IconSize  = size,
                           IconColor = color,
                           Size      = new Size(size, size),
                           BackColor = Color.Transparent,
                           SizeMode  = PictureBoxSizeMode.AutoSize,
                       })
                {
                    var bmp = new Bitmap(size, size);
                    bmp.MakeTransparent();
                    pb.DrawToBitmap(bmp, new Rectangle(0, 0, size, size));
                    _iconCache[key] = bmp;
                    return bmp;
                }
            }
        }
    }
}
