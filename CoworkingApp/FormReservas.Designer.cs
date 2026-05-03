namespace CoworkingApp
{
    partial class FormReservas
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvReservas       = new System.Windows.Forms.DataGridView();
            this.pnlBottom         = new System.Windows.Forms.Panel();
            this.cmbFiltroEstado   = new System.Windows.Forms.ComboBox();
            this.btnNovaReserva    = new System.Windows.Forms.Button();
            this.btnCancelarReserva= new System.Windows.Forms.Button();
            this.btnAtualizar      = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.dgvReservas)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();

            // dgvReservas
            this.dgvReservas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvReservas.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvReservas.ReadOnly = true;
            this.dgvReservas.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvReservas.MultiSelect = false;
            this.dgvReservas.AllowUserToAddRows = false;

            // pnlBottom
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Height = 50;

            var flow = new System.Windows.Forms.FlowLayoutPanel();
            flow.Dock = System.Windows.Forms.DockStyle.Fill;
            flow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flow.Padding = new System.Windows.Forms.Padding(5);

            var lblFiltro = new System.Windows.Forms.Label();
            lblFiltro.Text = "Filtro:";
            lblFiltro.Anchor = System.Windows.Forms.AnchorStyles.None;
            lblFiltro.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblFiltro.Width = 45;
            lblFiltro.Height = 23;

            this.cmbFiltroEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFiltroEstado.Width = 120;

            this.btnNovaReserva.Text     = "Nova Reserva";    this.btnNovaReserva.Width = 110;
            this.btnCancelarReserva.Text = "Cancelar Reserva";this.btnCancelarReserva.Width = 120;
            this.btnAtualizar.Text       = "Atualizar";       this.btnAtualizar.Width = 80;

            this.btnNovaReserva.Click     += new System.EventHandler(this.btnNovaReserva_Click);
            this.btnCancelarReserva.Click += new System.EventHandler(this.btnCancelarReserva_Click);
            this.btnAtualizar.Click       += new System.EventHandler(this.btnAtualizar_Click);

            flow.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblFiltro, this.cmbFiltroEstado,
                this.btnNovaReserva, this.btnCancelarReserva, this.btnAtualizar });

            this.pnlBottom.Controls.Add(flow);

            // FormReservas
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.dgvReservas);
            this.Controls.Add(this.pnlBottom);
            this.Name = "FormReservas";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)(this.dgvReservas)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.DataGridView dgvReservas;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.ComboBox cmbFiltroEstado;
        private System.Windows.Forms.Button btnNovaReserva, btnCancelarReserva, btnAtualizar;
    }
}
