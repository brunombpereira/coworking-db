using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormEspacos : Form
    {
        private int _selectedId = -1;

        public FormEspacos()
        {
            InitializeComponent();
            this.Text = "Espaços";
            dgvEspacos.SelectionChanged += DgvEspacos_SelectionChanged;
            SetEditMode(false);
            LoadData();
        }

        private void LoadData()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT espaco_id AS ID, nome AS Nome, morada AS Morada, " +
                "telefone AS Telefone, email AS Email, " +
                "CONVERT(varchar,hora_abertura,108) AS Abertura, " +
                "CONVERT(varchar,hora_fecho,108) AS Fecho " +
                "FROM espaco ORDER BY nome", conn))
            {
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvEspacos.DataSource = dt;
                if (dgvEspacos.Columns.Contains("ID")) dgvEspacos.Columns["ID"].Visible = false;
            }
        }

        private void DgvEspacos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvEspacos.SelectedRows.Count == 0) return;
            var row = dgvEspacos.SelectedRows[0];
            _selectedId          = Convert.ToInt32(row.Cells["ID"].Value);
            txtNome.Text         = row.Cells["Nome"].Value?.ToString();
            txtMorada.Text       = row.Cells["Morada"].Value?.ToString();
            txtTelefone.Text     = row.Cells["Telefone"].Value?.ToString();
            txtEmail.Text        = row.Cells["Email"].Value?.ToString();
            txtHoraAbertura.Text = row.Cells["Abertura"].Value?.ToString();
            txtHoraFecho.Text    = row.Cells["Fecho"].Value?.ToString();
        }

        private void btnNovo_Click(object sender, EventArgs e)
        {
            _selectedId = -1;
            txtNome.Text = txtMorada.Text = txtTelefone.Text = txtEmail.Text = "";
            txtHoraAbertura.Text = "08:00";
            txtHoraFecho.Text    = "20:00";
            SetEditMode(true);
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text) ||
                string.IsNullOrWhiteSpace(txtMorada.Text) ||
                string.IsNullOrWhiteSpace(txtHoraAbertura.Text) ||
                string.IsNullOrWhiteSpace(txtHoraFecho.Text))
            {
                MessageBox.Show("Nome, Morada, Hora Abertura e Hora Fecho são obrigatórios.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = Database.GetConnection())
                {
                    string sql = _selectedId == -1
                        ? "INSERT INTO espaco (nome,morada,telefone,email,hora_abertura,hora_fecho) VALUES (@n,@m,@t,@e,@ha,@hf)"
                        : "UPDATE espaco SET nome=@n,morada=@m,telefone=@t,email=@e,hora_abertura=@ha,hora_fecho=@hf WHERE espaco_id=@id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n",  txtNome.Text.Trim());
                        cmd.Parameters.AddWithValue("@m",  txtMorada.Text.Trim());
                        cmd.Parameters.AddWithValue("@t",  string.IsNullOrWhiteSpace(txtTelefone.Text) ? (object)DBNull.Value : txtTelefone.Text.Trim());
                        cmd.Parameters.AddWithValue("@e",  string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@ha", txtHoraAbertura.Text.Trim());
                        cmd.Parameters.AddWithValue("@hf", txtHoraFecho.Text.Trim());
                        if (_selectedId != -1) cmd.Parameters.AddWithValue("@id", _selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                SetEditMode(false);
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_selectedId == -1) return;
            if (MessageBox.Show("Eliminar este espaço?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM espaco WHERE espaco_id=@id", conn))
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

        private void btnVerSalas_Click(object sender, EventArgs e)
        {
            if (_selectedId == -1) return;
            new FormSalas(_selectedId, dgvEspacos.SelectedRows[0].Cells["Nome"].Value?.ToString()).Show();
        }

        private void btnVerPostos_Click(object sender, EventArgs e)
        {
            if (_selectedId == -1) return;
            new FormPostos(_selectedId, dgvEspacos.SelectedRows[0].Cells["Nome"].Value?.ToString()).Show();
        }

        private void btnCancelar_Click(object sender, EventArgs e) => SetEditMode(false);

        private void SetEditMode(bool editing)
        {
            btnNovo.Enabled      = !editing;
            btnGuardar.Enabled   =  editing;
            btnCancelar.Enabled  =  editing;
            btnEliminar.Enabled  = !editing && _selectedId != -1;
            btnVerSalas.Enabled  = !editing && _selectedId != -1;
            btnVerPostos.Enabled = !editing && _selectedId != -1;
            dgvEspacos.Enabled   = !editing;
        }
    }
}
