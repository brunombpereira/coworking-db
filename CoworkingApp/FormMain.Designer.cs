namespace CoworkingApp
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitulo       = new System.Windows.Forms.Label();
            this.pnlBotoes       = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPlanos       = new System.Windows.Forms.Button();
            this.btnEspacos      = new System.Windows.Forms.Button();
            this.btnClientes     = new System.Windows.Forms.Button();
            this.btnAdesoes      = new System.Windows.Forms.Button();
            this.btnReservas     = new System.Windows.Forms.Button();
            this.btnPagamentos   = new System.Windows.Forms.Button();
            this.btnRelatorios   = new System.Windows.Forms.Button();
            this.pnlBotoes.SuspendLayout();
            this.SuspendLayout();

            // lblTitulo
            this.lblTitulo.Dock      = System.Windows.Forms.DockStyle.Top;
            this.lblTitulo.Font      = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.Height    = 60;
            this.lblTitulo.Text      = "Sistema de Gestão de Coworking";
            this.lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // pnlBotoes
            this.pnlBotoes.Dock          = System.Windows.Forms.DockStyle.Fill;
            this.pnlBotoes.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnlBotoes.Padding       = new System.Windows.Forms.Padding(60, 20, 60, 20);
            this.pnlBotoes.WrapContents  = false;
            this.pnlBotoes.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.btnPlanos, this.btnEspacos, this.btnClientes,
                this.btnAdesoes, this.btnReservas, this.btnPagamentos, this.btnRelatorios
            });

            // Buttons (shared style)
            System.Windows.Forms.Button[] btns = {
                this.btnPlanos, this.btnEspacos, this.btnClientes,
                this.btnAdesoes, this.btnReservas, this.btnPagamentos, this.btnRelatorios
            };
            string[] labels = { "Planos", "Espaços", "Clientes", "Adesões", "Reservas", "Pagamentos", "Relatórios" };
            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].Text    = labels[i];
                btns[i].Width   = 320;
                btns[i].Height  = 40;
                btns[i].Margin  = new System.Windows.Forms.Padding(0, 5, 0, 5);
                btns[i].Font    = new System.Drawing.Font("Segoe UI", 11F);
            }

            // Wire click events
            this.btnPlanos.Click      += new System.EventHandler(this.btnPlanos_Click);
            this.btnEspacos.Click     += new System.EventHandler(this.btnEspacos_Click);
            this.btnClientes.Click    += new System.EventHandler(this.btnClientes_Click);
            this.btnAdesoes.Click     += new System.EventHandler(this.btnAdesoes_Click);
            this.btnReservas.Click    += new System.EventHandler(this.btnReservas_Click);
            this.btnPagamentos.Click  += new System.EventHandler(this.btnPagamentos_Click);
            this.btnRelatorios.Click  += new System.EventHandler(this.btnRelatorios_Click);

            // FormMain
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(440, 440);
            this.Controls.Add(this.pnlBotoes);
            this.Controls.Add(this.lblTitulo);
            this.Name = "FormMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.pnlBotoes.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label           lblTitulo;
        private System.Windows.Forms.FlowLayoutPanel pnlBotoes;
        private System.Windows.Forms.Button          btnPlanos;
        private System.Windows.Forms.Button          btnEspacos;
        private System.Windows.Forms.Button          btnClientes;
        private System.Windows.Forms.Button          btnAdesoes;
        private System.Windows.Forms.Button          btnReservas;
        private System.Windows.Forms.Button          btnPagamentos;
        private System.Windows.Forms.Button          btnRelatorios;
    }
}
