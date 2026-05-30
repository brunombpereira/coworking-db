using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Vista timeline horária por dia: linhas = salas, colunas = horas
    /// (HourStart..HourEnd). Pinta células coloridas conforme estado da
    /// reserva. Click numa célula com reserva dispara ReservaClicked.
    /// </summary>
    public class MapaReservasView : Panel
    {
        public class SalaInfo
        {
            public int RecursoId;
            public string Espaco;
            public string Sala;
        }

        public class ReservaCell
        {
            public int ReservaId;
            public int RecursoId;
            public TimeSpan HoraInicio;
            public TimeSpan HoraFim;
            public string Estado;
            public string Cliente;
            public decimal Valor;
        }

        public int HourStart { get; set; } = 8;
        public int HourEnd   { get; set; } = 20;  // exclusive: 8..19 = 12 colunas
        public int ColLabelW { get; set; } = 200;
        public int RowH      { get; set; } = 44;
        public int RowHeaderH{ get; set; } = 36;
        public int EspacoHdrH{ get; set; } = 28;

        private List<SalaInfo> _salas = new List<SalaInfo>();
        private List<ReservaCell> _reservas = new List<ReservaCell>();
        private List<(Rectangle rect, ReservaCell rsv)> _hitboxes = new List<(Rectangle, ReservaCell)>();
        private ReservaCell _hoverRsv;
        private ToolTip _tip;

        public event Action<int> ReservaClicked;

        public MapaReservasView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.CardBg;
            AutoScroll = true;
            _tip = new ToolTip { InitialDelay = 300, AutoPopDelay = 8000, ReshowDelay = 200 };
        }

        public void SetData(List<SalaInfo> salas, List<ReservaCell> reservas)
        {
            _salas    = salas    ?? new List<SalaInfo>();
            _reservas = reservas ?? new List<ReservaCell>();
            RecomputeSize();
            Invalidate();
        }

        private void RecomputeSize()
        {
            // Altura total: cabeçalho horas + (por espaço: cabeçalho + N salas * RowH)
            int h = RowHeaderH;
            string lastEsp = null;
            foreach (var s in _salas)
            {
                if (s.Espaco != lastEsp) { h += EspacoHdrH; lastEsp = s.Espaco; }
                h += RowH;
            }
            // Padding em baixo
            h += 12;
            // Set AutoScrollMinSize para activar scroll vertical se necessário
            AutoScrollMinSize = new Size(0, h);
        }

        private Point ToLogical(Point p) => new Point(p.X - AutoScrollPosition.X, p.Y - AutoScrollPosition.Y);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var logical = ToLogical(e.Location);
            ReservaCell hit = null;
            foreach (var (rect, rsv) in _hitboxes)
            {
                if (rect.Contains(logical)) { hit = rsv; break; }
            }
            if (hit != _hoverRsv)
            {
                _hoverRsv = hit;
                Cursor = hit != null ? Cursors.Hand : Cursors.Default;
                if (hit != null)
                {
                    string txt = $"{hit.Cliente}\n{hit.HoraInicio:hh\\:mm} – {hit.HoraFim:hh\\:mm}\n{hit.Estado} · {Theme.FormatEuro(hit.Valor)}";
                    _tip.Show(txt, this, e.Location.X + 12, e.Location.Y + 12, 4000);
                }
                else
                {
                    _tip.Hide(this);
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverRsv = null;
            _tip.Hide(this);
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            var logical = ToLogical(e.Location);
            foreach (var (rect, rsv) in _hitboxes)
            {
                if (rect.Contains(logical))
                {
                    ReservaClicked?.Invoke(rsv.ReservaId);
                    break;
                }
            }
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

            _hitboxes.Clear();

            int cols = HourEnd - HourStart;
            int gridW = Math.Max(400, Width - ColLabelW - 16);
            int colW  = gridW / cols;
            int gridX = ColLabelW;

            // ─── Cabeçalho horas ──────────────────────────────────────────
            using (var fHdr = new Font(Theme.FontBase.FontFamily, 9f, FontStyle.Bold))
            {
                // Label coluna 0 ("Sala")
                TextRenderer.DrawText(g, "RECURSO",
                    new Font(Theme.FontBase.FontFamily, 8.5f, FontStyle.Bold),
                    new Rectangle(8, 0, ColLabelW - 8, RowHeaderH),
                    Theme.TextMuted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                for (int i = 0; i < cols; i++)
                {
                    int hour = HourStart + i;
                    var rect = new Rectangle(gridX + i * colW, 0, colW, RowHeaderH);
                    TextRenderer.DrawText(g, hour.ToString("00") + "h", fHdr, rect,
                        Theme.TextSecondary,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }
            // Linha sob cabeçalho
            using (var pen = new Pen(Theme.CardBorder, 1f))
                g.DrawLine(pen, 0, RowHeaderH - 1, gridX + cols * colW, RowHeaderH - 1);

            // ─── Linhas por sala (agrupadas por espaço) ───────────────────
            int y = RowHeaderH;
            string lastEsp = null;
            foreach (var s in _salas)
            {
                if (s.Espaco != lastEsp)
                {
                    // Cabeçalho de espaço
                    var hdrRect = new Rectangle(0, y, gridX + cols * colW, EspacoHdrH);
                    using (var br = new SolidBrush(Theme.PageBg))
                        g.FillRectangle(br, hdrRect);
                    TextRenderer.DrawText(g, s.Espaco.ToUpper(),
                        new Font(Theme.FontBase.FontFamily, 8.5f, FontStyle.Bold),
                        new Rectangle(8, y, ColLabelW - 8, EspacoHdrH),
                        Theme.Accent,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    y += EspacoHdrH;
                    lastEsp = s.Espaco;
                }

                // Nome da sala (coluna 0)
                TextRenderer.DrawText(g, "Sala " + s.Sala,
                    new Font(Theme.FontBase.FontFamily, 10f, FontStyle.Bold),
                    new Rectangle(16, y, ColLabelW - 16, RowH),
                    Theme.TextPrimary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                // Grid de horas (linhas verticais subtis)
                using (var pen = new Pen(Theme.CardBorder, 1f))
                {
                    for (int i = 0; i <= cols; i++)
                    {
                        int x = gridX + i * colW;
                        g.DrawLine(pen, x, y, x, y + RowH);
                    }
                    g.DrawLine(pen, 0, y + RowH - 1, gridX + cols * colW, y + RowH - 1);
                }

                // Reservas desta sala
                foreach (var r in _reservas)
                {
                    if (r.RecursoId != s.RecursoId) continue;
                    double startH = r.HoraInicio.TotalHours;
                    double endH   = r.HoraFim.TotalHours;
                    if (endH <= HourStart || startH >= HourEnd) continue;
                    double clampedStart = Math.Max(startH, HourStart);
                    double clampedEnd   = Math.Min(endH, HourEnd);

                    int x1 = gridX + (int)Math.Round((clampedStart - HourStart) * colW);
                    int x2 = gridX + (int)Math.Round((clampedEnd   - HourStart) * colW);
                    var rsvRect = new Rectangle(x1 + 2, y + 4, Math.Max(20, x2 - x1 - 4), RowH - 8);

                    Color bg = ColorForEstado(r.Estado, fillBg: true);
                    Color fg = ColorForEstado(r.Estado, fillBg: false);

                    using (var path = ModernCard.RoundedRect(rsvRect, 6))
                    using (var br = new SolidBrush(bg))
                        g.FillPath(br, path);

                    // Barra accent à esquerda (3px)
                    using (var br = new SolidBrush(fg))
                        g.FillRectangle(br, rsvRect.X, rsvRect.Y, 3, rsvRect.Height);

                    // Texto do cliente — truncado pelo width
                    var textRect = new Rectangle(rsvRect.X + 8, rsvRect.Y, rsvRect.Width - 12, rsvRect.Height);
                    TextRenderer.DrawText(g, r.Cliente,
                        new Font(Theme.FontBase.FontFamily, 8.5f, FontStyle.Bold),
                        textRect, fg,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                    // Hitbox em coords lógicas (não ajustadas para scroll).
                    // ToLogical() converte e.Location quando ocorre o click.
                    _hitboxes.Add((rsvRect, r));
                }

                y += RowH;
            }

            // Empty state
            if (_salas.Count == 0)
            {
                TextRenderer.DrawText(g, "Sem salas disponíveis.",
                    Theme.FontBase,
                    new Rectangle(0, RowHeaderH + 20, Width, 30),
                    Theme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            }
        }

        private static Color ColorForEstado(string estado, bool fillBg)
        {
            switch (estado)
            {
                case "Confirmada": return fillBg ? Theme.StatusSuccessBg : Theme.StatusSuccessFg;
                case "Pendente":   return fillBg ? Theme.StatusWarningBg : Theme.StatusWarningFg;
                case "Cancelada":  return fillBg ? Theme.StatusDangerBg  : Theme.StatusDangerFg;
                case "Concluida":  return fillBg ? Theme.StatusNeutralBg : Theme.StatusNeutralFg;
                default:            return fillBg ? Theme.StatusNeutralBg : Theme.StatusNeutralFg;
            }
        }
    }
}
