using System;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using PdxModIDE.Core.Games;
using PdxModIDE.Core.Games.CK3;
using PdxModIDE.Project;

namespace PdxModIDE.UI
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            GameRegistry.Register(new CK3GamePlugin());

            Directory.CreateDirectory("data");
            Directory.CreateDirectory("logs");

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                File.AppendAllText(Path.Combine("logs", "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UNHANDLED: {FormatExceptionChain(ex)}\n");
                MessageBox.Show($"Error inesperado: {ex?.Message}\n\nDetalles guardados en logs/crash.log",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (_, args) =>
            {
                var ex = args.Exception;
                File.AppendAllText(Path.Combine("logs", "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DISPATCHER: {FormatExceptionChain(ex)}\n");
                args.Handled = true;
                MessageBox.Show($"Error en la interfaz: {(ex.InnerException ?? ex).Message}\n\nDetalles guardados en logs/crash.log",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        private static string FormatExceptionChain(Exception? ex)
        {
            var sb = new System.Text.StringBuilder();
            int depth = 0;
            while (ex != null)
            {
                sb.Append(new string(' ', depth * 2));
                sb.Append(depth == 0 ? "" : "Caused by: ");
                sb.Append(ex.GetType());
                sb.Append(": ");
                sb.Append(ex.Message);
                sb.Append('\n');
                sb.Append(ex.StackTrace);
                sb.Append('\n');
                ex = ex.InnerException;
                depth++;
            }
            return sb.ToString();
        }
    }
}
