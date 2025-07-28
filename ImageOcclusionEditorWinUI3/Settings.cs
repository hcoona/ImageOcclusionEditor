using Windows.Storage;

namespace ImageOcclusionEditorWinUI3
{
    internal static class Settings
    {
        private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

        public static bool Maximized
        {
            get => GetValue<bool>("Maximized", false);
            set => SetValue("Maximized", value);
        }

        public static string LocationX
        {
            get => GetValue<string>("LocationX", "0");
            set => SetValue("LocationX", value);
        }

        public static string LocationY
        {
            get => GetValue<string>("LocationY", "0");
            set => SetValue("LocationY", value);
        }

        public static string SizeWidth
        {
            get => GetValue<string>("SizeWidth", "1080");
            set => SetValue("SizeWidth", value);
        }

        public static string SizeHeight
        {
            get => GetValue<string>("SizeHeight", "720");
            set => SetValue("SizeHeight", value);
        }

        public static bool Minimized
        {
            get => GetValue<bool>("Minimized", false);
            set => SetValue("Minimized", value);
        }

        public static string StrokeColor
        {
            get => GetValue<string>("StrokeColor", "2D2D2D");
            set => SetValue("StrokeColor", value);
        }

        public static string StrokeWidth
        {
            get => GetValue<string>("StrokeWidth", "2");
            set => SetValue("StrokeWidth", value);
        }

        public static string FillColor
        {
            get => GetValue<string>("FillColor", "FFEBA2");
            set => SetValue("FillColor", value);
        }

        private static T GetValue<T>(string key, T defaultValue)
        {
            try
            {
                if (LocalSettings.Values.ContainsKey(key))
                {
                    return (T)LocalSettings.Values[key];
                }
            }
            catch
            {
                // Ignore errors and return default value
            }
            return defaultValue;
        }

        private static void SetValue<T>(string key, T value)
        {
            try
            {
                LocalSettings.Values[key] = value;
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}