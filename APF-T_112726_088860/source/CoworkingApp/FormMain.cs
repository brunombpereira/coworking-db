using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CoworkingApp.Controls;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    public partial class FormMain : Form
    {
        public static bool LogoutRequested { get; private set; }

        private Panel pnlContent;
        /// <summary>Área de conteúdo principal (sem sidebar) — usada por
        /// modais para se centrarem visualmente em vez de sobrepor a
        /// sidebar.</summary>
        public Panel ContentArea => pnlContent;
        private Button _activeBtn;
        private readonly List<Button> _navBtns = new List<Button>();
        private Label _lblModule;
        private System.Windows.Forms.Timer _clockTimer;
        private Panel _pnlConn;
        private Label _lblConn;
        private bool _connOk;
        private System.Windows.Forms.Timer _connTimer;

        public FormMain()
        {
            LogoutRequested = false;
            InitializeComponent();
            BuildUI();

            // Entrar directamente no Dashboard + activar o botão na sidebar.
            // Explícito em vez de PerformClick (mais robusto contra ordem
            // de eventos / state inicial).
            Navigate<UcDashboard>();
            if (_navBtns.Count > 0) SetActive(_navBtns[0]);

            ThemeManager.ThemeChanged += ApplyDwmTitleBar;
        }

        // ── Dark title bar (Win10 2004+/11) ─────────────────────────────
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDwmTitleBar();
        }

        private void ApplyDwmTitleBar()
        {
            if (!IsHandleCreated) return;
            try
            {
                int useDark = ThemeManager.Current == ThemeMode.Dark ? 1 : 0;
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE,
                                      ref useDark, sizeof(int));
            }
            catch { /* não é crítico */ }
        }


        private void BuildUI()
        {
            this.Text = "Coworking — Painel de Gestão";
            this.Icon = AppIcon.Get(32);
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
            // Indicador de ligação à BD (entre o módulo e o relógio).
            // Dot custom-painted + label de texto. Dock=Left com offset à
            // direita do _lblModule (que tem Width=200).
            _pnlConn = new Panel
            {
                Dock = DockStyle.Left,
                Width = 130,
                BackColor = Theme.CardBg,
                Padding = new Padding(0, 0, 0, 0),
            };
            _pnlConn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int diam = 8;
                int x = 4;
                int y = (_pnlConn.Height - diam) / 2;
                using (var br = new SolidBrush(_connOk ? Theme.StatusSuccessFg : Theme.StatusDangerFg))
                    g.FillEllipse(br, x, y, diam, diam);
            };
            _lblConn = new Label
            {
                Text = "—",
                ForeColor = Theme.TextSecondary,
                Font = Theme.FontBase,
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                BackColor = Theme.CardBg,
            };
            _pnlConn.Controls.Add(_lblConn);

            pnlStatus.Controls.Add(_pnlConn);
            pnlStatus.Controls.Add(_lblModule);
            pnlStatus.Controls.Add(lblClock);

            // Check inicial síncrono (rápido — timeout 2s) e periódico
            // assíncrono a cada 30s para não bloquear a UI.
            RefreshConnIndicator(Database.IsAvailable());
            _connTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _connTimer.Tick += (s, e) =>
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    bool ok = Database.IsAvailable();
                    if (IsHandleCreated)
                        BeginInvoke((Action)(() => RefreshConnIndicator(ok)));
                });
            };
            _connTimer.Start();

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
            sidebar.Width = 220;

            // ── Header: só o logo (sem texto duplicado com a title bar) ─
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Theme.SidebarBg,
            };
            var logoBox = new PictureBox
            {
                Image     = AppIcon.Get(32).ToBitmap(),
                Size      = new Size(32, 32),
                Location  = new Point(16, 12),
                SizeMode  = PictureBoxSizeMode.StretchImage,
                BackColor = Theme.SidebarBg,
            };
            pnlHeader.Controls.Add(logoBox);

            // ── Footer: avatar + nome → click abre menu ──────────────────
            var pnlFooter = BuildAvatarFooter();

            // ── Nav area ────────────────────────────────────────────────
            var pnlNav = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Theme.SidebarBg,
                Padding    = new Padding(0, 12, 0, 0),
                AutoScroll = true,
            };
            var flp = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = false,
            };

            AddSectionLabel(flp, "OPERACIONAL");
            AddNavItem(flp, "Dashboard",     IconChar.ThLarge,        () => Navigate<UcDashboard>());
            if (Session.IsStaff)
            {
                AddNavItem(flp, "Clientes",  IconChar.Users,          () => Navigate<UcClientes>());
                AddNavItem(flp, "Planos",    IconChar.ClipboardList,  () => Navigate<UcPlanos>());
                AddNavItem(flp, "Espaços",   IconChar.Building,       () => Navigate<UcEspacos>());
            }
            AddNavItem(flp, "Reservas",      IconChar.CalendarAlt,    () => Navigate<UcReservas>());
            AddNavItem(flp, "Notificações",  IconChar.Bell,           () => Navigate<UcNotificacoes>());

            if (Session.IsStaff)
            {
                AddSectionLabel(flp, "FINANCEIRO");
                AddNavItem(flp, "Adesões",       IconChar.Star,           () => Navigate<UcAdesoes>());
                AddNavItem(flp, "Pagamentos",    IconChar.CreditCard,     () => Navigate<UcPagamentos>());
                AddNavItem(flp, "Relatórios",    IconChar.ChartLine,      () => Navigate<UcRelatorios>());
                AddNavItem(flp, "Estatísticas",  IconChar.ChartBar,       () => Navigate<UcEstatisticas>());
            }

            if (Session.IsAdmin)
            {
                AddSectionLabel(flp, "ADMIN");
                AddNavItem(flp, "Utilizadores",  IconChar.UserShield,     () => Navigate<UcUtilizadores>());
            }

            pnlNav.Controls.Add(flp);
            sidebar.Controls.Add(pnlNav);
            sidebar.Controls.Add(pnlFooter);
            sidebar.Controls.Add(pnlHeader);
        }

        // ── Footer avatar com menu de perfil/tema/sair ──────────────────
        private Control BuildAvatarFooter()
        {
            var footer = new AvatarFooter
            {
                Dock         = DockStyle.Bottom,
                Username     = Session.Username ?? "?",
                AccentColor  = Theme.Accent,
                BackColor    = Theme.SidebarBg,
                HoverColor   = Theme.SidebarBgActive,
                TextColor    = Color.White,
                ChevronColor = Theme.SidebarText,
            };
            footer.Click += (s, e) => ShowProfilePopup(footer);
            return footer;
        }

        private void ShowProfilePopup(Control anchor)
        {
            var items = new System.Collections.Generic.List<ProfilePopup.MenuItemDef>
            {
                new ProfilePopup.MenuItemDef
                {
                    Text    = "Perfil",
                    Icon    = IconChar.User,
                    OnClick = () => Navigate<UcPerfil>(),
                },
                new ProfilePopup.MenuItemDef
                {
                    Text    = ThemeManager.Current == ThemeMode.Light ? "Modo escuro" : "Modo claro",
                    Icon    = ThemeManager.Current == ThemeMode.Light ? IconChar.Moon  : IconChar.Sun,
                    OnClick = () => ThemeManager.Toggle(),
                },
                new ProfilePopup.MenuItemDef { IsSeparator = true },
                new ProfilePopup.MenuItemDef
                {
                    Text     = "Sair",
                    Icon     = IconChar.SignOutAlt,
                    IsDanger = true,
                    OnClick  = () => { LogoutRequested = true; this.Close(); },
                },
            };

            var popup = new ProfilePopup(items);
            // Posicionar à DIREITA do footer (fora da sidebar) com a base
            // do popup alinhada com a base do footer — feel "explode from
            // the side" tipo Discord/Slack.
            var screenAnchor = anchor.PointToScreen(Point.Empty);
            popup.Location = new Point(
                screenAnchor.X + anchor.Width + 6,
                screenAnchor.Y - popup.Height + anchor.Height);
            popup.Show(this);
        }

        private void AddSectionLabel(FlowLayoutPanel container, string text)
        {
            container.Controls.Add(new Label
            {
                Text      = text,
                Font      = Theme.FontMicro,
                ForeColor = Theme.SidebarSectionLbl,
                AutoSize  = false,
                Width     = 200,
                Height    = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0),     // section a x=14
                Margin    = new Padding(0, 12, 0, 6),     // mais breathing antes/depois
            });
        }

        private void AddNavItem(FlowLayoutPanel container, string label, IconChar icon, Action onClick)
        {
            var btn = new IconButton
            {
                Text                = label,
                IconChar            = icon,
                IconColor           = Theme.SidebarText,
                IconSize            = 18,
                ImageAlign          = ContentAlignment.MiddleLeft,
                TextAlign           = ContentAlignment.MiddleLeft,
                TextImageRelation   = TextImageRelation.ImageBeforeText,
                FlatStyle           = FlatStyle.Flat,
                ForeColor           = Theme.SidebarText,
                BackColor           = Color.Transparent,
                Font                = new Font(Theme.FontBase.FontFamily, 9.5f),
                Height              = 38,
                Width               = 200,
                Padding             = new Padding(26, 0, 8, 0),   // items indented vs section (x=26)
                Margin              = new Padding(0, 0, 0, 3),
                Cursor              = Cursors.Hand,
                UseVisualStyleBackColor = false,
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

        /// <summary>Navega para um UC pelo seu Type (chamável por outros UCs
        /// via cast do parent FormMain). Exemplo: ((FormMain)FindForm())
        /// .NavigateTo(typeof(UcReservas)).</summary>
        public void NavigateTo(Type ucType)
        {
            var method = typeof(FormMain).GetMethod("Navigate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method?.MakeGenericMethod(ucType).Invoke(this, null);
        }

        private void Navigate<T>() where T : Control, new()
        {
            pnlContent.Controls.Clear();
            var uc = new T { Dock = DockStyle.Fill };
            pnlContent.Controls.Add(uc);

            var names = new System.Collections.Generic.Dictionary<Type, string>
            {
                { typeof(UcDashboard),     "Dashboard"     },
                { typeof(UcClientes),      "Clientes"      },
                { typeof(UcPlanos),        "Planos"        },
                { typeof(UcEspacos),       "Espaços"       },
                { typeof(UcReservas),      "Reservas"      },
                { typeof(UcNotificacoes),  "Notificações"  },
                { typeof(UcAdesoes),       "Adesões"       },
                { typeof(UcPagamentos),    "Pagamentos"    },
                { typeof(UcRelatorios),    "Relatórios"    },
                { typeof(UcEstatisticas),  "Estatísticas"  },
                { typeof(UcUtilizadores),  "Utilizadores"  },
                { typeof(UcPerfil),        "Perfil"        }
            };
            if (names.TryGetValue(typeof(T), out string name))
                _lblModule.Text = name;
        }

        private void RefreshConnIndicator(bool ok)
        {
            _connOk = ok;
            _lblConn.Text = ok ? "LIGADO" : "SEM LIGAÇÃO";
            _lblConn.ForeColor = ok ? Theme.StatusSuccessFg : Theme.StatusDangerFg;
            _pnlConn.Invalidate();
        }
    }
}
