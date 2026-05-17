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
        private ListBox _list;

        private readonly List<Item> _items = new List<Item>();
        private int _selectedIndex = -1;

        private class Item
        {
            public string Display;
            public object Value;
            public object Raw; // DataRow underlying se vier de DataTable
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

        private void ShowPopup()
        {
            if (_popup != null && !_popup.IsDisposed) { _popup.Close(); _popup = null; return; }

            _list = new ListBox
            {
                Dock          = DockStyle.Fill,
                BackColor     = Theme.CardBg,
                ForeColor     = Theme.TextPrimary,
                BorderStyle   = BorderStyle.None,
                Font          = Font,
                IntegralHeight = false,
                ItemHeight    = 26,
                DrawMode      = DrawMode.OwnerDrawFixed,
            };
            foreach (var it in _items) _list.Items.Add(it.Display);
            _list.SelectedIndex = _selectedIndex;
            _list.DrawItem += List_DrawItem;
            _list.MouseClick += (s, ev) =>
            {
                int idx = _list.IndexFromPoint(ev.Location);
                if (idx >= 0) { SelectedIndex = idx; _popup?.Close(); }
            };

            int listH = Math.Min(Math.Max(_items.Count, 1) * _list.ItemHeight + 4, 280);
            _popup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition   = FormStartPosition.Manual,
                ShowInTaskbar   = false,
                TopMost         = true,
                BackColor       = Theme.CardBorder,
                Padding         = new Padding(1),
                Size            = new Size(Width, listH),
            };
            _popup.Controls.Add(_list);

            var pt = PointToScreen(new Point(0, Height + 4));
            _popup.Location = pt;
            _popup.Deactivate += (s, ev) => _popup.Close();
            _popup.Show(FindForm());
        }

        private void List_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0
                    || (e.Index == _list.IndexFromPoint(_list.PointToClient(System.Windows.Forms.Cursor.Position)));
            Color bg = sel ? Theme.AccentSoft : Theme.CardBg;
            Color fg = Theme.TextPrimary;
            using (var br = new SolidBrush(bg)) e.Graphics.FillRectangle(br, e.Bounds);
            var txt = _list.Items[e.Index].ToString();
            var r = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, txt, e.Font, r, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
