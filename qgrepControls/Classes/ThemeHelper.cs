using Newtonsoft.Json;
using qgrepControls.ColorsWindow;
using qgrepControls.ModelViews;
using qgrepControls.Properties;
using qgrepControls.SearchWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace qgrepControls.Classes
{
    public sealed class ThemeHelper
    {
        private static ThemeHelper instance = null;
        private static readonly object padlock = new object();

        public static ThemeHelper Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ThemeHelper();
                    }
                    return instance;
                }
            }
        }

        public ColorScheme[] ColorSchemes = new ColorScheme[0];
        Dictionary<string, object> CachedResources;

        ThemeHelper()
        {
            string colorSchemesJson = System.Text.Encoding.Default.GetString(qgrepControls.Properties.Resources.colors_schemes);
            ColorSchemes = JsonConvert.DeserializeObject<ColorScheme[]>(colorSchemesJson);
        }

        public static Dictionary<string, object> GetResourcesFromColorScheme(IWrapperApp WrapperApp)
        {
            if(Instance.CachedResources == null)
            {
                UpdateResourcesFromColorScheme(WrapperApp);
            }

            return Instance.CachedResources;
        }

        public static void UpdateResourcesFromColorScheme(IWrapperApp WrapperApp)
        {
            if (WrapperApp.IsStandalone && Settings.Default.ColorScheme == 0)
            {
                Settings.Default.ColorScheme = 1;
                Settings.Default.Save();
            }

            Dictionary<string, object> resources = new Dictionary<string, object>();
            Dictionary<string, SolidColorBrush> brushes = new Dictionary<string, SolidColorBrush>();

            if (Settings.Default.ColorScheme < Instance.ColorSchemes.Length)
            {
                foreach (ColorEntry colorEntry in Instance.ColorSchemes[Settings.Default.ColorScheme].ColorEntries)
                {
                    brushes[colorEntry.Name] = new SolidColorBrush(ConvertColor(colorEntry.Color));
                }
                foreach (VsColorEntry colorEntry in Instance.ColorSchemes[Settings.Default.ColorScheme].VsColorEntries)
                {
                    brushes[colorEntry.Name] = new SolidColorBrush(ConvertColor(WrapperApp.GetColor(colorEntry.Color))) { Opacity = colorEntry.Opacity };
                }

                try
                {
                    List<ColorSchemeOverrides> colorSchemeOverrides = JsonConvert.DeserializeObject<List<ColorSchemeOverrides>>(Settings.Default.ColorOverrides);
                    foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
                    {
                        if (schemeOverrides.Name == Instance.ColorSchemes[Settings.Default.ColorScheme].Name)
                        {
                            foreach (ColorOverride colorOverride in schemeOverrides.ColorOverrides)
                            {
                                brushes[colorOverride.Name] = new SolidColorBrush(ConvertColor(colorOverride.Color));
                            }

                            break;
                        }
                    }
                }
                catch { }
            }

            foreach (KeyValuePair<string, SolidColorBrush> brush in brushes)
            {
                resources[brush.Key] = brush.Value;
                resources[brush.Key + ".Color"] = brush.Value.Color;
            }

            Instance.CachedResources = resources;
        }

        public static void UpdateColorsFromSettings(FrameworkElement userControl, IWrapperApp WrapperApp, bool isMainWindow = true)
        {
            Dictionary<string, object> resources = GetResourcesFromColorScheme(WrapperApp);
            MainWindow wrapperWindow = isMainWindow ? UIHelper.FindAncestor<MainWindow>(userControl) : null;

            foreach (var resource in resources)
            {
                userControl.Resources[resource.Key] = resource.Value;

                if (wrapperWindow != null)
                {
                    wrapperWindow.Resources[resource.Key] = resource.Value;
                }
            }
        }

        public static void UpdateFontFromSettings(FrameworkElement userControl, IWrapperApp WrapperApp)
        {
            if (WrapperApp.IsStandalone && Settings.Default.MonospaceFontFamily.Equals("Auto"))
            {
                string defaultFont = "Consolas";
                if (Fonts.SystemFontFamilies.Any(fontFamily => fontFamily.Source.Equals("Cascadia Mono", StringComparison.OrdinalIgnoreCase)))
                {
                    defaultFont = "Cascadia Mono";
                }

                Settings.Default.MonospaceFontFamily = defaultFont;
                Settings.Default.Save();
            }

            if (WrapperApp.IsStandalone && Settings.Default.NormalFontFamily.Equals("Auto"))
            {
                string defaultFont = "Arial";
                if (Fonts.SystemFontFamilies.Any(fontFamily => fontFamily.Source.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase)))
                {
                    defaultFont = "Segoe UI";
                }

                Settings.Default.NormalFontFamily = defaultFont;
                Settings.Default.Save();
            }

            if (Settings.Default.MonospaceFontFamily.Equals("Auto"))
            {
                userControl.Resources["MonospacedFontFamily"] = new FontFamily(WrapperApp.GetMonospaceFont());
            }
            else
            {
                userControl.Resources["MonospacedFontFamily"] = new FontFamily(Settings.Default.MonospaceFontFamily);
            }

            userControl.Resources["MonospacedFontSize"] = (double)Settings.Default.MonospaceFontSize;

            if (Settings.Default.NormalFontFamily.Equals("Auto"))
            {
                userControl.Resources["NormalFontFamily"] = new FontFamily(WrapperApp.GetNormalFont());
            }
            else
            {
                userControl.Resources["NormalFontFamily"] = new FontFamily(Settings.Default.NormalFontFamily);
            }

            userControl.Resources["NormalFontSize"] = (double)Settings.Default.NormalFontSize;
            userControl.Resources["GroupSize"] = (double)Settings.Default.GroupHeight;
            userControl.Resources["LineSize"] = (double)Settings.Default.LineHeight;
        }

        public static System.Windows.Media.Color ConvertColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Color ConvertColor(System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B); ;
        }

        public static void ClearCache()
        {
            Instance.CachedResources = null;
        }
    }
}
