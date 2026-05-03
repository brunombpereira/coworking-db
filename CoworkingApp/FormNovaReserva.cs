using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormNovaReserva : Form
    {
        private decimal _valorCalculado = 0;

        public FormNovaReserva()
        {
            InitializeComponent();
            this.Text = "Nova Reserva";
            dtpData.Value       = DateTime.Today;
            dtpHoraInicio.Value = DateTime.Today.AddHours(9);
            dtpHoraFim.Value    = DateTime.Today.AddHours(10);
            rbSala.Checked = true;
            rbSala.CheckedChanged  += (s, e) => { LoadRecursos(); ToggleParticipantes(); };
            rbPosto.CheckedChanged += (s, e) => { LoadRecursos(); ToggleParticipantes(); };
            LoadClientes();
            LoadRecursos();
            ToggleParticipantes();
            lblValorCalculado.Text = "Valor: —";
            btnConfirmar.Enabled   = false;
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

        private void LoadRecursos()
        {
            using (var conn = Database.GetConnection())
            {
                string sql = rbSala.Checked
                    ? "SELECT s.sala_id AS id, e.nome + ' — ' + s.nome + ' (cap:' + CAST(s.capacidade AS varchar) + ')' AS descricao, s.preco_hora " +
                      "FROM sala s JOIN espaco e ON s.espaco_id=e.espaco_id WHERE s.estado='Disponivel' ORDER BY descricao"
                    : "SELECT p.posto_id AS id, e.nome + ' — ' + p.codigo + ' (' + p.tipo + ')' AS descricao, p.preco_hora " +
                      "FROM posto_trabalho p JOIN espaco e ON p.espaco_id=e.espaco_id WHERE p.estado='Disponivel' ORDER BY descricao";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    var dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    cmbRecurso.DataSource    = dt;
                    cmbRecurso.DisplayMember = "descricao";
                    cmbRecurso.ValueMember   = "id";
                }
            }
            _valorCalculado = 0;
            lblValorCalculado.Text = "Valor: —";
            btnConfirmar.Enabled   = false;
        }

        private void ToggleParticipantes()
        {
            lblParticipantes.Visible = rbSala.Checked;
            txtParticipantes.Visible = rbSala.Checked;
        }

        private void btnCalcular_Click(object sender, EventArgs e)
        {
            if (cmbRecurso.SelectedValue == null) return;
            var inicio = dtpHoraInicio.Value.TimeOfDay;
            var fim    = dtpHoraFim.Value.TimeOfDay;
            if (fim <= inicio)
            {
                MessageBox.Show("Hora de fim deve ser posterior à hora de início.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var conn = Database.GetConnection())
            {
                string sql = rbSala.Checked
                    ? "SELECT preco_hora FROM sala WHERE sala_id=@id"
                    : "SELECT preco_hora FROM posto_trabalho WHERE posto_id=@id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", cmbRecurso.SelectedValue);
                    decimal precoHora = (decimal)cmd.ExecuteScalar();
                    double horas      = (fim - inicio).TotalHours;
                    _valorCalculado   = (decimal)horas * precoHora;
                    lblValorCalculado.Text = "Valor: " + _valorCalculado.ToString("C");
                    btnConfirmar.Enabled   = true;
                }
            }
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            if (cmbCliente.SelectedValue == null || cmbRecurso.SelectedValue == null) return;
            var inicio = dtpHoraInicio.Value.TimeOfDay;
            var fim    = dtpHoraFim.Value.TimeOfDay;
            if (fim <= inicio)
            {
                MessageBox.Show("Hora de fim deve ser posterior à hora de início.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int? participantes = null;
            if (rbSala.Checked && !string.IsNullOrWhiteSpace(txtParticipantes.Text))
            {
                if (!int.TryParse(txtParticipantes.Text, out var p) || p <= 0)
                {
                    MessageBox.Show("Número de participantes inválido.", "Validação",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                participantes = p;
            }
            try
            {
                using (var conn = Database.GetConnection())
                {
                    string sql = rbSala.Checked
                        ? "INSERT INTO reserva (cliente_id,sala_id,data_reserva,hora_inicio,hora_fim,estado,valor,num_participantes,notas) " +
                          "VALUES (@c,@r,@d,@hi,@hf,'Pendente',@v,@np,@n)"
                        : "INSERT INTO reserva (cliente_id,posto_id,data_reserva,hora_inicio,hora_fim,estado,valor,notas) " +
                          "VALUES (@c,@r,@d,@hi,@hf,'Pendente',@v,@n)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@c",  cmbCliente.SelectedValue);
                        cmd.Parameters.AddWithValue("@r",  cmbRecurso.SelectedValue);
                        cmd.Parameters.AddWithValue("@d",  dtpData.Value.Date);
                        cmd.Parameters.AddWithValue("@hi", inicio.ToString(@"hh\:mm"));
                        cmd.Parameters.AddWithValue("@hf", fim.ToString(@"hh\:mm"));
                        cmd.Parameters.AddWithValue("@v",  _valorCalculado);
                        if (rbSala.Checked)
                            cmd.Parameters.AddWithValue("@np", participantes.HasValue ? (object)participantes.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@n",  string.IsNullOrWhiteSpace(txtNotas.Text)
                            ? (object)DBNull.Value : txtNotas.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Reserva criada com sucesso!", "Sucesso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e) => this.Close();
    }
}
