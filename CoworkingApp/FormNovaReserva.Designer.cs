namespace CoworkingApp
{
    partial class FormNovaReserva
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.cmbCliente       = new System.Windows.Forms.ComboBox();
            this.rbSala           = new System.Windows.Forms.RadioButton();
            this.rbPosto          = new System.Windows.Forms.RadioButton();
            this.cmbRecurso       = new System.Windows.Forms.ComboBox();
            this.dtpData          = new System.Windows.Forms.DateTimePicker();
            this.dtpHoraInicio    = new System.Windows.Forms.DateTimePicker();
            this.dtpHoraFim       = new System.Windows.Forms.DateTimePicker();
            this.lblParticipantes = new System.Windows.Forms.Label();
            this.txtParticipantes = new System.Windows.Forms.TextBox();
            this.lblNotas         = new System.Windows.Forms.Label();
            this.txtNotas         = new System.Windows.Forms.TextBox();
            this.lblValorCalculado= new System.Windows.Forms.Label();
            this.btnCalcular      = new System.Windows.Forms.Button();
            this.btnConfirmar     = new System.Windows.Forms.Button();
            this.btnCancelar      = new System.Windows.Forms.Button();

            this.SuspendLayout();

            var tbl = new System.Windows.Forms.TableLayoutPanel();
            tbl.ColumnCount = 4;
            tbl.RowCount = 8;
            tbl.Dock = System.Windows.Forms.DockStyle.Fill;
            tbl.Padding = new System.Windows.Forms.Padding(10);
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            tbl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));

            // Row 0: Cliente
            var lblCliente = new System.Windows.Forms.Label();
            lblCliente.Text = "Cliente:"; lblCliente.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbCliente.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbCliente.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            tbl.Controls.Add(lblCliente,      0, 0);
            tbl.Controls.Add(this.cmbCliente, 1, 0);
            tbl.SetColumnSpan(this.cmbCliente, 3);

            // Row 1: Tipo (RadioButtons)
            var lblTipo = new System.Windows.Forms.Label();
            lblTipo.Text = "Tipo:"; lblTipo.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.rbSala.Text  = "Sala";  this.rbSala.Anchor  = System.Windows.Forms.AnchorStyles.Left;
            this.rbPosto.Text = "Posto"; this.rbPosto.Anchor = System.Windows.Forms.AnchorStyles.Left;
            tbl.Controls.Add(lblTipo,      0, 1);
            tbl.Controls.Add(this.rbSala,  1, 1);
            tbl.Controls.Add(this.rbPosto, 2, 1);

            // Row 2: Recurso
            var lblRecurso = new System.Windows.Forms.Label();
            lblRecurso.Text = "Recurso:"; lblRecurso.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cmbRecurso.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.cmbRecurso.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            tbl.Controls.Add(lblRecurso,       0, 2);
            tbl.Controls.Add(this.cmbRecurso,  1, 2);
            tbl.SetColumnSpan(this.cmbRecurso, 3);

            // Row 3: Data
            var lblData = new System.Windows.Forms.Label();
            lblData.Text = "Data:"; lblData.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.dtpData.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.dtpData.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            tbl.Controls.Add(lblData,      0, 3);
            tbl.Controls.Add(this.dtpData, 1, 3);

            // Row 4: Hora Início / Hora Fim
            var lblHI = new System.Windows.Forms.Label();
            lblHI.Text = "Hora Início:"; lblHI.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.dtpHoraInicio.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.dtpHoraInicio.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpHoraInicio.ShowUpDown = true;

            var lblHF = new System.Windows.Forms.Label();
            lblHF.Text = "Hora Fim:"; lblHF.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.dtpHoraFim.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.dtpHoraFim.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpHoraFim.ShowUpDown = true;

            tbl.Controls.Add(lblHI,              0, 4);
            tbl.Controls.Add(this.dtpHoraInicio, 1, 4);
            tbl.Controls.Add(lblHF,              2, 4);
            tbl.Controls.Add(this.dtpHoraFim,    3, 4);

            // Row 5: Participantes (conditional)
            this.lblParticipantes.Text = "Nº Participantes:";
            this.lblParticipantes.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtParticipantes.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tbl.Controls.Add(this.lblParticipantes, 0, 5);
            tbl.Controls.Add(this.txtParticipantes, 1, 5);

            // Row 5 (col 2-3): Notas label
            this.lblNotas.Text = "Notas:"; this.lblNotas.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.txtNotas.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tbl.Controls.Add(this.lblNotas, 2, 5);
            tbl.Controls.Add(this.txtNotas, 3, 5);

            // Row 6: Valor calculado
            this.lblValorCalculado.Text = "Valor: —";
            this.lblValorCalculado.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblValorCalculado.Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 10f, System.Drawing.FontStyle.Bold);
            tbl.Controls.Add(this.lblValorCalculado, 0, 6);
            tbl.SetColumnSpan(this.lblValorCalculado, 4);

            // Row 7: Buttons
            var pnlBtns = new System.Windows.Forms.FlowLayoutPanel();
            pnlBtns.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            pnlBtns.Anchor = System.Windows.Forms.AnchorStyles.Left;

            this.btnCalcular.Text  = "Calcular";  this.btnCalcular.Width = 90;
            this.btnConfirmar.Text = "Confirmar"; this.btnConfirmar.Width = 90;
            this.btnCancelar.Text  = "Cancelar";  this.btnCancelar.Width = 90;

            this.btnCalcular.Click  += new System.EventHandler(this.btnCalcular_Click);
            this.btnConfirmar.Click += new System.EventHandler(this.btnConfirmar_Click);
            this.btnCancelar.Click  += new System.EventHandler(this.btnCancelar_Click);

            pnlBtns.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.btnCalcular, this.btnConfirmar, this.btnCancelar });

            tbl.Controls.Add(pnlBtns, 0, 7);
            tbl.SetColumnSpan(pnlBtns, 4);

            this.Controls.Add(tbl);

            // FormNovaReserva
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 420);
            this.Name = "FormNovaReserva";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.ResumeLayout(false);
        }

        private System.Windows.Forms.ComboBox cmbCliente, cmbRecurso;
        private System.Windows.Forms.RadioButton rbSala, rbPosto;
        private System.Windows.Forms.DateTimePicker dtpData, dtpHoraInicio, dtpHoraFim;
        private System.Windows.Forms.Label lblParticipantes, lblNotas, lblValorCalculado;
        private System.Windows.Forms.TextBox txtParticipantes, txtNotas;
        private System.Windows.Forms.Button btnCalcular, btnConfirmar, btnCancelar;
    }
}
