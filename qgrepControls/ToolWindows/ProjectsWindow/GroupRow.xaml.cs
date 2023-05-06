using qgrepControls.Classes;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace qgrepControls.SearchWindow
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
            Dictionary<string, object> resouces = Parent.Parent.GetResourcesFromColorScheme();

            foreach (var resource in resouces)
            {
                Resources[resource.Key] = resource.Value;
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
