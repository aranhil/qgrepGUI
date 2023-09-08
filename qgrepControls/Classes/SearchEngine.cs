using qgrepControls.Properties;
using qgrepInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;

namespace qgrepControls.Classes
{
    public interface ISearchEngineEventsHandler
    {
        void OnResultEvent(string file, string lineNumber, string beginText, string highlight, string endText, SearchOptions searchOptions);
        void OnErrorEvent(string message, SearchOptions searchOptions);
        void OnStartSearchEvent(SearchOptions searchOptions);
        void OnFinishSearchEvent(SearchOptions searchOptions);
    }

    public delegate void UpdateMessageCallback(string message, DatabaseUpdate databaseUpdate);
    public delegate void UpdateStepCallback(DatabaseUpdate databaseUpdate);
    public delegate void UpdateProgressCallback(double percentage, DatabaseUpdate databaseUpdate);
    public enum CacheUsageType
    {
        Normal,
        Bypass,
        Forced,
    };

    public class CachedSearch
    {
        public List<string> Results { get; set; } = new List<string>();

        public SearchOptions SearchOptions { get; set; }
    }

    public class SearchOptions
    {
        public int Id { get; set; }
        public int ResultsCount { get; set; } = 0;
        public ISearchEngineEventsHandler EventsHandler { get; set; }
        public string Query { get; set; } = "";
        public string IncludeFiles { get; set; } = "";
        public string ExcludeFiles { get; set; } = "";
        public string FilterResults { get; set; } = "";
        public bool CaseSensitive { get; set; } = false;
        public bool WholeWord { get; set; } = false;
        public bool RegEx { get; set; } = false;
        public bool IncludeFilesRegEx { get; set; } = false;
        public bool ExcludeFilesRegEx { get; set; } = false;
        public bool FilterResultsRegEx { get; set; } = false;
        public int GroupingMode { get; set; } = 0;
        public List<string> Configs { get; set; } = new List<string>();
        public CacheUsageType CacheUsageType { get; set; } = CacheUsageType.Normal;
        public bool BypassHighlight { get; set; } = false;
        public bool FileSearchUnorderedKeywords { get; set; } = false;
        public bool IsFileSearch { get; set; } = false;
        public bool WasForceStopped { get; internal set; }
        public bool IsNewSearch { get; set; } = true;

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

    public class DatabaseUpdate
    {
        public List<string> ConfigPaths { get; set; }
        public List<string> Files { get; set; }
        public bool WasForceStopped { get; internal set; }
    }

    public class SearchEngine
    {
        private static SearchEngine instance = null;
        private static readonly object padlock = new object();

        private ManualResetEvent queueEvent = new ManualResetEvent(false);
        public int currentId = 1;

