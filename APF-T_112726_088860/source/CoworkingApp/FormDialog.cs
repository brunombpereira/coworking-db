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

        /// <summary>Adiciona um botão de acção ao footer (right-aligned).
        /// Torna o footer visível se ainda não estiver — usado por modo
        /// read-only para acções tipo "Descarregar PDF".</summary>
        public void AddFooterAction(Control button)
        {
            if (_footerFlow == null) return;
            _footerFlow.Controls.Add(button);
            Footer.Visible = true;
        }

        private FlowLayoutPanel _footerFlow;

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

            // Footer — sem linha divisória (era descontínua porque o flow
            // panel à direita ficava por cima de parte dela).
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = Theme.CardBg };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 16, 24, 16),
                AutoSize = true,
                BackColor = Theme.CardBg,
            };
            // Modo read-only (onSave=null): footer pode ser preenchido pelo
            // caller (ex: "Descarregar PDF"). Sem onSave, fica invisível por
            // omissão — caller deve fazer Footer.Visible=true se adicionar.
            bool readOnly = (onSave == null);
            _footerFlow = flow;
            pnlFooter.Controls.Add(flow);
            if (!readOnly)
            {
                var btnSave = new ModernButton
                {
                    Text = "Guardar", Style = ModernButton.Variant.Primary,
                    Font = Theme.FontBold, Size = new Size(140, 36),
                    Margin = new Padding(12, 0, 0, 0),
                };
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
                // Cancelar como text-button (sem border) — visual menos
                // proeminente que o Guardar.
                var btnCancel = new Button
                {
                    Text = "Cancelar",
                    Font = Theme.FontBold,
                    Size = new Size(100, 36),
                    Margin = new Padding(0),
                    BackColor = Theme.CardBg,
                    ForeColor = Theme.TextSecondary,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    TabStop = false,
                };
                btnCancel.FlatAppearance.BorderSize = 0;
                btnCancel.FlatAppearance.MouseOverBackColor = Theme.CardBg;
                btnCancel.MouseEnter += (s, e) => btnCancel.ForeColor = Theme.TextPrimary;
                btnCancel.MouseLeave += (s, e) => btnCancel.ForeColor = Theme.TextSecondary;
                btnCancel.Click      += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

                // Right-to-left flow → primeiro Add = mais à direita.
                flow.Controls.Add(btnSave);
                flow.Controls.Add(btnCancel);
            }
            else
            {
                pnlFooter.Visible = false;
            }
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
            // Centrar na área cliente do form (exclui title bar). Inclui a
            // sidebar (user pediu) mas tenta excluir a status bar (Dock=Bottom
            // no FormMain) para que o centro Y bata visualmente.
            if (this.Width <= 0 || this.Height <= 0) return;
            Rectangle area = parent.RectangleToScreen(parent.ClientRectangle);

            int statusH = 0;
            foreach (Control c in parent.Controls)
            {
                if (c.Dock == DockStyle.Bottom && c.Visible)
                {
                    statusH = c.Height;
                    break;
                }
            }
            area = new Rectangle(area.X, area.Y, area.Width, area.Height - statusH);

            // Slight bias para top-left: 40% no eixo Y (em vez de 50%) e
            // -32 px no eixo X. Dá uma posição visualmente menos centrada
            // ao meio absoluto e mais próxima do canto superior esquerdo.
            int x = area.X + (area.Width  - this.Width)  / 2 - 32;
            int y = area.Y + (int)((area.Height - this.Height) * 0.4);
            this.Location = new Point(x, y);
        }

        private void SizeToContent(int cardWidth)
        {
            int prefH = _body.PreferredSize.Height;
            // Header 48 + footer 68 (se visível) + padding body 32.
            int footerH = (Footer != null && Footer.Visible) ? 68 : 0;
            int contentH = Math.Min(prefH + 48 + footerH + 32, 600);  // cap 600
            int formH    = Math.Max(220, contentH) + 2;               // +2 border
            int formW    = cardWidth + 2;
            this.Size = new Size(formW, formH);
        }
    }
}
