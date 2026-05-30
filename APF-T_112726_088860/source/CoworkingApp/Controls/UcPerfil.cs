using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Microsoft.Data.SqlClient;

namespace CoworkingApp.Controls
{
    /// <summary>
    /// Página de perfil do utilizador autenticado. Hero card com avatar +
    /// info da conta, 3 KPIs com stats do utilizador, e 2 cards com
    /// detalhes (info pessoal / plano actual ou conta + acções).
    /// </summary>
    public class UcPerfil : UserControl
    {
        // Dados do cliente (carregados se Session.IsCliente)
        private string _nome, _nif, _email, _telefone;
        private DateTime? _dataRegisto;
        private int _kpiReservas;
        private decimal _kpiPago;
        private string _planoActual = "—";
        private DateTime? _planoFim;

        public UcPerfil()
        {
            BackColor = Theme.PageBg;
            Dock      = DockStyle.Fill;
            // Tem de carregar dados antes do BuildUI para os labels saberem o valor.
            try { LoadData(); } catch { /* sem BD usa defaults */ }
            BuildUI();
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 172));  // hero (era 160)
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.RowHeightKpis));  // KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // content

            root.Controls.Add(BuildTitle(),   0, 0);
            root.Controls.Add(BuildHero(),    0, 1);
            root.Controls.Add(BuildKpis(),    0, 2);
            root.Controls.Add(BuildContent(), 0, 3);
            Controls.Add(root);
        }

        private Control BuildTitle()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            pnl.Controls.Add(new Label
            {
                Text = "O meu perfil", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            });
            return pnl;
        }

        // ── Hero ────────────────────────────────────────────────────────
        private Control BuildHero()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 14, ShowShadow = false,
                Margin = Theme.ToolbarMarginBottom,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(28, 24, 28, 24) };

            string role = Session.Role ?? "—";
            Color roleColor = RoleColor(role);

            // ─── Esquerda: avatar grande 84px ────────────────────────
            var avatarHolder = new Panel { Dock = DockStyle.Left, Width = 100, BackColor = Theme.CardBg };
            string initial = (Session.Username ?? "?").Substring(0, 1).ToUpper();
            avatarHolder.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                int diam = 84;
                int cx = 0;
                int cy = (avatarHolder.Height - diam) / 2;
                using (var br = new SolidBrush(roleColor))
                    g.FillEllipse(br, cx, cy, diam, diam);
                using (var f = new Font(Theme.FontBase.FontFamily, 32f, FontStyle.Bold))
                {
                    var ts = TextRenderer.MeasureText(g, initial, f, Size.Empty, TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, initial, f,
                        new Point(cx + (diam - ts.Width) / 2, cy + (diam - ts.Height) / 2),
                        Color.White, TextFormatFlags.NoPadding);
                }
            };

            // ─── Direita: botão alterar password ────────────────────
            var actions = new Panel { Dock = DockStyle.Right, Width = 200, BackColor = Theme.CardBg, Padding = new Padding(0, 38, 0, 0) };
            var btnPwd = new ModernButton
            {
                Text = "Alterar password", Style = ModernButton.Variant.Secondary,
                Font = Theme.FontBold, Dock = DockStyle.Top, Height = 40,
            };
            btnPwd.Click += OpenChangePassword;
            actions.Controls.Add(btnPwd);

            // ─── Centro: nome + role pill + email/cliente ───────────
            var middle = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(12, 16, 0, 0) };

            var lblNome = new Label
            {
                Text = (Session.IsCliente && _nome != null) ? _nome : (Session.Username ?? "—"),
                Font = new Font(Theme.FontBase.FontFamily, 22f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Top, Height = 36, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };

            var rolePill = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Theme.CardBg, Padding = new Padding(0, 4, 0, 0) };
            var pill = new StatusPill
            {
                Text = role, Font = Theme.FontSub, Height = 22,
                Style = StatusPill.PillStyle.Dot, BackColor = Theme.CardBg,
            };
            pill.SetColors(Color.FromArgb(40, roleColor), roleColor);
            pill.Dock  = DockStyle.Left;
            pill.Width = StatusPill.MeasureDotWidth(role, Theme.FontSub);
            rolePill.Controls.Add(pill);

            string subline;
            if (Session.IsCliente)
                subline = (_email != null) ? $"@{Session.Username}  ·  {_email}" : $"@{Session.Username}";
            else if (Session.IsAdmin)
                subline = "Administrador do sistema";
            else
                subline = "Membro do staff";

            var lblSub = new Label
            {
                Text = subline, Font = Theme.FontBase, ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = 32,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 6, 0, 0),
            };

            middle.Controls.Add(lblSub);
            middle.Controls.Add(rolePill);
            middle.Controls.Add(lblNome);

            inner.Controls.Add(middle);
            inner.Controls.Add(actions);
            inner.Controls.Add(avatarHolder);
            card.Controls.Add(inner);
            return card;
        }

        // ── KPIs ────────────────────────────────────────────────────────
        private Control BuildKpis()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Theme.PageBg, Margin = Theme.ToolbarMarginBottom,
            };
            for (int i = 0; i < 3; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Control k1, k2, k3;
            if (Session.IsCliente)
            {
                k1 = BuildKpi("Reservas no total", IconChar.CalendarCheck, Theme.Accent,
                              _kpiReservas.ToString());
                k2 = BuildKpi("Total pago",         IconChar.EuroSign,     Theme.StatusSuccessFg,
                              Theme.FormatEuro(_kpiPago));
                k3 = BuildKpi("Membro desde",        IconChar.UserCheck,    Theme.StatusOrangeFg,
                              _dataRegisto.HasValue ? _dataRegisto.Value.ToString("MMM yyyy",
                                  System.Globalization.CultureInfo.GetCultureInfo("pt-PT")) : "—");
            }
            else
            {
                k1 = BuildKpi("Role",                  IconChar.UserShield, Theme.Accent,           Session.Role ?? "—");
                k2 = BuildKpi("Estado",                IconChar.CircleCheck, Theme.StatusSuccessFg, "Activo");
                k3 = BuildKpi("Sessão",                IconChar.RightToBracket, Theme.StatusOrangeFg, "Em curso");
            }
            k3.Margin = new Padding(0);
            grid.Controls.Add(k1, 0, 0);
            grid.Controls.Add(k2, 1, 0);
            grid.Controls.Add(k3, 2, 0);
            return grid;
        }

        private Control BuildKpi(string label, IconChar icon, Color iconColor, string value)
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
            var lblVal = new Label
            {
                Text = value, Font = new Font(Theme.FontBase.FontFamily, 20f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 6, 0, 0),
            };
            inner.Controls.Add(lblVal);
            inner.Controls.Add(topLine);
            card.Controls.Add(inner);
            return card;
        }

        // ── Content: 2 cols ─────────────────────────────────────────────
        private Control BuildContent()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.PageBg,
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var c1 = BuildInfoCard();
            var c2 = BuildExtraCard();
            c1.Margin = new Padding(0, 0, 6, 0);
            c2.Margin = new Padding(6, 0, 0, 0);
            grid.Controls.Add(c1, 0, 0);
            grid.Controls.Add(c2, 1, 0);
            return grid;
        }

        private Control BuildInfoCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(20, 16, 20, 20) };

            string title = Session.IsCliente ? "Informação pessoal" : "Conta";
            var header = BuildSectionHeader(title, IconChar.IdCard);

            // Field list (Dock=Top reverse order → adicionar do fim para cima).
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg };

            if (Session.IsCliente)
            {
                body.Controls.Add(BuildField("Data de registo",
                    _dataRegisto.HasValue ? _dataRegisto.Value.ToString("dd/MM/yyyy") : "—"));
                body.Controls.Add(BuildField("Telefone", _telefone ?? "—"));
                body.Controls.Add(BuildField("Email",    _email    ?? "—"));
                body.Controls.Add(BuildField("NIF",      _nif      ?? "—"));
                body.Controls.Add(BuildField("Nome",     _nome     ?? "—"));
            }
            else
            {
                body.Controls.Add(BuildField("Cliente associado", "—"));
                body.Controls.Add(BuildField("Role",     Session.Role ?? "—"));
                body.Controls.Add(BuildField("Username", Session.Username ?? "—"));
            }

            inner.Controls.Add(body);
            inner.Controls.Add(header);
            card.Controls.Add(inner);
            return card;
        }

        private Control BuildExtraCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill, BackColor = Theme.CardBg,
                BorderColor = Theme.CardBorder, CornerRadius = 12, ShowShadow = false,
            };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(20, 16, 20, 20) };

            if (Session.IsCliente)
            {
                var header = BuildSectionHeader("Adesão actual", IconChar.Star);
                var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(0, 12, 0, 0) };

                if (_planoActual == "—")
                {
                    body.Controls.Add(new Label
                    {
                        Text = "Sem adesão activa de momento.\nPodes subscrever um plano contactando o staff.",
                        Font = Theme.FontBase, ForeColor = Theme.TextSecondary, BackColor = Theme.CardBg,
                        Dock = DockStyle.Top, AutoSize = false, Height = 60,
                        TextAlign = ContentAlignment.TopLeft,
                    });
                }
                else
                {
                    body.Controls.Add(new Label
                    {
                        Text = _planoActual, Font = new Font(Theme.FontBase.FontFamily, 16f, FontStyle.Bold),
                        ForeColor = Theme.TextPrimary, BackColor = Theme.CardBg,
                        Dock = DockStyle.Top, Height = 30, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                    });
                    body.Controls.Add(new Label
                    {
                        Text = _planoFim.HasValue
                            ? $"Termina em {_planoFim.Value:dd/MM/yyyy}"
                            : "Sem data de fim definida",
                        Font = Theme.FontBase, ForeColor = Theme.TextSecondary, BackColor = Theme.CardBg,
                        Dock = DockStyle.Top, Height = 24, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(0, 6, 0, 0),
                    });
                }

                inner.Controls.Add(body);
                inner.Controls.Add(header);
            }
            else
            {
                // Admin/Staff — permissões e info sessão
                var header = BuildSectionHeader("Permissões", IconChar.ShieldHalved);
                var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.CardBg, Padding = new Padding(0, 12, 0, 0) };

                string[] perms = Session.IsAdmin
                    ? new[] {
                        "Gestão completa de clientes, espaços e reservas",
                        "Criar / editar / desactivar utilizadores",
                        "Aceder a relatórios e estatísticas avançadas",
                        "Configurar planos e políticas",
                    }
                    : new[] {
                        "Gerir reservas e pagamentos",
                        "Consultar dados de clientes",
                        "Aceder a relatórios operacionais",
                    };

                // Adicionar de baixo para cima (Dock=Top reverse z-order).
                for (int i = perms.Length - 1; i >= 0; i--)
                    body.Controls.Add(BuildPermItem(perms[i]));

                inner.Controls.Add(body);
                inner.Controls.Add(header);
            }
            card.Controls.Add(inner);
            return card;
        }

        private Panel BuildSectionHeader(string title, IconChar icon)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Theme.CardBg };
            pnl.Controls.Add(new Label
            {
                Text = title, Font = Theme.FontSection, ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0),
            });
            pnl.Controls.Add(new IconPictureBox
            {
                IconChar = icon, IconSize = 16, IconColor = Theme.Accent,
                BackColor = Theme.CardBg, Dock = DockStyle.Left, Width = 22,
                SizeMode = PictureBoxSizeMode.CenterImage,
            });
            return pnl;
        }

        private static Panel BuildField(string label, string value)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.CardBg, Padding = new Padding(0, 8, 0, 0) };
            var lblLabel = new Label
            {
                Text = label.ToUpper(), Font = Theme.FontMicro, ForeColor = Theme.TextMuted,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = 16,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            var lblValue = new Label
            {
                Text = value, Font = Theme.FontBase, ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg, Dock = DockStyle.Top, Height = 24,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
            };
            pnl.Controls.Add(lblValue);
            pnl.Controls.Add(lblLabel);
            return pnl;
        }

        private static Panel BuildPermItem(string text)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Theme.CardBg };
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(Theme.StatusSuccessFg))
                    g.FillEllipse(br, 4, (pnl.Height - 6) / 2, 6, 6);
            };
            pnl.Controls.Add(new Label
            {
                Text = text, Font = Theme.FontBase, ForeColor = Theme.TextSecondary,
                BackColor = Theme.CardBg, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0),
            });
            return pnl;
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

        // ── Data load ───────────────────────────────────────────────────
        private void LoadData()
        {
            using (var conn = Database.GetConnection())
            {
                if (Session.IsCliente && Session.ClienteId.HasValue)
                {
                    using (var cmd = new SqlCommand(
                        @"SELECT nome, nif, email, telefone, data_registo
                          FROM cliente WHERE cliente_id = @cid", conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", Session.ClienteId.Value);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                _nome     = r["nome"].ToString();
                                _nif      = r["nif"].ToString();
                                _email    = r["email"].ToString();
                                _telefone = r["telefone"] is DBNull ? null : r["telefone"].ToString();
                                _dataRegisto = Convert.ToDateTime(r["data_registo"]);
                            }
                        }
                    }

                    // KPIs: nº reservas e total pago
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM reserva WHERE cliente_id = @cid", conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", Session.ClienteId.Value);
                        _kpiReservas = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    using (var cmd = new SqlCommand(
                        "SELECT COALESCE(SUM(valor), 0) FROM pagamento WHERE cliente_id = @cid AND estado = 'Pago'", conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", Session.ClienteId.Value);
                        _kpiPago = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // Plano actual (adesão Ativa)
                    using (var cmd = new SqlCommand(
                        @"SELECT TOP 1 pl.nome_plano, a.data_fim
                          FROM adesao a
                          JOIN plano pl ON a.plano_id = pl.plano_id
                          WHERE a.cliente_id = @cid AND a.estado = 'Ativa'
                          ORDER BY a.data_inicio DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@cid", Session.ClienteId.Value);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                _planoActual = r["nome_plano"].ToString();
                                _planoFim    = r["data_fim"] is DBNull ? (DateTime?)null : Convert.ToDateTime(r["data_fim"]);
                            }
                        }
                    }
                }
            }
        }

        // ── Change password dialog ─────────────────────────────────────
        private void OpenChangePassword(object sender, EventArgs e)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
            };
            var txtAtual = AddInput(tbl, "Password actual *", password: true);
            var txtNova  = AddInput(tbl, "Nova password * (≥ 8)", password: true);

            using (var dlg = new FormDialog("Alterar password", tbl, 400, () =>
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
