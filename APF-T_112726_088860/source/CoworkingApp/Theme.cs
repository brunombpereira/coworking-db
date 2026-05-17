using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CoworkingApp
{
    public static class Theme
    {
        // ── Helpers ──────────────────────────────────────────────────────────
        private static Color L(string hex) => ColorTranslator.FromHtml(hex);
        private static Color Pick(string light, string dark) =>
            ThemeManager.Current == ThemeMode.Light ? L(light) : L(dark);

        // ── Sidebar ──────────────────────────────────────────────────────────
        public static Color SidebarBg          => Pick("#0f172a", "#020617");
        public static Color SidebarBgActive    => Pick("#312e81", "#1e1b4b");
        public static Color SidebarText        => Pick("#94a3b8", "#64748b");
        public static Color SidebarTextActive  => L("#a5b4fc");
        public static Color SidebarSectionLbl  => Pick("#64748b", "#475569");

        // ── Page / cards ─────────────────────────────────────────────────────
        public static Color PageBg     => Pick("#f8fafc", "#0f172a");
        public static Color ContentBg  => PageBg;
        public static Color CardBg     => Pick("#ffffff", "#1e293b");
        public static Color CardBorder => Pick("#e2e8f0", "#334155");
        public static Color ToolbarBg  => CardBg;

        // ── Text ─────────────────────────────────────────────────────────────
        public static Color TextPrimary   => Pick("#0f172a", "#f1f5f9");
        public static Color TextSecondary => Pick("#64748b", "#94a3b8");
        public static Color TextMuted     => Pick("#94a3b8", "#64748b");
        public static Color TextOnAccent  => Color.White;

        // ── Accent ───────────────────────────────────────────────────────────
        public static Color Accent      => L("#6366f1");
        public static Color AccentHover => Pick("#4f46e5", "#818cf8");
        public static Color AccentSoft  => Pick("#e0e7ff", "#312e81");

        // ── Status (bg / fg) ─────────────────────────────────────────────────
        public static Color StatusSuccessBg => Pick("#d1fae5", "#064e3b");
        public static Color StatusSuccessFg => Pick("#065f46", "#6ee7b7");
        public static Color StatusWarningBg => Pick("#fef3c7", "#78350f");
        public static Color StatusWarningFg => Pick("#92400e", "#fcd34d");
        public static Color StatusDangerBg  => Pick("#fee2e2", "#7f1d1d");
        public static Color StatusDangerFg  => Pick("#991b1b", "#fca5a5");
        public static Color StatusNeutralBg => Pick("#f1f5f9", "#1e293b");
        public static Color StatusNeutralFg => Pick("#475569", "#94a3b8");
        public static Color StatusOrangeBg  => Pick("#fed7aa", "#7c2d12");
        public static Color StatusOrangeFg  => Pick("#9a3412", "#fdba74");

        // ── Buttons ──────────────────────────────────────────────────────────
        public static Color BtnPrimaryBg  => Accent;
        public static Color BtnPrimaryFg  => Color.White;
        public static Color BtnNeutralBg  => CardBg;
        public static Color BtnNeutralFg  => TextSecondary;
        public static Color BtnNeutralBd  => CardBorder;
        public static Color BtnDangerBg   => L("#ef4444");
        public static Color BtnDangerFg   => Color.White;

        // ── Grid ─────────────────────────────────────────────────────────────
        public static Color GridHeaderBg  => L("#312e81");
        public static Color GridHeaderFg  => Color.White;
        public static Color GridRowAlt    => Pick("#f8fafc", "#1e293b");
        public static Color GridSelected  => Pick("#e0e7ff", "#1e3a8a");
        public static Color GridLine      => CardBorder;

        // ── Modal ────────────────────────────────────────────────────────────
        public static Color ModalOverlay  => Pick("#0f172a", "#020617");
        public static double ModalOpacity => ThemeManager.Current == ThemeMode.Light ? 0.45 : 0.65;

        // ── Form fields ──────────────────────────────────────────────────────
        public static Color FieldBg     => Pick("#f8fafc", "#0f172a");
        public static Color FieldBorder => CardBorder;

        // ── Tipografia ───────────────────────────────────────────────────────
        private const string FontFamily = "Segoe UI";

        public static readonly Font FontTitle   = new Font(FontFamily, 18f, FontStyle.Bold);
        public static readonly Font FontHero    = new Font(FontFamily, 28f, FontStyle.Bold);
        public static readonly Font FontSection = new Font(FontFamily, 12f, FontStyle.Bold);
        public static readonly Font FontBase    = new Font(FontFamily, 11f);
        public static readonly Font FontBold    = new Font(FontFamily, 11f, FontStyle.Bold);
        public static readonly Font FontLabel   = new Font(FontFamily, 9f);
        public static readonly Font FontMicro   = new Font(FontFamily, 8f, FontStyle.Bold);
        public static readonly Font FontSub     = new Font(FontFamily, 9f);

        // ── Helpers ──────────────────────────────────────────────────────────
        public static string FormatEuro(decimal value)
            => value.ToString("#,##0.00", new CultureInfo("pt-PT")) + " €";

        // ── Grid styling ─────────────────────────────────────────────────────
        public static void StyleGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = CardBg;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBg;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderFg;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontBold;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            dgv.ColumnHeadersHeight = 36;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = GridRowAlt;
            dgv.DefaultCellStyle.BackColor = CardBg;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = GridSelected;
            dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
            dgv.DefaultCellStyle.Font = FontBase;
            dgv.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            dgv.RowTemplate.Height = 34;
            dgv.GridColor = GridLine;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
        }

        // ── Button factories ─────────────────────────────────────────────────
        // Voltam ModernButton (rounded + hover/pressed states) com a paleta
        // do Theme. Compatível com o tipo Button — todos os call sites antigos
        // (BtnPrim → btn.Click+=, btn.Enabled=...) continuam a funcionar.
        public static Button BtnPrim(string text)
            => new ModernButton { Text = text, Style = ModernButton.Variant.Primary,   Width = 120, Height = 36 };

        public static Button BtnRed(string text)
            => new ModernButton { Text = text, Style = ModernButton.Variant.Danger,    Width = 120, Height = 36 };

        public static Button BtnGray(string text)
            => new ModernButton { Text = text, Style = ModernButton.Variant.Secondary, Width = 120, Height = 36 };

        // ── Toolbar / form panel ─────────────────────────────────────────────
        public static Panel Toolbar()
        {
            var bar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = ToolbarBg,
                Padding   = new Padding(12, 12, 12, 10),
            };
            // Border-bottom subtil — separa toolbar do grid sem ser ruido.
            bar.Paint += (s, e) =>
            {
                using (var pen = new Pen(CardBorder, 1))
                    e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            };
            return bar;
        }

        public static FlowLayoutPanel ToolbarFlow()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(4, 0, 0, 0)
            };
        }

        // FormPanel mantido para compatibilidade durante migração; não usado
        // após Phase C.
        public static Panel FormPanel(int height)
        {
            return new Panel
            {
                Dock = DockStyle.Bottom,
                Height = height,
                BackColor = CardBg,
                Padding = new Padding(16, 12, 16, 12),
                Visible = false
            };
        }

        // ── Field factories ──────────────────────────────────────────────────
        public static Label FieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = FontLabel,
                ForeColor = TextSecondary,
                AutoSize = true,
                Dock = DockStyle.Top
            };
        }

        public static TextBox Field()
        {
            return new TextBox
            {
                Font = FontBase,
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = FieldBg,
                ForeColor = TextPrimary
            };
        }

        public static ComboBox Combo()
        {
            return new ComboBox
            {
                Font = FontBase,
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = FieldBg,
                ForeColor = TextPrimary
            };
        }

        public static DateTimePicker DatePicker()
        {
            return new DateTimePicker
            {
                Font = FontBase,
                Dock = DockStyle.Top,
                Format = DateTimePickerFormat.Short
            };
        }

        // ── Status helpers ───────────────────────────────────────────────────
        public static void ApplyStatusColor(DataGridViewCellFormattingEventArgs e, string colName, DataGridView dgv)
        {
            if (e.ColumnIndex < 0 || e.Value == null) return;
            if (dgv.Columns[e.ColumnIndex].Name != colName) return;
            switch (e.Value.ToString())
            {
                case "Confirmada":
                case "Ativa":
                case "Pago":
                    e.CellStyle.BackColor = StatusSuccessBg;
                    e.CellStyle.ForeColor = StatusSuccessFg;
                    break;
                case "Pendente":
                    e.CellStyle.BackColor = StatusWarningBg;
                    e.CellStyle.ForeColor = StatusWarningFg;
                    break;
                case "Cancelada":
                case "Cancelado":
                    e.CellStyle.BackColor = StatusDangerBg;
                    e.CellStyle.ForeColor = StatusDangerFg;
                    break;
                case "Concluida":
                case "Terminada":
                    e.CellStyle.BackColor = StatusNeutralBg;
                    e.CellStyle.ForeColor = StatusNeutralFg;
                    break;
                case "Suspensa":
                    e.CellStyle.BackColor = StatusOrangeBg;
                    e.CellStyle.ForeColor = StatusOrangeFg;
                    break;
            }
        }

        public static void ShowSuccess(Label lbl, string msg = "Guardado com sucesso.")
        {
            lbl.Text      = msg;
            lbl.ForeColor = StatusSuccessFg;
            lbl.Visible   = true;
            var t = new Timer { Interval = 2500 };
            t.Tick += (s, e2) => { lbl.Visible = false; t.Stop(); t.Dispose(); };
            t.Start();
        }
    }
}
