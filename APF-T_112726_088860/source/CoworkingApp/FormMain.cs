using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CoworkingApp.Controls;

namespace CoworkingApp
{
    public partial class FormMain : Form
    {
        private Panel pnlContent;
        private Button _activeBtn;
        private readonly List<Button> _navBtns = new List<Button>();
        private ToolStripStatusLabel _lblModule;
        private System.Windows.Forms.Timer _clockTimer;

        public FormMain()
        {
            InitializeComponent();
            BuildUI();
            _navBtns[0].PerformClick();
        }

        private void BuildUI()
        {
            this.Text = "Coworking — Painel de Gestão";
            this.MinimumSize = new Size(900, 600);
            this.WindowState = FormWindowState.Maximized;
            this.Font = Theme.FontBase;
            this.BackColor = Theme.ContentBg;

            // StatusStrip (Dock=Bottom — must be added to Controls last / after content)
            var statusStrip = new StatusStrip
            {
                BackColor = Color.White,
                SizingGrip = false
            };
            _lblModule = new ToolStripStatusLabel
            {
                Text      = "Dashboard",
                Spring    = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ColorTranslator.FromHtml("#475569")
            };
            var lblClock = new ToolStripStatusLabel
            {
                ForeColor = ColorTranslator.FromHtml("#475569"),
                TextAlign = ContentAlignment.MiddleRight
            };
            statusStrip.Items.Add(_lblModule);
            statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
            statusStrip.Items.Add(lblClock);

            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _clockTimer.Start();
            lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.ContentBg
            };

            var pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 184,
                BackColor = Theme.SidebarBg
            };

            BuildSidebar(pnlSidebar);

            // Fill must be added before Left so docking works; Bottom last
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(statusStrip);
        }

        private void BuildSidebar(Panel sidebar)
        {
            // Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(16, 16, 8, 0)
            };
            var lblSub = new Label
            {
                Text = "PAINEL DE GESTÃO",
                ForeColor = Theme.SidebarText,
                Font = new Font("Segoe UI", 7.5f),
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = 18,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblTitle = new Label
            {
                Text = "COWORKING",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Theme.SidebarActive,
                Padding = new Padding(16, 0, 0, 0)
            };
            pnlFooter.Controls.Add(new Label
            {
                Text = "v1.0 — APF-T 2026",
                ForeColor = Theme.SidebarText,
                Font = new Font("Segoe UI", 7.5f),
                AutoSize = true,
                Top = 10,
                Left = 16
            });

            // Nav area
            var pnlNav = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(8, 8, 8, 0)
            };
            var flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var sections = new (string Label, Action Navigate)[]
            {
                ("▦  Dashboard",  () => Navigate<UcDashboard>()),
                ("◎  Clientes",   () => Navigate<UcClientes>()),
                ("☰  Planos",     () => Navigate<UcPlanos>()),
                ("⊞  Espaços",    () => Navigate<UcEspacos>()),
                ("◷  Reservas",   () => Navigate<UcReservas>()),
                ("★  Adesões",    () => Navigate<UcAdesoes>()),
                ("€  Pagamentos", () => Navigate<UcPagamentos>()),
                ("◈  Relatórios", () => Navigate<UcRelatorios>())
            };

            foreach (var (label, nav) in sections)
            {
                var btn = new Button
                {
                    Text = label,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Theme.SidebarText,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5f),
                    Height = 38,
                    Width = 168,
                    Padding = new Padding(8, 0, 0, 0),
                    Margin = new Padding(0, 0, 0, 2),
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#1e3a8a");

                var capture = nav;
                btn.Click += (s, e) =>
                {
                    SetActive((Button)s);
                    capture();
                };

                _navBtns.Add(btn);
                flp.Controls.Add(btn);
            }

            pnlNav.Controls.Add(flp);
            sidebar.Controls.Add(pnlNav);
            sidebar.Controls.Add(pnlFooter);
            sidebar.Controls.Add(pnlHeader);
        }

        private void SetActive(Button btn)
        {
            if (_activeBtn != null)
            {
                _activeBtn.BackColor = Color.Transparent;
                _activeBtn.ForeColor = Theme.SidebarText;
                _activeBtn.Font = new Font("Segoe UI", 9.5f);
            }
            _activeBtn = btn;
            btn.BackColor = Theme.SidebarActive;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        }

        private void Navigate<T>() where T : Control, new()
        {
            pnlContent.Controls.Clear();
            var uc = new T { Dock = DockStyle.Fill };
            pnlContent.Controls.Add(uc);

            var names = new System.Collections.Generic.Dictionary<Type, string>
            {
                { typeof(UcDashboard),  "Dashboard"  },
                { typeof(UcClientes),   "Clientes"   },
                { typeof(UcPlanos),     "Planos"     },
                { typeof(UcEspacos),    "Espaços"    },
                { typeof(UcReservas),   "Reservas"   },
                { typeof(UcAdesoes),    "Adesões"    },
                { typeof(UcPagamentos), "Pagamentos" },
                { typeof(UcRelatorios), "Relatórios" }
            };
            if (names.TryGetValue(typeof(T), out string name))
                _lblModule.Text = name;
        }
    }
}
