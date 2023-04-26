using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace qgrepSearch.Classes
{
    public class CollapsableRow : INotifyPropertyChanged
    {
        public bool IsCollapsed
        {
            get
            {
                return isCollapsed;
            }
            set
            {
                isCollapsed = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool isCollapsed = false;
    }
}
