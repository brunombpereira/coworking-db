using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CoworkingApp
{
    public static class Theme
    {
        public static readonly Color SidebarBg     = ColorTranslator.FromHtml("#1d4ed8");
        public static readonly Color SidebarActive = ColorTranslator.FromHtml("#1e40af");
        public static readonly Color SidebarText   = ColorTranslator.FromHtml("#bfdbfe");
        public static readonly Color ContentBg     = ColorTranslator.FromHtml("#f0f9ff");
        public static readonly Color GridHeader    = ColorTranslator.FromHtml("#1d4ed8");
        public static readonly Color GridHeaderFg  = Color.White;
        public static readonly Color GridRowAlt    = ColorTranslator.FromHtml("#f8fafc");
        public static readonly Color GridSelected  = ColorTranslator.FromHtml("#dbeafe");
        public static readonly Color FormBorder    = ColorTranslator.FromHtml("#bfdbfe");
        public static readonly Color BtnPrimary    = ColorTranslator.FromHtml("#1d4ed8");
        public static readonly Color BtnDanger     = ColorTranslator.FromHtml("#ef4444");
        public static readonly Color BtnNeutral    = ColorTranslator.FromHtml("#94a3b8");
        public static readonly Color ToolbarBg     = Color.White;
        public static readonly Color PageBg        = ColorTranslator.FromHtml("#f0f9ff");

        public static readonly Font FontBase  = new Font("Segoe UI", 9.5f);
        public static readonly Font FontBold  = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font FontTitle = new Font("Segoe UI", 15f, FontStyle.Bold);
        public static readonly Font FontSub   = new Font("Segoe UI", 8f);
        public static readonly Font FontLabel = new Font("Segoe UI", 8.5f);

        public static string FormatEuro(decimal value)
            => value.ToString("#,##0.00", new CultureInfo("pt-PT")) + " €";

        public static void StyleGrid(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderFg;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            dgv.ColumnHeadersHeight = 34;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = GridRowAlt;
            dgv.DefaultCellStyle.SelectionBackColor = GridSelected;
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgv.DefaultCellStyle.Font = FontBase;
            dgv.DefaultCellStyle.Padding = new Padding(4, 0, 0, 0);
            dgv.RowTemplate.Height = 30;
            dgv.GridColor = ColorTranslator.FromHtml("#e2e8f0");
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
        }

        public static Button Btn(string text, Color backColor, Color foreColor)
        {
            var b = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 30,
                AutoSize = true,
                Padding = new Padding(10, 0, 10, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        public static Button BtnPrim(string text) => Btn(text, BtnPrimary, Color.White);
        public static Button BtnRed(string text)  => Btn(text, BtnDanger,  Color.White);

        public static Button BtnGray(string text)
        {
            var b = new Button
            {
                Text = text,
                BackColor = ColorTranslator.FromHtml("#f1f5f9"),
                ForeColor = ColorTranslator.FromHtml("#475569"),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Height = 30,
                AutoSize = true,
                Padding = new Padding(10, 0, 10, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#e2e8f0");
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        public static Panel Toolbar()
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = ToolbarBg,
                Padding = new Padding(0, 8, 0, 8)
            };
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

        public static Panel FormPanel(int height)
        {
            var p = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = height,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10),
                Visible = false
            };
            p.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(FormBorder, 1))
                    e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
            };
            return p;
        }

        public static Label FieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = FontLabel,
                ForeColor = ColorTranslator.FromHtml("#64748b"),
                AutoSize = true,
                Dock = DockStyle.Top
            };
        }

        public static TextBox Field()
        {
            var t = new TextBox
            {
                Font = FontBase,
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ColorTranslator.FromHtml("#f8fafc")
            };
            return t;
        }

        public static ComboBox Combo()
        {
            return new ComboBox
            {
                Font = FontBase,
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
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

        public static void ApplyStatusColor(DataGridViewCellFormattingEventArgs e, string colName, DataGridView dgv)
        {
            if (e.ColumnIndex < 0 || e.Value == null) return;
            if (dgv.Columns[e.ColumnIndex].Name != colName) return;
            switch (e.Value.ToString())
            {
                case "Confirmada":
                case "Ativa":
                case "Pago":
                    e.CellStyle.BackColor = ColorTranslator.FromHtml("#dcfce7");
                    e.CellStyle.ForeColor = ColorTranslator.FromHtml("#15803d");
                    break;
                case "Pendente":
                    e.CellStyle.BackColor = ColorTranslator.FromHtml("#fef9c3");
                    e.CellStyle.ForeColor = ColorTranslator.FromHtml("#854d0e");
                    break;
                case "Cancelada":
                case "Cancelado":
                    e.CellStyle.BackColor = ColorTranslator.FromHtml("#fee2e2");
                    e.CellStyle.ForeColor = ColorTranslator.FromHtml("#b91c1c");
                    break;
                case "Concluida":
                case "Terminada":
                    e.CellStyle.BackColor = ColorTranslator.FromHtml("#f1f5f9");
                    e.CellStyle.ForeColor = ColorTranslator.FromHtml("#475569");
                    break;
                case "Suspensa":
                    e.CellStyle.BackColor = ColorTranslator.FromHtml("#fef3c7");
                    e.CellStyle.ForeColor = ColorTranslator.FromHtml("#92400e");
                    break;
            }
        }

        public static void ShowSuccess(Label lbl, string msg = "Guardado com sucesso.")
        {
            lbl.Text      = msg;
            lbl.ForeColor = ColorTranslator.FromHtml("#15803d");
            lbl.Visible   = true;
            var t = new System.Windows.Forms.Timer { Interval = 2500 };
            t.Tick += (s, e2) => { lbl.Visible = false; t.Stop(); t.Dispose(); };
            t.Start();
        }
    }
}
