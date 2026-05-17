using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Clientes redesenhado: stats row (3 KPIs) + toolbar com search +
    /// lista de cards (avatar + info + acções inline) em vez de DataGridView.
    /// </summary>
    public class UcClientes : UserControl
    {
        private Label _kpiTotal, _kpiNovos, _kpiComAdesao;
        private ModernInput _txtSearch;
        private Panel _listContainer;
        private Panel _emptyState;

        public UcClientes()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadData();
        }

        // ── UI ──────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Header (title + Novo Cliente button à direita)
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 84, BackColor = Theme.PageBg,
                Padding = new Padding(24, 18, 24, 0),
            };
            var titleArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            var lblTitle = new Label
            {
                Text = "Clientes", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top, Height = 34, AutoSize = false,
            };
            var lblSub = new Label
            {
                Text = "Gestão de contas de cliente do coworking",
                Font = Theme.FontLabel, ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
                Padding = new Padding(0, 4, 0, 0),
            };
            titleArea.Controls.Add(lblSub);
            titleArea.Controls.Add(lblTitle);

            var btnNovo = new ModernButton
            {
                Text  = "+ Novo Cliente",
                Style = ModernButton.Variant.Primary,
                Dock  = DockStyle.Right, Width = 170, Height = 42,
                Margin = new Padding(0),
            };
            btnNovo.Click += (s, e) => OpenEditor(null);

            var btnHolder = new Panel { Dock = DockStyle.Right, Width = 170, BackColor = Theme.PageBg, Padding = new Padding(0, 6, 0, 0) };
            btnHolder.Controls.Add(btnNovo);

            pnlTitle.Controls.Add(titleArea);
            pnlTitle.Controls.Add(btnHolder);

            // Content (stats + lista — search vai DENTRO do card da lista)
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
                BackColor = Theme.PageBg, Padding = new Padding(24, 12, 24, 24),
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));   // stats
            content.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));   // lista

            content.Controls.Add(BuildStatsRow(),  0, 0);
            content.Controls.Add(BuildListCard(),  0, 1);

            Controls.Add(content);
            Controls.Add(pnlTitle);
        }

        // ── Stats row ───────────────────────────────────────────────────
        private Control BuildStatsRow()
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.PageBg };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            var c1 = BuildKpi("Total de clientes",      IconChar.Users,        out _kpiTotal);
            var c2 = BuildKpi("Novos este mês",          IconChar.UserPlus,     out _kpiNovos);
            var c3 = BuildKpi("Com adesão activa",       IconChar.Star,         out _kpiComAdesao);
            c1.Margin = new Padding(0, 0, 4, 8);
            c2.Margin = new Padding(4, 0, 4, 8);
            c3.Margin = new Padding(4, 0, 0, 8);
            row.Controls.Add(c1, 0, 0);
            row.Controls.Add(c2, 1, 0);
            row.Controls.Add(c3, 2, 0);
            return row;
        }

        private Control BuildKpi(string title, IconChar icon, out Label valueLbl)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg, BorderColor = Theme.CardBorder,
                CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(18, 14, 18, 12) };

            var iconLbl = new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Right,
                SizeMode = PictureBoxSizeMode.CenterImage, Size = new Size(28, 24),
            };
            var lbl = new Label
            {
                Text = title.ToUpperInvariant(), Font = Theme.FontMicro,
                ForeColor = Theme.TextSecondary, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = 22, AutoSize = false,
            };
            var topLine = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Theme.CardBg };
            topLine.Controls.Add(iconLbl);
            topLine.Controls.Add(lbl);

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

        // ── List card (com search inline no topo) ───────────────────────
        private Control BuildListCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg, BorderColor = Theme.CardBorder,
                CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(8, 8, 8, 8) };

            // Search bar inline no topo do card
            var searchBar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.CardBg, Padding = new Padding(8, 8, 8, 12) };
            _txtSearch = new ModernInput { Dock = DockStyle.Fill, Height = 38 };
            _txtSearch.PlaceholderText = "Procurar nome, NIF, email…";
            _txtSearch.TextChanged += (s, e) => LoadData();
            searchBar.Controls.Add(_txtSearch);

            _listContainer = new Panel
            {
                Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.CardBg,
                Visible = false,
            };
            _listContainer.Resize += (s, e) => ResizeCards();

            _emptyState = BuildEmptyState("Nenhum cliente encontrado", IconChar.UserSlash);
            _emptyState.Dock = DockStyle.Fill;
            _emptyState.BackColor = Theme.CardBg;

            inner.Controls.Add(_listContainer);
            inner.Controls.Add(_emptyState);
            inner.Controls.Add(searchBar);
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
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                if (iconImg == null)
                {
                    using (var pb = new IconPictureBox { IconChar = icon, IconSize = 44, IconColor = Theme.TextMuted })
                        if (pb.Image != null) iconImg = (Image)pb.Image.Clone();
                }
                var textSize = TextRenderer.MeasureText(g, text, Theme.FontBase, Size.Empty, TextFormatFlags.NoPadding);
                int iconSize = 44, gap = 14;
                int totalH = iconSize + gap + textSize.Height;
                int startY = Math.Max(8, (pnl.Height - totalH) / 2);
                int iconX  = (pnl.Width - iconSize) / 2;
                int textX  = (pnl.Width - textSize.Width) / 2;
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
                {
                    // Stats
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM cliente", conn))
                        _kpiTotal.Text = cmd.ExecuteScalar().ToString();
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM cliente
                          WHERE data_registo >= DATEADD(DAY, 1-DAY(GETDATE()), CAST(GETDATE() AS date))", conn))
                        _kpiNovos.Text = cmd.ExecuteScalar().ToString();
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(DISTINCT cliente_id) FROM adesao WHERE estado='Ativa'", conn))
                        _kpiComAdesao.Text = cmd.ExecuteScalar().ToString();

                    LoadClients(conn);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadClients(SqlConnection conn)
        {
            _listContainer.Controls.Clear();
            int y = 0, count = 0;
            using (var cmd = new SqlCommand(
                @"SELECT c.cliente_id, c.nome, c.nif, c.email, c.telefone, c.data_registo,
                        (SELECT COUNT(*) FROM reserva WHERE cliente_id = c.cliente_id)        AS num_reservas,
                        (SELECT MAX(data_reserva) FROM reserva WHERE cliente_id = c.cliente_id) AS ultima,
                        CASE WHEN EXISTS (SELECT 1 FROM adesao WHERE cliente_id=c.cliente_id AND estado='Ativa')
                             THEN 1 ELSE 0 END AS tem_adesao
                  FROM cliente c
                  WHERE @q='' OR c.nome LIKE '%'+@q+'%' OR c.nif LIKE '%'+@q+'%' OR c.email LIKE '%'+@q+'%'
                  ORDER BY c.nome", conn))
            {
                cmd.Parameters.AddWithValue("@q", _txtSearch?.Text?.Trim() ?? "");
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var card = BuildClientCard(
                            id:          Convert.ToInt32(r["cliente_id"]),
                            nome:        r["nome"].ToString(),
                            nif:         r["nif"].ToString(),
                            email:       r["email"].ToString(),
                            telefone:    r["telefone"] is DBNull ? null : r["telefone"].ToString(),
                            numReservas: Convert.ToInt32(r["num_reservas"]),
                            ultimaReserva: r["ultima"] is DBNull ? null : (DateTime?)r["ultima"],
                            temAdesao:   Convert.ToInt32(r["tem_adesao"]) == 1
                        );
                        card.Location = new Point(0, y);
                        card.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        card.Width    = _listContainer.ClientSize.Width;
                        _listContainer.Controls.Add(card);
                        y += card.Height + 6;
                        count++;
                    }
                }
            }

            _listContainer.Visible = count > 0;
            _emptyState.Visible    = count == 0;
            _emptyState.Invalidate();
            ResizeCards();
        }

        private void ResizeCards()
        {
            if (_listContainer == null) return;
            int w = _listContainer.ClientSize.Width;
            if (w < 100) return;
            foreach (Control c in _listContainer.Controls) c.Width = w;
        }

        // ── Client card ─────────────────────────────────────────────────
        private Control BuildClientCard(int id, string nome, string nif, string email, string telefone,
                                        int numReservas, DateTime? ultimaReserva, bool temAdesao)
        {
            Color idleBg  = Theme.CardBg;
            Color hoverBg = MixColors(Theme.CardBg, Color.White, 0.05f);

            var row = new Panel
            {
                Height = 76, Margin = new Padding(0, 0, 0, 6),
                BackColor = idleBg, Cursor = Cursors.Hand,
            };

            // Avatar
            var avatarHolder = new Panel { Dock = DockStyle.Left, Width = 64, BackColor = idleBg };
            var avatar = new AvatarCircle
            {
                Initial = nome, CircleColor = Theme.Accent,
                Size = new Size(44, 44), Location = new Point(10, 16),
            };
            avatarHolder.Controls.Add(avatar);

            // Acções à direita (edit + delete + adesão badge se aplicável)
            var actions = new Panel { Dock = DockStyle.Right, Width = 110, BackColor = idleBg };
            var btnEdit = new IconButton
            {
                IconChar = IconChar.Pen, IconSize = 14, IconColor = Theme.TextSecondary,
                FlatStyle = FlatStyle.Flat, BackColor = idleBg, ForeColor = Theme.TextSecondary,
                Size = new Size(36, 36), Location = new Point(18, 20), Cursor = Cursors.Hand,
                TabStop = false,
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.FlatAppearance.MouseOverBackColor = Theme.SidebarBgActive;
            btnEdit.Click += (s, e) => OpenEditor(id);

            var btnDelete = new IconButton
            {
                IconChar = IconChar.TrashCan, IconSize = 14, IconColor = Theme.StatusDangerFg,
                FlatStyle = FlatStyle.Flat, BackColor = idleBg, ForeColor = Theme.StatusDangerFg,
                Size = new Size(36, 36), Location = new Point(58, 20), Cursor = Cursors.Hand,
                TabStop = false,
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, Theme.StatusDangerFg);
            btnDelete.Click += (s, e) => DeleteCliente(id, nome);

            actions.Controls.Add(btnEdit);
            actions.Controls.Add(btnDelete);

            // Stats (reservas + última) — vertical centering via Padding do container,
            // não nos labels individuais (que cortavam o texto).
            // Total interno: 24 (line1) + 20 (line2) = 44. Row 76 → top padding 16.
            var statsPanel = new Panel
            {
                Dock = DockStyle.Right, Width = 200, BackColor = idleBg,
                Padding = new Padding(0, 16, 8, 0),
            };
            string statsLine1 = numReservas + " reserva" + (numReservas == 1 ? "" : "s");
            string statsLine2 = ultimaReserva.HasValue
                ? "Última: " + ultimaReserva.Value.ToString("dd/MM/yyyy")
                : "Sem reservas";
            var lblStats1 = new Label
            {
                Text = statsLine1, Font = Theme.FontBold, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top, Height = 24, AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight, BackColor = idleBg,
            };
            var lblStats2 = new Label
            {
                Text = statsLine2, Font = Theme.FontSub, ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 20, AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight, BackColor = idleBg,
            };
            statsPanel.Controls.Add(lblStats2);
            statsPanel.Controls.Add(lblStats1);

            // Badge adesão (opcional, à direita do statsPanel)
            Control adesaoBadge = null;
            if (temAdesao)
            {
                adesaoBadge = new StatusPill
                {
                    Text  = "Com adesão",
                    Dock  = DockStyle.Right,
                    Width = 110, BackColor = idleBg,
                };
                ((StatusPill)adesaoBadge).SetColors(Theme.StatusSuccessBg, Theme.StatusSuccessFg);
            }

            // Texto principal (nome + email + nif/telefone) — Fill
            // Vertical centering via Padding do container. Total interno:
            // 24 (nome) + 20 (email) + 20 (meta) = 64. Row 76 → top padding 6.
            var pnlText = new Panel { Dock = DockStyle.Fill, BackColor = idleBg, Padding = new Padding(0, 6, 0, 0) };
            var lblNome = new Label
            {
                Text = nome, Font = new Font(Theme.FontBase.FontFamily, 11.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 24, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblEmail = new Label
            {
                Text = email, Font = Theme.FontSub, ForeColor = Theme.TextSecondary, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblMeta = new Label
            {
                Text = $"NIF {nif}" + (string.IsNullOrEmpty(telefone) ? "" : "  ·  " + telefone),
                Font = Theme.FontSub, ForeColor = Theme.TextMuted, BackColor = idleBg,
                Dock = DockStyle.Top, Height = 20, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            pnlText.Controls.Add(lblMeta);
            pnlText.Controls.Add(lblEmail);
            pnlText.Controls.Add(lblNome);

            // Ordem de adição importa para Dock stack
            row.Controls.Add(pnlText);     // Fill
            row.Controls.Add(actions);     // Right
            if (adesaoBadge != null) row.Controls.Add(adesaoBadge);  // Right (mais à esquerda)
            row.Controls.Add(statsPanel);  // Right
            row.Controls.Add(avatarHolder); // Left

            // Hover unificado
            void SetHover(bool on)
            {
                Color bg = on ? hoverBg : idleBg;
                row.BackColor = bg;
                foreach (Control c in row.Controls) PaintChildren(c, bg);
            }
            void Hook(Control c)
            {
                c.MouseEnter += (s, e) => SetHover(true);
                c.MouseLeave += (s, e) =>
                {
                    var p = row.PointToClient(System.Windows.Forms.Cursor.Position);
                    if (!row.ClientRectangle.Contains(p)) SetHover(false);
                };
                foreach (Control child in c.Controls) Hook(child);
            }
            Hook(row);

            // Click no row (fora dos action buttons) → abre editor
            void HookRowClick(Control c)
            {
                if (c == btnEdit || c == btnDelete) return;
                c.Click += (s, e) => OpenEditor(id);
                foreach (Control child in c.Controls) HookRowClick(child);
            }
            HookRowClick(row);

            return row;
        }

        private static void PaintChildren(Control c, Color bg)
        {
            c.BackColor = bg;
            foreach (Control child in c.Controls) PaintChildren(child, bg);
        }

        private static Color MixColors(Color a, Color b, float t)
            => Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));

        // ── Edit / Delete ───────────────────────────────────────────────
        private void OpenEditor(int? id)
        {
            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var txtNome = AddField(tbl, "Nome *");
            var txtNif  = AddField(tbl, "NIF *");
            var txtEmail = AddField(tbl, "Email *");
            var txtTelefone = AddField(tbl, "Telefone");

            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT nome, nif, email, telefone FROM cliente WHERE cliente_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtNome.Text = r["nome"]?.ToString() ?? "";
                            txtNif.Text = r["nif"]?.ToString() ?? "";
                            txtEmail.Text = r["email"]?.ToString() ?? "";
                            txtTelefone.Text = r["telefone"] is DBNull ? "" : r["telefone"].ToString();
                        }
                    }
                }
            }

            using (var dlg = new CoworkingApp.FormDialog(id.HasValue ? "Editar Cliente" : "Novo Cliente", tbl, 380, () =>
            {
                if (string.IsNullOrWhiteSpace(txtNome.Text)) throw new ApplicationException("Nome é obrigatório.");
                if (!Regex.IsMatch(txtNif.Text.Trim(), @"^\d{9}$")) throw new ApplicationException("NIF inválido (9 dígitos).");
                try { _ = new MailAddress(txtEmail.Text.Trim()); } catch { throw new ApplicationException("Email inválido."); }

                var sql = id.HasValue
                    ? "UPDATE cliente SET nome=@n, nif=@nif, email=@e, telefone=@t WHERE cliente_id=@id"
                    : "INSERT INTO cliente (nome, nif, email, telefone) VALUES (@n,@nif,@e,@t)";
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@n", txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@nif", txtNif.Text.Trim());
                    cmd.Parameters.AddWithValue("@e", txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", string.IsNullOrWhiteSpace(txtTelefone.Text) ? (object)DBNull.Value : txtTelefone.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void DeleteCliente(int id, string nome)
        {
            if (MessageBox.Show($"Eliminar o cliente \"{nome}\"?", "Confirmar",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var chk = new SqlCommand("SELECT COUNT(*) FROM pagamento WHERE cliente_id=@id", conn))
                    {
                        chk.Parameters.AddWithValue("@id", id);
                        if ((int)chk.ExecuteScalar() > 0)
                        {
                            MessageBox.Show("Não é possível eliminar — cliente tem pagamentos.", "Aviso",
                                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    using (var cmd = new SqlCommand("DELETE FROM cliente WHERE cliente_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Form helpers (template partilhado pelos outros UCs) ──────────
        // Cada helper cria um Panel Dock=Top com label+control e devolve o controlo.
        // Para acesso ao Panel wrapper (ex: esconder a row inteira), usar control.Parent:
        //   var cmb = AddCombo(tbl, "Opcional", new[]{"A","B"});
        //   cmb.Parent.Visible = false;
        internal static ModernInput AddField(TableLayoutPanel tbl, string label)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 12) };
            var input = new ModernInput { Dock = DockStyle.Top, Height = 38 };
            pnl.Controls.Add(input);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return input;
        }

        internal static ComboBox AddCombo(TableLayoutPanel tbl, string label, string[] items)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var cmb = Theme.Combo();
            if (items != null && items.Length > 0) cmb.Items.AddRange(items);
            pnl.Controls.Add(cmb);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return cmb;
        }

        internal static ComboBox AddComboDataSource(TableLayoutPanel tbl, string label, object dataSource, string display, string value)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var cmb = Theme.Combo();
            // Order matters: assign DisplayMember/ValueMember before DataSource to avoid late binding races.
            cmb.DisplayMember = display;
            cmb.ValueMember   = value;
            cmb.DataSource    = dataSource;
            pnl.Controls.Add(cmb);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return cmb;
        }

        internal static DateTimePicker AddDate(TableLayoutPanel tbl, string label)
        {
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 0, 10) };
            var dt = Theme.DatePicker();
            pnl.Controls.Add(dt);
            pnl.Controls.Add(Theme.FieldLabel(label));
            tbl.Controls.Add(pnl);
            return dt;
        }
    }
}
