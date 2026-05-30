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
    public class UcPagamentos : UserControl
    {
        // KPIs
        private Label _kpiTotal, _kpiPagos, _kpiValor, _kpiPendentes;

        // Toolbar
        private ModernSelect _cmbCliente, _cmbEstado;
        private ModernButton _btnNovo;

        // Lista
        private ScrollableList _list;
        private Panel _empty;

        // Cache
        private DataTable _allRows;

        public UcPagamentos()
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
                Padding = Theme.PagePadding,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.RowHeightTitle));   // title
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.RowHeightToolbar));   // toolbar
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.RowHeightKpis));  // KPIs
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
            pnl.Controls.Add(new Label
            {
                Text = "Pagamentos", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        // ── Toolbar ─────────────────────────────────────────────────────
        private Control BuildToolbar()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
                Margin = Theme.ToolbarMarginBottom,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(16, 12, 16, 12) };

            _btnNovo = new ModernButton
            {
                Text = "+ Novo Pagamento", Style = ModernButton.Variant.Primary,
                Font = Theme.FontBold, Size = new Size(170, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                // Cliente não cria pagamentos manualmente — são gerados pelo
                // sistema/staff (snapshot de preço, validação financeira, etc).
                Visible = true,
            };
            _btnNovo.Click += (s, e) => OpenEditor(null);

            _cmbCliente = new ModernSelect { Width = 190, Height = 36, Margin = new Padding(0) };
            _cmbCliente.SelectedIndexChanged += (s, e) => RenderRows();

            _cmbEstado = new ModernSelect { Width = 140, Height = 36, Margin = new Padding(0) };
            _cmbEstado.AddItems("(Todos)", "Pendente", "Pago", "Cancelado", "Reembolsado");
            _cmbEstado.SelectedIndexChanged += (s, e) => RenderRows();

            var flow = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = Theme.CardBg, Padding = new Padding(0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            flow.Controls.Add(MakeFilterLabel("Cliente"));
            flow.Controls.Add(_cmbCliente);
            flow.Controls.Add(MakeFilterLabel("Estado"));
            flow.Controls.Add(_cmbEstado);

            inner.Controls.Add(flow);
            inner.Controls.Add(_btnNovo);
            card.Controls.Add(inner);

            void Relayout()
            {
                var dr = inner.DisplayRectangle;
                int btnY = dr.Y + (dr.Height - _btnNovo.Height) / 2;
                _btnNovo.Location = new Point(dr.Right - _btnNovo.Width, btnY);
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
            var font = new Font(Theme.FontBase.FontFamily, 9f, FontStyle.Bold);
            return new Label
            {
                Text = text, Font = font, ForeColor = Theme.TextMuted,
                AutoSize = false, Width = TextRenderer.MeasureText(text, font).Width + 4,
                Height = 36, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg,
                Margin = new Padding(text == "Cliente" ? 8 : 18, 0, 8, 0),
            };
        }

        // ── KPIs ────────────────────────────────────────────────────────
        private Control BuildKpis()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Theme.PageBg, Margin = Theme.ToolbarMarginBottom,
            };
            for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var k1 = BuildKpi("Total pagamentos", IconChar.CreditCard,   Theme.Accent,           out _kpiTotal);
            var k2 = BuildKpi("Pagos",            IconChar.CircleCheck,  Theme.StatusSuccessFg,  out _kpiPagos);
            var k3 = BuildKpi("Valor pago",       IconChar.EuroSign,     Theme.Accent,           out _kpiValor);
            var k4 = BuildKpi("Pendentes",        IconChar.Clock,        Theme.StatusWarningFg,  out _kpiPendentes);
            k4.Margin = Theme.KpiCardLast;
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
                Margin = Theme.KpiCardGap,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(18, 14, 18, 14) };

            var topLine = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Theme.CardBg };
            topLine.Controls.Add(new Label
            {
                Text = label, Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0),
            });
            topLine.Controls.Add(new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = iconColor,
                BackColor = Theme.CardBg, Dock = DockStyle.Left, Width = 22,
                SizeMode = PictureBoxSizeMode.CenterImage,
            });

            valueLbl = new Label
            {
                Text = "—", Font = new Font(Theme.FontBase.FontFamily, 24f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
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
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = Theme.ListInnerPadding };

            _list = new ScrollableList { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Visible = false };
            _list.Content.BackColor = Theme.CardBg;
            _list.Resize += (s, e) => RenderRows();

            _empty = BuildEmptyState("Sem pagamentos no filtro selecionado", IconChar.CreditCard);
            _empty.Dock = DockStyle.Fill;

            inner.Controls.Add(_list);
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
                if (iconImg == null)
                {
                    using (var pb = new IconPictureBox { IconChar = icon, IconSize = 44, IconColor = Theme.TextMuted })
                        if (pb.Image != null) iconImg = (Image)pb.Image.Clone();
                }
                var ts = TextRenderer.MeasureText(g, text, Theme.FontBase, Size.Empty, TextFormatFlags.NoPadding);
                int iconSize = 44, gap = 14, totalH = iconSize + gap + ts.Height;
                int startY = Math.Max(8, (pnl.Height - totalH) / 2);
                int iconX = (pnl.Width - iconSize) / 2;
                int textX = (pnl.Width - ts.Width) / 2;
                if (iconImg != null) g.DrawImage(iconImg, iconX, startY, iconSize, iconSize);
                TextRenderer.DrawText(g, text, Theme.FontBase, new Point(textX, startY + iconSize + gap),
                    Theme.TextMuted, TextFormatFlags.NoPadding);
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
            return pnl;
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
            try
            {
                string sql = @"
                    SELECT TOP 200 pg.pagamento_id AS id, c.nome AS cliente, c.cliente_id AS cliente_id,
                           CASE
                               WHEN pg.adesao_id IS NOT NULL THEN 'Adesão #' + CAST(pg.adesao_id AS varchar) + ' (' + pl.nome_plano + ')'
                               ELSE 'Reserva #' + CAST(pg.reserva_id AS varchar)
                           END AS servico,
                           pg.adesao_id AS adesao_id, pg.reserva_id AS reserva_id,
                           pg.data_pagamento AS data, pg.valor AS valor,
                           pg.metodo_pagamento AS metodo, pg.estado AS estado
                    FROM pagamento pg
                    JOIN cliente c    ON pg.cliente_id = c.cliente_id
                    LEFT JOIN adesao a ON pg.adesao_id = a.adesao_id
                    LEFT JOIN plano pl ON a.plano_id  = pl.plano_id
                    ORDER BY pg.data_pagamento DESC, pg.pagamento_id DESC";
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand(sql, conn))
                using (var da   = new SqlDataAdapter(cmd))
                {
                    _allRows = new DataTable();
                    da.Fill(_allRows);
                }
                RenderRows();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRows()
        {
            if (_allRows == null || _list == null) return;

            _list.Content.SuspendLayout();
            _list.Content.Controls.Clear();

            int total = _allRows.Rows.Count;
            int pagos = 0, pendentes = 0;
            decimal valorPago = 0;
            foreach (DataRow r in _allRows.Rows)
            {
                string estado = r["estado"].ToString();
                if (estado == "Pago")     { pagos++; valorPago += Convert.ToDecimal(r["valor"]); }
                if (estado == "Pendente") pendentes++;
            }
            _kpiTotal    .Text = total.ToString();
            _kpiPagos    .Text = pagos.ToString();
            _kpiValor    .Text = Theme.FormatEuro(valorPago);
            _kpiPendentes.Text = pendentes.ToString();

            string estadoFiltro = (_cmbEstado.SelectedIndex > 0) ? _cmbEstado.SelectedText : null;
            int? cliFiltro = (_cmbCliente.SelectedIndex > 0 && _cmbCliente.SelectedValue != null
                              && !(_cmbCliente.SelectedValue is DBNull))
                ? (int?)Convert.ToInt32(_cmbCliente.SelectedValue) : null;

            var rowsView = new List<DataRow>();
            foreach (DataRow r in _allRows.Rows)
            {
                if (estadoFiltro != null && r["estado"].ToString() != estadoFiltro) continue;
                if (cliFiltro.HasValue && Convert.ToInt32(r["cliente_id"]) != cliFiltro.Value) continue;
                rowsView.Add(r);
            }

            // Cap render — proteção contra Win32 handle exhaustion com seeds grandes.
            const int MaxRender = 80;
            int totalView = rowsView.Count;
            int rendered  = Math.Min(totalView, MaxRender);

            int y = 0;
            int width = Math.Max(600, _list.ClientSize.Width - 20);
            for (int idx = 0; idx < rendered; idx++)
            {
                var r = rowsView[idx];
                int id        = Convert.ToInt32(r["id"]);
                string cli    = r["cliente"].ToString();
                string svc    = r["servico"].ToString();
                DateTime data = Convert.ToDateTime(r["data"]);
                decimal valor = Convert.ToDecimal(r["valor"]);
                string metodo = r["metodo"].ToString();
                string estado = r["estado"].ToString();

                var card = BuildPagamentoCard(id, cli, svc, data, valor, metodo, estado);
                card.Location = new Point(0, y);
                card.Width    = width;
                card.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _list.Content.Controls.Add(card);
                y += card.Height + 8;
            }
            if (totalView > MaxRender)
            {
                var more = new Label
                {
                    Text = $"+ {totalView - MaxRender} pagamentos mais antigos não mostrados",
                    Font = Theme.FontSub, ForeColor = Theme.TextMuted,
                    BackColor = Theme.CardBg, Height = 30,
                    Location = new Point(0, y), Width = width,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                };
                _list.Content.Controls.Add(more);
                y += more.Height;
            }
            _list.Content.ResumeLayout();
            _list.UpdateLayout(y);

            _list .Visible = rowsView.Count > 0;
            _empty.Visible = rowsView.Count == 0;
            _empty.Invalidate();
        }

        // ── Card ────────────────────────────────────────────────────────
        private Control BuildPagamentoCard(int id, string cliente, string servico,
                                            DateTime data, decimal valor, string metodo, string estado)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = UcEspacos.MixColors(Theme.CardBg, Color.White, 0.05f);
            Color metodoColor = MetodoColor(metodo);

            var row = new Panel
            {
                Height = 96, BackColor = idleBg, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 8),
            };

            // ─── Esquerda: ícone do método em circle ──────────────────
            var leftBlock = new Panel { Dock = DockStyle.Left, Width = 76, BackColor = idleBg };
            Image img = null;
            using (var pb = new IconPictureBox
                   { IconChar = MetodoIcon(metodo), IconSize = 22, IconColor = Color.White })
                if (pb.Image != null) img = (Image)pb.Image.Clone();
            leftBlock.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                int diam = 44;
                int cx = (leftBlock.Width  - diam) / 2;
                int cy = (leftBlock.Height - diam) / 2;
                using (var br = new SolidBrush(metodoColor))
                    g.FillEllipse(br, cx, cy, diam, diam);
                if (img != null)
                {
                    int s2 = 22;
                    g.DrawImage(img,
                        cx + (diam - s2) / 2,
                        cy + (diam - s2) / 2 + 1,
                        s2, s2);
                }
            };
            leftBlock.Resize += (s, e) => leftBlock.Invalidate();

            // ─── Direita: ações ───────────────────────────────────────
            // Cliente: pagamentos são read-only (gerados pelo sistema). Esconde
            // edit/delete. O detalhe + recibo PDF continuam acessíveis via click.
            bool roCliente = false;
            var actions = new Panel { Dock = DockStyle.Right, Width = 90, BackColor = idleBg };
            var btnEdit = UcEspacos.MakeIconBtn(IconChar.Pen, Theme.Accent, idleBg, () => OpenEditor(id));
            var btnDel  = UcEspacos.MakeIconBtn(IconChar.TrashCan, Theme.StatusDangerFg, idleBg, () => EliminarPagamento(id));
            btnEdit.Location = new Point(8,  30);
            btnDel .Location = new Point(46, 30);
            actions.Controls.Add(btnEdit);
            actions.Controls.Add(btnDel);

            // ─── Right info: valor + estado dot ───────────────────────
            var rightInfo = new Panel { Dock = DockStyle.Right, Width = 230, BackColor = idleBg, Padding = new Padding(0, 22, 16, 0) };
            var lblValor = new Label
            {
                Text = Theme.FormatEuro(valor),
                Font = new Font(Theme.FontBase.FontFamily, 14f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 32, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            };
            var estadoHolder = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = idleBg };
            var estadoPill = new StatusPill
            {
                Text = estado, Height = 22, BackColor = idleBg, Font = Theme.FontSub,
                Style = StatusPill.PillStyle.Dot,
            };
            estadoPill.SetColors(EstadoBg(estado), EstadoFg(estado));
            int eW = StatusPill.MeasureDotWidth(estado, Theme.FontSub);
            estadoPill.Dock  = DockStyle.Right;
            estadoPill.Width = eW;
            estadoHolder.Controls.Add(estadoPill);
            rightInfo.Controls.Add(estadoHolder);
            rightInfo.Controls.Add(lblValor);

            // ─── Centro: cliente + serviço + meta (método · data) ─────
            var middle = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(12, 18, 12, 0) };
            var lblCliente = new Label
            {
                Text = cliente, Font = new Font(Theme.FontBase.FontFamily, 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblServico = new Label
            {
                Text = servico, Font = Theme.FontSub,
                ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblMeta = new Label
            {
                Text = $"{metodo} · {data:dd/MM/yyyy}",
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 18, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            middle.Controls.Add(lblMeta);
            middle.Controls.Add(lblServico);
            middle.Controls.Add(lblCliente);

            row.Controls.Add(middle);
            row.Controls.Add(rightInfo);
            row.Controls.Add(actions);
            row.Controls.Add(leftBlock);

            UcEspacos.HookHover(row, idleBg, hoverBg, btnEdit, btnDel);
            UcEspacos.HookClick(row, btnEdit, btnDel, () =>
                OpenPagamentoDetail(id, cliente, servico, data, valor, metodo, estado));
            return row;
        }

        // ── Detail (read-only) ──────────────────────────────────────────
        private void OpenPagamentoDetail(int id, string cliente, string servico,
                                          DateTime data, decimal valor, string metodo, string estado)
        {
            var body = new Panel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            Color metodoColor = MetodoColor(metodo);
            var header = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Theme.CardBg };
            Image img = null;
            using (var pb = new IconPictureBox { IconChar = MetodoIcon(metodo), IconSize = 24, IconColor = Color.White })
                if (pb.Image != null) img = (Image)pb.Image.Clone();
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                int diam = 56;
                int cx = 0, cy = (header.Height - diam) / 2;
                using (var br = new SolidBrush(metodoColor)) g.FillEllipse(br, cx, cy, diam, diam);
                if (img != null) g.DrawImage(img, cx + (diam - 24) / 2, cy + (diam - 24) / 2 + 1, 24, 24);
                using (var f = new Font(Theme.FontBase.FontFamily, 18f, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, Theme.FormatEuro(valor), f, new Point(diam + 14, cy + 4),
                        Theme.TextPrimary, TextFormatFlags.NoPadding);
                }
                TextRenderer.DrawText(g, $"{metodo} · {data:dd/MM/yyyy}", Theme.FontSub, new Point(diam + 14, cy + 38),
                    Theme.TextSecondary, TextFormatFlags.NoPadding);
            };

            body.Controls.Add(BuildDetailFieldP("Estado",  estado));
            body.Controls.Add(BuildDetailFieldP("Data",    data.ToString("dd/MM/yyyy")));
            body.Controls.Add(BuildDetailFieldP("Método",  metodo));
            body.Controls.Add(BuildDetailFieldP("Serviço", servico));
            body.Controls.Add(BuildDetailFieldP("Cliente", cliente));
            body.Controls.Add(header);

            using (var dlg = new FormDialog($"Pagamento #{id}", body, 500, onSave: null))
            {
                // Acção extra no footer: descarregar recibo PDF.
                var btnPdf = new ModernButton
                {
                    Text = "↓ Recibo PDF",
                    Style = ModernButton.Variant.Primary,
                    Font = Theme.FontBold,
                    Size = new Size(160, 36),
                    Margin = new Padding(12, 0, 0, 0),
                };
                btnPdf.Click += (s, e) => GerarReciboPdf(id);
                dlg.AddFooterAction(btnPdf);
                dlg.ShowDialog(FindForm());
            }
        }

        private void GerarReciboPdf(int pagamentoId)
        {
            try
            {
                var data = ReciboPdf.Fetch(pagamentoId);
                if (data == null)
                {
                    MessageBox.Show("Pagamento não encontrado.", "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                using (var sfd = new SaveFileDialog
                {
                    Title = "Guardar recibo PDF",
                    Filter = "PDF (*.pdf)|*.pdf",
                    FileName = ReciboPdf.SuggestFilename(data),
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                })
                {
                    if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;
                    ReciboPdf.Generate(data, sfd.FileName);
                    var ans = MessageBox.Show(
                        $"Recibo gerado em:\n{sfd.FileName}\n\nAbrir agora?",
                        "Recibo gerado", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (ans == DialogResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = sfd.FileName,
                                UseShellExecute = true,
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Não foi possível abrir o ficheiro: " + ex.Message,
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao gerar PDF: " + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Panel BuildDetailFieldP(string label, string value)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Theme.CardBg, Padding = new Padding(0, 8, 0, 0) };
            pnl.Controls.Add(new Label
            {
                Text = value, Font = Theme.FontBase, ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            pnl.Controls.Add(new Label
            {
                Text = label.ToUpper(), Font = Theme.FontMicro, ForeColor = Theme.TextMuted, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = 16, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        private static Color MetodoColor(string metodo)
        {
            switch (metodo)
            {
                case "Dinheiro":      return Theme.StatusSuccessFg;
                case "Cartao":        return Theme.Accent;
                case "Transferencia": return ColorTranslator.FromHtml("#06b6d4");
                case "MBWay":         return ColorTranslator.FromHtml("#fb923c");
                case "PayPal":        return ColorTranslator.FromHtml("#8b5cf6");
                default:              return Theme.Accent;
            }
        }
        private static IconChar MetodoIcon(string metodo)
        {
            switch (metodo)
            {
                case "Dinheiro":      return IconChar.MoneyBill;
                case "Cartao":        return IconChar.CreditCard;
                case "Transferencia": return IconChar.BuildingColumns;
                case "MBWay":         return IconChar.MobileScreen;
                case "PayPal":        return IconChar.Paypal;
                default:              return IconChar.CreditCard;
            }
        }

        private static Color EstadoBg(string estado)
        {
            switch (estado)
            {
                case "Pendente":    return Theme.StatusWarningBg;
                case "Pago":        return Theme.StatusSuccessBg;
                case "Cancelado":   return Theme.StatusDangerBg;
                case "Reembolsado": return Theme.StatusNeutralBg;
                default:             return Theme.StatusNeutralBg;
            }
        }
        private static Color EstadoFg(string estado)
        {
            switch (estado)
            {
                case "Pendente":    return Theme.StatusWarningFg;
                case "Pago":        return Theme.StatusSuccessFg;
                case "Cancelado":   return Theme.StatusDangerFg;
                case "Reembolsado": return Theme.StatusNeutralFg;
                default:             return Theme.StatusNeutralFg;
            }
        }

        // ── Helpers / Editor (mantido) ─────────────────────────────────
        private DataTable LoadServicosPorCliente(int clienteId)
        {
            const string sql = @"
                SELECT 'A:' + CAST(a.adesao_id AS varchar) AS value,
                       'Adesão #' + CAST(a.adesao_id AS varchar) + ' — ' + pl.nome_plano +
                            ' (' + FORMAT(a.preco_acordado, '0.00') + ' €)' AS label,
                       a.preco_acordado AS preco
                FROM adesao a JOIN plano pl ON a.plano_id=pl.plano_id
                WHERE a.cliente_id=@c
                UNION ALL
                SELECT 'R:' + CAST(r.reserva_id AS varchar),
                       'Reserva #' + CAST(r.reserva_id AS varchar) + ' — ' +
                            CONVERT(varchar,r.data_reserva,103) +
                            ' (' + FORMAT(r.valor, '0.00') + ' €)',
                       r.valor
                FROM reserva r WHERE r.cliente_id=@c
                ORDER BY value";
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@c", clienteId);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        private void OpenEditor(int? id)
        {
            DataTable dsClientes;
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                dsClientes = new DataTable();
                adapter.Fill(dsClientes);
            }

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var cmbCliente = UcClientes.AddModernSelectDataSource(tbl, "Cliente *", dsClientes, "nome", "cliente_id");
            var cmbServico = UcClientes.AddModernSelect(tbl, "Serviço *", new string[0]);
            var dtData     = UcClientes.AddModernDateField(tbl, "Data pagamento *");
            var txtValor   = UcClientes.AddField(tbl, "Valor *", IconChar.EuroSign, placeholder: "120.00");
            var cmbMetodo  = UcClientes.AddModernSelect(tbl, "Método *", new[] { "Dinheiro", "Cartao", "Transferencia", "MBWay", "PayPal" });
            var cmbEstado  = UcClientes.AddModernSelect(tbl, "Estado *", new[] { "Pendente", "Pago", "Cancelado", "Reembolsado" });

            cmbCliente.SelectedIndexChanged += (s, e) =>
            {
                if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull) return;
                var dt = LoadServicosPorCliente(Convert.ToInt32(cmbCliente.SelectedValue));
                cmbServico.BindDataTable(dt, "label", "value");
            };
            cmbServico.SelectedIndexChanged += (s, e) =>
            {
                var row = cmbServico.SelectedRawData as DataRow;
                if (row != null)
                    txtValor.Text = Convert.ToDecimal(row["preco"]).ToString(CultureInfo.InvariantCulture);
            };

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT cliente_id, adesao_id, reserva_id, data_pagamento, valor, metodo_pagamento, estado
                      FROM pagamento WHERE pagamento_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            cmbCliente.SelectedValue = r["cliente_id"];
                            string svcKey = r["adesao_id"] != DBNull.Value
                                ? "A:" + Convert.ToInt32(r["adesao_id"])
                                : "R:" + Convert.ToInt32(r["reserva_id"]);
                            cmbServico.SelectedValue = svcKey;
                            dtData.Value  = Convert.ToDateTime(r["data_pagamento"]);
                            txtValor.Text = Convert.ToDecimal(r["valor"]).ToString(CultureInfo.InvariantCulture);
                            cmbMetodo.SelectByDisplay(r["metodo_pagamento"].ToString());
                            cmbEstado.SelectByDisplay(r["estado"].ToString());
                        }
                    }
                }
            }
            else
            {
                cmbMetodo.SelectedIndex = 0;
                cmbEstado.SelectByDisplay("Pago");
                if (cmbCliente.Count > 0)
                {
                    int idx0 = cmbCliente.SelectedIndex;
                    cmbCliente.SelectedIndex = -1;
                    cmbCliente.SelectedIndex = idx0 >= 0 ? idx0 : 0;
                }
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Pagamento" : "Novo Pagamento", tbl, 500, () =>
            {
                if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull) throw new ApplicationException("Cliente é obrigatório.");
                if (cmbServico.SelectedValue == null) throw new ApplicationException("Serviço é obrigatório.");
                if (!decimal.TryParse(txtValor.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) || valor <= 0)
                    throw new ApplicationException("Valor inválido (> 0).");

                string svc = cmbServico.SelectedValue.ToString();
                object adesaoVal = DBNull.Value, reservaVal = DBNull.Value;
                if (svc.StartsWith("A:"))      adesaoVal  = int.Parse(svc.Substring(2));
                else if (svc.StartsWith("R:")) reservaVal = int.Parse(svc.Substring(2));

                // snapshot do preço: lido sempre do serviço; trigger T6 valida.
                const string snapshotExpr = @"COALESCE(
                        (SELECT preco_acordado FROM adesao  WHERE adesao_id  = @a),
                        (SELECT valor          FROM reserva WHERE reserva_id = @r))";
                var sql = id.HasValue
                    ? $@"UPDATE pagamento
                            SET cliente_id=@c, adesao_id=@a, reserva_id=@r,
                                data_pagamento=@d, valor=@v,
                                preco_servico_snapshot={snapshotExpr},
                                metodo_pagamento=@m, estado=@e
                          WHERE pagamento_id=@id"
                    : $@"INSERT INTO pagamento
                            (cliente_id, adesao_id, reserva_id, data_pagamento,
                             valor, preco_servico_snapshot, metodo_pagamento, estado)
                          VALUES (@c,@a,@r,@d,@v,{snapshotExpr},@m,@e)";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@c", Convert.ToInt32(cmbCliente.SelectedValue));
                    cmd.Parameters.AddWithValue("@a", adesaoVal);
                    cmd.Parameters.AddWithValue("@r", reservaVal);
                    cmd.Parameters.AddWithValue("@d", dtData.Value.Date);
                    cmd.Parameters.AddWithValue("@v", valor);
                    cmd.Parameters.AddWithValue("@m", cmbMetodo.SelectedText);
                    cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedText);
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void EliminarPagamento(int id)
        {
            if (MessageBox.Show("Eliminar pagamento?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM pagamento WHERE pagamento_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
