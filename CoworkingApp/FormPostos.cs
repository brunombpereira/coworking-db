using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormPostos : Form
    {
        private readonly int _espacoId;
        private int _selectedId = -1;

        public FormPostos(int espacoId, string espacoNome)
        {
            InitializeComponent();
            _espacoId = espacoId;
            this.Text = "Postos de Trabalho — " + espacoNome;
            cmbTipo.Items.AddRange(new object[] { "Flex", "Fixo", "Privado" });
            cmbTipo.SelectedIndex = 0;
            cmbEstado.Items.AddRange(new object[] { "Disponivel", "Indisponivel", "Manutencao", "Inativo" });
            cmbEstado.SelectedIndex = 0;
            dgvPostos.SelectionChanged += DgvPostos_SelectionChanged;
            SetEditMode(false);
            LoadData();
        }

        private void LoadData()
        {
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(
                "SELECT posto_id AS ID, codigo AS Código, tipo AS Tipo, " +
                "preco_hora AS [Preço/Hora], estado AS Estado " +
                "FROM posto_trabalho WHERE espaco_id=@eid ORDER BY codigo", conn))
            {
                cmd.Parameters.AddWithValue("@eid", _espacoId);
                var dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
                dgvPostos.DataSource = dt;
                if (dgvPostos.Columns.Contains("ID")) dgvPostos.Columns["ID"].Visible = false;
            }
        }

        private void DgvPostos_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPostos.SelectedRows.Count == 0) return;
            var row = dgvPostos.SelectedRows[0];
            _selectedId       = Convert.ToInt32(row.Cells["ID"].Value);
            txtCodigo.Text    = row.Cells["Código"].Value?.ToString();
            cmbTipo.Text      = row.Cells["Tipo"].Value?.ToString();
            txtPrecoHora.Text = row.Cells["Preço/Hora"].Value?.ToString();
            cmbEstado.Text    = row.Cells["Estado"].Value?.ToString();
        }

        private void btnNovo_Click(object sender, EventArgs e)
        {
            _selectedId = -1;
            txtCodigo.Text = txtPrecoHora.Text = "";
            cmbTipo.SelectedIndex = cmbEstado.SelectedIndex = 0;
            SetEditMode(true);
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) ||
                !decimal.TryParse(txtPrecoHora.Text, out var preco) || preco < 0)
            {
                MessageBox.Show("Código obrigatório. Preço deve ser um número válido.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var conn = Database.GetConnection())
                {
                    string sql = _selectedId == -1
                        ? "INSERT INTO posto_trabalho (codigo,tipo,preco_hora,estado,espaco_id) VALUES (@c,@t,@p,@e,@eid)"
                        : "UPDATE posto_trabalho SET codigo=@c,tipo=@t,preco_hora=@p,estado=@e WHERE posto_id=@id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@c",   txtCodigo.Text.Trim());
                        cmd.Parameters.AddWithValue("@t",   cmbTipo.Text);
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
            if (MessageBox.Show("Eliminar este posto?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM posto_trabalho WHERE posto_id=@id", conn))
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
            dgvPostos.Enabled   = !editing;
        }
    }
}
