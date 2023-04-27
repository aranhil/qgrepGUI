using Microsoft.VisualStudio.PlatformUI;
using qgrepSearch.Classes;
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
    public partial class PathRow : System.Windows.Controls.UserControl
    {
        public class PathRowData
        {
            public PathRowData(string Path)
            {
                this.Path = Path;
            }

            public string Path { get; set; } = "";
        }

        public ProjectsWindow Parent;
        public PathRowData Data;

        public PathRow(ProjectsWindow Parent, PathRowData Data)
        {
            InitializeComponent();

            this.Data = Data;
            this.Parent = Parent;

            this.DataContext = Data;

            Icons.Visibility = Visibility.Collapsed;
            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, System.Windows.Media.Color> colors = Parent.Parent.GetColorsFromColorScheme();

            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        private void PathGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Visible;
        }

        private void PathGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Collapsed;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Parent.DeletePath(this);
        }
    }
}
