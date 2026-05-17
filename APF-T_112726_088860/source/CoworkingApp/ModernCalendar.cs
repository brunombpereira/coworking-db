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
    /// Calendário custom-painted para escolher uma data. Substitui o
    /// MonthCalendar nativo (sempre Windows-style branco). Header com
    /// navegação por mês, grid 7x6 com dias.
    /// </summary>
    public class ModernCalendar : Control
    {
        public event EventHandler<DateTime> DateSelected;

        private DateTime _value = DateTime.Today;
        private DateTime _displayMonth; // primeiro dia do mês mostrado

        private Rectangle _rectPrev, _rectNext, _rectPrevYear, _rectNextYear;
        private readonly Rectangle[,] _rectDays = new Rectangle[6, 7];
        private readonly DateTime?[,]  _dateGrid = new DateTime?[6, 7];

        private const int HeaderH    = 44;
        private const int WeekHeadH  = 26;
        private const int CellW      = 36;
        private const int CellH      = 34;
        private const int PadInner   = 12;

        public DateTime Value
        {
            get => _value;
            set { _value = value.Date; _displayMonth = new DateTime(_value.Year, _value.Month, 1); Invalidate(); }
        }

        public ModernCalendar()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);
            BackColor    = Theme.CardBg;
            ForeColor    = Theme.TextPrimary;
            Font         = Theme.FontBase;
            Width        = PadInner * 2 + CellW * 7;            // 7 colunas
            Height       = HeaderH + WeekHeadH + CellH * 6 + PadInner;
            _displayMonth = new DateTime(_value.Year, _value.Month, 1);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (_rectPrevYear.Contains(e.Location)) { _displayMonth = _displayMonth.AddYears(-1);  Invalidate(); return; }
            if (_rectNextYear.Contains(e.Location)) { _displayMonth = _displayMonth.AddYears(1);   Invalidate(); return; }
            if (_rectPrev    .Contains(e.Location)) { _displayMonth = _displayMonth.AddMonths(-1); Invalidate(); return; }
            if (_rectNext    .Contains(e.Location)) { _displayMonth = _displayMonth.AddMonths(1);  Invalidate(); return; }

            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 7; c++)
                {
                    if (_rectDays[r, c].Contains(e.Location) && _dateGrid[r, c].HasValue)
                    {
                        _value = _dateGrid[r, c].Value;
                        DateSelected?.Invoke(this, _value);
                        Invalidate();
                        return;
                    }
                }
            base.OnMouseClick(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool onArrow = _rectPrev    .Contains(e.Location) || _rectNext    .Contains(e.Location)
                        || _rectPrevYear.Contains(e.Location) || _rectNextYear.Contains(e.Location);
            Cursor = onArrow ? Cursors.Hand : Cursors.Default;
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 7; c++)
                    if (_rectDays[r, c].Contains(e.Location) && _dateGrid[r, c].HasValue)
                        Cursor = Cursors.Hand;
            base.OnMouseMove(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var br = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(br, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            DrawHeader(g);
            DrawWeekHead(g);
            DrawGrid(g);
        }

        private void DrawHeader(Graphics g)
        {
            int btnSize = 26;
            int y       = (HeaderH - btnSize) / 2;
            // Esquerda: << ano-1, < mês-1
            _rectPrevYear = new Rectangle(PadInner,                  y, btnSize, btnSize);
            _rectPrev     = new Rectangle(PadInner + btnSize + 4,    y, btnSize, btnSize);
            // Direita: > mês+1, >> ano+1
            _rectNext     = new Rectangle(Width - PadInner - btnSize * 2 - 4, y, btnSize, btnSize);
            _rectNextYear = new Rectangle(Width - PadInner - btnSize,         y, btnSize, btnSize);

            DrawChevron(g, _rectPrevYear, true,  doubleChevron: true);
            DrawChevron(g, _rectPrev,     true,  doubleChevron: false);
            DrawChevron(g, _rectNext,     false, doubleChevron: false);
            DrawChevron(g, _rectNextYear, false, doubleChevron: true);

            string label = _displayMonth.ToString("MMMM yyyy", new CultureInfo("pt-PT"));
            label = char.ToUpper(label[0]) + label.Substring(1);
            using (var f = new Font(Font.FontFamily, 11f, FontStyle.Bold))
            {
                var ts = TextRenderer.MeasureText(g, label, f, Size.Empty, TextFormatFlags.NoPadding);
                var pt = new Point((Width - ts.Width) / 2, (HeaderH - ts.Height) / 2);
                TextRenderer.DrawText(g, label, f, pt, Theme.TextPrimary, TextFormatFlags.NoPadding);
            }
        }

        private void DrawChevron(Graphics g, Rectangle r, bool left, bool doubleChevron)
        {
            bool hover = r.Contains(PointToClient(System.Windows.Forms.Cursor.Position));
            Color c = hover ? Theme.Accent : Theme.TextSecondary;

            // Bg hover subtle
            if (hover)
            {
                using (var path = ModernCard.RoundedRect(r, 6))
                using (var br   = new SolidBrush(Theme.AccentSoft))
                    g.FillPath(br, path);
            }

            using (var pen = new Pen(c, 1.6f))
            {
                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;
                int armSize = 4;
                if (doubleChevron)
                {
                    int off = 4;
                    if (left)
                    {
                        // < <
                        DrawSingleChevron(g, pen, cx - off,           cy, armSize, true);
                        DrawSingleChevron(g, pen, cx - off + armSize, cy, armSize, true);
                    }
                    else
                    {
                        // > >
                        DrawSingleChevron(g, pen, cx + off,           cy, armSize, false);
                        DrawSingleChevron(g, pen, cx + off - armSize, cy, armSize, false);
                    }
                }
                else
                {
                    DrawSingleChevron(g, pen, cx, cy, armSize + 1, left);
                }
            }
        }

        private void DrawSingleChevron(Graphics g, Pen pen, int cx, int cy, int arm, bool left)
        {
            if (left)
            {
                g.DrawLine(pen, cx + arm / 2, cy - arm, cx - arm / 2, cy);
                g.DrawLine(pen, cx - arm / 2, cy,       cx + arm / 2, cy + arm);
            }
            else
            {
                g.DrawLine(pen, cx - arm / 2, cy - arm, cx + arm / 2, cy);
                g.DrawLine(pen, cx + arm / 2, cy,       cx - arm / 2, cy + arm);
            }
        }

        private static readonly string[] DiasSemana = { "S", "T", "Q", "Q", "S", "S", "D" }; // Seg-Dom

        private void DrawWeekHead(Graphics g)
        {
            int y = HeaderH;
            for (int i = 0; i < 7; i++)
            {
                int x = PadInner + i * CellW;
                var r = new Rectangle(x, y, CellW, WeekHeadH);
                TextRenderer.DrawText(g, DiasSemana[i], new Font(Font.FontFamily, 9f, FontStyle.Bold),
                    r, Theme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            // Linha divisória subtle
            using (var pen = new Pen(Theme.CardBorder, 1))
                g.DrawLine(pen, PadInner, y + WeekHeadH - 1, Width - PadInner, y + WeekHeadH - 1);
        }

        private void DrawGrid(Graphics g)
        {
            int gridY = HeaderH + WeekHeadH;

            // 1º dia do mês — index na grid (0=Segunda)
            int firstDow = ((int)_displayMonth.DayOfWeek + 6) % 7; // dom=0 → 6
            int daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
            DateTime today = DateTime.Today;
            Point cursor = PointToClient(System.Windows.Forms.Cursor.Position);

            int dayCounter = 1;
            for (int r = 0; r < 6; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    int flat = r * 7 + c;
                    var rect = new Rectangle(PadInner + c * CellW, gridY + r * CellH, CellW, CellH);
                    _rectDays[r, c] = rect;

                    if (flat < firstDow || dayCounter > daysInMonth)
                    {
                        _dateGrid[r, c] = null;
                        continue;
                    }

                    var d = new DateTime(_displayMonth.Year, _displayMonth.Month, dayCounter);
                    _dateGrid[r, c] = d;
                    bool isSelected = (d == _value.Date);
                    bool isToday    = (d == today);
                    bool isHover    = rect.Contains(cursor) && !isSelected;

                    if (isSelected)
                    {
                        var inner = Rectangle.Inflate(rect, -3, -3);
                        using (var path = ModernCard.RoundedRect(inner, inner.Height / 2))
                        using (var br   = new SolidBrush(Theme.Accent))
                            g.FillPath(br, path);
                    }
                    else if (isHover)
                    {
                        var inner = Rectangle.Inflate(rect, -3, -3);
                        using (var path = ModernCard.RoundedRect(inner, inner.Height / 2))
                        using (var br   = new SolidBrush(Theme.AccentSoft))
                            g.FillPath(br, path);
                    }

                    Color fg = isSelected ? Color.White : Theme.TextPrimary;
                    TextRenderer.DrawText(g, dayCounter.ToString(), Font, rect, fg,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                      | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                    if (isToday && !isSelected)
                    {
                        // ponto pequeno indicando "hoje"
                        int dotSize = 4;
                        int dotX = rect.X + (rect.Width - dotSize) / 2;
                        int dotY = rect.Bottom - 6;
                        using (var br = new SolidBrush(Theme.Accent))
                            g.FillEllipse(br, dotX, dotY, dotSize, dotSize);
                    }

                    dayCounter++;
                }
            }
        }
    }
}
