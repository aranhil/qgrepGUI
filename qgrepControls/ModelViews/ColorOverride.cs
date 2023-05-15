using qgrepControls.SearchWindow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace qgrepControls.ModelViews
{
    public class ColorOverride : INotifyPropertyChanged
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
                Brush = new System.Windows.Media.SolidColorBrush(qgrepSearchWindowControl.ConvertColor(value));
                color = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
