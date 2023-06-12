using qgrepControls.Classes;
using System.Collections.Generic;
using System.Windows;

namespace qgrepControls.SearchWindow
{
    public partial class HotkeysWindow : System.Windows.Controls.UserControl
    {
        private class HotkeyComboBoxItem
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public qgrepSearchWindowControl SearchWindow;
        public MainWindow Dialog = null;
        public bool IsOk = false;
        public bool OpenSettings = false;
        public Dictionary<string, Hotkey> hotkeys = null;
        public Dictionary<string, Hotkey> readOnlyHotkeys = null;

        public HotkeysWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();

            Dictionary<string, Hotkey> bindings = null;
            if (SearchWindow.WrapperApp.CanEditKeyBindings())
            {
                bindings = SearchWindow.WrapperApp.ReadKeyBindings();
            }

            if (bindings != null)
            {
                hotkeys = new Dictionary<string, Hotkey>();

                ToggleCaseSensitive.Hotkey = bindings[ToggleCaseSensitive.Name];
                ToggleWholeWord.Hotkey = bindings[ToggleWholeWord.Name];
                ToggleRegEx.Hotkey = bindings[ToggleRegEx.Name];
                ToggleIncludeFiles.Hotkey = bindings[ToggleIncludeFiles.Name];
                ToggleExcludeFiles.Hotkey = bindings[ToggleExcludeFiles.Name];
                ToggleFilterResults.Hotkey = bindings[ToggleFilterResults.Name];
                ShowHistory.Hotkey = bindings[ShowHistory.Name];
                ToggleGroupBy.Hotkey = bindings[ToggleGroupBy.Name];
                ToggleGroupExpand.Hotkey = bindings[ToggleGroupExpand.Name];

                if (bindings["View.qgrepSearchFile"].IsGlobal)
                {
                    OpenFileSearch.Hotkey = bindings["View.qgrepSearchFile"];
                }

                if(!SearchWindow.WrapperApp.IsStandalone)
                {
                    if (bindings["View.qgrepSearchTool"].IsGlobal)
                    {
                        OpenToolWindow.Hotkey = bindings["View.qgrepSearchTool"];
                    }
                }
                else
                {
                    ToolWindowRow.Height = new GridLength(0);
                    GlobalHotkeysRow.Height = new GridLength(0);
                    LocalHotkeysRow.Height = new GridLength(2);
                }

                for (int i = 0; i < 9; i++)
                {
                    string toggleKey = $"ToggleSearchFilter{i + 1}";
                    string selectKey = $"SelectSearchFilter{i + 1}";

                    hotkeys[toggleKey] = bindings[toggleKey];
                    hotkeys[selectKey] = bindings[selectKey];
                }

                Settings.Visibility = Visibility.Collapsed;
            }
            else
            {
                readOnlyHotkeys = new Dictionary<string, Hotkey>();
                Dictionary<string, string> readOnlyBindings = SearchWindow.WrapperApp.ReadKeyBindingsReadOnly();

                ToggleCaseSensitive.Hotkey = new Hotkey(readOnlyBindings[ToggleCaseSensitive.Name]);
                ToggleWholeWord.Hotkey = new Hotkey(readOnlyBindings[ToggleWholeWord.Name]);
                ToggleRegEx.Hotkey = new Hotkey(readOnlyBindings[ToggleRegEx.Name]);
                ToggleIncludeFiles.Hotkey = new Hotkey(readOnlyBindings[ToggleIncludeFiles.Name]);
                ToggleExcludeFiles.Hotkey = new Hotkey(readOnlyBindings[ToggleExcludeFiles.Name]);
                ToggleFilterResults.Hotkey = new Hotkey(readOnlyBindings[ToggleFilterResults.Name]);
                ShowHistory.Hotkey = new Hotkey(readOnlyBindings[ShowHistory.Name]);
                ToggleGroupBy.Hotkey = new Hotkey(readOnlyBindings[ToggleGroupBy.Name]);
                ToggleGroupExpand.Hotkey = new Hotkey(readOnlyBindings[ToggleGroupExpand.Name]);

                OpenFileSearch.Hotkey = new Hotkey(readOnlyBindings["View.qgrepSearchFile"]);
                OpenToolWindow.Hotkey = new Hotkey(readOnlyBindings["View.qgrepSearchTool"]);

                for (int i = 0; i < 9; i++)
                {
                    string toggleKey = $"ToggleSearchFilter{i + 1}";
                    string selectKey = $"SelectSearchFilter{i + 1}";

                    readOnlyHotkeys[toggleKey] = new Hotkey(readOnlyBindings[toggleKey]);
                    readOnlyHotkeys[selectKey] = new Hotkey(readOnlyBindings[selectKey]);
                }

                Settings.Visibility = Visibility.Visible;
                OK.Visibility = Visibility.Collapsed;
                Cancel.Visibility = Visibility.Collapsed;
            }

            for (int i = 0; i < 9; i++)
            {
                string toggleKey = $"ToggleSearchFilter{i + 1}";
                string selectKey = $"SelectSearchFilter{i + 1}";

                ToggleSearchFilterComboBox.Items.Add(new HotkeyComboBoxItem() { Key = toggleKey, Value = string.Format(Properties.Resources.ToggleSearchFilter, i + 1) });
                SelectSearchFilterComboBox.Items.Add(new HotkeyComboBoxItem() { Key = selectKey, Value = string.Format(Properties.Resources.SelectSearchFilter, i + 1) });
            }

