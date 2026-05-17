using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    /// <summary>
    /// Container Panel com TextBox embedded. Cantos arredondados, border
    /// neutro em idle, accent indigo subtil em focus. Suporta opcionalmente
    /// um ícone clicável à direita (ex.: toggle eye no password).
    /// </summary>
    public class ModernInput : Panel
    {
        private readonly TextBox        _inner;
        private          IconPictureBox _trailing;

        public int   CornerRadius     { get; set; } = 8;
        public Color BorderColorIdle  { get; set; } = Theme.CardBorder;
        public Color BorderColorFocus { get; set; } = Theme.Accent;

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
        public string PlaceholderText
        {
            get => _inner.PlaceholderText;
            set => _inner.PlaceholderText = value;
        }
        public TextBox Inner => _inner;

        public new event EventHandler TextChanged
        {
            add    => _inner.TextChanged += value;
            remove => _inner.TextChanged -= value;
        }

        /// <summary>Ícone clicável à direita (ex.: olho para show/hide password).</summary>
        public event EventHandler TrailingIconClicked;

        /// <summary>Define o ícone trailing. Null/None remove-o.</summary>
        public IconChar TrailingIcon
        {
            get => _trailing?.IconChar ?? IconChar.None;
            set
            {
                if (value == IconChar.None)
                {
                    if (_trailing != null) { Controls.Remove(_trailing); _trailing.Dispose(); _trailing = null; }
                    return;
                }
                if (_trailing == null)
                {
                    _trailing = new IconPictureBox
                    {
                        IconSize  = 22,                    // antes 16 → blur por subpixel-AA pequeno
                        IconColor = Theme.TextSecondary,
                        BackColor = Theme.FieldBg,
                        Size      = new Size(34, 26),
                        Dock      = DockStyle.Right,
                        Cursor    = Cursors.Hand,
                        SizeMode  = PictureBoxSizeMode.CenterImage,
                    };
                    _trailing.Click      += (s, e) => TrailingIconClicked?.Invoke(this, EventArgs.Empty);
                    _trailing.MouseEnter += (s, e) => { _trailing.IconColor = Theme.TextPrimary; };
                    _trailing.MouseLeave += (s, e) => { _trailing.IconColor = Theme.TextSecondary; };
                    Controls.Add(_trailing);
                    _trailing.BringToFront();
                }
                _trailing.IconChar = value;
            }
        }

        public ModernInput()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw, true);

            BackColor = Theme.FieldBg;
            Padding   = new Padding(14, 10, 14, 10);
            Height    = 42;

            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor   = Theme.FieldBg,
                ForeColor   = Theme.TextPrimary,
                Font        = Theme.FontBase,
                Dock        = DockStyle.Fill,
            };
            _inner.GotFocus  += (s, e) => Invalidate();
            _inner.LostFocus += (s, e) => Invalidate();
            Controls.Add(_inner);
        }

        public new bool Focus() => _inner.Focus();

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

            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            bool focused = _inner.Focused;

            using (var path  = ModernCard.RoundedRect(rect, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);

                Color borderColor = focused ? BorderColorFocus : BorderColorIdle;
                float w           = focused ? 1.6f : 1f;
                using (var pen = new Pen(borderColor, w))
                    g.DrawPath(pen, path);
            }
        }
    }
}
