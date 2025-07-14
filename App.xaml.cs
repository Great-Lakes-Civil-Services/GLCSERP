using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CivilProcessERP.ViewModels;
using CivilProcessERP.Services;

namespace CivilProcessERP
{
    public partial class App : System.Windows.Application
    {
        private IHost _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<NavigationService>();
                    services.AddSingleton<MainDashboardViewModel>(); // ViewModel is still registered
                    services.AddSingleton<MainWindow>(); 
                    services.AddSingleton<LandlordTenantViewModel>();// Only inject MainWindow, no direct ViewModel
                })
                .Build();

            await _host.StartAsync();

            if (_host == null)
            {
                throw new InvalidOperationException("Host could not be created.");
            }

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
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