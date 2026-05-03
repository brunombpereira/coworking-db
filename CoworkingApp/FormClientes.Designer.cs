namespace CoworkingApp
{
    partial class FormClientes
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvClientes  = new System.Windows.Forms.DataGridView();
            this.pnlDetalhe   = new System.Windows.Forms.Panel();
            this.lblNome      = new System.Windows.Forms.Label();
            this.txtNome      = new System.Windows.Forms.TextBox();
            this.lblNif       = new System.Windows.Forms.Label();
            this.txtNif       = new System.Windows.Forms.TextBox();
            this.lblEmail     = new System.Windows.Forms.Label();
            this.txtEmail     = new System.Windows.Forms.TextBox();
            this.lblTelefone  = new System.Windows.Forms.Label();
            this.txtTelefone  = new System.Windows.Forms.TextBox();
            this.btnNovo      = new System.Windows.Forms.Button();
            this.btnGuardar   = new System.Windows.Forms.Button();
            this.btnEliminar  = new System.Windows.Forms.Button();
            this.btnCancelar  = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.dgvClientes)).BeginInit();
            this.pnlDetalhe.SuspendLayout();
            this.SuspendLayout();

            // dgvClientes
            this.dgvClientes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvClientes.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvClientes.ReadOnly = true;
            this.dgvClientes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvClientes.MultiSelect = false;
            this.dgvClientes.AllowUserToAddRows = false;

            // pnlDetalhe
            this.pnlDetalhe.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlDetalhe.Height = 170;
            this.pnlDetalhe.Padding = new System.Windows.Forms.Padding(10);

            var tbl = new System.Windows.Forms.TableLayoutPanel();
            tbl.ColumnCount = 4;
            tbl.RowCount = 2;
            tbl.Dock = System.Windows.Forms.DockStyle.Top;
            tbl.Height = 100;
            tbl.Padding = new System.Windows.Forms.Padding(5);

            this.lblNome.Text = "Nome:"; this.lblNome.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtNome.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblNif.Text = "NIF:"; this.lblNif.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtNif.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblEmail.Text = "Email:"; this.lblEmail.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtEmail.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblTelefone.Text = "Telefone:"; this.lblTelefone.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtTelefone.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            tbl.Controls.Add(this.lblNome,     0, 0);
            tbl.Controls.Add(this.txtNome,     1, 0);
            tbl.Controls.Add(this.lblNif,      2, 0);
            tbl.Controls.Add(this.txtNif,      3, 0);
            tbl.Controls.Add(this.lblEmail,    0, 1);
            tbl.Controls.Add(this.txtEmail,    1, 1);
            tbl.Controls.Add(this.lblTelefone, 2, 1);
            tbl.Controls.Add(this.txtTelefone, 3, 1);
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

            // FormClientes
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 500);
            this.Controls.Add(this.dgvClientes);
            this.Controls.Add(this.pnlDetalhe);
            this.Name = "FormClientes";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)(this.dgvClientes)).EndInit();
            this.pnlDetalhe.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.DataGridView dgvClientes;
        private System.Windows.Forms.Panel pnlDetalhe;
        private System.Windows.Forms.Label lblNome, lblNif, lblEmail, lblTelefone;
        private System.Windows.Forms.TextBox txtNome, txtNif, txtEmail, txtTelefone;
        private System.Windows.Forms.Button btnNovo, btnGuardar, btnEliminar, btnCancelar;
    }
}
