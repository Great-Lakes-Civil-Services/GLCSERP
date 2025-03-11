using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.Data;
using CivilProcessERP.Services;
using CivilProcessERP.ViewModels;
using CivilProcessERP.Models.Job; // Alias the Job class to avoid ambiguity
namespace CivilProcessERP.Views
{
    public partial class MainDashboard : UserControl
    {
        private JobRepository jobRepository = new JobRepository();
        private readonly NavigationService _navigationService;
        public MainDashboard(NavigationService navigationService)
        {
            Console.WriteLine("[INFO] Entering MainDashboard Constructor");

            InitializeComponent();
            _navigationService = navigationService;

            Loaded += (s, e) => Console.WriteLine("[INFO] MainDashboard Loaded Successfully!");
        }

        private void NavigateToPage(object sender, RoutedEventArgs e)
{
    if (sender is Button button && button.Tag is string pageName)
    {
        Console.WriteLine($"[DEBUG] Button Clicked: {pageName}");

        UserControl newPage = _navigationService.GetView(pageName);

        if (newPage != null)
        {
            Console.WriteLine($"[DEBUG] Navigating to: {pageName}");
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.AddNewTab(newPage, pageName);
        }
        else
        {
            Console.WriteLine($"[ERROR] Page '{pageName}' not found!");
            MessageBox.Show($"Page '{pageName}' not found!", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}


        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string jobNumber = SearchBox.Text.Trim();
                if (!string.IsNullOrEmpty(jobNumber))
                {
                    Job job = jobRepository.GetJobDetails(jobNumber);
                    if (job != null)
                    {
                        OpenJobInNewTab(job);
                    }
                    else
                    {
                        MessageBox.Show("Job not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        

        private void OpenJobInNewTab(Job job)
        {
            // Find the TabControl in the MainWindow
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.AddJobTab(job);
            }
        }


         private void AddNewTab(UserControl content, string title)
        {
            if (DataContext is MainDashboardViewModel viewModel)
            {
                var existingTab = viewModel.OpenTabs.FirstOrDefault(tab => tab.Title == title);
                
                if (existingTab == null)  // ✅ Prevent duplicate tabs
                {
                    var newTab = new TabItemViewModel(title, content);
                    viewModel.OpenTabs.Add(newTab);
                    viewModel.SelectedTab = newTab; // ✅ Switch to the new tab
                }
                else
                {
                    viewModel.SelectedTab = existingTab; // ✅ If already open, switch to it
                }
            }
        }

        

        // Remove this method from here
    }
}
