﻿using Newtonsoft.Json;
using qgrepControls.Classes;
using qgrepControls.Properties;
using qgrepControls.SearchWindow;
using System;
using System.Collections.Generic;
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
    public class ColorOverride
    {
        public string Name = "";
        public System.Drawing.Color Color = new System.Drawing.Color();
    }

    public class ColorSchemeOverrides
    {
        public string Name = "";
        public List<ColorOverride> ColorOverrides = new List<ColorOverride>();
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

        public ColorsWindow(qgrepSearchWindowControl Parent)
        {
            this.Parent = Parent;
            InitializeComponent();

            foreach (ColorScheme colorScheme in Parent.colorSchemes)
            {
                if (Parent.ExtensionInterface.IsStandalone && colorScheme.Name == "Auto")
                    continue;

                ColorSchemeComboBox.Items.Add(new ComboBoxItem() { Content = colorScheme.Name, });
            }

            ColorSchemeComboBox.SelectedIndex = Settings.Default.ColorScheme - (Parent.ExtensionInterface.IsStandalone ? 1 : 0);

            LoadFromSettings();
            Parent.LoadColorsFromResources(this);
        }

        private void LoadFromSettings()
        {
            try
            {
                colorSchemeOverrides = JsonConvert.DeserializeObject<List<ColorSchemeOverrides>>(Settings.Default.ColorOverrides);
            }
            catch { }

            ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];

            OverridesPanel.Children.Clear();

            bool foundOverride = false;
            foreach (ColorSchemeOverrides colorSchemeOverrides in colorSchemeOverrides)
            {
                if(colorSchemeOverrides.Name == currentScheme.Name)
                {
                    foundOverride = true;
                    foreach (ColorOverride colorOverride in colorSchemeOverrides.ColorOverrides)
                    {
                        OverridesPanel.Children.Add(new OverrideRow(this, new OverrideRow.OverrideRowData(colorOverride.Name, colorOverride.Color)));
                    }
                }
            }

            if(!foundOverride)
            {
                colorSchemeOverrides.Add(new ColorSchemeOverrides() { Name = currentScheme.Name });

                Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                Settings.Default.Save();
            }

            OverridesPanel.Children.Add(new RowAdd(Parent, "Add new color override", new RowAdd.ClickCallbackFunction(AddOverride)));
            CheckAddButtonVisibility();
            UpdateHint();
        }
        private void ColorSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.ColorScheme = ColorSchemeComboBox.SelectedIndex + (Parent.ExtensionInterface.IsStandalone ? 1 : 0);
            Settings.Default.Save();
            Parent.UpdateColorsFromSettings();
            LoadFromSettings();
            Parent.LoadColorsFromResources(this);
        }

        private void OverridesPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowAddPanel(OverridesPanel);
        }

        private void OverridesPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAddPanel(OverridesPanel);
        }
        void HideAddPanel(StackPanel panel)
        {
            if (panel.Children.Count > 0)
            {
                panel.Children[panel.Children.Count - 1].Visibility = Visibility.Hidden;
            }
        }
        void ShowAddPanel(StackPanel panel)
        {
            if (panel.Children.Count > 0)
            {
                panel.Children[panel.Children.Count - 1].Visibility = Visibility.Visible;
            }
        }

        private void CheckAddButtonVisibility()
        {
            if (OverridesPanel.IsMouseOver)
            {
                ShowAddPanel(OverridesPanel);
            }
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
                        selectedItem = isEditing ? schemeOverrides.ColorOverrides.Find(x => x.Name == selectedName) : null;
                        break;
                    }
                }
            }

            overrideWindow.OverrideName.ItemsSource = uniqueColors;
            overrideWindow.CheckDuplicates = !isEditing;
            overrideWindow.OverrideName.IsEnabled = !isEditing;
            overrideWindow.OverrideName.SelectedIndex = selectedItem != null ? uniqueColors.FindIndex(x => x.Name == selectedItem.Name) : 0;

            if(selectedItem != null)
            {
                overrideWindow.OverrideColor.SelectedColor = qgrepSearchWindowControl.ConvertColor(selectedItem.Color);
            }

            MainWindow overrideDialog = Parent.CreateWindow(overrideWindow, isEditing ? "Edit color override" : "Add color override", this);
            overrideWindow.Dialog = overrideDialog;
            overrideDialog.ShowDialog();
            return overrideWindow;
        }

        private void AddOverride()
        {
            ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];
            OverrideWindow overrideWindow = ShowOverrideDialog();

            if (overrideWindow.IsOK)
            {
                foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
                {
                    if (schemeOverrides.Name == currentScheme.Name)
                    {
                        ComboBoxColorItem selectedColor = overrideWindow.OverrideName.SelectedItem as ComboBoxColorItem;

                        schemeOverrides.ColorOverrides.Add(new ColorOverride()
                        {
                            Name = selectedColor.Name,
                            Color = qgrepSearchWindowControl.ConvertColor(overrideWindow.OverrideColor.SelectedColor.GetValueOrDefault(new Color()))
                        });
                        Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                        Settings.Default.Save();

                        LoadFromSettings();
                        Parent.LoadColorsFromResources(this);
                        Parent.UpdateColorsFromSettings();
                        break;
                    }
                }
            }
        }

        public void DeleteOverride(OverrideRow overrideRow)
        {
            ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];

            foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
            {
                if (schemeOverrides.Name == currentScheme.Name)
                {
                    schemeOverrides.ColorOverrides.RemoveAll(x => x.Name == overrideRow.Data.Name);

                    Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                    Settings.Default.Save();

                    LoadFromSettings();
                    Parent.LoadColorsFromResources(this);
                    Parent.UpdateColorsFromSettings();
                    break;
                }
            }
        }

        public void EditOverride(OverrideRow overrideRow)
        {
            ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];
            OverrideWindow overrideWindow = ShowOverrideDialog(overrideRow.Data.Name);

            if (overrideWindow.IsOK)
            {
                foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
                {
                    if (schemeOverrides.Name == currentScheme.Name)
                    {
                        ComboBoxColorItem selectedColor = overrideWindow.OverrideName.SelectedItem as ComboBoxColorItem;
                        ColorOverride selectedOverride = schemeOverrides.ColorOverrides.Find(x => x.Name == selectedColor.Name);
                        if (selectedOverride != null)
                        {
                            selectedOverride.Color = qgrepSearchWindowControl.ConvertColor(overrideWindow.OverrideColor.SelectedColor.GetValueOrDefault(new Color()));

                            Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                            Settings.Default.Save();

                            LoadFromSettings();
                            Parent.LoadColorsFromResources(this);
                            Parent.UpdateColorsFromSettings();
                        }

                        break;
                    }
                }
            }
        }

        public List<ColorOverride> GetCurrentColorOverrides()
        {
            ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];

            foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
            {
                if (schemeOverrides.Name == currentScheme.Name)
                {
                    return schemeOverrides.ColorOverrides;
                }
            }

            return null;
        }

        private void UpdateHint()
        {
            OverridesHint.Visibility = OverridesPanel.Children.Count > 1 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AddNewOverride_Click(object sender, RoutedEventArgs e)
        {
            AddOverride();
        }

        private void DeleteAllOverride_Click(object sender, RoutedEventArgs e)
        {
            ColorScheme currentScheme = Parent.colorSchemes[Settings.Default.ColorScheme];

            foreach (ColorSchemeOverrides schemeOverrides in colorSchemeOverrides)
            {
                if (schemeOverrides.Name == currentScheme.Name)
                {
                    schemeOverrides.ColorOverrides.Clear();

                    Settings.Default.ColorOverrides = JsonConvert.SerializeObject(colorSchemeOverrides, Formatting.None);
                    Settings.Default.Save();

                    LoadFromSettings();
                    Parent.LoadColorsFromResources(this);
                    Parent.UpdateColorsFromSettings();
                    break;
                }
            }
        }
    }
}
