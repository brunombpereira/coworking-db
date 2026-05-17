using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace CoworkingApp
{
    /// <summary>
    /// Gera (uma única vez) o ícone "C" da app a partir de GDI+. Aplica-se
    /// a Form.Icon — Windows usa este ícone tanto na title bar do form como
    /// na entrada da taskbar enquanto o form estiver vivo.
    ///
    /// Limitação: a taskbar PRÉ-arranque (antes do primeiro form abrir) e
    /// o ficheiro .exe em si continuam com o ícone default. Para um ícone
    /// permanente, é preciso um ficheiro .ico real referenciado no csproj
    /// como &lt;ApplicationIcon&gt;app.ico&lt;/ApplicationIcon&gt;.
    /// </summary>
    public static class AppIcon
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(System.IntPtr hIcon);

        private static Icon _icon;

        public static Icon Get(int size = 32)
        {
            if (_icon != null) return _icon;

            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Fundo arredondado indigo
                var rect = new Rectangle(0, 0, size - 1, size - 1);
                using (var path  = ModernCard.RoundedRect(rect, size / 5))
                using (var brush = new SolidBrush(Theme.Accent))
                {
                    g.FillPath(brush, path);
                }

                // Letra "C" branca centrada
                using (var font = new Font("Segoe UI", size * 0.55f, FontStyle.Bold,
                                            GraphicsUnit.Pixel))
                using (var sf   = new StringFormat
                       {
                           Alignment     = StringAlignment.Center,
                           LineAlignment = StringAlignment.Center,
                       })
                using (var brush = new SolidBrush(Color.White))
                {
                    var textRect = new RectangleF(0, -1, size, size);
                    g.DrawString("C", font, brush, textRect, sf);
                }
            }

            System.IntPtr hIcon = bmp.GetHicon();
            _icon = (Icon)Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon);
            bmp.Dispose();
            return _icon;
        }
    }
}
