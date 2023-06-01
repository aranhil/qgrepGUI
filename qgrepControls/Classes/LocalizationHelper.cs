using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace qgrepControls.Classes
{
    public class LocalizationHelper
    {
        public static string Translate(List<string> parameters)
        {
            if (parameters.Count > 1)
            {
                TestLanguage();

                string resourceKey = parameters[0];
                string format = Resources.ResourceManager.GetString(resourceKey);

                if (format != null)
                {
                    object[] args = parameters.Skip(1).Select(s =>
                    {
                        string resourceValue = Resources.ResourceManager.GetString(s);
                        if (resourceValue != null)
                        {
                            return resourceValue;
                        }

                        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
                        {
                            return (object)num;
                        }
                        else if (LooksLikePath(s))
                        {
                            return ConfigParser.FromUtf8(s);
                        }

                        return s;
                    }).ToArray();

                    return string.Format(format, args);
                }
            }

            return "";
        }

        public static void TestLanguage()
        {
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("ro-RO");
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("ro-RO");
        }

        private static bool LooksLikePath(string path)
        {
            try
            {
                string fullPath = System.IO.Path.GetFullPath(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
