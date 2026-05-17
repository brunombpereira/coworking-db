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
        private          IconChar       _leadingIcon = IconChar.None;
        private          Image          _leadingImage;
        private const    int            LeadingIconSize    = 18;
        private const    int            LeadingIconLeftPad = 14;
        private const    int            LeadingIconGap     = 6;

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

        /// <summary>Ícone à esquerda (decorativo — ex.: lupa de pesquisa).
        /// Pintado manualmente em OnPaint para evitar os offsets internos do
        /// IconPictureBox + SizeMode.CenterImage, que faziam o glyph FA
        /// aparecer demasiado em cima.</summary>
        public IconChar LeadingIcon
        {
            get => _leadingIcon;
            set
            {
                if (_leadingIcon == value) return;
                _leadingIcon = value;
                _leadingImage?.Dispose();
                _leadingImage = null;
                if (value != IconChar.None)
                {
                    using (var pb = new IconPictureBox
                           { IconChar = value, IconSize = LeadingIconSize, IconColor = Theme.TextMuted })
                    {
                        if (pb.Image != null) _leadingImage = (Image)pb.Image.Clone();
                    }
                }
                // Reserva espaço à esquerda do TextBox para o icon não sobrepor.
                int leftPad = (value == IconChar.None)
                    ? LeadingIconLeftPad
                    : LeadingIconLeftPad + LeadingIconSize + LeadingIconGap;
                Padding = new Padding(leftPad, Padding.Top, Padding.Right, Padding.Bottom);
                Invalidate();
            }
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
            // ESC desfaz o focus (e limpa o texto se houver) — útil em
            // campos de pesquisa para descartar a query.
            _inner.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    _inner.Text = string.Empty;
                    // Mover focus para o parent → o LostFocus dispara e o
                    // border de focus desaparece.
                    Form host = FindForm();
                    if (host != null) host.ActiveControl = null;
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
            };
            Controls.Add(_inner);
        }

        public new bool Focus() => _inner.Focus();

        protected override void OnClick(EventArgs e)
        {
            _inner.Focus();
            base.OnClick(e);
        }

        // Re-centra verticalmente o TextBox interno sempre que o Size muda.
        // O TextBox renderiza o texto no topo do seu Dock=Fill area, por
        // isso para visualmente centrar o texto no container precisamos de
        // padding-top calculado a partir da font height.
        private bool _recentering;
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RecenterText();
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            RecenterText();
        }
        private void RecenterText()
        {
            if (_recentering || _inner == null || Height < 16) return;
            _recentering = true;
            try
            {
                int fontH    = TextRenderer.MeasureText("Hg", Font).Height;
                // Top padding = (Height − fontH)/2 → centra o texto verticalmente
                // (TextBox renderiza text top-aligned, então padding-top empurra
                // o TextBox para baixo até o texto ficar no centro).
                int topPad   = Math.Max(2, (Height - fontH) / 2);
                // Bottom padding tem de deixar pelo menos 16px de client area
                // para o LeadingIcon/TrailingIcon (16px) não ficar clipado.
                const int iconMinH = 16;
                int clientH  = Math.Max(fontH, iconMinH);
                int botPad   = Math.Max(2, Height - clientH - topPad);
                Padding = new Padding(Padding.Left, topPad, Padding.Right, botPad);
            }
            finally { _recentering = false; }
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

            // Leading icon — pintado manualmente para controlo total de posição.
            if (_leadingImage != null)
            {
                int x = LeadingIconLeftPad;
                int y = (Height - LeadingIconSize) / 2 + 1;  // +1 compensa padding interno do glyph FA
                g.DrawImage(_leadingImage, x, y, LeadingIconSize, LeadingIconSize);
            }
        }
    }
}
