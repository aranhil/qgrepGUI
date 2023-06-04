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
    public enum HistoricItemType
    {
        Search,
        Open,
    }

    public class HistoricItemData
    {
        public string Text { get; set; }
        public string Line { get; set; }
        public HistoricItemType Type { get; set; }
    }

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

        public string OperationBeginText { get; set; }
        public string OperationEndText { get; set; }
        public Visibility OperationVisibility { get; set; }

        public void SetOperationText(string text)
        {
            string[] parts = text.Split(new string[] { "{0}" }, StringSplitOptions.None);
            if(parts.Length > 0)
            {
                OperationBeginText = parts[0];
            }
            if(parts.Length > 1)
            {
                OperationEndText = parts[1];
            }
        }

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
