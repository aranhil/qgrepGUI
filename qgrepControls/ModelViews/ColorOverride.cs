using qgrepControls.Classes;

namespace qgrepControls.ModelViews
{
    public class ColorOverride : SelectableData
    {
        private System.Drawing.Color color;
        public ColorOverride(string Name, System.Drawing.Color Color)
        {
            this.Name = Name;
            this.Color = Color;
        }

        public string Name { get; set; } = "";

        public System.Windows.Media.Brush brush;

        public System.Windows.Media.Brush Brush
        {
            get
            {
                return brush;
            }
            set
            {
                brush = value;
                OnPropertyChanged();
            }
        }
        public System.Drawing.Color Color
        {
            get
            {
                return color;
            }
            set
            {
                Brush = new System.Windows.Media.SolidColorBrush(ThemeHelper.ConvertColor(value));
                color = value;
            }
        }
    }
}
