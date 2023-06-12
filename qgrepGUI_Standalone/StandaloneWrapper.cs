using ControlzEx;
using Newtonsoft.Json;
using qgrepControls.Classes;
using qgrepControls.ModelViews;
using qgrepGUI.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace qgrepGUI
{
    class StandaloneWrapper : IWrapperApp
    {
        Window Window;
        public StandaloneWrapper(Window window)
        {
            Window = window;
        }

        public bool SearchWindowOpened 
        {
            get 
            {
                return false;
            }
            set
            {
            }
        }

        public bool IsStandalone
        {
            get
            {
                return true;
            }
        }

        public string GetSelectedText()
        {
            return "";
        }

        public string GetConfigPath(bool useGlobalPath)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "qgrepSearch");

            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            return appFolderPath;
        }

        public void OpenFile(string path, string line)
        {
            try
            {
                System.Diagnostics.Process.Start(path);
            }
            catch { }
        }

        public void GatherAllFoldersAndExtensionsFromSolution(MessageCallback extensionsList, MessageCallback folderCallback)
        {
        }

        public Color GetColor(string resourceKey)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            return Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
        }

        public void RefreshResources(Dictionary<string, object> newResources)
        {
            foreach(var resource in newResources)
            {
                Window.Resources[resource.Key] = resource.Value;
                Application.Current.Resources[resource.Key] = resource.Value;
            }
        }

        private Dictionary<string, Hotkey> LoadDefaultKeyBindings()
        {
            Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();
            MainWindow mainWindow = (Application.Current.MainWindow as MainWindow);
            bindings["ToggleCaseSensitive"] = new Hotkey(mainWindow.ToggleCaseSensitive.Key, mainWindow.ToggleCaseSensitive.Modifiers);
            bindings["ToggleWholeWord"] = new Hotkey(mainWindow.ToggleWholeWord.Key, mainWindow.ToggleWholeWord.Modifiers);
            bindings["ToggleRegEx"] = new Hotkey(mainWindow.ToggleRegEx.Key, mainWindow.ToggleRegEx.Modifiers);
            bindings["ToggleIncludeFiles"] = new Hotkey(mainWindow.ToggleIncludeFiles.Key, mainWindow.ToggleIncludeFiles.Modifiers);
            bindings["ToggleExcludeFiles"] = new Hotkey(mainWindow.ToggleExcludeFiles.Key, mainWindow.ToggleExcludeFiles.Modifiers);
            bindings["ToggleFilterResults"] = new Hotkey(mainWindow.ToggleFilterResults.Key, mainWindow.ToggleFilterResults.Modifiers);
            bindings["ShowHistory"] = new Hotkey(mainWindow.ShowHistory.Key, mainWindow.ShowHistory.Modifiers);
            bindings["OpenFileSearch"] = new Hotkey(mainWindow.OpenFileSearch.Key, mainWindow.OpenFileSearch.Modifiers);
            bindings["ToggleGroupBy"] = new Hotkey(mainWindow.ToggleGroupBy.Key, mainWindow.ToggleGroupBy.Modifiers);
            bindings["ToggleGroupExpand"] = new Hotkey(mainWindow.ToggleGroupExpand.Key, mainWindow.ToggleGroupExpand.Modifiers);
            bindings["ToggleSearchFilter1"] = new Hotkey(mainWindow.ToggleSearchFilter1.Key, mainWindow.ToggleSearchFilter1.Modifiers);
            bindings["ToggleSearchFilter2"] = new Hotkey(mainWindow.ToggleSearchFilter2.Key, mainWindow.ToggleSearchFilter2.Modifiers);
            bindings["ToggleSearchFilter3"] = new Hotkey(mainWindow.ToggleSearchFilter3.Key, mainWindow.ToggleSearchFilter3.Modifiers);
            bindings["ToggleSearchFilter4"] = new Hotkey(mainWindow.ToggleSearchFilter4.Key, mainWindow.ToggleSearchFilter4.Modifiers);
            bindings["ToggleSearchFilter5"] = new Hotkey(mainWindow.ToggleSearchFilter5.Key, mainWindow.ToggleSearchFilter5.Modifiers);
            bindings["ToggleSearchFilter6"] = new Hotkey(mainWindow.ToggleSearchFilter6.Key, mainWindow.ToggleSearchFilter6.Modifiers);
            bindings["ToggleSearchFilter7"] = new Hotkey(mainWindow.ToggleSearchFilter7.Key, mainWindow.ToggleSearchFilter7.Modifiers);
            bindings["ToggleSearchFilter8"] = new Hotkey(mainWindow.ToggleSearchFilter8.Key, mainWindow.ToggleSearchFilter8.Modifiers);
            bindings["ToggleSearchFilter9"] = new Hotkey(mainWindow.ToggleSearchFilter9.Key, mainWindow.ToggleSearchFilter9.Modifiers);
            bindings["SelectSearchFilter1"] = new Hotkey(mainWindow.SelectSearchFilter1.Key, mainWindow.SelectSearchFilter1.Modifiers);
            bindings["SelectSearchFilter2"] = new Hotkey(mainWindow.SelectSearchFilter2.Key, mainWindow.SelectSearchFilter2.Modifiers);
            bindings["SelectSearchFilter3"] = new Hotkey(mainWindow.SelectSearchFilter3.Key, mainWindow.SelectSearchFilter3.Modifiers);
            bindings["SelectSearchFilter4"] = new Hotkey(mainWindow.SelectSearchFilter4.Key, mainWindow.SelectSearchFilter4.Modifiers);
            bindings["SelectSearchFilter5"] = new Hotkey(mainWindow.SelectSearchFilter5.Key, mainWindow.SelectSearchFilter5.Modifiers);
            bindings["SelectSearchFilter6"] = new Hotkey(mainWindow.SelectSearchFilter6.Key, mainWindow.SelectSearchFilter6.Modifiers);
            bindings["SelectSearchFilter7"] = new Hotkey(mainWindow.SelectSearchFilter7.Key, mainWindow.SelectSearchFilter7.Modifiers);
            bindings["SelectSearchFilter8"] = new Hotkey(mainWindow.SelectSearchFilter8.Key, mainWindow.SelectSearchFilter8.Modifiers);
            bindings["SelectSearchFilter9"] = new Hotkey(mainWindow.SelectSearchFilter9.Key, mainWindow.SelectSearchFilter9.Modifiers);

            SaveKeyBindings(bindings);
            return bindings;
        }

        public Dictionary<string, Hotkey> ReadKeyBindings()
        {
            Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();

            if (Settings.Default.KeyBindings.Length == 0)
            {
                bindings = LoadDefaultKeyBindings();
            }
            else
            {
                try
                {
                    bindings = JsonConvert.DeserializeObject<Dictionary<string, Hotkey>>(Settings.Default.KeyBindings);
                }
                catch { }

                if (bindings.Count != 28)
                {
                    LoadDefaultKeyBindings();
                }
            }

            return bindings;
        }

        public void ApplyKeyBindings(Dictionary<string, Hotkey> bindings)
        {
            MainWindow mainWindow = (Application.Current.MainWindow as MainWindow);

            mainWindow.ToggleCaseSensitive.Key = bindings["ToggleCaseSensitive"].Key;
            mainWindow.ToggleCaseSensitive.Modifiers = bindings["ToggleCaseSensitive"].Modifiers;

            mainWindow.ToggleWholeWord.Key = bindings["ToggleWholeWord"].Key;
            mainWindow.ToggleWholeWord.Modifiers = bindings["ToggleWholeWord"].Modifiers;

            mainWindow.ToggleRegEx.Key = bindings["ToggleRegEx"].Key;
            mainWindow.ToggleRegEx.Modifiers = bindings["ToggleRegEx"].Modifiers;

            mainWindow.ToggleIncludeFiles.Key = bindings["ToggleIncludeFiles"].Key;
            mainWindow.ToggleIncludeFiles.Modifiers = bindings["ToggleIncludeFiles"].Modifiers;

            mainWindow.ToggleExcludeFiles.Key = bindings["ToggleExcludeFiles"].Key;
            mainWindow.ToggleExcludeFiles.Modifiers = bindings["ToggleExcludeFiles"].Modifiers;

            mainWindow.ToggleFilterResults.Key = bindings["ToggleFilterResults"].Key;
            mainWindow.ToggleFilterResults.Modifiers = bindings["ToggleFilterResults"].Modifiers;

            mainWindow.ShowHistory.Key = bindings["ShowHistory"].Key;
            mainWindow.ShowHistory.Modifiers = bindings["ShowHistory"].Modifiers;

            mainWindow.OpenFileSearch.Key = bindings["OpenFileSearch"].Key;
            mainWindow.OpenFileSearch.Modifiers = bindings["OpenFileSearch"].Modifiers;

            mainWindow.ToggleGroupBy.Key = bindings["ToggleGroupBy"].Key;
            mainWindow.ToggleGroupBy.Modifiers = bindings["ToggleGroupBy"].Modifiers;

            mainWindow.ToggleGroupExpand.Key = bindings["ToggleGroupExpand"].Key;
            mainWindow.ToggleGroupExpand.Modifiers = bindings["ToggleGroupExpand"].Modifiers;

            mainWindow.ToggleSearchFilter1.Key = bindings["ToggleSearchFilter1"].Key;
            mainWindow.ToggleSearchFilter1.Modifiers = bindings["ToggleSearchFilter1"].Modifiers;

            mainWindow.ToggleSearchFilter2.Key = bindings["ToggleSearchFilter2"].Key;
            mainWindow.ToggleSearchFilter2.Modifiers = bindings["ToggleSearchFilter2"].Modifiers;

            mainWindow.ToggleSearchFilter3.Key = bindings["ToggleSearchFilter3"].Key;
            mainWindow.ToggleSearchFilter3.Modifiers = bindings["ToggleSearchFilter3"].Modifiers;

            mainWindow.ToggleSearchFilter4.Key = bindings["ToggleSearchFilter4"].Key;
            mainWindow.ToggleSearchFilter4.Modifiers = bindings["ToggleSearchFilter4"].Modifiers;

            mainWindow.ToggleSearchFilter5.Key = bindings["ToggleSearchFilter5"].Key;
            mainWindow.ToggleSearchFilter5.Modifiers = bindings["ToggleSearchFilter5"].Modifiers;

            mainWindow.ToggleSearchFilter6.Key = bindings["ToggleSearchFilter6"].Key;
            mainWindow.ToggleSearchFilter6.Modifiers = bindings["ToggleSearchFilter6"].Modifiers;

            mainWindow.ToggleSearchFilter7.Key = bindings["ToggleSearchFilter7"].Key;
            mainWindow.ToggleSearchFilter7.Modifiers = bindings["ToggleSearchFilter7"].Modifiers;

            mainWindow.ToggleSearchFilter8.Key = bindings["ToggleSearchFilter8"].Key;
            mainWindow.ToggleSearchFilter8.Modifiers = bindings["ToggleSearchFilter8"].Modifiers;

            mainWindow.ToggleSearchFilter9.Key = bindings["ToggleSearchFilter9"].Key;
            mainWindow.ToggleSearchFilter9.Modifiers = bindings["ToggleSearchFilter9"].Modifiers;

            mainWindow.SelectSearchFilter1.Key = bindings["SelectSearchFilter1"].Key;
            mainWindow.SelectSearchFilter1.Modifiers = bindings["SelectSearchFilter1"].Modifiers;

            mainWindow.SelectSearchFilter2.Key = bindings["SelectSearchFilter2"].Key;
            mainWindow.SelectSearchFilter2.Modifiers = bindings["SelectSearchFilter2"].Modifiers;

            mainWindow.SelectSearchFilter3.Key = bindings["SelectSearchFilter3"].Key;
            mainWindow.SelectSearchFilter3.Modifiers = bindings["SelectSearchFilter3"].Modifiers;

            mainWindow.SelectSearchFilter4.Key = bindings["SelectSearchFilter4"].Key;
            mainWindow.SelectSearchFilter4.Modifiers = bindings["SelectSearchFilter4"].Modifiers;

            mainWindow.SelectSearchFilter5.Key = bindings["SelectSearchFilter5"].Key;
            mainWindow.SelectSearchFilter5.Modifiers = bindings["SelectSearchFilter5"].Modifiers;

            mainWindow.SelectSearchFilter6.Key = bindings["SelectSearchFilter6"].Key;
            mainWindow.SelectSearchFilter6.Modifiers = bindings["SelectSearchFilter6"].Modifiers;

            mainWindow.SelectSearchFilter7.Key = bindings["SelectSearchFilter7"].Key;
            mainWindow.SelectSearchFilter7.Modifiers = bindings["SelectSearchFilter7"].Modifiers;

            mainWindow.SelectSearchFilter8.Key = bindings["SelectSearchFilter8"].Key;
            mainWindow.SelectSearchFilter8.Modifiers = bindings["SelectSearchFilter8"].Modifiers;

            mainWindow.SelectSearchFilter9.Key = bindings["SelectSearchFilter9"].Key;
            mainWindow.SelectSearchFilter9.Modifiers = bindings["SelectSearchFilter9"].Modifiers;
        }

        public void SaveKeyBindings(Dictionary<string, Hotkey> bindings)
        {
            Settings.Default.KeyBindings = JsonConvert.SerializeObject(bindings);
            Settings.Default.Save();
        }

        public Window GetMainWindow()
        {
            return null;
        }

        public string GetMonospaceFont()
        {
            return "";
        }

        public string GetNormalFont()
        {
            return "";
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags
        );

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        public const uint SHGFI_ICON = 0x000000100;     // get icon
        public const uint SHGFI_LARGEICON = 0x000000000; // get large icon
        public const uint SHGFI_SMALLICON = 0x000000001; // get small icon
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        private Dictionary<string, BitmapSource> iconCache = new Dictionary<string, BitmapSource>();
        private readonly object lockObject = new object();

        public void GetFileIcon(string filePath, SearchResult searchResult)
        {
            string fileExtension = Path.GetExtension(filePath);

            if (iconCache.ContainsKey(fileExtension))
            {
                searchResult.ImageSource = iconCache[fileExtension];
            }
            else
            {
                Task.Run(() =>
                {
                    lock (lockObject)
                    {
                        BitmapSource iconSource = null;
                        TaskRunner.RunOnUIThread(() =>
                        {
                            iconSource = iconCache.ContainsKey(fileExtension) ? iconCache[fileExtension] : null;
                        });

                        if (iconSource != null)
                        {
                            searchResult.ImageSource = iconSource;
                        }
                        else
                        {
                            SHFILEINFO shfi = new SHFILEINFO();
                            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | SHGFI_SMALLICON;

                            SHGetFileInfo(filePath, FILE_ATTRIBUTE_NORMAL, ref shfi, (uint)System.Runtime.InteropServices.Marshal.SizeOf(shfi), flags);
                            System.Drawing.Icon icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(shfi.hIcon).Clone();

                            DestroyIcon(shfi.hIcon);

                            TaskRunner.RunOnUIThread(() =>
                            {
                                if (icon != null)
                                {
                                    iconCache[fileExtension] = GetBitmapSourceFromIcon(icon);
                                }

                                searchResult.ImageSource = iconCache[fileExtension];
                            });
                        }
                    }
                });
            }
        }

        public BitmapSource GetBitmapSourceFromIcon(Icon icon)
        {
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return bitmapSource;
        }

        public void GetIcon(string document, uint background, SearchResult searchResult)
        {
            try
            {
                GetFileIcon(document, searchResult);
            }
            catch { }
        }

        public void StartBackgroundTask(string title)
        {
        }

        public void UpdateBackgroundTaskPercentage(int progress)
        {
        }

        public void UpdateBackgroundTaskMessage(string message)
        {
        }

        public void StopBackgroundTask()
        {
        }

        public bool LoadConfigAtStartup()
        {
            return true;
        }

        public void IncludeFile(string path)
        {
        }

        public bool IsActiveDocumentCpp()
        {
            return false;
        }
    }
}
