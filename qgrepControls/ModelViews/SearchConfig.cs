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
    public class SearchRule : SelectableData
    {
        public bool IsExclude { get; set; }
        public string RegEx { get; set; }
    }
    public class SearchPath : SelectableData
    {
        public string Path { get; set; }
    }
    public class SearchGroup : SelectableData
    {
        public int Index { get; set; }
        public ObservableCollection<SearchPath> Paths { get; set; }
        public ObservableCollection<SearchRule> Rules { get; set; }
    }
    public class SearchConfig: SelectableData
    {
        public string Name { get; set; }
        public ObservableCollection<SearchGroup> SearchGroups { get; set; }
    }
}
