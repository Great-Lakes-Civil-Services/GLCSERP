using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CivilProcessERP.Services;
using CivilProcessERP.Helpers;

namespace CivilProcessERP.ViewModels
{
    public class UpdateViewModel : BaseViewModel
    {
        private readonly AutoUpdater _autoUpdater;
        private UpdateInfo? _availableUpdate;
        private bool _isChecking;
        private bool _isUpdating;
        
        public UpdateInfo? AvailableUpdate
        {
            get => _availableUpdate;
            set => SetProperty(ref _availableUpdate, value);
        }
        
        public bool IsChecking
        {
            get => _isChecking;
            set => SetProperty(ref _isChecking, value);
        }
        
        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetProperty(ref _isUpdating, value);
        }
        
        public ICommand CheckForUpdatesCommand { get; }
        public ICommand InstallUpdateCommand { get; }
        public ICommand RemindLaterCommand { get; }
        
        public UpdateViewModel()
        {
            _autoUpdater = new AutoUpdater();
            
            CheckForUpdatesCommand = new RelayCommand(async (object? _) => await CheckForUpdatesAsync());
            InstallUpdateCommand = new RelayCommand(async (object? _) => await InstallUpdateAsync());
            RemindLaterCommand = new RelayCommand((object? _) => RemindLater());
        }
        
        private async Task CheckForUpdatesAsync()
        {
            IsChecking = true;
            
            try
            {
                AvailableUpdate = await _autoUpdater.CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Update check failed: {ex.Message}", 
                    "Update Error", System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
            }
            finally
            {
                IsChecking = false;
            }
        }
        
        private async Task InstallUpdateAsync()
        {
            if (AvailableUpdate == null) return;
            
            var result = System.Windows.MessageBox.Show(
                $"Install GLERP v{AvailableUpdate.Version}?\n\n{AvailableUpdate.ReleaseNotes}",
                "Install Update",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
                
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                IsUpdating = true;
                
                try
                {
                    var success = await _autoUpdater.DownloadAndInstallUpdateAsync(AvailableUpdate);
                    if (success)
                    {
                        System.Windows.Application.Current.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Update installation failed: {ex.Message}",
                        "Update Error", System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    IsUpdating = false;
                }
            }
        }
        
        private void RemindLater()
        {
            AvailableUpdate = null;
        }
    }
} 