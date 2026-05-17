using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Panel com cantos arredondados, opcional border subtil e soft shadow
    /// externa (multi-pass com alpha baixo). Tudo opaco para evitar problemas
    /// de transparência do WinForms.
    /// </summary>
    public class ModernCard : Panel
    {
        public int    CornerRadius { get; set; } = 12;
        public Color  BorderColor  { get; set; } = Color.Empty;  // Empty = sem border
        public float  BorderWidth  { get; set; } = 1f;
        public bool   ShowShadow   { get; set; } = true;
        public int    ShadowSpread { get; set; } = 12;

        public ModernCard()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.CardBg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Limpa com a cor do parent (para a shadow não acumular entre paints)
            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            // Área do card (inset para shadow caber)
            int s = ShowShadow ? ShadowSpread : 0;
            var cardRect = new Rectangle(s, s, Width - s * 2 - 1, Height - s * 2 - 1);

            // Soft shadow — multi-pass com alpha decrescente
            if (ShowShadow)
            {
                for (int i = ShadowSpread; i >= 1; i--)
                {
                    var shadowRect = cardRect;
                    shadowRect.Inflate(i, i);
                    int alpha = (int)(30.0 * (1.0 - (double)i / ShadowSpread) + 4);
                    using (var path = RoundedRect(shadowRect, CornerRadius + i))
                    using (var pen  = new Pen(Color.FromArgb(alpha, 0, 0, 0), 1.4f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Fill + (opcional) border
            using (var path = RoundedRect(cardRect, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);

                if (BorderColor != Color.Empty)
                {
                    using (var pen = new Pen(BorderColor, BorderWidth))
                        g.DrawPath(pen, path);
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(r); return path; }
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
