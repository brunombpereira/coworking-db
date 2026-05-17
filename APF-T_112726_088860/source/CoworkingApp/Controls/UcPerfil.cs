using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Página de perfil do utilizador autenticado. Mostra avatar/inicial,
    /// username, role e (se Cliente) os detalhes do cliente associado.
    /// </summary>
    public class UcPerfil : UserControl
    {
        public UcPerfil()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 56, BackColor = Theme.PageBg,
                Padding = new Padding(20, 14, 20, 0)
            };
            pnlTitle.Controls.Add(new Label
            {
                Text = "Perfil", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false
            });
            Controls.Add(pnlTitle);

            var card = new ModernCard
            {
                Size         = new Size(620, 420),
                BackColor    = Theme.CardBg,
                BorderColor  = Color.Empty,
                CornerRadius = 14,
                ShowShadow   = false,
                Location     = new Point(28, 28 + 56),
            };
            Controls.Add(card);

            const int padX = 32;
            int       y    = 32;

            // ── Avatar grande (80×80) ───────────────────────────────────
            var avatar = new AvatarCircle
            {
                Initial  = Session.Username ?? "?",
                Size     = new Size(80, 80),
                Location = new Point(padX, y),
            };
            card.Controls.Add(avatar);

            // ── Username + role badge ───────────────────────────────────
            var lblUser = new Label
            {
                Text      = Session.Username ?? "—",
                Font      = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg,
                AutoSize  = true,
                Location  = new Point(padX + 96, y + 8),
            };
            card.Controls.Add(lblUser);

            var lblRole = new Label
            {
                Text      = Session.Role,
                Font      = Theme.FontMicro,
                ForeColor = Color.White,
                BackColor = Theme.Accent,
                AutoSize  = false,
                Size      = new Size(60, 22),
                Location  = new Point(padX + 96, y + 48),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding   = new Padding(0),
            };
            card.Controls.Add(lblRole);
            y += 110;

            // ── Detalhes do cliente (se aplicável) ──────────────────────
            if (Session.IsCliente && Session.ClienteId.HasValue)
            {
                LoadClienteDetails(card, padX, y);
            }
            else
            {
                card.Controls.Add(new Label
                {
                    Text      = Session.IsAdmin
                                    ? "Administrador do sistema — sem cliente associado."
                                    : "Membro do staff — sem cliente associado.",
                    Font      = Theme.FontBase,
                    ForeColor = Theme.TextSecondary,
                    BackColor = Theme.CardBg,
                    AutoSize  = false,
                    Size      = new Size(card.Width - padX * 2, 24),
                    Location  = new Point(padX, y),
                });
                y += 40;
            }

            // ── Botão alterar password ──────────────────────────────────
            var btnChangePwd = Theme.BtnPrim("Alterar password");
            btnChangePwd.Width    = 180;
            btnChangePwd.Location = new Point(padX, card.Height - 64);
            btnChangePwd.Click   += OpenChangePassword;
            card.Controls.Add(btnChangePwd);
        }

        private void LoadClienteDetails(Control parent, int x, int y)
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand(
                    @"SELECT nome, nif, email, telefone,
                             CONVERT(varchar, data_registo, 103) AS registo
                      FROM   cliente WHERE cliente_id = @cid", conn))
                {
                    cmd.Parameters.AddWithValue("@cid", Session.ClienteId.Value);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            AddFieldRow(parent, x, y +   0, "Nome",     rdr["nome"].ToString());
                            AddFieldRow(parent, x, y +  44, "NIF",      rdr["nif"].ToString());
                            AddFieldRow(parent, x, y +  88, "Email",    rdr["email"].ToString());
                            AddFieldRow(parent, x, y + 132, "Telefone", rdr["telefone"]?.ToString() ?? "—");
                            AddFieldRow(parent, x, y + 176, "Registo",  rdr["registo"].ToString());
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void AddFieldRow(Control parent, int x, int y, string label, string value)
        {
            parent.Controls.Add(new Label
            {
                Text      = label.ToUpper(),
                Font      = Theme.FontMicro,
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg,
                AutoSize  = true,
                Location  = new Point(x, y),
            });
            parent.Controls.Add(new Label
            {
                Text      = value,
                Font      = Theme.FontBase,
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg,
                AutoSize  = true,
                Location  = new Point(x, y + 16),
            });
        }

        private void OpenChangePassword(object sender, EventArgs e)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };
            var txtAtual = AddInput(tbl, "Password actual *", password: true);
            var txtNova  = AddInput(tbl, "Nova password * (≥ 8)", password: true);

            using (var dlg = new FormDialog("Alterar password", tbl, 380, () =>
            {
                if (txtNova.Text.Length < 8)
                    throw new ApplicationException("Nova password deve ter ≥ 8 caracteres.");

                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("sp_change_password", conn)
                       { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@utilizador_id",  Session.UtilizadorId);
                    cmd.Parameters.AddWithValue("@password_atual", txtAtual.Text);
                    cmd.Parameters.AddWithValue("@password_nova",  txtNova.Text);
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    MessageBox.Show("Password alterada com sucesso.", "OK",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static TextBox AddInput(TableLayoutPanel tbl, string label, bool password = false)
        {
            tbl.Controls.Add(new Label
            {
                Text = label, Font = Theme.FontLabel, ForeColor = Theme.TextSecondary,
                AutoSize = true, Margin = new Padding(0, 6, 0, 2),
            });
            var tb = new TextBox
            {
                Dock = DockStyle.Top, Font = Theme.FontBase,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary,
                UseSystemPasswordChar = password,
            };
            tbl.Controls.Add(tb);
            return tb;
        }
    }
}
