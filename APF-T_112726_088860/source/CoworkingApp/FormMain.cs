using System;
using System.Collections.Generic;
using System.Drawing;
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
        private Button _activeBtn;
        private readonly List<Button> _navBtns = new List<Button>();
        private Label _lblModule;
        private System.Windows.Forms.Timer _clockTimer;

        public FormMain()
        {
            LogoutRequested = false;
            InitializeComponent();
            BuildUI();
            if (_navBtns.Count > 0) _navBtns[0].PerformClick();

            // Reaplica title bar dark quando o tema mudar (toggle no footer).
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
        private Panel BuildAvatarFooter()
        {
            var footer = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = Theme.SidebarBg,
                Cursor    = Cursors.Hand,
            };

            // Linha divisória subtil
            var divider = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = Color.FromArgb(30, Theme.SidebarText),
            };
            footer.Controls.Add(divider);

            var avatar = new AvatarCircle
            {
                Initial     = Session.Username ?? "?",
                CircleColor = Theme.Accent,
                Size        = new Size(36, 36),
                Location    = new Point(16, 14),
            };
            footer.Controls.Add(avatar);

            var lblName = new Label
            {
                Text      = Session.Username ?? "—",
                ForeColor = Color.White,
                Font      = new Font(Theme.FontBase.FontFamily, 10f, FontStyle.Bold),
                BackColor = Theme.SidebarBg,
                AutoSize  = false,
                Size      = new Size(140, 20),
                Location  = new Point(60, 22),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            footer.Controls.Add(lblName);

            var chevron = new IconPictureBox
            {
                IconChar  = IconChar.EllipsisVertical,
                IconColor = Theme.SidebarText,
                IconSize  = 16,
                Size      = new Size(20, 22),
                Location  = new Point(190, 22),
                SizeMode  = PictureBoxSizeMode.CenterImage,
                BackColor = Theme.SidebarBg,
            };
            footer.Controls.Add(chevron);

            // ContextMenuStrip
            var menu = BuildProfileMenu();

            void ShowMenu()
            {
                menu.Show(footer, new Point(footer.Width - menu.Width - 4,
                                            -menu.Height + 4));
            }
            footer.Click  += (s, e) => ShowMenu();
            avatar.Click  += (s, e) => ShowMenu();
            lblName.Click += (s, e) => ShowMenu();
            chevron.Click += (s, e) => ShowMenu();

            // Hover feedback (subtle background change)
            void SetHover(bool on)
            {
                Color bg = on ? Theme.SidebarBgActive : Theme.SidebarBg;
                footer.BackColor  = bg;
                lblName.BackColor = bg;
                chevron.BackColor = bg;
                divider.BackColor = Color.FromArgb(30, Theme.SidebarText);
            }
            footer.MouseEnter += (s, e) => SetHover(true);
            footer.MouseLeave += (s, e) => SetHover(false);

            return footer;
        }

        private ContextMenuStrip BuildProfileMenu()
        {
            var menu = new ContextMenuStrip
            {
                BackColor       = Theme.CardBg,
                ForeColor       = Theme.TextPrimary,
                Font            = Theme.FontBase,
                ShowImageMargin = true,
                RenderMode      = ToolStripRenderMode.Professional,
                Renderer        = new DarkMenuRenderer(),
            };

            var miPerfil = new ToolStripMenuItem("Perfil")
            {
                Image     = IconToImage(IconChar.User, 16, Theme.TextSecondary),
                ForeColor = Theme.TextPrimary,
            };
            miPerfil.Click += (s, e) => Navigate<UcPerfil>();

            string themeText = ThemeManager.Current == ThemeMode.Light ? "Modo escuro" : "Modo claro";
            IconChar themeIcon = ThemeManager.Current == ThemeMode.Light ? IconChar.Moon : IconChar.Sun;
            var miTema = new ToolStripMenuItem(themeText)
            {
                Image     = IconToImage(themeIcon, 16, Theme.TextSecondary),
                ForeColor = Theme.TextPrimary,
            };
            miTema.Click += (s, e) =>
            {
                ThemeManager.Toggle();
                miTema.Text  = ThemeManager.Current == ThemeMode.Light ? "Modo escuro" : "Modo claro";
                miTema.Image = IconToImage(
                    ThemeManager.Current == ThemeMode.Light ? IconChar.Moon : IconChar.Sun,
                    16, Theme.TextSecondary);
            };

            var miSair = new ToolStripMenuItem("Sair")
            {
                Image     = IconToImage(IconChar.SignOutAlt, 16, Theme.StatusDangerFg),
                ForeColor = Theme.TextPrimary,
            };
            miSair.Click += (s, e) =>
            {
                LogoutRequested = true;
                this.Close();
            };

            menu.Items.Add(miPerfil);
            menu.Items.Add(miTema);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miSair);
            return menu;
        }

        private static Image IconToImage(IconChar icon, int size, Color color)
        {
            // Render via IconPictureBox + DrawToBitmap (mais fiável que ler pb.Image
            // directamente, que pode ser null antes do primeiro paint).
            using (var pb = new IconPictureBox
                   {
                       IconChar  = icon,
                       IconSize  = size,
                       IconColor = color,
                       Size      = new Size(size, size),
                       BackColor = Theme.CardBg,
                       SizeMode  = PictureBoxSizeMode.AutoSize,
                   })
            {
                var bmp = new Bitmap(size, size);
                pb.DrawToBitmap(bmp, new Rectangle(0, 0, size, size));
                return bmp;
            }
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
    }
}
