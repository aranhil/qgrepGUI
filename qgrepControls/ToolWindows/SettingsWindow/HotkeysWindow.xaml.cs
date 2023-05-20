using qgrepControls.Classes;
using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace qgrepControls.SearchWindow
{
    public partial class HotkeysWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;
        public MainWindow Dialog = null;
        public bool IsOk = false;

        public HotkeysWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();

            Dictionary<string, Hotkey> bindings = SearchWindow.ExtensionInterface.ReadKeyBindings();
            ToggleCaseSensitive.Hotkey = new Classes.Hotkey(bindings[ToggleCaseSensitive.Name].Key, bindings[ToggleCaseSensitive.Name].Modifiers);
            ToggleWholeWord.Hotkey = new Classes.Hotkey(bindings[ToggleWholeWord.Name].Key, bindings[ToggleWholeWord.Name].Modifiers);
            ToggleRegEx.Hotkey = new Classes.Hotkey(bindings[ToggleRegEx.Name].Key, bindings[ToggleRegEx.Name].Modifiers);
            ToggleIncludeFiles.Hotkey = new Classes.Hotkey(bindings[ToggleIncludeFiles.Name].Key, bindings[ToggleIncludeFiles.Name].Modifiers);
            ToggleExcludeFiles.Hotkey = new Classes.Hotkey(bindings[ToggleExcludeFiles.Name].Key, bindings[ToggleExcludeFiles.Name].Modifiers);
            ToggleFilterResults.Hotkey = new Classes.Hotkey(bindings[ToggleFilterResults.Name].Key, bindings[ToggleFilterResults.Name].Modifiers);
            ShowHistory.Hotkey = new Classes.Hotkey(bindings[ShowHistory.Name].Key, bindings[ShowHistory.Name].Modifiers);
            OpenFileSearch.Hotkey = new Classes.Hotkey(bindings[OpenFileSearch.Name].Key, bindings[OpenFileSearch.Name].Modifiers);
            ToggleGroupingBy.Hotkey = new Classes.Hotkey(bindings[ToggleGroupingBy.Name].Key, bindings[ToggleGroupingBy.Name].Modifiers);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            IsOk = true;

            if (Dialog != null)
            {
                Dialog.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if(Dialog != null)
            {
                Dialog.Close();
            }
        }

        public Dictionary<string, Hotkey> GetBindings()
        {
            Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();
            bindings[ToggleCaseSensitive.Name] = new Hotkey(ToggleCaseSensitive.Hotkey.Key, ToggleCaseSensitive.Hotkey.Modifiers);
            bindings[ToggleWholeWord.Name] = new Hotkey(ToggleWholeWord.Hotkey.Key, ToggleWholeWord.Hotkey.Modifiers);
            bindings[ToggleRegEx.Name] = new Hotkey(ToggleRegEx.Hotkey.Key, ToggleRegEx.Hotkey.Modifiers);
            bindings[ToggleIncludeFiles.Name] = new Hotkey(ToggleIncludeFiles.Hotkey.Key, ToggleIncludeFiles.Hotkey.Modifiers);
            bindings[ToggleExcludeFiles.Name] = new Hotkey(ToggleExcludeFiles.Hotkey.Key, ToggleExcludeFiles.Hotkey.Modifiers);
            bindings[ToggleFilterResults.Name] = new Hotkey(ToggleFilterResults.Hotkey.Key, ToggleFilterResults.Hotkey.Modifiers);
            bindings[ShowHistory.Name] = new Hotkey(ShowHistory.Hotkey.Key, ShowHistory.Hotkey.Modifiers);
            bindings[OpenFileSearch.Name] = new Hotkey(OpenFileSearch.Hotkey.Key, OpenFileSearch.Hotkey.Modifiers);
            bindings[ToggleGroupingBy.Name] = new Hotkey(ToggleGroupingBy.Hotkey.Key, ToggleGroupingBy.Hotkey.Modifiers);
            return bindings;
        }
    }
}
