using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    /// <summary>
    /// Campo de data com look modern (chip rounded com bg FieldBg, border
    /// CardBorder). Botão que mostra a data formatada com ícone calendário.
    /// On click abre um MonthCalendar popup. Suficiente para filtros — não é
    /// um substituto completo de DateTimePicker (sem time, sem keyboard nav).
    /// </summary>
    public class ModernDateField : Control
    {
        public int   CornerRadius     { get; set; } = 8;
        public Color BorderColorIdle  { get; set; } = Theme.CardBorder;

        private DateTime _value = DateTime.Today;
        public DateTime Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;

        private bool _hover;
        private Form _popup;
        private MonthCalendar _cal;

        public ModernDateField()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);

            BackColor = Theme.FieldBg;
            ForeColor = Theme.TextPrimary;
            Font      = Theme.FontBase;
            Height    = 36;
            Width     = 130;
            Cursor    = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e)      { ShowPopup(); base.OnClick(e); }

        private void ShowPopup()
        {
            if (_popup != null && !_popup.IsDisposed) { _popup.Close(); _popup = null; return; }

            _cal = new MonthCalendar
            {
                SelectionStart = _value,
                SelectionEnd   = _value,
                MaxSelectionCount = 1,
                ShowToday = true,
                ShowTodayCircle = true,
            };
            _cal.DateChanged += (s, ev) =>
            {
                Value = ev.Start;
                _popup?.Close();
            };

            _popup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition   = FormStartPosition.Manual,
                ShowInTaskbar   = false,
                TopMost         = true,
                BackColor       = Theme.CardBg,
                Padding         = new Padding(2),
            };
            _popup.Controls.Add(_cal);
            _popup.Size = new Size(_cal.Width + 4, _cal.Height + 4);

            var screenPt = PointToScreen(new Point(0, Height + 4));
            _popup.Location = screenPt;
            _popup.Deactivate += (s, ev) => _popup.Close();
            _popup.Show(FindForm());
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

            // Ícone calendário à esquerda
            using (var icon = GetCalendarImg())
            {
                int iconSz = 14;
                int iy     = (Height - iconSz) / 2;
                g.DrawImage(icon, 10, iy, iconSz, iconSz);
            }

            // Texto da data
            string txt = _value.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-PT"));
            var ts = TextRenderer.MeasureText(g, txt, Font, Size.Empty, TextFormatFlags.NoPadding);
            int tx = 32;
            int ty = (Height - ts.Height) / 2;
            TextRenderer.DrawText(g, txt, Font, new Point(tx, ty), ForeColor,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        private Image GetCalendarImg()
        {
            using (var pb = new IconPictureBox
                   { IconChar = IconChar.CalendarDay, IconSize = 14, IconColor = Theme.TextSecondary })
                return pb.Image != null ? (Image)pb.Image.Clone() : null;
        }
    }
}
