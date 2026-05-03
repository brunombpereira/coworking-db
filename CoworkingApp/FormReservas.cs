using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormReservas : Form
    {
        public FormReservas()
        {
            InitializeComponent();
            this.Text = "Reservas";
            cmbFiltroEstado.Items.AddRange(new object[] { "(Todos)","Pendente","Confirmada","Cancelada","Concluida" });
            cmbFiltroEstado.SelectedIndex = 0;
            cmbFiltroEstado.SelectedIndexChanged += (s, e) => LoadData();
            LoadData();
        }

        private void LoadData()
        {
            string filtro = cmbFiltroEstado.Text == "(Todos)" ? "" : cmbFiltroEstado.Text;
            using (var conn = Database.GetConnection())
            {
                string sql =
                    "SELECT r.reserva_id AS ID, c.nome AS Cliente, " +
                    "ISNULL(s.nome, p.codigo) AS Recurso, " +
                    "CASE WHEN r.sala_id IS NOT NULL THEN 'Sala' ELSE 'Posto' END AS Tipo, " +
                    "CONVERT(varchar,r.data_reserva,103) AS Data, " +
                    "CONVERT(varchar,r.hora_inicio,108) AS [H.Início], " +
                    "CONVERT(varchar,r.hora_fim,108) AS [H.Fim], " +
                    "r.valor AS Valor, r.estado AS Estado " +
                    "FROM reserva r " +
                    "JOIN cliente c ON r.cliente_id=c.cliente_id " +
                    "LEFT JOIN sala s ON r.sala_id=s.sala_id " +
                    "LEFT JOIN posto_trabalho p ON r.posto_id=p.posto_id " +
                    (string.IsNullOrEmpty(filtro) ? "" : "WHERE r.estado=@e ") +
                    "ORDER BY r.data_reserva DESC, r.hora_inicio";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(filtro))
                        cmd.Parameters.AddWithValue("@e", filtro);
                    var dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    dgvReservas.DataSource = dt;
                    if (dgvReservas.Columns.Contains("ID")) dgvReservas.Columns["ID"].Visible = false;
                }
            }
        }

        private void btnNovaReserva_Click(object sender, EventArgs e)
        {
            new FormNovaReserva().ShowDialog();
            LoadData();
        }

        private void btnCancelarReserva_Click(object sender, EventArgs e)
        {
            if (dgvReservas.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvReservas.SelectedRows[0].Cells["ID"].Value);
            string estado = dgvReservas.SelectedRows[0].Cells["Estado"].Value?.ToString();
            if (estado == "Cancelada" || estado == "Concluida")
            {
                MessageBox.Show("Não é possível cancelar uma reserva já " + estado.ToLower() + ".", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Cancelar esta reserva?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("UPDATE reserva SET estado='Cancelada' WHERE reserva_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAtualizar_Click(object sender, EventArgs e) => LoadData();
    }
}
