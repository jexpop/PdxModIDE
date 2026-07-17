using System.Collections.Generic;
using System.Windows;

namespace PdxModIDE.UI
{
    public class DiffChoiceDialog : Window
    {
        public DiffChoiceDialog(string fileKey, System.Collections.Generic.List<string>? modDiff, System.Collections.Generic.List<string>? gameDiff)
        {
            Title = $"Diff — {fileKey}";
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            SetResourceReference(BackgroundProperty, "WindowBackground");

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            var promptText = new System.Windows.Controls.TextBlock
            {
                Text = "Selecciona comparación:",
                Margin = new Thickness(0, 0, 0, 10)
            };
            promptText.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "WindowForeground");
            panel.Children.Add(promptText);

            var btnMod = new System.Windows.Controls.Button
            {
                Content = "MOD ↔ Backup",
                Width = 150,
                Margin = new Thickness(0, 0, 0, 5),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
            btnMod.Click += (s, e) =>
            {
                if (modDiff != null && modDiff.Count > 0)
                {
                    var dlg = new DiffViewDialog($"MOD ↔ Backup — {fileKey}", modDiff);
                    dlg.Owner = this;
                    dlg.ShowDialog();
                }
                else
                {
                    System.Windows.MessageBox.Show("No hay diff disponible");
                }
            };

            var btnGame = new System.Windows.Controls.Button
            {
                Content = "Juego ↔ Backup",
                Width = 150,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
            btnGame.Click += (s, e) =>
            {
                if (gameDiff != null && gameDiff.Count > 0)
                {
                    var dlg = new DiffViewDialog($"Juego ↔ Backup — {fileKey}", gameDiff);
                    dlg.Owner = this;
                    dlg.ShowDialog();
                }
                else
                {
                    System.Windows.MessageBox.Show("No hay diff disponible");
                }
            };

            panel.Children.Add(btnMod);
            panel.Children.Add(btnGame);
            Content = panel;
        }
    }
}
