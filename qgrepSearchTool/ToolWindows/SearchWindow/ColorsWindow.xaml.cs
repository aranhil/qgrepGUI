using Microsoft.VisualStudio.Package;
using qgrepSearch.Properties;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for Colors.xaml
    /// </summary>
    public partial class ColorsWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;

        public ColorsWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();

            Feedback.Content = "";
            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, System.Windows.Media.Color> colors = SearchWindow.GetColorsFromResources();

            foreach (var color in colors)
            {
                ColorPicker colorPicker = (ColorPicker)this.FindName(color.Key);
                Resources[color.Key] = new SolidColorBrush(color.Value);
                colorPicker.SelectedColor = color.Value;
            }
        }

        private Dictionary<string, System.Windows.Media.Color> GetColors()
        {
            Dictionary<string, System.Windows.Media.Color> colors = new Dictionary<string, System.Windows.Media.Color>();

            foreach (var availableColor in qgrepSearchWindowControl.colorsAvailable)
            {
                ColorPicker colorPicker = (ColorPicker)this.FindName(availableColor);
                colors[availableColor] = colorPicker.SelectedColor.Value;
            }

            return colors;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            SearchWindow.UpdateColorsFromSettings();
            LoadColorsFromResources();

            Feedback.Content = "Loaded colors from settings!";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SearchWindow.UpdateSettingsColors(GetColors());
            Feedback.Content = "Saved colors to settings!";
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            SearchWindow.UpdateColors(GetColors());

            Dictionary<string, System.Windows.Media.Color> colors = SearchWindow.GetColorsFromResources();
            foreach (var availableColor in qgrepSearchWindowControl.colorsAvailable)
            {
                ColorPicker colorPicker = (ColorPicker)this.FindName(availableColor);
                Resources[availableColor] = new SolidColorBrush(colorPicker.SelectedColor.Value);
            }

            Feedback.Content = "Applied colors. Don't forget to save them!";
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
            SearchWindow.UpdateColorsFromSettings();
            LoadColorsFromResources();

            Feedback.Content = "Reset the colors to default!";
        }
    }
}
