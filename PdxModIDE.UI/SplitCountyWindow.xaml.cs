using System.Collections.ObjectModel;
using System.Windows;

namespace PdxModIDE.UI
{
    public class CountyEntry
    {
        public string ProvinceId { get; set; } = "";
        public string BaronyKey { get; set; } = "";
        public string CountyKey { get; set; } = "";
        public string ParentTitle { get; set; } = "";
    }

    public partial class SplitCountyWindow : Window
    {
        public SplitCountyWindow()
        {
            InitializeComponent();
        }

        public SplitCountyWindow(string title, ObservableCollection<CountyEntry> entries) : this()
        {
            Title = string.Format(
                System.Windows.Application.Current.TryFindResource("SplitCounty_Title") as string ?? "Split County — {0}",
                title);

            CountyList.ItemsSource = entries;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
