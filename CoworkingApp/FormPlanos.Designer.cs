namespace CoworkingApp
{
    partial class FormPlanos
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvPlanos   = new System.Windows.Forms.DataGridView();
            this.pnlDetalhe  = new System.Windows.Forms.Panel();
            this.lblNome     = new System.Windows.Forms.Label();
            this.txtNome     = new System.Windows.Forms.TextBox();
            this.lblPreco    = new System.Windows.Forms.Label();
            this.txtPreco    = new System.Windows.Forms.TextBox();
            this.lblDuracao  = new System.Windows.Forms.Label();
            this.txtDuracao  = new System.Windows.Forms.TextBox();
            this.lblDescricao= new System.Windows.Forms.Label();
            this.txtDescricao= new System.Windows.Forms.TextBox();
            this.btnNovo     = new System.Windows.Forms.Button();
            this.btnGuardar  = new System.Windows.Forms.Button();
            this.btnEliminar = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlanos)).BeginInit();
            this.pnlDetalhe.SuspendLayout();
            this.SuspendLayout();

            // dgvPlanos
            this.dgvPlanos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPlanos.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPlanos.ReadOnly = true;
            this.dgvPlanos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlanos.MultiSelect = false;
            this.dgvPlanos.AllowUserToAddRows = false;

            // pnlDetalhe
            this.pnlDetalhe.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlDetalhe.Height = 180;
            this.pnlDetalhe.Padding = new System.Windows.Forms.Padding(10);

            // Labels and TextBoxes (using TableLayoutPanel for alignment)
            var tbl = new System.Windows.Forms.TableLayoutPanel();
            tbl.ColumnCount = 4;
            tbl.RowCount = 3;
            tbl.Dock = System.Windows.Forms.DockStyle.Top;
            tbl.Height = 110;
            tbl.Padding = new System.Windows.Forms.Padding(5);

            this.lblNome.Text = "Nome:"; this.lblNome.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtNome.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblPreco.Text = "Preço/Mês (€):"; this.lblPreco.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtPreco.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblDuracao.Text = "Duração (meses):"; this.lblDuracao.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtDuracao.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblDescricao.Text = "Descrição:"; this.lblDescricao.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtDescricao.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            tbl.Controls.Add(this.lblNome, 0, 0);
            tbl.Controls.Add(this.txtNome, 1, 0);
            tbl.Controls.Add(this.lblPreco, 2, 0);
            tbl.Controls.Add(this.txtPreco, 3, 0);
            tbl.Controls.Add(this.lblDuracao, 0, 1);
            tbl.Controls.Add(this.txtDuracao, 1, 1);
            tbl.Controls.Add(this.lblDescricao, 2, 1);
            tbl.Controls.Add(this.txtDescricao, 3, 1);
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));

            // Buttons panel
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

            // FormPlanos
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 500);
            this.Controls.Add(this.dgvPlanos);
            this.Controls.Add(this.pnlDetalhe);
            this.Name = "FormPlanos";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)(this.dgvPlanos)).EndInit();
            this.pnlDetalhe.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.DataGridView dgvPlanos;
        private System.Windows.Forms.Panel pnlDetalhe;
        private System.Windows.Forms.Label lblNome, lblPreco, lblDuracao, lblDescricao;
        private System.Windows.Forms.TextBox txtNome, txtPreco, txtDuracao, txtDescricao;
        private System.Windows.Forms.Button btnNovo, btnGuardar, btnEliminar, btnCancelar;
    }
}
