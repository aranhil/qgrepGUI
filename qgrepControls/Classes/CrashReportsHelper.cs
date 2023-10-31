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
        public static string LastReport = "";
        public static string LastReportPath = "";
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
            if(report.Length > 0 && report.Contains("qgrep"))
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

        public static void ReadLatestCrashReport()
        {
            LastReport = "";

            try
            {
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "qgrepSearch");
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                FileInfo[] files = dirInfo.GetFiles("CrashReport_*.txt");

                foreach (FileInfo file in files)
                {
                    LastReportPath = file.FullName;
                    LastReport = File.ReadAllText(file.FullName);
                }

                string errorsLogPath = Path.Combine(folderPath, "LogErrors.txt");
                if(File.Exists(errorsLogPath))
                {
                    File.Delete(errorsLogPath);
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

        public static void MarkReportAsRead()
        {
            try
            {
                string reportDirectory = Path.GetDirectoryName(LastReportPath);
                string reportFileName = Path.GetFileName(LastReportPath);

                string newReportPath = Path.Combine(reportDirectory, "_" + reportFileName);

                File.Move(LastReportPath, newReportPath);
            }
            catch { }
        }
    }
}
