using ControlzEx;
using Newtonsoft.Json;
using qgrepControls.Classes;
using qgrepGUI.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace qgrepGUI
{
    class qgrepExtension : IExtensionInterface
    {
        Window Window;
        public qgrepExtension(Window window)
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

        public string GetSolutionPath()
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

        public void GatherAllFoldersAndExtensionsFromSolution(StringCallback extensionsList, StringCallback folderCallback)
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

                if (bindings.Count != 10)
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

        public Icon GetFileIcon(string filePath)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            IntPtr hImgSmall = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);

            Icon icon = Icon.FromHandle(shinfo.hIcon);

            return icon;
        }
        public BitmapSource GetBitmapSourceFromIcon(Icon icon)
        {
            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return bitmapSource;
        }

        public BitmapSource GetIcon(string document, uint background)
        {
            try
            {
                return GetBitmapSourceFromIcon(GetFileIcon(document));
            }
            catch { return null; }
        }
    }
}
