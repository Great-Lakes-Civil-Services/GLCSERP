using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using CivilProcessERP.Models; // Ensure the namespace containing UserModel is imported
using CivilProcessERP.Views;
using CivilProcessERP.ViewModels;
using ViewModel = CivilProcessERP.ViewModels;
using CivilProcessERP.Services;
using CivilProcessERP.Models.Job;
using System.Threading.Tasks;

namespace CivilProcessERP
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly Stack<UserControl> _navigationHistory = new Stack<UserControl>();
        private readonly Stack<UserControl> _forwardHistory = new Stack<UserControl>();
        private readonly NavigationService _navigationService;

        private bool isDraggingTab = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private Point _dragStartPoint;


        public bool CanGoBack => _navigationHistory.Count > 0;
        public bool CanGoForward => _forwardHistory.Count > 0;
        private readonly DispatcherTimer _idleTimer;
        private const int IdleTimeoutMinutes = 5;
        private DateTime _lastActivityTime;

        // Define LoginContent as a ContentControl
        //public ContentControl LoginContent { get; set; }


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            LoginContent.Content = new LoginView(); // Initial login screen
            DashboardLayoutGrid.Visibility = Visibility.Collapsed;

            _navigationService = new NavigationService();

            // Initialize idle timer
            _lastActivityTime = DateTime.Now;
            _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _idleTimer.Tick += IdleTimer_Tick;
            _idleTimer.Start();

            // Listen to user activity
            InputManager.Current.PreProcessInput += OnActivityDetected;
        }


        private void NavigateToPage(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageName)
            {
                UserControl newPage = _navigationService.GetView(pageName);
                if (newPage != null)
                {
                    NavigateTo(newPage);
                }
                else
                {
                    MessageBox.Show($"Page '{pageName}' not found!", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NavigateTo(UserControl newPage)
        {
            if (DataContext is MainDashboardViewModel viewModel)
            {
                if (viewModel.SelectedTab != null)
                {
                    _navigationHistory.Push(viewModel.SelectedTab.Content);
                }

                _forwardHistory.Clear();
                AddNewTab(newPage, newPage.GetType().Name);
                UpdateNavigationButtons();
            }
        }

       public void AddNewTab(UserControl content, string title)
{
    if (DataContext is MainDashboardViewModel viewModel)
    {
        Console.WriteLine($"[DEBUG] AddNewTab() called with title: {title}");

        if (viewModel.OpenTabs.Count >= 6)
        {
            MessageBox.Show("You can only have 6 tabs open at a time. Please close one before opening a new tab.",
                "Tab Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existingTab = viewModel.OpenTabs.FirstOrDefault(tab => tab.Title == title);
        if (existingTab != null)
        {
            viewModel.SelectedTab = existingTab;
            return;
        }

        var newTab = new TabItemViewModel(title, content);

        // 🔥 Hook up tab close event
        newTab.TabCloseRequested += (_, __) => RemoveTab(content);

        viewModel.OpenTabs.Add(newTab);
        viewModel.SelectedTab = newTab;
    }
}


        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
{
    if (sender is Button btn && btn.Tag is TabItemViewModel tabVM)
    {
        Console.WriteLine($"[DEBUG] ❌ Close button clicked for tab: {tabVM.Title}");
        RemoveTab(tabVM.Content); // ✅ same logic used by TestCloseTab
    }
}


        private void GoBack(object sender, RoutedEventArgs e)
        {
            if (_navigationHistory.Count > 0)
            {
                _forwardHistory.Push((DataContext as MainDashboardViewModel)?.SelectedTab.Content);
                NavigateTo(_navigationHistory.Pop());
            }
        }

        private void GoForward(object sender, RoutedEventArgs e)
        {
            if (_forwardHistory.Count > 0)
            {
                _navigationHistory.Push((DataContext as MainDashboardViewModel)?.SelectedTab.Content);
                NavigateTo(_forwardHistory.Pop());
            }
        }

        private void UpdateNavigationButtons()
        {
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

       private void TabControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    _dragStartPoint = e.GetPosition(null); // record the mouse down position
}

private void TabControl_PreviewMouseMove(object sender, MouseEventArgs e)
{
    if (e.LeftButton != MouseButtonState.Pressed)
        return;

    Point currentPosition = e.GetPosition(null);
    Vector diff = _dragStartPoint - currentPosition;

    // Only treat as a drag if the mouse moved more than a few pixels
    if (Math.Abs(diff.X) > 10 || Math.Abs(diff.Y) > 10)
    {
        var tabControl = sender as TabControl;
        if (tabControl?.SelectedItem is TabItemViewModel tabVM)
        {
            Console.WriteLine($"[DEBUG] Drag initiated for tab: {tabVM.Title}");
            DragDrop.DoDragDrop(tabControl, tabVM, DragDropEffects.Move);
            isDraggingTab = true;
        }
    }
}
        private void TabControl_Drop(object sender, DragEventArgs e)
        {
            isDraggingTab = false;
        }

        private void TabControl_DragOver(object sender, DragEventArgs e)
        {
            if (isDraggingTab && e.GetPosition(this).X >= this.ActualWidth - 20) // near right edge
            {
                if ((FindName("MainTabControl") as TabControl)?.SelectedItem is TabItemViewModel tabVM)
                {
                    Console.WriteLine($"[DEBUG] Dragged tab reached right edge. Detaching: {tabVM.Title}");
                    DetachTab(tabVM);
                    isDraggingTab = false; // prevent repeated triggers
                }
            }
        }

        private void DetachTab(TabItemViewModel tabVM)
        {
            Console.WriteLine($"[DEBUG] Detaching tab: {tabVM.Title}");

            if (DataContext is MainDashboardViewModel viewModel)
            {
                viewModel.OpenTabs.Remove(tabVM);

                var newWindow = new Window
                {
                    Title = tabVM.Title,
                    Width = 1000,
                    Height = 700,
                    Content = tabVM.Content
                };

                newWindow.Show();
            }
        }

        public void OpenJobTab(Job job)
        {
            if (job == null) return;
            
            string tabTitle = $"Job #{job.JobId}";

            if (DataContext is MainDashboardViewModel viewModel)
            {
                Console.WriteLine($"[DEBUG] OpenJobTab() called with Job ID: {job?.JobId}");
                Console.WriteLine($"[DEBUG] Current Open Tabs: {string.Join(", ", viewModel.OpenTabs.Select(t => t.Title))}");

                if (viewModel.OpenTabs.Count >= 6)
                {
                    MessageBox.Show("You can only have 6 tabs open at a time. Please close one before opening a new job.",
                        "Tab Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Check if the job tab already exists
                var existingTab = viewModel.OpenTabs.FirstOrDefault(tab => tab.Title == tabTitle);
                if (existingTab != null)
                {
                    viewModel.SelectedTab = existingTab; // ✅ Switch to it
                    return;
                }

                // Create JobDetailsView and new tab
                var jobDetailsView = new JobDetailsView(job);
var newTab = new TabItemViewModel(tabTitle, jobDetailsView);
newTab.TabCloseRequested += (_, __) => RemoveTab(jobDetailsView);

viewModel.OpenTabs.Add(newTab);
viewModel.SelectedTab = newTab;

                //MainContentArea.Content = newTab.Content; // ✅ Ensure content updates
            }
            else
            {
                Console.WriteLine("[ERROR] MainDashboardViewModel not set as DataContext in MainWindow.");
                MessageBox.Show("Dashboard not loaded. Cannot open job tab.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddJobTab(Job job)
        {
            if (DataContext is MainDashboardViewModel viewModel)
            {
                if (viewModel.OpenTabs.Count >= 6)
                {
                    MessageBox.Show("You can only have 6 tabs open at a time. Please close one before opening a new tab.",
                        "Tab Limit Reached", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Use JobId consistently for tab title
                string tabTitle = $"Job #{job.JobId}";

                // Check if the job tab already exists
                var existingTab = viewModel.OpenTabs.FirstOrDefault(tab => tab.Title == tabTitle);
                if (existingTab != null)
                {
                    viewModel.SelectedTab = existingTab; // Switch to existing tab
                    return;
                }

                // Create new JobDetailsView and bind job data
                var jobDetailsView = new JobDetailsView(job);
                var newTab = new TabItemViewModel(tabTitle, jobDetailsView);

                // Add new tab
                viewModel.OpenTabs.Add(newTab);
                viewModel.SelectedTab = newTab; // Switch to the new tab
                
                Console.WriteLine($"[DEBUG] Job tab added: {tabTitle}");
            }
        }

        public async void LoadMainDashboardAfterLogin(string username)
        {
            var dashboardVM = new MainDashboardViewModel(_navigationService);
            DataContext = dashboardVM;
            
            Console.WriteLine("[DEBUG] MainDashboardViewModel set as DataContext");

            UserSearchService _userSearch = new();
            var fullUser = await _userSearch.GetUserByLoginAsync(username); // Use async version
            SessionManager.CurrentUser = fullUser;

            // Switch UI visibility
            LoginContent.Visibility = Visibility.Collapsed;
            DashboardLayoutGrid.Visibility = Visibility.Visible;

            NavigateTo((UserControl)_navigationService.GetView("Dashboard"));
            _lastActivityTime = DateTime.Now;
            _idleTimer.Start(); // Resume idle tracking
        }

        public void RemoveTab(UserControl content)
        {
            if (DataContext is MainDashboardViewModel viewModel)
            {
                var tabToRemove = viewModel.OpenTabs.FirstOrDefault(tab => tab.Content == content);
                if (tabToRemove != null)
                {
                    Console.WriteLine($"[DEBUG] Removing tab: {tabToRemove.Title}");
                    viewModel.OpenTabs.Remove(tabToRemove);
                    viewModel.SelectedTab = viewModel.OpenTabs.LastOrDefault();
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Attempted to remove tab but it was not found.");
                }
            }
        }

        // Test method to manually close a tab (for debugging)
        public void TestCloseTab(string tabTitle)
        {
            if (DataContext is MainDashboardViewModel viewModel)
            {
                var tabToClose = viewModel.OpenTabs.FirstOrDefault(tab => tab.Title == tabTitle);
                if (tabToClose != null)
                {
                    Console.WriteLine($"[DEBUG] Test closing tab: {tabTitle}");
                    RemoveTab(tabToClose.Content);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Tab '{tabTitle}' not found for testing.");
                }
            }
        }

        private void TestCloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainDashboardViewModel viewModel && viewModel.SelectedTab != null)
            {
                Console.WriteLine($"[DEBUG] Test close button clicked for tab: {viewModel.SelectedTab.Title}");
                TestCloseTab(viewModel.SelectedTab.Title);
            }
            else
            {
                Console.WriteLine("[DEBUG] No selected tab to close.");
            }
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            if ((DateTime.Now - _lastActivityTime).TotalMinutes >= IdleTimeoutMinutes)
            {
                _idleTimer.Stop();
                MessageBox.Show("Session expired due to inactivity.", "Auto Logout", MessageBoxButton.OK, MessageBoxImage.Warning);
                Logout();
            }
        }

        private void OnActivityDetected(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is MouseEventArgs || e.StagingItem.Input is KeyEventArgs)
            {
                _lastActivityTime = DateTime.Now;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                Logout();
            }
            // else: do nothing
        }


// Simple RelayCommand implementation


        public void Logout()
        {
            DashboardLayoutGrid.Visibility = Visibility.Collapsed;
            LoginContent.Visibility = Visibility.Visible;

            //MainContentArea.Content = null;
            SessionManager.CurrentUser = null;
            LoginContent.Content = new LoginView();

            _idleTimer.Stop(); // Pause idle timer until next loginßß
        }

        private void AdministrationButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToAdministration(); // this handles permission check and navigation
        }

        public async void NavigateToAdministration()
        {
            var currentUser = SessionManager.CurrentUser;
            var userPermissionService = new UserPermissionService();
            var directPerms = await userPermissionService.GetPermissionsForUserAsync(currentUser.UserNumber);
            var rolePerms = (await new RolePermissionService().GetPermissionsForRoleAsync(currentUser.RoleNumber))
                                                        .Select(p => p.Permission);
            var effectivePerms = new HashSet<string>(rolePerms.Concat(directPerms));

            if (!effectivePerms.Contains("ViewAdministration"))
            {
                MessageBox.Show("You do not have permission to access the Administration panel.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddNewTab(new AdministrationView(currentUser), "Administration");
        }
    }
}