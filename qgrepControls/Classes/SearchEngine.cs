using qgrepControls.Properties;
using qgrepControls.SearchWindow;
using qgrepInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using System.Windows.Shapes;

namespace qgrepControls.Classes
{
    public delegate void ResultCallback(string file, string lineNumber, string beginText, string highlight, string endText, SearchOptions searchOptions);
    public delegate void SearchStepCallback(SearchOptions searchOptions);
    public delegate void StringCallback(string message);
    public delegate void UpdateStepCallback();
    public delegate void UpdateProgressCallback(int percentage);

    public class CachedSearch
    {
        public List<string> Results { get; set; } = new List<string>();

        public SearchOptions SearchOptions { get; set; }
    }

    public class SearchOptions
    {
        public string Query { get; set; } = "";
        public string IncludeFiles { get; set; } = "";
        public string ExcludeFiles { get; set; } = "";
        public string FilterResults { get; set; } = "";
        public bool CaseSensitive { get; set; } = false;
        public bool WholeWord { get; set; } = false;
        public bool RegEx { get; set; } = false;
        public bool IncludeFilesRegEx { get; set; } = false;
        public bool ExcludeFilesRegEx { get; set;} = false;
        public bool FilterResultsRegEx { get; set; } = false;
        public int GroupingMode { get; set; } = 0;
        public List<string> Configs { get; set; } = new List<string>();

        public bool CanUseCache(SearchOptions newSearchOptions)
        {
            return Query.Equals(newSearchOptions.Query) &&
                IncludeFiles.Equals(newSearchOptions.IncludeFiles) &&
                ExcludeFiles.Equals(newSearchOptions.ExcludeFiles) &&
                CaseSensitive == newSearchOptions.CaseSensitive &&
                WholeWord == newSearchOptions.WholeWord &&
                RegEx == newSearchOptions.RegEx &&
                IncludeFilesRegEx == newSearchOptions.IncludeFilesRegEx &&
                ExcludeFilesRegEx == newSearchOptions.ExcludeFilesRegEx &&
                Configs.SequenceEqual(newSearchOptions.Configs);
        }
    }

    public class SearchEngine
    {
        public bool IsBusy { get; private set; } = false;
        private bool ForceStop { get; set; } = false;
        public bool IsSearchQueued { get { return QueuedSearchOptions != null; } }

        private SearchOptions QueuedSearchOptions = null;
        private SearchOptions QueuedSearchFilesOptions = null;
        private List<string> QueuedDatabaseUpdate = null;
        private CachedSearch CachedSearch = new CachedSearch();

        public ResultCallback ResultCallback { get; set; } = null;
        public StringCallback ErrorCallback { get; set; } = null;
        public SearchStepCallback StartSearchCallback { get; set; } = null;
        public SearchStepCallback FinishSearchCallback { get; set; } = null;
        public UpdateStepCallback StartUpdateCallback { get; set; } = null;
        public UpdateStepCallback FinishUpdateCallback { get; set; } = null;
        public StringCallback UpdateInfoCallback { get; set; } = null;
        public UpdateProgressCallback UpdateProgressCallback { get; set; } = null;

