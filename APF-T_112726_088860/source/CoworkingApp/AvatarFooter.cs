using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    /// <summary>
    /// Footer da sidebar (avatar circle + username + chevron) como um único
    /// Control. Paint manual evita o hover-flicker que acontece quando há
    /// múltiplos child controls a interceptar MouseEnter/Leave.
    /// </summary>
    public class AvatarFooter : Control
    {
        public string  Username     { get; set; } = "?";
        public Color   AccentColor  { get; set; } = Theme.Accent;
        public new Color BackColor  { get; set; } = Theme.SidebarBg;
        public Color   HoverColor   { get; set; } = Theme.SidebarBgActive;
        public Color   TextColor    { get; set; } = Color.White;
        public Color   ChevronColor { get; set; } = Theme.SidebarText;

        private bool _hover;

        public AvatarFooter()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);
            Cursor = Cursors.Hand;
            Height = 52;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Background (com hover)
            using (var bg = new SolidBrush(_hover ? HoverColor : BackColor))
                g.FillRectangle(bg, ClientRectangle);

            // Divider top (subtil)
            using (var pen = new Pen(Color.FromArgb(28, Color.White), 1))
                g.DrawLine(pen, 0, 0, Width, 0);

            // Avatar circle (32x32) à esquerda
            int avatarSize = 32;
            int avatarX    = 12;
            int avatarY    = (Height - avatarSize) / 2;
            using (var brush = new SolidBrush(AccentColor))
                g.FillEllipse(brush, avatarX, avatarY, avatarSize, avatarSize);

            // Inicial dentro do círculo
            string initial = string.IsNullOrEmpty(Username) ? "?" : Username.Substring(0, 1).ToUpper();
            using (var font  = new Font("Segoe UI", avatarSize * 0.45f, FontStyle.Bold))
            using (var br    = new SolidBrush(Color.White))
            using (var sf    = new StringFormat
                   {
                       Alignment     = StringAlignment.Center,
                       LineAlignment = StringAlignment.Center,
                   })
            {
                g.DrawString(initial, font, br,
                             new RectangleF(avatarX, avatarY - 1, avatarSize, avatarSize), sf);
            }

            // Nome no meio
            int textX = avatarX + avatarSize + 12;
            int textW = Width - textX - 32;
            var textRect = new Rectangle(textX, 0, textW, Height);
            using (var font = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, Username ?? "—", font, textRect, TextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                  | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }

            // Ellipsis vertical à direita (3 pontos)
            int dotX = Width - 18;
            int dotY = Height / 2;
            using (var brush = new SolidBrush(ChevronColor))
            {
                g.FillEllipse(brush, dotX - 1, dotY - 8, 3, 3);
                g.FillEllipse(brush, dotX - 1, dotY - 1, 3, 3);
                g.FillEllipse(brush, dotX - 1, dotY + 6, 3, 3);
            }
        }
    }
}
