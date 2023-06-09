﻿using qgrepControls.ModelViews;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ListBox = System.Windows.Controls.ListBox;

namespace qgrepControls.UserControls
{
    /// <summary>
    /// Interaction logic for ConfigListBox.xaml
    /// </summary>
    public partial class ConfigListBox : UserControl
    {
        public enum EditType
        {
            None,
            Text,
            Custom
        }

        public bool IsDeselectable { get; set; } = false;

        public delegate void EditFinished(string newValue);
        public delegate void EditClicked();

        public EditFinished OnEditFinished;
        public EditClicked OnEditClicked;

        private EditType itemEditType = EditType.None;
        public EditType ItemEditType
        {
            get
            {
                return itemEditType;
            }
            set
            {
                itemEditType = value;
                EditButton.IsEnabled = itemEditType != EditType.None && InnerListBox.SelectedItems.Count == 1 ? true : false;
                EditButton.Visibility = itemEditType == EditType.None ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public bool IsPreviousSelectedOnRemove { get; set; } = false;

        public ConfigListBox()
        {
            InitializeComponent();
        }

        public void SetItemsSource(IEnumerable ItemsSource)
        {
            InnerListBox.ItemsSource = ItemsSource;

            if (ItemsSource != null)
            {
                (ItemsSource as INotifyCollectionChanged).CollectionChanged += ConfigListBox_CollectionChanged;
            }

            RemoveButton.IsEnabled = InnerListBox.SelectedItems.Count > 0 ? true : false;
            RemoveAllButton.IsEnabled = InnerListBox.Items.Count > 0 ? true : false;
            AddButton.IsEnabled = ItemsSource != null ? true : false;
        }

        private void InnerListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsDeselectable)
            {
                try
                {
                    DependencyObject parent = e.OriginalSource as DependencyObject;
                    while (parent != null)
                    {
                        if (parent is ListBoxItem)
                            return;
                        if (parent is ListBox)
                            break;

                        try
                        {
                            parent = VisualTreeHelper.GetParent(parent) ?? LogicalTreeHelper.GetParent(parent);
                        }
                        catch (InvalidOperationException)
                        {
                            parent = LogicalTreeHelper.GetParent(parent);
                        }
                    }

                    InnerListBox.SelectedIndex = -1;
                }
                catch (Exception) { }
            }
        }

        private void ConfigListBox_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RemoveAllButton.IsEnabled = InnerListBox.Items.Count > 0 ? true : false;
            RemoveButton.IsEnabled = InnerListBox.SelectedItems.Count > 0 ? true : false;

            if (IsPreviousSelectedOnRemove)
            {
                if (InnerListBox.SelectedItems.Count == 0 && InnerListBox.Items.Count > 0)
                {
                    int newSelectedIndex = 0;
                    if (e.OldStartingIndex - 1 >= 0)
                    {
                        newSelectedIndex = e.OldStartingIndex - 1;
                    }

                    InnerListBox.SelectedItem = InnerListBox.Items[newSelectedIndex];
                    (InnerListBox.ItemContainerGenerator.ContainerFromItem(InnerListBox.SelectedItem) as ListBoxItem)?.Focus();
                }
            }
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
            EditButton.IsEnabled = ItemEditType != EditType.None && InnerListBox.SelectedItems.Count == 1 ? true : false;
            RemoveButton.IsEnabled = InnerListBox.SelectedItems.Count > 0 ? true : false;
        }

        private bool OnRemove()
        {
            List<object> selectedItems = new List<object>();
            foreach (object item in InnerListBox.SelectedItems)
            {
                selectedItems.Add(item);
            }

            bool removedAnything = false;
            foreach (object item in selectedItems)
            {
                IEditableData editableItem = item as IEditableData;
                if (editableItem != null && editableItem.InEditMode)
                {
                    continue;
                }

                removedAnything = true;
                (InnerListBox.Items as IEditableCollectionView).Remove(item);
            }

            return removedAnything;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            OnRemove();
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

        private void OnEdit()
        {
            if (ItemEditType == EditType.Text)
            {
                foreach (IEditableData item in InnerListBox.Items)
                {
                    item.InEditMode = false;
                }

                IEditableData editableData = InnerListBox.SelectedItem as IEditableData;
                if (editableData != null)
                {
                    editableData.InEditMode = true;
                }
            }
            else if (ItemEditType == EditType.Custom)
            {
                if (OnEditClicked != null)
                {
                    OnEditClicked();
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            OnEdit();
        }
        private void EditBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            foreach (IEditableData item in InnerListBox.Items)
            {
                item.InEditMode = false;
            }

            if (OnEditFinished != null)
            {
                OnEditFinished((sender as TextBox).Text);
            }
        }

        private void EditBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                foreach (IEditableData item in InnerListBox.Items)
                {
                    item.InEditMode = false;
                }

                if (OnEditFinished != null)
                {
                    OnEditFinished((sender as TextBox).Text);
                }
            }
        }

        private void EditBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                (sender as TextBox).Focus();
                (sender as TextBox).SelectAll();
            }
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && InnerListBox.SelectedItems.Count == 1)
            {
                OnEdit();
                e.Handled = true;
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                e.Handled = OnRemove();
            }
        }
    }
}
