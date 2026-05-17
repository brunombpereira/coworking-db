using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    /// <summary>
    /// Login redesenhado — dark glassmorphism / neon.
    /// • Form borderless com title bar custom (drag + close).
    /// • Background com 3 "orbs" semi-transparentes a fakear o glow de glass.
    /// • Card central NeonPanel com border gradient cyan→magenta.
    /// • Título "COWORKING" com efeito text-glow.
    /// • Campos NeonTextBox com ícone à esquerda.
    /// • Botão "ENTRAR" NeonButton em hover/pressed states.
    /// </summary>
    public class FormLogin : Form
    {
        private NeonTextBox _txtUser;
        private NeonTextBox _txtPwd;
        private NeonButton  _btnLogin;
        private Label       _lblErro;

        // ── Custom title bar drag ────────────────────────────────────────
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr h, int m, int w, int l);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION       = 0x2;

        // ── Dark title bar (legacy fallback se borderless não for usado) ─
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

        public FormLogin()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.UserPaint, true);

            Text                 = "Coworking — Login";
            FormBorderStyle      = FormBorderStyle.None;
            StartPosition        = FormStartPosition.CenterScreen;
            ShowInTaskbar        = true;
            BackColor            = NeonStyle.BgBase;
            ForeColor            = NeonStyle.TextPrimary;
            Font                 = NeonStyle.FontBody;
            ClientSize           = new Size(520, 620);
            DoubleBuffered       = true;
            KeyPreview           = true;
            KeyDown             += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            BuildUI();
        }

        // ── Background pintado: gradient + orbs neon ─────────────────────
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Fundo: gradient diagonal de BgDeep → BgBase
            using (var bg = new LinearGradientBrush(ClientRectangle,
                       NeonStyle.BgDeep, NeonStyle.BgBase, 135f))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            // Orbs decorativos (atrás do card) — fake glassmorphism
            DrawOrb(g, new Point(70,  120), 180, NeonStyle.NeonViolet);
            DrawOrb(g, new Point(450, 470), 220, NeonStyle.NeonMagenta);
            DrawOrb(g, new Point(420, 100), 130, NeonStyle.NeonCyan);
        }

        private static void DrawOrb(Graphics g, Point center, int radius, Color color)
        {
            // Multi-pass: ellipses crescentes com alpha decrescente → glow soft
            for (int i = 8; i >= 1; i--)
            {
                int r     = radius + i * 8;
                int alpha = (int)(45.0 * (1.0 - (double)i / 8) + 5);
                var rect  = new Rectangle(center.X - r / 2, center.Y - r / 2, r, r);
                using (var path  = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    using (var brush = new PathGradientBrush(path)
                    {
                        CenterColor    = NeonStyle.WithAlpha(color, alpha),
                        SurroundColors = new[] { Color.FromArgb(0, color) }
                    })
                    {
                        g.FillPath(brush, path);
                    }
                }
            }
        }

        private void BuildUI()
        {
            // ── Title bar custom (32px alto) ─────────────────────────────
            var titleBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 32,
                BackColor = Color.Transparent,
            };
            titleBar.MouseDown += TitleBar_MouseDown;

            var btnClose = new IconButton
            {
                IconChar           = IconChar.Xmark,
                IconColor          = NeonStyle.TextSecondary,
                IconSize           = 16,
                BackColor          = Color.Transparent,
                ForeColor          = NeonStyle.TextSecondary,
                FlatStyle          = FlatStyle.Flat,
                Size               = new Size(40, 32),
                Dock               = DockStyle.Right,
                Cursor             = Cursors.Hand,
                TabStop            = false,
            };
            btnClose.FlatAppearance.BorderSize       = 0;
            btnClose.FlatAppearance.MouseOverBackColor = NeonStyle.WithAlpha(NeonStyle.NeonRed, 60);
            btnClose.Click            += (s, e) => Close();
            btnClose.MouseEnter       += (s, e) => btnClose.IconColor = NeonStyle.NeonRed;
            btnClose.MouseLeave       += (s, e) => btnClose.IconColor = NeonStyle.TextSecondary;
            titleBar.Controls.Add(btnClose);

            // Brand pequena no canto esquerdo do title bar
            var brandSmall = new Label
            {
                Text      = "  ◆  COWORKING",
                Font      = NeonStyle.FontCapsBold,
                ForeColor = NeonStyle.TextMuted,
                Dock      = DockStyle.Left,
                Width     = 200,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(12, 0, 0, 0),
            };
            brandSmall.MouseDown += TitleBar_MouseDown;
            titleBar.Controls.Add(brandSmall);
            Controls.Add(titleBar);

            // ── Card central (420×500) ───────────────────────────────────
            var card = new NeonPanel
            {
                Size         = new Size(420, 500),
                BorderColor1 = NeonStyle.NeonCyan,
                BorderColor2 = NeonStyle.NeonMagenta,
                CornerRadius = NeonStyle.RadiusLg,
            };
            card.Location = new Point(
                (ClientSize.Width  - card.Width)  / 2,
                (ClientSize.Height - card.Height) / 2 + 8);
            Controls.Add(card);
            card.BringToFront();

            // ── Conteúdo do card ─────────────────────────────────────────
            const int padX = 36;
            int       y    = 38;

            // Hero title com text-glow (paint custom via Label override? Simpler: usamos paint manual via OnPaint do card... ou só Label normal com cor neon — visualmente bate ok)
            var lblHero = new GlowLabel
            {
                Text       = "COWORKING",
                Font       = NeonStyle.FontHero,
                ForeColor  = NeonStyle.NeonCyan,
                GlowColor  = NeonStyle.NeonCyan,
                AutoSize   = false,
                Size       = new Size(card.Width - padX * 2, 44),
                Location   = new Point(padX, y),
                TextAlign  = ContentAlignment.MiddleLeft,
                BackColor  = Color.Transparent,
            };
            card.Controls.Add(lblHero);
            y += lblHero.Height + 2;

            var lblSub = new Label
            {
                Text      = "Sistema de Gestão · Inicie sessão",
                Font      = NeonStyle.FontCaption,
                ForeColor = NeonStyle.TextSecondary,
                AutoSize  = false,
                Size      = new Size(card.Width - padX * 2, 18),
                Location  = new Point(padX, y),
                BackColor = Color.Transparent,
            };
            card.Controls.Add(lblSub);
            y += lblSub.Height + NeonStyle.Sp6;

            // ── Username ─────────────────────────────────────────────────
            y += AddField(card, padX, y, IconChar.User, "USERNAME", out _txtUser, false);

            // ── Password ─────────────────────────────────────────────────
            y += AddField(card, padX, y, IconChar.Lock, "PASSWORD", out _txtPwd, true);

            y += NeonStyle.Sp3;

            // ── Botão Entrar ─────────────────────────────────────────────
            _btnLogin = new NeonButton
            {
                Text     = "ENTRAR",
                Location = new Point(padX, y),
                Size     = new Size(card.Width - padX * 2, 48),
                Color1   = NeonStyle.NeonCyan,
                Color2   = NeonStyle.NeonMagenta,
            };
            _btnLogin.Click += BtnLogin_Click;
            card.Controls.Add(_btnLogin);
            y += _btnLogin.Height + NeonStyle.Sp3;

            // ── Erro ─────────────────────────────────────────────────────
            _lblErro = new Label
            {
                Font      = NeonStyle.FontCaption,
                ForeColor = NeonStyle.NeonRed,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(card.Width - padX * 2, 20),
                Location  = new Point(padX, y),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible   = false,
            };
            card.Controls.Add(_lblErro);

            this.AcceptButton = _btnLogin;
        }

        /// <summary>Adiciona icon + label + NeonTextBox; devolve a altura consumida.</summary>
        private int AddField(NeonPanel parent, int x, int y, IconChar icon, string label,
                             out NeonTextBox field, bool password)
        {
            int startY = y;

            // Linha icon + label
            var iconLbl = new IconPictureBox
            {
                IconChar         = icon,
                IconColor        = NeonStyle.NeonCyan,
                IconSize         = 12,
                BackColor        = Color.Transparent,
                Size             = new Size(14, 14),
                Location         = new Point(x, y + 2),
                SizeMode         = PictureBoxSizeMode.AutoSize,
            };
            parent.Controls.Add(iconLbl);

            var lbl = new Label
            {
                Text       = label,
                Font       = NeonStyle.FontCapsBold,
                ForeColor  = NeonStyle.TextSecondary,
                AutoSize   = true,
                Location   = new Point(x + 20, y),
                BackColor  = Color.Transparent,
            };
            parent.Controls.Add(lbl);
            y += 22;

            // NeonTextBox
            field = new NeonTextBox
            {
                Location              = new Point(x, y),
                Size                  = new Size(parent.Width - x * 2, 42),
                UseSystemPasswordChar = password,
            };
            parent.Controls.Add(field);
            y += field.Height + NeonStyle.Sp3;

            return y - startY;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        // ── Lógica de login (igual à versão anterior) ────────────────────
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            _lblErro.Visible = false;

            string user = _txtUser.Text?.Trim();
            string pwd  = _txtPwd.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pwd))
            {
                ShowError("Preencha username e password.");
                return;
            }

            _btnLogin.Enabled = false;
            this.Cursor       = Cursors.WaitCursor;

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("sp_login_user", conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@username", user);
                    cmd.Parameters.AddWithValue("@password", pwd);

                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (!rdr.Read() || rdr["utilizador_id"] == DBNull.Value)
                        {
                            ShowError("Credenciais inválidas.");
                            return;
                        }
                        int    uid = Convert.ToInt32(rdr["utilizador_id"]);
                        string r   = rdr["role"].ToString();
                        int?   cid = rdr["cliente_id"] == DBNull.Value
                                   ? (int?)null
                                   : Convert.ToInt32(rdr["cliente_id"]);
                        Session.Login(uid, user, r, cid);
                    }
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqlException ex)
            {
                ShowError(Database.SqlErrorMessage(ex));
            }
            finally
            {
                _btnLogin.Enabled = true;
                this.Cursor       = Cursors.Default;
            }
        }

        private void ShowError(string msg)
        {
            _lblErro.Text    = msg;
            _lblErro.Visible = true;
        }
    }

    /// <summary>
    /// Label que desenha o texto com efeito glow (mesmo texto pintado N vezes
    /// com alpha decrescente em torno da posição final).
    /// </summary>
    internal class GlowLabel : Label
    {
        public Color GlowColor   { get; set; } = NeonStyle.NeonCyan;
        public int   GlowSpread  { get; set; } = 4;
        public int   GlowPasses  { get; set; } = 6;

        public GlowLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { /* skip */ }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            if (Parent != null)
            {
                using (var bg = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(bg, ClientRectangle);
            }

            var fmt = StringAlignment.Near;
            if (TextAlign == ContentAlignment.MiddleCenter || TextAlign == ContentAlignment.TopCenter
             || TextAlign == ContentAlignment.BottomCenter) fmt = StringAlignment.Center;

            using (var sf = new StringFormat
                {
                    Alignment     = fmt,
                    LineAlignment = StringAlignment.Center
                })
            {
                // Glow halo (multi-pass com offsets em x e y)
                for (int pass = GlowPasses; pass >= 1; pass--)
                {
                    int   spread = (GlowSpread * pass) / GlowPasses;
                    int   alpha  = (int)(80.0 * (1.0 - (double)pass / GlowPasses) + 10);
                    using (var glowBrush = new SolidBrush(NeonStyle.WithAlpha(GlowColor, alpha)))
                    {
                        for (int dx = -spread; dx <= spread; dx += spread)
                        for (int dy = -spread; dy <= spread; dy += spread)
                        {
                            if (dx == 0 && dy == 0) continue;
                            var r = new RectangleF(dx, dy, Width, Height);
                            g.DrawString(Text, Font, glowBrush, r, sf);
                        }
                    }
                }
                // Texto principal por cima
                using (var fg = new SolidBrush(ForeColor))
                    g.DrawString(Text, Font, fg, ClientRectangle, sf);
            }
        }
    }
}
