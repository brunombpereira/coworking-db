using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcReservas : UserControl
    {
        private DataGridView dgv;
        private Button btnNova, btnCancelar;
        private DateTimePicker dtpFiltroDE, dtpFiltroAte;
        private ComboBox cmbFiltroCliente, cmbFiltroEstado;
        private int _selectedId = -1;
        private string _selectedEstado = "";

        public UcReservas()
        {
            this.BackColor = Theme.PageBg;
            this.Dock = DockStyle.Fill;
            BuildUI();
            LoadFiltroClientes();
            LoadData();
        }

        private void BuildUI()
        {
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.PageBg, Padding = new Padding(20, 14, 20, 0) };
            pnlTitle.Controls.Add(new Label { Text = "Reservas", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill, AutoSize = false });

            var pnlToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();
            btnNova = Theme.BtnPrim("+ Nova");
            btnCancelar = Theme.BtnRed("Cancelar reserva");
            btnCancelar.Width = 160;
            btnCancelar.Enabled = false;
            btnNova.Click += (s, e) => OpenEditor();
            btnCancelar.Click += BtnCancelar_Click;

            // Filter row
            dtpFiltroDE = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Margin = new Padding(8, 4, 0, 0), Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            dtpFiltroAte = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short, Margin = new Padding(4, 4, 0, 0), Value = DateTime.Today.AddMonths(2) };
            cmbFiltroCliente = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 4, 0, 0), BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary };
            cmbFiltroEstado = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 4, 0, 0), BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary };
            cmbFiltroEstado.Items.AddRange(new object[] { "(Todos)", "Pendente", "Confirmada", "Cancelada", "Concluida" });
            cmbFiltroEstado.SelectedIndex = 0;

            dtpFiltroDE.ValueChanged += (s, e) => LoadData();
            dtpFiltroAte.ValueChanged += (s, e) => LoadData();
            cmbFiltroCliente.SelectedIndexChanged += (s, e) => LoadData();
            cmbFiltroEstado.SelectedIndexChanged += (s, e) => LoadData();

            flow.Controls.Add(btnNova);
            flow.Controls.Add(btnCancelar);
            flow.Controls.Add(new Label { Text = "  De:", Font = Theme.FontLabel, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(8, 10, 0, 0) });
            flow.Controls.Add(dtpFiltroDE);
            flow.Controls.Add(new Label { Text = " até:", Font = Theme.FontLabel, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(2, 10, 0, 0) });
            flow.Controls.Add(dtpFiltroAte);
            flow.Controls.Add(cmbFiltroCliente);
            flow.Controls.Add(cmbFiltroEstado);
            pnlToolbar.Controls.Add(flow);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) { _selectedId = -1; btnCancelar.Enabled = false; return; }
                _selectedId = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);
                _selectedEstado = dgv.SelectedRows[0].Cells["Estado"].Value?.ToString() ?? "";
                btnCancelar.Enabled = (_selectedEstado != "Cancelada" && _selectedEstado != "Concluida");
            };
            dgv.CellFormatting += (s, e) =>
            {
                if (dgv.Columns.Count == 0 || e.Value == null) return;
                Theme.ApplyStatusColor(e, "Estado", dgv);
                if (dgv.Columns[e.ColumnIndex].Name == "Valor")
                {
                    if (decimal.TryParse(e.Value.ToString(), out decimal val))
                    {
                        e.Value = Theme.FormatEuro(val);
                        e.FormattingApplied = true;
                    }
                }
            };

            this.Controls.Add(dgv);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlTitle);
        }

        private void LoadFiltroClientes()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    var rowTodos = dt.NewRow();
                    rowTodos["cliente_id"] = DBNull.Value;
                    rowTodos["nome"] = "(Todos)";
                    dt.Rows.InsertAt(rowTodos, 0);
                    cmbFiltroCliente.DataSource = dt;
                    cmbFiltroCliente.DisplayMember = "nome";
                    cmbFiltroCliente.ValueMember = "cliente_id";
                    cmbFiltroCliente.SelectedIndex = 0;
                }
            }
            catch (SqlException) { /* ignore */ }
        }

        private void LoadData()
        {
            if (dtpFiltroDE != null && dtpFiltroAte != null && dtpFiltroDE.Value.Date > dtpFiltroAte.Value.Date) return;
            try
            {
                var whereParts = new List<string> { "r.data_reserva BETWEEN @de AND @ate" };
                string estadoFiltro = cmbFiltroEstado?.SelectedIndex > 0 ? cmbFiltroEstado.Text : null;
                if (estadoFiltro != null) whereParts.Add("r.estado = @e");
                bool filtraCliente = cmbFiltroCliente?.SelectedIndex > 0
                    && cmbFiltroCliente.SelectedValue != null
                    && !(cmbFiltroCliente.SelectedValue is DBNull);
                if (filtraCliente) whereParts.Add("r.cliente_id = @c");

                string sql = $@"
                    SELECT r.reserva_id AS ID, c.nome AS Cliente,
                           CASE WHEN s.recurso_id IS NOT NULL THEN 'Sala ' + s.nome ELSE 'Posto ' + p.codigo END AS Recurso,
                           rc.tipo AS Tipo,
                           CONVERT(varchar,r.data_reserva,103) AS Data,
                           CONVERT(varchar,r.hora_inicio,108) AS [H.Início],
                           CONVERT(varchar,r.hora_fim,108) AS [H.Fim],
                           r.valor AS Valor, r.estado AS Estado,
                           r.num_participantes AS [Nº Pess.]
                    FROM reserva r
                    JOIN cliente c ON r.cliente_id=c.cliente_id
                    JOIN recurso rc ON r.recurso_id=rc.recurso_id
                    LEFT JOIN sala s ON rc.recurso_id=s.recurso_id
                    LEFT JOIN posto p ON rc.recurso_id=p.recurso_id
                    WHERE {string.Join(" AND ", whereParts)}
                    ORDER BY r.data_reserva DESC, r.hora_inicio";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@de", dtpFiltroDE.Value.Date);
                    cmd.Parameters.AddWithValue("@ate", dtpFiltroAte.Value.Date);
                    if (estadoFiltro != null) cmd.Parameters.AddWithValue("@e", estadoFiltro);
                    if (filtraCliente) cmd.Parameters.AddWithValue("@c", Convert.ToInt32(cmbFiltroCliente.SelectedValue));
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        dgv.DataSource = dt;
                        if (dgv.Columns.Contains("ID")) dgv.Columns["ID"].Visible = false;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEditor()
        {
            // Carregar combos data
            DataTable dsClientes, dsRecursos;
            using (var conn = Database.GetConnection())
            {
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    dsClientes = new DataTable();
                    adapter.Fill(dsClientes);
                }
                const string recSql = @"
                    SELECT recurso_id, label, tipo, preco FROM (
                        SELECT s.recurso_id,
                               e.nome + ' / Sala ' + s.nome AS label,
                               'Sala' AS tipo, s.preco_hora AS preco
                        FROM sala s JOIN espaco e ON s.espaco_id=e.espaco_id
                        WHERE s.estado='Disponivel'
                        UNION ALL
                        SELECT p.recurso_id,
                               e.nome + ' / Posto ' + p.codigo + ' (' + p.tipo_posto + ')' AS label,
                               'Posto' AS tipo, p.preco_dia AS preco
                        FROM posto p JOIN espaco e ON p.espaco_id=e.espaco_id
                        WHERE p.estado='Disponivel'
                    ) x ORDER BY label";
                using (var cmd = new SqlCommand(recSql, conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    dsRecursos = new DataTable();
                    adapter.Fill(dsRecursos);
                }
            }

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var cmbCliente = UcClientes.AddComboDataSource(tbl, "Cliente *", dsClientes, "nome", "cliente_id");
            var cmbRecurso = UcClientes.AddComboDataSource(tbl, "Recurso *", dsRecursos, "label", "recurso_id");
            var dtData     = UcClientes.AddDate(tbl, "Data *");
            var txtHIni    = UcClientes.AddField(tbl, "Hora início (HH:MM)");
            var txtHFim    = UcClientes.AddField(tbl, "Hora fim (HH:MM)");
            var txtParts   = UcClientes.AddField(tbl, "Nº Participantes");
            var txtNotas   = UcClientes.AddField(tbl, "Notas");
            var lblValor   = new Label { Text = "Valor: —", Font = Theme.FontSection, ForeColor = Theme.TextPrimary, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 8, 0, 8) };
            tbl.Controls.Add(lblValor);

            txtHIni.Text = "09:00";
            txtHFim.Text = "10:00";
            dtData.Value = DateTime.Today;

            decimal valorCalc = 0;
            string tipoSel = "";

            // Helper to recalc valor
            Action recalc = () =>
            {
                if (cmbRecurso.SelectedValue == null || cmbRecurso.SelectedValue is DBNull) { lblValor.Text = "Valor: —"; valorCalc = 0; return; }
                var rowView = (DataRowView)cmbRecurso.SelectedItem;
                tipoSel = rowView["tipo"].ToString();
                decimal preco = Convert.ToDecimal(rowView["preco"]);
                if (tipoSel == "Sala")
                {
                    txtHIni.Parent.Visible = true;
                    txtHFim.Parent.Visible = true;
                    txtParts.Parent.Visible = true;
                    if (TimeSpan.TryParse(txtHIni.Text.Trim(), out TimeSpan hi) &&
                        TimeSpan.TryParse(txtHFim.Text.Trim(), out TimeSpan hf) &&
                        hf > hi)
                    {
                        valorCalc = (decimal)(hf - hi).TotalHours * preco;
                    }
                    else valorCalc = 0;
                }
                else // Posto
                {
                    txtHIni.Parent.Visible = false;
                    txtHFim.Parent.Visible = false;
                    txtParts.Parent.Visible = false;
                    valorCalc = preco;
                }
                lblValor.Text = "Valor: " + Theme.FormatEuro(valorCalc);
            };

            cmbRecurso.SelectedIndexChanged += (s, e) => recalc();
            txtHIni.TextChanged += (s, e) => recalc();
            txtHFim.TextChanged += (s, e) => recalc();
            recalc();

            using (var dlg = new FormDialog("Nova Reserva", tbl, 460, () =>
            {
                if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull) throw new ApplicationException("Cliente é obrigatório.");
                if (cmbRecurso.SelectedValue == null || cmbRecurso.SelectedValue is DBNull) throw new ApplicationException("Recurso é obrigatório.");
                if (dtData.Value.Date < DateTime.Today) throw new ApplicationException("Data não pode ser no passado.");

                int recId = Convert.ToInt32(cmbRecurso.SelectedValue);
                int cliId = Convert.ToInt32(cmbCliente.SelectedValue);
                object pHIni = DBNull.Value, pHFim = DBNull.Value, pParts = DBNull.Value;
                decimal valor;

                if (tipoSel == "Sala")
                {
                    if (!TimeSpan.TryParse(txtHIni.Text.Trim(), out TimeSpan hi)) throw new ApplicationException("Hora início inválida (HH:MM).");
                    if (!TimeSpan.TryParse(txtHFim.Text.Trim(), out TimeSpan hf)) throw new ApplicationException("Hora fim inválida (HH:MM).");
                    if (hf <= hi) throw new ApplicationException("Hora fim deve ser depois de hora início.");
                    pHIni = hi;
                    pHFim = hf;
                    if (!string.IsNullOrWhiteSpace(txtParts.Text))
                    {
                        if (!int.TryParse(txtParts.Text, out int np) || np <= 0) throw new ApplicationException("Nº Participantes inválido.");
                        pParts = np;
                    }
                    var rowView = (DataRowView)cmbRecurso.SelectedItem;
                    decimal preco = Convert.ToDecimal(rowView["preco"]);
                    valor = (decimal)(hf - hi).TotalHours * preco;
                }
                else
                {
                    var rowView = (DataRowView)cmbRecurso.SelectedItem;
                    valor = Convert.ToDecimal(rowView["preco"]);
                }

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes, notas)
                      VALUES (@c,@r,@d,@hi,@hf,@v,'Pendente',@np,@n)", conn))
                {
                    cmd.Parameters.AddWithValue("@c", cliId);
                    cmd.Parameters.AddWithValue("@r", recId);
                    cmd.Parameters.AddWithValue("@d", dtData.Value.Date);
                    cmd.Parameters.AddWithValue("@hi", pHIni);
                    cmd.Parameters.AddWithValue("@hf", pHFim);
                    cmd.Parameters.AddWithValue("@v", valor);
                    cmd.Parameters.AddWithValue("@np", pParts);
                    cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(txtNotas.Text) ? (object)DBNull.Value : txtNotas.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            if (_selectedId < 0) return;
            if (_selectedEstado == "Cancelada" || _selectedEstado == "Concluida")
            {
                MessageBox.Show("Esta reserva não pode ser cancelada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Cancelar a reserva selecionada?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("UPDATE reserva SET estado='Cancelada' WHERE reserva_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _selectedId);
                    cmd.ExecuteNonQuery();
                }
                LoadData();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(Database.SqlErrorMessage(ex), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
