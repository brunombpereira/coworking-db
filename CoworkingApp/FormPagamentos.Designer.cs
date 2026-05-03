namespace CoworkingApp
{
    partial class FormPagamentos
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvPagamentos        = new System.Windows.Forms.DataGridView();
            this.pnlDetalhe           = new System.Windows.Forms.Panel();
            this.cmbCliente           = new System.Windows.Forms.ComboBox();
            this.cmbItem              = new System.Windows.Forms.ComboBox();
            this.cmbMetodo            = new System.Windows.Forms.ComboBox();
            this.lblValor             = new System.Windows.Forms.Label();
            this.btnRegistarPagamento = new System.Windows.Forms.Button();
            this.btnAtualizar         = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.dgvPagamentos)).BeginInit();
            this.pnlDetalhe.SuspendLayout();
            this.SuspendLayout();

            // dgvPagamentos
            this.dgvPagamentos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPagamentos.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPagamentos.ReadOnly = true;
            this.dgvPagamentos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPagamentos.MultiSelect = false;
            this.dgvPagamentos.AllowUserToAddRows = false;

            // pnlDetalhe
            this.pnlDetalhe.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlDetalhe.Height = 130;
            this.pnlDetalhe.Padding = new System.Windows.Forms.Padding(10);

            var tbl = new System.Windows.Forms.TableLayoutPanel();
            tbl.ColumnCount = 6;
            tbl.RowCount = 2;
            tbl.Dock = System.Windows.Forms.DockStyle.Top;
            tbl.Height = 80;
            tbl.Padding = new System.Windows.Forms.Padding(5);

            var lblCliente = new System.Windows.Forms.Label();
            lblCliente.Text = "Cliente:"; lblCliente.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbCliente.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbCliente.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            var lblItem = new System.Windows.Forms.Label();
            lblItem.Text = "Item:"; lblItem.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbItem.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            var lblMetodo = new System.Windows.Forms.Label();
            lblMetodo.Text = "Método:"; lblMetodo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbMetodo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbMetodo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            this.lblValor.Text = "";
            this.lblValor.Anchor = System.Windows.Forms.AnchorStyles.Left;

            tbl.Controls.Add(lblCliente,        0, 0);
            tbl.Controls.Add(this.cmbCliente,   1, 0);
            tbl.Controls.Add(lblItem,           2, 0);
            tbl.Controls.Add(this.cmbItem,      3, 0);
            tbl.Controls.Add(lblMetodo,         4, 0);
            tbl.Controls.Add(this.cmbMetodo,    5, 0);
            tbl.Controls.Add(this.lblValor,     0, 1);
            tbl.SetColumnSpan(this.lblValor, 6);

            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));

            var pnlBtns = new System.Windows.Forms.FlowLayoutPanel();
            pnlBtns.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlBtns.Height = 40;
            pnlBtns.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;

            this.btnRegistarPagamento.Text  = "Registar Pagamento"; this.btnRegistarPagamento.Width = 150;
            this.btnAtualizar.Text          = "Atualizar";          this.btnAtualizar.Width = 80;

            this.btnRegistarPagamento.Click += new System.EventHandler(this.btnRegistarPagamento_Click);
            this.btnAtualizar.Click         += new System.EventHandler(this.btnAtualizar_Click);

            pnlBtns.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.btnRegistarPagamento, this.btnAtualizar });

            this.pnlDetalhe.Controls.Add(tbl);
            this.pnlDetalhe.Controls.Add(pnlBtns);

            // FormPagamentos
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 550);
            this.Controls.Add(this.dgvPagamentos);
            this.Controls.Add(this.pnlDetalhe);
            this.Name = "FormPagamentos";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)(this.dgvPagamentos)).EndInit();
            this.pnlDetalhe.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.DataGridView dgvPagamentos;
        private System.Windows.Forms.Panel pnlDetalhe;
        private System.Windows.Forms.ComboBox cmbCliente, cmbItem, cmbMetodo;
        private System.Windows.Forms.Label lblValor;
        private System.Windows.Forms.Button btnRegistarPagamento, btnAtualizar;
    }
}
