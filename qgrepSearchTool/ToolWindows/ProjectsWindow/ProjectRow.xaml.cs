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
        public class ProjectRowData: SelectableData
        {
            public ProjectRowData(string ProjectName)
            {
                this.ProjectName = ProjectName;
            }

            public string ProjectName { get; set; } = "";
        }

        public ProjectsWindow Parent;
        public ProjectRowData Data;
        private bool InEditMode = false;

        public ProjectRow(ProjectsWindow Parent, ProjectRowData Data)
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

        private void ProjectGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!InEditMode)
            {
                Icons.Visibility = Visibility.Visible;
            }
        }

        private void ProjectGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Collapsed;
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
        }

        private void EnterEditMode()
        {
            EditBox.Text = Data.ProjectName;
            EditBox.Visibility = Visibility.Visible;
            EditBox.Focus();
            EditBox.SelectAll();
            Icons.Visibility = Visibility.Collapsed;
            ProjectName.Visibility = Visibility.Collapsed;
            InEditMode = true;
        }

        private void ExitEditMode()
        {
            EditBox.Visibility = Visibility.Collapsed;
            ProjectName.Visibility = Visibility.Visible;
            InEditMode = false;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EnterEditMode();
        }

        private void EditBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ExitEditMode();
        }

        private void EditBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ExitEditMode();
                Parent.ChangeProjectName(this, EditBox.Text);
            }
        }
    }
}
