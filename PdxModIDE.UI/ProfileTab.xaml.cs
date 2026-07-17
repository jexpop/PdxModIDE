using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public partial class ProfileTab : System.Windows.Controls.UserControl
    {
        private bool _isRecalculating;

        public ProfileTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private MainViewModel ViewModel => (MainViewModel)DataContext!;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ModulesScroll.SizeChanged += OnModulesSizeChanged;
            Dispatcher.BeginInvoke(new Action(RecalculateLayout));
        }

        private void OnModulesSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (!_isRecalculating)
                RecalculateLayout();
        }

        private void RecalculateLayout()
        {
            if (_isRecalculating) return;
            _isRecalculating = true;

            try
            {
                var wrapPanel = FindVisualChild<WrapPanel>(ModulesScroll);
                if (wrapPanel == null) return;

                int totalItems = wrapPanel.Children.Count;
                if (totalItems == 0) return;

                double availableWidth = wrapPanel.ActualWidth;
                if (availableWidth <= 0) return;

                int columns = Math.Max(1, (int)(availableWidth / 210));
                if (columns > totalItems) columns = totalItems;

                double itemWidth = availableWidth / columns;
                double itemHeight = 28;
                int itemsPerColumn = (int)Math.Ceiling((double)totalItems / columns);

                wrapPanel.ItemWidth = itemWidth - 2;
                wrapPanel.ItemHeight = itemHeight;
                wrapPanel.Height = itemsPerColumn * itemHeight;
            }
            finally
            {
                _isRecalculating = false;
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
