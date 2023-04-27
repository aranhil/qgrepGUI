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
    public partial class GroupRow : System.Windows.Controls.UserControl
    {
        public class GroupRowData : SelectableData
        {
            public GroupRowData(string GroupName, bool IsReadOnly, int Index)
            {
                this.GroupName = GroupName;
                this.IsReadOnly = IsReadOnly;
                this.Index = Index;
            }

            public string GroupName { get; set; } = "";
            public bool IsReadOnly = false;
            public int Index = 0;
        }

        public ProjectsWindow Parent;
        public GroupRowData Data;

        public GroupRow(ProjectsWindow Parent, GroupRowData Data)
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

        private void GroupGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Data.IsReadOnly)
            {
                Icons.Visibility = Visibility.Visible;
            }
        }

        private void GroupGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Data.IsReadOnly)
            {
                Icons.Visibility = Visibility.Collapsed;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Data.IsReadOnly)
            {
                Parent.DeleteGroup(this);
            }
        }

        private void GroupGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Parent.SelectGroup(this);
        }

        public void Select(bool isSelected)
        {
            Data.IsSelected = isSelected;
        }
    }
}
