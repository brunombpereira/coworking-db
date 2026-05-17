using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Botão arredondado com fill gradient cyan→magenta e glow externo
    /// que intensifica em hover. Mantém comportamento de Button standard
    /// (IsDefault, AcceptButton, eventos de Click).
    /// </summary>
    public class NeonButton : Button
    {
        public int   CornerRadius { get; set; } = NeonStyle.RadiusMd;
        public Color Color1       { get; set; } = NeonStyle.NeonCyan;
        public Color Color2       { get; set; } = NeonStyle.NeonMagenta;
        public int   GlowSpread   { get; set; } = NeonStyle.GlowSpread;

        private bool _hover;
        private bool _pressed;

        public NeonButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint
                   | ControlStyles.SupportsTransparentBackColor, true);

            FlatStyle              = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor              = Color.Transparent;
            ForeColor              = NeonStyle.TextPrimary;
            Font                   = NeonStyle.FontButton;
            Cursor                 = Cursors.Hand;
            Height                 = 42;
            UseCompatibleTextRendering = false;
        }

        protected override void OnMouseEnter(System.EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(System.EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown (MouseEventArgs e)   { _pressed = true;  Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp   (MouseEventArgs e)   { _pressed = false; Invalidate(); base.OnMouseUp(e);   }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode   = PixelOffsetMode.HighQuality;

            // Background do parent (para o glow não acumular entre paints)
            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            // Área do botão (inset para glow)
            var rect = new Rectangle(GlowSpread, GlowSpread,
                                     Width  - GlowSpread * 2 - 1,
                                     Height - GlowSpread * 2 - 1);

            // Glow externo — mais intenso em hover
            int basePasses = _hover ? NeonStyle.GlowPasses : NeonStyle.GlowPasses - 2;
            int baseAlpha  = _hover ? 110 : 50;
            for (int i = basePasses; i >= 1; i--)
            {
                var glowRect = rect;
                int expand = (GlowSpread * i) / NeonStyle.GlowPasses;
                glowRect.Inflate(expand, expand);
                int alpha = (int)(baseAlpha * (1.0 - (double)i / basePasses) + 15);
                using (var path = NeonPanel.RoundedRect(glowRect, CornerRadius + expand))
                using (var pen  = new Pen(NeonStyle.WithAlpha(Color1, alpha), 1.4f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Fill: gradient cyan→magenta. Em pressed escurece ligeiramente.
            using (var path = NeonPanel.RoundedRect(rect, CornerRadius))
            {
                Color c1 = _pressed ? DarkerBy(Color1, 0.15f) : Color1;
                Color c2 = _pressed ? DarkerBy(Color2, 0.15f) : Color2;
                using (var brush = new LinearGradientBrush(rect, c1, c2, 30f))
                {
                    g.FillPath(brush, path);
                }
                // Sobre-imposição subtil para textura "vidro"
                using (var sheen = new LinearGradientBrush(rect,
                           Color.FromArgb(_hover ? 70 : 40, Color.White),
                           Color.FromArgb(0, Color.White), 90f))
                {
                    g.FillPath(sheen, path);
                }
            }

            // Texto centrado
            TextRenderer.DrawText(g, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
              | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        private static Color DarkerBy(Color c, float factor)
        {
            return Color.FromArgb(c.A,
                (int)(c.R * (1 - factor)),
                (int)(c.G * (1 - factor)),
                (int)(c.B * (1 - factor)));
        }
    }
}
