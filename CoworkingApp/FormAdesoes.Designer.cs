namespace CoworkingApp
{
    partial class FormAdesoes
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvAdesoes   = new System.Windows.Forms.DataGridView();
            this.pnlDetalhe   = new System.Windows.Forms.Panel();
            this.cmbCliente   = new System.Windows.Forms.ComboBox();
            this.cmbPlano     = new System.Windows.Forms.ComboBox();
            this.dtpDataInicio= new System.Windows.Forms.DateTimePicker();
            this.cmbEstado    = new System.Windows.Forms.ComboBox();
            this.lblDataFim   = new System.Windows.Forms.Label();
            this.btnNovo      = new System.Windows.Forms.Button();
            this.btnGuardar   = new System.Windows.Forms.Button();
            this.btnEliminar  = new System.Windows.Forms.Button();
            this.btnCancelar  = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.dgvAdesoes)).BeginInit();
            this.pnlDetalhe.SuspendLayout();
            this.SuspendLayout();

            // dgvAdesoes
            this.dgvAdesoes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAdesoes.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvAdesoes.ReadOnly = true;
            this.dgvAdesoes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAdesoes.MultiSelect = false;
            this.dgvAdesoes.AllowUserToAddRows = false;

            // pnlDetalhe
            this.pnlDetalhe.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlDetalhe.Height = 200;
            this.pnlDetalhe.Padding = new System.Windows.Forms.Padding(10);

            var tbl = new System.Windows.Forms.TableLayoutPanel();
            tbl.ColumnCount = 4;
            tbl.RowCount = 3;
            tbl.Dock = System.Windows.Forms.DockStyle.Top;
            tbl.Height = 130;
            tbl.Padding = new System.Windows.Forms.Padding(5);

            var lblCliente = new System.Windows.Forms.Label();
            lblCliente.Text = "Cliente:"; lblCliente.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbCliente.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbCliente.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            var lblPlano = new System.Windows.Forms.Label();
            lblPlano.Text = "Plano:"; lblPlano.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbPlano.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbPlano.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            var lblDataInicio = new System.Windows.Forms.Label();
            lblDataInicio.Text = "Data Início:"; lblDataInicio.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.dtpDataInicio.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.dtpDataInicio.Format = System.Windows.Forms.DateTimePickerFormat.Short;

            var lblEstado = new System.Windows.Forms.Label();
            lblEstado.Text = "Estado:"; lblEstado.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbEstado.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            this.lblDataFim.Text = "Data fim calculada: —";
            this.lblDataFim.Anchor = System.Windows.Forms.AnchorStyles.Left;

            tbl.Controls.Add(lblCliente,         0, 0);
            tbl.Controls.Add(this.cmbCliente,    1, 0);
            tbl.Controls.Add(lblPlano,           2, 0);
            tbl.Controls.Add(this.cmbPlano,      3, 0);
            tbl.Controls.Add(lblDataInicio,      0, 1);
            tbl.Controls.Add(this.dtpDataInicio, 1, 1);
            tbl.Controls.Add(lblEstado,          2, 1);
            tbl.Controls.Add(this.cmbEstado,     3, 1);
            tbl.Controls.Add(this.lblDataFim,    0, 2);
            tbl.SetColumnSpan(this.lblDataFim, 4);
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));

            var pnlBtns = new System.Windows.Forms.FlowLayoutPanel();
            pnlBtns.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlBtns.Height = 40;
            pnlBtns.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;

            this.btnNovo.Text     = "Novo";     this.btnNovo.Width = 80;
            this.btnGuardar.Text  = "Guardar";  this.btnGuardar.Width = 80;
            this.btnEliminar.Text = "Eliminar"; this.btnEliminar.Width = 80;
            this.btnCancelar.Text = "Cancelar"; this.btnCancelar.Width = 80;

            this.btnNovo.Click     += new System.EventHandler(this.btnNovo_Click);
            this.btnGuardar.Click  += new System.EventHandler(this.btnGuardar_Click);
            this.btnEliminar.Click += new System.EventHandler(this.btnEliminar_Click);
            this.btnCancelar.Click += new System.EventHandler(this.btnCancelar_Click);

            pnlBtns.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.btnNovo, this.btnGuardar, this.btnEliminar, this.btnCancelar });

            this.pnlDetalhe.Controls.Add(tbl);
            this.pnlDetalhe.Controls.Add(pnlBtns);

            // FormAdesoes
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 500);
            this.Controls.Add(this.dgvAdesoes);
            this.Controls.Add(this.pnlDetalhe);
            this.Name = "FormAdesoes";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)(this.dgvAdesoes)).EndInit();
            this.pnlDetalhe.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.DataGridView dgvAdesoes;
        private System.Windows.Forms.Panel pnlDetalhe;
        private System.Windows.Forms.ComboBox cmbCliente, cmbPlano, cmbEstado;
        private System.Windows.Forms.DateTimePicker dtpDataInicio;
        private System.Windows.Forms.Label lblDataFim;
        private System.Windows.Forms.Button btnNovo, btnGuardar, btnEliminar, btnCancelar;
    }
}
