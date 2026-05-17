using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Wrapper visual para ComboBox: panel rounded com bg FieldBg, border
    /// CardBorder, ComboBox interno com FlatStyle. Para parecer um chip
    /// modern dentro de filtros/toolbar.
    /// </summary>
    public class ModernCombo : Panel
    {
        private readonly ComboBox _inner;

        public int   CornerRadius     { get; set; } = 8;
        public Color BorderColorIdle  { get; set; } = Theme.CardBorder;
        public Color BorderColorFocus { get; set; } = Theme.Accent;

        public object SelectedValue { get => _inner.SelectedValue; set => _inner.SelectedValue = value; }
        public int    SelectedIndex { get => _inner.SelectedIndex; set => _inner.SelectedIndex = value; }
        public object SelectedItem  { get => _inner.SelectedItem;  set => _inner.SelectedItem  = value; }
        public object DataSource    { get => _inner.DataSource;    set => _inner.DataSource    = value; }
        public string DisplayMember { get => _inner.DisplayMember; set => _inner.DisplayMember = value; }
        public string ValueMember   { get => _inner.ValueMember;   set => _inner.ValueMember   = value; }
        public ComboBox.ObjectCollection Items => _inner.Items;
        public ComboBox Inner => _inner;

        public override string Text
        {
            get => _inner?.Text ?? string.Empty;
            set { if (_inner != null) _inner.Text = value; }
        }

        public new event EventHandler SelectedIndexChanged
        {
            add    { _inner.SelectedIndexChanged += value; }
            remove { _inner.SelectedIndexChanged -= value; }
        }

        public ModernCombo()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw, true);

            BackColor = Theme.FieldBg;
            Padding   = new Padding(12, 0, 4, 0);
            Height    = 36;

            _inner = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat,
                BackColor     = Theme.FieldBg,
                ForeColor     = Theme.TextPrimary,
                Font          = Theme.FontBase,
                Dock          = DockStyle.Fill,
            };
            _inner.GotFocus  += (s, e) => Invalidate();
            _inner.LostFocus += (s, e) => Invalidate();
            Controls.Add(_inner);
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

            var rect    = new Rectangle(0, 0, Width - 1, Height - 1);
            bool focused = _inner.Focused;
            Color borderColor = focused ? BorderColorFocus : BorderColorIdle;
            float w = focused ? 1.4f : 1f;

            using (var path  = ModernCard.RoundedRect(rect, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            using (var pen   = new Pen(borderColor, w))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
        }
    }
}
