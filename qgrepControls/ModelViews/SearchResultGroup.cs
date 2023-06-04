using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace qgrepControls.ModelViews
{
    public class SearchResultGroup : INotifyPropertyChanged
    {
        private bool isSelected = false;
        private bool isExpanded = false;
        private bool isExpandedChanged = false;

        public string File { get; set; } = "";
        public string TrimmedFile { get; set; } = "";
        public ObservableCollection<SearchResult> SearchResults { get; set; } = new ObservableCollection<SearchResult>();

        public bool isActiveDocumentCpp = false;
        public bool IsActiveDocumentCpp
        {
            get
            {
                return isActiveDocumentCpp;
            }
            set
            {
                isActiveDocumentCpp = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                OnPropertyChanged();
            }
        }
        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }
            set
            {
                isExpandedChanged = true;
                isExpanded = value;
                OnPropertyChanged();
            }
        }

        public void SetIsExpanded(bool value)
        {
            if(!isExpandedChanged)
            {
                IsExpanded = value;
                isExpandedChanged = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
