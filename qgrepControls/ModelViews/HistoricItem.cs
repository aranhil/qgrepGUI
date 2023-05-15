using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace qgrepControls.ModelViews
{
    public class HistoricItem
    {
        private string text;
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
                OnPropertyChanged();
            }
        }

        public string Operation { get; set; }
        public Visibility OperationVisibility { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class HistoricSearch : HistoricItem
    {
        public string SearchedText { get; set; }
    }

    public class HistoricOpen : HistoricItem
    {
        public string OpenedPath { get; set; }
        public string OpenedLine { get; set; }
    }
}