            ToggleSearchFilterComboBox.SelectedIndex = 0;
            SelectSearchFilterComboBox.SelectedIndex = 0;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            IsOk = true;

            if (Dialog != null)
            {
                Dialog.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (Dialog != null)
            {
                Dialog.Close();
            }
        }

        public Dictionary<string, Hotkey> GetBindings()
        {
            Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();
            bindings[ToggleCaseSensitive.Name] = ToggleCaseSensitive.Hotkey;
            bindings[ToggleWholeWord.Name] = ToggleWholeWord.Hotkey;
            bindings[ToggleRegEx.Name] = ToggleRegEx.Hotkey;
            bindings[ToggleIncludeFiles.Name] = ToggleIncludeFiles.Hotkey;
            bindings[ToggleExcludeFiles.Name] = ToggleExcludeFiles.Hotkey;
            bindings[ToggleFilterResults.Name] = ToggleFilterResults.Hotkey;
            bindings[ShowHistory.Name] = ShowHistory.Hotkey;
            bindings[ToggleGroupBy.Name] = ToggleGroupBy.Hotkey;
            bindings[ToggleGroupExpand.Name] = ToggleGroupExpand.Hotkey;

            bindings["View.qgrepSearchFile"] = OpenFileSearch.Hotkey;
            bindings["View.qgrepSearchFile"].IsGlobal = true;

            if(!SearchWindow.WrapperApp.IsStandalone)
            {
                bindings["View.qgrepSearchTool"] = OpenToolWindow.Hotkey;
                bindings["View.qgrepSearchTool"].IsGlobal = true;
            }

            foreach (KeyValuePair<string, Hotkey> hotkey in hotkeys)
            {
                bindings[hotkey.Key] = hotkey.Value;
            }

            HotkeyComboBoxItem toggleHotkey = ToggleSearchFilterComboBox.SelectedItem as HotkeyComboBoxItem;
            if (toggleHotkey != null)
            {
                bindings[toggleHotkey.Key] = ToggleSearchFilter.Hotkey;
            }

            HotkeyComboBoxItem selectHotkey = SelectSearchFilterComboBox.SelectedItem as HotkeyComboBoxItem;
            if (selectHotkey != null)
            {
                bindings[selectHotkey.Key] = SelectSearchFilter.Hotkey;
            }

            return bindings;
        }

        private void ToggleSearchFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(e.RemovedItems.Count > 0)
            {
                HotkeyComboBoxItem oldHotkey = e.RemovedItems[0] as HotkeyComboBoxItem;
                if(oldHotkey != null)
                {
                    if(hotkeys != null)
                    {
                        hotkeys[oldHotkey.Key] = new Hotkey(ToggleSearchFilter.Hotkey.Key, ToggleSearchFilter.Hotkey.Modifiers);
                    }
                }
            }

            if(e.AddedItems.Count > 0)
            {
                HotkeyComboBoxItem newHotkey = e.AddedItems[0] as HotkeyComboBoxItem;
                if(newHotkey != null)
                {
                    if (hotkeys != null)
                    {
                        ToggleSearchFilter.Hotkey = hotkeys[newHotkey.Key];
                    }
                    else if(readOnlyHotkeys != null)
                    {
                        ToggleSearchFilter.Hotkey = readOnlyHotkeys[newHotkey.Key];
                    }
                }
            }
        }

        private void SelectSearchFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                HotkeyComboBoxItem oldHotkey = e.RemovedItems[0] as HotkeyComboBoxItem;
                if (oldHotkey != null)
                {
                    if (hotkeys != null)
                    {
                        hotkeys[oldHotkey.Key] = new Hotkey(SelectSearchFilter.Hotkey.Key, SelectSearchFilter.Hotkey.Modifiers);
                    }
                }
            }

            if (e.AddedItems.Count > 0)
            {
                HotkeyComboBoxItem newHotkey = e.AddedItems[0] as HotkeyComboBoxItem;
                if (newHotkey != null)
                {
                    if (hotkeys != null)
                    {
                        SelectSearchFilter.Hotkey = hotkeys[newHotkey.Key];
                    }
                    else if(readOnlyHotkeys != null)
                    {
                        SelectSearchFilter.Hotkey = readOnlyHotkeys[newHotkey.Key];
                    }
                }
            }
        }

        private void HotkeyChanged(object sender, System.EventArgs e)
        {
            List<string> conflicts = SearchWindow.WrapperApp.GetConflictingCommandsForBinding(GetBindings());
            if(conflicts.Count > 0)
            {
                WarningText.Visibility = Visibility.Visible;
                WarningText.Text = Properties.Resources.CommandsOverwritten;

                foreach(string conflict in conflicts)
                {
                    WarningText.Text += "\n" + conflict;
                }
            }
            else
            {
                WarningText.Visibility = Visibility.Collapsed;
                WarningText.Text = "";
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings = true;

            if (Dialog != null)
            {
                Dialog.Close();
            }
        }
    }
}
