using System;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CivilProcessERP.ViewModels;
using CivilProcessERP.Services;

namespace CivilProcessERP
{
    public partial class App : Application
    {
        private IHost? _host;

        // Import AllocConsole from kernel32.dll
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Enable Console Output
            AllocConsole();

            Console.WriteLine("Console is now available!");

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<NavigationService>();
                    // Register ViewModels
                    services.AddSingleton<MainDashboardViewModel>();

                    // Register MainWindow with constructor injection
                    services.AddSingleton<MainWindow>(provider =>
                        new MainWindow(provider.GetRequiredService<MainDashboardViewModel>()));
                })
                .Build();

            await _host.StartAsync();
            Console.WriteLine("MainWindow is about to be shown...");

            if (_host == null)
            {
                throw new InvalidOperationException("Host could not be created.");
            }

            // Show MainWindow using DI
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            Console.WriteLine("MainWindow has been shown.");
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync();
            }
            base.OnExit(e);
        }
    }
}
