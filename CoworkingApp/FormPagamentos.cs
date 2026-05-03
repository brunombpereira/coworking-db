using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormPagamentos : Form
    {
        private DataTable _itensTable;

        public FormPagamentos()
        {
            InitializeComponent();
            this.Text = "Pagamentos";
            cmbMetodo.Items.AddRange(new object[] { "Dinheiro","Cartao","Transferencia","MBWay","PayPal" });
            cmbMetodo.SelectedIndex = 0;
            cmbCliente.SelectedIndexChanged += (s, e) => LoadItens();
            LoadClientes();
            LoadPagamentos();
        }

        private void LoadClientes()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
            {
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                cmbCliente.DataSource    = dt;
                cmbCliente.DisplayMember = "nome";
                cmbCliente.ValueMember   = "cliente_id";
            }
        }

        private void LoadItens()
        {
            if (cmbCliente.SelectedValue == null) return;
            int clienteId = Convert.ToInt32(cmbCliente.SelectedValue);
            _itensTable = new DataTable();
            _itensTable.Columns.Add("id",         typeof(int));
            _itensTable.Columns.Add("tipo",        typeof(string));
            _itensTable.Columns.Add("descricao",   typeof(string));
            _itensTable.Columns.Add("valor",       typeof(decimal));
            _itensTable.Columns.Add("reserva_id",  typeof(object));
            _itensTable.Columns.Add("adesao_id",   typeof(object));

            using (var conn = Database.GetConnection())
            {
                using (var cmd = new SqlCommand(
                    "SELECT r.reserva_id, " +
                    "ISNULL(s.nome, p.codigo) + ' — ' + CONVERT(varchar,r.data_reserva,103) + ' ' + CONVERT(varchar,r.hora_inicio,108) + '-' + CONVERT(varchar,r.hora_fim,108) AS descricao, " +
                    "r.valor FROM reserva r " +
                    "LEFT JOIN sala s ON r.sala_id=s.sala_id " +
                    "LEFT JOIN posto_trabalho p ON r.posto_id=p.posto_id " +
                    "WHERE r.cliente_id=@c AND r.estado IN ('Pendente','Confirmada') " +
                    "AND NOT EXISTS (SELECT 1 FROM pagamento pg WHERE pg.reserva_id=r.reserva_id AND pg.estado='Pago')", conn))
                {
                    cmd.Parameters.AddWithValue("@c", clienteId);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            _itensTable.Rows.Add(reader.GetInt32(0), "Reserva", "Reserva: " + reader.GetString(1), reader.GetDecimal(2), reader.GetInt32(0), DBNull.Value);
                }
                using (var cmd = new SqlCommand(
                    "SELECT a.adesao_id, p.nome_plano, p.preco_mensal " +
                    "FROM adesao a JOIN plano p ON a.plano_id=p.plano_id " +
                    "WHERE a.cliente_id=@c AND a.estado='Pendente' " +
                    "AND NOT EXISTS (SELECT 1 FROM pagamento pg WHERE pg.adesao_id=a.adesao_id AND pg.estado='Pago')", conn))
                {
                    cmd.Parameters.AddWithValue("@c", clienteId);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            _itensTable.Rows.Add(reader.GetInt32(0), "Adesão", "Adesão: " + reader.GetString(1), reader.GetDecimal(2), DBNull.Value, reader.GetInt32(0));
                }
            }
            cmbItem.DataSource    = _itensTable;
            cmbItem.DisplayMember = "descricao";
            cmbItem.ValueMember   = "id";
            lblValor.Text = _itensTable.Rows.Count > 0 ? "" : "Sem itens pendentes de pagamento.";
            cmbItem.SelectedIndexChanged += (s, e) => UpdateValorLabel();
            UpdateValorLabel();
        }

        private void UpdateValorLabel()
        {
            if (cmbItem.SelectedIndex < 0 || _itensTable == null || _itensTable.Rows.Count == 0) return;
            var row = _itensTable.Rows[cmbItem.SelectedIndex];
            lblValor.Text = "Valor a pagar: " + ((decimal)row["valor"]).ToString("C");
        }

        private void LoadPagamentos()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT p.pagamento_id AS ID, c.nome AS Cliente, " +
                "CONVERT(varchar,p.data_pagamento,103) AS Data, " +
                "p.valor AS Valor, p.metodo_pagamento AS Método, p.estado AS Estado, " +
                "CASE WHEN p.reserva_id IS NOT NULL THEN 'Reserva #' + CAST(p.reserva_id AS varchar) " +
                "     ELSE 'Adesão #' + CAST(p.adesao_id AS varchar) END AS Referência " +
                "FROM pagamento p JOIN cliente c ON p.cliente_id=c.cliente_id " +
                "ORDER BY p.data_pagamento DESC", conn))
            {
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvPagamentos.DataSource = dt;
                if (dgvPagamentos.Columns.Contains("ID")) dgvPagamentos.Columns["ID"].Visible = false;
            }
        }

        private void btnRegistarPagamento_Click(object sender, EventArgs e)
        {
            if (cmbCliente.SelectedValue == null || cmbItem.SelectedIndex < 0 ||
                _itensTable == null || _itensTable.Rows.Count == 0)
            {
                MessageBox.Show("Seleciona um cliente e um item.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var itemRow   = _itensTable.Rows[cmbItem.SelectedIndex];
            int clienteId = Convert.ToInt32(cmbCliente.SelectedValue);
            decimal valor = (decimal)itemRow["valor"];
            object reservaId = itemRow["reserva_id"];
            object adesaoId  = itemRow["adesao_id"];

            using (var conn = Database.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO pagamento (cliente_id,valor,metodo_pagamento,estado,reserva_id,adesao_id) " +
                        "VALUES (@c,@v,@m,'Pago',@rid,@aid)", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@c",   clienteId);
                        cmd.Parameters.AddWithValue("@v",   valor);
                        cmd.Parameters.AddWithValue("@m",   cmbMetodo.Text);
                        cmd.Parameters.AddWithValue("@rid", reservaId is DBNull ? (object)DBNull.Value : Convert.ToInt32(reservaId));
                        cmd.Parameters.AddWithValue("@aid", adesaoId  is DBNull ? (object)DBNull.Value : Convert.ToInt32(adesaoId));
                        cmd.ExecuteNonQuery();
                    }
                    if (!(reservaId is DBNull))
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE reserva SET estado='Confirmada' WHERE reserva_id=@id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", Convert.ToInt32(reservaId));
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE adesao SET estado='Ativa' WHERE adesao_id=@id", conn, tran))
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
                    MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnAtualizar_Click(object sender, EventArgs e)
        {
            LoadItens();
            LoadPagamentos();
        }
    }
}
