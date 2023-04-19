using Microsoft.VisualStudio.PlatformUI;
using qgrepSearch.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace qgrepSearch.ToolWindows
{
    public partial class FileExtensionRow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;
        private FileExtensionsGroup ExtensionsGroup;

        private FileExtensionWindow FileExtensionWindow = null;
        private DialogWindow FileExtensionDialog = null;

        public FileExtensionRow(qgrepSearchWindowControl SearchWindow, FileExtensionsGroup ExtensionsGroup)
        {
            InitializeComponent();

            this.SearchWindow = SearchWindow;
            this.ExtensionsGroup = ExtensionsGroup;

            ExtensionName.Content = ExtensionsGroup.Name;
            ExtensionRegEx.Content = ExtensionsGroup.RegEx;

            Icons.Visibility = Visibility.Collapsed;
            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, System.Windows.Media.Color> colors = SearchWindow.GetColorsFromResources();

            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        private void FileExtensionsGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Visible;
            FileExtensionsGrid.Background = Resources["ResultHoverColor"] as SolidColorBrush;
        }

        private void FileExtensionsGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Collapsed;
            FileExtensionsGrid.Background = Resources["BackgroundColor"] as SolidColorBrush;
        }

        private void EditWindowResultCallback(bool accepted)
        {
            if(accepted)
            {
                if(FileExtensionWindow != null)
                {
                    ExtensionsGroup.Name = FileExtensionWindow.GroupName.Text;
                    ExtensionsGroup.RegEx = FileExtensionWindow.GroupRegEx.Text;
                    ExtensionsGroup.Edit();
                }
            }

            FileExtensionWindow = null;
            FileExtensionDialog.Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            FileExtensionWindow = new qgrepSearch.ToolWindows.FileExtensionWindow(SearchWindow, new FileExtensionWindow.Callback(EditWindowResultCallback));
            FileExtensionWindow.GroupName.Text = ExtensionsGroup.Name;
            FileExtensionWindow.GroupRegEx.Text = ExtensionsGroup.RegEx;

            FileExtensionDialog = new DialogWindow
            {
                Title = "Modify group",
                Content = FileExtensionWindow,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                HasMinimizeButton = false,
                HasMaximizeButton = false,
            };

            FileExtensionDialog.ShowModal();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ExtensionsGroup.Delete();
        }
    }
}
