using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Panel com cantos arredondados, border gradient neon e glow externo
    /// opcional. Pintado manualmente em OnPaint para suportar antialiasing.
    /// </summary>
    public class NeonPanel : Panel
    {
        public int    CornerRadius { get; set; } = NeonStyle.RadiusLg;
        public Color  BorderColor1 { get; set; } = NeonStyle.NeonCyan;
        public Color  BorderColor2 { get; set; } = NeonStyle.NeonMagenta;
        public float  BorderWidth  { get; set; } = 1.5f;
        public bool   ShowGlow     { get; set; } = true;
        public int    GlowSpread   { get; set; } = NeonStyle.GlowSpread;

        public NeonPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);
            BackColor = NeonStyle.CardBg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode   = PixelOffsetMode.HighQuality;

            // Limpa toda a área com a cor do parent (para o glow não acumular)
            if (Parent != null)
            {
                using (var bgBrush = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Área do card (inset para o glow caber dentro do control)
            var cardRect = new Rectangle(GlowSpread, GlowSpread,
                                         Width  - GlowSpread * 2 - 1,
                                         Height - GlowSpread * 2 - 1);

            // Glow externo: multi-pass com alpha decrescente
            if (ShowGlow && GlowSpread > 0)
            {
                for (int i = NeonStyle.GlowPasses; i >= 1; i--)
                {
                    var glowRect = cardRect;
                    int expand = (GlowSpread * i) / NeonStyle.GlowPasses;
                    glowRect.Inflate(expand, expand);

                    int alpha = (int)(60.0 * (1.0 - (double)i / NeonStyle.GlowPasses) + 20);
                    using (var path = RoundedRect(glowRect, CornerRadius + expand))
                    using (var pen  = new Pen(NeonStyle.WithAlpha(BorderColor1, alpha), 1.2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Fill do card
            using (var path  = RoundedRect(cardRect, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);

                // Border gradient cyan→magenta
                using (var gradient = new LinearGradientBrush(
                           cardRect, BorderColor1, BorderColor2, 45f))
                using (var pen      = new Pen(gradient, BorderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Não chamamos base.OnPaint — pintamos tudo nós.
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Skip — tratamos do background em OnPaint para evitar flicker.
        }

        /// <summary>Constrói um GraphicsPath de rectângulo com cantos arredondados.</summary>
        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            int d = radius * 2;
            if (d > r.Width)  d = r.Width;
            if (d > r.Height) d = r.Height;

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
