using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcNotificacoes : UserControl
    {
        private DataGridView _dgv;
        private CheckBox _chkSoPorLer;
        private Button _btnRefrescar;
        private Button _btnMarcarLida;
        private Button _btnMarcarTodasLidas;
        private Label _lblTotal;

        public UcNotificacoes()
        {
            InitUI();
            Carregar();
        }

        private void InitUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.PageBg;

            // --- Título ---
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.PageBg
            };
            var lblTitle = new Label
            {
                Text = "Notificações",
                Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(18, 14)
            };
            pnlTitle.Controls.Add(lblTitle);

            // --- Toolbar ---
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.ToolbarBg,
                Padding = new Padding(10, 12, 10, 12)
            };
            var flw = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            _chkSoPorLer = new CheckBox
            {
                Text = "Só por ler",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, 6, 16, 0),
                ForeColor = Theme.TextPrimary
            };
            _chkSoPorLer.CheckedChanged += (s, e) => Carregar();

            _btnRefrescar = Theme.BtnGray("Refrescar");
            _btnRefrescar.Margin = new Padding(0, 2, 8, 0);
            _btnRefrescar.Click += (s, e) => Carregar();

            _btnMarcarLida = Theme.BtnPrim("Marcar lida");
            _btnMarcarLida.Margin = new Padding(0, 2, 8, 0);
            _btnMarcarLida.Click += BtnMarcarLida_Click;

            _btnMarcarTodasLidas = Theme.BtnGray("Marcar todas lidas");
            _btnMarcarTodasLidas.Margin = new Padding(0, 2, 8, 0);
            _btnMarcarTodasLidas.Width = 170;
            _btnMarcarTodasLidas.Click += BtnMarcarTodasLidas_Click;

            _lblTotal = new Label
            {
                AutoSize = true,
                Margin = new Padding(12, 8, 0, 0),
                ForeColor = Theme.TextSecondary,
                Font = Theme.FontSub
            };

            flw.Controls.Add(_chkSoPorLer);
            flw.Controls.Add(_btnRefrescar);
            flw.Controls.Add(_btnMarcarLida);
            flw.Controls.Add(_btnMarcarTodasLidas);
            flw.Controls.Add(_lblTotal);

            pnlToolbar.Controls.Add(flw);

            // --- Grid ---
            _dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(_dgv);

            // Order: Fill first, Tops afterwards
            this.Controls.Add(_dgv);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlTitle);
        }

        private void Carregar()
        {
            string sql;
            // Cliente só vê as próprias; staff/admin vêem tudo.
            string whereCliente = Session.IsCliente
                ? "AND n.cliente_id = @cid "
                : "";

            if (_chkSoPorLer.Checked)
            {
                sql = $@"
SELECT n.notificacao_id AS ID, c.nome AS Cliente, n.tipo AS Tipo,
       n.assunto AS Assunto, n.mensagem AS Mensagem,
       n.data_criacao AS [Data]
FROM notificacao n
JOIN cliente c ON n.cliente_id = c.cliente_id
WHERE n.lida = 0 {whereCliente}
ORDER BY n.data_criacao DESC";
            }
            else
            {
                sql = $@"
SELECT n.notificacao_id AS ID, c.nome AS Cliente, n.tipo AS Tipo,
       n.assunto AS Assunto, n.mensagem AS Mensagem,
       n.data_criacao AS [Data], n.lida AS Lida
FROM notificacao n
JOIN cliente c ON n.cliente_id = c.cliente_id
WHERE 1=1 {whereCliente}
ORDER BY n.data_criacao DESC";
            }

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (Session.IsCliente && Session.ClienteId.HasValue)
                        cmd.Parameters.AddWithValue("@cid", Session.ClienteId.Value);

                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        _dgv.DataSource = dt;
                        _lblTotal.Text = $"{dt.Rows.Count} notificações";

                        if (_dgv.Columns.Contains("ID"))
                            _dgv.Columns["ID"].Visible = false;
                        if (_dgv.Columns.Contains("Mensagem"))
                            _dgv.Columns["Mensagem"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnMarcarLida_Click(object sender, EventArgs e)
        {
            if (_dgv.CurrentRow == null) return;
            object idVal = _dgv.CurrentRow.Cells["ID"]?.Value;
            if (idVal == null || idVal == DBNull.Value) return;

            int id = Convert.ToInt32(idVal);
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("sp_marcar_notificacao_lida", conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@notificacao_id", id);
                    cmd.ExecuteNonQuery();
                }
                Carregar();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnMarcarTodasLidas_Click(object sender, EventArgs e)
        {
            if (_dgv.Rows.Count == 0) return;
            if (MessageBox.Show($"Marcar {_dgv.Rows.Count} notificações como lidas?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes) return;

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("sp_marcar_notificacao_lida", conn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.Add("@notificacao_id", SqlDbType.Int);
                    foreach (DataGridViewRow row in _dgv.Rows)
                    {
                        if (row.Cells["ID"].Value == null || row.Cells["ID"].Value == DBNull.Value) continue;
                        cmd.Parameters["@notificacao_id"].Value = Convert.ToInt32(row.Cells["ID"].Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                Carregar();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
