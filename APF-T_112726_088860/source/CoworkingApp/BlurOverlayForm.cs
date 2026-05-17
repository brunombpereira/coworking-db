using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Form que cobre completamente o owner, mostrando uma versão "blur"
    /// do conteúdo do parent capturado no momento de criação. Usado como
    /// pano de fundo para diálogos modais (efeito frosted glass).
    ///
    /// Implementação simples e barata: captura via DrawToBitmap, downscale
    /// 12x + upscale com HighQualityBicubic → efeito de desfoque sem custo
    /// de Gaussian blur real. Suficiente para o look pretendido.
    /// </summary>
    public class BlurOverlayForm : Form
    {
        private Bitmap _blurred;

        public BlurOverlayForm(Form parent)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.Manual;
            ShowInTaskbar   = false;
            DoubleBuffered  = true;
            BackColor       = Color.Black;

            if (parent == null || parent.IsDisposed) return;

            Bounds = parent.Bounds;
            CaptureBlur(parent);
        }

        private void CaptureBlur(Form parent)
        {
            try
            {
                int w = parent.Width;
                int h = parent.Height;
                if (w <= 0 || h <= 0) return;

                using (var full = new Bitmap(w, h))
                {
                    parent.DrawToBitmap(full, new Rectangle(0, 0, w, h));

                    // Downscale agressivo → upscale com bicubic = pseudo-blur barato.
                    const int factor = 12;
                    int sw = Math.Max(1, w / factor);
                    int sh = Math.Max(1, h / factor);
                    using (var small = new Bitmap(sw, sh))
                    {
                        using (var gs = Graphics.FromImage(small))
                        {
                            gs.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            gs.DrawImage(full, 0, 0, sw, sh);
                        }
                        _blurred = new Bitmap(w, h);
                        using (var gb = Graphics.FromImage(_blurred))
                        {
                            gb.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gb.PixelOffsetMode   = PixelOffsetMode.HighQuality;
                            gb.DrawImage(small, new Rectangle(0, 0, w, h));
                        }
                    }
                }
            }
            catch { /* fallback: BackColor preto sólido */ }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_blurred != null)
                e.Graphics.DrawImage(_blurred, 0, 0, Width, Height);
            // Tint escuro subtle por cima para mais contraste com o modal.
            using (var br = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
                e.Graphics.FillRectangle(br, ClientRectangle);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _blurred?.Dispose();
            _blurred = null;
            base.OnFormClosed(e);
        }

        // Não responde a clicks — o modal por cima é que recebe.
        protected override void OnMouseDown(MouseEventArgs e) { /* swallow */ }
    }
}
