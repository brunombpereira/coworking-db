using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Círculo com a inicial do utilizador no centro. Usado no avatar
    /// do footer da sidebar e no UcPerfil. Pintado em OnPaint para AA.
    /// </summary>
    public class AvatarCircle : Panel
    {
        private string _initial = "?";

        public string Initial
        {
            get => _initial;
            set { _initial = string.IsNullOrEmpty(value) ? "?" : value.Substring(0, 1).ToUpper(); Invalidate(); }
        }

        public Color CircleColor { get; set; } = Theme.Accent;

        public AvatarCircle()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Size      = new Size(32, 32);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            using (var brush = new SolidBrush(CircleColor))
                g.FillEllipse(brush, 0, 0, Width - 1, Height - 1);

            float fontSize = Height * 0.45f;
            using (var font  = new Font("Segoe UI", fontSize, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            using (var sf    = new StringFormat
                   {
                       Alignment     = StringAlignment.Center,
                       LineAlignment = StringAlignment.Center,
                   })
            {
                // -1 no Y compensa o baseline de algumas fontes
                g.DrawString(_initial, font, brush,
                             new RectangleF(0, -1, Width, Height), sf);
            }
        }
    }
}
