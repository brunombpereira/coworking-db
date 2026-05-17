using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Segmented control (2+ opções) com bg FieldBg e pill accent no segmento
    /// activo. Útil para filtros binários ou ternários (ex: Só por ler / Todas).
    /// </summary>
    public class SegmentedControl : Control
    {
        public int CornerRadius { get; set; } = 8;

        private string[] _segments = Array.Empty<string>();
        private int _selectedIndex = 0;
        private int _hoverIdx      = -1;

        public event EventHandler SelectedIndexChanged;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string[] Segments
        {
            get => _segments;
            set { _segments = value ?? Array.Empty<string>(); Invalidate(); }
        }

        public SegmentedControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);
            BackColor = Theme.FieldBg;
            ForeColor = Theme.TextPrimary;
            Font      = Theme.FontBold;
            Height    = 36;
            Width     = 200;
            Cursor    = Cursors.Hand;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = SegmentAtX(e.X);
            if (idx != _hoverIdx) { _hoverIdx = idx; Invalidate(); }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { _hoverIdx = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            int idx = SegmentAtX(e.X);
            if (idx >= 0) SelectedIndex = idx;
            base.OnMouseClick(e);
        }

        private int SegmentAtX(int x)
        {
            if (_segments.Length == 0) return -1;
            int segW = Width / _segments.Length;
            int idx  = x / segW;
            if (idx < 0 || idx >= _segments.Length) return -1;
            return idx;
        }

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

            var bgRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = ModernCard.RoundedRect(bgRect, CornerRadius))
            using (var br   = new SolidBrush(BackColor))
            using (var pen  = new Pen(Theme.CardBorder, 1f))
            {
                g.FillPath(br, path);
                g.DrawPath(pen, path);
            }

            if (_segments.Length == 0) return;

            int segW = Width / _segments.Length;
            for (int i = 0; i < _segments.Length; i++)
            {
                bool selected = (i == _selectedIndex);
                bool hover    = (i == _hoverIdx) && !selected;
                var segR = new Rectangle(i * segW + 3, 3, segW - 6, Height - 6);

                if (selected)
                {
                    using (var p = ModernCard.RoundedRect(segR, CornerRadius - 2))
                    using (var b = new SolidBrush(Theme.Accent))
                        g.FillPath(b, p);
                }
                else if (hover)
                {
                    using (var p = ModernCard.RoundedRect(segR, CornerRadius - 2))
                    using (var b = new SolidBrush(Theme.AccentSoft))
                        g.FillPath(b, p);
                }

                Color fg = selected ? Color.White : (hover ? Theme.Accent : Theme.TextSecondary);
                TextRenderer.DrawText(g, _segments[i], Font, segR, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                  | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }
        }
    }
}
