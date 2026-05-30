using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Gestão de utilizadores (acesso só Admin). Lista todos via
    /// vw_utilizadores_listagem; permite criar, resetar password e
    /// activar/desactivar.
    /// </summary>
    public class UcUtilizadores : UserControl
    {
        // KPIs
        private Label _kpiTotal, _kpiAdmins, _kpiStaff, _kpiClientes;

        // Toolbar
        private SegmentedControl _segRole;
        private ModernButton _btnNovo;

        // Lista
        private ScrollableList _list;
        private Panel _empty;

        // Cache
        private DataTable _allRows;

        public UcUtilizadores()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            HandleCreated += (s, e) => { try { LoadData(); } catch { /* sem BD */ } };
        }

        // ── BUILD UI ────────────────────────────────────────────────────
        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                BackColor = Theme.PageBg,
                Padding = Theme.PagePadding,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.RowHeightTitle));   // title
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.RowHeightToolbar));   // toolbar
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.RowHeightKpis));  // KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // list

            root.Controls.Add(BuildTitle(),   0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);
            root.Controls.Add(BuildKpis(),    0, 2);
            root.Controls.Add(BuildList(),    0, 3);
            Controls.Add(root);
        }

        private Control BuildTitle()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            pnl.Controls.Add(new Label
            {
                Text = "Utilizadores", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        // ── Toolbar ─────────────────────────────────────────────────────
        private Control BuildToolbar()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
                Margin = Theme.ToolbarMarginBottom,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(16, 12, 16, 12) };

            _btnNovo = new ModernButton
            {
                Text = "+ Novo utilizador", Style = ModernButton.Variant.Primary,
                Font = Theme.FontBold, Size = new Size(180, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _btnNovo.Click += (s, e) => OpenNovoDialog();

            _segRole = new SegmentedControl
            {
                Segments = new[] { "Todos", "Admin", "Staff", "Cliente" },
                SelectedIndex = 0, Width = 320, Height = 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            _segRole.SelectedIndexChanged += (s, e) => RenderRows();

            inner.Controls.Add(_segRole);
            inner.Controls.Add(_btnNovo);
            card.Controls.Add(inner);

            void Relayout()
            {
                var dr = inner.DisplayRectangle;
                int btnY = dr.Y + (dr.Height - _btnNovo.Height) / 2;
                _btnNovo.Location = new Point(dr.Right - _btnNovo.Width, btnY);
                int segY = dr.Y + (dr.Height - _segRole.Height) / 2;
                _segRole.Location = new Point(dr.X, segY);
            }
            inner.SizeChanged   += (s, e) => Relayout();
            inner.HandleCreated += (s, e) => Relayout();
            return card;
        }

        // ── KPIs ────────────────────────────────────────────────────────
        private Control BuildKpis()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
                BackColor = Theme.PageBg, Margin = Theme.ToolbarMarginBottom,
            };
            for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var k1 = BuildKpi("Total",     IconChar.Users,      Theme.Accent,           out _kpiTotal);
            var k2 = BuildKpi("Admins",    IconChar.UserShield, Theme.StatusDangerFg,   out _kpiAdmins);
            var k3 = BuildKpi("Staff",     IconChar.UserGear,   Theme.StatusOrangeFg,   out _kpiStaff);
            var k4 = BuildKpi("Clientes",  IconChar.User,       Theme.StatusSuccessFg,  out _kpiClientes);
            k4.Margin = Theme.KpiCardLast;
            grid.Controls.Add(k1, 0, 0);
            grid.Controls.Add(k2, 1, 0);
            grid.Controls.Add(k3, 2, 0);
            grid.Controls.Add(k4, 3, 0);
            return grid;
        }

        private Control BuildKpi(string label, IconChar icon, Color iconColor, out Label valueLbl)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
                Margin = Theme.KpiCardGap,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(18, 14, 18, 14) };

            var topLine = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Theme.CardBg };
            topLine.Controls.Add(new Label
            {
                Text = label, Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0),
            });
            topLine.Controls.Add(new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = iconColor,
                BackColor = Theme.CardBg, Dock = DockStyle.Left, Width = 22,
                SizeMode = PictureBoxSizeMode.CenterImage,
            });

            valueLbl = new Label
            {
                Text = "—", Font = new Font(Theme.FontBase.FontFamily, 24f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 4, 0, 0),
            };
            inner.Controls.Add(valueLbl);
            inner.Controls.Add(topLine);
            card.Controls.Add(inner);
            return card;
        }

        // ── Lista ───────────────────────────────────────────────────────
        private Control BuildList()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = Theme.ListInnerPadding };

            _list = new ScrollableList { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Visible = false };
            _list.Content.BackColor = Theme.CardBg;
            _list.Resize += (s, e) => RenderRows();

            _empty = BuildEmptyState("Sem utilizadores no filtro", IconChar.Users);
            _empty.Dock = DockStyle.Fill;

            inner.Controls.Add(_list);
            inner.Controls.Add(_empty);
            card.Controls.Add(inner);
            return card;
        }

        private Panel BuildEmptyState(string text, IconChar icon)
        {
            var pnl = new Panel { BackColor = Theme.CardBg };
            Image iconImg = null;
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (iconImg == null)
                {
                    using (var pb = new IconPictureBox { IconChar = icon, IconSize = 44, IconColor = Theme.TextMuted })
                        if (pb.Image != null) iconImg = (Image)pb.Image.Clone();
                }
                var ts = TextRenderer.MeasureText(g, text, Theme.FontBase, Size.Empty, TextFormatFlags.NoPadding);
                int iconSize = 44, gap = 14, totalH = iconSize + gap + ts.Height;
                int startY = Math.Max(8, (pnl.Height - totalH) / 2);
                int iconX = (pnl.Width - iconSize) / 2;
                int textX = (pnl.Width - ts.Width) / 2;
                if (iconImg != null) g.DrawImage(iconImg, iconX, startY, iconSize, iconSize);
                TextRenderer.DrawText(g, text, Theme.FontBase, new Point(textX, startY + iconSize + gap),
                    Theme.TextMuted, TextFormatFlags.NoPadding);
            };
            pnl.Resize += (s, e) => pnl.Invalidate();
            return pnl;
        }

        // ── Data ────────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand(
                    @"SELECT utilizador_id, username, role, cliente_nome, ativo,
                             data_criacao, ultimo_login
                      FROM vw_utilizadores_listagem
                      ORDER BY CASE role WHEN 'Admin' THEN 1 WHEN 'Staff' THEN 2 ELSE 3 END,
                               username", conn))
                using (var ad = new SqlDataAdapter(cmd))
                {
                    _allRows = new DataTable();
                    ad.Fill(_allRows);
                }
                RenderRows();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRows()
        {
            if (_allRows == null || _list == null) return;

            _list.Content.SuspendLayout();
            _list.Content.Controls.Clear();

            // KPIs sobre tudo
            int total = _allRows.Rows.Count;
            int nAdmin = 0, nStaff = 0, nCliente = 0;
            foreach (DataRow r in _allRows.Rows)
            {
                string role = r["role"].ToString();
                if      (role == "Admin")   nAdmin++;
                else if (role == "Staff")   nStaff++;
                else if (role == "Cliente") nCliente++;
            }
            _kpiTotal   .Text = total.ToString();
            _kpiAdmins  .Text = nAdmin.ToString();
            _kpiStaff   .Text = nStaff.ToString();
            _kpiClientes.Text = nCliente.ToString();

            // Filtro segmentado
            string filterRole = null;
            switch (_segRole.SelectedIndex)
            {
                case 1: filterRole = "Admin";   break;
                case 2: filterRole = "Staff";   break;
                case 3: filterRole = "Cliente"; break;
            }

            var view = new List<DataRow>();
            foreach (DataRow r in _allRows.Rows)
            {
                if (filterRole != null && r["role"].ToString() != filterRole) continue;
                view.Add(r);
            }

            int y = 0;
            int width = Math.Max(600, _list.ClientSize.Width - 20);
            foreach (var r in view)
            {
                int id        = Convert.ToInt32(r["utilizador_id"]);
                string user   = r["username"].ToString();
                string role   = r["role"].ToString();
                string cli    = r["cliente_nome"] is DBNull ? null : r["cliente_nome"].ToString();
                bool ativo    = Convert.ToBoolean(r["ativo"]);
                DateTime dCri = Convert.ToDateTime(r["data_criacao"]);
                DateTime? dLog = r["ultimo_login"] is DBNull ? (DateTime?)null : Convert.ToDateTime(r["ultimo_login"]);

                var card = BuildUserCard(id, user, role, cli, ativo, dCri, dLog);
                card.Location = new Point(0, y);
                card.Width    = width;
                card.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _list.Content.Controls.Add(card);
                y += card.Height + 8;
            }
            _list.Content.ResumeLayout();
            _list.UpdateLayout(y);

            _list .Visible = view.Count > 0;
            _empty.Visible = view.Count == 0;
            _empty.Invalidate();
        }

        // ── Card ────────────────────────────────────────────────────────
        private Control BuildUserCard(int id, string username, string role, string cliente,
                                       bool ativo, DateTime dataCriacao, DateTime? ultimoLogin)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = UcEspacos.MixColors(Theme.CardBg, Color.White, 0.05f);
            Color roleColor = RoleColor(role);

            var wrap = new Panel { Height = 96, BackColor = idleBg, Padding = new Padding(0, 0, 0, 8) };
            var row  = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Cursor = Cursors.Default };

            // ─── Esquerda: avatar circle com ícone role ──────────────
            var leftBlock = new Panel { Dock = DockStyle.Left, Width = 76, BackColor = idleBg };
            Image img = null;
            using (var pb = new IconPictureBox { IconChar = RoleIcon(role), IconSize = 22, IconColor = Color.White })
                if (pb.Image != null) img = (Image)pb.Image.Clone();
            leftBlock.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                int diam = 44;
                int cx = (leftBlock.Width  - diam) / 2;
                int cy = (leftBlock.Height - diam) / 2;
                using (var br = new SolidBrush(roleColor)) g.FillEllipse(br, cx, cy, diam, diam);
                if (img != null) g.DrawImage(img, cx + (diam - 22) / 2, cy + (diam - 22) / 2 + 1, 22, 22);
            };

            // ─── Acções (direita) ────────────────────────────────────
            var actions = new Panel { Dock = DockStyle.Right, Width = 90, BackColor = idleBg };
            var btnReset = MakeIconBtn(IconChar.Key,
                Theme.Accent, idleBg, () => OpenResetDialog(id, username));
            var btnToggle = MakeIconBtn(ativo ? IconChar.UserSlash : IconChar.UserPlus,
                ativo ? Theme.StatusDangerFg : Theme.StatusSuccessFg, idleBg,
                () => ToggleAtivo(id, ativo, username));
            btnReset .Location = new Point(8, 30);
            btnToggle.Location = new Point(46, 30);
            actions.Controls.Add(btnReset);
            actions.Controls.Add(btnToggle);

            // ─── Right info: estado + última actividade ──────────────
            var rightInfo = new Panel { Dock = DockStyle.Right, Width = 200, BackColor = idleBg, Padding = new Padding(0, 18, 16, 0) };
            var pillHolder = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = idleBg };
            var pill = new StatusPill
            {
                Text = ativo ? "Activo" : "Inactivo",
                Height = 22, Style = StatusPill.PillStyle.Dot,
                Font = Theme.FontSub, BackColor = idleBg,
            };
            if (ativo) pill.SetColors(Theme.StatusSuccessBg, Theme.StatusSuccessFg);
            else       pill.SetColors(Theme.StatusNeutralBg, Theme.StatusNeutralFg);
            pill.Dock  = DockStyle.Right;
            pill.Width = StatusPill.MeasureDotWidth(pill.Text, Theme.FontSub);
            pillHolder.Controls.Add(pill);

            var lblLogin = new Label
            {
                Text = ultimoLogin.HasValue
                    ? $"Último login: {ultimoLogin.Value:dd/MM/yyyy HH:mm}"
                    : "Nunca entrou",
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 4, 0, 0),
            };
            rightInfo.Controls.Add(lblLogin);
            rightInfo.Controls.Add(pillHolder);

            // ─── Centro: username + role + cliente/data ──────────────
            var middle = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(12, 18, 12, 0) };
            var lblUser = new Label
            {
                Text = username, Font = new Font(Theme.FontBase.FontFamily, 12f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            string subLine = (cliente != null) ? $"{role} · {cliente}" : role;
            var lblRole = new Label
            {
                Text = subLine, Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblData = new Label
            {
                Text = $"Criado em {dataCriacao:dd/MM/yyyy}",
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 18, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            middle.Controls.Add(lblData);
            middle.Controls.Add(lblRole);
            middle.Controls.Add(lblUser);

            row.Controls.Add(middle);
            row.Controls.Add(rightInfo);
            row.Controls.Add(actions);
            row.Controls.Add(leftBlock);
            wrap.Controls.Add(row);

            // Hover
            void Recurse(Control c, Color bg)
            {
                if (c is IconButton) return;
                c.BackColor = bg;
                foreach (Control x in c.Controls) Recurse(x, bg);
            }
            void Hook(Control c)
            {
                c.MouseEnter += (s, e) => Recurse(row, hoverBg);
                c.MouseLeave += (s, e) =>
                {
                    var p = row.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!row.ClientRectangle.Contains(p)) Recurse(row, idleBg);
                };
                foreach (Control x in c.Controls) Hook(x);
            }
            Hook(row);

            return wrap;
        }

        private static IconButton MakeIconBtn(IconChar icon, Color hoverColor, Color bg, Action onClick)
        {
            var btn = new IconButton
            {
                IconChar = icon, IconSize = 16, IconColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Theme.TextSecondary,
                Size = new Size(36, 36), Cursor = Cursors.Hand, TabStop = false,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = bg;
            btn.MouseEnter += (s, e) => btn.IconColor = hoverColor;
            btn.MouseLeave += (s, e) => btn.IconColor = Theme.TextSecondary;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private static Color RoleColor(string role)
        {
            switch (role)
            {
                case "Admin":   return Theme.StatusDangerFg;
                case "Staff":   return Theme.StatusOrangeFg;
                case "Cliente": return Theme.Accent;
                default:         return Theme.TextSecondary;
            }
        }
        private static IconChar RoleIcon(string role)
        {
            switch (role)
            {
                case "Admin":   return IconChar.UserShield;
                case "Staff":   return IconChar.UserGear;
                case "Cliente": return IconChar.User;
                default:         return IconChar.User;
            }
        }

        // ── Novo utilizador ─────────────────────────────────────────────
        private void OpenNovoDialog()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 0, 0, 8),
            };

            var txtUser = AddInput(tbl, "Username *");
            var txtPwd  = AddInput(tbl, "Password * (≥ 8 caracteres)", password: true);

            tbl.Controls.Add(new Label
            {
                Text = "Role *", Font = Theme.FontLabel, ForeColor = Theme.TextSecondary,
                AutoSize = true, Margin = new Padding(0, 6, 0, 2),
            });
            var cmbRole = new ComboBox
            {
                Dock = DockStyle.Top, Font = Theme.FontBase,
                DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
                BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary,
            };
            cmbRole.Items.AddRange(new object[] { "Admin", "Staff", "Cliente" });
            cmbRole.SelectedIndex = 1;
            tbl.Controls.Add(cmbRole);

            var lblCliente = new Label
            {
                Text = "Cliente *", Font = Theme.FontLabel, ForeColor = Theme.TextSecondary,
                AutoSize = true, Margin = new Padding(0, 6, 0, 2), Visible = false,
            };
            var cmbCliente = new ComboBox
            {
                Dock = DockStyle.Top, Font = Theme.FontBase,
                DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
                BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary, Visible = false,
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

        private void OpenResetDialog(int id, string username)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };
            var txtPwd = AddInput(tbl, "Nova password * (≥ 8 caracteres)", password: true);

            using (var dlg = new FormDialog($"Reset password — {username}", tbl, 400, () =>
            {
                if (txtPwd.Text.Length < 8)
                    throw new ApplicationException("Password deve ter ≥ 8 caracteres.");

                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("sp_admin_reset_password", conn)
                       { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@utilizador_id", id);
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

        private void ToggleAtivo(int id, bool currentAtivo, string username)
        {
            string verb = currentAtivo ? "desactivar" : "activar";
            if (MessageBox.Show($"{char.ToUpper(verb[0])}{verb.Substring(1)} o utilizador '{username}'?",
                                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes) return;

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd  = new SqlCommand("sp_admin_toggle_user_active", conn)
                       { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@utilizador_id", id);
                    cmd.Parameters.AddWithValue("@ativo",         !currentAtivo);
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
