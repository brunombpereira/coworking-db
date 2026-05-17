using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;

namespace CoworkingApp.Controls
{
    public class UcRelatorios : UserControl
    {
        private enum Tab { Disponibilidade, Cliente, Analise }
        private Tab _active = Tab.Disponibilidade;
        private TabButton _tabDisp, _tabCli, _tabAna;
        private Panel _content;

        // Tab 1
        private SegmentedControl _segTipo;
        private ModernDateField  _dtDispData;
        private ModernSelect     _hIni, _hFim;
        private ModernButton     _btnPesqDisp;
        private ScrollableList   _listDisp;
        private Panel            _dispEmpty;
        private Label            _dispHeader;

        // Tab 2
        private ModernSelect     _cmbCliCli;
        private Label            _kpiCliReservas, _kpiCliPago, _kpiCliUltima;
        private ScrollableList   _listCliReservas, _listCliPagamentos;
        private TableLayoutPanel _cliListGrid;
        private Panel            _cliEmpty;

        // Tab 3
        private ModernDateField  _dtAnaIni, _dtAnaFim;
        private ModernButton     _btnAnaAplicar;
        private Label            _kpiAnaReceita, _kpiAnaReservas, _kpiAnaPagos;
        private Chart            _chartReceitaMensal;
        private Chart            _chartOcupacao;
        private Chart            _chartMetodos;

        private bool _anaLoaded = false;

        public UcRelatorios()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadClientesCombo();
            // Auto-load Disponibilidade ao abrir o UC (após handle criado).
            HandleCreated += (s, e) => { try { LoadDispData(); } catch { /* sem BD não rebenta */ } };
        }

        // ── BUILD UI ────────────────────────────────────────────────────
        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.PageBg,
                Padding = new Padding(20, 16, 20, 16),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // title
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // tab bar
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // content

            root.Controls.Add(BuildTitle(),  0, 0);
            root.Controls.Add(BuildTabBar(), 0, 1);
            root.Controls.Add(BuildContent(), 0, 2);

