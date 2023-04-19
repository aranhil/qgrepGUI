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
    public partial class FileExtensionWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;
        public delegate void Callback(bool accepted);
        private Callback ResultCallback;

        public FileExtensionWindow(qgrepSearchWindowControl SearchWindow, Callback ResultCallback)
        {
            this.SearchWindow = SearchWindow;
            this.ResultCallback = ResultCallback;

            InitializeComponent();
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

        private void SaveGroup_Click(object sender, RoutedEventArgs e)
        {
            ResultCallback(true);
        }

        private void CancelGroup_Click(object sender, RoutedEventArgs e)
        {
            ResultCallback(false);
        }

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                ResultCallback(true);
            }
            else if(e.Key == Key.Escape)
            {
                ResultCallback(false);
            }
        }
    }
}
