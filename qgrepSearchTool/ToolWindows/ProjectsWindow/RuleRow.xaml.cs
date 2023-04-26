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

        private void RuleGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Visible;
        }

        private void RuleGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Collapsed;
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
