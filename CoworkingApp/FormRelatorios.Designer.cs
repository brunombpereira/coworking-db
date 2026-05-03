namespace CoworkingApp
{
    partial class FormRelatorios
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabRelatorios      = new System.Windows.Forms.TabControl();
            this.tabDisponibilidade = new System.Windows.Forms.TabPage();
            this.tabHistCliente     = new System.Windows.Forms.TabPage();
            this.tabHistPag         = new System.Windows.Forms.TabPage();
            this.tabOcupacao        = new System.Windows.Forms.TabPage();
            this.tabReceita         = new System.Windows.Forms.TabPage();

            // Disponibilidade controls
            this.rbDispSala         = new System.Windows.Forms.RadioButton();
            this.rbDispPosto        = new System.Windows.Forms.RadioButton();
            this.dtpDisp            = new System.Windows.Forms.DateTimePicker();
            this.dtpDispHI          = new System.Windows.Forms.DateTimePicker();
            this.dtpDispHF          = new System.Windows.Forms.DateTimePicker();
            this.btnPesquisarDisp   = new System.Windows.Forms.Button();
            this.dgvDisponibilidade = new System.Windows.Forms.DataGridView();

            // Hist Cliente controls
            this.cmbClienteHist     = new System.Windows.Forms.ComboBox();
            this.btnPesquisarHist   = new System.Windows.Forms.Button();
            this.dgvHistCliente     = new System.Windows.Forms.DataGridView();

            // Hist Pagamentos controls
            this.cmbClientePag      = new System.Windows.Forms.ComboBox();
            this.btnPesquisarPag    = new System.Windows.Forms.Button();
            this.dgvHistPag         = new System.Windows.Forms.DataGridView();

            // Ocupacao controls
            this.dtpOcupIni         = new System.Windows.Forms.DateTimePicker();
            this.dtpOcupFim         = new System.Windows.Forms.DateTimePicker();
            this.btnPesquisarOcup   = new System.Windows.Forms.Button();
            this.dgvOcupacao        = new System.Windows.Forms.DataGridView();

            // Receita controls
            this.dtpRecIni          = new System.Windows.Forms.DateTimePicker();
            this.dtpRecFim          = new System.Windows.Forms.DateTimePicker();
            this.btnPesquisarRec    = new System.Windows.Forms.Button();
            this.dgvReceita         = new System.Windows.Forms.DataGridView();
            this.lblTotalReceita    = new System.Windows.Forms.Label();

            this.tabRelatorios.SuspendLayout();
            this.tabDisponibilidade.SuspendLayout();
            this.tabHistCliente.SuspendLayout();
            this.tabHistPag.SuspendLayout();
            this.tabOcupacao.SuspendLayout();
            this.tabReceita.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDisponibilidade)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistCliente)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistPag)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOcupacao)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReceita)).BeginInit();
            this.SuspendLayout();

            // ===================== TabControl =====================
            this.tabRelatorios.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabRelatorios.TabPages.AddRange(new System.Windows.Forms.TabPage[] {
                this.tabDisponibilidade,
                this.tabHistCliente,
                this.tabHistPag,
                this.tabOcupacao,
                this.tabReceita });

            // ===================== Tab 1: Disponibilidade =====================
            this.tabDisponibilidade.Text = "Disponibilidade";

            var pnlDispFilter = new System.Windows.Forms.Panel();
            pnlDispFilter.Dock = System.Windows.Forms.DockStyle.Top;
            pnlDispFilter.Height = 50;

            var flowDisp = new System.Windows.Forms.FlowLayoutPanel();
            flowDisp.Dock = System.Windows.Forms.DockStyle.Fill;
            flowDisp.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowDisp.Padding = new System.Windows.Forms.Padding(5);

            this.rbDispSala.Text    = "Sala";  this.rbDispSala.Checked = true;
            this.rbDispSala.Anchor  = System.Windows.Forms.AnchorStyles.None;
            this.rbDispSala.Width   = 60;
            this.rbDispSala.Height  = 23;
            this.rbDispPosto.Text   = "Posto";
            this.rbDispPosto.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbDispPosto.Width  = 60;
            this.rbDispPosto.Height = 23;

            this.dtpDisp.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDisp.Width  = 100;

            var lblDispHI = new System.Windows.Forms.Label();
            lblDispHI.Text = "Das:"; lblDispHI.Width = 30; lblDispHI.Height = 23;
            lblDispHI.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpDispHI.Format     = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpDispHI.ShowUpDown = true;
            this.dtpDispHI.Width      = 80;
            this.dtpDispHI.Value      = new System.DateTime(2000, 1, 1, 8, 0, 0);

            var lblDispHF = new System.Windows.Forms.Label();
            lblDispHF.Text = "Às:"; lblDispHF.Width = 25; lblDispHF.Height = 23;
            lblDispHF.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpDispHF.Format     = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpDispHF.ShowUpDown = true;
            this.dtpDispHF.Width      = 80;
            this.dtpDispHF.Value      = new System.DateTime(2000, 1, 1, 20, 0, 0);

            this.btnPesquisarDisp.Text  = "Pesquisar"; this.btnPesquisarDisp.Width = 90;
            this.btnPesquisarDisp.Click += new System.EventHandler(this.btnPesquisarDisp_Click);

            flowDisp.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.rbDispSala, this.rbDispPosto,
                this.dtpDisp,
                lblDispHI, this.dtpDispHI,
                lblDispHF, this.dtpDispHF,
                this.btnPesquisarDisp });

            pnlDispFilter.Controls.Add(flowDisp);

            this.dgvDisponibilidade.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDisponibilidade.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDisponibilidade.ReadOnly = true;
            this.dgvDisponibilidade.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvDisponibilidade.MultiSelect = false;
            this.dgvDisponibilidade.AllowUserToAddRows = false;

            this.tabDisponibilidade.Controls.Add(this.dgvDisponibilidade);
            this.tabDisponibilidade.Controls.Add(pnlDispFilter);

            // ===================== Tab 2: Histórico Cliente =====================
            this.tabHistCliente.Text = "Histórico Cliente";

            var pnlHistFilter = new System.Windows.Forms.Panel();
            pnlHistFilter.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHistFilter.Height = 40;

            var flowHist = new System.Windows.Forms.FlowLayoutPanel();
            flowHist.Dock = System.Windows.Forms.DockStyle.Fill;
            flowHist.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowHist.Padding = new System.Windows.Forms.Padding(5);

            var lblClienteHist = new System.Windows.Forms.Label();
            lblClienteHist.Text = "Cliente:"; lblClienteHist.Width = 55; lblClienteHist.Height = 23;
            lblClienteHist.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbClienteHist.Width = 200;
            this.cmbClienteHist.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.btnPesquisarHist.Text  = "Pesquisar"; this.btnPesquisarHist.Width = 90;
            this.btnPesquisarHist.Click += new System.EventHandler(this.btnPesquisarHist_Click);

            flowHist.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblClienteHist, this.cmbClienteHist, this.btnPesquisarHist });

            pnlHistFilter.Controls.Add(flowHist);

            this.dgvHistCliente.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistCliente.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistCliente.ReadOnly = true;
            this.dgvHistCliente.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistCliente.MultiSelect = false;
            this.dgvHistCliente.AllowUserToAddRows = false;

            this.tabHistCliente.Controls.Add(this.dgvHistCliente);
            this.tabHistCliente.Controls.Add(pnlHistFilter);

            // ===================== Tab 3: Histórico Pagamentos =====================
            this.tabHistPag.Text = "Histórico Pagamentos";

            var pnlHistPagFilter = new System.Windows.Forms.Panel();
            pnlHistPagFilter.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHistPagFilter.Height = 40;

            var flowHistPag = new System.Windows.Forms.FlowLayoutPanel();
            flowHistPag.Dock = System.Windows.Forms.DockStyle.Fill;
            flowHistPag.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowHistPag.Padding = new System.Windows.Forms.Padding(5);

            var lblClientePag = new System.Windows.Forms.Label();
            lblClientePag.Text = "Cliente:"; lblClientePag.Width = 55; lblClientePag.Height = 23;
            lblClientePag.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbClientePag.Width = 200;
            this.cmbClientePag.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.btnPesquisarPag.Text  = "Pesquisar"; this.btnPesquisarPag.Width = 90;
            this.btnPesquisarPag.Click += new System.EventHandler(this.btnPesquisarPag_Click);

            flowHistPag.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblClientePag, this.cmbClientePag, this.btnPesquisarPag });

            pnlHistPagFilter.Controls.Add(flowHistPag);

            this.dgvHistPag.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHistPag.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvHistPag.ReadOnly = true;
            this.dgvHistPag.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvHistPag.MultiSelect = false;
            this.dgvHistPag.AllowUserToAddRows = false;

            this.tabHistPag.Controls.Add(this.dgvHistPag);
            this.tabHistPag.Controls.Add(pnlHistPagFilter);

            // ===================== Tab 4: Ocupação =====================
            this.tabOcupacao.Text = "Ocupação Espaços";

            var pnlOcupFilter = new System.Windows.Forms.Panel();
            pnlOcupFilter.Dock = System.Windows.Forms.DockStyle.Top;
            pnlOcupFilter.Height = 40;

            var flowOcup = new System.Windows.Forms.FlowLayoutPanel();
            flowOcup.Dock = System.Windows.Forms.DockStyle.Fill;
            flowOcup.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowOcup.Padding = new System.Windows.Forms.Padding(5);

            var lblOcupDe = new System.Windows.Forms.Label();
            lblOcupDe.Text = "De:"; lblOcupDe.Width = 25; lblOcupDe.Height = 23;
            lblOcupDe.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpOcupIni.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpOcupIni.Width  = 100;
            var lblOcupAte = new System.Windows.Forms.Label();
            lblOcupAte.Text = "Até:"; lblOcupAte.Width = 30; lblOcupAte.Height = 23;
            lblOcupAte.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpOcupFim.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpOcupFim.Width  = 100;
            this.btnPesquisarOcup.Text  = "Pesquisar"; this.btnPesquisarOcup.Width = 90;
            this.btnPesquisarOcup.Click += new System.EventHandler(this.btnPesquisarOcup_Click);

            flowOcup.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblOcupDe, this.dtpOcupIni,
                lblOcupAte, this.dtpOcupFim,
                this.btnPesquisarOcup });

            pnlOcupFilter.Controls.Add(flowOcup);

            this.dgvOcupacao.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOcupacao.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvOcupacao.ReadOnly = true;
            this.dgvOcupacao.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvOcupacao.MultiSelect = false;
            this.dgvOcupacao.AllowUserToAddRows = false;

            this.tabOcupacao.Controls.Add(this.dgvOcupacao);
            this.tabOcupacao.Controls.Add(pnlOcupFilter);

            // ===================== Tab 5: Receita =====================
            this.tabReceita.Text = "Receita";

            var pnlRecFilter = new System.Windows.Forms.Panel();
            pnlRecFilter.Dock = System.Windows.Forms.DockStyle.Top;
            pnlRecFilter.Height = 40;

            var flowRec = new System.Windows.Forms.FlowLayoutPanel();
            flowRec.Dock = System.Windows.Forms.DockStyle.Fill;
            flowRec.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowRec.Padding = new System.Windows.Forms.Padding(5);

            var lblRecDe = new System.Windows.Forms.Label();
            lblRecDe.Text = "De:"; lblRecDe.Width = 25; lblRecDe.Height = 23;
            lblRecDe.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpRecIni.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpRecIni.Width  = 100;
            var lblRecAte = new System.Windows.Forms.Label();
            lblRecAte.Text = "Até:"; lblRecAte.Width = 30; lblRecAte.Height = 23;
            lblRecAte.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpRecFim.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpRecFim.Width  = 100;
            this.btnPesquisarRec.Text  = "Pesquisar"; this.btnPesquisarRec.Width = 90;
            this.btnPesquisarRec.Click += new System.EventHandler(this.btnPesquisarRec_Click);

            flowRec.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblRecDe, this.dtpRecIni,
                lblRecAte, this.dtpRecFim,
                this.btnPesquisarRec });

            pnlRecFilter.Controls.Add(flowRec);

            var pnlRecBottom = new System.Windows.Forms.Panel();
            pnlRecBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlRecBottom.Height = 30;

            this.lblTotalReceita.Text = "Total: —";
            this.lblTotalReceita.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalReceita.Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 10f, System.Drawing.FontStyle.Bold);
            this.lblTotalReceita.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTotalReceita.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);

            pnlRecBottom.Controls.Add(this.lblTotalReceita);

            this.dgvReceita.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvReceita.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvReceita.ReadOnly = true;
            this.dgvReceita.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvReceita.MultiSelect = false;
            this.dgvReceita.AllowUserToAddRows = false;

            this.tabReceita.Controls.Add(this.dgvReceita);
            this.tabReceita.Controls.Add(pnlRecBottom);
            this.tabReceita.Controls.Add(pnlRecFilter);

            // ===================== FormRelatorios =====================
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.tabRelatorios);
            this.Name = "FormRelatorios";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.tabRelatorios.ResumeLayout(false);
            this.tabDisponibilidade.ResumeLayout(false);
            this.tabHistCliente.ResumeLayout(false);
            this.tabHistPag.ResumeLayout(false);
            this.tabOcupacao.ResumeLayout(false);
            this.tabReceita.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDisponibilidade)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistCliente)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHistPag)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOcupacao)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReceita)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TabControl tabRelatorios;
        private System.Windows.Forms.TabPage tabDisponibilidade, tabHistCliente, tabHistPag, tabOcupacao, tabReceita;

        // Tab 1 — Disponibilidade
        private System.Windows.Forms.RadioButton rbDispSala, rbDispPosto;
        private System.Windows.Forms.DateTimePicker dtpDisp, dtpDispHI, dtpDispHF;
        private System.Windows.Forms.Button btnPesquisarDisp;
        private System.Windows.Forms.DataGridView dgvDisponibilidade;

        // Tab 2 — Histórico Cliente
        private System.Windows.Forms.ComboBox cmbClienteHist;
        private System.Windows.Forms.Button btnPesquisarHist;
        private System.Windows.Forms.DataGridView dgvHistCliente;

        // Tab 3 — Histórico Pagamentos
        private System.Windows.Forms.ComboBox cmbClientePag;
        private System.Windows.Forms.Button btnPesquisarPag;
        private System.Windows.Forms.DataGridView dgvHistPag;

        // Tab 4 — Ocupação
        private System.Windows.Forms.DateTimePicker dtpOcupIni, dtpOcupFim;
        private System.Windows.Forms.Button btnPesquisarOcup;
        private System.Windows.Forms.DataGridView dgvOcupacao;

        // Tab 5 — Receita
        private System.Windows.Forms.DateTimePicker dtpRecIni, dtpRecFim;
        private System.Windows.Forms.Button btnPesquisarRec;
        private System.Windows.Forms.DataGridView dgvReceita;
        private System.Windows.Forms.Label lblTotalReceita;
    }
}
