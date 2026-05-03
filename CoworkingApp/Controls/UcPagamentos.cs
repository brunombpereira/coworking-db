using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcPagamentos : UserControl
    {
        private ComboBox cmbCliente;
        private ComboBox cmbItem;
        private ComboBox cmbMetodo;
        private Label lblValor;
        private Button btnRegistar;
        private Button btnAtualizar;
        private DataGridView dgvPagamentos;
        private DataTable _itensTable;

        public UcPagamentos()
        {
            _itensTable = new DataTable();
            _itensTable.Columns.Add("id", typeof(int));
            _itensTable.Columns.Add("tipo", typeof(string));
            _itensTable.Columns.Add("descricao", typeof(string));
            _itensTable.Columns.Add("valor", typeof(decimal));
            _itensTable.Columns.Add("reserva_id", typeof(object));
            _itensTable.Columns.Add("adesao_id", typeof(object));

            InitUI();
            LoadClientes();
            LoadPagamentos();
        }

        private void InitUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.ContentBg;

            // --- dgvPagamentos (Dock=Fill) ---
            dgvPagamentos = new DataGridView();
            dgvPagamentos.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvPagamentos);
            dgvPagamentos.CellFormatting += (s, e) =>
            {
                if (dgvPagamentos.Columns.Count > 0 &&
                    e.ColumnIndex >= 0 &&
                    dgvPagamentos.Columns[e.ColumnIndex].HeaderText == "Valor" &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            // --- pnlRecentHeader (Dock=Top) ---
            var pnlRecentHeader = new Panel();
            pnlRecentHeader.Dock = DockStyle.Top;
            pnlRecentHeader.Height = 34;
            pnlRecentHeader.BackColor = Color.White;
            pnlRecentHeader.Padding = new Padding(14, 6, 0, 0);
            var lblHistorico = new Label();
            lblHistorico.Text = "Histórico de Pagamentos";
            lblHistorico.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblHistorico.ForeColor = Color.FromArgb(0x0c, 0x4a, 0x6e);
            lblHistorico.AutoSize = true;
            lblHistorico.Location = new Point(14, 8);
            pnlRecentHeader.Controls.Add(lblHistorico);

            // --- pnlSeparator (Dock=Top) ---
            var pnlSeparator = new Panel();
            pnlSeparator.Dock = DockStyle.Top;
            pnlSeparator.Height = 1;
            pnlSeparator.BackColor = Color.FromArgb(0xe2, 0xe8, 0xf0);

            // --- pnlRegisto (Dock=Top) ---
            var pnlRegisto = new Panel();
            pnlRegisto.Dock = DockStyle.Top;
            pnlRegisto.Height = 190;
            pnlRegisto.BackColor = Color.White;
            pnlRegisto.Padding = new Padding(14, 10, 14, 10);

            var lblRegistarHeader = new Label();
            lblRegistarHeader.Text = "REGISTAR PAGAMENTO";
            lblRegistarHeader.ForeColor = Color.FromArgb(0x1d, 0x4e, 0xd8);
            lblRegistarHeader.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblRegistarHeader.AutoSize = true;
            lblRegistarHeader.Location = new Point(14, 10);

            var tlp = new TableLayoutPanel();
            tlp.ColumnCount = 4;
            tlp.RowCount = 2;
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            tlp.Location = new Point(14, 34);
            tlp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tlp.Height = 130;

            // Col 0: Cliente
            var pnlCliente = new Panel();
            pnlCliente.Dock = DockStyle.Fill;
            var lblCliente = Theme.FieldLabel("Cliente *");
            cmbCliente = Theme.Combo();
            pnlCliente.Controls.Add(cmbCliente);
            pnlCliente.Controls.Add(lblCliente);
            tlp.Controls.Add(pnlCliente, 0, 0);

            // Col 1: Item
            var pnlItem = new Panel();
            pnlItem.Dock = DockStyle.Fill;
            var lblItem = Theme.FieldLabel("Item a Pagar *");
            cmbItem = Theme.Combo();
            pnlItem.Controls.Add(cmbItem);
            pnlItem.Controls.Add(lblItem);
            tlp.Controls.Add(pnlItem, 1, 0);

            // Col 2: Método
            var pnlMetodo = new Panel();
            pnlMetodo.Dock = DockStyle.Fill;
            var lblMetodo = Theme.FieldLabel("Método *");
            cmbMetodo = Theme.Combo();
            cmbMetodo.Items.AddRange(new object[] { "Dinheiro", "Cartao", "Transferencia", "MBWay", "PayPal" });
            pnlMetodo.Controls.Add(cmbMetodo);
            pnlMetodo.Controls.Add(lblMetodo);
            tlp.Controls.Add(pnlMetodo, 2, 0);

            // Col 3: Valor label
            lblValor = new Label();
            lblValor.Text = "Selecione um item";
            lblValor.ForeColor = Color.FromArgb(0x0c, 0x4a, 0x6e);
            lblValor.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblValor.Dock = DockStyle.Fill;
            lblValor.TextAlign = ContentAlignment.MiddleLeft;
            tlp.Controls.Add(lblValor, 3, 0);

            // Row 1: buttons (span all 4 columns)
            var flwButtons = new FlowLayoutPanel();
            flwButtons.Dock = DockStyle.Fill;
            flwButtons.WrapContents = false;
            flwButtons.FlowDirection = FlowDirection.LeftToRight;
            flwButtons.Padding = new Padding(0, 8, 0, 0);

            btnRegistar = Theme.BtnPrim("Registar Pagamento");
            btnAtualizar = Theme.BtnGray("⟳  Atualizar");
            flwButtons.Controls.Add(btnRegistar);
            flwButtons.Controls.Add(btnAtualizar);
            tlp.Controls.Add(flwButtons, 0, 1);
            tlp.SetColumnSpan(flwButtons, 4);

            pnlRegisto.Controls.Add(tlp);
            pnlRegisto.Controls.Add(lblRegistarHeader);

            // Resize handler so tlp fills pnlRegisto width
            pnlRegisto.Resize += (s, e) =>
            {
                tlp.Width = pnlRegisto.ClientSize.Width - 28;
            };

            // --- pnlTitle (Dock=Top) ---
            var pnlTitle = new Panel();
            pnlTitle.Dock = DockStyle.Top;
            pnlTitle.Height = 56;
            pnlTitle.BackColor = Theme.ContentBg;

            var lblTitle = new Label();
            lblTitle.Text = "Pagamentos";
            lblTitle.Font = Theme.FontTitle;
            lblTitle.ForeColor = Color.FromArgb(0x0c, 0x4a, 0x6e);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(18, 14);
            pnlTitle.Controls.Add(lblTitle);

            // Add order: Fill first, then Top panels in reverse visual order, title last
            this.Controls.Add(dgvPagamentos);      // Dock=Fill
            this.Controls.Add(pnlRecentHeader);    // Dock=Top
            this.Controls.Add(pnlSeparator);       // Dock=Top
            this.Controls.Add(pnlRegisto);         // Dock=Top
            this.Controls.Add(pnlTitle);           // Dock=Top — added last = visually topmost

            // Wire events
            cmbCliente.SelectedIndexChanged += (s, e) => LoadItens();
            cmbItem.SelectedIndexChanged += (s, e) => UpdateValorLabel();
            btnRegistar.Click += BtnRegistar_Click;
            btnAtualizar.Click += (s, e) => { LoadItens(); LoadPagamentos(); };
        }

        private void LoadClientes()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbCliente.DisplayMember = "nome";
                    cmbCliente.ValueMember = "cliente_id";
                    cmbCliente.DataSource = dt;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao carregar clientes: " + ex.Message, "Erro BD",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadItens()
        {
            _itensTable.Rows.Clear();

            if (cmbCliente.SelectedValue == null) return;

            int clienteId;
            try { clienteId = Convert.ToInt32(cmbCliente.SelectedValue); }
            catch { return; }

            try
            {
                using (var conn = Database.GetConnection())
                {
                    // Reservas pendentes sem pagamento
                    string sqlReservas = @"
SELECT r.reserva_id,
  ISNULL(s.nome, p.codigo) + ' — ' + CONVERT(varchar,r.data_reserva,103) + ' ' +
  CONVERT(varchar,r.hora_inicio,108) + '-' + CONVERT(varchar,r.hora_fim,108) AS descricao,
  r.valor
FROM reserva r
LEFT JOIN sala s ON r.sala_id=s.sala_id
LEFT JOIN posto_trabalho p ON r.posto_id=p.posto_id
WHERE r.cliente_id=@c AND r.estado IN ('Pendente','Confirmada')
AND NOT EXISTS (SELECT 1 FROM pagamento pg WHERE pg.reserva_id=r.reserva_id AND pg.estado='Pago')";

                    using (var cmd = new SqlCommand(sqlReservas, conn))
                    {
                        cmd.Parameters.AddWithValue("@c", clienteId);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                int reservaId = rdr.GetInt32(0);
                                string descricao = rdr.GetString(1);
                                decimal valor = rdr.GetDecimal(2);
                                _itensTable.Rows.Add(reservaId, "Reserva", "Reserva: " + descricao, valor, reservaId, DBNull.Value);
                            }
                        }
                    }

                    // Adesões pendentes sem pagamento
                    string sqlAdesoes = @"
SELECT a.adesao_id, p.nome_plano, p.preco_mensal
FROM adesao a JOIN plano p ON a.plano_id=p.plano_id
WHERE a.cliente_id=@c AND a.estado='Pendente'
AND NOT EXISTS (SELECT 1 FROM pagamento pg WHERE pg.adesao_id=a.adesao_id AND pg.estado='Pago')";

                    using (var cmd = new SqlCommand(sqlAdesoes, conn))
                    {
                        cmd.Parameters.AddWithValue("@c", clienteId);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                int adesaoId = rdr.GetInt32(0);
                                string nomePlano = rdr.GetString(1);
                                decimal preco = rdr.GetDecimal(2);
                                _itensTable.Rows.Add(adesaoId, "Adesão", "Adesão: " + nomePlano, preco, DBNull.Value, adesaoId);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao carregar itens: " + ex.Message, "Erro BD",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            cmbItem.DataSource = null;
            cmbItem.DataSource = _itensTable;
            cmbItem.DisplayMember = "descricao";
            cmbItem.ValueMember = "id";

            if (_itensTable.Rows.Count == 0)
                lblValor.Text = "Sem itens pendentes";
            else
                UpdateValorLabel();
        }

        private void UpdateValorLabel()
        {
            if (cmbItem.SelectedIndex < 0 || _itensTable.Rows.Count == 0) return;
            try
            {
                decimal valor = (decimal)_itensTable.Rows[cmbItem.SelectedIndex]["valor"];
                lblValor.Text = "Valor: " + Theme.FormatEuro(valor);
            }
            catch { }
        }

        private void LoadPagamentos()
        {
            try
            {
                using (var conn = Database.GetConnection())
                {
                    string sql = @"
SELECT p.pagamento_id AS ID, c.nome AS Cliente,
  CONVERT(varchar,p.data_pagamento,103) AS Data,
  p.valor AS Valor, p.metodo_pagamento AS Método, p.estado AS Estado,
  CASE WHEN p.reserva_id IS NOT NULL THEN 'Reserva #' + CAST(p.reserva_id AS varchar)
       ELSE 'Adesão #' + CAST(p.adesao_id AS varchar) END AS Referência
FROM pagamento p JOIN cliente c ON p.cliente_id=c.cliente_id
ORDER BY p.data_pagamento DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        dgvPagamentos.DataSource = dt;
                    }
                }

                if (dgvPagamentos.Columns.Contains("ID"))
                    dgvPagamentos.Columns["ID"].Visible = false;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao carregar pagamentos: " + ex.Message, "Erro BD",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRegistar_Click(object sender, EventArgs e)
        {
            if (cmbCliente.SelectedValue == null || cmbItem.SelectedIndex < 0 || _itensTable.Rows.Count == 0)
            {
                MessageBox.Show("Seleciona um cliente e um item.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var itemRow = _itensTable.Rows[cmbItem.SelectedIndex];
            int clienteId = Convert.ToInt32(cmbCliente.SelectedValue);
            decimal valor = (decimal)itemRow["valor"];
            object reservaId = itemRow["reserva_id"];
            object adesaoId = itemRow["adesao_id"];

            using (var conn = Database.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO pagamento (cliente_id,valor,metodo_pagamento,estado,reserva_id,adesao_id) VALUES (@c,@v,@m,'Pago',@rid,@aid)",
                        conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@c", clienteId);
                        cmd.Parameters.AddWithValue("@v", valor);
                        cmd.Parameters.AddWithValue("@m", cmbMetodo.Text);
                        cmd.Parameters.AddWithValue("@rid", reservaId is DBNull ? (object)DBNull.Value : Convert.ToInt32(reservaId));
                        cmd.Parameters.AddWithValue("@aid", adesaoId is DBNull ? (object)DBNull.Value : Convert.ToInt32(adesaoId));
                        cmd.ExecuteNonQuery();
                    }

                    if (!(reservaId is DBNull))
                    {
                        using (var cmd = new SqlCommand("UPDATE reserva SET estado='Confirmada' WHERE reserva_id=@id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", Convert.ToInt32(reservaId));
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new SqlCommand("UPDATE adesao SET estado='Ativa' WHERE adesao_id=@id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", Convert.ToInt32(adesaoId));
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                    MessageBox.Show("Pagamento registado com sucesso!", "Sucesso",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadItens();
                    LoadPagamentos();
                }
                catch (SqlException ex)
                {
                    tran.Rollback();
                    MessageBox.Show("Erro: " + ex.Message, "Erro BD",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
