using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;

namespace CoworkingApp.Controls
{
    public class UcReservas : UserControl
    {
        // KPI labels
        private Label _kpiTotal, _kpiConfirm, _kpiPend, _kpiRev;

        // Toolbar/filtros
        private ModernDateField _dtDe, _dtAte;
        private ModernSelect _cmbCliente, _cmbEstado;
        private ModernButton _btnNova;

        // Lista
        private Panel _listHost;
        private Panel _listInner;
        private Panel _empty;

        public UcReservas()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadFiltroClientes();
            LoadData();
        }

        // ── BUILD UI ────────────────────────────────────────────────────
        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                BackColor = Theme.PageBg,
                Padding = new Padding(20, 16, 20, 16),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // title
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));   // toolbar
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));  // KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // list

            root.Controls.Add(BuildTitle(),   0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);
            root.Controls.Add(BuildKpis(),    0, 2);
            root.Controls.Add(BuildList(),    0, 3);

            Controls.Add(root);
        }

        private Control BuildTitle()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var lbl = new Label
            {
                Text = "Reservas", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            pnl.Controls.Add(lbl);
            return pnl;
        }

        // ── Toolbar ─────────────────────────────────────────────────────
        private Control BuildToolbar()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
                Margin = new Padding(0, 0, 0, 12),
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(16, 12, 16, 12) };

            // ─── Botão Nova (à direita, anchored) ─────────────────────
            _btnNova = new ModernButton
            {
                Text = "+ Nova Reserva", Style = ModernButton.Variant.Primary,
                Font = Theme.FontBold, Size = new Size(160, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnNova.Click += (s, e) => OpenEditor();

            // ─── Filtros (chips modern) ───────────────────────────────
            _cmbEstado = MakeCombo(140);
            _cmbEstado.AddItems("(Todos)", "Pendente", "Confirmada", "Cancelada", "Concluida");
            _cmbEstado.SelectedIndexChanged += (s, e) => LoadData();

            _cmbCliente = MakeCombo(190);
            _cmbCliente.SelectedIndexChanged += (s, e) => LoadData();

            _dtAte = MakeDate(DateTime.Today.AddMonths(2));
            _dtAte.ValueChanged += (s, e) => LoadData();
            _dtDe  = MakeDate(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
            _dtDe.ValueChanged  += (s, e) => LoadData();

            var flow = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = Theme.CardBg, Padding = new Padding(0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            flow.Controls.Add(MakeFilterLabel("Período"));
            flow.Controls.Add(_dtDe);
            flow.Controls.Add(MakeArrowLabel());
            flow.Controls.Add(_dtAte);
            flow.Controls.Add(MakeFilterLabel("Cliente"));
            flow.Controls.Add(_cmbCliente);
            flow.Controls.Add(MakeFilterLabel("Estado"));
            flow.Controls.Add(_cmbEstado);

            inner.Controls.Add(flow);
            inner.Controls.Add(_btnNova);
            card.Controls.Add(inner);

            // Posicionamento: filtros à esquerda, botão à direita, ambos
            // centrados verticalmente. Sem espaço gritante entre eles.
            void Relayout()
            {
                var dr = inner.DisplayRectangle;
                int btnY = dr.Y + (dr.Height - _btnNova.Height) / 2;
                _btnNova.Location = new Point(dr.Right - _btnNova.Width, btnY);

                int flowY = dr.Y + (dr.Height - flow.Height) / 2;
                flow.Location = new Point(dr.X, flowY);
            }
            inner.SizeChanged   += (s, e) => Relayout();
            flow.SizeChanged    += (s, e) => Relayout();
            inner.HandleCreated += (s, e) => Relayout();

            return card;
        }

        private Label MakeFilterLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font(Theme.FontBase.FontFamily, 9f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg,
                Margin = new Padding(text == "Período" ? 0 : 18, 14, 8, 0),
            };
        }

        private Label MakeArrowLabel()
        {
            return new Label
            {
                Text = "→",
                Font = new Font(Theme.FontBase.FontFamily, 11f, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                BackColor = Theme.CardBg,
                Margin = new Padding(8, 14, 8, 0),
            };
        }

        private ModernSelect MakeCombo(int width = 150)
        {
            return new ModernSelect
            {
                Width = width, Height = 36,
                Margin = new Padding(0, 6, 0, 0),
            };
        }

        private ModernDateField MakeDate(DateTime initial)
        {
            return new ModernDateField
            {
                Width = 130, Height = 36,
                Value = initial,
                Margin = new Padding(0, 6, 0, 0),
            };
        }

        // ── KPIs ────────────────────────────────────────────────────────
        private Control BuildKpis()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Theme.PageBg,
                Margin = new Padding(0, 0, 0, 12),
            };
            for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var k1 = BuildKpi("Total no período", IconChar.CalendarDays, Theme.Accent,           out _kpiTotal);
            var k2 = BuildKpi("Confirmadas",      IconChar.CircleCheck,  Theme.StatusSuccessFg, out _kpiConfirm);
            var k3 = BuildKpi("Pendentes",        IconChar.Clock,        Theme.StatusWarningFg, out _kpiPend);
            var k4 = BuildKpi("Receita prevista", IconChar.EuroSign,     Theme.Accent,           out _kpiRev);
            k4.Margin = new Padding(0); // último: sem margin à direita
            grid.Controls.Add(k1, 0, 0);
            grid.Controls.Add(k2, 1, 0);
            grid.Controls.Add(k3, 2, 0);
            grid.Controls.Add(k4, 3, 0);
            return grid;
        }

        private Control BuildKpi(string label, IconChar icon, Color iconColor, out Label valueLbl)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
                Margin = new Padding(0, 0, 12, 0),
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(18, 14, 18, 14) };

            var topLine = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Theme.CardBg };
            var iconLbl = new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = iconColor,
                BackColor = Theme.CardBg, Dock = DockStyle.Left, Width = 22,
                SizeMode = PictureBoxSizeMode.CenterImage,
            };
            var lbl = new Label
            {
                Text = label, Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0),
            };
            topLine.Controls.Add(lbl);
            topLine.Controls.Add(iconLbl);

            valueLbl = new Label
            {
                Text = "—", Font = new Font(Theme.FontBase.FontFamily, 24f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 4, 0, 0),
            };

            inner.Controls.Add(valueLbl);
            inner.Controls.Add(topLine);
            card.Controls.Add(inner);
            return card;
        }

        // ── Lista ───────────────────────────────────────────────────────
        private Control BuildList()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(10, 10, 10, 10) };

            _listHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, AutoScroll = true, Visible = false };
            _listInner = new Panel { Dock = DockStyle.Top, BackColor = Theme.CardBg, Height = 0 };
            _listHost.Controls.Add(_listInner);
            _listHost.Resize += (s, e) => ResizeListItems();

            _empty = BuildEmptyState("Sem reservas no intervalo selecionado", IconChar.CalendarXmark);
            _empty.Dock = DockStyle.Fill;

            inner.Controls.Add(_listHost);
            inner.Controls.Add(_empty);
            card.Controls.Add(inner);
            return card;
        }

        private Panel BuildEmptyState(string text, IconChar icon)
        {
            var pnl = new Panel { BackColor = Theme.CardBg };
            Image iconImg = null;
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                if (iconImg == null)
                {
                    using (var pb = new IconPictureBox { IconChar = icon, IconSize = 44, IconColor = Theme.TextMuted })
                        if (pb.Image != null) iconImg = (Image)pb.Image.Clone();
                }
                var ts = TextRenderer.MeasureText(g, text, Theme.FontBase, Size.Empty, TextFormatFlags.NoPadding);
                int iconSize = 44, gap = 14, totalH = iconSize + gap + ts.Height;
                int startY = Math.Max(8, (pnl.Height - totalH) / 2);
                int iconX  = (pnl.Width - iconSize) / 2;
                int textX  = (pnl.Width - ts.Width) / 2;
                if (iconImg != null) g.DrawImage(iconImg, iconX, startY, iconSize, iconSize);
                TextRenderer.DrawText(g, text, Theme.FontBase, new Point(textX, startY + iconSize + gap),
                    Theme.TextMuted, TextFormatFlags.NoPadding);
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
            return pnl;
        }

        private void ResizeListItems()
        {
            if (_listInner == null) return;
            int w = _listHost.ClientSize.Width;
            _listInner.Width = w;
            foreach (Control c in _listInner.Controls) c.Width = w;
        }

        // ── Data ────────────────────────────────────────────────────────
        private void LoadFiltroClientes()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    var rowTodos = dt.NewRow();
                    rowTodos["cliente_id"] = DBNull.Value;
                    rowTodos["nome"]       = "(Todos)";
                    dt.Rows.InsertAt(rowTodos, 0);
                    _cmbCliente.BindDataTable(dt, "nome", "cliente_id");
                }
            }
            catch (SqlException) { /* ignore */ }
        }

        private void LoadData()
        {
            if (_dtDe == null || _dtAte == null) return;
            if (_dtDe.Value.Date > _dtAte.Value.Date) return;
            try
            {
                var whereParts = new List<string> { "r.data_reserva BETWEEN @de AND @ate" };
                string estadoFiltro = (_cmbEstado != null && _cmbEstado.SelectedIndex > 0)
                    ? _cmbEstado.SelectedText : null;
                if (estadoFiltro != null) whereParts.Add("r.estado = @e");
                bool filtraCliente = _cmbCliente != null && _cmbCliente.SelectedIndex > 0
                    && _cmbCliente.SelectedValue != null
                    && !(_cmbCliente.SelectedValue is DBNull);
                if (filtraCliente) whereParts.Add("r.cliente_id = @c");

                string sql = $@"
                    SELECT TOP 200 r.reserva_id AS id, c.nome AS cliente,
                           CASE WHEN s.recurso_id IS NOT NULL THEN 'Sala ' + s.nome
                                ELSE 'Posto ' + p.codigo END AS recurso,
                           rc.tipo AS tipo_rec,
                           r.data_reserva AS dt, r.hora_inicio AS hi, r.hora_fim AS hf,
                           r.valor AS valor, r.estado AS estado,
                           r.num_participantes AS num_pess
                    FROM reserva r
                    JOIN cliente c    ON r.cliente_id = c.cliente_id
                    JOIN recurso rc   ON r.recurso_id = rc.recurso_id
                    LEFT JOIN sala s  ON rc.recurso_id = s.recurso_id
                    LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
                    WHERE {string.Join(" AND ", whereParts)}
                    ORDER BY r.data_reserva DESC, r.hora_inicio";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@de",  _dtDe.Value.Date);
                    cmd.Parameters.AddWithValue("@ate", _dtAte.Value.Date);
                    if (estadoFiltro != null) cmd.Parameters.AddWithValue("@e", estadoFiltro);
                    if (filtraCliente) cmd.Parameters.AddWithValue("@c", Convert.ToInt32(_cmbCliente.SelectedValue));
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        RenderRows(dt);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRows(DataTable dt)
        {
            _listInner.SuspendLayout();
            _listInner.Controls.Clear();

            // KPIs sobre TODAS as linhas — cap só na renderização.
            int total = 0, confirm = 0, pend = 0;
            decimal receita = 0;
            foreach (DataRow rr in dt.Rows)
            {
                total++;
                string estR = rr["estado"].ToString();
                if (estR == "Confirmada") confirm++;
                else if (estR == "Pendente") pend++;
                if (estR != "Cancelada") receita += Convert.ToDecimal(rr["valor"]);
            }

            // Cap render — proteção contra Win32 handle exhaustion.
            const int MaxRender = 80;
            int rendered = Math.Min(dt.Rows.Count, MaxRender);

            int y = 0;
            int width = Math.Max(600, _listHost.ClientSize.Width);
            for (int idx = 0; idx < rendered; idx++)
            {
                DataRow r = dt.Rows[idx];
                int     id        = Convert.ToInt32(r["id"]);
                string  cliente   = r["cliente"].ToString();
                string  recurso   = r["recurso"].ToString();
                string  tipoRec   = r["tipo_rec"].ToString();
                DateTime data     = (DateTime)r["dt"];
                TimeSpan? hi      = r["hi"] is DBNull ? (TimeSpan?)null : (TimeSpan)r["hi"];
                TimeSpan? hf      = r["hf"] is DBNull ? (TimeSpan?)null : (TimeSpan)r["hf"];
                decimal  valor    = Convert.ToDecimal(r["valor"]);
                string   estado   = r["estado"].ToString();
                int?     numPess  = r["num_pess"] is DBNull ? (int?)null : Convert.ToInt32(r["num_pess"]);

                var card = BuildReservaCard(id, cliente, recurso, tipoRec, data, hi, hf, valor, estado, numPess);
                card.Location = new Point(0, y);
                card.Width    = width;
                card.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _listInner.Controls.Add(card);
                y += card.Height + 8;
            }
            if (dt.Rows.Count > MaxRender)
            {
                var more = new Label
                {
                    Text = $"+ {dt.Rows.Count - MaxRender} reservas mais antigas não mostradas",
                    Font = Theme.FontSub, ForeColor = Theme.TextMuted,
                    BackColor = Theme.CardBg, Height = 30,
                    Location = new Point(0, y), Width = width,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                };
                _listInner.Controls.Add(more);
                y += more.Height;
            }

            _listInner.Height = y;
            _listInner.ResumeLayout();

            _kpiTotal  .Text = total.ToString();
            _kpiConfirm.Text = confirm.ToString();
            _kpiPend   .Text = pend.ToString();
            _kpiRev    .Text = Theme.FormatEuro(receita);

            _listHost.Visible = dt.Rows.Count > 0;
            _empty   .Visible = dt.Rows.Count == 0;
            _empty   .Invalidate();
        }

        // ── Card ────────────────────────────────────────────────────────
        private Control BuildReservaCard(int id, string cliente, string recurso, string tipoRec,
                                          DateTime data, TimeSpan? hi, TimeSpan? hf,
                                          decimal valor, string estado, int? numPess)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = UcEspacos.MixColors(Theme.CardBg, Color.White, 0.05f);

            var row = new Panel
            {
                Height = 96, BackColor = idleBg, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 8),
            };

            // ─── Bloco data (Dock=Left) ───────────────────────────────
            var dateBlock = new Panel { Dock = DockStyle.Left, Width = 78, BackColor = idleBg };
            string mesPt   = data.ToString("MMM", new CultureInfo("pt-PT")).ToUpper().TrimEnd('.');
            int    dia     = data.Day;
            string semana  = data.ToString("ddd", new CultureInfo("pt-PT")).ToUpper();
            dateBlock.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using (var diaFont = new Font(Theme.FontBase.FontFamily, 22f, FontStyle.Bold))
                using (var mesFont = new Font(Theme.FontBase.FontFamily, 9f, FontStyle.Bold))
                {
                    var diaSize = TextRenderer.MeasureText(g, dia.ToString(), diaFont, Size.Empty, TextFormatFlags.NoPadding);
                    var mesSize = TextRenderer.MeasureText(g, mesPt, mesFont, Size.Empty, TextFormatFlags.NoPadding);
                    int totH = diaSize.Height + mesSize.Height + 2;
                    int top  = (dateBlock.Height - totH) / 2;
                    int cx   = dateBlock.Width / 2;
                    TextRenderer.DrawText(g, mesPt, mesFont,
                        new Point(cx - mesSize.Width / 2, top),
                        Theme.Accent, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, dia.ToString(), diaFont,
                        new Point(cx - diaSize.Width / 2, top + mesSize.Height + 2),
                        Theme.TextPrimary, TextFormatFlags.NoPadding);
                }
            };
            dateBlock.Resize += (s, e) => dateBlock.Invalidate();

            // ─── Actions (Dock=Right) ─────────────────────────────────
            var actions = new Panel { Dock = DockStyle.Right, Width = 100, BackColor = idleBg };
            var btnEdit = UcEspacos.MakeIconBtn(IconChar.Pen, Theme.Accent, idleBg, () => OpenEditor(id));
            bool canCancel = (estado != "Cancelada" && estado != "Concluida");
            var btnCancel = UcEspacos.MakeIconBtn(IconChar.Ban, Theme.StatusDangerFg, idleBg, () => CancelarReserva(id, canCancel));
            btnCancel.Enabled = canCancel;
            btnEdit  .Location = new Point(8,  30);
            btnCancel.Location = new Point(52, 30);
            actions.Controls.Add(btnEdit);
            actions.Controls.Add(btnCancel);

            // ─── Right info (Dock=Right): valor + estado dot ──────────
            var rightInfo = new Panel { Dock = DockStyle.Right, Width = 210, BackColor = idleBg, Padding = new Padding(0, 22, 16, 0) };
            var lblValor = new Label
            {
                Text = Theme.FormatEuro(valor),
                Font = new Font(Theme.FontBase.FontFamily, 14f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 30, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            };
            var estadoHolder = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = idleBg };
            var estadoPill = new StatusPill
            {
                Text = estado, Height = 22, BackColor = idleBg, Font = Theme.FontSub,
                Style = StatusPill.PillStyle.Dot,
            };
            estadoPill.SetColors(EstadoReservaBg(estado), EstadoReservaFg(estado));
            // Dock=Right + Width fixo → right-aligned dentro do holder.
            int eW = StatusPill.MeasureDotWidth(estado, Theme.FontSub);
            estadoPill.Dock  = DockStyle.Right;
            estadoPill.Width = eW;
            estadoHolder.Controls.Add(estadoPill);

            rightInfo.Controls.Add(estadoHolder);
            rightInfo.Controls.Add(lblValor);

            // ─── Middle (Dock=Fill): cliente + recurso/pess + horas ───
            var middle = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(12, 18, 12, 0) };
            var lblCliente = new Label
            {
                Text = cliente, Font = new Font(Theme.FontBase.FontFamily, 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            string pessTxt = numPess.HasValue ? $" · {numPess.Value} pess." : "";
            var lblRecurso = new Label
            {
                Text = $"{recurso} ({tipoRec}){pessTxt}",
                Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            string horaTxt;
            if (hi.HasValue && hf.HasValue)
                horaTxt = $"{hi.Value:hh\\:mm} – {hf.Value:hh\\:mm}";
            else
                horaTxt = "Dia completo";
            var lblHora = new Label
            {
                Text = horaTxt, Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 18, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            middle.Controls.Add(lblHora);
            middle.Controls.Add(lblRecurso);
            middle.Controls.Add(lblCliente);

            row.Controls.Add(middle);
            row.Controls.Add(rightInfo);
            row.Controls.Add(actions);
            row.Controls.Add(dateBlock);

            UcEspacos.HookHover(row, idleBg, hoverBg, btnEdit, btnCancel);
            UcEspacos.HookClick(row, btnEdit, btnCancel, () => OpenEditor(id));
            return row;
        }

        private static Color EstadoReservaBg(string estado)
        {
            switch (estado)
            {
                case "Pendente":   return Theme.StatusWarningBg;
                case "Confirmada": return Theme.StatusSuccessBg;
                case "Cancelada":  return Theme.StatusDangerBg;
                case "Concluida":  return Theme.StatusNeutralBg;
                default:            return Theme.StatusNeutralBg;
            }
        }
        private static Color EstadoReservaFg(string estado)
        {
            switch (estado)
            {
                case "Pendente":   return Theme.StatusWarningFg;
                case "Confirmada": return Theme.StatusSuccessFg;
                case "Cancelada":  return Theme.StatusDangerFg;
                case "Concluida":  return Theme.StatusNeutralFg;
                default:            return Theme.StatusNeutralFg;
            }
        }

        // ── Editor (mantido, com id opcional para edição futura) ────────
        private void OpenEditor(int? id = null)
        {
            // Por agora só new reservation; id reservado para edição futura.
            DataTable dsClientes, dsRecursos;
            using (var conn = Database.GetConnection())
            {
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    dsClientes = new DataTable();
                    adapter.Fill(dsClientes);
                }
                const string recSql = @"
                    SELECT recurso_id, label, tipo, preco FROM (
                        SELECT s.recurso_id,
                               e.nome + ' / Sala ' + s.nome AS label,
                               'Sala' AS tipo, s.preco_hora AS preco
                        FROM sala s JOIN espaco e ON s.espaco_id=e.espaco_id
                        WHERE s.estado='Disponivel'
                        UNION ALL
                        SELECT p.recurso_id,
                               e.nome + ' / Posto ' + p.codigo + ' (' + p.tipo_posto + ')' AS label,
                               'Posto' AS tipo, p.preco_dia AS preco
                        FROM posto p JOIN espaco e ON p.espaco_id=e.espaco_id
                        WHERE p.estado='Disponivel'
                    ) x ORDER BY label";
                using (var cmd = new SqlCommand(recSql, conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    dsRecursos = new DataTable();
                    adapter.Fill(dsRecursos);
                }
            }

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var cmbCliente = UcClientes.AddComboDataSource(tbl, "Cliente *", dsClientes, "nome", "cliente_id");
            var cmbRecurso = UcClientes.AddComboDataSource(tbl, "Recurso *", dsRecursos, "label", "recurso_id");
            var dtData     = UcClientes.AddDate(tbl, "Data *");
            var txtHIni    = UcClientes.AddField(tbl, "Hora início (HH:MM)");
            var txtHFim    = UcClientes.AddField(tbl, "Hora fim (HH:MM)");
            var txtParts   = UcClientes.AddField(tbl, "Nº Participantes");
            var txtNotas   = UcClientes.AddField(tbl, "Notas");
            var lblValor   = new Label { Text = "Valor: —", Font = Theme.FontSection, ForeColor = Theme.TextPrimary, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 8, 0, 8) };
            tbl.Controls.Add(lblValor);

            txtHIni.Text = "09:00";
            txtHFim.Text = "10:00";
            dtData.Value = DateTime.Today;

            decimal valorCalc = 0;
            string  tipoSel   = "";

            Action recalc = () =>
            {
                if (cmbRecurso.SelectedValue == null || cmbRecurso.SelectedValue is DBNull)
                { lblValor.Text = "Valor: —"; valorCalc = 0; return; }
                var rowView = (DataRowView)cmbRecurso.SelectedItem;
                tipoSel = rowView["tipo"].ToString();
                decimal preco = Convert.ToDecimal(rowView["preco"]);
                if (tipoSel == "Sala")
                {
                    txtHIni.Parent.Visible = true;
                    txtHFim.Parent.Visible = true;
                    txtParts.Parent.Visible = true;
                    if (TimeSpan.TryParse(txtHIni.Text.Trim(), out TimeSpan hi) &&
                        TimeSpan.TryParse(txtHFim.Text.Trim(), out TimeSpan hf) &&
                        hf > hi)
                        valorCalc = (decimal)(hf - hi).TotalHours * preco;
                    else valorCalc = 0;
                }
                else
                {
                    txtHIni.Parent.Visible = false;
                    txtHFim.Parent.Visible = false;
                    txtParts.Parent.Visible = false;
                    valorCalc = preco;
                }
                lblValor.Text = "Valor: " + Theme.FormatEuro(valorCalc);
            };

            cmbRecurso.SelectedIndexChanged += (s, e) => recalc();
            txtHIni.TextChanged += (s, e) => recalc();
            txtHFim.TextChanged += (s, e) => recalc();
            recalc();

            using (var dlg = new FormDialog("Nova Reserva", tbl, 460, () =>
            {
                if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull) throw new ApplicationException("Cliente é obrigatório.");
                if (cmbRecurso.SelectedValue == null || cmbRecurso.SelectedValue is DBNull) throw new ApplicationException("Recurso é obrigatório.");
                if (dtData.Value.Date < DateTime.Today) throw new ApplicationException("Data não pode ser no passado.");

                int recId = Convert.ToInt32(cmbRecurso.SelectedValue);
                int cliId = Convert.ToInt32(cmbCliente.SelectedValue);
                object pHIni = DBNull.Value, pHFim = DBNull.Value, pParts = DBNull.Value;
                decimal valor;

                if (tipoSel == "Sala")
                {
                    if (!TimeSpan.TryParse(txtHIni.Text.Trim(), out TimeSpan hi)) throw new ApplicationException("Hora início inválida (HH:MM).");
                    if (!TimeSpan.TryParse(txtHFim.Text.Trim(), out TimeSpan hf)) throw new ApplicationException("Hora fim inválida (HH:MM).");
                    if (hf <= hi) throw new ApplicationException("Hora fim deve ser depois de hora início.");
                    pHIni = hi;
                    pHFim = hf;
                    if (!string.IsNullOrWhiteSpace(txtParts.Text))
                    {
                        if (!int.TryParse(txtParts.Text, out int np) || np <= 0) throw new ApplicationException("Nº Participantes inválido.");
                        pParts = np;
                    }
                    var rowView = (DataRowView)cmbRecurso.SelectedItem;
                    decimal preco = Convert.ToDecimal(rowView["preco"]);
                    valor = (decimal)(hf - hi).TotalHours * preco;
                }
                else
                {
                    var rowView = (DataRowView)cmbRecurso.SelectedItem;
                    valor = Convert.ToDecimal(rowView["preco"]);
                }

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes, notas)
                      VALUES (@c,@r,@d,@hi,@hf,@v,'Pendente',@np,@n)", conn))
                {
                    cmd.Parameters.AddWithValue("@c", cliId);
                    cmd.Parameters.AddWithValue("@r", recId);
                    cmd.Parameters.AddWithValue("@d", dtData.Value.Date);
                    cmd.Parameters.AddWithValue("@hi", pHIni);
                    cmd.Parameters.AddWithValue("@hf", pHFim);
                    cmd.Parameters.AddWithValue("@v", valor);
                    cmd.Parameters.AddWithValue("@np", pParts);
                    cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(txtNotas.Text) ? (object)DBNull.Value : txtNotas.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void CancelarReserva(int id, bool canCancel)
        {
            if (!canCancel)
            {
                MessageBox.Show("Esta reserva não pode ser cancelada.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Cancelar a reserva selecionada?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("UPDATE reserva SET estado='Cancelada' WHERE reserva_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
