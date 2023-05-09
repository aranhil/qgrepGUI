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

namespace qgrepControls.Classes
{
    public delegate void ResultCallback(string file, string lineNumber, string beginText, string highlight, string endText);
    public delegate void FinishCallback();

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
        public bool FilterResultsRegEx { get; set;} = false;
        public string Configs { get; set; } = "";
        public ResultCallback ResultCallback { get; set; } = null;
        public FinishCallback FinishCallback { get; set; } = null;
    }

    public class SearchEngine
    {
        public bool IsBusy { get; private set; } = false;
        private bool ForceStop { get; set; } = false;

        private SearchOptions SearchOptions = null;
        private SearchOptions QueuedSearchOptions = null;

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
            SearchOptions = searchOptions;

            Task.Run(() =>
            {
                List<string> arguments = new List<string>
                {
                    "qgrep",
                    "search",
                    SearchOptions.Configs
                };

                if (!SearchOptions.CaseSensitive) arguments.Add("i");
                if (!SearchOptions.RegEx && !SearchOptions.WholeWord) arguments.Add("l");

                if (SearchOptions.IncludeFiles.Length > 0)
                {
                    arguments.Add("fi" + (SearchOptions.IncludeFilesRegEx ? SearchOptions.IncludeFiles : Regex.Escape(SearchOptions.IncludeFiles)));
                }

                if (SearchOptions.ExcludeFiles.Length > 0)
                {
                    arguments.Add("fe" + (SearchOptions.ExcludeFilesRegEx ? SearchOptions.ExcludeFiles : Regex.Escape(SearchOptions.ExcludeFiles)));
                }

                arguments.Add("HM");

                switch(SearchOptions.Query.Length)
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

                if (SearchOptions.WholeWord)
                {
                    arguments.Add("\\b" + (SearchOptions.RegEx ? SearchOptions.Query : Regex.Escape(SearchOptions.Query)) + "\\b");
                }
                else
                {
                    arguments.Add(SearchOptions.Query);
                }

                QGrepWrapper.CallQGrepAsync(arguments, StringHandler, ErrorHandler, ProgressHandler);
                IsBusy = false;

                SearchOptions.FinishCallback();
                SearchOptions = null;

                ProcessQueue();
            });
        }

        private void ProcessQueue()
        {
            if(QueuedSearchOptions != null)
            {
                SearchAsync(QueuedSearchOptions);
                QueuedSearchOptions = null;
            }
        }

        private bool StringHandler(string result)
        {
            if(ForceStop)
            {
                return true;
            }

            if(SearchOptions.ResultCallback != null)
            {
                result = result.Substring(0, result.Length - 1);
                string file, beginText, endText, highlightedText, lineNo;

                int currentIndex = result.IndexOf('\xB0');
                if (currentIndex >= 0)
                {
                    string fileAndLineNo = result.Substring(0, currentIndex);
                    result = currentIndex + 1 < result.Length ? result.Substring(currentIndex + 1) : "";

                    int indexOfParanthesis = fileAndLineNo.LastIndexOf('(');
                    lineNo = fileAndLineNo.Substring(indexOfParanthesis + 1, fileAndLineNo.Length - indexOfParanthesis - 2);
                    file = fileAndLineNo.Substring(0, indexOfParanthesis);

                    if (SearchOptions.FilterResults.Length > 0)
                    {
                        string rawText = result.Replace("\xB1", "").Replace("\xB2", "");

                        if (SearchOptions.FilterResultsRegEx)
                        {
                            if (!Regex.Match(file, SearchOptions.FilterResults).Success && !Regex.Match(rawText, SearchOptions.FilterResults).Success)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!file.ToLower().Contains(SearchOptions.FilterResults) && !rawText.Contains(SearchOptions.FilterResults))
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

                int highlightBegin = 0, highlightEnd = 0;
                Highlight(result, ref highlightBegin, ref highlightEnd);

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

                SearchOptions.ResultCallback(file, lineNo, beginText, highlightedText, endText);
            }

            return false;
        }

        private void Highlight(string result, ref int begingHighlight, ref int endHighlight)
        {
            string query = SearchOptions.Query;

            if (!SearchOptions.RegEx)
            {
                query = Regex.Escape(query);
            }
            if (SearchOptions.WholeWord)
            {
                query = "\\b" + query + "\\b";
            }
            if (!SearchOptions.CaseSensitive)
            {
                query = query.ToLower();
                result = result.ToLower();
            }

            Match match = Regex.Match(result, query);
            begingHighlight = match.Index;
            endHighlight = match.Length;
        }

        private void ErrorHandler(string result)
        {

        }

        public void ProgressHandler(int percentage)
        {

        }
    }
}
