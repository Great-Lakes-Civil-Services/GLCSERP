using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using CivilProcessERP.Helpers;
using CivilProcessERP.Models.Job;
using CivilProcessERP.Services;
using CivilProcessERP.Views;

namespace CivilProcessERP.ViewModels
{
    public class MainDashboardViewModel : BaseViewModel
    {
        private readonly NavigationService _navService;

        public ObservableCollection<TabItemViewModel> OpenTabs { get; set; } = new();
        private TabItemViewModel? _selectedTab;
        private string _searchText = "";

        public TabItemViewModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        //public string TestProperty => "DataContext is working!";

        public ICommand OpenNewTabCommand { get; }
        //public ICommand CloseTabCommand { get; }

        public MainDashboardViewModel(NavigationService navigationService)
        {
            _navService = navigationService;
            OpenNewTabCommand = new RelayCommand(async param => await OpenNewTabAsync(param));

            
            //CloseTabCommand = new RelayCommand(param => 
            // {
            //     Console.WriteLine("[DEBUG] CloseTabCommand.Execute called");
            //     CloseTab(param);
            // });

            Console.WriteLine("[DEBUG] MainDashboardViewModel initialized with CloseTabCommand");

            if (!OpenTabs.Any(tab => tab.Title == "Dashboard"))
            {
                _ = OpenNewTabAsync("Dashboard"); // Fire and forget on startup
            }
        }
        
       

 public ICommand CloseTabCommand => new RelayCommand(tab =>
{
    Console.WriteLine("[DEBUG] CloseTabCommand triggered");

    if (tab is TabItemViewModel tabVM)
    {
        Console.WriteLine($"[DEBUG] Removing tab: {tabVM.Title}");
        OpenTabs.Remove(tabVM);
        SelectedTab = OpenTabs.LastOrDefault();
    }
    else
    {
        Console.WriteLine("[DEBUG] Invalid parameter to CloseTabCommand");
    }
});

    
       public async Task OpenNewTabAsync(object? param)
        {
            if (param == null)
            {
                Console.WriteLine("[ERROR] OpenNewTabAsync() called with null param.");
                return;
            }

            // ✅ Add 6-tab limit check
            if (OpenTabs.Count >= 6)
            {
                System.Windows.MessageBox.Show("You can only have 6 tabs open at a time. Please close one before opening a new tab.",
                    "Tab Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tabTitle = param is Job job ? $"Job #{job.JobId}" : param.ToString() ?? "New Tab";
            Console.WriteLine($"[DEBUG] Attempting to open tab: {tabTitle}");

            // Prevent duplicates
            var existingTab = OpenTabs.FirstOrDefault(tab => tab.Title == tabTitle);
            if (existingTab != null)
            {
                Console.WriteLine($"[DEBUG] Tab '{tabTitle}' already open. Focusing it.");
                SelectedTab = existingTab;
                return;
            }

            System.Windows.Controls.UserControl? view = null;

if (param is Job jobData)
{
    Console.WriteLine($"[DEBUG] Creating JobDetailsView for: {jobData.JobId}");
    view = new JobDetailsView(jobData); // ✅ new ViewModel per tab

    var newTab = new TabItemViewModel($"Job #{jobData.JobId}", view);

    newTab.TabCloseRequested += (_, __) =>
    {
        OpenTabs.Remove(newTab);
        SelectedTab = OpenTabs.LastOrDefault();
    };

    OpenTabs.Add(newTab);
    SelectedTab = newTab;

    Console.WriteLine($"[DEBUG] ✅ New tab added: Job #{jobData.JobId}. Total open tabs: {OpenTabs.Count}");
    return;
}
            else
            {
                Console.WriteLine($"[DEBUG] Creating generic view for: {tabTitle}");
                view = _navService.GetView(tabTitle);
            }

            if (view != null)
{
    var newTab = new TabItemViewModel(tabTitle, view);

    // ✅ FIX: Hook up the close event to remove the tab when close is requested
    newTab.TabCloseRequested += (_, __) =>
    {
        OpenTabs.Remove(newTab);
        SelectedTab = OpenTabs.LastOrDefault();
    };

    OpenTabs.Add(newTab);
    SelectedTab = newTab;

    Console.WriteLine($"[DEBUG] ✅ New tab added: {tabTitle}. Total open tabs: {OpenTabs.Count}");
}

            else
            {
                Console.WriteLine($"[ERROR] View for '{tabTitle}' is null. Could not open tab.");
            }
        }

        private void CloseTab(object? tab)
        {
            Console.WriteLine($"[DEBUG] CloseTab called with parameter: {tab?.GetType().Name}");
            
            if (tab is TabItemViewModel tabToClose)
            {
                Console.WriteLine($"[DEBUG] Closing tab: {tabToClose.Title}");
                OpenTabs.Remove(tabToClose);
                
                // Select the last remaining tab, or null if no tabs left
                SelectedTab = OpenTabs.Count > 0 ? OpenTabs.Last() : null;
                Console.WriteLine($"[DEBUG] Tab closed. Remaining tabs: {OpenTabs.Count}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] CloseTab called with invalid parameter type: {tab?.GetType().Name}");
            }
        }
    }

    public class TabItemViewModel : BaseViewModel
    {
        public string Title { get; }
        public System.Windows.Controls.UserControl Content { get; }
        public ICommand CloseCommand { get; }
        public event EventHandler? TabCloseRequested;

        // Track parent window for drag/drop and close logic
        public MainWindow ParentWindow { get; set; }

        public TabItemViewModel(string title, System.Windows.Controls.UserControl content)
        {
            Title = title;
            Content = content;
            CloseCommand = new RelayCommand(_ => CloseTab());
        }

        public void CloseTab()
        {
            if (ParentWindow != null)
            {
                var vm = ParentWindow.DataContext as MainDashboardViewModel;
                if (vm != null && vm.OpenTabs.Contains(this))
                {
                    vm.OpenTabs.Remove(this);
                    if (vm.OpenTabs.Count == 0)
                        ParentWindow.Close();
                }
            }
            TabCloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // Move this tab to another window
        public void MoveToWindow(MainWindow targetWindow)
        {
            if (ParentWindow != null)
            {
                var oldVm = ParentWindow.DataContext as MainDashboardViewModel;
                if (oldVm != null && oldVm.OpenTabs.Contains(this))
                    oldVm.OpenTabs.Remove(this);
            }
            var newVm = targetWindow.DataContext as MainDashboardViewModel;
            if (newVm != null && !newVm.OpenTabs.Contains(this))
            {
                newVm.OpenTabs.Add(this);
                ParentWindow = targetWindow;
            }
        }
    }
}
