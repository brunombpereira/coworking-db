using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormClientes : Form
    {
        private int _selectedId = -1;

        public FormClientes()
        {
            InitializeComponent();
            this.Text = "Clientes";
            dgvClientes.SelectionChanged += DgvClientes_SelectionChanged;
            SetEditMode(false);
            LoadData();
        }

        private void LoadData()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT cliente_id AS ID, nome AS Nome, nif AS NIF, " +
                "email AS Email, telefone AS Telefone, " +
                "CONVERT(varchar,data_registo,103) AS [Data Registo] " +
                "FROM cliente ORDER BY nome", conn))
            {
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvClientes.DataSource = dt;
                if (dgvClientes.Columns.Contains("ID")) dgvClientes.Columns["ID"].Visible = false;
            }
        }

        private void DgvClientes_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvClientes.SelectedRows.Count == 0) return;
            var row = dgvClientes.SelectedRows[0];
            _selectedId      = Convert.ToInt32(row.Cells["ID"].Value);
            txtNome.Text     = row.Cells["Nome"].Value?.ToString();
            txtNif.Text      = row.Cells["NIF"].Value?.ToString();
            txtEmail.Text    = row.Cells["Email"].Value?.ToString();
            txtTelefone.Text = row.Cells["Telefone"].Value?.ToString();
        }

        private void btnNovo_Click(object sender, EventArgs e)
        {
            _selectedId = -1;
            txtNome.Text = txtNif.Text = txtEmail.Text = txtTelefone.Text = "";
            SetEditMode(true);
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text) ||
                string.IsNullOrWhiteSpace(txtNif.Text)  ||
                string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Nome, NIF e Email são obrigatórios.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = Database.GetConnection())
                {
                    string sql = _selectedId == -1
                        ? "INSERT INTO cliente (nome,nif,email,telefone) VALUES (@n,@nif,@e,@t)"
                        : "UPDATE cliente SET nome=@n,nif=@nif,email=@e,telefone=@t WHERE cliente_id=@id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n",   txtNome.Text.Trim());
                        cmd.Parameters.AddWithValue("@nif", txtNif.Text.Trim());
                        cmd.Parameters.AddWithValue("@e",   txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@t",   string.IsNullOrWhiteSpace(txtTelefone.Text)
                            ? (object)DBNull.Value : txtTelefone.Text.Trim());
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
            if (MessageBox.Show("Eliminar este cliente e todos os seus dados?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM cliente WHERE cliente_id=@id", conn))
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
            btnNovo.Enabled     = !editing;
            btnGuardar.Enabled  =  editing;
            btnCancelar.Enabled =  editing;
            btnEliminar.Enabled = !editing && _selectedId != -1;
            dgvClientes.Enabled = !editing;
        }
    }
}
