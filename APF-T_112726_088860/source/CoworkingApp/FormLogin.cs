using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp
{
    public class FormLogin : DpiAwareForm
    {
        private TextBox _txtUser;
        private TextBox _txtPwd;
        private Button  _btnLogin;
        private Label   _lblErro;

        public FormLogin()
        {
            // Mode antes de Dimensions (senão Dimensions é sobrescrito).
            this.AutoScaleMode       = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.Text                = "Coworking — Login";
            this.StartPosition       = FormStartPosition.CenterScreen;
            this.FormBorderStyle     = FormBorderStyle.FixedSingle;
            this.MaximizeBox         = false;
            this.MinimizeBox         = false;
            this.Size                = new Size(420, 360);
            this.BackColor           = Theme.PageBg;
            this.Font                = Theme.FontBase;
            BuildUI();
            this.AcceptButton        = _btnLogin;
        }

        private void BuildUI()
        {
            var pnlCard = new Panel
            {
                BackColor = Theme.CardBg,
                Size      = new Size(360, 280),
                Location  = new Point(22, 22),
                Padding   = new Padding(28)
            };

            var lblTitle = new Label
            {
                Text      = "Bem-vindo",
                Font      = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(28, 22)
            };

            var lblSub = new Label
            {
                Text      = "Inicie sessão para continuar.",
                Font      = Theme.FontSub,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Location  = new Point(28, 58)
            };

            var lblUser = new Label
            {
                Text      = "Username",
                Font      = Theme.FontLabel,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Location  = new Point(28, 92)
            };
            _txtUser = new TextBox
            {
                Font        = Theme.FontBase,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Theme.FieldBg,
                ForeColor   = Theme.TextPrimary,
                Location    = new Point(28, 112),
                Size        = new Size(300, 26)
            };

            var lblPwd = new Label
            {
                Text      = "Password",
                Font      = Theme.FontLabel,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Location  = new Point(28, 146)
            };
            _txtPwd = new TextBox
            {
                Font            = Theme.FontBase,
                BorderStyle     = BorderStyle.FixedSingle,
                BackColor       = Theme.FieldBg,
                ForeColor       = Theme.TextPrimary,
                Location        = new Point(28, 166),
                Size            = new Size(300, 26),
                UseSystemPasswordChar = true
            };

            _btnLogin = Theme.BtnPrim("Entrar");
            _btnLogin.Location = new Point(28, 208);
            _btnLogin.Size     = new Size(300, 36);
            _btnLogin.Click   += BtnLogin_Click;

            _lblErro = new Label
            {
                ForeColor = Theme.StatusDangerFg,
                BackColor = Color.Transparent,
                AutoSize  = false,
                Size      = new Size(300, 18),
                Location  = new Point(28, 250),
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = Theme.FontSub,
                Visible   = false
            };

            pnlCard.Controls.Add(lblTitle);
            pnlCard.Controls.Add(lblSub);
            pnlCard.Controls.Add(lblUser);
            pnlCard.Controls.Add(_txtUser);
            pnlCard.Controls.Add(lblPwd);
            pnlCard.Controls.Add(_txtPwd);
            pnlCard.Controls.Add(_btnLogin);
            pnlCard.Controls.Add(_lblErro);

            this.Controls.Add(pnlCard);
        }

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
            this.Cursor = Cursors.WaitCursor;

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
                        int  uid  = Convert.ToInt32(rdr["utilizador_id"]);
                        string r  = rdr["role"].ToString();
                        int? cid  = rdr["cliente_id"] == DBNull.Value
                                  ? (int?)null
                                  : Convert.ToInt32(rdr["cliente_id"]);

                        Session.Login(uid, user, r, cid);
                    }
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (SqlException ex)
            {
                ShowError(Database.SqlErrorMessage(ex));
            }
            finally
            {
                _btnLogin.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        private void ShowError(string msg)
        {
            _lblErro.Text    = msg;
            _lblErro.Visible = true;
        }
    }
}
