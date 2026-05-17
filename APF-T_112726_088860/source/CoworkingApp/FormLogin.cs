using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp
{
    /// <summary>
    /// Login — design sóbrio e profissional. Dark theme forçado, system title bar
    /// (dark via DWM), card centrado com soft shadow, paleta slate + indigo
    /// herdada do Theme.cs.
    /// </summary>
    public class FormLogin : Form
    {
        private ModernInput  _txtUser;
        private ModernInput  _txtPwd;
        private ModernButton _btnLogin;
        private Label        _lblErro;

        // DWMWA_USE_IMMERSIVE_DARK_MODE — pinta a title bar a dark em Win10/11.
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public FormLogin()
        {
            // Força dark mode no login (independentemente do que está em ThemeManager.Current).
            ThemeManager.Set(ThemeMode.Dark);

            Text                 = "Coworking";
            Icon                 = AppIcon.Get(32);
            FormBorderStyle      = FormBorderStyle.FixedDialog;
            MaximizeBox          = false;
            MinimizeBox          = false;
            StartPosition        = FormStartPosition.CenterScreen;
            ClientSize           = new Size(620, 560);
            BackColor            = Theme.PageBg;
            ForeColor            = Theme.TextPrimary;
            Font                 = Theme.FontBase;
            KeyPreview           = true;
            KeyDown             += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            DoubleBuffered       = true;

            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Title bar dark em Windows 10 (2004+) / 11. Falha silenciosa em mais velhos.
            try
            {
                int useDark = 1;
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE,
                                      ref useDark, sizeof(int));
            }
            catch { /* não é crítico */ }
        }

        private void BuildUI()
        {
            // ── Card central ─────────────────────────────────────────────
            var card = new ModernCard
            {
                Size         = new Size(540, 470),
                BackColor    = Theme.CardBg,
                BorderColor  = Color.Empty,  // sem linha à volta
                CornerRadius = 14,
                ShowShadow   = false,        // o contraste PageBg/CardBg já basta no dark
            };
            card.Location = new Point(
                (ClientSize.Width  - card.Width)  / 2,
                (ClientSize.Height - card.Height) / 2);
            Controls.Add(card);

            // Conteúdo do card
            const int padX = 32;
            int       y    = 36;

            // Logo: app icon (rounded "C" indigo, mesmo que aparece na title bar)
            var logo = new PictureBox
            {
                Image     = AppIcon.Get(40).ToBitmap(),
                Size      = new Size(40, 40),
                Location  = new Point(padX, y),
                SizeMode  = PictureBoxSizeMode.StretchImage,
                BackColor = Theme.CardBg,
            };
            card.Controls.Add(logo);

            // Título "Coworking"
            var lblTitle = new Label
            {
                Text      = "Coworking",
                Font      = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize  = false,
                Size      = new Size(card.Width - padX * 2 - 48, 40),
                Location  = new Point(padX + 52, y),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg,
            };
            card.Controls.Add(lblTitle);
            y += 56;

            // Subtítulo
            var lblSub = new Label
            {
                Text      = "Sistema de gestão de espaços",
                Font      = Theme.FontSub,
                ForeColor = Theme.TextSecondary,
                AutoSize  = false,
                Size      = new Size(card.Width - padX * 2, 18),
                Location  = new Point(padX, y),
                BackColor = Theme.CardBg,
            };
            card.Controls.Add(lblSub);
            y += 36;

            // ── Username ─────────────────────────────────────────────────
            AddLabel(card, padX, y, "Username");
            y += 22;
            _txtUser = new ModernInput
            {
                Location = new Point(padX, y),
                Size     = new Size(card.Width - padX * 2, 42),
            };
            card.Controls.Add(_txtUser);
            y += 56;

            // ── Password ─────────────────────────────────────────────────
            AddLabel(card, padX, y, "Password");
            y += 22;
            _txtPwd = new ModernInput
            {
                Location              = new Point(padX, y),
                Size                  = new Size(card.Width - padX * 2, 42),
                UseSystemPasswordChar = true,
                TrailingIcon          = IconChar.Eye,
            };
            _txtPwd.TrailingIconClicked += (s, e) =>
            {
                _txtPwd.UseSystemPasswordChar = !_txtPwd.UseSystemPasswordChar;
                _txtPwd.TrailingIcon = _txtPwd.UseSystemPasswordChar
                    ? IconChar.Eye
                    : IconChar.EyeSlash;
            };
            card.Controls.Add(_txtPwd);
            y += 56;

            // ── Botão Entrar ─────────────────────────────────────────────
            _btnLogin = new ModernButton
            {
                Text     = "Entrar",
                Style    = ModernButton.Variant.Primary,
                Location = new Point(padX, y),
                Size     = new Size(card.Width - padX * 2, 44),
            };
            _btnLogin.Click += BtnLogin_Click;
            card.Controls.Add(_btnLogin);
            y += _btnLogin.Height + 12;

            // ── Link "Criar conta" ───────────────────────────────────────
            var lnkRegister = new Label
            {
                Text      = "Não tem conta? Criar conta",
                Font      = Theme.FontSub,
                ForeColor = Theme.Accent,
                AutoSize  = false,
                Size      = new Size(card.Width - padX * 2, 20),
                Location  = new Point(padX, y),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.CardBg,
                Cursor    = Cursors.Hand,
            };
            lnkRegister.MouseEnter += (s, e) => lnkRegister.Font = new Font(Theme.FontSub, FontStyle.Underline);
            lnkRegister.MouseLeave += (s, e) => lnkRegister.Font = Theme.FontSub;
            lnkRegister.Click      += OpenRegister;
            card.Controls.Add(lnkRegister);
            y += 24;

            // ── Erro ─────────────────────────────────────────────────────
            _lblErro = new Label
            {
                Font      = Theme.FontSub,
                ForeColor = Theme.StatusDangerFg,
                BackColor = Theme.CardBg,
                AutoSize  = false,
                Size      = new Size(card.Width - padX * 2, 20),
                Location  = new Point(padX, y),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible   = false,
            };
            card.Controls.Add(_lblErro);

            this.AcceptButton = _btnLogin;
        }

        private void OpenRegister(object sender, EventArgs e)
        {
            using (var reg = new FormRegister())
            {
                if (reg.ShowDialog(this) == DialogResult.OK)
                {
                    // Registo bem-sucedido — FormRegister fez auto-login via Session.
                    // Propagamos OK para o Program.cs abrir o FormMain.
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
        }

        private static void AddLabel(Control parent, int x, int y, string text)
        {
            parent.Controls.Add(new Label
            {
                Text       = text,
                Font       = Theme.FontLabel,
                ForeColor  = Theme.TextSecondary,
                AutoSize   = true,
                Location   = new Point(x, y),
                BackColor  = Theme.CardBg,
            });
        }

        // ── Auth ─────────────────────────────────────────────────────────
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
}
