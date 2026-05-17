using System;
using System.Windows.Forms;

namespace CoworkingApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // PerMonitorV2 — em .NET 8 WinForms o auto-handling de
            // WM_DPICHANGED já funciona out-of-the-box. Tem de ser
            // chamado ANTES de EnableVisualStyles.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) =>
                MessageBox.Show(e.Exception.Message, "Erro inesperado",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                MessageBox.Show(e.ExceptionObject?.ToString() ?? "Erro desconhecido.", "Erro fatal",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            ThemeManager.Load();

            // Loop: login -> main -> (optional logout) -> login again.
            while (true)
            {
                using (var login = new FormLogin())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }
                Application.Run(new FormMain());
                if (!FormMain.LogoutRequested) break;
                Session.Clear();
            }
        }
    }
}
