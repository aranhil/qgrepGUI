using Newtonsoft.Json;
using qgrepControls.Classes;
using qgrepControls.ModelViews;
using qgrepControls.Properties;
using qgrepControls.SearchWindow;
using qgrepControls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace qgrepControls.ColorsWindow
{
    public class ColorSchemeOverrides
    {
        public string Name = "";
        public ObservableCollection<ColorOverride> ColorOverrides = new ObservableCollection<ColorOverride>();
    }

    public class ComboBoxColorItem: INotifyPropertyChanged
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
        public new qgrepSearchWindowControl Parent;
        private List<ColorSchemeOverrides> colorSchemeOverrides = new List<ColorSchemeOverrides>();
        public ObservableCollection<ColorOverride> CurrentColorOverrides = new ObservableCollection<ColorOverride>();

        public ColorsWindow(qgrepSearchWindowControl Parent)
        {
            this.Parent = Parent;
            InitializeComponent();

            LoadFromSettings();

            foreach (ColorScheme colorScheme in Parent.colorSchemes)
            {
                if (Parent.ExtensionInterface.IsStandalone && colorScheme.Name == "Auto")
                    continue;

                ColorSchemeComboBox.Items.Add(new ComboBoxItem() { Content = colorScheme.Name, });
            }

            ColorSchemeComboBox.SelectedIndex = Settings.Default.ColorScheme - (Parent.ExtensionInterface.IsStandalone ? 1 : 0);

            ColorsListBox.ItemEditType = ConfigListBox.EditType.Custom;
            ColorsListBox.Title.Text = "Color overrides";
            ColorsListBox.AddButton.ToolTip = "Add new color override";
            ColorsListBox.EditButton.ToolTip = "Edit color override";
            ColorsListBox.RemoveButton.ToolTip = "Remove selected color override(s)";
            ColorsListBox.RemoveAllButton.ToolTip = "Remove all color overrides";
            ColorsListBox.AddButton.Click += AddNewColor_Click;
            ColorsListBox.OnEditClicked += EditColor_Click;

            Parent.LoadColorsFromResources(this);
        }

        private void EditColor_Click()
        {
            ColorOverride selectedOverride = ColorsListBox.InnerListBox.SelectedItem as ColorOverride;
            OverrideWindow overrideWindow = ShowOverrideDialog(selectedOverride.Name);

            if (overrideWindow.IsOK)
            {
                selectedOverride.Color = qgrepSearchWindowControl.ConvertColor(overrideWindow.OverrideColor.SelectedColor.GetValueOrDefault(new Color()));
                Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                SaveToSettings();
            }
        }

        private void SaveToSettings()
        {
            Settings.Default.Save();

            Parent.LoadColorsFromResources(this);
            Parent.LoadColorsFromResources(ColorsListBox);
            Parent.UpdateColorsFromSettings();
        }

        private void AddNewColor_Click(object sender, RoutedEventArgs e)
        {
            OverrideWindow overrideWindow = ShowOverrideDialog();

            if (overrideWindow.IsOK)
            {
                ComboBoxColorItem selectedColor = overrideWindow.OverrideName.SelectedItem as ComboBoxColorItem;

                CurrentColorOverrides.Add(new ColorOverride(selectedColor.Name, 
                    qgrepSearchWindowControl.ConvertColor(overrideWindow.OverrideColor.SelectedColor.GetValueOrDefault(new Color()))));

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
        }

        private void ColorSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.ColorScheme = ColorSchemeComboBox.SelectedIndex + (Parent.ExtensionInterface.IsStandalone ? 1 : 0);
            SaveToSettings();

            CurrentColorOverrides.CollectionChanged -= ColorOverrides_CollectionChanged;

            CurrentColorOverrides = colorSchemeOverrides[ColorSchemeComboBox.SelectedIndex].ColorOverrides;
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
            ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];

            OverrideWindow overrideWindow = new OverrideWindow(this);

            List<ComboBoxColorItem> uniqueColors = new List<ComboBoxColorItem>();
            Color initialColor = new Color();

            foreach (ColorEntry colorEntry in currentScheme.ColorEntries)
            {
                if (!uniqueColors.Exists(x => x.Name == colorEntry.Name))
                {
                    if (uniqueColors.Count == 0)
                    {
                        initialColor = qgrepSearchWindowControl.ConvertColor(colorEntry.Color);
                    }

                    uniqueColors.Add(new ComboBoxColorItem() { Name = colorEntry.Name, Color = colorEntry.Color });
                }
            }

            foreach (VsColorEntry colorEntry in currentScheme.VsColorEntries)
            {
                if (!uniqueColors.Exists(x => x.Name == colorEntry.Name))
                {
                    System.Drawing.Color extensionColor = Parent.ExtensionInterface.GetColor(colorEntry.Color);

                    if (uniqueColors.Count == 0)
                    {
                        initialColor = qgrepSearchWindowControl.ConvertColor(extensionColor);
                    }

                    uniqueColors.Add(new ComboBoxColorItem() { Name = colorEntry.Name, Color = extensionColor });

                    Debug.Write($"Name: {colorEntry.Name}, ");
                    Debug.WriteLine($"Color: {extensionColor}");

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
                overrideWindow.OverrideColor.SelectedColor = qgrepSearchWindowControl.ConvertColor(selectedItem.Color);
            }

            MainWindow overrideDialog = Parent.CreateWindow(overrideWindow, isEditing ? "Edit color override" : "Add color override", this);
            overrideWindow.Dialog = overrideDialog;
            overrideDialog.ShowDialog();
            return overrideWindow;
        }

        //private void AddOverride()
        //{
        //    ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];
        //    OverrideWindow overrideWindow = ShowOverrideDialog();

        //    if (overrideWindow.IsOK)
        //    {
        //        foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
        //        {
        //            if (schemeOverrides.Name == currentScheme.Name)
        //            {
        //                ComboBoxColorItem selectedColor = overrideWindow.OverrideName.SelectedItem as ComboBoxColorItem;

        //                schemeOverrides.ColorOverrides.Add(new ColorOverride()
        //                {
        //                    Name = selectedColor.Name,
        //                    Color = qgrepSearchWindowControl.ConvertColor(overrideWindow.OverrideColor.SelectedColor.GetValueOrDefault(new Color()))
        //                });
        //                Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
        //                Settings.Default.Save();

        //                LoadFromSettings();
        //                Parent.LoadColorsFromResources(this);
        //                Parent.UpdateColorsFromSettings();
        //                break;
        //            }
        //        }
        //    }
        //}

        //public void DeleteOverride(OverrideRow overrideRow)
        //{
        //    ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];

        //    foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
        //    {
        //        if (schemeOverrides.Name == currentScheme.Name)
        //        {
        //            schemeOverrides.ColorOverrides.RemoveAll(x => x.Name == overrideRow.Data.Name);

        //            Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
        //            Settings.Default.Save();

        //            LoadFromSettings();
        //            Parent.LoadColorsFromResources(this);
        //            Parent.UpdateColorsFromSettings();
        //            break;
        //        }
        //    }
        //}

        //public void EditOverride(OverrideRow overrideRow)
        //{
        //    ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];
        //    OverrideWindow overrideWindow = ShowOverrideDialog(overrideRow.Data.Name);

        //    if (overrideWindow.IsOK)
        //    {
        //        foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
        //        {
        //            if (schemeOverrides.Name == currentScheme.Name)
        //            {
        //                ComboBoxColorItem selectedColor = overrideWindow.OverrideName.SelectedItem as ComboBoxColorItem;
        //                ColorOverride selectedOverride = schemeOverrides.ColorOverrides.Find(x => x.Name == selectedColor.Name);
        //                if (selectedOverride != null)
        //                {
        //                    selectedOverride.Color = qgrepSearchWindowControl.ConvertColor(overrideWindow.OverrideColor.SelectedColor.GetValueOrDefault(new Color()));

        //                    Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
        //                    Settings.Default.Save();

        //                    LoadFromSettings();
        //                    Parent.LoadColorsFromResources(this);
        //                    Parent.UpdateColorsFromSettings();
        //                }

        //                break;
        //            }
        //        }
        //    }
        //}

        //public List<ColorOverride> GetCurrentColorOverrides()
        //{
        //    ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];

        //    foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
        //    {
        //        if (schemeOverrides.Name == currentScheme.Name)
        //        {
        //            return schemeOverrides.ColorOverrides;
        //        }
        //    }

        //    return null;
        //}

        //private void UpdateHint()
        //{
        //    OverridesHint.Visibility = OverridesPanel.Children.Count > 1 ? Visibility.Collapsed : Visibility.Visible;
        //}

        //private void AddNewOverride_Click(object sender, RoutedEventArgs e)
        //{
        //    AddOverride();
        //}

        //private void DeleteAllOverride_Click(object sender, RoutedEventArgs e)
        //{
        //    ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];

        //    foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
        //    {
        //        if (schemeOverrides.Name == currentScheme.Name)
        //        {
        //            schemeOverrides.ColorOverrides.Clear();

        //            Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
        //            Settings.Default.Save();

        //            LoadFromSettings();
        //            Parent.LoadColorsFromResources(this);
        //            Parent.UpdateColorsFromSettings();
        //            break;
        //        }
        //    }
        //}
    }
}
