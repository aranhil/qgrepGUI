using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
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
            Dictionary<string, System.Windows.Media.Color> colors = SearchWindow.GetColorsFromColorScheme();

            foreach (var color in colors)
            {
                ColorPicker colorPicker = (ColorPicker)this.FindName(color.Key);
                Resources[color.Key] = new SolidColorBrush(color.Value);
                colorPicker.SelectedColor = color.Value;
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            SearchWindow.UpdateColorsFromSettings();
            LoadColorsFromResources();

            Feedback.Content = "Loaded colors from settings!";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Feedback.Content = "Saved colors to settings!";
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
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