            Controls.Add(root);
            SwitchTab(Tab.Disponibilidade);
        }

        private Control BuildTitle()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            pnl.Controls.Add(new Label
            {
                Text = "Relatórios", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        private Control BuildTabBar()
        {
            var bar = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            // Linha divisória inferior
            bar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.CardBorder, 1f))
                    e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            };

            _tabDisp = new TabButton { Text = "Disponibilidade", Icon = IconChar.MagnifyingGlass, Width = 170, Location = new Point(0, 8) };
            _tabCli  = new TabButton { Text = "Por Cliente",     Icon = IconChar.User,            Width = 150, Location = new Point(180, 8) };
            _tabAna  = new TabButton { Text = "Análise",         Icon = IconChar.ChartLine,       Width = 130, Location = new Point(340, 8) };

            _tabDisp.Click += (s, e) => SwitchTab(Tab.Disponibilidade);
            _tabCli .Click += (s, e) => SwitchTab(Tab.Cliente);
            _tabAna .Click += (s, e) => SwitchTab(Tab.Analise);

            bar.Controls.Add(_tabDisp);
            bar.Controls.Add(_tabCli);
            bar.Controls.Add(_tabAna);
            return bar;
        }

        private Control BuildContent()
        {
            _content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg, Padding = new Padding(0, 12, 0, 0) };
            _content.Controls.Add(BuildTabDisp());
            _content.Controls.Add(BuildTabCli());
            _content.Controls.Add(BuildTabAna());
            return _content;
        }

        private void SwitchTab(Tab t)
        {
            _active = t;
            _tabDisp.Active = (t == Tab.Disponibilidade);
            _tabCli .Active = (t == Tab.Cliente);
            _tabAna .Active = (t == Tab.Analise);
            foreach (Control c in _content.Controls)
            {
                if (c.Name == "tabDisp") c.Visible = (t == Tab.Disponibilidade);
                if (c.Name == "tabCli")  c.Visible = (t == Tab.Cliente);
                if (c.Name == "tabAna")  c.Visible = (t == Tab.Analise);
            }
            // Lazy-load Análise na primeira visita
            if (t == Tab.Analise && !_anaLoaded)
            {
                try { LoadAnaData(); _anaLoaded = true; } catch { /* sem BD */ }
            }
        }

        // ─── TAB 1: Disponibilidade ─────────────────────────────────────
        private Control BuildTabDisp()
        {
            var card = new ModernCard
            {
                Name = "tabDisp", Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
            };
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.CardBg,
                Padding = new Padding(16, 14, 16, 16),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));  // filtros
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // header contextual
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // lista

            // ─── Filtros (Locations sem overlap) ──────────────────────
            var filterRow = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            _segTipo = new SegmentedControl
            {
                Segments = new[] { "Sala", "Posto" }, SelectedIndex = 0,
                Width = 160, Height = 36, Location = new Point(0, 10),
            };
            _dtDispData = new ModernDateField
            {
                Width = 130, Height = 36, Value = DateTime.Today,
                Location = new Point(176, 10),
            };
            _hIni = MakeHora("09:00", 322);
            _hFim = MakeHora("10:00", 422);
            _btnPesqDisp = new ModernButton
            {
                Text = "Pesquisar", Style = ModernButton.Variant.Primary, Font = Theme.FontBold,
                Size = new Size(120, 40), Location = new Point(522, 8),
            };
            _btnPesqDisp.Click += (s, e) => LoadDispData();
            filterRow.Controls.AddRange(new Control[] { _segTipo, _dtDispData, _hIni, _hFim, _btnPesqDisp });

            // ─── Header contextual (em vez de KPI separado)
            _dispHeader = new Label
            {
                Text = "Define os filtros e clica em Pesquisar",
                Font = Theme.FontSection, ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 6, 0, 4),
            };

            // ─── Lista ───
            var listHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            _listDisp = new ScrollableList { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Visible = false };
            _listDisp.Content.BackColor = Theme.CardBg;
            _dispEmpty = BuildEmptyState("Clica em 'Pesquisar' para listar os recursos disponíveis", IconChar.MagnifyingGlass);
            _dispEmpty.Dock = DockStyle.Fill;
            listHost.Controls.Add(_listDisp);
            listHost.Controls.Add(_dispEmpty);

            root.Controls.Add(filterRow,   0, 0);
            root.Controls.Add(_dispHeader, 0, 1);
            root.Controls.Add(listHost,    0, 2);
            card.Controls.Add(root);
            return card;
        }

        private ModernSelect MakeHora(string defaultVal, int x)
        {
            var sel = new ModernSelect { Width = 84, Height = 36, Location = new Point(x, 10) };
            for (int h = 0; h < 24; h++)
                sel.AddItems($"{h:00}:00", $"{h:00}:30");
            sel.SelectByDisplay(defaultVal);
            return sel;
        }

        private void LoadDispData()
        {
            bool isSala = (_segTipo.SelectedIndex == 0);
            string hi = _hIni.SelectedText;
            string hf = _hFim.SelectedText;
            DateTime data = _dtDispData.Value.Date;
            if (string.Compare(hi, hf, StringComparison.Ordinal) >= 0)
            {
                MessageBox.Show("Hora fim deve ser posterior à hora início.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sql = isSala
                ? @"SELECT s.recurso_id AS id, e.nome AS espaco, s.nome AS nome,
                           s.capacidade AS extra, s.preco_hora AS preco, 'Sala' AS tipo
                    FROM sala s JOIN espaco e ON s.espaco_id=e.espaco_id
                    WHERE s.estado='Disponivel' AND NOT EXISTS (
                        SELECT 1 FROM reserva r WHERE r.recurso_id=s.recurso_id
                        AND r.data_reserva=@d AND r.estado<>'Cancelada'
                        AND r.hora_inicio < @hf AND r.hora_fim > @hi)
                    ORDER BY e.nome, s.nome"
                : @"SELECT p.recurso_id AS id, e.nome AS espaco, p.codigo AS nome,
                           NULL AS extra, p.preco_dia AS preco, 'Posto' AS tipo
                    FROM posto p JOIN espaco e ON p.espaco_id=e.espaco_id
                    WHERE p.estado='Disponivel' AND NOT EXISTS (
                        SELECT 1 FROM reserva r WHERE r.recurso_id=p.recurso_id
                        AND r.data_reserva=@d AND r.estado<>'Cancelada')
                    ORDER BY e.nome, p.codigo";

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@d", data);
                    cmd.Parameters.AddWithValue("@hi", hi);
                    cmd.Parameters.AddWithValue("@hf", hf);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        RenderDispRows(dt, isSala);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderDispRows(DataTable dt, bool isSala)
        {
            _listDisp.Content.SuspendLayout();
            _listDisp.Content.Controls.Clear();

            string tipo  = isSala ? "salas" : "postos";
            string horas = isSala ? $" das {_hIni.SelectedText} às {_hFim.SelectedText}" : "";
            _dispHeader.Text = dt.Rows.Count == 0
                ? $"Sem {tipo} disponíveis em {_dtDispData.Value:dd/MM/yyyy}{horas}"
                : $"{dt.Rows.Count} {tipo} disponíveis em {_dtDispData.Value:dd/MM/yyyy}{horas}";

            // Agrupar por espaço para criar section headers.
            var byEspaco = new Dictionary<string, List<DataRow>>();
            foreach (DataRow r in dt.Rows)
            {
                string e = r["espaco"].ToString();
                if (!byEspaco.ContainsKey(e)) byEspaco[e] = new List<DataRow>();
                byEspaco[e].Add(r);
            }

            // Construir todos os controls em ordem reversa (Dock=Top → reverse z).
            var controls = new List<Control>();
            int totalH = 0;
            int spaceIdx = 0;
            foreach (var pair in byEspaco)
            {
                // Section header (espaço + count)
                var header = BuildSectionHeader(pair.Key, pair.Value.Count, tipo);
                header.Dock = DockStyle.Top;
                controls.Add(header);
                totalH += header.Height + 4;
                spaceIdx++;

                foreach (DataRow r in pair.Value)
                {
                    string nome   = r["nome"].ToString();
                    string tipoR  = r["tipo"].ToString();
                    decimal preco = Convert.ToDecimal(r["preco"]);
                    int? extra    = r["extra"] is DBNull ? (int?)null : Convert.ToInt32(r["extra"]);
                    string unidade = isSala ? "/hora" : "/dia";

                    var card = BuildResourceCard(pair.Key, nome, tipoR, extra, preco, unidade);
                    card.Dock = DockStyle.Top;
                    controls.Add(card);
                    totalH += card.Height + 6;
                }
            }
            // Adicionar reversed para preservar a ordem visual.
            for (int i = controls.Count - 1; i >= 0; i--)
                _listDisp.Content.Controls.Add(controls[i]);

            _listDisp.Content.ResumeLayout();
            _listDisp.UpdateLayout(totalH);
            _listDisp.Visible  = dt.Rows.Count > 0;
            _dispEmpty.Visible = dt.Rows.Count == 0;
            _dispEmpty.Invalidate();
        }

        private Control BuildSectionHeader(string espaco, int count, string tipo)
        {
            var pnl = new Panel { Height = 42, BackColor = Theme.CardBg, Padding = new Padding(4, 14, 4, 4) };
            var lblName = new Label
            {
                Text = espaco, Font = new Font(Theme.FontBase.FontFamily, 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Left, AutoSize = false, Width = 320, TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblCount = new Label
            {
                Text = $"{count} {(count == 1 ? tipo.TrimEnd('s') : tipo)}",
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = Theme.CardBg,
                Dock = DockStyle.Right, AutoSize = false, Width = 200, TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 8, 0),
            };
            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.CardBorder };
            pnl.Controls.Add(lblName);
            pnl.Controls.Add(lblCount);
            pnl.Controls.Add(divider);
            return pnl;
        }

        private Control BuildResourceCard(string espaco, string nome, string tipo, int? capacidade, decimal preco, string unidade)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = UcEspacos.MixColors(Theme.CardBg, Color.White, 0.05f);
            // Outer wrapper para Padding bottom funcionar como gap entre cards.
            var wrap = new Panel { Height = 86, BackColor = idleBg, Padding = new Padding(0, 0, 0, 8) };
            var row  = new Panel { Dock = DockStyle.Fill, BackColor = idleBg };

            Color tipoColor = (tipo == "Sala") ? Theme.Accent : ColorTranslator.FromHtml("#06b6d4");
            IconChar tipoIcon = (tipo == "Sala") ? IconChar.DoorClosed : IconChar.Chair;

            // ─── Esquerda: avatar tipo ───────────────────────────────
            var leftBlock = new Panel { Dock = DockStyle.Left, Width = 68, BackColor = idleBg };
            Image img = null;
            using (var pb = new IconPictureBox { IconChar = tipoIcon, IconSize = 20, IconColor = Color.White })
                if (pb.Image != null) img = (Image)pb.Image.Clone();
            leftBlock.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int diam = 42;
                int cx = (leftBlock.Width  - diam) / 2;
                int cy = (leftBlock.Height - diam) / 2;
                using (var br = new SolidBrush(tipoColor)) g.FillEllipse(br, cx, cy, diam, diam);
                if (img != null) g.DrawImage(img, cx + (diam - 20) / 2, cy + (diam - 20) / 2 + 1, 20, 20);
            };

            // ─── Direita: botão Reservar ─────────────────────────────
            var actions = new Panel { Dock = DockStyle.Right, Width = 130, BackColor = idleBg, Padding = new Padding(0, 20, 12, 0) };
            var btnReservar = new ModernButton
            {
                Text = "Reservar", Style = ModernButton.Variant.Primary,
                Font = Theme.FontBold, Dock = DockStyle.Top, Height = 36,
            };
            btnReservar.Click += (s, e) =>
            {
                MessageBox.Show($"Abrir nova reserva para '{nome}' em {_dtDispData.Value:dd/MM/yyyy}.",
                    "Reservar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            actions.Controls.Add(btnReservar);

            // ─── Preço + chip Disponível ─────────────────────────────
            var precoInfo = new Panel { Dock = DockStyle.Right, Width = 170, BackColor = idleBg, Padding = new Padding(0, 16, 12, 0) };
            var lblPreco = new Label
            {
                Text = Theme.FormatEuro(preco) + " " + unidade,
                Font = new Font(Theme.FontBase.FontFamily, 14f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 30, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            };
            var pillHolder = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = idleBg };
            var pill = new StatusPill
            {
                Text = "Disponível", Height = 22, Font = Theme.FontSub,
                BackColor = idleBg, Style = StatusPill.PillStyle.Dot,
            };
            pill.SetColors(Theme.StatusSuccessBg, Theme.StatusSuccessFg);
            pill.Dock  = DockStyle.Right;
            pill.Width = StatusPill.MeasureDotWidth("Disponível", Theme.FontSub);
            pillHolder.Controls.Add(pill);
            precoInfo.Controls.Add(pillHolder);
            precoInfo.Controls.Add(lblPreco);

            // ─── Centro: nome + capacidade/tipo ──────────────────────
            var middle = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(10, 14, 8, 0) };
            string subline = capacidade.HasValue
                ? $"{capacidade.Value} lugares · €{preco.ToString("0.##", CultureInfo.InvariantCulture)} {unidade}"
                : $"{tipo} · €{preco.ToString("0.##", CultureInfo.InvariantCulture)} {unidade}";
            middle.Controls.Add(new Label
            {
                Text = subline, Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Bottom, Height = 22, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            middle.Controls.Add(new Label
            {
                Text = nome, Font = new Font(Theme.FontBase.FontFamily, 13f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });

            row.Controls.Add(middle);
            row.Controls.Add(precoInfo);
            row.Controls.Add(actions);
            row.Controls.Add(leftBlock);
            wrap.Controls.Add(row);

            // Hover subtle no row (sem afectar o botão).
            void Recurse(Control c, Color bg)
            {
                if (c is ModernButton) return;
                c.BackColor = bg;
                foreach (Control x in c.Controls) Recurse(x, bg);
            }
            void Hook(Control c)
            {
                c.MouseEnter += (s, e) => Recurse(row, hoverBg);
                c.MouseLeave += (s, e) =>
                {
                    var p = row.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!row.ClientRectangle.Contains(p)) Recurse(row, idleBg);
                };
                foreach (Control x in c.Controls) Hook(x);
            }
            Hook(row);

            return wrap;
        }

        // ─── TAB 2: Por Cliente ─────────────────────────────────────────
        private Control BuildTabCli()
        {
            var card = new ModernCard
            {
                Name = "tabCli", Dock = DockStyle.Fill, BackColor = Theme.CardBg, Visible = false,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
            };
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.CardBg,
                Padding = new Padding(16, 14, 16, 16),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // filtro
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));  // 3 KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // 2 listas

            var filterRow = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            var lblCli = new Label
            {
                Text = "Cliente", Font = new Font(Theme.FontBase.FontFamily, 9f, FontStyle.Bold),
                ForeColor = Theme.TextMuted, BackColor = Theme.CardBg,
                Location = new Point(0, 10), Size = new Size(54, 36), TextAlign = ContentAlignment.MiddleLeft,
            };
            _cmbCliCli = new ModernSelect { Width = 240, Height = 36, Location = new Point(60, 10) };
            _cmbCliCli.SelectedIndexChanged += (s, e) => LoadClienteData();
            filterRow.Controls.Add(lblCli);
            filterRow.Controls.Add(_cmbCliCli);

            // KPI row (3 cards)
            var kpiGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.CardBg,
                Margin = new Padding(0, 8, 0, 8),
            };
            for (int i = 0; i < 3; i++) kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            kpiGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var k1 = BuildSmallKpi("Reservas",      IconChar.CalendarCheck, Theme.Accent,          out _kpiCliReservas);
            var k2 = BuildSmallKpi("Total pago",     IconChar.EuroSign,      Theme.StatusSuccessFg, out _kpiCliPago);
            var k3 = BuildSmallKpi("Última atividade", IconChar.Clock,        Theme.TextSecondary,   out _kpiCliUltima);
            k3.Margin = new Padding(0);
            kpiGrid.Controls.Add(k1, 0, 0);
            kpiGrid.Controls.Add(k2, 1, 0);
            kpiGrid.Controls.Add(k3, 2, 0);

            // Lista 2 colunas: Reservas | Pagamentos
            _cliListGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.CardBg,
                Visible = false,
            };
            _cliListGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _cliListGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _cliListGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var col1 = BuildSubListCard("Reservas",   out _listCliReservas);
            var col2 = BuildSubListCard("Pagamentos", out _listCliPagamentos);
            col1.Margin = new Padding(0, 0, 6, 0);
            col2.Margin = new Padding(6, 0, 0, 0);
            _cliListGrid.Controls.Add(col1, 0, 0);
            _cliListGrid.Controls.Add(col2, 1, 0);

            _cliEmpty = BuildEmptyState("Selecciona um cliente para ver o histórico", IconChar.User);
            _cliEmpty.Dock = DockStyle.Fill;
            var listHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            listHost.Controls.Add(_cliListGrid);
            listHost.Controls.Add(_cliEmpty);

            root.Controls.Add(filterRow, 0, 0);
            root.Controls.Add(kpiGrid,   0, 1);
            root.Controls.Add(listHost,  0, 2);
            card.Controls.Add(root);
            return card;
        }

        private Control BuildSmallKpi(string label, IconChar icon, Color iconColor, out Label valLbl)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 10, ShowShadow = false,
                Margin = new Padding(0, 0, 12, 0),
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(16, 12, 16, 12) };
            var topLine = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Theme.CardBg };
            topLine.Controls.Add(new Label
            {
                Text = label, Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0),
            });
            topLine.Controls.Add(new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = iconColor, BackColor = Theme.CardBg,
                Dock = DockStyle.Left, Width = 22, SizeMode = PictureBoxSizeMode.CenterImage,
            });
            valLbl = new Label
            {
                Text = "—", Font = new Font(Theme.FontBase.FontFamily, 20f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 4, 0, 0),
            };
            inner.Controls.Add(valLbl);
            inner.Controls.Add(topLine);
            card.Controls.Add(inner);
            return card;
        }

        private Control BuildSubListCard(string title, out ScrollableList list)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 10, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(12, 10, 12, 12) };
            var header = new Label
            {
                Text = title, Font = Theme.FontSection, ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = 26, TextAlign = ContentAlignment.MiddleLeft,
            };
            list = new ScrollableList { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            list.Content.BackColor = Theme.CardBg;
            inner.Controls.Add(list);
            inner.Controls.Add(header);
            card.Controls.Add(inner);
            return card;
        }

        private void LoadClienteData()
        {
            if (_cmbCliCli.SelectedValue == null || _cmbCliCli.SelectedValue is DBNull) return;
            int cid = Convert.ToInt32(_cmbCliCli.SelectedValue);

            try
            {
                using (var conn = Database.GetConnection())
                {
                    int nReservas = 0;
                    decimal totalPago = 0;
                    DateTime? ultima = null;

                    // Reservas
                    var dtR = new DataTable();
                    using (var cmd = new SqlCommand(
                        @"SELECT r.reserva_id, r.data_reserva, r.hora_inicio, r.hora_fim,
                                 r.valor, r.estado,
                                 CASE WHEN s.recurso_id IS NOT NULL THEN 'Sala ' + s.nome
                                      ELSE 'Posto ' + p.codigo END AS recurso
                          FROM reserva r
                          JOIN recurso rc ON r.recurso_id = rc.recurso_id
                          LEFT JOIN sala s  ON rc.recurso_id = s.recurso_id
                          LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
                          WHERE r.cliente_id = @c ORDER BY r.data_reserva DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@c", cid);
                        using (var da = new SqlDataAdapter(cmd)) da.Fill(dtR);
                    }
                    nReservas = dtR.Rows.Count;

                    // Pagamentos
                    var dtP = new DataTable();
                    using (var cmd = new SqlCommand(
                        @"SELECT data_pagamento, valor, metodo_pagamento, estado,
                                 CASE WHEN reserva_id IS NOT NULL THEN 'Reserva #' + CAST(reserva_id AS varchar)
                                      ELSE 'Adesão #' + CAST(adesao_id AS varchar) END AS ref
                          FROM pagamento WHERE cliente_id = @c
                          ORDER BY data_pagamento DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@c", cid);
                        using (var da = new SqlDataAdapter(cmd)) da.Fill(dtP);
                    }
                    foreach (DataRow row in dtP.Rows)
                    {
                        if (row["estado"].ToString() == "Pago") totalPago += Convert.ToDecimal(row["valor"]);
                        DateTime d = Convert.ToDateTime(row["data_pagamento"]);
                        if (!ultima.HasValue || d > ultima) ultima = d;
                    }
                    foreach (DataRow row in dtR.Rows)
                    {
                        DateTime d = Convert.ToDateTime(row["data_reserva"]);
                        if (!ultima.HasValue || d > ultima) ultima = d;
                    }

                    _kpiCliReservas.Text = nReservas.ToString();
                    _kpiCliPago    .Text = Theme.FormatEuro(totalPago);
                    _kpiCliUltima  .Text = ultima.HasValue ? ultima.Value.ToString("dd/MM/yyyy") : "—";

                    RenderCliReservas(dtR);
                    RenderCliPagamentos(dtP);
                    _cliListGrid.Visible = true;
                    _cliEmpty   .Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderCliReservas(DataTable dt)
        {
            _listCliReservas.Content.SuspendLayout();
            _listCliReservas.Content.Controls.Clear();
            int y = 0, w = Math.Max(280, _listCliReservas.ClientSize.Width - 20);
            foreach (DataRow r in dt.Rows)
            {
                string recurso = r["recurso"].ToString();
                DateTime data  = Convert.ToDateTime(r["data_reserva"]);
                decimal valor  = Convert.ToDecimal(r["valor"]);
                string estado  = r["estado"].ToString();
                var card = BuildSubItemCard($"{recurso}", $"{data:dd/MM/yyyy}", valor, estado, TipoColorReserva(estado));
                card.Location = new Point(0, y); card.Width = w;
                card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _listCliReservas.Content.Controls.Add(card);
                y += card.Height + 6;
            }
            _listCliReservas.Content.ResumeLayout();
            _listCliReservas.UpdateLayout(y);
        }

        private void RenderCliPagamentos(DataTable dt)
        {
            _listCliPagamentos.Content.SuspendLayout();
            _listCliPagamentos.Content.Controls.Clear();
            int y = 0, w = Math.Max(280, _listCliPagamentos.ClientSize.Width - 20);
            foreach (DataRow r in dt.Rows)
            {
                string refLine = r["ref"].ToString();
                DateTime data  = Convert.ToDateTime(r["data_pagamento"]);
                decimal valor  = Convert.ToDecimal(r["valor"]);
                string estado  = r["estado"].ToString();
                string sub     = $"{r["metodo_pagamento"]} · {data:dd/MM/yyyy}";
                var card = BuildSubItemCard(refLine, sub, valor, estado, TipoColorPagamento(estado));
                card.Location = new Point(0, y); card.Width = w;
                card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _listCliPagamentos.Content.Controls.Add(card);
                y += card.Height + 6;
            }
            _listCliPagamentos.Content.ResumeLayout();
            _listCliPagamentos.UpdateLayout(y);
        }

        private Control BuildSubItemCard(string title, string sub, decimal valor, string estado, (Color bg, Color fg) cores)
        {
            Color idleBg = Theme.CardBg;
            var row = new Panel { Height = 64, BackColor = idleBg, Margin = new Padding(0, 0, 0, 6) };

            var rightInfo = new Panel { Dock = DockStyle.Right, Width = 110, BackColor = idleBg, Padding = new Padding(0, 10, 8, 0) };
            rightInfo.Controls.Add(new Label
            {
                Text = Theme.FormatEuro(valor),
                Font = new Font(Theme.FontBase.FontFamily, 11f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 22, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            });
            var pillHolder = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = idleBg };
            var pill = new StatusPill
            {
                Text = estado, Height = 20, Style = StatusPill.PillStyle.Dot,
                Font = Theme.FontSub, BackColor = idleBg,
            };
            pill.SetColors(cores.bg, cores.fg);
            pill.Dock = DockStyle.Right;
            pill.Width = StatusPill.MeasureDotWidth(estado, Theme.FontSub);
            pillHolder.Controls.Add(pill);
            rightInfo.Controls.Add(pillHolder);

            var middle = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(10, 12, 8, 0) };
            middle.Controls.Add(new Label
            {
                Text = sub, Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Bottom, Height = 18, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            middle.Controls.Add(new Label
            {
                Text = title, Font = new Font(Theme.FontBase.FontFamily, 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });

            row.Controls.Add(middle);
            row.Controls.Add(rightInfo);
            return row;
        }

        private static (Color bg, Color fg) TipoColorReserva(string estado)
        {
            switch (estado)
            {
                case "Confirmada": return (Theme.StatusSuccessBg, Theme.StatusSuccessFg);
                case "Pendente":   return (Theme.StatusWarningBg, Theme.StatusWarningFg);
                case "Cancelada":  return (Theme.StatusDangerBg,  Theme.StatusDangerFg);
                default:            return (Theme.StatusNeutralBg, Theme.StatusNeutralFg);
            }
        }
        private static (Color bg, Color fg) TipoColorPagamento(string estado)
        {
            switch (estado)
            {
                case "Pago":        return (Theme.StatusSuccessBg, Theme.StatusSuccessFg);
                case "Pendente":    return (Theme.StatusWarningBg, Theme.StatusWarningFg);
                case "Cancelado":   return (Theme.StatusDangerBg,  Theme.StatusDangerFg);
                default:             return (Theme.StatusNeutralBg, Theme.StatusNeutralFg);
            }
        }

        // ─── TAB 3: Análise ─────────────────────────────────────────────
        private Control BuildTabAna()
        {
            var card = new ModernCard
            {
                Name = "tabAna", Dock = DockStyle.Fill, BackColor = Theme.CardBg, Visible = false,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
            };
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.CardBg,
                Padding = new Padding(16, 14, 16, 16),
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // filtro
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));  // KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // charts

            var filterRow = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg };
            var lblP = new Label
            {
                Text = "Período", Font = new Font(Theme.FontBase.FontFamily, 9f, FontStyle.Bold),
                ForeColor = Theme.TextMuted, BackColor = Theme.CardBg,
                Location = new Point(0, 10), Size = new Size(60, 36), TextAlign = ContentAlignment.MiddleLeft,
            };
            _dtAnaIni = new ModernDateField { Width = 130, Height = 36, Value = DateTime.Today.AddMonths(-3), Location = new Point(66, 10) };
            var lblArrow = new Label
            {
                Text = "→", Font = new Font(Theme.FontBase.FontFamily, 11f),
                ForeColor = Theme.TextMuted, BackColor = Theme.CardBg,
                Location = new Point(204, 10), Size = new Size(20, 36), TextAlign = ContentAlignment.MiddleCenter,
            };
            _dtAnaFim = new ModernDateField { Width = 130, Height = 36, Value = DateTime.Today, Location = new Point(228, 10) };
            _btnAnaAplicar = new ModernButton
            {
                Text = "Aplicar", Style = ModernButton.Variant.Primary, Font = Theme.FontBold,
                Size = new Size(110, 40), Location = new Point(370, 8),
            };
            _btnAnaAplicar.Click += (s, e) => LoadAnaData();
            filterRow.Controls.AddRange(new Control[] { lblP, _dtAnaIni, lblArrow, _dtAnaFim, _btnAnaAplicar });

            // KPIs (3 cards)
            var kpiGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.CardBg,
                Margin = new Padding(0, 8, 0, 8),
            };
            for (int i = 0; i < 3; i++) kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            kpiGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var ka = BuildSmallKpi("Receita total", IconChar.EuroSign,     Theme.Accent,           out _kpiAnaReceita);
            var kb = BuildSmallKpi("Nº reservas",    IconChar.CalendarDays, Theme.StatusSuccessFg,  out _kpiAnaReservas);
            var kc = BuildSmallKpi("Nº pagamentos",  IconChar.CreditCard,    Theme.StatusOrangeFg,   out _kpiAnaPagos);
            kc.Margin = new Padding(0);
            kpiGrid.Controls.Add(ka, 0, 0);
            kpiGrid.Controls.Add(kb, 1, 0);
            kpiGrid.Controls.Add(kc, 2, 0);

            // Charts (2 col: mensal + pie/ocupação)
            var chartGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Theme.CardBg,
            };
            chartGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            chartGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            chartGrid.Controls.Add(BuildChartCard("Receita mensal", out _chartReceitaMensal, isLine: false, isPie: false), 0, 0);
            chartGrid.Controls.Add(BuildChartCard("Métodos de pagamento", out _chartMetodos,       isLine: false, isPie: true),  1, 0);
            var ocup = BuildChartCard("Ocupação por espaço", out _chartOcupacao, isLine: false, isPie: false);
            chartGrid.SetColumnSpan(ocup, 2);
            chartGrid.Controls.Add(ocup, 0, 1);

            root.Controls.Add(filterRow, 0, 0);
            root.Controls.Add(kpiGrid,   0, 1);
            root.Controls.Add(chartGrid, 0, 2);
            card.Controls.Add(root);
            return card;
        }

        private Control BuildChartCard(string title, out Chart chart, bool isLine, bool isPie)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 10, ShowShadow = false,
                Margin = new Padding(0, 0, 12, 12),
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(14, 10, 14, 14) };
            var lblTitle = new Label
            {
                Text = title, Font = Theme.FontSection, ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = 26, TextAlign = ContentAlignment.MiddleLeft,
            };
            chart = new Chart { Dock = DockStyle.Fill, BackColor = Theme.CardBg, MinimumSize = new Size(1, 1) };
            var area = new ChartArea("main") { BackColor = Color.Transparent };
            area.AxisX.LineColor = Theme.CardBorder;
            area.AxisY.LineColor = Theme.CardBorder;
            area.AxisX.LabelStyle.ForeColor = Theme.TextMuted;
            area.AxisY.LabelStyle.ForeColor = Theme.TextMuted;
            area.AxisX.MajorGrid.LineColor = Color.Transparent;
            area.AxisY.MajorGrid.LineColor = Theme.CardBorder;
            if (isPie) { area.Position.Auto = true; }
            chart.ChartAreas.Add(area);

            var ser = new Series("s")
            {
                ChartType = isPie ? SeriesChartType.Doughnut : (isLine ? SeriesChartType.Line : SeriesChartType.Column),
                Color = Theme.Accent, BorderWidth = 2,
            };
            chart.Series.Add(ser);
            if (isPie)
            {
                var leg = new Legend("leg") { Docking = Docking.Right, BackColor = Color.Transparent, ForeColor = Theme.TextSecondary, Font = Theme.FontSub };
                chart.Legends.Add(leg);
                ser.Legend = "leg";
            }
            inner.Controls.Add(chart);     // Fill primeiro (bottom z)
            inner.Controls.Add(lblTitle);  // Top depois (top z, processa primeiro)
            card.Controls.Add(inner);
            return card;
        }

        private void LoadAnaData()
        {
            if (_dtAnaIni.Value > _dtAnaFim.Value) { MessageBox.Show("Data inicial deve ser anterior à final."); return; }
            try
            {
                using (var conn = Database.GetConnection())
                {
                    // KPIs + receita mensal
                    decimal receita = 0;
                    int nResv = 0, nPag = 0;
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*), ISNULL(SUM(valor),0) FROM pagamento
                          WHERE estado='Pago' AND data_pagamento BETWEEN @i AND @f", conn))
                    {
                        cmd.Parameters.AddWithValue("@i", _dtAnaIni.Value.Date);
                        cmd.Parameters.AddWithValue("@f", _dtAnaFim.Value.Date);
                        using (var r = cmd.ExecuteReader())
                            if (r.Read()) { nPag = r.GetInt32(0); receita = r.GetDecimal(1); }
                    }
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM reserva
                          WHERE estado <> 'Cancelada' AND data_reserva BETWEEN @i AND @f", conn))
                    {
                        cmd.Parameters.AddWithValue("@i", _dtAnaIni.Value.Date);
                        cmd.Parameters.AddWithValue("@f", _dtAnaFim.Value.Date);
                        nResv = (int)cmd.ExecuteScalar();
                    }
                    _kpiAnaReceita .Text = Theme.FormatEuro(receita);
                    _kpiAnaReservas.Text = nResv.ToString();
                    _kpiAnaPagos   .Text = nPag.ToString();

                    // Mensal chart
                    var serM = _chartReceitaMensal.Series["s"]; serM.Points.Clear();
                    using (var cmd = new SqlCommand(
                        @"SELECT CAST(YEAR(data_pagamento) AS varchar) + '/' +
                                 RIGHT('0' + CAST(MONTH(data_pagamento) AS varchar), 2) AS mes,
                                 SUM(valor) AS total
                          FROM pagamento
                          WHERE estado='Pago' AND data_pagamento BETWEEN @i AND @f
                          GROUP BY YEAR(data_pagamento), MONTH(data_pagamento)
                          ORDER BY YEAR(data_pagamento), MONTH(data_pagamento)", conn))
                    {
                        cmd.Parameters.AddWithValue("@i", _dtAnaIni.Value.Date);
                        cmd.Parameters.AddWithValue("@f", _dtAnaFim.Value.Date);
                        using (var rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                                serM.Points.AddXY(rdr.GetString(0), Convert.ToDouble(rdr.GetDecimal(1)));
                    }

                    // Métodos pie
                    var serP = _chartMetodos.Series["s"]; serP.Points.Clear();
                    using (var cmd = new SqlCommand(
                        @"SELECT metodo_pagamento, COUNT(*) FROM pagamento
                          WHERE estado='Pago' AND data_pagamento BETWEEN @i AND @f
                          GROUP BY metodo_pagamento", conn))
                    {
                        cmd.Parameters.AddWithValue("@i", _dtAnaIni.Value.Date);
                        cmd.Parameters.AddWithValue("@f", _dtAnaFim.Value.Date);
                        using (var rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                                serP.Points.AddXY(rdr.GetString(0), rdr.GetInt32(1));
                    }
                    Color[] pal = new[]
                    {
                        Theme.Accent, ColorTranslator.FromHtml("#8b5cf6"), ColorTranslator.FromHtml("#10b981"),
                        ColorTranslator.FromHtml("#f59e0b"), ColorTranslator.FromHtml("#ef4444"),
                    };
                    for (int i = 0; i < serP.Points.Count; i++) serP.Points[i].Color = pal[i % pal.Length];

                    // Ocupação por espaço
                    var serO = _chartOcupacao.Series["s"]; serO.Points.Clear();
                    using (var cmd = new SqlCommand(
                        @"SELECT e.nome, COUNT(r.reserva_id) AS reservas
                          FROM espaco e
                          LEFT JOIN sala s    ON s.espaco_id = e.espaco_id
                          LEFT JOIN posto p   ON p.espaco_id = e.espaco_id
                          LEFT JOIN reserva r ON r.recurso_id IN (s.recurso_id, p.recurso_id)
                            AND r.estado <> 'Cancelada' AND r.data_reserva BETWEEN @i AND @f
                          GROUP BY e.nome ORDER BY e.nome", conn))
                    {
                        cmd.Parameters.AddWithValue("@i", _dtAnaIni.Value.Date);
                        cmd.Parameters.AddWithValue("@f", _dtAnaFim.Value.Date);
                        using (var rdr = cmd.ExecuteReader())
                            while (rdr.Read())
                                serO.Points.AddXY(rdr.GetString(0), rdr.GetInt32(1));
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────
        private void LoadClientesCombo()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var da   = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    _cmbCliCli.BindDataTable(dt, "nome", "cliente_id");
                }
            }
            catch (SqlException) { /* ignore */ }
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
    }
}
