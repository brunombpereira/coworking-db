using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormAdesoes : Form
    {
        private int _selectedId = -1;

        public FormAdesoes()
        {
            InitializeComponent();
            this.Text = "Adesões";
            cmbEstado.Items.AddRange(new object[] { "Pendente","Ativa","Suspensa","Cancelada","Terminada" });
            cmbEstado.SelectedIndex = 0;
            dtpDataInicio.Value = DateTime.Today;
            dgvAdesoes.SelectionChanged += DgvAdesoes_SelectionChanged;
            LoadCombos();
            SetEditMode(false);
            LoadData();
        }

        private void LoadCombos()
        {
            using (var conn = Database.GetConnection())
            {
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                {
                    var dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    cmbCliente.DataSource    = dt;
                    cmbCliente.DisplayMember = "nome";
                    cmbCliente.ValueMember   = "cliente_id";
                }
                using (var cmd = new SqlCommand("SELECT plano_id, nome_plano FROM plano ORDER BY nome_plano", conn))
                {
                    var dt = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt);
                    cmbPlano.DataSource    = dt;
                    cmbPlano.DisplayMember = "nome_plano";
                    cmbPlano.ValueMember   = "plano_id";
                }
            }
            cmbPlano.SelectedIndexChanged += (s, e) => UpdateDataFimLabel();
            dtpDataInicio.ValueChanged    += (s, e) => UpdateDataFimLabel();
            UpdateDataFimLabel();
        }

        private void UpdateDataFimLabel()
        {
            if (cmbPlano.SelectedValue == null) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT duracao_meses FROM plano WHERE plano_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", cmbPlano.SelectedValue);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        int dur = (int)result;
                        lblDataFim.Text = "Data fim calculada: " + dtpDataInicio.Value.AddMonths(dur).ToString("dd/MM/yyyy");
                    }
                }
            }
            catch { }
        }

        private void LoadData()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT a.adesao_id AS ID, c.nome AS Cliente, p.nome_plano AS Plano, " +
                "CONVERT(varchar,a.data_inicio,103) AS [Início], " +
                "CONVERT(varchar,a.data_fim,103) AS [Fim], a.estado AS Estado " +
                "FROM adesao a JOIN cliente c ON a.cliente_id=c.cliente_id " +
                "JOIN plano p ON a.plano_id=p.plano_id ORDER BY a.data_inicio DESC", conn))
            {
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvAdesoes.DataSource = dt;
                if (dgvAdesoes.Columns.Contains("ID")) dgvAdesoes.Columns["ID"].Visible = false;
            }
        }

        private void DgvAdesoes_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAdesoes.SelectedRows.Count == 0) return;
            var row = dgvAdesoes.SelectedRows[0];
            _selectedId    = Convert.ToInt32(row.Cells["ID"].Value);
            cmbEstado.Text = row.Cells["Estado"].Value?.ToString();
        }

        private void btnNovo_Click(object sender, EventArgs e)
        {
            _selectedId = -1;
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
            if (cmbPlano.Items.Count > 0)   cmbPlano.SelectedIndex   = 0;
            dtpDataInicio.Value = DateTime.Today;
            cmbEstado.SelectedIndex = 0;
            SetEditMode(true);
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (_selectedId == -1)
            {
                try
                {
                    using (var conn = Database.GetConnection())
                    using (var cmd = new SqlCommand(
                        "INSERT INTO adesao (cliente_id,plano_id,data_inicio,estado) VALUES (@c,@p,@d,@e)", conn))
                    {
                        cmd.Parameters.AddWithValue("@c", cmbCliente.SelectedValue);
                        cmd.Parameters.AddWithValue("@p", cmbPlano.SelectedValue);
                        cmd.Parameters.AddWithValue("@d", dtpDataInicio.Value.Date);
                        cmd.Parameters.AddWithValue("@e", cmbEstado.Text);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                try
                {
                    using (var conn = Database.GetConnection())
                    using (var cmd = new SqlCommand(
                        "UPDATE adesao SET estado=@e WHERE adesao_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@e",  cmbEstado.Text);
                        cmd.Parameters.AddWithValue("@id", _selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            SetEditMode(false);
            LoadData();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_selectedId == -1) return;
            if (MessageBox.Show("Eliminar esta adesão?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM adesao WHERE adesao_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _selectedId);
                    cmd.ExecuteNonQuery();
                }
                _selectedId = -1;
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e) => SetEditMode(false);

        private void SetEditMode(bool editing)
        {
            btnNovo.Enabled      = !editing;
            btnGuardar.Enabled   =  editing;
            btnCancelar.Enabled  =  editing;
            btnEliminar.Enabled  = !editing && _selectedId != -1;
            dgvAdesoes.Enabled   = !editing;
            cmbCliente.Enabled   =  editing && _selectedId == -1;
            cmbPlano.Enabled     =  editing && _selectedId == -1;
            dtpDataInicio.Enabled=  editing && _selectedId == -1;
            cmbEstado.Enabled    =  editing;
        }
    }
}
