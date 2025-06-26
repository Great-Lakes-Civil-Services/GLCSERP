using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CivilProcessERP.Data;
using CivilProcessERP.Models.Job;
using CivilProcessERP.Services;
using CivilProcessERP.ViewModels;

namespace CivilProcessERP.Views
{
    public partial class MainDashboard : UserControl
    {
        private readonly JobRepository _jobRepository = new();
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


        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string jobNumber = SearchBox.Text.Trim();
                if (!string.IsNullOrEmpty(jobNumber))
                {
                    try
                    {
                        // Use JobService for consistency with other parts of the application
                        var jobService = new JobService();
                        Job job = await jobService.GetJobById(jobNumber);
                        
                        if (job != null)
                        {
                            OpenJobInNewTab(job);
                            SearchBox.Text = ""; // Clear search box after successful search
                        }
                        else
                        {
                            MessageBox.Show("Job not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to fetch job: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OpenJobInNewTab(Job job)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.AddJobTab(job);
        }

        private void AddNewTab(UserControl content, string title)
        {
            if (DataContext is MainDashboardViewModel viewModel)
            {
                var existingTab = viewModel.OpenTabs.FirstOrDefault(tab => tab.Title == title);

                if (existingTab == null)
                {
                    var newTab = new TabItemViewModel(title, content);
                    viewModel.OpenTabs.Add(newTab);
                    viewModel.SelectedTab = newTab;
                }
                else
                {
                    viewModel.SelectedTab = existingTab;
                }
            }
        }
    }
}
