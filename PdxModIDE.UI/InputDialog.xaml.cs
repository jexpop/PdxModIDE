using System.Windows;

namespace PdxModIDE.UI
{
    public partial class InputDialog : System.Windows.Window
    {
        public string ResponseText { get; private set; } = "";

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OkButton_Click(sender, new RoutedEventArgs());
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
