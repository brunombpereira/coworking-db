using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace CoworkingApp
{
    public enum ThemeMode { Light, Dark }

    public static class ThemeManager
    {
        private static readonly string ConfigDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "CoworkingApp");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "theme.json");

        public static event Action ThemeChanged;
        public static ThemeMode Current { get; private set; } = ThemeMode.Light;

        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;
                var json = File.ReadAllText(ConfigPath);
                var data = new JavaScriptSerializer().Deserialize<ConfigData>(json);
                if (data != null && Enum.TryParse(data.Mode, out ThemeMode m))
                    Current = m;
            }
            catch { /* ignore — keep default Light */ }
        }

        public static void Toggle()
        {
            Current = Current == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
            Save();
            ThemeChanged?.Invoke();
        }

        public static void Set(ThemeMode mode)
        {
            if (mode == Current) return;
            Current = mode;
            Save();
            ThemeChanged?.Invoke();
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var json = new JavaScriptSerializer()
                    .Serialize(new ConfigData { Mode = Current.ToString() });
                File.WriteAllText(ConfigPath, json);
            }
            catch { /* ignore */ }
        }

        private class ConfigData { public string Mode { get; set; } }
    }
}
