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

        public FormDialog(string title, Control content, int width = 360, Action onSave = null)
        {
            _saveCallback = onSave;

            // ── Form (overlay) ───────────────────────────────────────────────
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.BackColor = Theme.ModalOverlay;
            this.Opacity = Theme.ModalOpacity;
            this.KeyPreview = true;
            this.Font = Theme.FontBase;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            };

            // ── Card central ─────────────────────────────────────────────────
            _card = new Panel
            {
                BackColor = Theme.CardBg,
                Width = width,
                Padding = new Padding(0)
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
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnClose);

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
            var btnSave = Theme.BtnPrim("Guardar");
            btnSave.Click += (s, e) =>
            {
                try
                {
                    _saveCallback?.Invoke();
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    MessageBox.Show(Database.SqlErrorMessage(ex), "Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            var btnCancel = Theme.BtnGray("Cancelar");
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            flow.Controls.Add(btnSave);
            flow.Controls.Add(btnCancel);
            pnlFooter.Controls.Add(flow);

            // Compose
            _card.Controls.Add(_body);
            _card.Controls.Add(pnlFooter);
            _card.Controls.Add(pnlHeader);

            this.Controls.Add(_card);

            this.Load += (s, e) => CenterCard();
            this.Shown += (s, e) => content.Focus();
        }

        public new DialogResult ShowDialog(IWin32Window owner)
        {
            // Cobrir o owner
            if (owner is Form ownerForm)
            {
                this.Bounds = ownerForm.Bounds;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
            return base.ShowDialog(owner);
        }

        private void CenterCard()
        {
            // Auto-fit altura do card pelo body preferred + header + footer
            int prefH = _body.PreferredSize.Height;
            int total = Math.Min(prefH + 48 + 56 + 32, this.Height - 40);
            _card.Height = Math.Max(180, total);
            _card.Left = (this.Width - _card.Width) / 2;
            _card.Top = (this.Height - _card.Height) / 2;
        }
    }
}
