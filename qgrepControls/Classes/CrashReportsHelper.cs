using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace qgrepControls.Classes
{
    public class CrashReportsHelper
    {
        public static DateTime LastReportTimestamp = DateTime.MinValue;
        public static string LastReport = "";
        public static readonly object padlock = new object();

        public static void DebugToRoamingLog(string message)
        {
            lock(padlock)
            {
                try
                {
                    string roamingFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string filePath = System.IO.Path.Combine(roamingFolderPath, "LogErrors.txt");
                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        writer.WriteLine(message);
                    }
                }
                catch { }
            }
        }

        public static void WriteCrashReport(Exception ex)
        {
            //System.Diagnostics.Debugger.Launch();

            string report = PrintExceptionDetails(ex);
            if(report.Length > 0)
            {
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "qgrepSearch");
                if(!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = "CrashReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                string fullPath = Path.Combine(folderPath, fileName);

                try
                {
                    File.WriteAllText(fullPath, report);
                }
                catch { }
            }
        }
        private static string PrintExceptionDetails(Exception ex)
        {
            string report = "";

            while (ex != null)
            {
                report += "An unhandled exception occurred: " + ex.Message + "\n";
                report += "Stack Trace: " + ex.StackTrace + "\n";

                ex = ex.InnerException;
            }

            return report;
        }

        public static void ReadLatestCrashReport(DateTime lastReportTime)
        {
            LastReport = "";

            if (lastReportTime == null)
            {
                lastReportTime = DateTime.MinValue;
            }

            try
            {
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "qgrepSearch");
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                FileInfo[] files = dirInfo.GetFiles("CrashReport_*.txt");

                foreach (FileInfo file in files)
                {
                    // Remove the prefix and extension from the filename to get the date-time suffix.
                    string dateTimeSuffix = file.Name.Substring(12, file.Name.Length - 16);

                    if (DateTime.TryParseExact(dateTimeSuffix, "yyyyMMdd_HHmmss",
                                               null, System.Globalization.DateTimeStyles.None,
                                               out DateTime fileTime) &&
                        fileTime > lastReportTime)
                    {
                        LastReport = File.ReadAllText(file.FullName);
                        LastReportTimestamp = fileTime;
                        break;
                    }
                }
            }
            catch { }
        }

        public static void SendCrashReport()
        {
            string formId = "1FAIpQLScwDOYfKjWXdMBDJcEVN6CaJd_Z7L6l7tlXVWkxpys5GoIUlg";
            string fieldId = "entry.1229215598";
            string encodedCrashReport = WebUtility.UrlEncode(LastReport);

            string url = $"https://docs.google.com/forms/d/e/{formId}/viewform?usp=pp_url&{fieldId}={encodedCrashReport}";

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while opening the URL: " + ex.Message);
            }
        }
    }
}
