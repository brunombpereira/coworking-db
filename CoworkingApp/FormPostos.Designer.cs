namespace CoworkingApp
{
    partial class FormPostos
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvPostos    = new System.Windows.Forms.DataGridView();
            this.pnlDetalhe   = new System.Windows.Forms.Panel();
            this.lblCodigo    = new System.Windows.Forms.Label();
            this.txtCodigo    = new System.Windows.Forms.TextBox();
            this.lblTipo      = new System.Windows.Forms.Label();
            this.cmbTipo      = new System.Windows.Forms.ComboBox();
            this.lblPrecoHora = new System.Windows.Forms.Label();
            this.txtPrecoHora = new System.Windows.Forms.TextBox();
            this.lblEstado    = new System.Windows.Forms.Label();
            this.cmbEstado    = new System.Windows.Forms.ComboBox();
            this.btnNovo      = new System.Windows.Forms.Button();
            this.btnGuardar   = new System.Windows.Forms.Button();
            this.btnEliminar  = new System.Windows.Forms.Button();
            this.btnCancelar  = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.dgvPostos)).BeginInit();
            this.pnlDetalhe.SuspendLayout();
            this.SuspendLayout();

            // dgvPostos
            this.dgvPostos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPostos.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPostos.ReadOnly = true;
            this.dgvPostos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPostos.MultiSelect = false;
            this.dgvPostos.AllowUserToAddRows = false;

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

            this.lblCodigo.Text = "Código:"; this.lblCodigo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtCodigo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblTipo.Text = "Tipo:"; this.lblTipo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbTipo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbTipo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lblPrecoHora.Text = "Preço/Hora (€):"; this.lblPrecoHora.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtPrecoHora.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblEstado.Text = "Estado:"; this.lblEstado.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbEstado.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            tbl.Controls.Add(this.lblCodigo,    0, 0);
            tbl.Controls.Add(this.txtCodigo,    1, 0);
            tbl.Controls.Add(this.lblTipo,      2, 0);
            tbl.Controls.Add(this.cmbTipo,      3, 0);
            tbl.Controls.Add(this.lblPrecoHora, 0, 1);
            tbl.Controls.Add(this.txtPrecoHora, 1, 1);
            tbl.Controls.Add(this.lblEstado,    2, 1);
            tbl.Controls.Add(this.cmbEstado,    3, 1);
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

            // FormPostos
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 500);
            this.Controls.Add(this.dgvPostos);
            this.Controls.Add(this.pnlDetalhe);
            this.Name = "FormPostos";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)(this.dgvPostos)).EndInit();
            this.pnlDetalhe.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.DataGridView dgvPostos;
        private System.Windows.Forms.Panel pnlDetalhe;
        private System.Windows.Forms.Label lblCodigo, lblTipo, lblPrecoHora, lblEstado;
        private System.Windows.Forms.TextBox txtCodigo, txtPrecoHora;
        private System.Windows.Forms.ComboBox cmbTipo, cmbEstado;
        private System.Windows.Forms.Button btnNovo, btnGuardar, btnEliminar, btnCancelar;
    }
}
