using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace qgrepControls.SearchWindow
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
            Dictionary<string, SolidColorBrush> colors = Parent.Parent.GetBrushesFromColorScheme();

            foreach (var color in colors)
            {
                Resources[color.Key] = color.Value;
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
