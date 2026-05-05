using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CoworkingApp.Controls;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    public partial class FormMain : Form
    {
        private Panel pnlContent;
        private Button _activeBtn;
        private readonly List<Button> _navBtns = new List<Button>();
        private Label _lblModule;
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

            // Status bar custom
            var pnlStatus = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Theme.CardBg,
                Padding = new Padding(14, 0, 14, 0)
            };
            var pnlStatusBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Theme.CardBorder
            };
            pnlStatus.Controls.Add(pnlStatusBorder);

            _lblModule = new Label
            {
                Text = "Dashboard",
                ForeColor = Theme.TextSecondary,
                Font = Theme.FontBase,
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 200,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblClock = new Label
            {
                ForeColor = Theme.TextSecondary,
                Font = Theme.FontBase,
                Dock = DockStyle.Right,
                AutoSize = false,
                Width = 200,
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlStatus.Controls.Add(_lblModule);
            pnlStatus.Controls.Add(lblClock);

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
                BackColor = Theme.SidebarBg
            };

            BuildSidebar(pnlSidebar);

            // Fill must be added before Left so docking works; Bottom last
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlStatus);
        }

        private void BuildSidebar(Panel sidebar)
        {
            sidebar.Width = 200;

            // Header
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(14, 14, 8, 0)
            };
            var lblTitle = new Label
            {
                Text = "Coworking",
                ForeColor = Color.White,
                Font = new Font(Theme.FontBase.FontFamily, 13f, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(34, 0, 0, 0)
            };
            // Logo box
            var logoBox = new Panel
            {
                BackColor = Theme.Accent,
                Size = new Size(24, 24),
                Location = new Point(14, 20)
            };
            pnlHeader.Controls.Add(logoBox);
            pnlHeader.Controls.Add(lblTitle);

            // Footer com toggle theme
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(8, 4, 8, 4)
            };
            var btnTheme = new IconButton
            {
                IconChar = ThemeManager.Current == ThemeMode.Light ? IconChar.Moon : IconChar.Sun,
                IconColor = Color.White,
                IconSize = 16,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = ThemeManager.Current == ThemeMode.Light ? "  Modo escuro" : "  Modo claro",
                Font = new Font(Theme.FontBase.FontFamily, 9f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Padding = new Padding(8, 0, 0, 0)
            };
            btnTheme.FlatAppearance.BorderSize = 0;
            btnTheme.Click += (s, e) =>
            {
                ThemeManager.Toggle();
                btnTheme.IconChar = ThemeManager.Current == ThemeMode.Light ? IconChar.Moon : IconChar.Sun;
                btnTheme.Text = ThemeManager.Current == ThemeMode.Light ? "  Modo escuro" : "  Modo claro";
            };
            pnlFooter.Controls.Add(btnTheme);

            // Nav area
            var pnlNav = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.SidebarBg,
                Padding = new Padding(8, 8, 8, 0),
                AutoScroll = true
            };
            var flp = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false
            };

            AddSectionLabel(flp, "OPERACIONAL");
            AddNavItem(flp, "Dashboard",  IconChar.ThLarge,        () => Navigate<UcDashboard>());
            AddNavItem(flp, "Clientes",   IconChar.Users,          () => Navigate<UcClientes>());
            AddNavItem(flp, "Planos",     IconChar.ClipboardList,  () => Navigate<UcPlanos>());
            AddNavItem(flp, "Espaços",    IconChar.Building,       () => Navigate<UcEspacos>());
            AddNavItem(flp, "Reservas",   IconChar.CalendarAlt,    () => Navigate<UcReservas>());

            AddSectionLabel(flp, "FINANCEIRO");
            AddNavItem(flp, "Adesões",    IconChar.Star,           () => Navigate<UcAdesoes>());
            AddNavItem(flp, "Pagamentos", IconChar.CreditCard,     () => Navigate<UcPagamentos>());
            AddNavItem(flp, "Relatórios", IconChar.ChartLine,      () => Navigate<UcRelatorios>());

            pnlNav.Controls.Add(flp);
            sidebar.Controls.Add(pnlNav);
            sidebar.Controls.Add(pnlFooter);
            sidebar.Controls.Add(pnlHeader);
        }

        private void AddSectionLabel(FlowLayoutPanel container, string text)
        {
            container.Controls.Add(new Label
            {
                Text = text,
                Font = Theme.FontMicro,
                ForeColor = Theme.SidebarSectionLbl,
                AutoSize = false,
                Width = 180,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Margin = new Padding(0, 6, 0, 4)
            });
        }

        private void AddNavItem(FlowLayoutPanel container, string label, IconChar icon, Action onClick)
        {
            var btn = new IconButton
            {
                Text = "  " + label,
                IconChar = icon,
                IconColor = Theme.SidebarText,
                IconSize = 16,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.SidebarText,
                BackColor = Color.Transparent,
                Font = new Font(Theme.FontBase.FontFamily, 9.5f),
                Height = 36,
                Width = 180,
                Padding = new Padding(10, 0, 0, 0),
                Margin = new Padding(0, 0, 0, 2),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SidebarBgActive;

            btn.Click += (s, e) =>
            {
                SetActive(btn);
                onClick();
            };

            _navBtns.Add(btn);
            container.Controls.Add(btn);
        }

        private void SetActive(Button btn)
        {
            if (_activeBtn != null && _activeBtn is IconButton oldBtn)
            {
                _activeBtn.BackColor = Color.Transparent;
                _activeBtn.ForeColor = Theme.SidebarText;
                oldBtn.IconColor = Theme.SidebarText;
                _activeBtn.Font = new Font(Theme.FontBase.FontFamily, 9.5f);
            }
            _activeBtn = btn;
            btn.BackColor = Theme.SidebarBgActive;
            btn.ForeColor = Theme.SidebarTextActive;
            if (btn is IconButton newBtn) newBtn.IconColor = Theme.SidebarTextActive;
            btn.Font = new Font(Theme.FontBase.FontFamily, 9.5f, FontStyle.Bold);
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
