using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ScsExtractorGui.WinUI3;

namespace ScsExtractorGui
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
    }

    public sealed partial class App : Application
    {
        private Window? _window;

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
