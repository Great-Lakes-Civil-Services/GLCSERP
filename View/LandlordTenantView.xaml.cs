// using System.Windows;
// using System.Windows.Controls;
// using CivilProcessERP.ViewModels; // Ensure this line is present
// using CivilProcessERP; // Add this for MainWindow access
// using CivilProcessERP.Models.Job; // Add this for Job class access

// namespace CivilProcessERP.Views
// {
//     using CivilProcessERP.Models; // Ensure this is present for Job class

//     public partial class LandlordTenantView : System.Windows.Controls.UserControl
//     {
//         private LandlordTenantViewModel _viewModel;

//         public LandlordTenantView(Job job)
//     {
//         InitializeComponent();
//         _viewModel = new LandlordTenantViewModel(job); // ✅ Pass job to ViewModel
//         DataContext = _viewModel;
//     }

// private async void SearchJobButton_Click(object sender, RoutedEventArgs e)
// {
//     Console.WriteLine("[DEBUG] SearchJobButton_Click triggered");

//     JobSearchBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();

//     if (string.IsNullOrWhiteSpace(_viewModel.SearchJobNumber))
//     {
//         _viewModel.SearchJobNumber = JobSearchBox.Text;
//     }

//     Console.WriteLine($"[DEBUG] 🔍 Calling SearchJobAsync with: {_viewModel.SearchJobNumber}");

//     var job = await _viewModel.SearchJobAsync();

//     if (job != null)
//     {
//         Console.WriteLine($"[DEBUG] ✅ Job found (from async): {job.JobId}");

//         if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
//         {
//             mainWindow.OpenJobTab(job); // ✅ triggers unique tab logic
//         }
//     }
//     else
//     {
//         System.Windows.MessageBox.Show("No job found for the given number.", "Search Result", MessageBoxButton.OK, MessageBoxImage.Warning);
//     }
// }



//         private void AddJobButton_Click(object sender, RoutedEventArgs e)
//         {
//             if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
//             {
//                 var newJob = new Job(); // Create a blank job object
//                 var addJobView = new AddJobView(newJob); // New AddJobView
//                 mainWindow.AddNewTab(addJobView, $"Add Job"); // Open in a new tab
//             }
//         }

//        private void JobListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
// {
//     var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

//     if (mainWindow != null && _viewModel.SelectedJob != null)
//     {
//         Console.WriteLine($"[DEBUG] JobListBox selection changed. Opening tab for: {_viewModel.SelectedJob.JobId}");
//         mainWindow.OpenJobTab(_viewModel.SelectedJob);
//     }
//     else
//     {
//         Console.WriteLine("[ERROR] Either MainWindow is null or SelectedJob is null.");
//     }
// }

using System;
using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.ViewModels; // Add this for LandlordTenantViewModel
using CivilProcessERP.Models.Job; // Add this for Job class access

namespace CivilProcessERP.Views
{
    public partial class LandlordTenantView : System.Windows.Controls.UserControl
    {
        private LandlordTenantViewModel _viewModel;

        public LandlordTenantView(Job job)
        {
            InitializeComponent();
            _viewModel = new LandlordTenantViewModel(job);
            DataContext = _viewModel;
        }

        // Make sure you have a TextBox named JobSearchBox in your XAML, e.g.:
        // <TextBox x:Name="JobSearchBox" ... />

        private async void SearchJobButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[DEBUG] SearchJobButton_Click triggered");

            // Try to find the JobSearchBox TextBox by name if not already defined
            var jobSearchBox = this.FindName("JobSearchBox") as System.Windows.Controls.TextBox;
            if (jobSearchBox != null)
            {
                jobSearchBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                if (string.IsNullOrWhiteSpace(_viewModel.SearchJobNumber))
                {
                    _viewModel.SearchJobNumber = jobSearchBox.Text;
                }
            }
            else
            {
                Console.WriteLine("[ERROR] JobSearchBox TextBox not found in the view.");
            }

            Console.WriteLine($"[DEBUG] 🔍 Calling SearchJobAsync with: {_viewModel.SearchJobNumber}");
            var job = await _viewModel.SearchJobAsync();
            if (job != null)
            {
                Console.WriteLine($"[DEBUG] ✅ Job found (from async): {job.JobId}");
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.OpenJob(job);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No job found for the given number.", "Search Result", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddJobButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var newJob = new Job();
                mainWindow.OpenJob(newJob);
            }
        }

        private void JobListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && _viewModel.SelectedJob != null)
            {
                Console.WriteLine($"[DEBUG] JobListBox selection changed. Opening job: {_viewModel.SelectedJob.JobId}");
                mainWindow.OpenJob(_viewModel.SelectedJob);
            }
            else
            {
                Console.WriteLine("[ERROR] Either MainWindow is null or SelectedJob is null.");
            }
        }
    }
}