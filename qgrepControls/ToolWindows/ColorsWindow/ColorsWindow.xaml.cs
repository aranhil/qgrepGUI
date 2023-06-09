﻿using Newtonsoft.Json;
using qgrepControls.Classes;
using qgrepControls.ModelViews;
using qgrepControls.Properties;
using qgrepControls.SearchWindow;
using qgrepControls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace qgrepControls.ColorsWindow
{
    public class ColorSchemeOverrides
    {
        public string Name = "";
        public ObservableCollection<ColorOverride> ColorOverrides = new ObservableCollection<ColorOverride>();
    }

    public class ComboBoxColorItem : INotifyPropertyChanged
    {
        private string _name = "";
        private System.Drawing.Color _color = new System.Drawing.Color();

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public System.Drawing.Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public partial class ColorsWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;
        private List<ColorSchemeOverrides> colorSchemeOverrides = new List<ColorSchemeOverrides>();
        public ObservableCollection<ColorOverride> CurrentColorOverrides = new ObservableCollection<ColorOverride>();

        public ColorsWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();

            LoadFromSettings();

            foreach (ColorScheme colorScheme in ThemeHelper.Instance.ColorSchemes)
            {
                if (SearchWindow.WrapperApp.IsStandalone && colorScheme.Name == Properties.Resources.AutoOptionContent)
                    continue;

                ColorSchemeComboBox.Items.Add(new ComboBoxItem() { Content = colorScheme.Name, });
            }

            if (!SearchWindow.WrapperApp.IsStandalone)
            {
                NormalFontComboBox.Items.Add(new ComboBoxItem() { Content = Properties.Resources.AutoOptionContent });
                MonospaceFontComboBox.Items.Add(new ComboBoxItem() { Content = Properties.Resources.AutoOptionContent });
            }

            List<FontFamily> fontFamilies = new List<FontFamily>(Fonts.SystemFontFamilies);
            fontFamilies = fontFamilies.OrderBy(family => family.Source).ToList();
            foreach (FontFamily family in fontFamilies)
            {
                NormalFontComboBox.Items.Add(new ComboBoxItem() { Content = family.Source });
                MonospaceFontComboBox.Items.Add(new ComboBoxItem() { Content = family.Source });
            }

            for (int i = 0; i < MonospaceFontComboBox.Items.Count; i++)
            {
                if ((MonospaceFontComboBox.Items[i] as ComboBoxItem).Content.Equals(Settings.Default.MonospaceFontFamily))
                {
                    MonospaceFontComboBox.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < NormalFontComboBox.Items.Count; i++)
            {
                if ((NormalFontComboBox.Items[i] as ComboBoxItem).Content.Equals(Settings.Default.NormalFontFamily))
                {
                    NormalFontComboBox.SelectedIndex = i;
                    break;
                }
            }

            NormalFontTextBox.Text = Settings.Default.NormalFontSize.ToString();
            MonospaceFontTextBox.Text = Settings.Default.MonospaceFontSize.ToString();
            GroupHeightTextBox.Text = Settings.Default.GroupHeight.ToString();
            LineHeightTextBox.Text = Settings.Default.LineHeight.ToString();

            ColorSchemeComboBox.SelectedIndex = Settings.Default.ColorScheme - (SearchWindow.WrapperApp.IsStandalone ? 1 : 0);

            ColorsListBox.ItemEditType = ConfigListBox.EditType.Custom;
            ColorsListBox.Title.Text = Properties.Resources.ColorOverridesTitle;
            ColorsListBox.AddButton.ToolTip = Properties.Resources.AddColorOverrideTooltip;
            ColorsListBox.EditButton.ToolTip = Properties.Resources.EditColorOverrideTooltip;
            ColorsListBox.RemoveButton.ToolTip = Properties.Resources.RemoveColorOverrideTooltip;
            ColorsListBox.RemoveAllButton.ToolTip = Properties.Resources.RemoveAllColorOverridesTooltip;
            ColorsListBox.AddButton.Click += AddNewColor_Click;
            ColorsListBox.OnEditClicked += EditColor_Click;
            ColorsListBox.IsDeselectable = true;

            ThemeHelper.UpdateColorsFromSettings(this, SearchWindow.WrapperApp);
        }

        private void EditColor_Click()
        {
            ColorOverride selectedOverride = ColorsListBox.InnerListBox.SelectedItem as ColorOverride;
            OverrideWindow overrideWindow = ShowOverrideDialog(selectedOverride.Name);

            if (overrideWindow.IsOK)
            {
                selectedOverride.Color = ThemeHelper.ConvertColor(overrideWindow.OverrideColor.SelectedColor.GetValueOrDefault(new Color()));
                Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                SaveToSettings();
            }
        }

        private void SaveToSettings()
        {
            Settings.Default.Save();

            ThemeHelper.ClearCache();
            ThemeHelper.UpdateColorsFromSettings(this, SearchWindow.WrapperApp);
            ThemeHelper.UpdateColorsFromSettings(ColorsListBox, SearchWindow.WrapperApp, false);
            ThemeHelper.UpdateColorsFromSettings(SearchWindow, SearchWindow.WrapperApp);
            SearchWindow.WrapperApp.RefreshResources(ThemeHelper.GetResourcesFromColorScheme(SearchWindow.WrapperApp));
        }

        private void AddNewColor_Click(object sender, RoutedEventArgs e)
        {
            OverrideWindow overrideWindow = ShowOverrideDialog();

            if (overrideWindow.IsOK)
            {
                ComboBoxColorItem selectedColor = overrideWindow.OverrideName.SelectedItem as ComboBoxColorItem;

                CurrentColorOverrides.Add(new ColorOverride(selectedColor.Name,
                    ThemeHelper.ConvertColor(overrideWindow.OverrideColor.SelectedColor.GetValueOrDefault(new Color()))));

                Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                SaveToSettings();
            }
        }

        private void LoadFromSettings()
        {
            try
            {
                colorSchemeOverrides = JsonConvert.DeserializeObject<List<ColorSchemeOverrides>>(Settings.Default.ColorOverrides);
            }
            catch { }

            if (colorSchemeOverrides.Count != ThemeHelper.Instance.ColorSchemes.Length)
            {
                colorSchemeOverrides.Clear();
                foreach (ColorScheme colorScheme in ThemeHelper.Instance.ColorSchemes)
                {
                    colorSchemeOverrides.Add(new ColorSchemeOverrides() { Name = colorScheme.Name });
                }

                Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                SaveToSettings();
            }
        }

        private void ColorSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.ColorScheme = ColorSchemeComboBox.SelectedIndex + (SearchWindow.WrapperApp.IsStandalone ? 1 : 0);
            SaveToSettings();

            CurrentColorOverrides.CollectionChanged -= ColorOverrides_CollectionChanged;

            CurrentColorOverrides = colorSchemeOverrides[Settings.Default.ColorScheme].ColorOverrides;
            ColorsListBox.SetItemsSource(CurrentColorOverrides);

            CurrentColorOverrides.CollectionChanged += ColorOverrides_CollectionChanged;
        }

        private void ColorOverrides_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
            SaveToSettings();
        }

        private OverrideWindow ShowOverrideDialog(string selectedName = null)
        {
            bool isEditing = selectedName != null;
            ColorScheme currentScheme = ThemeHelper.Instance.ColorSchemes[Settings.Default.ColorScheme];

            OverrideWindow overrideWindow = new OverrideWindow(this);

            List<ComboBoxColorItem> uniqueColors = new List<ComboBoxColorItem>();
            Color initialColor = new Color();

            foreach (ColorEntry colorEntry in currentScheme.ColorEntries)
            {
                if (!uniqueColors.Exists(x => x.Name == colorEntry.Name))
                {
                    if (uniqueColors.Count == 0)
                    {
                        initialColor = ThemeHelper.ConvertColor(colorEntry.Color);
                    }

                    uniqueColors.Add(new ComboBoxColorItem() { Name = colorEntry.Name, Color = colorEntry.Color });
                }
            }

            foreach (VsColorEntry colorEntry in currentScheme.VsColorEntries)
            {
                if (!uniqueColors.Exists(x => x.Name == colorEntry.Name))
                {
                    System.Drawing.Color extensionColor = SearchWindow.WrapperApp.GetColor(colorEntry.Color);

                    if (uniqueColors.Count == 0)
                    {
                        initialColor = ThemeHelper.ConvertColor(extensionColor);
                    }

                    uniqueColors.Add(new ComboBoxColorItem() { Name = colorEntry.Name, Color = extensionColor });

                    //Debug.Write($"Name: {colorEntry.Name}, ");
                    //Debug.WriteLine($"Color: {extensionColor}");
                }
            }

            uniqueColors = uniqueColors.OrderBy(x => x.Name).ToList();
            ColorOverride selectedItem = null;

            if (isEditing)
            {
                foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
                {
                    if (schemeOverrides.Name == currentScheme.Name)
                    {
                        selectedItem = isEditing ? schemeOverrides.ColorOverrides.First(x => x.Name == selectedName) : null;
                        break;
                    }
                }
            }

            overrideWindow.OverrideName.ItemsSource = uniqueColors;
            overrideWindow.CheckDuplicates = !isEditing;
            overrideWindow.OverrideName.IsEnabled = !isEditing;
            overrideWindow.OverrideName.SelectedIndex = selectedItem != null ? uniqueColors.FindIndex(x => x.Name == selectedItem.Name) : 0;

            if (selectedItem != null)
            {
                overrideWindow.OverrideColor.SelectedColor = ThemeHelper.ConvertColor(selectedItem.Color);
            }

            MainWindow overrideDialog = UIHelper.CreateWindow(overrideWindow,
                isEditing ? Properties.Resources.EditColorOverrideTooltip : Properties.Resources.AddColorOverrideTooltip, SearchWindow.WrapperApp, this);

            overrideWindow.Dialog = overrideDialog;
            overrideDialog.ShowDialog();
            return overrideWindow;
        }

        private void MonospaceFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.MonospaceFontFamily = (MonospaceFontComboBox.SelectedItem as ComboBoxItem).Content as string;
            Settings.Default.Save();

            ThemeHelper.UpdateFontFromSettings(SearchWindow, SearchWindow.WrapperApp);
        }

        private void NormalFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.NormalFontFamily = (NormalFontComboBox.SelectedItem as ComboBoxItem).Content as string;
            Settings.Default.Save();

            ThemeHelper.UpdateFontFromSettings(SearchWindow, SearchWindow.WrapperApp);
        }

        private void NormalFontTextBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int value;
            if (Int32.TryParse(NormalFontTextBox.Text, out value) && value > 0 && value < 50)
            {
                Settings.Default.NormalFontSize = value;
                Settings.Default.Save();

                ThemeHelper.UpdateFontFromSettings(SearchWindow, SearchWindow.WrapperApp);
            }
        }

        private void MonospaceFontTextBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int value;
            if (Int32.TryParse(MonospaceFontTextBox.Text, out value) && value > 0 && value < 50)
            {
                Settings.Default.MonospaceFontSize = value;
                Settings.Default.Save();

                ThemeHelper.UpdateFontFromSettings(SearchWindow, SearchWindow.WrapperApp);
            }
        }

        private void GroupHeightTextBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int value;
            if (Int32.TryParse(GroupHeightTextBox.Text, out value) && value > 0 && value < 50)
            {
                Settings.Default.GroupHeight = value;
                Settings.Default.Save();

                ThemeHelper.UpdateFontFromSettings(SearchWindow, SearchWindow.WrapperApp);
            }
        }

        private void LineHeightTextBox_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            int value;
            if (Int32.TryParse(LineHeightTextBox.Text, out value) && value > 0 && value < 50)
            {
                Settings.Default.LineHeight = value;
                Settings.Default.Save();

                ThemeHelper.UpdateFontFromSettings(SearchWindow, SearchWindow.WrapperApp);
            }
        }
    }
}