        public void SearchAsync(SearchOptions searchOptions)
        {
            if(IsBusy) 
            {
                QueuedSearchOptions = searchOptions;
                ForceStop = true;
                return; 
            }

            IsBusy = true;
            ForceStop = false;

            Task.Run(() =>
            {
                StartSearchCallback(searchOptions);

                if(CachedSearch.SearchOptions != null && CachedSearch.SearchOptions.CanUseCache(searchOptions))
                {
                    CachedSearch.SearchOptions = searchOptions;

                    foreach (string cachedSearchResult in CachedSearch.Results)
                    {
                        if (StringHandler(cachedSearchResult, searchOptions, false))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    CachedSearch.Results.Clear();
                    CachedSearch.SearchOptions = searchOptions;

                    List<string> arguments = new List<string>
                    {
                        "qgrep",
                        "search",
                        string.Join(",", searchOptions.Configs)
                    };

                    if (!searchOptions.CaseSensitive) arguments.Add("i");
                    if (!searchOptions.RegEx && !searchOptions.WholeWord) arguments.Add("l");

                    if (searchOptions.IncludeFiles.Length > 0)
                    {
                        arguments.Add("fi" + (searchOptions.IncludeFilesRegEx ? searchOptions.IncludeFiles : Regex.Escape(searchOptions.IncludeFiles)));
                    }

                    if (searchOptions.ExcludeFiles.Length > 0)
                    {
                        arguments.Add("fe" + (searchOptions.ExcludeFilesRegEx ? searchOptions.ExcludeFiles : Regex.Escape(searchOptions.ExcludeFiles)));
                    }

                    arguments.Add("HD");

                    switch(searchOptions.Query.Length)
                    {
                        case 1:
                            arguments.Add("L1000");
                        break;
                        case 2:
                            arguments.Add("L2000");
                        break;
                        case 3:
                            arguments.Add("L4000");
                        break;
                        case 4:
                            arguments.Add("L6000");
                        break;
                        default:
                            arguments.Add("L10000");
                        break;
                    }

                    arguments.Add("V");

                    if (searchOptions.WholeWord)
                    {
                        arguments.Add("\\b" + (searchOptions.RegEx ? searchOptions.Query : Regex.Escape(searchOptions.Query)) + "\\b");
                    }
                    else
                    {
                        arguments.Add(searchOptions.Query);
                    }

                    QGrepWrapper.CallQGrepAsync(arguments, 
                        (string result) => { return StringHandler(result, searchOptions); }, ErrorHandler, ProgressHandler);
                }

                IsBusy = false;

                FinishSearchCallback(searchOptions);

                ProcessQueue();
            });
        }
        public void SearchFilesAsync(SearchOptions searchOptions)
        {
            if (IsBusy)
            {
                QueuedSearchFilesOptions = searchOptions;
                ForceStop = true;
                return;
            }

            IsBusy = true;
            ForceStop = false;

            Task.Run(() =>
            {
                StartSearchCallback(searchOptions);

                List<string> arguments = new List<string>
                {
                    "qgrep",
                    "files",
                    string.Join(",", searchOptions.Configs),
                    "i",
                    searchOptions.Query
                };

                QGrepWrapper.CallQGrepAsync(arguments,
                    (string result) => { return StringHandler(result, searchOptions); }, ErrorHandler, ProgressHandler);

                IsBusy = false;

                FinishSearchCallback(searchOptions);

                ProcessQueue();
            });
        }

        private void ProcessQueue()
        {
            if (QueuedSearchOptions != null)
            {
                SearchAsync(QueuedSearchOptions);
                QueuedSearchOptions = null;
            }
            else if (QueuedSearchFilesOptions != null)
            {
                SearchFilesAsync(QueuedSearchFilesOptions);
                QueuedSearchFilesOptions = null;
            }
            else if(QueuedDatabaseUpdate != null)
            {
                UpdateDatabaseAsync(QueuedDatabaseUpdate);
                QueuedDatabaseUpdate = null;
            }
        }

        private bool StringHandler(string result, SearchOptions searchOptions, bool cacheResult = true)
        {
            if(ForceStop)
            {
                return true;
            }

            if(cacheResult)
            {
                CachedSearch.Results.Add(result);
            }

            if (ResultCallback != null)
            {
                string file = "", beginText, endText, highlightedText, lineNo = "";

                int currentIndex = result.IndexOf('\xB0');
                if (currentIndex >= 0)
                {
                    string fileAndLineNo = result.Substring(0, currentIndex);
                    result = currentIndex + 1 < result.Length ? result.Substring(currentIndex + 1) : "";

                    int indexOfParanthesis = fileAndLineNo.LastIndexOf('(');
                    if (indexOfParanthesis >= 0)
                    {
                        lineNo = fileAndLineNo.Substring(indexOfParanthesis + 1, fileAndLineNo.Length - indexOfParanthesis - 2);
                        file = fileAndLineNo.Substring(0, indexOfParanthesis);

                        if (searchOptions.FilterResults.Length > 0)
                        {
                            string rawText = result.Replace("\xB1", "").Replace("\xB2", "");

                            if (searchOptions.FilterResultsRegEx)
                            {
                                if (!Regex.Match(file.ToLower(), searchOptions.FilterResults).Success && !Regex.Match(rawText.ToLower(), searchOptions.FilterResults).Success)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (!file.ToLower().Contains(searchOptions.FilterResults) && !rawText.ToLower().Contains(searchOptions.FilterResults))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                int highlightBegin = 0, highlightEnd = 0;
                Highlight(result, ref highlightBegin, ref highlightEnd, searchOptions);

                currentIndex = highlightBegin;
                if (currentIndex >= 0)
                {
                    beginText = result.Substring(0, currentIndex);
                    result = currentIndex < result.Length ? result.Substring(currentIndex) : "";
                }
                else
                {
                    return false;
                }

                currentIndex = highlightEnd;
                if (currentIndex >= 0)
                {
                    highlightedText = result.Substring(0, currentIndex);
                    result = currentIndex < result.Length ? result.Substring(currentIndex) : "";
                }
                else
                {
                    return false;
                }

                endText = result;

                ResultCallback(file, lineNo, beginText, highlightedText, endText, searchOptions);
            }

            return false;
        }

        private void Highlight(string result, ref int begingHighlight, ref int endHighlight, SearchOptions searchOptions)
        {
            string query = searchOptions.Query;

            if (!searchOptions.RegEx)
            {
                query = Regex.Escape(query);
            }
            if (searchOptions.WholeWord)
            {
                query = "\\b" + query + "\\b";
            }
            if (!searchOptions.CaseSensitive)
            {
                query = query.ToLower();
                result = result.ToLower();
            }

            Match match = Regex.Match(result, query);
            begingHighlight = match.Index;
            endHighlight = match.Length;
        }

        public void UpdateDatabaseAsync(List<string> configsPaths)
        {
            if (IsBusy)
            {
                QueuedDatabaseUpdate = configsPaths;
                return;
            }

            IsBusy = true;

            Task.Run(() =>
            {
                StartUpdateCallback();

                foreach (String configPath in configsPaths)
                {
                    QGrepWrapper.StringCallback stringCallback = new QGrepWrapper.StringCallback(DatabaseMessageHandler);
                    QGrepWrapper.ErrorCallback errorsCallback = new QGrepWrapper.ErrorCallback(ErrorHandler);
                    QGrepWrapper.ProgressCalback progressCalback = new QGrepWrapper.ProgressCalback(ProgressHandler);
                    List<string> parameters = new List<string>
                    {
                        "qgrep",
                        "update",
                        configPath
                    };
                    QGrepWrapper.CallQGrepAsync(parameters, stringCallback, errorsCallback, progressCalback);
                }

                IsBusy = false;

                FinishUpdateCallback();

                ProcessQueue();
            });
        }

        private void ErrorHandler(string result)
        {
            ErrorCallback(result);
        }

        private bool DatabaseMessageHandler(string result)
        {
            if (result.EndsWith("\n"))
            {
                result = result.Substring(0, result.Length - 1);
            }

            UpdateInfoCallback(result);
            return false;
        }

        public void ProgressHandler(int percentage)
        {
            UpdateProgressCallback(percentage);
        }
    }
}
