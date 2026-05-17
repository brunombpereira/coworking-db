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
        private IconChar _icon = IconChar.None;
        public IconChar Icon
        {
            get => _icon;
            set { _icon = value; RecalcSize(); Invalidate(); }
        }
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
            Height    = 40;
            Font      = new Font(Theme.FontBase.FontFamily, 10f, FontStyle.Bold);
            Cursor    = Cursors.Hand;
            BackColor = Color.Transparent;
            RecalcSize();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            RecalcSize();
            Invalidate();
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            RecalcSize();
        }

        /// <summary>Width = padding + icon (se houver) + texto + padding direita.</summary>
        private void RecalcSize()
        {
            int textW;
            using (var bmp = new System.Drawing.Bitmap(1, 1))
            using (var g   = System.Drawing.Graphics.FromImage(bmp))
                textW = (int)Math.Ceiling(g.MeasureString(Text ?? "", Font).Width);

            int padL    = 14;
            int padR    = 14;
            int iconGap = (_icon != IconChar.None) ? 22 /*16 + 6 gap*/ : 0;
            Width = padL + iconGap + textW + padR;
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

            // Hover background subtle (excepto na active — já tem accent forte).
            if (_hover && !_active)
            {
                var bgRect = new Rectangle(2, 2, Width - 4, Height - 6);
                using (var path = ModernCard.RoundedRect(bgRect, 8))
                using (var br   = new SolidBrush(Theme.AccentSoft))
                    g.FillPath(br, path);
            }

            Color fg = _active ? Theme.Accent
                     : _hover  ? Theme.Accent
                               : Theme.TextSecondary;

            // Conteúdo centrado horizontalmente dentro do botão.
            int textW;
            using (var bmp = new System.Drawing.Bitmap(1, 1))
            using (var g2  = System.Drawing.Graphics.FromImage(bmp))
                textW = (int)Math.Ceiling(g2.MeasureString(Text ?? "", Font).Width);
            int iconBlock = (Icon != IconChar.None) ? 22 : 0;
            int contentW  = iconBlock + textW;
            int startX    = (Width - contentW) / 2;
            int iconY     = (Height - 4 - 16) / 2;

            if (Icon != IconChar.None)
            {
                using (var pb = new IconPictureBox { IconChar = Icon, IconSize = 16, IconColor = fg })
                    if (pb.Image != null)
                        g.DrawImage(pb.Image, startX, iconY, 16, 16);
            }
            var textRect = new Rectangle(startX + iconBlock, 0, textW + 4, Height - 4);
            TextRenderer.DrawText(g, Text, Font, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            // Underline accent — 2px arredondado (era 3px square).
            if (_active)
            {
                int underlineH = 2;
                int underlineW = Math.Max(40, contentW);
                int underlineX = (Width - underlineW) / 2;
                var rect = new Rectangle(underlineX, Height - underlineH, underlineW, underlineH);
                using (var path = ModernCard.RoundedRect(rect, 1))
                using (var br   = new SolidBrush(Theme.Accent))
                    g.FillPath(br, path);
            }
        }
    }
}
