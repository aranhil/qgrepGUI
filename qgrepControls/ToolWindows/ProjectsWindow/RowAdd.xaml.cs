using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace qgrepControls.ToolWindows
{
    public partial class RowAdd : System.Windows.Controls.UserControl
    {
        public ProjectsWindow Parent;
        public delegate void ClickCallbackFunction();
        private ClickCallbackFunction ClickCallback;

        public RowAdd(ProjectsWindow Parent, string tooltip, ClickCallbackFunction ClickCallback)
        {
            InitializeComponent();

            this.Parent = Parent;
            this.ClickCallback = ClickCallback;
            this.ToolTip = tooltip;

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

        private void AddGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClickCallback();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ClickCallback();
        }
    }
}
