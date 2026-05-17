using System.Drawing;

namespace CoworkingApp
{
    /// <summary>
    /// Paleta + tipografia + raios para o tema "dark glassmorphism / neon".
    /// Usado pelos NeonPanel/NeonButton/NeonTextBox e pelo FormLogin redesenhado.
    /// </summary>
    public static class NeonStyle
    {
        // ── Background ───────────────────────────────────────────────────
        // Quase preto, com leve tinta azul-violeta → dá profundidade ao neon.
        public static readonly Color BgDeep    = Color.FromArgb(0x08, 0x08, 0x12);
        public static readonly Color BgBase    = Color.FromArgb(0x0a, 0x0a, 0x14);
        public static readonly Color BgRaised  = Color.FromArgb(0x14, 0x14, 0x1f);
        public static readonly Color CardBg    = Color.FromArgb(0xc0, 0x18, 0x18, 0x28);
        public static readonly Color CardInner = Color.FromArgb(0x60, 0x28, 0x28, 0x40);

        // ── Neon accents ─────────────────────────────────────────────────
        public static readonly Color NeonCyan    = Color.FromArgb(0x00, 0xd9, 0xff);
        public static readonly Color NeonMagenta = Color.FromArgb(0xff, 0x2d, 0xd4);
        public static readonly Color NeonViolet  = Color.FromArgb(0x8b, 0x5c, 0xf6);
        public static readonly Color NeonGreen   = Color.FromArgb(0x00, 0xff, 0x88);
        public static readonly Color NeonRed     = Color.FromArgb(0xff, 0x40, 0x81);

        // ── Text ─────────────────────────────────────────────────────────
        public static readonly Color TextPrimary   = Color.FromArgb(0xff, 0xff, 0xff);
        public static readonly Color TextSecondary = Color.FromArgb(0xa1, 0xa1, 0xc4);
        public static readonly Color TextMuted     = Color.FromArgb(0x6b, 0x6b, 0x8c);
        public static readonly Color TextDisabled  = Color.FromArgb(0x44, 0x44, 0x5c);

        // ── Borders ──────────────────────────────────────────────────────
        public static readonly Color BorderSubtle = Color.FromArgb(0x30, 0xff, 0xff, 0xff);
        public static readonly Color BorderNeon   = NeonCyan;

        // ── Tipografia ───────────────────────────────────────────────────
        private const string FontFamily = "Segoe UI";

        public static readonly Font FontHero      = new Font(FontFamily, 26f, FontStyle.Bold);
        public static readonly Font FontTitle     = new Font(FontFamily, 18f, FontStyle.Bold);
        public static readonly Font FontSection   = new Font(FontFamily, 13f, FontStyle.Bold);
        public static readonly Font FontBody      = new Font(FontFamily, 10.5f);
        public static readonly Font FontBodyBold  = new Font(FontFamily, 10.5f, FontStyle.Bold);
        public static readonly Font FontCaption   = new Font(FontFamily, 9f);
        public static readonly Font FontCapsBold  = new Font(FontFamily, 8.5f, FontStyle.Bold);
        public static readonly Font FontButton    = new Font(FontFamily, 11f, FontStyle.Bold);
        public static readonly Font FontMono      = new Font("Consolas", 10f);

        // ── Cantos ───────────────────────────────────────────────────────
        public const int RadiusSm = 6;
        public const int RadiusMd = 10;
        public const int RadiusLg = 16;

        // ── Spacing scale (px @ 96 DPI) ─────────────────────────────────
        public const int Sp1 = 4;
        public const int Sp2 = 8;
        public const int Sp3 = 12;
        public const int Sp4 = 16;
        public const int Sp5 = 24;
        public const int Sp6 = 32;
        public const int Sp7 = 48;

        // ── Glow ─────────────────────────────────────────────────────────
        /// <summary>Quantas vezes desenhar para fakear o glow (mais = mais suave + mais lento).</summary>
        public const int GlowPasses = 6;
        public const int GlowSpread = 8;

        // ── Helpers ──────────────────────────────────────────────────────
        public static Color WithAlpha(Color c, int alpha)
            => Color.FromArgb(alpha, c.R, c.G, c.B);
    }
}