        public static SearchEngine Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new SearchEngine();
                    }
                    return instance;
                }
            }
        }

        private SearchEngine()
        {
            TaskRunner.RunInBackgroundAsync(QueueUpdate);
        }

        private string LastUpdateMessage = "";
        private double LastUpdateProgress = -1;
        private System.Timers.Timer UpdateTimer = new System.Timers.Timer();

        public bool IsBusy { get; private set; } = false;
        public bool IsUpdatingDatabase { get; private set; } = false;
        private bool ForceStop { get; set; } = false;
        private bool ForceDatabaseStop { get; set; } = false;
        public bool IsSearchQueued { get { return QueuedSearchOptions != null || QueuedSearchFilesOptions != null; } }

        private List<string> NonIndexedFiles = new List<string>();

        private SearchOptions QueuedSearchOptions = null;
        private SearchOptions QueuedSearchFilesOptions = null;
        private DatabaseUpdate QueuedDatabaseUpdate = null;
        private CachedSearch CachedSearch = new CachedSearch();

        public UpdateStepCallback StartUpdateCallback { get; set; } = null;
        public UpdateStepCallback FinishUpdateCallback { get; set; } = null;
        public UpdateMessageCallback UpdateInfoCallback { get; set; } = null;
        public UpdateProgressCallback UpdateProgressCallback { get; set; } = null;

        public void SearchAsync(SearchOptions searchOptions)
        {
            searchOptions.Id = currentId++;

            if (IsBusy || !MutexUtility.Instance.TryAcquireMutex())
            {
                //CrashReportsHelper.DebugToRoamingLog($"SearchAsync Busy Id: {searchOptions.Id}, Query: {searchOptions.Query}");

                QueuedSearchOptions = searchOptions;

                if (searchOptions.CacheUsageType != CacheUsageType.Forced)
                {
                    ForceStop = true;
                }

                return;
            }

            IsBusy = true;
            ForceStop = false;

            //CrashReportsHelper.DebugToRoamingLog($"SearchAsync Id: {searchOptions.Id}, Query: {searchOptions.Query}");

            QueuedSearchOptions = null;
            QueuedSearchFilesOptions = null;

            TaskRunner.RunInBackgroundAsync(() =>
            {
                searchOptions.EventsHandler.OnStartSearchEvent(searchOptions);

                if (searchOptions.CacheUsageType != CacheUsageType.Bypass && CachedSearch.SearchOptions != null &&
                    (searchOptions.CacheUsageType == CacheUsageType.Forced || CachedSearch.SearchOptions.CanUseCache(searchOptions)))
                {
                    if (searchOptions.CacheUsageType == CacheUsageType.Forced)
                    {
                        searchOptions.Query = CachedSearch.SearchOptions.Query;
                        searchOptions.CaseSensitive = CachedSearch.SearchOptions.CaseSensitive;
                        searchOptions.WholeWord = CachedSearch.SearchOptions.WholeWord;
                        searchOptions.RegEx = CachedSearch.SearchOptions.RegEx;
                    }

                    CachedSearch.SearchOptions = searchOptions;

                    foreach (string cachedSearchResult in CachedSearch.Results)
                    {
                        if(ForceStop)
                        {
                            break;
                        }

                        StringHandler(cachedSearchResult, searchOptions, false);
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
                        string processedIncludeFiles = searchOptions.IncludeFiles.Replace("\\", "/");
                        arguments.Add("fi" + (searchOptions.IncludeFilesRegEx ? processedIncludeFiles : Regex.Escape(processedIncludeFiles)));
                    }

                    if (searchOptions.ExcludeFiles.Length > 0)
                    {
                        string processedExcludeFiles = searchOptions.ExcludeFiles.Replace("\\", "/");
                        arguments.Add("fe" + (searchOptions.ExcludeFilesRegEx ? processedExcludeFiles : Regex.Escape(processedExcludeFiles)));
                    }

                    arguments.Add("HD");

                    switch (searchOptions.Query.Length)
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
                        (string result) => { StringHandler(result, searchOptions); },
                        () => { return ForceStop; },
                        null,
                        (List<string> results) => { SearchLocalizedStringHandler(results, searchOptions); });
                }

                if (ForceStop)
                {
                    searchOptions.WasForceStopped = true;
                    CachedSearch.SearchOptions = null;
                }

                searchOptions.EventsHandler.OnFinishSearchEvent(searchOptions);

                IsBusy = false;
                TaskRunner.RunOnUIThread(() =>
                {
                    MutexUtility.Instance.WorkDone();
                });

                CheckQueue();
            });
        }
        public void SearchFilesAsync(SearchOptions searchOptions)
        {
            if (IsBusy || !MutexUtility.Instance.TryAcquireMutex())
            {
                QueuedSearchFilesOptions = searchOptions;

                if (searchOptions.CacheUsageType != CacheUsageType.Forced)
                {
                    ForceStop = true;
                }

                return;
            }

            IsBusy = true;
            ForceStop = false;

            QueuedSearchOptions = null;
            QueuedSearchFilesOptions = null;

            TaskRunner.RunInBackgroundAsync(() =>
            {
                searchOptions.EventsHandler.OnStartSearchEvent(searchOptions);

                string processedQuery = searchOptions.FileSearchUnorderedKeywords ? searchOptions.Query : searchOptions.Query.Replace("\\", "/");
                List<string> arguments = new List<string>
                {
                    "qgrep",
                    "files",
                    string.Join(",", searchOptions.Configs),
                    "i",
                    "V",
                    searchOptions.FileSearchUnorderedKeywords ? "fc" : "fp",
                    (searchOptions.FileSearchUnorderedKeywords ? ConfigParser.GetPathToRemove(Settings.Default.FilesSearchScopeIndex) + "\xB0" : "") +
                    (searchOptions.RegEx ? processedQuery : Regex.Escape(processedQuery))
                };

                QGrepWrapper.CallQGrepAsync(arguments,
                    (string result) => { StringHandler(result, searchOptions, true); },
                    () => { return ForceStop; },
                    null,
                    (List<string> results) => { SearchLocalizedStringHandler(results, searchOptions); });
                
                if(ForceStop)
                {
                    searchOptions.WasForceStopped = true;
                    CachedSearch.SearchOptions = null;
                }

                searchOptions.EventsHandler.OnFinishSearchEvent(searchOptions);

                IsBusy = false;
                TaskRunner.RunOnUIThread(() =>
                {
                    MutexUtility.Instance.WorkDone();
                });

                CheckQueue();
            });
        }

        private void CheckQueue()
        {
            if(QueuedDatabaseUpdate != null || QueuedSearchOptions != null || QueuedSearchFilesOptions != null)
            {
                queueEvent.Set();
            }
        }

        private void QueueUpdate()
        {
            while (true)
            {
                queueEvent.WaitOne();
                ProcessQueue();
                Thread.Sleep(10);
            }
        }

        private void ProcessQueue()
        {
            TaskRunner.RunOnUIThread(() =>
            {
                if (QueuedDatabaseUpdate != null)
                {
                    var queuedDatabaseUpdate = QueuedDatabaseUpdate;
                    QueuedDatabaseUpdate = null;

                    UpdateDatabaseAsync(queuedDatabaseUpdate);
                }
                else if (QueuedSearchOptions != null)
                {
                    var queuedSearchOptions = QueuedSearchOptions;
                    QueuedSearchOptions = null;

                    SearchAsync(queuedSearchOptions);
                }
                else if (QueuedSearchFilesOptions != null)
                {
                    var queuedSearchFilesOptions = QueuedSearchFilesOptions;
                    QueuedSearchFilesOptions = null;

                    SearchFilesAsync(queuedSearchFilesOptions);
                }
                else
                {
                    queueEvent.Reset();
                }
            });
        }

        private void StringHandler(string result, SearchOptions searchOptions, bool cacheResult = true)
        {
            if (ForceStop)
            {
                return;
            }

            if (cacheResult)
            {
                CachedSearch.Results.Add(result);
            }

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
                    file = ConfigParser.FromUtf8(fileAndLineNo.Substring(0, indexOfParanthesis));

                    if (searchOptions.FilterResults.Length > 0)
                    {
                        if (searchOptions.FilterResultsRegEx)
                        {
                            bool matchesFilter = false;

                            if (Settings.Default.FilterSearchScopeIndex == 1 || Settings.Default.FilterSearchScopeIndex == 2)
                            {
                                if (Regex.Match(file.ToLower(), searchOptions.FilterResults.ToLower()).Success)
                                {
                                    matchesFilter = true;
                                }
                            }

                            if (Settings.Default.FilterSearchScopeIndex == 0 || Settings.Default.FilterSearchScopeIndex == 2)
                            {
                                if (Regex.Match(result.ToLower(), searchOptions.FilterResults.ToLower()).Success)
                                {
                                    matchesFilter = true;
                                }
                            }

                            if (!matchesFilter)
                            {
                                return;
                            }
                        }
                        else
                        {
                            bool matchesFilter = false;

                            if (Settings.Default.FilterSearchScopeIndex == 1 || Settings.Default.FilterSearchScopeIndex == 2)
                            {
                                if (file.ToLower().Contains(searchOptions.FilterResults.ToLower()))
                                {
                                    matchesFilter = true;
                                }
                            }

                            if (Settings.Default.FilterSearchScopeIndex == 0 || Settings.Default.FilterSearchScopeIndex == 2)
                            {
                                if (result.ToLower().Contains(searchOptions.FilterResults.ToLower()))
                                {
                                    matchesFilter = true;
                                }
                            }

                            if (!matchesFilter)
                            {
                                return;
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            if (!searchOptions.BypassHighlight)
            {
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
                    return;
                }

                currentIndex = highlightEnd;
                if (currentIndex >= 0)
                {
                    highlightedText = result.Substring(0, currentIndex);
                    result = currentIndex < result.Length ? result.Substring(currentIndex) : "";
                }
                else
                {
                    return;
                }

                endText = result;
            }
            else
            {
                beginText = result;
                highlightedText = "";
                endText = "";
            }

            searchOptions.EventsHandler.OnResultEvent(file, lineNo, beginText, highlightedText, endText, searchOptions);
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

            try
            {
                Match match = Regex.Match(result, query);
                begingHighlight = match.Index;
                endHighlight = match.Length;
            }
            catch { }
        }

        private void SearchLocalizedStringHandler(List<string> results, SearchOptions searchOptions)
        {
            if (ForceStop)
            {
                return;
            }

            searchOptions.EventsHandler.OnErrorEvent(LocalizationHelper.Translate(results), searchOptions);
        }

        public void UpdateDatabaseAsync(DatabaseUpdate databaseUpdate)
        {
            if (IsBusy || !MutexUtility.Instance.TryAcquireMutex())
            {
                QueuedDatabaseUpdate = databaseUpdate;
                return;
            }

            IsBusy = true;
            IsUpdatingDatabase = true;
            ForceDatabaseStop = false;

            TaskRunner.RunInBackgroundAsync(() =>
            {
                StartUpdateCallback(databaseUpdate);
                UpdateTimer.Stop();
                LastUpdateProgress = -1;

                try
                {
                    if (databaseUpdate.Files != null && NonIndexedFiles.Count < 15)
                    {
                        foreach (string file in databaseUpdate.Files)
                        {
                            if (!NonIndexedFiles.Contains(file))
                            {
                                NonIndexedFiles.Add(file);
                            }
                        }

                        List<string> parameters = new List<string>
                        {
                            "qgrep",
                            "change",
                            string.Join(",", databaseUpdate.ConfigPaths),
                            string.Join(",", databaseUpdate.Files)
                        };

                        QGrepWrapper.CallQGrepAsync(parameters,
                            (string message) => { DatabaseMessageHandler(message, databaseUpdate); },
                            () => { return ForceDatabaseStop; },
                            (double percentage) => { ProgressHandler(percentage, databaseUpdate); },
                            (List<string> messages) => { UpdateLocalizedStringHandler(messages, databaseUpdate); });
                    }
                    else
                    {
                        NonIndexedFiles.Clear();

                        foreach (string configPath in databaseUpdate.ConfigPaths)
                        {
                            List<string> parameters = new List<string>
                            {
                                "qgrep",
                                "update",
                                configPath
                            };

                            QGrepWrapper.CallQGrepAsync(parameters,
                                (string message) => { DatabaseMessageHandler(message, databaseUpdate); },
                                () => { return ForceDatabaseStop; },
                                (double percentage) => { ProgressHandler(percentage, databaseUpdate); },
                                (List<string> messages) => { UpdateLocalizedStringHandler(messages, databaseUpdate); });
                        }
                    }
                }
                catch { }

                if(ForceDatabaseStop)
                {
                    databaseUpdate.WasForceStopped = true;
                }

                FinishUpdateCallback(databaseUpdate);
                StartLastUpdatedTimer();
                LastUpdateProgress = -1;

                IsBusy = false;
                IsUpdatingDatabase = false;

                TaskRunner.RunOnUIThread(() =>
                {
                    MutexUtility.Instance.WorkDone();
                });

                CheckQueue();
            });
        }
        public void ForceStopDatabaseUpdate()
        {
            if (IsBusy || !MutexUtility.Instance.TryAcquireMutex())
            {
                ForceDatabaseStop = true;
                return;
            }
        }

        private void UpdateLocalizedStringHandler(List<string> results, DatabaseUpdate databaseUpdate)
        {
            if (ForceDatabaseStop)
            {
                return;
            }

            UpdateInfoCallback(LocalizationHelper.Translate(results), databaseUpdate);
        }

        private void DatabaseMessageHandler(string result, DatabaseUpdate databaseUpdate)
        {
            if(ForceDatabaseStop)
            {
                return;
            }

            if (result.EndsWith("\n"))
            {
                result = result.Substring(0, result.Length - 1);
            }

            LastUpdateMessage = result;
            UpdateInfoCallback(result, databaseUpdate);
        }

        public void ProgressHandler(double percentage, DatabaseUpdate databaseUpdate)
        {
            if (ForceDatabaseStop)
            {
                return;
            }

            LastUpdateProgress = percentage;
            UpdateProgressCallback(percentage, databaseUpdate);
        }

        private void StartLastUpdatedTimer()
        {
            UpdateTimer = new System.Timers.Timer(5000);
            UpdateTimer.Elapsed += UpdateTimer_Elapsed;
            UpdateTimer.Start();
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Instance.IsBusy)
            {
                UpdateLastUpdated();
            }
        }

        public void UpdateLastUpdated()
        {
            DateTime lastUpdated = ConfigParser.GetLastUpdated();
            string timeAgo = string.Format(Properties.Resources.LastIndexUpdateLabelContent, GetTimeAgoString(lastUpdated));

            DatabaseMessageHandler(timeAgo, null);
        }

        public void ShowLastUpdateMessage()
        {
            UpdateInfoCallback(LastUpdateMessage, null);
            UpdateProgressCallback(LastUpdateProgress, null);
        }

        public static string GetTimeAgoString(DateTime eventTime)
        {
            if (eventTime == DateTime.MaxValue)
            {
                return Properties.Resources.NeverText;
            }

            TimeSpan timeSinceEvent = DateTime.Now - eventTime;

            if (timeSinceEvent.TotalSeconds < 60)
            {
                return Properties.Resources.JustNowText;
            }
            else if (timeSinceEvent.TotalMinutes < 60)
            {
                return string.Format(Properties.Resources.MinutesAgoText, (int)timeSinceEvent.TotalMinutes);
            }
            else if (timeSinceEvent.TotalHours < 24)
            {
                return string.Format(Properties.Resources.HoursAgoText, (int)timeSinceEvent.TotalHours);
            }
            else if (timeSinceEvent.TotalDays < 7)
            {
                return string.Format(Properties.Resources.DaysAgoText, (int)timeSinceEvent.TotalDays);
            }
            else
            {
                return eventTime.ToString("g");
            }
        }
    }
}
