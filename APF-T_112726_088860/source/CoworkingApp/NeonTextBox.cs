using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// TextBox custom com cantos arredondados e border neon que intensifica
    /// em focus. Internamente, é um Panel com um TextBox child docked Fill,
    /// e OnPaint do Panel desenha o border.
    /// </summary>
    public class NeonTextBox : Panel
    {
        private readonly TextBox _inner;
        private bool _hover;

        public int   CornerRadius     { get; set; } = NeonStyle.RadiusSm;
        public Color BorderColorIdle  { get; set; } = NeonStyle.BorderSubtle;
        public Color BorderColorFocus { get; set; } = NeonStyle.NeonCyan;

        // ── Expor propriedades comuns do TextBox interno ────────────────
        public override string Text
        {
            get => _inner?.Text ?? string.Empty;
            set { if (_inner != null) _inner.Text = value; }
        }
        public bool UseSystemPasswordChar
        {
            get => _inner.UseSystemPasswordChar;
            set => _inner.UseSystemPasswordChar = value;
        }
        public char PasswordChar
        {
            get => _inner.PasswordChar;
            set => _inner.PasswordChar = value;
        }
        public TextBox Inner => _inner;

        public new event EventHandler TextChanged
        {
            add    => _inner.TextChanged += value;
            remove => _inner.TextChanged -= value;
        }

        public NeonTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);

            BackColor = NeonStyle.BgRaised;
            Padding   = new Padding(14, 10, 14, 10);
            Height    = 42;

            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor   = NeonStyle.BgRaised,
                ForeColor   = NeonStyle.TextPrimary,
                Font        = NeonStyle.FontBody,
                Dock        = DockStyle.Fill,
            };
            _inner.GotFocus  += (s, e) => Invalidate();
            _inner.LostFocus += (s, e) => Invalidate();
            Controls.Add(_inner);
        }

        public new bool Focus()                => _inner.Focus();
        public new void Select(int s, int len) => _inner.Select(s, len);

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        // Click no Panel → foco no TextBox interno
        protected override void OnClick(EventArgs e)
        {
            _inner.Focus();
            base.OnClick(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background do parent — limpa zonas fora do path
            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Fill
            using (var path  = NeonPanel.RoundedRect(rect, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);

                // Border — bright se _inner tem focus
                bool focused = _inner.Focused;
                Color borderColor = focused
                    ? BorderColorFocus
                    : (_hover ? NeonStyle.WithAlpha(BorderColorFocus, 80) : BorderColorIdle);
                float w = focused ? 1.6f : 1f;
                using (var pen = new Pen(borderColor, w))
                {
                    g.DrawPath(pen, path);
                }

                // Glow externo subtil quando focado
                if (focused)
                {
                    for (int i = 3; i >= 1; i--)
                    {
                        var glowRect = rect;
                        glowRect.Inflate(i, i);
                        using (var glowPath = NeonPanel.RoundedRect(glowRect, CornerRadius + i))
                        using (var pen = new Pen(NeonStyle.WithAlpha(BorderColorFocus, 40 - i * 10), 1f))
                        {
                            g.DrawPath(pen, glowPath);
                        }
                    }
                }
            }
        }
    }
}
