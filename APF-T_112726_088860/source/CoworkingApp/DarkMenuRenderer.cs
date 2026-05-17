using System.Drawing;
using System.Windows.Forms;

namespace CoworkingApp
{
    /// <summary>
    /// Renderer custom para ContextMenuStrip que respeita o tema dark da app.
    /// Usado no menu do avatar da sidebar (Perfil / Modo escuro/claro / Sair).
    /// </summary>
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(Point.Empty, e.Item.Size);
            Color bg = e.Item.Selected ? Theme.SidebarBgActive : Theme.CardBg;
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, rect);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.TextPrimary : Theme.TextMuted;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var rect = e.Item.Bounds;
            int y    = rect.Height / 2;
            using (var pen = new Pen(Theme.CardBorder))
                e.Graphics.DrawLine(pen, rect.Left + 24, y, rect.Right - 8, y);
        }
    }

    internal class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder                    => Theme.CardBorder;
        public override Color MenuItemBorder                => Color.Transparent;
        public override Color MenuItemSelected              => Theme.SidebarBgActive;
        public override Color MenuItemSelectedGradientBegin => Theme.SidebarBgActive;
        public override Color MenuItemSelectedGradientEnd   => Theme.SidebarBgActive;
        public override Color MenuItemPressedGradientBegin  => Theme.SidebarBgActive;
        public override Color MenuItemPressedGradientEnd    => Theme.SidebarBgActive;
        public override Color ToolStripDropDownBackground   => Theme.CardBg;
        public override Color ImageMarginGradientBegin      => Theme.CardBg;
        public override Color ImageMarginGradientMiddle     => Theme.CardBg;
        public override Color ImageMarginGradientEnd        => Theme.CardBg;
        public override Color SeparatorDark                 => Theme.CardBorder;
        public override Color SeparatorLight                => Theme.CardBorder;
    }
}
