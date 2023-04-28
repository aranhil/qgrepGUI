using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace qgrepControls.Classes
{
    public interface IExtensionInterface
    {
        string GetSolutionPath();
        void OpenWindow(UserControl userControl, string title);
        void OpenFile(string path, int line);
        string GetSelectedText();
        bool WindowOpened();
    }
}
