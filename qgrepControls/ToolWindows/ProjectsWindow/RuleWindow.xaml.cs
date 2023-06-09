﻿using qgrepControls.Classes;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace qgrepControls.SearchWindow
{
    public partial class RuleWindow : System.Windows.Controls.UserControl
    {
        public MainWindow Dialog = null;
        public delegate void Callback(bool accepted);
        public bool IsOK = false;

        public RuleWindow(IWrapperApp WrapperApp)
        {
            InitializeComponent();
            ThemeHelper.UpdateColorsFromSettings(this, WrapperApp);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            IsOK = true;
            if (Dialog != null)
            {
                Dialog.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsOK = false;
            if (Dialog != null)
            {
                Dialog.Close();
            }
        }

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                IsOK = true;
                if (Dialog != null)
                {
                    Dialog.Close();
                }
            }
        }

        private void PredefinedButton_Click(object sender, RoutedEventArgs e)
        {
            PredefinedPopup.IsOpen = !PredefinedPopup.IsOpen;
            RegExTextBox.SelectAll();
        }

        private void ComboBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PredefinedPopup.IsOpen = false;
            ComboBoxItem comboBox = sender as ComboBoxItem;
            if (comboBox != null)
            {
                RegExTextBox.Text = comboBox.Tag as String;
            }
        }
    }
}
