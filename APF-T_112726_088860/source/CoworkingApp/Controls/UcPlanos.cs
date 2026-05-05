using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcPlanos : UserControl
    {
        private DataGridView dgv;
        private Panel pnlForm;
        private Button btnNovo, btnEditar, btnEliminar, btnGuardar, btnCancelar;
        private TextBox txtNome, txtPreco, txtDuracao, txtDescricao;
        private ComboBox cmbTipoPlano;
        private int _editId = -1;
        private Label _lblStatus;

        public UcPlanos()
        {
            this.BackColor = Theme.ContentBg;
            this.Dock = DockStyle.Fill;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            // ── Title panel ──────────────────────────────────────────────────
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.ContentBg,
                Padding = new Padding(20, 12, 0, 0)
            };
            var lblTitle = new Label
            {
                Text = "Planos",
                Font = Theme.FontTitle,
                ForeColor = ColorTranslator.FromHtml("#0c4a6e"),
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            pnlTitle.Controls.Add(lblTitle);

            // ── Toolbar ──────────────────────────────────────────────────────
            var pnlToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();

            btnNovo     = Theme.BtnPrim("+ Novo");
            btnEditar   = Theme.BtnGray("Editar");
            btnEliminar = Theme.BtnRed("Eliminar");
            btnGuardar  = Theme.BtnPrim("Guardar");
            btnCancelar = Theme.BtnGray("Cancelar");

            btnEditar.Enabled   = false;
            btnEliminar.Enabled = false;
            btnGuardar.Visible  = false;
            btnCancelar.Visible = false;

            btnNovo.Click     += BtnNovo_Click;
            btnEditar.Click   += BtnEditar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnGuardar.Click  += BtnGuardar_Click;
            btnCancelar.Click += BtnCancelar_Click;

            _lblStatus = new Label { AutoSize = true, Visible = false, Margin = new Padding(16, 8, 0, 0), Font = Theme.FontSub };
            flow.Controls.Add(btnNovo);
            flow.Controls.Add(btnEditar);
            flow.Controls.Add(btnEliminar);
            flow.Controls.Add(btnGuardar);
            flow.Controls.Add(btnCancelar);
            flow.Controls.Add(_lblStatus);
            pnlToolbar.Controls.Add(flow);

            // ── Grid ─────────────────────────────────────────────────────────
            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);
            dgv.SelectionChanged  += Dgv_SelectionChanged;
            dgv.CellFormatting    += Dgv_CellFormatting;

            // ── Form panel ───────────────────────────────────────────────────
            pnlForm = Theme.FormPanel(164);

            // 4-column TableLayoutPanel for fields
            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 3,
                AutoSize    = false
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));

            // Row 0: Nome, Preço/Mês, Duração, Descrição
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Nome *"),            txtNome      = Theme.Field()), 0, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Preço/Mês *"),       txtPreco     = Theme.Field()), 1, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Duração (meses) *"), txtDuracao   = Theme.Field()), 2, 0);
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Descrição"),         txtDescricao = Theme.Field()), 3, 0);

            // Row 1: Tipo
            cmbTipoPlano = Theme.Combo();
            cmbTipoPlano.Items.AddRange(new object[] { "Flex", "Fixo", "Privado" });
            cmbTipoPlano.SelectedIndex = 0;
            tbl.Controls.Add(MakeFieldCell(Theme.FieldLabel("Tipo *"), cmbTipoPlano), 0, 1);

            // Row 2: buttons FlowPanel spans all 4 columns
            var flowBtns = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Padding       = new Padding(0, 4, 0, 0)
            };
            var btnSave   = Theme.BtnPrim("Guardar");
            var btnCancel = Theme.BtnGray("Cancelar");
            btnSave.Click   += BtnGuardar_Click;
            btnCancel.Click += BtnCancelar_Click;
            flowBtns.Controls.Add(btnSave);
            flowBtns.Controls.Add(btnCancel);
            tbl.SetColumnSpan(flowBtns, 4);
            tbl.Controls.Add(flowBtns, 0, 2);

            pnlForm.Controls.Add(tbl);

            // ── Add to UserControl (CRITICAL ORDER) ──────────────────────────
            this.Controls.Add(pnlForm);
            this.Controls.Add(dgv);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlTitle);
        }

        private Panel MakeFieldCell(Label lbl, Control field)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0) };
            // Dock=Top: field is added first so it appears below the label
            p.Controls.Add(field);
            p.Controls.Add(lbl);
            return p;
        }

        private void SetEditMode(bool editing)
        {
            btnNovo.Visible     = btnEditar.Visible     = btnEliminar.Visible = !editing;
            btnGuardar.Visible  = btnCancelar.Visible   = editing;
            dgv.Enabled         = !editing;
            pnlForm.Visible     = editing;
            if (!editing)
                btnEditar.Enabled = btnEliminar.Enabled = dgv.SelectedRows.Count > 0;
        }

        private void LoadData()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT plano_id AS ID, nome_plano AS Nome, tipo_plano AS Tipo,
                             preco_mensal AS [Preço/Mês],
                             duracao_meses AS [Duração (meses)],
                             descricao AS Descrição
                      FROM plano
                      ORDER BY nome_plano", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgv.DataSource = dt;

                    if (dgv.Columns.Contains("ID"))
                        dgv.Columns["ID"].Visible = false;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) return;
            var row = dgv.SelectedRows[0];
            _editId = Convert.ToInt32(row.Cells["ID"].Value);

            txtNome.Text      = row.Cells["Nome"].Value?.ToString() ?? "";
            txtPreco.Text     = row.Cells["Preço/Mês"].Value?.ToString() ?? "";
            txtDuracao.Text   = row.Cells["Duração (meses)"].Value?.ToString() ?? "";
            txtDescricao.Text = row.Cells["Descrição"].Value?.ToString() ?? "";

            var tipo = row.Cells["Tipo"].Value?.ToString() ?? "Flex";
            var idx  = cmbTipoPlano.Items.IndexOf(tipo);
            cmbTipoPlano.SelectedIndex = idx >= 0 ? idx : 0;

            btnEditar.Enabled   = true;
            btnEliminar.Enabled = true;
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns.Count == 0 || e.Value == null) return;
            if (dgv.Columns[e.ColumnIndex].Name == "Preço/Mês")
            {
                if (decimal.TryParse(e.Value.ToString(), out decimal val))
                {
                    e.Value = Theme.FormatEuro(val);
                    e.FormattingApplied = true;
                }
            }
        }

        private void BtnNovo_Click(object sender, EventArgs e)
        {
            _editId = -1;
            txtNome.Text      = "";
            txtPreco.Text     = "";
            txtDuracao.Text   = "";
            txtDescricao.Text = "";
            cmbTipoPlano.SelectedIndex = 0;  // Flex
            SetEditMode(true);
            txtNome.Focus();
        }

        private void BtnEditar_Click(object sender, EventArgs e)
        {
            if (_editId < 0) return;
            SetEditMode(true);
            txtNome.Focus();
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (_editId < 0) return;
            var res = MessageBox.Show("Eliminar o plano selecionado?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) return;

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM plano WHERE plano_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _editId);
                    cmd.ExecuteNonQuery();
                }
                _editId = -1;
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            // ── Validation ───────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Nome é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtPreco.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal preco) || preco < 0)
            {
                MessageBox.Show("Preço inválido.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(txtDuracao.Text, out int dur) || dur <= 0)
            {
                MessageBox.Show("Duração inválida.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbTipoPlano.SelectedItem == null)
            {
                MessageBox.Show("Tipo é obrigatório.", "Validação",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            object descricaoVal = string.IsNullOrWhiteSpace(txtDescricao.Text)
                ? (object)DBNull.Value
                : txtDescricao.Text.Trim();

            try
            {
                using (var conn = Database.GetConnection())
                {
                    SqlCommand cmd;
                    if (_editId < 0)
                    {
                        cmd = new SqlCommand(
                            "INSERT INTO plano (nome_plano,tipo_plano,preco_mensal,duracao_meses,descricao) " +
                            "VALUES (@n,@tp,@p,@d,@desc)", conn);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            "UPDATE plano SET nome_plano=@n,tipo_plano=@tp,preco_mensal=@p," +
                            "duracao_meses=@d,descricao=@desc WHERE plano_id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", _editId);
                    }

                    cmd.Parameters.AddWithValue("@n",    txtNome.Text.Trim());
                    cmd.Parameters.AddWithValue("@tp",   cmbTipoPlano.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@p",    preco);
                    cmd.Parameters.AddWithValue("@d",    dur);
                    cmd.Parameters.AddWithValue("@desc", descricaoVal);

                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }

                Theme.ShowSuccess(_lblStatus);
                SetEditMode(false);
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            SetEditMode(false);
        }
    }
}
