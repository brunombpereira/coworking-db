using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp.Controls
{
    public class UcRelatorios : UserControl
    {
        // Tab 1
        private RadioButton rbSala;
        private RadioButton rbPosto;
        private DateTimePicker dtpDisp;
        private DateTimePicker dtpDispHI;
        private DateTimePicker dtpDispHF;
        private Button btnPesquisarDisp;
        private DataGridView dgvDisponibilidade;

        // Tab 2
        private ComboBox cmbClienteHist;
        private Button btnPesquisarHist;
        private DataGridView dgvHistCliente;

        // Tab 3
        private ComboBox cmbClientePag;
        private Button btnPesquisarPag;
        private DataGridView dgvHistPag;

        // Tab 4
        private DateTimePicker dtpOcupIni;
        private DateTimePicker dtpOcupFim;
        private Button btnPesquisarOcup;
        private DataGridView dgvOcupacao;
        private DateTimePicker dtpRecIni;
        private DateTimePicker dtpRecFim;
        private Button btnPesquisarRec;
        private Label lblTotalReceita;
        private DataGridView dgvReceita;

        public UcRelatorios()
        {
            InitUI();
            LoadClientesCombos();
        }

        private void InitUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Theme.ContentBg;

            // --- pnlTitle (Dock=Top) ---
            var pnlTitle = new Panel();
            pnlTitle.Dock = DockStyle.Top;
            pnlTitle.Height = 56;
            pnlTitle.BackColor = Theme.ContentBg;

            var lblTitle = new Label();
            lblTitle.Text = "Relatórios";
            lblTitle.Font = Theme.FontTitle;
            lblTitle.ForeColor = Color.FromArgb(0x0c, 0x4a, 0x6e);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(18, 14);
            pnlTitle.Controls.Add(lblTitle);

            // --- tabControl (Dock=Fill) ---
            var tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = Theme.FontBase;

            tabControl.TabPages.Add(BuildTabDisponibilidade());
            tabControl.TabPages.Add(BuildTabHistorialCliente());
            tabControl.TabPages.Add(BuildTabHistorialPagamentos());
            tabControl.TabPages.Add(BuildTabOcupacaoReceita());

            // Add order: Fill first, then Top panels (title last = visually topmost)
            this.Controls.Add(tabControl);   // Dock=Fill
            this.Controls.Add(pnlTitle);     // Dock=Top — added last = visually topmost
        }

        // ─── Tab 1: Disponibilidade ─────────────────────────────────────────────

        private TabPage BuildTabDisponibilidade()
        {
            var tab = new TabPage("Disponibilidade");
            tab.BackColor = Color.White;

            // FlowLayoutPanel (Dock=Top)
            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 56;
            flw.BackColor = Color.White;
            flw.Padding = new Padding(10, 8, 0, 8);
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;

            rbSala = new RadioButton();
            rbSala.Text = "Sala";
            rbSala.Checked = true;
            rbSala.AutoSize = true;
            rbSala.Margin = new Padding(0, 4, 8, 0);

            rbPosto = new RadioButton();
            rbPosto.Text = "Posto";
            rbPosto.AutoSize = true;
            rbPosto.Margin = new Padding(0, 4, 12, 0);

            var lblData = new Label();
            lblData.Text = "Data:";
            lblData.AutoSize = true;
            lblData.Margin = new Padding(0, 7, 4, 0);

            dtpDisp = new DateTimePicker();
            dtpDisp.Format = DateTimePickerFormat.Short;
            dtpDisp.Width = 100;
            dtpDisp.Margin = new Padding(0, 3, 12, 0);

            var lblHI = new Label();
            lblHI.Text = "H.Início:";
            lblHI.AutoSize = true;
            lblHI.Margin = new Padding(0, 7, 4, 0);

            dtpDispHI = new DateTimePicker();
            dtpDispHI.Format = DateTimePickerFormat.Time;
            dtpDispHI.ShowUpDown = true;
            dtpDispHI.Width = 80;
            dtpDispHI.Value = DateTime.Today.AddHours(9);
            dtpDispHI.Margin = new Padding(0, 3, 12, 0);

            var lblHF = new Label();
            lblHF.Text = "H.Fim:";
            lblHF.AutoSize = true;
            lblHF.Margin = new Padding(0, 7, 4, 0);

            dtpDispHF = new DateTimePicker();
            dtpDispHF.Format = DateTimePickerFormat.Time;
            dtpDispHF.ShowUpDown = true;
            dtpDispHF.Width = 80;
            dtpDispHF.Value = DateTime.Today.AddHours(10);
            dtpDispHF.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarDisp = Theme.BtnPrim("Pesquisar");
            btnPesquisarDisp.Margin = new Padding(0, 3, 0, 0);
            btnPesquisarDisp.Click += BtnPesquisarDisp_Click;

            flw.Controls.Add(rbSala);
            flw.Controls.Add(rbPosto);
            flw.Controls.Add(lblData);
            flw.Controls.Add(dtpDisp);
            flw.Controls.Add(lblHI);
            flw.Controls.Add(dtpDispHI);
            flw.Controls.Add(lblHF);
            flw.Controls.Add(dtpDispHF);
            flw.Controls.Add(btnPesquisarDisp);

            dgvDisponibilidade = new DataGridView();
            dgvDisponibilidade.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvDisponibilidade);
            dgvDisponibilidade.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && dgvDisponibilidade.Columns[e.ColumnIndex].HeaderText == "€/Hora" &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            tab.Controls.Add(dgvDisponibilidade);  // Fill
            tab.Controls.Add(flw);                 // Top — added last = visually topmost
            return tab;
        }

        private void BtnPesquisarDisp_Click(object sender, EventArgs e)
        {
            string hi = dtpDispHI.Value.TimeOfDay.ToString(@"hh\:mm");
            string hf = dtpDispHF.Value.TimeOfDay.ToString(@"hh\:mm");
            DateTime data = dtpDisp.Value.Date;

            string sql;
            if (rbSala.Checked)
            {
                sql = @"
SELECT s.sala_id AS ID, e.nome AS Espaço, s.nome AS Sala,
  s.capacidade AS Capacidade, s.preco_hora AS [€/Hora]
FROM sala s JOIN espaco e ON s.espaco_id=e.espaco_id
WHERE s.estado='Disponivel'
AND NOT EXISTS (
  SELECT 1 FROM reserva r WHERE r.sala_id=s.sala_id
  AND r.data_reserva=@d AND r.estado<>'Cancelada'
  AND r.hora_inicio < @hf AND r.hora_fim > @hi)
ORDER BY e.nome, s.nome";
            }
            else
            {
                sql = @"
SELECT p.posto_id AS ID, e.nome AS Espaço, p.codigo AS Código,
  p.tipo AS Tipo, p.preco_hora AS [€/Hora]
FROM posto_trabalho p JOIN espaco e ON p.espaco_id=e.espaco_id
WHERE p.estado='Disponivel'
AND NOT EXISTS (
  SELECT 1 FROM reserva r WHERE r.posto_id=p.posto_id
  AND r.data_reserva=@d AND r.estado<>'Cancelada'
  AND r.hora_inicio < @hf AND r.hora_fim > @hi)
ORDER BY e.nome, p.codigo";
            }

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@d", data);
                    cmd.Parameters.AddWithValue("@hi", hi);
                    cmd.Parameters.AddWithValue("@hf", hf);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        dgvDisponibilidade.DataSource = dt;
                    }
                }

                if (dgvDisponibilidade.Columns.Contains("ID"))
                    dgvDisponibilidade.Columns["ID"].Visible = false;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Tab 2: Historial por Cliente ───────────────────────────────────────

        private TabPage BuildTabHistorialCliente()
        {
            var tab = new TabPage("Historial por Cliente");
            tab.BackColor = Color.White;

            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 50;
            flw.BackColor = Color.White;
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;
            flw.Padding = new Padding(10, 8, 0, 8);

            var lblCliente = new Label();
            lblCliente.Text = "Cliente:";
            lblCliente.AutoSize = true;
            lblCliente.Margin = new Padding(0, 7, 4, 0);

            cmbClienteHist = new ComboBox();
            cmbClienteHist.Width = 220;
            cmbClienteHist.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbClienteHist.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarHist = Theme.BtnPrim("Pesquisar");
            btnPesquisarHist.Margin = new Padding(0, 3, 0, 0);
            btnPesquisarHist.Click += BtnPesquisarHist_Click;

            flw.Controls.Add(lblCliente);
            flw.Controls.Add(cmbClienteHist);
            flw.Controls.Add(btnPesquisarHist);

            dgvHistCliente = new DataGridView();
            dgvHistCliente.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvHistCliente);
            dgvHistCliente.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && dgvHistCliente.Columns[e.ColumnIndex].HeaderText == "Valor" &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            tab.Controls.Add(dgvHistCliente);  // Fill
            tab.Controls.Add(flw);             // Top — added last = visually topmost
            return tab;
        }

        private void BtnPesquisarHist_Click(object sender, EventArgs e)
        {
            if (cmbClienteHist.SelectedValue == null) return;
            int clienteId = Convert.ToInt32(cmbClienteHist.SelectedValue);

            string sql = @"
SELECT ISNULL(s.nome, p.codigo) AS Recurso,
  CASE WHEN r.sala_id IS NOT NULL THEN 'Sala' ELSE 'Posto' END AS Tipo,
  CONVERT(varchar,r.data_reserva,103) AS Data,
  CONVERT(varchar,r.hora_inicio,108)+'-'+CONVERT(varchar,r.hora_fim,108) AS Horário,
  r.valor AS Valor, r.estado AS Estado
FROM reserva r
LEFT JOIN sala s ON r.sala_id=s.sala_id
LEFT JOIN posto_trabalho p ON r.posto_id=p.posto_id
WHERE r.cliente_id=@c ORDER BY r.data_reserva DESC";

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@c", clienteId);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        dgvHistCliente.DataSource = dt;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Tab 3: Historial de Pagamentos ─────────────────────────────────────

        private TabPage BuildTabHistorialPagamentos()
        {
            var tab = new TabPage("Historial de Pagamentos");
            tab.BackColor = Color.White;

            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 50;
            flw.BackColor = Color.White;
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;
            flw.Padding = new Padding(10, 8, 0, 8);

            var lblCliente = new Label();
            lblCliente.Text = "Cliente:";
            lblCliente.AutoSize = true;
            lblCliente.Margin = new Padding(0, 7, 4, 0);

            cmbClientePag = new ComboBox();
            cmbClientePag.Width = 220;
            cmbClientePag.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbClientePag.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarPag = Theme.BtnPrim("Pesquisar");
            btnPesquisarPag.Margin = new Padding(0, 3, 0, 0);
            btnPesquisarPag.Click += BtnPesquisarPag_Click;

            flw.Controls.Add(lblCliente);
            flw.Controls.Add(cmbClientePag);
            flw.Controls.Add(btnPesquisarPag);

            dgvHistPag = new DataGridView();
            dgvHistPag.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvHistPag);
            dgvHistPag.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && dgvHistPag.Columns[e.ColumnIndex].HeaderText == "Valor" &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            tab.Controls.Add(dgvHistPag);  // Fill
            tab.Controls.Add(flw);         // Top — added last = visually topmost
            return tab;
        }

        private void BtnPesquisarPag_Click(object sender, EventArgs e)
        {
            if (cmbClientePag.SelectedValue == null) return;
            int clienteId = Convert.ToInt32(cmbClientePag.SelectedValue);

            string sql = @"
SELECT CONVERT(varchar,p.data_pagamento,103) AS Data,
  p.valor AS Valor, p.metodo_pagamento AS Método, p.estado AS Estado,
  CASE WHEN p.reserva_id IS NOT NULL THEN 'Reserva #'+CAST(p.reserva_id AS varchar)
       ELSE 'Adesão #'+CAST(p.adesao_id AS varchar) END AS Referência
FROM pagamento p WHERE p.cliente_id=@c ORDER BY p.data_pagamento DESC";

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@c", clienteId);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        dgvHistPag.DataSource = dt;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Tab 4: Ocupação e Receita ──────────────────────────────────────────

        private TabPage BuildTabOcupacaoReceita()
        {
            var tab = new TabPage("Ocupação e Receita");
            tab.BackColor = Color.White;

            // Sub-section B (Dock=Fill) — added first so A (Dock=Top) sits above it
            var pnlReceita = new Panel();
            pnlReceita.Dock = DockStyle.Fill;
            pnlReceita.BackColor = Color.White;
            BuildReceitaPanel(pnlReceita);

            // Sub-section A (Dock=Top, Height=200)
            var pnlOcupacao = new Panel();
            pnlOcupacao.Dock = DockStyle.Top;
            pnlOcupacao.Height = 200;
            pnlOcupacao.BackColor = Color.White;
            BuildOcupacaoPanel(pnlOcupacao);

            tab.Controls.Add(pnlReceita);   // Fill — added first
            tab.Controls.Add(pnlOcupacao);  // Top — added last = visually topmost
            return tab;
        }

        private void BuildOcupacaoPanel(Panel pnl)
        {
            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 50;
            flw.BackColor = Color.White;
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;
            flw.Padding = new Padding(10, 8, 0, 8);

            var lblDe = new Label();
            lblDe.Text = "De:";
            lblDe.AutoSize = true;
            lblDe.Margin = new Padding(0, 7, 4, 0);

            dtpOcupIni = new DateTimePicker();
            dtpOcupIni.Format = DateTimePickerFormat.Short;
            dtpOcupIni.Width = 100;
            dtpOcupIni.Value = DateTime.Today.AddDays(-30);
            dtpOcupIni.Margin = new Padding(0, 3, 12, 0);

            var lblAte = new Label();
            lblAte.Text = "Até:";
            lblAte.AutoSize = true;
            lblAte.Margin = new Padding(0, 7, 4, 0);

            dtpOcupFim = new DateTimePicker();
            dtpOcupFim.Format = DateTimePickerFormat.Short;
            dtpOcupFim.Width = 100;
            dtpOcupFim.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarOcup = Theme.BtnPrim("Ver Ocupação");
            btnPesquisarOcup.Margin = new Padding(0, 3, 0, 0);
            btnPesquisarOcup.Click += BtnPesquisarOcup_Click;

            flw.Controls.Add(lblDe);
            flw.Controls.Add(dtpOcupIni);
            flw.Controls.Add(lblAte);
            flw.Controls.Add(dtpOcupFim);
            flw.Controls.Add(btnPesquisarOcup);

            var lblOcupHeader = new Label();
            lblOcupHeader.Text = "Ocupação por Espaço";
            lblOcupHeader.Dock = DockStyle.Top;
            lblOcupHeader.Height = 24;
            lblOcupHeader.Padding = new Padding(10, 4, 0, 0);
            lblOcupHeader.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            dgvOcupacao = new DataGridView();
            dgvOcupacao.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvOcupacao);

            pnl.Controls.Add(dgvOcupacao);      // Fill
            pnl.Controls.Add(lblOcupHeader);     // Top
            pnl.Controls.Add(flw);              // Top — added last = visually topmost
        }

        private void BuildReceitaPanel(Panel pnl)
        {
            var flw = new FlowLayoutPanel();
            flw.Dock = DockStyle.Top;
            flw.Height = 50;
            flw.BackColor = Color.White;
            flw.WrapContents = false;
            flw.FlowDirection = FlowDirection.LeftToRight;
            flw.Padding = new Padding(10, 8, 0, 8);

            var lblDe = new Label();
            lblDe.Text = "De:";
            lblDe.AutoSize = true;
            lblDe.Margin = new Padding(0, 7, 4, 0);

            dtpRecIni = new DateTimePicker();
            dtpRecIni.Format = DateTimePickerFormat.Short;
            dtpRecIni.Width = 100;
            dtpRecIni.Value = DateTime.Today.AddDays(-30);
            dtpRecIni.Margin = new Padding(0, 3, 12, 0);

            var lblAte = new Label();
            lblAte.Text = "Até:";
            lblAte.AutoSize = true;
            lblAte.Margin = new Padding(0, 7, 4, 0);

            dtpRecFim = new DateTimePicker();
            dtpRecFim.Format = DateTimePickerFormat.Short;
            dtpRecFim.Width = 100;
            dtpRecFim.Margin = new Padding(0, 3, 12, 0);

            btnPesquisarRec = Theme.BtnPrim("Ver Receita");
            btnPesquisarRec.Margin = new Padding(0, 3, 12, 0);
            btnPesquisarRec.Click += BtnPesquisarRec_Click;

            lblTotalReceita = new Label();
            lblTotalReceita.AutoSize = true;
            lblTotalReceita.ForeColor = Color.FromArgb(0x0c, 0x4a, 0x6e);
            lblTotalReceita.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblTotalReceita.Margin = new Padding(8, 7, 0, 0);

            flw.Controls.Add(lblDe);
            flw.Controls.Add(dtpRecIni);
            flw.Controls.Add(lblAte);
            flw.Controls.Add(dtpRecFim);
            flw.Controls.Add(btnPesquisarRec);
            flw.Controls.Add(lblTotalReceita);

            var lblRecHeader = new Label();
            lblRecHeader.Text = "Receita por Método de Pagamento";
            lblRecHeader.Dock = DockStyle.Top;
            lblRecHeader.Height = 24;
            lblRecHeader.Padding = new Padding(10, 4, 0, 0);
            lblRecHeader.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            dgvReceita = new DataGridView();
            dgvReceita.Dock = DockStyle.Fill;
            Theme.StyleGrid(dgvReceita);
            dgvReceita.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && dgvReceita.Columns[e.ColumnIndex].HeaderText == "Total" &&
                    e.Value != null && e.Value != DBNull.Value)
                {
                    try { e.Value = Theme.FormatEuro(Convert.ToDecimal(e.Value)); e.FormattingApplied = true; }
                    catch { }
                }
            };

            pnl.Controls.Add(dgvReceita);      // Fill
            pnl.Controls.Add(lblRecHeader);    // Top
            pnl.Controls.Add(flw);             // Top — added last = visually topmost
        }

        private void BtnPesquisarOcup_Click(object sender, EventArgs e)
        {
            string sql = @"
SELECT e.nome AS Espaço,
  COUNT(DISTINCT s.sala_id) AS Salas,
  COUNT(DISTINCT p.posto_id) AS Postos,
  SUM(CASE WHEN r.sala_id IS NOT NULL AND r.estado<>'Cancelada'
           AND r.data_reserva BETWEEN @ini AND @fim THEN 1 ELSE 0 END) AS [Reservas Sala],
  SUM(CASE WHEN r.posto_id IS NOT NULL AND r.estado<>'Cancelada'
           AND r.data_reserva BETWEEN @ini AND @fim THEN 1 ELSE 0 END) AS [Reservas Posto]
FROM espaco e
LEFT JOIN sala s ON e.espaco_id=s.espaco_id
LEFT JOIN posto_trabalho p ON e.espaco_id=p.espaco_id
LEFT JOIN reserva r ON (r.sala_id=s.sala_id OR r.posto_id=p.posto_id)
GROUP BY e.espaco_id, e.nome ORDER BY e.nome";

            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ini", dtpOcupIni.Value.Date);
                    cmd.Parameters.AddWithValue("@fim", dtpOcupFim.Value.Date);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        dgvOcupacao.DataSource = dt;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPesquisarRec_Click(object sender, EventArgs e)
        {
            string sqlReceita = @"
SELECT metodo_pagamento AS Método, COUNT(*) AS [Nº Pagamentos], SUM(valor) AS Total
FROM pagamento
WHERE estado='Pago' AND data_pagamento BETWEEN @ini AND @fim
GROUP BY metodo_pagamento ORDER BY Total DESC";

            string sqlTotal = @"
SELECT ISNULL(SUM(valor),0) FROM pagamento WHERE estado='Pago' AND data_pagamento BETWEEN @ini AND @fim";

            try
            {
                using (var conn = Database.GetConnection())
                {
                    using (var cmd = new SqlCommand(sqlReceita, conn))
                    {
                        cmd.Parameters.AddWithValue("@ini", dtpRecIni.Value.Date);
                        cmd.Parameters.AddWithValue("@fim", dtpRecFim.Value.Date);
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);
                            dgvReceita.DataSource = dt;
                        }
                    }

                    using (var cmdTotal = new SqlCommand(sqlTotal, conn))
                    {
                        cmdTotal.Parameters.AddWithValue("@ini", dtpRecIni.Value.Date);
                        cmdTotal.Parameters.AddWithValue("@fim", dtpRecFim.Value.Date);
                        decimal total = (decimal)cmdTotal.ExecuteScalar();
                        lblTotalReceita.Text = "Total: " + Theme.FormatEuro(total);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro BD", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── Shared helpers ─────────────────────────────────────────────────────

        private void LoadClientesCombos()
        {
            try
            {
                using (var conn = Database.GetConnection())
                using (var cmd = new SqlCommand("SELECT cliente_id, nome FROM cliente ORDER BY nome", conn))
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cmbClienteHist.DisplayMember = "nome";
                    cmbClienteHist.ValueMember = "cliente_id";
                    cmbClienteHist.DataSource = dt.Copy();

                    cmbClientePag.DisplayMember = "nome";
                    cmbClientePag.ValueMember = "cliente_id";
                    cmbClientePag.DataSource = dt.Copy();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao carregar clientes: " + ex.Message, "Erro BD",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
