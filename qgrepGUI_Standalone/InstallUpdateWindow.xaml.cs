using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace qgrepGUI
{
    /// <summary>
    /// Interaction logic for InstallUpdateWindow.xaml
    /// </summary>
    public partial class InstallUpdateWindow : UserControl
    {
        public bool IsOk = false;
        public bool IsSkip = false;
        public qgrepControls.MainWindow Dialog = null;

        public InstallUpdateWindow()
        {
            InitializeComponent();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            IsOk = true;

            if (Dialog != null)
            {
                Dialog.Close();
            }
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            IsSkip = true;

            if (Dialog != null)
            {
                Dialog.Close();
            }
        }
    }
}
