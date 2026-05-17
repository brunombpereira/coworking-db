using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp
{
    public class FormDialog : Form
    {
        private readonly Panel _card;
        private readonly Panel _body;
        private readonly Action _saveCallback;
        private Form _ownerForCenter;
        /// <summary>Footer do dialog (Guardar/Cancelar ou Fechar) — exposto
        /// para callers em modo read-only adicionarem botões extra.</summary>
        public Panel Footer { get; private set; }

        /// <summary>
        /// Modal genérico para forms de criar/editar.
        /// </summary>
        /// <param name="title">Título mostrado no header.</param>
        /// <param name="content">Control com o formulário. Será dock'd Fill no body do dialog (atributo Dock é sobrescrito).</param>
        /// <param name="width">Largura do card central. Default 360px.</param>
        /// <param name="onSave">
        /// Delegate executado ao clicar Guardar. Para validação client-side, lança ApplicationException com mensagem
        /// para o utilizador (capturado e mostrado em MessageBox de Aviso). SqlException é capturada e formatada via
        /// Database.SqlErrorMessage. Outras exceptions propagam-se.
        /// </param>
        public FormDialog(string title, Control content, int width = 360, Action onSave = null)
        {
            _saveCallback = onSave;

            // ── Form (dialog discreto centrado no content area) ──────────────
            this.AutoScaleMode       = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.FormBorderStyle     = FormBorderStyle.None;
            this.StartPosition       = FormStartPosition.Manual; // posicionamos em ShowDialog
            this.ShowInTaskbar       = false;
            this.BackColor           = Theme.CardBorder; // border 1px à volta
            this.Padding             = new Padding(1);
            this.KeyPreview          = true;
            this.Font                = Theme.FontBase;
            this.TopMost             = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            };

            // ── Card preenche o form (border 1px do Padding do Form fica visível) ─
            _card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.CardBg,
                Padding = new Padding(0),
            };

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.CardBg };
            var lblTitle = new Label
            {
                Text = title,
                Font = Theme.FontSection,
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0)
            };
            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font(Theme.FontBase.FontFamily, 11f),
                ForeColor = Theme.TextMuted,
                BackColor = Theme.CardBg,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 40,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlHeader.Controls.Add(btnClose);
            pnlHeader.Controls.Add(lblTitle);

            // Body
            _body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.CardBg,
                Padding = new Padding(18, 8, 18, 8),
                AutoScroll = true
            };
            content.Dock = DockStyle.Fill;
            _body.Controls.Add(content);

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Theme.CardBg };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 12, 18, 12),
                AutoSize = true
            };
            // Se onSave for null → modo read-only: só botão 'Fechar'.
            bool readOnly = (onSave == null);
            if (!readOnly)
            {
                var btnSave = Theme.BtnPrim("Guardar");
                btnSave.Click += (s, e) =>
                {
                    try
                    {
                        _saveCallback?.Invoke();
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    catch (Microsoft.Data.SqlClient.SqlException ex)
                    {
                        MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (ApplicationException ex)
                    {
                        MessageBox.Show(ex.Message, "Validação",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                flow.Controls.Add(btnSave);
            }
            var btnCancel = Theme.BtnGray(readOnly ? "Fechar" : "Cancelar");
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            flow.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(flow);
            // Expor o footer para que callers possam adicionar acções (ex.
            // 'Editar dados' num detalhe read-only).
            Footer = pnlFooter;

            // Compose
            _card.Controls.Add(_body);
            _card.Controls.Add(pnlFooter);
            _card.Controls.Add(pnlHeader);

            this.Controls.Add(_card);

            // Tamanho do form = card width (+2 padding) × altura calculada do
            // body + header(48) + footer(56) + padding(32) + 2 border.
            this.Load  += (s, e) =>
            {
                SizeToContent(width);
                if (_ownerForCenter != null) CenterInContentArea(_ownerForCenter);
            };
            this.Shown += (s, e) => content.Focus();
        }

        public new DialogResult ShowDialog(IWin32Window owner)
        {
            BlurOverlayForm overlay = null;
            if (owner is Form parentForm && !parentForm.IsDisposed)
            {
                // 1. Overlay com blur do parent.
                overlay = new BlurOverlayForm(parentForm);
                overlay.Show(parentForm);
                // 2. Memorizar parent para o Load chamar CenterInContentArea
                //    APÓS SizeToContent (Width/Height só ficam válidos lá).
                _ownerForCenter = parentForm;
            }
            try
            {
                return base.ShowDialog(owner);
            }
            finally
            {
                overlay?.Close();
                overlay?.Dispose();
            }
        }

        private void CenterInContentArea(Form parent)
        {
            // Procurar a property ContentArea por reflection (não dependemos
            // de FormMain directamente para evitar circular reference).
            Rectangle area;
            var prop = parent.GetType().GetProperty("ContentArea",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop != null && prop.GetValue(parent) is Control contentCtrl)
            {
                area = contentCtrl.RectangleToScreen(contentCtrl.ClientRectangle);
            }
            else
            {
                area = parent.Bounds;
            }
            // Garantir que o form já tem tamanho válido (SizeToContent corre
            // no Load — chamamos aqui para que Location esteja correcto antes
            // do Show).
            if (this.Width  <= 0 || this.Height <= 0) return;
            this.Location = new Point(
                area.X + (area.Width  - this.Width)  / 2,
                area.Y + (area.Height - this.Height) / 2);
        }

        private void SizeToContent(int cardWidth)
        {
            int prefH = _body.PreferredSize.Height;
            // Header 48 + footer 56 + padding body 32 = 136 (matches Padding
            // do body 16+16=32 em ambos os eixos? confirmar — actualmente
            // _body tem Padding 16 cada lado = 32 vertical total).
            int contentH = Math.Min(prefH + 48 + 56 + 32, 600);  // cap 600
            int formH    = Math.Max(220, contentH) + 2;          // +2 border
            int formW    = cardWidth + 2;
            this.Size = new Size(formW, formH);
        }
    }
}
