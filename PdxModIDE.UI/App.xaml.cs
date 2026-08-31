using System;
using System.Diagnostics;
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
        private static string _logPath = "";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Setup log file - use fixed path relative to exe location
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "debug.log");
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            
            // Write startup marker
            File.AppendAllText(_logPath, $"=== App Starting {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
            File.AppendAllText(_logPath, $"BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}\n");
            File.AppendAllText(_logPath, $"LogPath: {_logPath}\n");

            // Also add TraceListener for Trace.WriteLine
            var traceListener = new TextWriterTraceListener(_logPath)
            {
                Name = "FileLog"
            };
            traceListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId;
            Trace.Listeners.Add(traceListener);
            Trace.AutoFlush = true;
            Trace.WriteLine($"=== Trace Starting ===");

            GameRegistry.Register(new CK3GamePlugin());

            Directory.CreateDirectory("data");
            Directory.CreateDirectory("logs");

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                File.AppendAllText(Path.Combine("logs", "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UNHANDLED: {FormatExceptionChain(ex)}\n");
                string msg = System.Windows.Application.Current.TryFindResource("App_UnhandledError") as string
                    ?? "Unexpected error: {0}\n\nDetails saved to logs/crash.log";
                MessageBox.Show(string.Format(msg, ex?.Message),
                    Res("App_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (_, args) =>
            {
                var ex = args.Exception;
                File.AppendAllText(Path.Combine("logs", "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DISPATCHER: {FormatExceptionChain(ex)}\n");
                args.Handled = true;
                string msg = System.Windows.Application.Current.TryFindResource("App_UIError") as string
                    ?? "UI error: {0}\n\nDetails saved to logs/crash.log";
                MessageBox.Show(string.Format(msg, (ex.InnerException ?? ex).Message),
                    Res("App_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
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

        public static void Log(string message)
        {
            if (!string.IsNullOrEmpty(_logPath))
            {
                try
                {
                    File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\n");
                }
                catch { }
            }
        }
    }
}
