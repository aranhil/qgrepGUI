using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace qgrepControls.SearchWindow
{
    public partial class RuleRow : System.Windows.Controls.UserControl
    {
        public class RuleRowData
        {
            public RuleRowData(bool RuleExclude, string RuleContent, int Index)
            {
                this.RuleType = RuleExclude ? "Exclude" : "Include";
                this.RuleContent = RuleContent;
                this.Index = Index;
            }

            public string RuleType { get; set; } = "";
            public string RuleContent { get; set; } = "";
            public int Index = 0;
        }

        public ProjectsWindow Parent;
        public RuleRowData Data;

        public RuleRow(ProjectsWindow Parent, RuleRowData Data)
        {
            InitializeComponent();

            this.Data = Data;
            this.Parent = Parent;

            this.DataContext = Data;

            Icons.Visibility = Visibility.Collapsed;
            Parent.Parent.LoadColorsFromResources(this);
        }

        private void RuleGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Visible;
        }

        private void RuleGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Collapsed;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Parent.EditRule(this);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Parent.DeleteRule(this);
        }

        private void RuleGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }
    }
}
