using System;
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

        /// <summary>Quando true, o pill desenha apenas com a largura do texto +
        /// padding e fica left-aligned dentro do controlo. Útil quando o
        /// StatusPill é colocado com Dock=Top (que estica a largura) mas
        /// queremos um pill compacto, em vez de full-width.</summary>
        public bool AutoWidthFromText { get; set; } = false;

        /// <summary>Padding horizontal interno do pill (à volta do texto).</summary>
        public int HorizontalPadding { get; set; } = 12;

        public enum PillStyle { Filled, Dot }
        /// <summary>Filled = pill com bg + texto. Dot = pequeno círculo na cor
        /// _pillFg + texto na cor _pillFg (estilo Linear/Vercel, sem fundo).</summary>
        public PillStyle Style { get; set; } = PillStyle.Filled;

        /// <summary>Largura necessária para Dot pill com determinado texto/font.
        /// Mede com NoPadding + buffer generoso (TextRenderer.MeasureText
        /// pode subestimar ligeiramente vs DrawText).</summary>
        public static int MeasureDotWidth(string text, Font font)
        {
            // Sem NoPadding para garantir margem suficiente — preferimos
            // pill um pouco mais largo a texto cortado.
            int textW = TextRenderer.MeasureText(text ?? "", font).Width;
            return 8 /*dot*/ + 8 /*gap*/ + textW + 12 /*right buffer*/;
        }

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

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
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

            // Dot style: pequeno círculo + texto, sem fundo.
            if (Style == PillStyle.Dot)
            {
                int dotSize = 8;
                int dotX    = 0;
                int dotY    = (Height - dotSize) / 2;
                using (var br = new SolidBrush(_pillFg))
                    g.FillEllipse(br, dotX, dotY, dotSize, dotSize);

                int textX = dotSize + 8;
                var textRect = new Rectangle(textX, 0, Width - textX, Height);
                TextRenderer.DrawText(g, Text, Font, textRect, _pillFg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                  | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                return;
            }

            // Pill — altura 22 centrada
            int pillHeight = 22;
            int pillY      = (Height - pillHeight) / 2;

            Rectangle pillRect;
            if (AutoWidthFromText)
            {
                // Largura = texto + padding horizontal, left-aligned (gap 0 da borda esquerda).
                var ts = TextRenderer.MeasureText(g, Text ?? string.Empty, Font, Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                int pillW = Math.Min(Width, ts.Width + HorizontalPadding * 2);
                pillRect  = new Rectangle(0, pillY, pillW, pillHeight);
            }
            else
            {
                int margin = 8;  // gap horizontal do edge
                pillRect   = new Rectangle(margin, pillY, Width - margin * 2, pillHeight);
            }
            int radius = pillHeight / 2;  // pill perfeita

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
