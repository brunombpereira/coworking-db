using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcPagamentos : UserControl
    {
        private DataGridView dgv;
        private Button btnNovo, btnEditar, btnEliminar;
        private ComboBox cmbFiltroCliente, cmbFiltroEstado;
        private int _selectedId = -1;

        public UcPagamentos()
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
            pnlTitle.Controls.Add(new Label { Text = "Pagamentos", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill, AutoSize = false });

            var pnlToolbar = Theme.Toolbar();
            var flow = Theme.ToolbarFlow();
            btnNovo = Theme.BtnPrim("+ Novo");
            btnEditar = Theme.BtnGray("Editar");
            btnEliminar = Theme.BtnRed("Eliminar");
            btnEditar.Enabled = false;
            btnEliminar.Enabled = false;
            btnNovo.Click += (s, e) => OpenEditor(null);
            btnEditar.Click += (s, e) => OpenEditor(_selectedId);
            btnEliminar.Click += BtnEliminar_Click;

            cmbFiltroCliente = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 4, 0, 0), BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary };
            cmbFiltroEstado = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 4, 0, 0), BackColor = Theme.FieldBg, ForeColor = Theme.TextPrimary };
            cmbFiltroEstado.Items.AddRange(new object[] { "(Todos)", "Pendente", "Pago", "Cancelado", "Reembolsado" });
            cmbFiltroEstado.SelectedIndex = 0;
            cmbFiltroCliente.SelectedIndexChanged += (s, e) => LoadData();
            cmbFiltroEstado.SelectedIndexChanged += (s, e) => LoadData();

            flow.Controls.Add(btnNovo);
            flow.Controls.Add(btnEditar);
            flow.Controls.Add(btnEliminar);
            flow.Controls.Add(cmbFiltroCliente);
            flow.Controls.Add(cmbFiltroEstado);
            pnlToolbar.Controls.Add(flow);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgv);
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) { _selectedId = -1; btnEditar.Enabled = btnEliminar.Enabled = false; return; }
                _selectedId = Convert.ToInt32(dgv.SelectedRows[0].Cells["ID"].Value);
                btnEditar.Enabled = btnEliminar.Enabled = true;
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
            try
            {
                var whereParts = new System.Collections.Generic.List<string>();
                string estadoFiltro = cmbFiltroEstado?.SelectedIndex > 0 ? cmbFiltroEstado.Text : null;
                if (estadoFiltro != null) whereParts.Add("pg.estado = @e");
                bool filtraCliente = cmbFiltroCliente?.SelectedIndex > 0
                    && cmbFiltroCliente.SelectedValue != null
                    && !(cmbFiltroCliente.SelectedValue is DBNull);
                if (filtraCliente) whereParts.Add("pg.cliente_id = @c");
                string where = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                string sql = $@"
                    SELECT pg.pagamento_id AS ID, c.nome AS Cliente,
                           CASE
                               WHEN pg.adesao_id IS NOT NULL THEN 'Adesão #' + CAST(pg.adesao_id AS varchar) + ' (' + pl.nome_plano + ')'
                               ELSE 'Reserva #' + CAST(pg.reserva_id AS varchar)
                           END AS Serviço,
                           CONVERT(varchar,pg.data_pagamento,103) AS Data,
                           pg.valor AS Valor, pg.metodo_pagamento AS Método, pg.estado AS Estado
                    FROM pagamento pg
                    JOIN cliente c ON pg.cliente_id=c.cliente_id
                    LEFT JOIN adesao a ON pg.adesao_id=a.adesao_id
                    LEFT JOIN plano pl ON a.plano_id=pl.plano_id
                    {where}
                    ORDER BY pg.data_pagamento DESC, pg.pagamento_id DESC";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
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

        private DataTable LoadServicosPorCliente(int clienteId)
        {
            // Adesões + reservas pendentes do cliente, formato "A:1" / "R:4"
            const string sql = @"
                SELECT 'A:' + CAST(a.adesao_id AS varchar) AS value,
                       'Adesão #' + CAST(a.adesao_id AS varchar) + ' — ' + pl.nome_plano +
                            ' (' + FORMAT(a.preco_acordado, '0.00') + ' €)' AS label,
                       a.preco_acordado AS preco
                FROM adesao a JOIN plano pl ON a.plano_id=pl.plano_id
                WHERE a.cliente_id=@c
                UNION ALL
                SELECT 'R:' + CAST(r.reserva_id AS varchar),
                       'Reserva #' + CAST(r.reserva_id AS varchar) + ' — ' +
                            CONVERT(varchar,r.data_reserva,103) +
                            ' (' + FORMAT(r.valor, '0.00') + ' €)',
                       r.valor
                FROM reserva r WHERE r.cliente_id=@c
                ORDER BY value";

            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@c", clienteId);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        private void OpenEditor(int? id)
        {
            DataTable dsClientes;
            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
            using (var adapter = new SqlDataAdapter(cmd))
            {
                dsClientes = new DataTable();
                adapter.Fill(dsClientes);
            }

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var cmbCliente = UcClientes.AddComboDataSource(tbl, "Cliente *", dsClientes, "nome", "cliente_id");
            var cmbServico = UcClientes.AddCombo(tbl, "Serviço *", new string[0]);
            cmbServico.DisplayMember = "label";
            cmbServico.ValueMember = "value";
            var dtData     = UcClientes.AddDate(tbl, "Data pagamento *");
            var txtValor   = UcClientes.AddField(tbl, "Valor *");
            var cmbMetodo  = UcClientes.AddCombo(tbl, "Método *", new[] { "Dinheiro", "Cartao", "Transferencia", "MBWay", "PayPal" });
            var cmbEstado  = UcClientes.AddCombo(tbl, "Estado *", new[] { "Pendente", "Pago", "Cancelado", "Reembolsado" });

            cmbCliente.SelectedIndexChanged += (s, e) =>
            {
                if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull) { cmbServico.DataSource = null; return; }
                cmbServico.DataSource = LoadServicosPorCliente(Convert.ToInt32(cmbCliente.SelectedValue));
            };
            cmbServico.SelectedIndexChanged += (s, e) =>
            {
                if (cmbServico.SelectedItem is DataRowView rv)
                {
                    txtValor.Text = Convert.ToDecimal(rv["preco"]).ToString(CultureInfo.InvariantCulture);
                }
            };

            // Pre-load se editing
            if (id.HasValue)
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(
                    @"SELECT cliente_id, adesao_id, reserva_id, data_pagamento, valor, metodo_pagamento, estado
                      FROM pagamento WHERE pagamento_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            cmbCliente.SelectedValue = r["cliente_id"];
                            // SelectedIndexChanged carregou serviços; agora seleciona o correto
                            string svcKey = r["adesao_id"] != DBNull.Value
                                ? "A:" + Convert.ToInt32(r["adesao_id"])
                                : "R:" + Convert.ToInt32(r["reserva_id"]);
                            cmbServico.SelectedValue = svcKey;
                            dtData.Value = Convert.ToDateTime(r["data_pagamento"]);
                            txtValor.Text = Convert.ToDecimal(r["valor"]).ToString(CultureInfo.InvariantCulture);
                            var met = r["metodo_pagamento"].ToString();
                            var idxM = cmbMetodo.Items.IndexOf(met);
                            cmbMetodo.SelectedIndex = idxM >= 0 ? idxM : 0;
                            var est = r["estado"].ToString();
                            var idxE = cmbEstado.Items.IndexOf(est);
                            cmbEstado.SelectedIndex = idxE >= 0 ? idxE : 0;
                        }
                    }
                }
            }
            else
            {
                cmbMetodo.SelectedIndex = 0;
                cmbEstado.SelectedIndex = 1; // Pago default
                if (cmbCliente.Items.Count > 0)
                {
                    cmbCliente.SelectedIndex = -1;
                    cmbCliente.SelectedIndex = 0;
                }
            }

            using (var dlg = new FormDialog(id.HasValue ? "Editar Pagamento" : "Novo Pagamento", tbl, 460, () =>
            {
                if (cmbCliente.SelectedValue == null || cmbCliente.SelectedValue is DBNull) throw new ApplicationException("Cliente é obrigatório.");
                if (cmbServico.SelectedValue == null) throw new ApplicationException("Serviço é obrigatório.");
                if (!decimal.TryParse(txtValor.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) || valor <= 0)
                    throw new ApplicationException("Valor inválido (> 0).");

                string svc = cmbServico.SelectedValue.ToString();
                object adesaoVal = DBNull.Value, reservaVal = DBNull.Value;
                if (svc.StartsWith("A:")) adesaoVal = int.Parse(svc.Substring(2));
                else if (svc.StartsWith("R:")) reservaVal = int.Parse(svc.Substring(2));

                // preco_servico_snapshot lido sempre do serviço — o trigger T6
                // garante que coincide; o CHECK ck_pagamento_valor_snapshot
                // rejeita @v se não bater certo com o preço actual.
                const string snapshotExpr = @"COALESCE(
                        (SELECT preco_acordado FROM adesao  WHERE adesao_id  = @a),
                        (SELECT valor          FROM reserva WHERE reserva_id = @r))";
                var sql = id.HasValue
                    ? $@"UPDATE pagamento
                            SET cliente_id=@c, adesao_id=@a, reserva_id=@r,
                                data_pagamento=@d, valor=@v,
                                preco_servico_snapshot={snapshotExpr},
                                metodo_pagamento=@m, estado=@e
                          WHERE pagamento_id=@id"
                    : $@"INSERT INTO pagamento
                            (cliente_id, adesao_id, reserva_id, data_pagamento,
                             valor, preco_servico_snapshot, metodo_pagamento, estado)
                          VALUES (@c,@a,@r,@d,@v,{snapshotExpr},@m,@e)";

                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value);
                    cmd.Parameters.AddWithValue("@c", Convert.ToInt32(cmbCliente.SelectedValue));
                    cmd.Parameters.AddWithValue("@a", adesaoVal);
                    cmd.Parameters.AddWithValue("@r", reservaVal);
                    cmd.Parameters.AddWithValue("@d", dtData.Value.Date);
                    cmd.Parameters.AddWithValue("@v", valor);
                    cmd.Parameters.AddWithValue("@m", cmbMetodo.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@e", cmbEstado.SelectedItem.ToString());
                    cmd.ExecuteNonQuery();
                }
            }))
            {
                if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK) LoadData();
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (_selectedId < 0) return;
            if (MessageBox.Show("Eliminar pagamento?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("DELETE FROM pagamento WHERE pagamento_id=@id", conn))
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
