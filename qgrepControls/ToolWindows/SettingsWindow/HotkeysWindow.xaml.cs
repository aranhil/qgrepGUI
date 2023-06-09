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
        public Dictionary<string, Hotkey> hotkeys = new Dictionary<string, Hotkey>();

        public HotkeysWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();

            Dictionary<string, Hotkey> bindings = SearchWindow.WrapperApp.ReadKeyBindings();
            ToggleCaseSensitive.Hotkey = new Hotkey(bindings[ToggleCaseSensitive.Name].Key, bindings[ToggleCaseSensitive.Name].Modifiers);
            ToggleWholeWord.Hotkey = new Hotkey(bindings[ToggleWholeWord.Name].Key, bindings[ToggleWholeWord.Name].Modifiers);
            ToggleRegEx.Hotkey = new Hotkey(bindings[ToggleRegEx.Name].Key, bindings[ToggleRegEx.Name].Modifiers);
            ToggleIncludeFiles.Hotkey = new Hotkey(bindings[ToggleIncludeFiles.Name].Key, bindings[ToggleIncludeFiles.Name].Modifiers);
            ToggleExcludeFiles.Hotkey = new Hotkey(bindings[ToggleExcludeFiles.Name].Key, bindings[ToggleExcludeFiles.Name].Modifiers);
            ToggleFilterResults.Hotkey = new Hotkey(bindings[ToggleFilterResults.Name].Key, bindings[ToggleFilterResults.Name].Modifiers);
            ShowHistory.Hotkey = new Hotkey(bindings[ShowHistory.Name].Key, bindings[ShowHistory.Name].Modifiers);
            OpenFileSearch.Hotkey = new Hotkey(bindings[OpenFileSearch.Name].Key, bindings[OpenFileSearch.Name].Modifiers);
            ToggleGroupBy.Hotkey = new Hotkey(bindings[ToggleGroupBy.Name].Key, bindings[ToggleGroupBy.Name].Modifiers);
            ToggleGroupExpand.Hotkey = new Hotkey(bindings[ToggleGroupExpand.Name].Key, bindings[ToggleGroupExpand.Name].Modifiers);

            for (int i = 0; i < 9; i++)
            {
                string toggleKey = $"ToggleSearchFilter{i + 1}";
                string selectKey = $"SelectSearchFilter{i + 1}";

                hotkeys[toggleKey] = new Hotkey(bindings[toggleKey].Key, bindings[toggleKey].Modifiers);
                hotkeys[selectKey] = new Hotkey(bindings[selectKey].Key, bindings[selectKey].Modifiers);

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
            bindings[OpenFileSearch.Name] = OpenFileSearch.Hotkey;
            bindings[ToggleGroupBy.Name] = ToggleGroupBy.Hotkey;
            bindings[ToggleGroupExpand.Name] = ToggleGroupExpand.Hotkey;

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
            if (e.RemovedItems.Count > 0)
            {
                HotkeyComboBoxItem oldHotkey = e.RemovedItems[0] as HotkeyComboBoxItem;
                if (oldHotkey != null)
                {
                    hotkeys[oldHotkey.Key] = new Hotkey(ToggleSearchFilter.Hotkey.Key, ToggleSearchFilter.Hotkey.Modifiers);
                }
            }

            if (e.AddedItems.Count > 0)
            {
                HotkeyComboBoxItem newHotkey = e.AddedItems[0] as HotkeyComboBoxItem;
                if (newHotkey != null)
                {
                    ToggleSearchFilter.Hotkey = hotkeys[newHotkey.Key];
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
                    hotkeys[oldHotkey.Key] = new Hotkey(SelectSearchFilter.Hotkey.Key, SelectSearchFilter.Hotkey.Modifiers);
                }
            }

            if (e.AddedItems.Count > 0)
            {
                HotkeyComboBoxItem newHotkey = e.AddedItems[0] as HotkeyComboBoxItem;
                if (newHotkey != null)
                {
                    SelectSearchFilter.Hotkey = hotkeys[newHotkey.Key];
                }
            }
        }
    }
}
