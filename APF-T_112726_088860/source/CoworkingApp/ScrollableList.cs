using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Panel scrollable verticalmente com scrollbar custom (track dark + thumb
    /// rounded), sem scrollbar nativa. Items são adicionados a Content (Panel
    /// interno). Chamar UpdateLayout(contentHeight) após adicionar items.
    /// </summary>
    public class ScrollableList : Panel
    {
        public Panel Content { get; }

        private int _scrollY = 0;
        private const int ScrollbarW = 6;
        private const int TrackPad   = 4;

        private bool _draggingThumb;
        private int  _dragOffsetY;

        public ScrollableList()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.Selectable, true);
            TabStop = true;

            Content = new Panel { Location = new Point(0, 0), BackColor = BackColor };
            Controls.Add(Content);

            Resize += (s, e) => Relayout();
        }

        /// <summary>Chama após adicionar/remover items em Content. contentHeight
        /// é a altura total dos items (somatório).</summary>
        public void UpdateLayout(int contentHeight)
        {
            Content.Height = contentHeight;
            Relayout();
            Invalidate();
        }

        private bool NeedsScroll() => Content.Height > ClientSize.Height;
        private int  MaxScroll()    => Math.Max(0, Content.Height - ClientSize.Height);

        private void Relayout()
        {
            int w = ClientSize.Width - (NeedsScroll() ? ScrollbarW + TrackPad * 2 : 0);
            Content.Width = Math.Max(1, w);
            _scrollY = Math.Min(_scrollY, MaxScroll());
            Content.Location = new Point(0, -_scrollY);
        }

        protected override void OnMouseEnter(EventArgs e) { Focus(); base.OnMouseEnter(e); }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (!NeedsScroll()) { base.OnMouseWheel(e); return; }
            int delta = -e.Delta / 2; // sensitivity
            _scrollY  = Math.Max(0, Math.Min(MaxScroll(), _scrollY + delta));
            Content.Location = new Point(0, -_scrollY);
            Invalidate();
            base.OnMouseWheel(e);
        }

        // ── Scrollbar drag ─────────────────────────────────────────────
        private Rectangle ThumbRect()
        {
            int trackX = ClientSize.Width - ScrollbarW - TrackPad;
            int trackY = TrackPad;
            int trackH = ClientSize.Height - TrackPad * 2;
            float ratio  = (float)ClientSize.Height / Content.Height;
            int   thumbH = Math.Max(24, (int)(trackH * ratio));
            int   thumbY = trackY + (int)((trackH - thumbH) * ((float)_scrollY / Math.Max(1, MaxScroll())));
            return new Rectangle(trackX, thumbY, ScrollbarW, thumbH);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (NeedsScroll() && ThumbRect().Contains(e.Location))
            {
                _draggingThumb = true;
                _dragOffsetY   = e.Y - ThumbRect().Y;
                Capture        = true;
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _draggingThumb = false;
            Capture        = false;
            base.OnMouseUp(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_draggingThumb && NeedsScroll())
            {
                int trackY = TrackPad;
                int trackH = ClientSize.Height - TrackPad * 2;
                int thumbH = ThumbRect().Height;
                int y      = e.Y - _dragOffsetY - trackY;
                float frac = (float)y / (trackH - thumbH);
                frac = Math.Max(0, Math.Min(1, frac));
                _scrollY = (int)(MaxScroll() * frac);
                Content.Location = new Point(0, -_scrollY);
                Invalidate();
            }
            else if (NeedsScroll())
            {
                Cursor = ThumbRect().Contains(e.Location) ? Cursors.Hand : Cursors.Default;
            }
            base.OnMouseMove(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!NeedsScroll()) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int trackX = ClientSize.Width - ScrollbarW - TrackPad;
            int trackY = TrackPad;
            int trackH = ClientSize.Height - TrackPad * 2;
            using (var br = new SolidBrush(Color.FromArgb(20, Theme.TextSecondary)))
                g.FillRectangle(br, trackX, trackY, ScrollbarW, trackH);

            var thumb = ThumbRect();
            using (var path = ModernCard.RoundedRect(thumb, ScrollbarW / 2))
            using (var br   = new SolidBrush(_draggingThumb ? Theme.Accent : Theme.TextMuted))
                g.FillPath(br, path);
        }
    }
}
