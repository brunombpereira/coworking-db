using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormPlanos : Form
    {
        private int _selectedId = -1;

        public FormPlanos()
        {
            InitializeComponent();
            this.Text = "Planos";
            dgvPlanos.SelectionChanged += DgvPlanos_SelectionChanged;
            SetEditMode(false);
            LoadData();
        }

        private void LoadData()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT plano_id AS ID, nome_plano AS Nome, preco_mensal AS [Preço/Mês], " +
                "duracao_meses AS [Duração (meses)], descricao AS Descrição FROM plano ORDER BY nome_plano", conn))
            {
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvPlanos.DataSource = dt;
                if (dgvPlanos.Columns.Contains("ID")) dgvPlanos.Columns["ID"].Visible = false;
            }
        }

        private void DgvPlanos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPlanos.SelectedRows.Count == 0) return;
            var row = dgvPlanos.SelectedRows[0];
            _selectedId       = Convert.ToInt32(row.Cells["ID"].Value);
            txtNome.Text      = row.Cells["Nome"].Value?.ToString();
            txtPreco.Text     = row.Cells["Preço/Mês"].Value?.ToString();
            txtDuracao.Text   = row.Cells["Duração (meses)"].Value?.ToString();
            txtDescricao.Text = row.Cells["Descrição"].Value?.ToString();
        }

        private void btnNovo_Click(object sender, EventArgs e)
        {
            _selectedId = -1;
            txtNome.Text = txtPreco.Text = txtDuracao.Text = txtDescricao.Text = "";
            SetEditMode(true);
            txtNome.Focus();
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text) ||
                string.IsNullOrWhiteSpace(txtPreco.Text) ||
                string.IsNullOrWhiteSpace(txtDuracao.Text))
            {
                MessageBox.Show("Nome, Preço e Duração são obrigatórios.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtPreco.Text, out var preco) || preco < 0)
            {
                MessageBox.Show("Preço inválido.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(txtDuracao.Text, out var dur) || dur <= 0)
            {
                MessageBox.Show("Duração inválida.", "Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = Database.GetConnection())
                {
                    string sql = _selectedId == -1
                        ? "INSERT INTO plano (nome_plano,preco_mensal,duracao_meses,descricao) VALUES (@n,@p,@d,@desc)"
                        : "UPDATE plano SET nome_plano=@n,preco_mensal=@p,duracao_meses=@d,descricao=@desc WHERE plano_id=@id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@n",    txtNome.Text.Trim());
                        cmd.Parameters.AddWithValue("@p",    preco);
                        cmd.Parameters.AddWithValue("@d",    dur);
                        cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(txtDescricao.Text)
                            ? (object)DBNull.Value : txtDescricao.Text.Trim());
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
            if (MessageBox.Show("Eliminar este plano?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM plano WHERE plano_id=@id", conn))
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
            dgvPlanos.Enabled   = !editing;
        }
    }
}
