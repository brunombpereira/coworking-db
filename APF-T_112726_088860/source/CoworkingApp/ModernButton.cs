using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Botão com cantos arredondados, cor sólida, hover/pressed states discretos.
    /// Sem gradients, sem glow — sóbrio e profissional.
    /// </summary>
    public class ModernButton : Button
    {
        public enum Variant { Primary, Secondary, Danger }

        public int     CornerRadius { get; set; } = 8;
        public Variant Style        { get; set; } = Variant.Primary;

        private bool _hover;
        private bool _pressed;

        public ModernButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);

            FlatStyle              = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor              = Color.Transparent;
            Font                   = Theme.FontBold;
            Cursor                 = Cursors.Hand;
            Height                 = 40;
            UseCompatibleTextRendering = false;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown (MouseEventArgs e) { _pressed = true;  Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp   (MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e);   }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Background do parent
            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            (Color fill, Color text, Color border) = ResolveColors();

            using (var path = ModernCard.RoundedRect(rect, CornerRadius))
            using (var brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
                if (border != Color.Empty)
                {
                    using (var pen = new Pen(border, 1f))
                        g.DrawPath(pen, path);
                }
            }

            TextRenderer.DrawText(g, Text, Font, rect, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
              | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        private (Color fill, Color text, Color border) ResolveColors()
        {
            switch (Style)
            {
                case Variant.Primary:
                    {
                        Color baseColor = Theme.Accent;
                        Color hover     = Theme.AccentHover;
                        Color pressed   = Darker(hover, 0.12f);
                        Color fill      = _pressed ? pressed : (_hover ? hover : baseColor);
                        return (fill, Color.White, Color.Empty);
                    }
                case Variant.Danger:
                    {
                        Color baseColor = Theme.BtnDangerBg;
                        Color hover     = Lighter(baseColor, 0.10f);
                        Color pressed   = Darker(baseColor, 0.12f);
                        Color fill      = _pressed ? pressed : (_hover ? hover : baseColor);
                        return (fill, Color.White, Color.Empty);
                    }
                case Variant.Secondary:
                default:
                    {
                        Color baseColor = _hover ? Lighter(Theme.CardBg, 0.06f) : Theme.CardBg;
                        if (_pressed) baseColor = Darker(Theme.CardBg, 0.05f);
                        return (baseColor, Theme.TextPrimary, Theme.CardBorder);
                    }
            }
        }

        private static Color Lighter(Color c, float f)
            => Color.FromArgb(c.A,
                Clamp((int)(c.R + (255 - c.R) * f)),
                Clamp((int)(c.G + (255 - c.G) * f)),
                Clamp((int)(c.B + (255 - c.B) * f)));

        private static Color Darker(Color c, float f)
            => Color.FromArgb(c.A,
                Clamp((int)(c.R * (1 - f))),
                Clamp((int)(c.G * (1 - f))),
                Clamp((int)(c.B * (1 - f))));

        private static int Clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);
    }
}
