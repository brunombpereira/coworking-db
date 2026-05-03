using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormRelatorios : Form
    {
        public FormRelatorios()
        {
            InitializeComponent();
            this.Text = "Relatórios";
            dtpOcupIni.Value = DateTime.Today.AddDays(-30);
            dtpOcupFim.Value = DateTime.Today;
            dtpRecIni.Value  = DateTime.Today.AddDays(-30);
            dtpRecFim.Value  = DateTime.Today;
            LoadClientesCombos();
        }

        private void LoadClientesCombos()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
            {
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                cmbClienteHist.DataSource    = dt.Copy();
                cmbClienteHist.DisplayMember = "nome";
                cmbClienteHist.ValueMember   = "cliente_id";
                cmbClientePag.DataSource     = dt.Copy();
                cmbClientePag.DisplayMember  = "nome";
                cmbClientePag.ValueMember    = "cliente_id";
            }
        }

        private void btnPesquisarDisp_Click(object sender, EventArgs e)
        {
            var data = dtpDisp.Value.Date;
            var hi   = dtpDispHI.Value.TimeOfDay.ToString(@"hh\:mm");
            var hf   = dtpDispHF.Value.TimeOfDay.ToString(@"hh\:mm");
            string sql = rbDispSala.Checked
                ? "SELECT s.sala_id AS ID, e.nome AS Espaço, s.nome AS Sala, " +
                  "s.capacidade AS Capacidade, s.preco_hora AS [€/Hora] " +
                  "FROM sala s JOIN espaco e ON s.espaco_id=e.espaco_id " +
                  "WHERE s.estado='Disponivel' " +
                  "AND NOT EXISTS (" +
                  "  SELECT 1 FROM reserva r WHERE r.sala_id=s.sala_id " +
                  "  AND r.data_reserva=@d AND r.estado<>'Cancelada' " +
                  "  AND r.hora_inicio < @hf AND r.hora_fim > @hi) " +
                  "ORDER BY e.nome, s.nome"
                : "SELECT p.posto_id AS ID, e.nome AS Espaço, p.codigo AS Código, " +
                  "p.tipo AS Tipo, p.preco_hora AS [€/Hora] " +
                  "FROM posto_trabalho p JOIN espaco e ON p.espaco_id=e.espaco_id " +
                  "WHERE p.estado='Disponivel' " +
                  "AND NOT EXISTS (" +
                  "  SELECT 1 FROM reserva r WHERE r.posto_id=p.posto_id " +
                  "  AND r.data_reserva=@d AND r.estado<>'Cancelada' " +
                  "  AND r.hora_inicio < @hf AND r.hora_fim > @hi) " +
                  "ORDER BY e.nome, p.codigo";
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@d",  data);
                cmd.Parameters.AddWithValue("@hi", hi);
                cmd.Parameters.AddWithValue("@hf", hf);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvDisponibilidade.DataSource = dt;
                if (dt.Columns.Contains("ID")) dgvDisponibilidade.Columns["ID"].Visible = false;
            }
        }

        private void btnPesquisarHist_Click(object sender, EventArgs e)
        {
            if (cmbClienteHist.SelectedValue == null) return;
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(s.nome, p.codigo) AS Recurso, " +
                "CASE WHEN r.sala_id IS NOT NULL THEN 'Sala' ELSE 'Posto' END AS Tipo, " +
                "CONVERT(varchar,r.data_reserva,103) AS Data, " +
                "CONVERT(varchar,r.hora_inicio,108)+'-'+CONVERT(varchar,r.hora_fim,108) AS Horário, " +
                "r.valor AS Valor, r.estado AS Estado " +
                "FROM reserva r " +
                "LEFT JOIN sala s ON r.sala_id=s.sala_id " +
                "LEFT JOIN posto_trabalho p ON r.posto_id=p.posto_id " +
                "WHERE r.cliente_id=@c ORDER BY r.data_reserva DESC", conn))
            {
                cmd.Parameters.AddWithValue("@c", cmbClienteHist.SelectedValue);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvHistCliente.DataSource = dt;
            }
        }

        private void btnPesquisarPag_Click(object sender, EventArgs e)
        {
            if (cmbClientePag.SelectedValue == null) return;
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT CONVERT(varchar,p.data_pagamento,103) AS Data, p.valor AS Valor, " +
                "p.metodo_pagamento AS Método, p.estado AS Estado, " +
                "CASE WHEN p.reserva_id IS NOT NULL THEN 'Reserva #'+CAST(p.reserva_id AS varchar) " +
                "     ELSE 'Adesão #'+CAST(p.adesao_id AS varchar) END AS Referência " +
                "FROM pagamento p WHERE p.cliente_id=@c ORDER BY p.data_pagamento DESC", conn))
            {
                cmd.Parameters.AddWithValue("@c", cmbClientePag.SelectedValue);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvHistPag.DataSource = dt;
            }
        }

        private void btnPesquisarOcup_Click(object sender, EventArgs e)
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT e.nome AS Espaço, " +
                "COUNT(DISTINCT s.sala_id) AS Salas, " +
                "COUNT(DISTINCT p.posto_id) AS Postos, " +
                "SUM(CASE WHEN r.sala_id IS NOT NULL AND r.estado<>'Cancelada' " +
                "         AND r.data_reserva BETWEEN @ini AND @fim THEN 1 ELSE 0 END) AS [Reservas Sala], " +
                "SUM(CASE WHEN r.posto_id IS NOT NULL AND r.estado<>'Cancelada' " +
                "         AND r.data_reserva BETWEEN @ini AND @fim THEN 1 ELSE 0 END) AS [Reservas Posto] " +
                "FROM espaco e " +
                "LEFT JOIN sala s ON e.espaco_id=s.espaco_id " +
                "LEFT JOIN posto_trabalho p ON e.espaco_id=p.espaco_id " +
                "LEFT JOIN reserva r ON (r.sala_id=s.sala_id OR r.posto_id=p.posto_id) " +
                "GROUP BY e.espaco_id, e.nome ORDER BY e.nome", conn))
            {
                cmd.Parameters.AddWithValue("@ini", dtpOcupIni.Value.Date);
                cmd.Parameters.AddWithValue("@fim", dtpOcupFim.Value.Date);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvOcupacao.DataSource = dt;
            }
        }

        private void btnPesquisarRec_Click(object sender, EventArgs e)
        {
            using (var conn = Database.GetConnection())
            {
                using (var cmd = new SqlCommand(
                    "SELECT metodo_pagamento AS Método, COUNT(*) AS [Nº Pagamentos], " +
                    "SUM(valor) AS Total " +
                    "FROM pagamento " +
                    "WHERE estado='Pago' AND data_pagamento BETWEEN @ini AND @fim " +
                    "GROUP BY metodo_pagamento ORDER BY Total DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@ini", dtpRecIni.Value.Date);
                    cmd.Parameters.AddWithValue("@fim", dtpRecFim.Value.Date);
                    var dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    dgvReceita.DataSource = dt;
                }
                using (var cmdTotal = new SqlCommand(
                    "SELECT ISNULL(SUM(valor),0) FROM pagamento WHERE estado='Pago' " +
                    "AND data_pagamento BETWEEN @ini AND @fim", conn))
                {
                    cmdTotal.Parameters.AddWithValue("@ini", dtpRecIni.Value.Date);
                    cmdTotal.Parameters.AddWithValue("@fim", dtpRecFim.Value.Date);
                    lblTotalReceita.Text = "Total: " + ((decimal)cmdTotal.ExecuteScalar()).ToString("C");
                }
            }
        }
    }
}
