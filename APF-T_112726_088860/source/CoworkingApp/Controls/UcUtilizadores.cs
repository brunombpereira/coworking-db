using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Gestão de utilizadores (acesso só Admin). Lista todos via
    /// vw_utilizadores_listagem; permite criar (qualquer role), resetar
    /// password e activar/desactivar.
    /// </summary>
    public class UcUtilizadores : UserControl
    {
        private DataGridView dgv;
        private Button       btnNovo, btnReset, btnToggle;
        private Label        lblCount;
        private int          _selectedId    = -1;
        private bool         _selectedAtivo = true;

        public UcUtilizadores()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            var pnlTitle = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 56,
                BackColor = Theme.PageBg,
                Padding = new Padding(20, 14, 20, 0)
            };
            pnlTitle.Controls.Add(new Label
            {
                Text = "Utilizadores",
                Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill,
                AutoSize = false
            });

            var pnlToolbar = Theme.Toolbar();
            var flow       = Theme.ToolbarFlow();

            btnNovo   = Theme.BtnPrim("+ Novo");
            btnReset  = Theme.BtnGray("Reset password");
            btnToggle = Theme.BtnGray("Desactivar");
            btnReset.Enabled  = false;
            btnToggle.Enabled = false;

            btnNovo.Click   += (s, e) => OpenNovoDialog();
            btnReset.Click  += (s, e) => OpenResetDialog();
            btnToggle.Click += (s, e) => ToggleAtivo();

            lblCount = new Label
            {
                Font = Theme.FontLabel,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(12, 10, 0, 0)
            };

            flow.Controls.Add(btnNovo);
            flow.Controls.Add(btnReset);
            flow.Controls.Add(btnToggle);
            flow.Controls.Add(lblCount);
            pnlToolbar.Controls.Add(flow);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0)
                {
                    _selectedId = -1;
                    btnReset.Enabled = btnToggle.Enabled = false;
                    return;
                }
                _selectedId    = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);
                _selectedAtivo = Convert.ToBoolean(dgv.SelectedRows[0].Cells["Activo"].Value);
                btnReset.Enabled = btnToggle.Enabled = true;
                btnToggle.Text   = _selectedAtivo ? "Desactivar" : "Activar";
            };

            Controls.Add(dgv);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlTitle);
        }

        private void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand(
                    @"SELECT utilizador_id AS ID,
                             username      AS Username,
                             role          AS Role,
                             cliente_nome  AS Cliente,
                             ativo         AS Activo,
                             CONVERT(varchar, data_criacao, 103) + ' '
                               + CONVERT(varchar, data_criacao, 108) AS [Criado em],
                             CASE WHEN ultimo_login IS NULL THEN '—'
                                  ELSE CONVERT(varchar, ultimo_login, 103) + ' '
                                     + CONVERT(varchar, ultimo_login, 108)
                             END AS [Último login]
                      FROM vw_utilizadores_listagem
                      ORDER BY role, username", conn))
                using (var ad = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    ad.Fill(dt);
                    dgv.DataSource = dt;
                    if (dgv.Columns.Contains("ID")) dgv.Columns["ID"].Visible = false;
                    lblCount.Text = dt.Rows.Count + " utilizadores";
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Novo utilizador ─────────────────────────────────────────────
        private void OpenNovoDialog()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 0, 0, 8),
            };

            var txtUser = AddInput(tbl, "Username *");
            var txtPwd  = AddInput(tbl, "Password * (≥ 8 caracteres)", password: true);

            tbl.Controls.Add(new Label
            {
                Text = "Role *",
                Font = Theme.FontLabel,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 2),
            });
            var cmbRole = new ComboBox
            {
                Dock          = DockStyle.Top,
                Font          = Theme.FontBase,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat,
                BackColor     = Theme.FieldBg,
                ForeColor     = Theme.TextPrimary,
            };
            cmbRole.Items.AddRange(new object[] { "Admin", "Staff", "Cliente" });
            cmbRole.SelectedIndex = 1; // Staff por default
            tbl.Controls.Add(cmbRole);

            // Cliente picker (visível só quando role=Cliente)
            var lblCliente = new Label
            {
                Text = "Cliente *",
                Font = Theme.FontLabel,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 2),
                Visible = false,
            };
            var cmbCliente = new ComboBox
            {
                Dock          = DockStyle.Top,
                Font          = Theme.FontBase,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat,
                BackColor     = Theme.FieldBg,
                ForeColor     = Theme.TextPrimary,
                Visible       = false,
            };
            tbl.Controls.Add(lblCliente);
            tbl.Controls.Add(cmbCliente);

            cmbRole.SelectedIndexChanged += (s, e) =>
            {
                bool needsCliente = (string)cmbRole.SelectedItem == "Cliente";
                lblCliente.Visible = cmbCliente.Visible = needsCliente;
                if (needsCliente && cmbCliente.Items.Count == 0)
                    LoadClientesCombo(cmbCliente);
            };

            using (var dlg = new FormDialog("Novo utilizador", tbl, 420, () =>
            {
                if (string.IsNullOrWhiteSpace(txtUser.Text))
                    throw new ApplicationException("Username obrigatório.");
                if (txtPwd.Text.Length < 8)
                    throw new ApplicationException("Password deve ter ≥ 8 caracteres.");

                string role = cmbRole.SelectedItem.ToString();
                int? clienteId = null;
                if (role == "Cliente")
                {
                    if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull)
                        throw new ApplicationException("Cliente obrigatório quando role é Cliente.");
                    clienteId = Convert.ToInt32(cmbCliente.SelectedValue);
                }

                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("sp_admin_create_user", conn)
                       { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@username", txtUser.Text.Trim());
                    cmd.Parameters.AddWithValue("@password", txtPwd.Text);
                    cmd.Parameters.AddWithValue("@role",     role);
                    cmd.Parameters.AddWithValue("@cliente_id", (object)clienteId ?? DBNull.Value);
                    var pOut = new SqlParameter("@utilizador_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(pOut);
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK) LoadData();
            }
        }

        // ── Reset password ──────────────────────────────────────────────
        private void OpenResetDialog()
        {
            if (_selectedId < 0) return;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };
            var txtPwd = AddInput(tbl, "Nova password * (≥ 8 caracteres)", password: true);

            using (var dlg = new FormDialog("Reset password", tbl, 380, () =>
            {
                if (txtPwd.Text.Length < 8)
                    throw new ApplicationException("Password deve ter ≥ 8 caracteres.");

                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("sp_admin_reset_password", conn)
                       { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@utilizador_id", _selectedId);
                    cmd.Parameters.AddWithValue("@password_nova", txtPwd.Text);
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    MessageBox.Show("Password actualizada com sucesso.",
                                    "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ── Toggle ativo ────────────────────────────────────────────────
        private void ToggleAtivo()
        {
            if (_selectedId < 0) return;
            string verb = _selectedAtivo ? "desactivar" : "activar";
            if (MessageBox.Show($"{char.ToUpper(verb[0])}{verb.Substring(1)} este utilizador?",
                                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes) return;

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("sp_admin_toggle_user_active", conn)
                       { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@utilizador_id", _selectedId);
                    cmd.Parameters.AddWithValue("@ativo",         !_selectedAtivo);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────
        private static TextBox AddInput(TableLayoutPanel tbl, string label, bool password = false)
        {
            tbl.Controls.Add(new Label
            {
                Text = label,
                Font = Theme.FontLabel,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 2),
            });
            var tb = new TextBox
            {
                Dock        = DockStyle.Top,
                Font        = Theme.FontBase,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Theme.FieldBg,
                ForeColor   = Theme.TextPrimary,
                UseSystemPasswordChar = password,
            };
            tbl.Controls.Add(tb);
            return tb;
        }

        private void LoadClientesCombo(ComboBox cmb)
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand(
                    @"SELECT cliente_id, nome
                      FROM   cliente
                      WHERE  cliente_id NOT IN (
                          SELECT cliente_id FROM utilizador WHERE cliente_id IS NOT NULL
                      )
                      ORDER BY nome", conn))
                using (var ad = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    ad.Fill(dt);
                    cmb.DataSource    = dt;
                    cmb.DisplayMember = "nome";
                    cmb.ValueMember   = "cliente_id";
                }
            }
            catch (SqlException) { /* ignore */ }
        }
    }
}
