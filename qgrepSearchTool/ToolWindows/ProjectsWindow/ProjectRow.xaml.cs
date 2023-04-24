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
    public partial class ProjectRow : System.Windows.Controls.UserControl
    {
        public class ProjectRowData
        {
            public ProjectRowData(string ProjectName)
            {
                this.ProjectName = ProjectName;
                this.IsSelected = false;
            }

            public string ProjectName = "";
            public bool IsSelected = false;
        }

        public ProjectsWindow Parent;
        public ProjectRowData Data;

        public ProjectRow(ProjectsWindow Parent, ProjectRowData Data)
        {
            InitializeComponent();

            this.Data = Data;
            this.Parent = Parent;

            this.ProjectName.Content = Data.ProjectName;

            Icons.Visibility = Visibility.Collapsed;
            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, System.Windows.Media.Color> colors = Parent.Parent.GetColorsFromResources();

            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        private void ProjectGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Visible;

            if (!Data.IsSelected)
            {
                ProjectGrid.Background = Resources["ResultHoverColor"] as SolidColorBrush;
            }
        }

        private void ProjectGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Collapsed;

            if (!Data.IsSelected)
            {
                ProjectGrid.Background = Resources["BackgroundColor"] as SolidColorBrush;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Parent.DeleteProject(this);
        }

        private void ProjectGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Parent.SelectProject(this);
        }

        public void Select(bool isSelected)
        {
            Data.IsSelected = isSelected;

            if (isSelected)
            {
                ProjectGrid.Background = Resources["ResultSelectedColor"] as SolidColorBrush;
            }
            else
            {
                ProjectGrid.Background = Resources["BackgroundColor"] as SolidColorBrush;
            }
        }
    }
}
