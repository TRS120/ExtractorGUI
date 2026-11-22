using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using ScsExtractorGui.WinUI3;

namespace ScsExtractorGui
{
    public static class Program
    {
        private static readonly string CrashLogPath =
            Path.Combine(AppContext.BaseDirectory, "ExtractorGui_crash.log");

        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                WriteCrashLog("AppDomain.UnhandledException", e.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

            bool bootstrapInitialized = false;

            try
            {
                WinRT.ComWrappersSupport.InitializeComWrappers();

                // Initialize Windows App SDK runtime for unpackaged WinUI 3 execution
                bootstrapInitialized = Bootstrap.TryInitialize(0x00010006, string.Empty, out int hr);
                if (!bootstrapInitialized)
                {
                    WriteCrashLog($"Bootstrap.TryInitialize failed (HRESULT: 0x{hr:X8})", null);
                    return;
                }

                Application.Start(p =>
                {
                    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                });
            }
            catch (Exception ex)
            {
                WriteCrashLog("Program.Main exception", ex);
                throw;
            }
            finally
            {
                if (bootstrapInitialized)
                {
                    try
                    {
                        Bootstrap.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        WriteCrashLog("Bootstrap.Shutdown exception", ex);
                    }
                }
            }
        }

        internal static void WriteCrashLog(string source, Exception? ex)
        {
            try
            {
                string exText = ex != null ? ex.ToString() : "No exception object.";
                string divider = new string('-', 60);
                string entry =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]{Environment.NewLine}" +
                    $"{exText}{Environment.NewLine}" +
                    $"{divider}{Environment.NewLine}";

                File.AppendAllText(CrashLogPath, entry);
            }
            catch
            {
                // Ignore logging errors during shutdown/crash
            }
        }
    }

    public sealed partial class App : Application
    {
        private Window? _window;

        public App()
        {
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Program.WriteCrashLog("App.UnhandledException", e.Exception);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                _window = new MainWindow();
                _window.Activate();
            }
            catch (Exception ex)
            {
                Program.WriteCrashLog("App.OnLaunched", ex);
                throw;
            }
        }
    }
}
