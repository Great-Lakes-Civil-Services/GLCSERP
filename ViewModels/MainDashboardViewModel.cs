using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using CivilProcessERP.Helpers;
using CivilProcessERP.Views;
using CivilProcessERP.Services;
using CivilProcessERP.Models.Job;

namespace CivilProcessERP.ViewModels
{
    public class MainDashboardViewModel : BaseViewModel
    {
        private readonly NavigationService _navService;

        public ObservableCollection<TabItemViewModel> OpenTabs { get; set; } = new();
        private TabItemViewModel? _selectedTab;

        public TabItemViewModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged(nameof(SelectedTab));
            }
        }

        public ICommand OpenNewTabCommand { get; }
        public ICommand CloseTabCommand { get; }

        public MainDashboardViewModel(NavigationService navigationService)
        {
            _navService = navigationService;
            OpenNewTabCommand = new RelayCommand(param => OpenNewTab(param));
            CloseTabCommand = new RelayCommand(param => CloseTab(param));

            if (!OpenTabs.Any(tab => tab.Title == "Dashboard"))
            {
                OpenNewTab("Dashboard");
            }
        }

        /// <summary>
        /// Opens a new tab dynamically. If a Job object is passed, it opens a JobDetailsView.
        /// </summary>
     private void OpenNewTab(object? title)
{
    if (title == null) return;

    string tabTitle = title.ToString() ?? "New Tab";

    // ✅ Log debug information
    Console.WriteLine($"[DEBUG] Attempting to Open Tab: {tabTitle}");
    Console.WriteLine($"[DEBUG] Current Open Tabs: {string.Join(", ", OpenTabs.Select(t => t.Title))}");

    // ✅ Prevent re-opening the same tab
    var existingTab = OpenTabs.FirstOrDefault(tab => tab.Title == tabTitle);
    if (existingTab != null)
    {
        Console.WriteLine($"[DEBUG] Tab '{tabTitle}' is already open. Switching focus.");
        SelectedTab = existingTab; // Focus on already opened tab
        return;
    }

    // ✅ Prevent Dashboard from repeatedly opening itself
    if (tabTitle == "Dashboard" && OpenTabs.Any(tab => tab.Title == "Dashboard"))
    {
        Console.WriteLine($"[WARNING] Dashboard tab is already open. Ignoring duplicate open request.");
        return;
    }

    UserControl? view = _navService.GetView(tabTitle);

    if (tabTitle == "JobDetails" && title is Job job)
    {
        view = new JobDetailsView(job);
    }

    if (view != null)
    {
        var newTab = new TabItemViewModel(tabTitle, view);
        OpenTabs.Add(newTab);
        SelectedTab = newTab; // Switch to the new tab

        Console.WriteLine($"[DEBUG] New Tab Opened: {tabTitle}");
        Console.WriteLine($"[DEBUG] Updated Open Tabs: {string.Join(", ", OpenTabs.Select(t => t.Title))}");
    }
    else
    {
        Console.WriteLine($"[ERROR] Failed to open tab: {tabTitle}. No corresponding view found.");
    }
}



        private void CloseTab(object? tab)
        {
            if (tab is TabItemViewModel tabToClose)
            {
                OpenTabs.Remove(tabToClose);
                SelectedTab = OpenTabs.LastOrDefault(); // Switch to the last opened tab
            }
        }
    }

    public class TabItemViewModel : BaseViewModel
    {
        public string Title { get; }
        public UserControl Content { get; }

        public TabItemViewModel(string title, UserControl content)
        {
            Title = title;
            Content = content;
        }
    }
}
