// using System.Windows.Controls;
// using System.Windows; // Add this line
// using System.Windows.Input; // Add this line
// using CivilProcessERP.ViewModels;

// namespace CivilProcessERP.Views
// {
//     public partial class LandlordTenantView : UserControl
//     {
//         private TabControl MainTabControl;
//         private Stack<UserControl> _navigationHistory = new Stack<UserControl>();
//         private Stack<UserControl> _forwardHistory = new Stack<UserControl>();
//         private bool isDraggingTab;

//         public LandlordTenantView()
//         {
//             InitializeComponent();
//             DataContext = new LandlordTenantViewModel(); // ✅ Attach ViewModel to XAML
//             MainTabControl = (TabControl)FindName("MainTabControl");
//         }


        
//         private void NavigateToPage(object sender, RoutedEventArgs e)
//         {
//             if (sender is Button button && button.Tag is string pageName)
//             {
//                 UserControl newPage = pageName switch
//                 {
//                     "Dashboard" => new MainDashboard(),
//                     "Executions" => new ExecutionsView(),
//                     "GeneralCivil" => new GeneralCivilView(),
//                     "LandlordTenant" => new LandlordTenantView()  { DataContext = new LandlordTenantViewModel() },
//                     "Evictions" => new EvictionsView(),
//                     "ServerMGT" => new ServerManagementView(),
//                     "Logistics" => new LogisticsView(),
//                     "Administration" => new AdministrationView(),
//                     "HuronPortal" => new HuronPortalView(),
//                     "Courts" => new CourtsView(),
//                     "Clients" => new ClientsView(),
//                     "EmployeesCorner" => new EmployeesCornerView(),
//                     _ => null
//                 };

//                 if (newPage != null)
//                     NavigateTo(newPage);
//             }
//         }

//         private void NavigateTo(UserControl newPage)
//         {
//             if (MainTabControl.SelectedItem is TabItemViewModel tab)
//             {
//                 _navigationHistory.Push(tab.Content);
//             }

//             _forwardHistory.Clear();
//             AddNewTab(newPage, newPage.GetType().Name);
//             UpdateNavigationButtons();
//         }

//         private void AddNewTab(UserControl content, string title)
//         {
//             var newTab = new TabItemViewModel(title, content);
//             if (!MainTabControl.Items.Contains(newTab))
//             {
//                 if (DataContext is MainDashboardViewModel viewModel)
//                 {
//                     viewModel.OpenTabs.Add(newTab);
//                 }
//             }
//             MainTabControl.SelectedItem = newTab;
//         }

//         private void GoBack(object sender, RoutedEventArgs e)
//         {
//             if (_navigationHistory.Count > 0)
//             {
//                 _forwardHistory.Push(MainTabControl.SelectedItem as UserControl);
//                 NavigateTo(_navigationHistory.Pop());
//             }
//         }

//         private void GoForward(object sender, RoutedEventArgs e)
//         {
//             if (_forwardHistory.Count > 0)
//             {
//                 _navigationHistory.Push(MainTabControl.SelectedItem as UserControl);
//                 NavigateTo(_forwardHistory.Pop());
//             }
//         }

//         private void UpdateNavigationButtons()
//         {
//             (FindName("GoBack") as Button).IsEnabled = _navigationHistory.Count > 0;
//             (FindName("GoForward") as Button).IsEnabled = _forwardHistory.Count > 0;
//         }

//         private void TabControl_PreviewMouseMove(object sender, MouseEventArgs e)
//         {
//             if (e.LeftButton == MouseButtonState.Pressed && MainTabControl.SelectedItem is TabItemViewModel tabVM)
//             {
//                 DragDrop.DoDragDrop(MainTabControl, tabVM, DragDropEffects.Move);
//                 isDraggingTab = true;
//             }
//         }

//         private void TabControl_DragOver(object sender, DragEventArgs e)
//         {
//             if (isDraggingTab && e.GetPosition(this).X > this.ActualWidth)
//             {
//                 if (MainTabControl.SelectedItem is TabItemViewModel tabVM)
//                 {
//                     DetachTab(tabVM);
//                 }
//             }
//         }

//         private void DetachTab(TabItemViewModel tabVM)
//         {
//             var newWindow = new Window
//             {
//                 Title = tabVM.Title,
//                 Content = new ContentControl { Content = tabVM.Content },
//                 Width = 800,
//                 Height = 600
//             };

//             newWindow.Show();
//         }
//     }
// }


using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.ViewModels; // Ensure this line is present

namespace CivilProcessERP.Views
{
    public partial class LandlordTenantView : UserControl
    {
        private ViewModels.CivilProcessERP.ViewModels.LandlordTenantViewModel _viewModel;

        public LandlordTenantView()
        {
            InitializeComponent();
            _viewModel = new ViewModels.CivilProcessERP.ViewModels.LandlordTenantViewModel();
            DataContext = _viewModel; // ✅ Ensure ViewModel is attached
        }

    private void SearchJobButton_Click(object sender, RoutedEventArgs e)
{
    Console.WriteLine("[DEBUG] SearchJob() triggered");

    // 🔁 Force the binding to push UI to ViewModel
    var binding = JobSearchBox.GetBindingExpression(TextBox.TextProperty);
    binding?.UpdateSource();

    // ✅ Read and confirm right before search
    Console.WriteLine($"[DEBUG] Value from UI TextBox: {JobSearchBox.Text}");
    Console.WriteLine($"[DEBUG] ViewModel SearchJobNumber BEFORE fix: {_viewModel.SearchJobNumber}");

    // 🚨 TEMP: Directly assign if ViewModel didn't get value
    if (string.IsNullOrWhiteSpace(_viewModel.SearchJobNumber))
    {
        _viewModel.SearchJobNumber = JobSearchBox.Text;
        Console.WriteLine("[DEBUG] 🛠️ ViewModel.SearchJobNumber was empty, manually set from UI");
    }

    Console.WriteLine($"[DEBUG] ViewModel SearchJobNumber AFTER fix: {_viewModel.SearchJobNumber}");

    _viewModel.SearchJob();
    Console.WriteLine("[DEBUG] SearchJob() Completed");

    if (_viewModel.CurrentJob != null)
    {
        var jobDetailsView = new JobDetailsView(_viewModel.CurrentJob);
        var mainWindow = Application.Current.MainWindow as MainWindow;
        mainWindow?.AddNewTab(jobDetailsView, $"Job {_viewModel.CurrentJob.JobId}");
    }
    else
    {
        MessageBox.Show("No job found.", "Search Result", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}


        private void AddJobButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CivilProcessERP.ViewModels.LandlordTenantViewModel viewModel)
            {
                viewModel.AddNewJob();
            }
        }
    }
}
