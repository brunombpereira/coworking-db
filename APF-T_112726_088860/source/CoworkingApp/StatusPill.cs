using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Label-like control com cantos arredondados (pill). Usado para indicar
    /// estados em listas (Confirmada, Pendente, Pago, Cancelada, etc.).
    /// </summary>
    public class StatusPill : Control
    {
        private Color _pillBg = Theme.StatusNeutralBg;
        private Color _pillFg = Theme.StatusNeutralFg;

        public StatusPill()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);
            Font   = Theme.FontMicro;
            Height = 24;
        }

        public void SetColors(Color background, Color foreground)
        {
            _pillBg = background;
            _pillFg = foreground;
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Limpa com a cor do parent (suporta hover do row, que muda Parent.BackColor)
            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            // Pill — altura 22 centrada, largura dinâmica conforme texto
            int pillHeight = 22;
            int pillY      = (Height - pillHeight) / 2;
            int margin     = 8;  // gap horizontal do edge

            var pillRect = new Rectangle(margin, pillY, Width - margin * 2, pillHeight);
            int radius   = pillHeight / 2;  // pill perfeita

            using (var path = NeonRound(pillRect, radius))
            using (var brush = new SolidBrush(_pillBg))
                g.FillPath(brush, path);

            // Texto centrado
            TextRenderer.DrawText(g, Text, Font, pillRect, _pillFg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
              | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        private static GraphicsPath NeonRound(Rectangle r, int radius)
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
