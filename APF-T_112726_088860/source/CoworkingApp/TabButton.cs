using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    /// <summary>
    /// Tab button estilo Linear/GitHub: texto + ícone opcional + underline
    /// accent na tab activa. Sem fundo (transparent), só cor de texto muda.
    /// </summary>
    public class TabButton : Control
    {
        public IconChar Icon { get; set; } = IconChar.None;
        public bool Active   { get => _active; set { _active = value; Invalidate(); } }
        private bool _active;
        private bool _hover;

        public TabButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint
                   | ControlStyles.SupportsTransparentBackColor, true);
            Size      = new Size(130, 38);
            Font      = new Font(Theme.FontBase.FontFamily, 10f, FontStyle.Bold);
            Cursor    = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            if (Parent != null)
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);

            Color fg = _active ? Theme.Accent
                     : _hover  ? Theme.TextPrimary
                               : Theme.TextSecondary;

            int iconLeft = 4;
            int textLeft = iconLeft;
            int contentY = (Height - 4 - 16) / 2;
            if (Icon != IconChar.None)
            {
                using (var pb = new IconPictureBox { IconChar = Icon, IconSize = 16, IconColor = fg })
                {
                    if (pb.Image != null)
                        g.DrawImage(pb.Image, iconLeft, contentY, 16, 16);
                }
                textLeft = iconLeft + 22;
            }
            var textRect = new Rectangle(textLeft, 0, Width - textLeft - 4, Height - 4);
            TextRenderer.DrawText(g, Text, Font, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            if (_active)
            {
                using (var brush = new SolidBrush(Theme.Accent))
                    g.FillRectangle(brush, 0, Height - 3, Width, 3);
            }
        }
    }
}
