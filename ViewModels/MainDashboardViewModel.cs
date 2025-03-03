using System.Windows.Controls;
using CivilProcessERP.Helpers;
using System.Windows.Input;
using CivilProcessERP.Services; // Ensure this is the correct namespace for NavigationService

namespace CivilProcessERP.ViewModels
{
    public class MainDashboardViewModel : BaseViewModel
    {
        private UserControl _currentView = new UserControl();
        public UserControl CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateToLandlordTenantCommand { get; }

        private readonly NavigationService _navigationService;

        public MainDashboardViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToLandlordTenantCommand = new RelayCommand(o => NavigateToLandlordTenant());

            // Default view is the Main Dashboard
            CurrentView = _navigationService.GetView("MainDashboard");
        }

        private void NavigateToLandlordTenant()
        {
            CurrentView = _navigationService.GetView("LandlordTenant");
        }
    }
}
