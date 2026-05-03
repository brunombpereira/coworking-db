using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormSalas : Form
    {
        private readonly int _espacoId;
        private int _selectedId = -1;

        public FormSalas(int espacoId, string espacoNome)
        {
            InitializeComponent();
            _espacoId = espacoId;
            this.Text = "Salas — " + espacoNome;
            cmbEstado.Items.AddRange(new object[] { "Disponivel", "Indisponivel", "Manutencao", "Inativa" });
            cmbEstado.SelectedIndex = 0;
            dgvSalas.SelectionChanged += DgvSalas_SelectionChanged;
            SetEditMode(false);
            LoadData();
        }

        private void LoadData()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT sala_id AS ID, nome AS Nome, capacidade AS Capacidade, " +
                "preco_hora AS [Preço/Hora], estado AS Estado " +
                "FROM sala WHERE espaco_id=@eid ORDER BY nome", conn))
            {
                cmd.Parameters.AddWithValue("@eid", _espacoId);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvSalas.DataSource = dt;
                if (dgvSalas.Columns.Contains("ID")) dgvSalas.Columns["ID"].Visible = false;
            }
        }

        private void DgvSalas_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSalas.SelectedRows.Count == 0) return;
            var row = dgvSalas.SelectedRows[0];
            _selectedId        = Convert.ToInt32(row.Cells["ID"].Value);
            txtNome.Text       = row.Cells["Nome"].Value?.ToString();
            txtCapacidade.Text = row.Cells["Capacidade"].Value?.ToString();
            txtPrecoHora.Text  = row.Cells["Preço/Hora"].Value?.ToString();
            cmbEstado.Text     = row.Cells["Estado"].Value?.ToString();
        }

        private void btnNovo_Click(object sender, EventArgs e)
        {
            _selectedId = -1;
            txtNome.Text = txtCapacidade.Text = txtPrecoHora.Text = "";
            cmbEstado.SelectedIndex = 0;
            SetEditMode(true);
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text) ||
                !int.TryParse(txtCapacidade.Text, out var cap) || cap <= 0 ||
                !decimal.TryParse(txtPrecoHora.Text, out var preco) || preco < 0)
            {
                MessageBox.Show("Nome obrigatório. Capacidade e Preço devem ser números válidos.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = Database.GetConnection())
                {
                    string sql = _selectedId == -1
                        ? "INSERT INTO sala (nome,capacidade,preco_hora,estado,espaco_id) VALUES (@n,@c,@p,@e,@eid)"
                        : "UPDATE sala SET nome=@n,capacidade=@c,preco_hora=@p,estado=@e WHERE sala_id=@id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n",   txtNome.Text.Trim());
                        cmd.Parameters.AddWithValue("@c",   cap);
                        cmd.Parameters.AddWithValue("@p",   preco);
                        cmd.Parameters.AddWithValue("@e",   cmbEstado.Text);
                        if (_selectedId == -1) cmd.Parameters.AddWithValue("@eid", _espacoId);
                        else                   cmd.Parameters.AddWithValue("@id",  _selectedId);
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
            if (MessageBox.Show("Eliminar esta sala?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM sala WHERE sala_id=@id", conn))
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
            dgvSalas.Enabled    = !editing;
        }
    }
}
