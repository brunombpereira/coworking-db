namespace CoworkingApp
{
    partial class FormEspacos
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvEspacos      = new System.Windows.Forms.DataGridView();
            this.pnlDetalhe      = new System.Windows.Forms.Panel();
            this.lblNome         = new System.Windows.Forms.Label();
            this.txtNome         = new System.Windows.Forms.TextBox();
            this.lblMorada       = new System.Windows.Forms.Label();
            this.txtMorada       = new System.Windows.Forms.TextBox();
            this.lblTelefone     = new System.Windows.Forms.Label();
            this.txtTelefone     = new System.Windows.Forms.TextBox();
            this.lblEmail        = new System.Windows.Forms.Label();
            this.txtEmail        = new System.Windows.Forms.TextBox();
            this.lblHoraAbertura = new System.Windows.Forms.Label();
            this.txtHoraAbertura = new System.Windows.Forms.TextBox();
            this.lblHoraFecho    = new System.Windows.Forms.Label();
            this.txtHoraFecho    = new System.Windows.Forms.TextBox();
            this.btnNovo         = new System.Windows.Forms.Button();
            this.btnGuardar      = new System.Windows.Forms.Button();
            this.btnEliminar     = new System.Windows.Forms.Button();
            this.btnCancelar     = new System.Windows.Forms.Button();
            this.btnVerSalas     = new System.Windows.Forms.Button();
            this.btnVerPostos    = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEspacos)).BeginInit();
            this.pnlDetalhe.SuspendLayout();
            this.SuspendLayout();

            // dgvEspacos
            this.dgvEspacos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvEspacos.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvEspacos.ReadOnly = true;
            this.dgvEspacos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvEspacos.MultiSelect = false;
            this.dgvEspacos.AllowUserToAddRows = false;

            // pnlDetalhe
            this.pnlDetalhe.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlDetalhe.Height = 200;
            this.pnlDetalhe.Padding = new System.Windows.Forms.Padding(10);

            // TableLayoutPanel for fields
            var tbl = new System.Windows.Forms.TableLayoutPanel();
            tbl.ColumnCount = 4;
            tbl.RowCount = 3;
            tbl.Dock = System.Windows.Forms.DockStyle.Top;
            tbl.Height = 130;
            tbl.Padding = new System.Windows.Forms.Padding(5);

            this.lblNome.Text = "Nome:"; this.lblNome.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtNome.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblMorada.Text = "Morada:"; this.lblMorada.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtMorada.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblTelefone.Text = "Telefone:"; this.lblTelefone.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtTelefone.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblEmail.Text = "Email:"; this.lblEmail.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtEmail.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblHoraAbertura.Text = "Abertura (HH:MM):"; this.lblHoraAbertura.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtHoraAbertura.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblHoraFecho.Text = "Fecho (HH:MM):"; this.lblHoraFecho.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtHoraFecho.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            tbl.Controls.Add(this.lblNome,         0, 0);
            tbl.Controls.Add(this.txtNome,         1, 0);
            tbl.Controls.Add(this.lblMorada,       2, 0);
            tbl.Controls.Add(this.txtMorada,       3, 0);
            tbl.Controls.Add(this.lblTelefone,     0, 1);
            tbl.Controls.Add(this.txtTelefone,     1, 1);
            tbl.Controls.Add(this.lblEmail,        2, 1);
            tbl.Controls.Add(this.txtEmail,        3, 1);
            tbl.Controls.Add(this.lblHoraAbertura, 0, 2);
            tbl.Controls.Add(this.txtHoraAbertura, 1, 2);
            tbl.Controls.Add(this.lblHoraFecho,    2, 2);
            tbl.Controls.Add(this.txtHoraFecho,    3, 2);
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));

            // Buttons panel
            var pnlBtns = new System.Windows.Forms.FlowLayoutPanel();
            pnlBtns.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlBtns.Height = 50;
            pnlBtns.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;

            this.btnNovo.Text      = "Novo";       this.btnNovo.Width = 80;
            this.btnGuardar.Text   = "Guardar";    this.btnGuardar.Width = 80;
            this.btnEliminar.Text  = "Eliminar";   this.btnEliminar.Width = 80;
            this.btnCancelar.Text  = "Cancelar";   this.btnCancelar.Width = 80;
            this.btnVerSalas.Text  = "Ver Salas";  this.btnVerSalas.Width = 100;
            this.btnVerPostos.Text = "Ver Postos"; this.btnVerPostos.Width = 100;

            this.btnNovo.Click      += new System.EventHandler(this.btnNovo_Click);
            this.btnGuardar.Click   += new System.EventHandler(this.btnGuardar_Click);
            this.btnEliminar.Click  += new System.EventHandler(this.btnEliminar_Click);
            this.btnCancelar.Click  += new System.EventHandler(this.btnCancelar_Click);
            this.btnVerSalas.Click  += new System.EventHandler(this.btnVerSalas_Click);
            this.btnVerPostos.Click += new System.EventHandler(this.btnVerPostos_Click);

            pnlBtns.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.btnNovo, this.btnGuardar, this.btnEliminar, this.btnCancelar,
                this.btnVerSalas, this.btnVerPostos });

            this.pnlDetalhe.Controls.Add(tbl);
            this.pnlDetalhe.Controls.Add(pnlBtns);

            // FormEspacos
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 550);
            this.Controls.Add(this.dgvEspacos);
            this.Controls.Add(this.pnlDetalhe);
            this.Name = "FormEspacos";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)(this.dgvEspacos)).EndInit();
            this.pnlDetalhe.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.DataGridView dgvEspacos;
        private System.Windows.Forms.Panel pnlDetalhe;
        private System.Windows.Forms.Label lblNome, lblMorada, lblTelefone, lblEmail, lblHoraAbertura, lblHoraFecho;
        private System.Windows.Forms.TextBox txtNome, txtMorada, txtTelefone, txtEmail, txtHoraAbertura, txtHoraFecho;
        private System.Windows.Forms.Button btnNovo, btnGuardar, btnEliminar, btnCancelar, btnVerSalas, btnVerPostos;
    }
}
