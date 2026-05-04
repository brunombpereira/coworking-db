using System;
using System.Windows.Forms;

namespace CoworkingApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.Message, "Erro inesperado",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show(e.ExceptionObject?.ToString() ?? "Erro desconhecido.", "Erro fatal",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Run(new FormMain());
        }
    }
}
