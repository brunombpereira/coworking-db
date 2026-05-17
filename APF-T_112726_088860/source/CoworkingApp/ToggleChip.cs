using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Chip toggleable (on/off) com pill rounded. ON = bg AccentSoft + dot
    /// accent + texto accent. OFF = bg FieldBg + dot neutral + texto secondary.
    /// </summary>
    public class ToggleChip : Control
    {
        private bool _checked = false;
        private bool _hover;

        public int CornerRadius { get; set; } = 16;

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler CheckedChanged;

        public ToggleChip()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);
            BackColor = Theme.FieldBg;
            ForeColor = Theme.TextPrimary;
            Font      = Theme.FontBold;
            Height    = 36;
            Width     = 130;
            Cursor    = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e)      { Checked = !Checked; base.OnClick(e); }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color bg2 = _checked ? Theme.AccentSoft : Theme.FieldBg;
            Color border = _checked ? Theme.Accent : (_hover ? Theme.Accent : Theme.CardBorder);
            float bw = _checked ? 1.4f : 1f;

            using (var path  = ModernCard.RoundedRect(rect, CornerRadius))
            using (var brush = new SolidBrush(bg2))
            using (var pen   = new Pen(border, bw))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // Dot indicator à esquerda
            int dotSize = 8;
            int dotX    = 12;
            int dotY    = (Height - dotSize) / 2;
            using (var br = new SolidBrush(_checked ? Theme.Accent : Theme.TextMuted))
                g.FillEllipse(br, dotX, dotY, dotSize, dotSize);

            // Texto
            int textX = dotX + dotSize + 8;
            var textRect = new Rectangle(textX, 0, Width - textX - 12, Height);
            Color fg = _checked ? Theme.Accent : Theme.TextSecondary;
            TextRenderer.DrawText(g, Text, Font, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
              | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }
}
