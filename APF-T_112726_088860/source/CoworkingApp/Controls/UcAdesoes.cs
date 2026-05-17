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
    public class UcAdesoes : UserControl
    {
        // KPIs
        private Label _kpiTotal, _kpiAtivas, _kpiReceita, _kpiPendentes;

        // Toolbar
        private ModernSelect _cmbCliente, _cmbEstado;
        private ModernButton _btnNova;

        // Lista
        private ScrollableList _list;
        private Panel _empty;

        // Cache
        private DataTable _allRows;

        public UcAdesoes()
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
            pnl.Controls.Add(new Label
            {
                Text = "Adesões", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
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
                Margin = new Padding(0, 0, 0, 12),
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(16, 12, 16, 12) };

            // Botão Nova (direita)
            _btnNova = new ModernButton
            {
                Text = "+ Nova Adesão", Style = ModernButton.Variant.Primary,
                Font = Theme.FontBold, Size = new Size(150, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnNova.Click += (s, e) => OpenEditor(null);

            // Filtros (esquerda)
            _cmbCliente = new ModernSelect { Width = 190, Height = 36, Margin = new Padding(0) };
            _cmbCliente.SelectedIndexChanged += (s, e) => RenderRows();

            _cmbEstado = new ModernSelect { Width = 140, Height = 36, Margin = new Padding(0) };
            _cmbEstado.AddItems("(Todos)", "Pendente", "Ativa", "Suspensa", "Cancelada", "Terminada");
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
            inner.Controls.Add(_btnNova);
            card.Controls.Add(inner);

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
                AutoSize = true, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg,
                Margin = new Padding(text == "Cliente" ? 0 : 18, 14, 8, 0),
            };
        }

        // ── KPIs ────────────────────────────────────────────────────────
        private Control BuildKpis()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Theme.PageBg, Margin = new Padding(0, 0, 0, 12),
            };
            for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var k1 = BuildKpi("Total adesões",  IconChar.Star,         Theme.Accent,           out _kpiTotal);
            var k2 = BuildKpi("Ativas",         IconChar.CircleCheck,  Theme.StatusSuccessFg,  out _kpiAtivas);
            var k3 = BuildKpi("Receita mensal", IconChar.EuroSign,     Theme.Accent,           out _kpiReceita);
            var k4 = BuildKpi("Pendentes",      IconChar.Clock,        Theme.StatusWarningFg,  out _kpiPendentes);
            k4.Margin = new Padding(0);
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
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(10) };

            _list = new ScrollableList { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Visible = false };
            _list.Content.BackColor = Theme.CardBg;
            _list.Resize += (s, e) => RenderRows();

            _empty = BuildEmptyState("Sem adesões no filtro selecionado", IconChar.Star);
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
                    SELECT a.adesao_id AS id, c.nome AS cliente, c.cliente_id AS cliente_id,
                           p.nome_plano AS plano, p.tipo_plano AS tipo,
                           CASE WHEN a.recurso_id IS NULL THEN '—'
                                ELSE COALESCE(po.codigo, 'Sala ' + s.nome) END AS posto,
                           a.data_inicio AS dini, a.data_fim AS dfim,
                           a.preco_acordado AS preco, a.estado AS estado
                    FROM adesao a
                    JOIN cliente c    ON a.cliente_id = c.cliente_id
                    JOIN plano p      ON a.plano_id = p.plano_id
                    LEFT JOIN posto po ON a.recurso_id = po.recurso_id
                    LEFT JOIN sala s  ON a.recurso_id = s.recurso_id
                    ORDER BY a.data_inicio DESC";
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

            // KPIs sobre TUDO (não sobre o filtrado)
            int total = _allRows.Rows.Count;
            int ativas = 0, pendentes = 0;
            decimal receita = 0;
            foreach (DataRow r in _allRows.Rows)
            {
                string estado = r["estado"].ToString();
                if (estado == "Ativa")     { ativas++;    receita += Convert.ToDecimal(r["preco"]); }
                if (estado == "Pendente")  { pendentes++; }
            }
            _kpiTotal    .Text = total.ToString();
            _kpiAtivas   .Text = ativas.ToString();
            _kpiReceita  .Text = Theme.FormatEuro(receita);
            _kpiPendentes.Text = pendentes.ToString();

            // Filter
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

            int y = 0;
            int width = Math.Max(600, _list.ClientSize.Width - 20);
            foreach (var r in rowsView)
            {
                int id        = Convert.ToInt32(r["id"]);
                string cli    = r["cliente"].ToString();
                string plano  = r["plano"].ToString();
                string tipo   = r["tipo"].ToString();
                string posto  = r["posto"].ToString();
                DateTime dini = Convert.ToDateTime(r["dini"]);
                DateTime? dfim = r["dfim"] is DBNull ? (DateTime?)null : Convert.ToDateTime(r["dfim"]);
                decimal preco = Convert.ToDecimal(r["preco"]);
                string estado = r["estado"].ToString();

                var card = BuildAdesaoCard(id, cli, plano, tipo, posto, dini, dfim, preco, estado);
                card.Location = new Point(0, y);
                card.Width    = width;
                card.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _list.Content.Controls.Add(card);
                y += card.Height + 8;
            }
            _list.Content.ResumeLayout();
            _list.UpdateLayout(y);

            _list .Visible = rowsView.Count > 0;
            _empty.Visible = rowsView.Count == 0;
            _empty.Invalidate();
        }

        // ── Card ────────────────────────────────────────────────────────
        private Control BuildAdesaoCard(int id, string cliente, string plano, string tipo,
                                         string posto, DateTime dini, DateTime? dfim,
                                         decimal preco, string estado)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = UcEspacos.MixColors(Theme.CardBg, Color.White, 0.05f);
            Color tipoColor = TipoColor(tipo);

            var row = new Panel
            {
                Height = 96, BackColor = idleBg, Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 8),
            };

            // ─── Esquerda: bloco tipo plano (ícone + nome tipo) ───────
            var leftBlock = new Panel { Dock = DockStyle.Left, Width = 84, BackColor = idleBg };
            Image img = null;
            using (var pb = new IconPictureBox
                   { IconChar = TipoIcon(tipo), IconSize = 22, IconColor = Color.White })
                if (pb.Image != null) img = (Image)pb.Image.Clone();
            leftBlock.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int diam = 44;
                int cx = (leftBlock.Width - diam) / 2;
                int cy = (leftBlock.Height - diam) / 2 - 8;
                using (var br = new SolidBrush(tipoColor))
                    g.FillEllipse(br, cx, cy, diam, diam);
                if (img != null)
                {
                    int s2 = 22;
                    g.DrawImage(img,
                        cx + (diam - s2) / 2,
                        cy + (diam - s2) / 2 + 1,
                        s2, s2);
                }
                // Label tipo embaixo
                var tipoSize = TextRenderer.MeasureText(g, tipo, Theme.FontMicro, Size.Empty, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, tipo, Theme.FontMicro,
                    new Point((leftBlock.Width - tipoSize.Width) / 2, cy + diam + 4),
                    tipoColor, TextFormatFlags.NoPadding);
            };
            leftBlock.Resize += (s, e) => leftBlock.Invalidate();

            // ─── Direita: ações ───────────────────────────────────────
            var actions = new Panel { Dock = DockStyle.Right, Width = 90, BackColor = idleBg };
            var btnEdit = UcEspacos.MakeIconBtn(IconChar.Pen, Theme.Accent, idleBg, () => OpenEditor(id));
            var btnDel  = UcEspacos.MakeIconBtn(IconChar.TrashCan, Theme.StatusDangerFg, idleBg, () => EliminarAdesao(id));
            btnEdit.Location = new Point(8, 30);
            btnDel .Location = new Point(46, 30);
            actions.Controls.Add(btnEdit);
            actions.Controls.Add(btnDel);

            // ─── Right info: preço + estado ───────────────────────────
            var rightInfo = new Panel { Dock = DockStyle.Right, Width = 200, BackColor = idleBg, Padding = new Padding(0, 22, 16, 0) };
            var lblPreco = new Label
            {
                Text = Theme.FormatEuro(preco) + " /mês",
                Font = new Font(Theme.FontBase.FontFamily, 14f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            };
            var estadoHolder = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = idleBg };
            var estadoPill = new StatusPill
            {
                Text = estado, Height = 22, BackColor = idleBg, Font = Theme.FontSub,
                Style = StatusPill.PillStyle.Dot,
            };
            estadoPill.SetColors(EstadoBg(estado), EstadoFg(estado));
            int eW = 8 + 8 + TextRenderer.MeasureText(estado, Theme.FontSub).Width + 4;
            estadoPill.Dock  = DockStyle.Right;
            estadoPill.Width = eW;
            estadoHolder.Controls.Add(estadoPill);

            rightInfo.Controls.Add(estadoHolder);
            rightInfo.Controls.Add(lblPreco);

            // ─── Centro: cliente + plano + posto + período ────────────
            var middle = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(12, 18, 12, 0) };
            var lblCliente = new Label
            {
                Text = cliente, Font = new Font(Theme.FontBase.FontFamily, 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            string subLine = posto == "—" ? plano : $"{plano} · Posto {posto}";
            var lblPlano = new Label
            {
                Text = subLine, Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            string periodoTxt = dfim.HasValue
                ? $"{dini:dd/MM/yyyy} → {dfim.Value:dd/MM/yyyy}"
                : $"desde {dini:dd/MM/yyyy}";
            var lblPeriodo = new Label
            {
                Text = periodoTxt, Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 18, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            middle.Controls.Add(lblPeriodo);
            middle.Controls.Add(lblPlano);
            middle.Controls.Add(lblCliente);

            row.Controls.Add(middle);
            row.Controls.Add(rightInfo);
            row.Controls.Add(actions);
            row.Controls.Add(leftBlock);

            UcEspacos.HookHover(row, idleBg, hoverBg, btnEdit, btnDel);
            UcEspacos.HookClick(row, btnEdit, btnDel, () => OpenEditor(id));
            return row;
        }

        private static Color TipoColor(string tipo)
        {
            if (tipo == "Flex")    return ColorTranslator.FromHtml("#06b6d4");
            if (tipo == "Fixo")    return Theme.Accent;
            if (tipo == "Privado") return ColorTranslator.FromHtml("#8b5cf6");
            return Theme.Accent;
        }
        private static IconChar TipoIcon(string tipo)
        {
            if (tipo == "Flex")    return IconChar.PersonRunning;
            if (tipo == "Fixo")    return IconChar.Chair;
            if (tipo == "Privado") return IconChar.DoorClosed;
            return IconChar.Star;
        }

        private static Color EstadoBg(string estado)
        {
            switch (estado)
            {
                case "Pendente":   return Theme.StatusWarningBg;
                case "Ativa":      return Theme.StatusSuccessBg;
                case "Suspensa":   return Theme.StatusOrangeBg;
                case "Cancelada":  return Theme.StatusDangerBg;
                case "Terminada":  return Theme.StatusNeutralBg;
                default:            return Theme.StatusNeutralBg;
            }
        }
        private static Color EstadoFg(string estado)
        {
            switch (estado)
            {
                case "Pendente":   return Theme.StatusWarningFg;
                case "Ativa":      return Theme.StatusSuccessFg;
                case "Suspensa":   return Theme.StatusOrangeFg;
                case "Cancelada":  return Theme.StatusDangerFg;
                case "Terminada":  return Theme.StatusNeutralFg;
                default:            return Theme.StatusNeutralFg;
            }
        }

        // ── Editor (mantido — só refactor cosmético) ────────────────────
        private void OpenEditor(int? id)
        {
            DataTable dsClientes, dsPlanos;
            using (var conn = Database.GetConnection())
            {
                using (var c = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var a = new SqlDataAdapter(c)) { dsClientes = new DataTable(); a.Fill(dsClientes); }
                using (var c = new SqlCommand("SELECT plano_id, nome_plano, tipo_plano, preco_mensal FROM plano ORDER BY nome_plano", conn))
                using (var a = new SqlDataAdapter(c)) { dsPlanos = new DataTable(); a.Fill(dsPlanos); }
            }

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var cmbCliente = UcClientes.AddComboDataSource(tbl, "Cliente *", dsClientes, "nome", "cliente_id");
            var cmbPlano   = UcClientes.AddComboDataSource(tbl, "Plano *", dsPlanos, "nome_plano", "plano_id");
            var dtInicio   = UcClientes.AddDate(tbl, "Data início *");
            var cmbPosto   = UcClientes.AddCombo(tbl, "Posto atribuído *", new string[0]);
            var txtPreco   = UcClientes.AddField(tbl, "Preço acordado *");
            var cmbEstado  = UcClientes.AddCombo(tbl, "Estado *", new[] { "Pendente", "Ativa", "Suspensa", "Cancelada", "Terminada" });
            cmbEstado.SelectedIndex = 0;

            cmbPosto.DisplayMember = "label";
            cmbPosto.ValueMember   = "recurso_id";

            Action<string> loadPostos = (tipo) =>
            {
                using (var conn = Database.GetConnection())
                using (var c = new SqlCommand(
                    "SELECT p.recurso_id, e.nome + ' / ' + p.codigo AS label FROM posto p JOIN espaco e ON p.espaco_id=e.espaco_id WHERE p.tipo_posto=@t AND p.estado='Disponivel' ORDER BY label", conn))
                using (var a = new SqlDataAdapter(c))
                {
                    c.Parameters.AddWithValue("@t", tipo);
                    var dt = new DataTable();
                    a.Fill(dt);
                    cmbPosto.DataSource = dt;
                }
            };

            string tipoPlanoSel = "Flex";
            decimal precoMensalSel = 0;

            cmbPlano.SelectedIndexChanged += (s, e) =>
            {
                if (cmbPlano.SelectedItem == null) return;
                var rv = (DataRowView)cmbPlano.SelectedItem;
                tipoPlanoSel   = rv["tipo_plano"].ToString();
                precoMensalSel = Convert.ToDecimal(rv["preco_mensal"]);
                txtPreco.Text  = precoMensalSel.ToString(CultureInfo.InvariantCulture);
                if (tipoPlanoSel == "Flex")
                {
                    cmbPosto.Parent.Visible = false;
                    cmbPosto.DataSource = null;
                }
                else
                {
                    cmbPosto.Parent.Visible = true;
                    loadPostos(tipoPlanoSel);
                }
            };

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT a.cliente_id, a.plano_id, a.recurso_id, a.data_inicio, a.preco_acordado, a.estado, p.tipo_plano, p.preco_mensal
                      FROM adesao a JOIN plano p ON a.plano_id=p.plano_id WHERE a.adesao_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            cmbCliente.SelectedValue = r["cliente_id"];
                            cmbPlano.SelectedValue   = r["plano_id"];
                            tipoPlanoSel = r["tipo_plano"].ToString();
                            if (tipoPlanoSel != "Flex" && r["recurso_id"] != DBNull.Value)
                                cmbPosto.SelectedValue = r["recurso_id"];
                            dtInicio.Value = Convert.ToDateTime(r["data_inicio"]);
                            txtPreco.Text  = Convert.ToDecimal(r["preco_acordado"]).ToString(CultureInfo.InvariantCulture);
                            var estado = r["estado"].ToString();
                            var idx = cmbEstado.Items.IndexOf(estado);
                            cmbEstado.SelectedIndex = idx >= 0 ? idx : 0;
                        }
                    }
                }
            }
            else if (cmbPlano.Items.Count > 0)
            {
                cmbPlano.SelectedIndex = -1;
                cmbPlano.SelectedIndex = 0;
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Adesão" : "Nova Adesão", tbl, 460, () =>
            {
                if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull) throw new ApplicationException("Cliente é obrigatório.");
                if (cmbPlano.SelectedValue   == null || cmbPlano.SelectedValue   is DBNull) throw new ApplicationException("Plano é obrigatório.");
                if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0)
                    throw new ApplicationException("Preço acordado inválido.");

                object recursoVal = DBNull.Value;
                if (tipoPlanoSel != "Flex")
                {
                    if (cmbPosto.SelectedValue == null || cmbPosto.SelectedValue is DBNull)
                        throw new ApplicationException("Posto atribuído é obrigatório para planos Fixo/Privado.");
                    recursoVal = Convert.ToInt32(cmbPosto.SelectedValue);
                }

                var sql = id.HasValue
                    ? "UPDATE adesao SET cliente_id=@c, plano_id=@p, recurso_id=@r, data_inicio=@d, preco_acordado=@pr, estado=@e WHERE adesao_id=@id"
                    : "INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado) VALUES (@c,@p,@r,@d,@pr,@e)";
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@c",  Convert.ToInt32(cmbCliente.SelectedValue));
                    cmd.Parameters.AddWithValue("@p",  Convert.ToInt32(cmbPlano.SelectedValue));
                    cmd.Parameters.AddWithValue("@r",  recursoVal);
                    cmd.Parameters.AddWithValue("@d",  dtInicio.Value.Date);
                    cmd.Parameters.AddWithValue("@pr", preco);
                    cmd.Parameters.AddWithValue("@e",  cmbEstado.SelectedItem.ToString());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void EliminarAdesao(int id)
        {
            if (MessageBox.Show("Eliminar adesão?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM pagamento WHERE adesao_id=@id", conn))
                    {
                        chk.Parameters.AddWithValue("@id", id);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — adesão tem pagamentos.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM adesao WHERE adesao_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
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
