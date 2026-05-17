using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    /// <summary>
    /// Dropdown selector custom-painted (chip rounded + chevron). On click
    /// abre um popup ListBox styled. Substitui o ComboBox nativo, que tem
    /// chrome de sistema impossível de estilizar em dark theme.
    ///
    /// Suporta DataTable (DataSource + DisplayMember + ValueMember) ou
    /// Items.Add(string) para listas simples.
    /// </summary>
    public class ModernSelect : Control
    {
        public int   CornerRadius     { get; set; } = 8;
        public Color BorderColorIdle  { get; set; } = Theme.CardBorder;

        private bool _hover;
        private Form _popup;
        private Panel _listPanel;
        private int   _scrollOffset = 0;
        private const int ItemH      = 28;
        private const int ScrollbarW = 6;
        private const int PopupPadV  = 4;

        private readonly List<Item> _items = new List<Item>();
        private int _selectedIndex = -1;

        private class Item
        {
            public string Display;
            public object Value;
            public object Raw; // DataRow underlying se vier de DataTable
        }

        /// <summary>Panel que aceita Focus (necessário para receber MouseWheel).</summary>
        private class FocusablePanel : Panel
        {
            public FocusablePanel()
            {
                SetStyle(ControlStyles.Selectable, true);
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            }
        }

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

        public object SelectedValue
        {
            get => (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex].Value : null;
            set
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (Equals(_items[i].Value, value)) { SelectedIndex = i; return; }
                }
            }
        }

        public string SelectedText =>
            (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex].Display : "";

        /// <summary>Adiciona items simples por string. Value = string.</summary>
        public void AddItems(params string[] items)
        {
            foreach (var s in items) _items.Add(new Item { Display = s, Value = s });
            if (_selectedIndex < 0 && _items.Count > 0) SelectedIndex = 0;
            Invalidate();
        }

        /// <summary>Carrega items a partir de DataTable (display/value cols).</summary>
        public void BindDataTable(DataTable dt, string displayCol, string valueCol)
        {
            _items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                _items.Add(new Item
                {
                    Display = r[displayCol]?.ToString() ?? "",
                    Value   = r[valueCol] is DBNull ? null : r[valueCol],
                    Raw     = r,
                });
            }
            if (_items.Count > 0) SelectedIndex = 0;
            Invalidate();
        }

        public ModernSelect()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);
            BackColor = Theme.FieldBg;
            ForeColor = Theme.TextPrimary;
            Font      = Theme.FontBase;
            Height    = 36;
            Width     = 150;
            Cursor    = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e)      { ShowPopup();    base.OnClick(e); }

        private int _hoverIdx = -1;

        private void ShowPopup()
        {
            if (_popup != null && !_popup.IsDisposed) { _popup.Close(); _popup = null; return; }
            _scrollOffset = 0;
            _hoverIdx     = -1;

            const int MaxVisible = 8;
            int totalH    = _items.Count * ItemH + PopupPadV * 2;
            int visibleH  = Math.Min(totalH, MaxVisible * ItemH + PopupPadV * 2);

            _listPanel = new FocusablePanel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.CardBg,
                TabStop   = true,
            };
            _listPanel.MouseMove  += ListPanel_MouseMove;
            _listPanel.MouseEnter += (s, ev) => _listPanel.Focus();
            _listPanel.MouseLeave += (s, ev) => { _hoverIdx = -1; _listPanel.Invalidate(); };
            _listPanel.MouseClick += ListPanel_MouseClick;
            _listPanel.MouseWheel += ListPanel_MouseWheel;
            _listPanel.Paint      += ListPanel_Paint;

            _popup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition   = FormStartPosition.Manual,
                ShowInTaskbar   = false,
                TopMost         = true,
                BackColor       = Theme.CardBorder,
                Padding         = new Padding(1),
                Size            = new Size(Width, visibleH + 2),
            };
            _popup.Controls.Add(_listPanel);

            var pt = PointToScreen(new Point(0, Height + 4));
            _popup.Location = pt;
            _popup.Deactivate += (s, ev) => _popup.Close();
            _popup.Show(FindForm());
            _listPanel.Focus(); // habilita MouseWheel
        }

        private int VisibleItems() =>
            (_listPanel.ClientSize.Height - PopupPadV * 2) / ItemH;

        private int MaxScroll() =>
            Math.Max(0, _items.Count - VisibleItems());

        private int IndexAtPoint(Point p)
        {
            int y = p.Y - PopupPadV;
            if (y < 0) return -1;
            int idx = y / ItemH + _scrollOffset;
            return (idx >= 0 && idx < _items.Count) ? idx : -1;
        }

        private void ListPanel_MouseMove(object sender, MouseEventArgs e)
        {
            int idx = IndexAtPoint(e.Location);
            if (idx != _hoverIdx) { _hoverIdx = idx; _listPanel.Invalidate(); }
        }

        private void ListPanel_MouseClick(object sender, MouseEventArgs e)
        {
            int idx = IndexAtPoint(e.Location);
            if (idx >= 0) { SelectedIndex = idx; _popup?.Close(); }
        }

        private void ListPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            int max = MaxScroll();
            if (max <= 0) return;
            int delta = -Math.Sign(e.Delta);
            _scrollOffset = Math.Max(0, Math.Min(max, _scrollOffset + delta));
            _listPanel.Invalidate();
        }

        private void ListPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var br = new SolidBrush(Theme.CardBg))
                g.FillRectangle(br, _listPanel.ClientRectangle);

            int visible = VisibleItems();
            int width   = _listPanel.ClientSize.Width;
            int max     = MaxScroll();
            bool needScroll = max > 0;
            int contentW = needScroll ? width - ScrollbarW - 4 : width;

            for (int i = 0; i < visible && (_scrollOffset + i) < _items.Count; i++)
            {
                int idx = _scrollOffset + i;
                var rect = new Rectangle(0, PopupPadV + i * ItemH, contentW, ItemH);
                bool isHover    = (idx == _hoverIdx);
                bool isSelected = (idx == _selectedIndex);

                if (isHover || isSelected)
                {
                    var fillR = Rectangle.Inflate(rect, -4, -2);
                    Color bg = isHover ? Theme.AccentSoft : Color.FromArgb(40, Theme.Accent);
                    using (var path = ModernCard.RoundedRect(fillR, 6))
                    using (var brF  = new SolidBrush(bg))
                        g.FillPath(brF, path);
                }

                Color fg = (isSelected && !isHover) ? Theme.Accent : Theme.TextPrimary;
                var txtR = new Rectangle(rect.X + 12, rect.Y, rect.Width - (isSelected ? 32 : 16), rect.Height);
                TextRenderer.DrawText(g, _items[idx].Display, Font, txtR, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                  | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

                if (isSelected)
                {
                    int cx = rect.Right - 16, cy = rect.Y + rect.Height / 2;
                    using (var pen = new Pen(Theme.Accent, 1.8f))
                    {
                        g.DrawLine(pen, cx - 4, cy,     cx - 1, cy + 3);
                        g.DrawLine(pen, cx - 1, cy + 3, cx + 5, cy - 3);
                    }
                }
            }

            // Scrollbar custom (thumb arredondado à direita)
            if (needScroll)
            {
                int trackX = width - ScrollbarW - 2;
                int trackY = PopupPadV;
                int trackH = _listPanel.ClientSize.Height - PopupPadV * 2;
                using (var br = new SolidBrush(Color.FromArgb(20, Theme.TextSecondary)))
                    g.FillRectangle(br, trackX, trackY, ScrollbarW, trackH);

                float ratio  = (float)visible / _items.Count;
                int   thumbH = Math.Max(24, (int)(trackH * ratio));
                int   thumbY = trackY + (int)((trackH - thumbH) * ((float)_scrollOffset / max));
                using (var path = ModernCard.RoundedRect(new Rectangle(trackX, thumbY, ScrollbarW, thumbH), ScrollbarW / 2))
                using (var br   = new SolidBrush(Theme.TextMuted))
                    g.FillPath(br, path);
            }
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

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path  = ModernCard.RoundedRect(rect, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            using (var pen   = new Pen(_hover ? Theme.Accent : BorderColorIdle, _hover ? 1.4f : 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // Chevron down à direita
            int cx = Width - 18;
            int cy = Height / 2;
            using (var pen = new Pen(Theme.TextSecondary, 1.4f))
            {
                g.DrawLine(pen, cx - 4, cy - 2, cx,     cy + 2);
                g.DrawLine(pen, cx,     cy + 2, cx + 4, cy - 2);
            }

            // Texto do selecionado
            string txt = SelectedText;
            if (!string.IsNullOrEmpty(txt))
            {
                var r = new Rectangle(12, 0, Width - 30, Height);
                TextRenderer.DrawText(g, txt, Font, r, ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                  | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
