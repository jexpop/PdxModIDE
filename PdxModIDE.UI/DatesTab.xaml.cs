using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public partial class DatesTab : System.Windows.Controls.UserControl
    {
        private MainViewModel? _viewModel;
        private bool _isRecalculating;

        public DatesTab()
        {
            InitializeComponent();
            Loaded += DatesTab_Loaded;
        }

        private void DatesTab_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            ModulesScroll.SizeChanged += OnModulesSizeChanged;
            RefreshEndDate();
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

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
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

        private void RefreshEndDate()
        {
            if (_viewModel == null) return;

            var modDate = _viewModel.ReadModEndDate();
            var gameDate = _viewModel.ReadEndDate();

            if (!string.IsNullOrEmpty(modDate))
            {
                EndDateTextBox.Text = modDate;
                EndDateSourceText.Text = Res("DatesTab_SourceMod");
            }
            else if (!string.IsNullOrEmpty(gameDate))
            {
                EndDateTextBox.Text = gameDate;
                EndDateSourceText.Text = Res("DatesTab_SourceGame");
            }
            else
            {
                EndDateTextBox.Text = "";
                EndDateSourceText.Text = Res("DatesTab_SourceNotFound");
            }
        }

        private void SaveEndDate_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var newDate = EndDateTextBox.Text.Trim();
            if (string.IsNullOrEmpty(newDate))
            {
                System.Windows.MessageBox.Show(Res("Msg_InvalidDate"), Res("App_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _viewModel.SaveEndDate();
            RefreshEndDate();
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            _viewModel.ProcessModules(false);
        }
    }
}
