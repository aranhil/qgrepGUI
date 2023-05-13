using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
using ListBox = System.Windows.Controls.ListBox;

namespace qgrepControls.UserControls
{
    /// <summary>
    /// Interaction logic for ConfigListBox.xaml
    /// </summary>
    public partial class ConfigListBox : UserControl
    {
        public ConfigListBox()
        {
            InitializeComponent();
        }

        public void SetItemsSource(IEnumerable ItemsSource)
        {
            InnerListBox.ItemsSource = ItemsSource;

            if(ItemsSource != null)
            {
                (ItemsSource as INotifyCollectionChanged).CollectionChanged += ConfigListBox_CollectionChanged;
            }

            RemoveAllButton.Visibility = InnerListBox.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            AddButton.Visibility = ItemsSource != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InnerListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //DependencyObject parent = e.OriginalSource as DependencyObject;
            //while (parent != null)
            //{
            //    if (parent is ListBoxItem)
            //        return; 
            //    if (parent is ListBox)
            //        break;
            //    parent = VisualTreeHelper.GetParent(parent);
            //}

            //InnerListBox.SelectedIndex = -1;
        }

        private void ConfigListBox_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RemoveAllButton.Visibility = InnerListBox.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            //ButtonsPanel.Visibility = Visibility.Visible;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            //ButtonsPanel.Visibility = Visibility.Collapsed;
        }

        private void InnerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EditButton.Visibility = InnerListBox.SelectedItems.Count == 1 ? Visibility.Visible : Visibility.Collapsed;
            RemoveButton.Visibility = InnerListBox.SelectedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            List<Object> selectedItems = new List<object>();
            foreach (object item in InnerListBox.SelectedItems)
            {
                selectedItems.Add(item);
            }
            foreach(object item in selectedItems)
            {
                (InnerListBox.Items as IEditableCollectionView).Remove(item);
            }
        }

        private void RemoveAllButton_Click(object sender, RoutedEventArgs e)
        {
            List<Object> selectedItems = new List<object>();
            foreach (object item in InnerListBox.Items)
            {
                selectedItems.Add(item);
            }
            foreach (object item in selectedItems)
            {
                (InnerListBox.Items as IEditableCollectionView).Remove(item);
            }
        }
    }
}
