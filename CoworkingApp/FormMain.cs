using System;
using System.Windows.Forms;

namespace CoworkingApp
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            this.Text = "Coworking — Menu Principal";
            this.MinimumSize = new System.Drawing.Size(500, 400);
        }

        private void btnPlanos_Click(object sender, EventArgs e)      => new FormPlanos().Show();
        private void btnEspacos_Click(object sender, EventArgs e)     => new FormEspacos().Show();
        private void btnClientes_Click(object sender, EventArgs e)    => new FormClientes().Show();
        private void btnAdesoes_Click(object sender, EventArgs e)     => new FormAdesoes().Show();
        private void btnReservas_Click(object sender, EventArgs e)    => new FormReservas().Show();
        private void btnPagamentos_Click(object sender, EventArgs e)  => new FormPagamentos().Show();
        private void btnRelatorios_Click(object sender, EventArgs e)  => new FormRelatorios().Show();
    }
}
