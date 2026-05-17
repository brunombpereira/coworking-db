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
    /// Auto-registo de cliente. Cria cliente + utilizador (role=Cliente)
    /// numa única transacção (sp_registar_cliente_completo) e faz auto-login
    /// imediato.
    /// </summary>
    public class FormRegister : Form
    {
        private ModernInput  _txtNome, _txtNif, _txtEmail, _txtTelefone;
        private ModernInput  _txtUser, _txtPwd, _txtPwdConfirm;
        private ModernButton _btnCriar;
        private Label        _lblErro;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public FormRegister()
        {
            Text             = "Criar conta — Coworking";
            Icon             = AppIcon.Get(32);
            FormBorderStyle  = FormBorderStyle.FixedDialog;
            MaximizeBox      = false;
            MinimizeBox      = false;
            StartPosition    = FormStartPosition.CenterParent;
            ClientSize       = new Size(620, 720);
            BackColor        = Theme.PageBg;
            ForeColor        = Theme.TextPrimary;
            Font             = Theme.FontBase;
            KeyPreview       = true;
            KeyDown         += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            DoubleBuffered   = true;

            BuildUI();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int useDark = 1;
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
            }
            catch { }
        }

        private void BuildUI()
        {
            var card = new ModernCard
            {
                Size         = new Size(540, 650),
                BackColor    = Theme.CardBg,
                BorderColor  = Color.Empty,
                CornerRadius = 14,
                ShowShadow   = false,
            };
            card.Location = new Point(
                (ClientSize.Width  - card.Width)  / 2,
                (ClientSize.Height - card.Height) / 2);
            Controls.Add(card);

            const int padX = 32;
            int       y    = 28;

            // Logo + título
            var logo = new PictureBox
            {
                Image     = AppIcon.Get(40).ToBitmap(),
                Size      = new Size(40, 40),
                Location  = new Point(padX, y),
                SizeMode  = PictureBoxSizeMode.StretchImage,
                BackColor = Theme.CardBg,
            };
            card.Controls.Add(logo);

            var lblTitle = new Label
            {
                Text      = "Criar conta",
                Font      = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize  = false,
                Size      = new Size(card.Width - padX * 2 - 48, 40),
                Location  = new Point(padX + 52, y),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.CardBg,
            };
            card.Controls.Add(lblTitle);
            y += 50;

            var lblSub = new Label
            {
                Text      = "Junte-se ao Coworking — preencha os seus dados",
                Font      = Theme.FontSub,
                ForeColor = Theme.TextSecondary,
                AutoSize  = false,
                Size      = new Size(card.Width - padX * 2, 18),
                Location  = new Point(padX, y),
                BackColor = Theme.CardBg,
            };
            card.Controls.Add(lblSub);
            y += 32;

            int fullWidth = card.Width - padX * 2;
            int halfWidth = (fullWidth - 12) / 2;

            // ── Nome (full) ──────────────────────────────────────────────
            y += AddLabeledField(card, padX, y, fullWidth, "Nome completo", out _txtNome);

            // ── NIF | Telefone (2 cols) ──────────────────────────────────
            int rowStart = y;
            AddLabeledField(card, padX,                     y, halfWidth, "NIF (9 dígitos)", out _txtNif);
            int afterRow = AddLabeledField(card, padX + halfWidth + 12, rowStart, halfWidth, "Telefone (opcional)", out _txtTelefone);
            y += afterRow;

            // ── Email ────────────────────────────────────────────────────
            y += AddLabeledField(card, padX, y, fullWidth, "Email", out _txtEmail);

            // ── Username ─────────────────────────────────────────────────
            y += AddLabeledField(card, padX, y, fullWidth, "Username", out _txtUser);

            // ── Password com toggle ──────────────────────────────────────
            y += AddLabeledField(card, padX, y, fullWidth, "Password (mín. 8 caracteres)", out _txtPwd, true);

            // ── Confirmar password com toggle ────────────────────────────
            y += AddLabeledField(card, padX, y, fullWidth, "Confirmar password", out _txtPwdConfirm, true);

            y += 6;

            // ── Botão Criar conta ────────────────────────────────────────
            _btnCriar = new ModernButton
            {
                Text     = "Criar conta",
                Style    = ModernButton.Variant.Primary,
                Location = new Point(padX, y),
                Size     = new Size(fullWidth, 44),
            };
            _btnCriar.Click += BtnCriar_Click;
            card.Controls.Add(_btnCriar);
            y += _btnCriar.Height + 8;

            // ── Link voltar ao login ─────────────────────────────────────
            var lnkVoltar = new Label
            {
                Text      = "Já tem conta? Voltar ao login",
                Font      = Theme.FontSub,
                ForeColor = Theme.Accent,
                AutoSize  = false,
                Size      = new Size(fullWidth, 20),
                Location  = new Point(padX, y),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.CardBg,
                Cursor    = Cursors.Hand,
            };
            lnkVoltar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            lnkVoltar.MouseEnter += (s, e) => lnkVoltar.Font = new Font(Theme.FontSub, FontStyle.Underline);
            lnkVoltar.MouseLeave += (s, e) => lnkVoltar.Font = Theme.FontSub;
            card.Controls.Add(lnkVoltar);
            y += 24;

            // ── Erro ─────────────────────────────────────────────────────
            _lblErro = new Label
            {
                Font      = Theme.FontSub,
                ForeColor = Theme.StatusDangerFg,
                BackColor = Theme.CardBg,
                AutoSize  = false,
                Size      = new Size(fullWidth, 20),
                Location  = new Point(padX, y),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible   = false,
            };
            card.Controls.Add(_lblErro);

            AcceptButton = _btnCriar;
        }

        /// <summary>Adiciona label + ModernInput; devolve altura consumida.</summary>
        private int AddLabeledField(Control parent, int x, int y, int width, string label,
                                    out ModernInput field, bool password = false)
        {
            int startY = y;

            parent.Controls.Add(new Label
            {
                Text       = label,
                Font       = Theme.FontLabel,
                ForeColor  = Theme.TextSecondary,
                AutoSize   = true,
                Location   = new Point(x, y),
                BackColor  = Theme.CardBg,
            });
            y += 20;

            field = new ModernInput
            {
                Location              = new Point(x, y),
                Size                  = new Size(width, 42),
                UseSystemPasswordChar = password,
            };
            if (password)
            {
                var f = field;
                f.TrailingIcon = IconChar.Eye;
                f.TrailingIconClicked += (s, e) =>
                {
                    f.UseSystemPasswordChar = !f.UseSystemPasswordChar;
                    f.TrailingIcon = f.UseSystemPasswordChar ? IconChar.Eye : IconChar.EyeSlash;
                };
            }
            parent.Controls.Add(field);
            y += field.Height + 10;

            return y - startY;
        }

        // ── Submit ──────────────────────────────────────────────────────
        private void BtnCriar_Click(object sender, EventArgs e)
        {
            _lblErro.Visible = false;

            string nome     = _txtNome.Text?.Trim();
            string nif      = _txtNif.Text?.Trim();
            string email    = _txtEmail.Text?.Trim();
            string telefone = string.IsNullOrWhiteSpace(_txtTelefone.Text) ? null : _txtTelefone.Text.Trim();
            string username = _txtUser.Text?.Trim();
            string pwd      = _txtPwd.Text;
            string pwdConf  = _txtPwdConfirm.Text;

            // Validações client-side
            if (string.IsNullOrEmpty(nome)     ||
                string.IsNullOrEmpty(nif)      ||
                string.IsNullOrEmpty(email)    ||
                string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(pwd))
            {
                ShowError("Preencha todos os campos obrigatórios.");
                return;
            }

            if (nif.Length != 9 || !long.TryParse(nif, out _))
            {
                ShowError("NIF deve ter exactamente 9 dígitos.");
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                ShowError("Email inválido.");
                return;
            }

            if (pwd.Length < 8)
            {
                ShowError("Password deve ter pelo menos 8 caracteres.");
                return;
            }

            if (pwd != pwdConf)
            {
                ShowError("As passwords não coincidem.");
                return;
            }

            _btnCriar.Enabled = false;
            Cursor            = Cursors.WaitCursor;

            try
            {
                int novoClienteId, novoUtilizadorId;

                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("sp_registar_cliente_completo", conn)
                       { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@nome",     nome);
                    cmd.Parameters.AddWithValue("@nif",      nif);
                    cmd.Parameters.AddWithValue("@email",    email);
                    cmd.Parameters.AddWithValue("@telefone", (object)telefone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", pwd);

                    var pCliente = new SqlParameter("@cliente_id",    SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var pUser    = new SqlParameter("@utilizador_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(pCliente);
                    cmd.Parameters.Add(pUser);

                    cmd.ExecuteNonQuery();

                    novoClienteId    = Convert.ToInt32(pCliente.Value);
                    novoUtilizadorId = Convert.ToInt32(pUser.Value);
                }

                // Auto-login (a sessão fica pronta para o FormMain a seguir)
                Session.Login(novoUtilizadorId, username, "Cliente", novoClienteId);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqlException ex)
            {
                ShowError(Database.SqlErrorMessage(ex));
            }
            finally
            {
                _btnCriar.Enabled = true;
                Cursor            = Cursors.Default;
            }
        }

        private void ShowError(string msg)
        {
            _lblErro.Text    = msg;
            _lblErro.Visible = true;
        }
    }
}
