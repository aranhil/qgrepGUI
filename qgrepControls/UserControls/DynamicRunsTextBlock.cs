using qgrepControls.ModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Collections.Specialized;
using qgrepControls.Classes;

namespace qgrepControls.UserControls
{
    public class DynamicRunsTextBlock : TextBlock
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(TextModelCollection), typeof(DynamicRunsTextBlock), new PropertyMetadata(OnItemsChanged));

        public DynamicRunsTextBlock()
        {
        }

        public TextModelCollection Items
        {
            get
            {
                return (TextModelCollection)GetValue(ItemsProperty);
            }
            set
            {
                TextModelCollection oldItems = (TextModelCollection)GetValue(ItemsProperty);
                oldItems.CollectionChanged -= OnItemsChanged;

                SetValue(ItemsProperty, value);
                value.CollectionChanged += OnItemsChanged;
            }
        }

        private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshInlines();
        }

        static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DynamicRunsTextBlock)d).RefreshInlines();
        }

        void RefreshInlines()
        {
            Inlines.Clear();
            foreach (TextModel text in Items)
            {
                var run = new Run(text.Text);
                if(text.IsHighlight)
                {
                    run.Background = ThemeHelper.GetBrush("Result.Highlight.Background");
                    run.Foreground = ThemeHelper.GetBrush("Result.Highlight.Foreground");
                }
                else
                {
                    run.Foreground = ThemeHelper.GetBrush("Result.Text.Foreground");
                }   
                Inlines.Add(run);
            }
        }
    }
}
