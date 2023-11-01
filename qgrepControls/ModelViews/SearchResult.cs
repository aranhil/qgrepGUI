using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace qgrepControls.ModelViews
{
    public class TextModel
    {
        public string Text { get; set; }
        public bool IsHighlight { get; set; }
    }

    public class TextModelCollection : ObservableCollection<TextModel>
    {
    }

    public class SearchResult : INotifyPropertyChanged
    {
        private bool isSelected;

        public TextModelCollection TextModels { get; set; }
        public string File { get; set; }
        public string Line { get; set; }
        public string TrimmedFileAndLine { get; set; }
        public string FileAndLine { get; set; }
        public string BeginText { get; set; }
        public string EndText { get; set; }
        public string HighlightedText { get; set; }
        public string FullResult { get; set; }
        public SearchResultGroup Parent { get; set; }

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

        public BitmapSource imageSource;
        public BitmapSource ImageSource
        {
            get
            {
                return imageSource;
            }
            set
            {
                imageSource = value;
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
                return false;
            }
            set
            {
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
