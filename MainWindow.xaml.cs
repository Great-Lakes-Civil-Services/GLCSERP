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
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Forms; // For multi-monitor support

namespace CivilProcessERP
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly Stack<System.Windows.Controls.UserControl> _navigationHistory = new Stack<System.Windows.Controls.UserControl>();
        private readonly Stack<System.Windows.Controls.UserControl> _forwardHistory = new Stack<System.Windows.Controls.UserControl>();
        private NavigationService? _navigationService;

        private bool isDraggingTab = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private System.Windows.Point _dragStartPoint;


        public bool CanGoBack => _navigationHistory.Count > 0;
        public bool CanGoForward => _forwardHistory.Count > 0;
        private DispatcherTimer? _idleTimer;
        private const int IdleTimeoutMinutes = 5;
        private DateTime _lastActivityTime;

        // Define LoginContent as a ContentControl
        //public ContentControl LoginContent { get; set; }

        // Add a static list to track all open MainWindow instances
        public static List<MainWindow> OpenWindows { get; } = new List<MainWindow>();

        // Add this property to reference the ContentControl from XAML

        public MainWindow()
        {
            try
            {
                Console.WriteLine("[DEBUG] MainWindow constructor started");
                InitializeComponent();
                Console.WriteLine("[DEBUG] InitializeComponent completed");

                // FIX: Initialize the navigation service!
                _navigationService = new NavigationService();

                // Set DataContext to MainDashboardViewModel instead of 'this'
                DataContext = new MainDashboardViewModel(_navigationService!);

                if (DashboardLayoutGrid == null)
                {
                    Console.WriteLine("[ERROR] DashboardLayoutGrid is null after InitializeComponent");
                    System.Windows.MessageBox.Show("DashboardLayoutGrid is null after InitializeComponent");
                }
                if (LoginContent == null)
                {
                    Console.WriteLine("[ERROR] LoginContent is null after InitializeComponent");
                    System.Windows.MessageBox.Show("LoginContent is null after InitializeComponent");
                }
                if (MainContentControl == null)
                {
                    Console.WriteLine("[ERROR] MainContentControl is null after InitializeComponent");
                    System.Windows.MessageBox.Show("MainContentControl is null after InitializeComponent");
                }
                if (_navigationService == null)
                {
                    Console.WriteLine("[ERROR] _navigationService is null in MainWindow constructor");
                    System.Windows.MessageBox.Show("_navigationService is null in MainWindow constructor");
                }
                // Add more checks for other fields if needed

                // Defensive null checks for critical controls
                if (DashboardLayoutGrid == null || LoginContent == null || MainContentControl == null)
                {
                    string msg = $"[MainWindow] One or more critical controls are null.\n" +
                                 $"DashboardLayoutGrid: {(DashboardLayoutGrid == null ? "null" : "ok")}\n" +
                                 $"LoginContent: {(LoginContent == null ? "null" : "ok")}\n" +
                                 $"MainContentControl: {(MainContentControl == null ? "null" : "ok")}";
                    System.IO.File.AppendAllText("output.log", msg + "\n");
                    System.Windows.MessageBox.Show(msg, "MainWindow Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (SessionManager.CurrentUser != null && SessionManager.CurrentUser.Enabled)
                {
                    LoadMainDashboardAfterLogin(SessionManager.CurrentUser.LoginName);
                }
                else
                {
                    DashboardLayoutGrid.Visibility = Visibility.Collapsed;
                    LoginContent.Visibility = Visibility.Visible;
                    LoginContent.Content = new LoginView();
                }

                // Initialize idle timer
                _lastActivityTime = DateTime.Now;
                _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                _idleTimer.Tick += IdleTimer_Tick;
                _idleTimer.Start();

                // Listen to user activity
                InputManager.Current.PreProcessInput += OnActivityDetected;
                OpenWindows.Add(this);
                this.Closed += (s, e) => OpenWindows.Remove(this);
                this.Closing += (s, e) => { if (_idleTimer != null) _idleTimer.Stop(); };
                this.WindowState = WindowState.Maximized; // Always open maximized
                Console.WriteLine("[DEBUG] MainWindow constructor finished");
            }
            catch (Exception ex)
            {
                string msg = $"MainWindow failed to initialize: {ex.Message}\n{ex.StackTrace}";
                Console.WriteLine("[FATAL] " + msg);
                System.Windows.MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }


        // Refactored navigation: update MainContentControl.Content for this window only
        private void NavigateToPage(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string pageName)
            {
                System.Windows.Controls.UserControl? newPage = _navigationService?.GetView(pageName);
                if (newPage != null)
                {
                    NavigateTo(newPage);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Page '{pageName}' not found!", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NavigateTo(System.Windows.Controls.UserControl newPage)
        {
            if (MainContentControl.Content is System.Windows.Controls.UserControl currentView && currentView != newPage)
            {
                _navigationHistory.Push(currentView);
                // Clear forward history on new navigation
                _forwardHistory.Clear();
            }
            MainContentControl.Content = newPage;
            UpdateNavigationButtons();
        }

       public void AddNewTab(System.Windows.Controls.UserControl content, string title)
{
    Console.WriteLine($"[DEBUG] AddNewTab() called. DataContext type: {DataContext?.GetType().FullName}");
    if (DataContext is MainDashboardViewModel viewModel)
    {
        Console.WriteLine($"[DEBUG] AddNewTab() DataContext is MainDashboardViewModel, proceeding...");
        Console.WriteLine($"[DEBUG] AddNewTab() called with title: {title}");

        if (viewModel.OpenTabs.Count >= 6)
        {
            System.Windows.MessageBox.Show("You can only have 6 tabs open at a time. Please close one before opening a new tab.",
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
    else
    {
        Console.WriteLine("[ERROR] DataContext is not MainDashboardViewModel in AddNewTab!");
    }
}


        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
{
    if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemViewModel tabVM)
    {
        Console.WriteLine($"[DEBUG] ❌ Close button clicked for tab: {tabVM.Title}");
        RemoveTab(tabVM.Content); // ✅ same logic used by TestCloseTab
    }
}


        private void GoBack(object sender, RoutedEventArgs e)
        {
            if (_navigationHistory.Count > 0)
            {
                var currentView = MainContentControl.Content as System.Windows.Controls.UserControl;
                if (currentView != null)
                {
                    _forwardHistory.Push(currentView);
                }
                var previousView = _navigationHistory.Pop();
                MainContentControl.Content = previousView;
                UpdateNavigationButtons();
            }
        }

        private void GoForward(object sender, RoutedEventArgs e)
        {
            if (_forwardHistory.Count > 0)
            {
                var currentView = MainContentControl.Content as System.Windows.Controls.UserControl;
                if (currentView != null)
                {
                    _navigationHistory.Push(currentView);
                }
                var nextView = _forwardHistory.Pop();
                MainContentControl.Content = nextView;
                UpdateNavigationButtons();
            }
        }

        private void UpdateNavigationButtons()
        {
            GoBackBtn.IsEnabled = _navigationHistory.Count > 0;
            GoForwardBtn.IsEnabled = _forwardHistory.Count > 0;
        }

       // --- Tab Drag/Drop Logic ---
        private void TabControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tabControl = sender as System.Windows.Controls.TabControl;
            var tabItem = FindAncestor<TabItem>((DependencyObject)e.OriginalSource);
            if (tabItem != null)
            {
                _dragStartPoint = e.GetPosition(null);
                // _draggedTab = tabItem.DataContext as TabItemViewModel; // Removed
            }
        }

        private void TabControl_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // if (e.LeftButton == MouseButtonState.Pressed && _draggedTab != null) // Removed
            // {
            //     System.Windows.Point currentPosition = e.GetPosition(null);
            //     if (Math.Abs(currentPosition.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
            //         Math.Abs(currentPosition.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            //     {
            //         DragDrop.DoDragDrop((DependencyObject)sender, _draggedTab, System.Windows.DragDropEffects.Move);
            //     }
            // }
        }

        private void TabControl_Drop(object sender, System.Windows.DragEventArgs e)
        {
            // if (e.Data.GetDataPresent(typeof(TabItemViewModel))) // Removed
            // {
            //     var droppedTab = e.Data.GetData(typeof(TabItemViewModel)) as TabItemViewModel;
            //     if (droppedTab != null && droppedTab != _draggedTab)
            //     {
            //         var vm = this.DataContext as MainDashboardViewModel;
            //         if (vm != null && !vm.OpenTabs.Contains(droppedTab))
            //         {
            //             vm.OpenTabs.Add(droppedTab);
            //             droppedTab.ParentWindow = this;
            //         }
            //     }
            // }
        }

        private void TabControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // _draggedTab = null; // Removed
        }

        private void TabControl_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            // Optionally, set e.Effects or handle drag feedback here
            e.Effects = System.Windows.DragDropEffects.Move;
            e.Handled = true;
        }

        // Helper to find ancestor of a type
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t)
                    return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        // Detach tab to new window (with multi-monitor support)
        private void DetachTab(TabItemViewModel tabVM)
        {
            var vm = this.DataContext as MainDashboardViewModel;
            if (vm != null && vm.OpenTabs.Contains(tabVM))
            {
                vm.OpenTabs.Remove(tabVM);
                // Determine the screen where the mouse is
                var mousePos = System.Windows.Forms.Control.MousePosition;
                var targetScreen = Screen.FromPoint(mousePos);
                // Create new window
                var newWindow = new MainWindow();
                var newVm = new MainDashboardViewModel(newWindow._navigationService!);
                newVm.OpenTabs.Add(tabVM);
                tabVM.ParentWindow = newWindow;
                newWindow.DataContext = newVm;
                // Set window position and size to match the target screen
                newWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                newWindow.Left = targetScreen.WorkingArea.Left;
                newWindow.Top = targetScreen.WorkingArea.Top;
                newWindow.Width = targetScreen.WorkingArea.Width;
                newWindow.Height = targetScreen.WorkingArea.Height;
                newWindow.Show();
                newWindow.WindowState = WindowState.Maximized;
                // If this window has no tabs left, close it
                if (vm.OpenTabs.Count == 0)
                    this.Close();
            }
        }

        // Accept tab from another window
        public void AcceptTab(TabItemViewModel tabVM)
        {
            var vm = this.DataContext as MainDashboardViewModel;
            if (vm != null && !vm.OpenTabs.Contains(tabVM))
            {
                vm.OpenTabs.Add(tabVM);
                tabVM.ParentWindow = this;
            }
        }

        // Add this method to open a job in the current window
        public void OpenJob(Job job)
        {
            if (job == null) return;
            NavigateTo(new JobDetailsView(job));
        }

        // When loading dashboard after login, set MainContentControl.Content
        public void LoadMainDashboardAfterLogin(string username)
        {
            if (DashboardLayoutGrid == null || LoginContent == null || MainContentControl == null)
            {
                string msg = $"[MainWindow] One or more critical controls are null in LoadMainDashboardAfterLogin.\n" +
                             $"DashboardLayoutGrid: {(DashboardLayoutGrid == null ? "null" : "ok")}\n" +
                             $"LoginContent: {(LoginContent == null ? "null" : "ok")}\n" +
                             $"MainContentControl: {(MainContentControl == null ? "null" : "ok")}";
                System.IO.File.AppendAllText("output.log", msg + "\n");
                System.Windows.MessageBox.Show(msg, "MainWindow Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        
            // FIX: Show dashboard, hide login
            DashboardLayoutGrid.Visibility = Visibility.Visible;
            LoginContent.Visibility = Visibility.Collapsed;

            var dashboard = _navigationService?.GetView("Dashboard");
             
            if (MainContentControl == null)
            {
                System.Windows.MessageBox.Show("MainContentControl is null in LoadMainDashboardAfterLogin!");
                return;
            }
            if (dashboard == null)
            {
                System.Windows.MessageBox.Show("NavigationService.GetView(\"Dashboard\") returned null!");
                return;
            }
            NavigateTo(dashboard);
        }

        public void RemoveTab(System.Windows.Controls.UserControl content)
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
            if (!this.IsVisible || !this.IsLoaded)
            {
                _idleTimer?.Stop();
                return;
            }
            if ((DateTime.Now - _lastActivityTime).TotalMinutes >= IdleTimeoutMinutes)
            {
                _idleTimer?.Stop();
                System.Windows.MessageBox.Show("Session expired due to inactivity.", "Auto Logout", MessageBoxButton.OK, MessageBoxImage.Warning);
                Logout();
            }
        }

        private void OnActivityDetected(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is System.Windows.Input.MouseEventArgs || e.StagingItem.Input is System.Windows.Input.KeyEventArgs)
            {
                _lastActivityTime = DateTime.Now;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result =  System.Windows.MessageBox.Show(
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

            if (_idleTimer != null)
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
                System.Windows.MessageBox.Show("You do not have permission to access the Administration panel.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigateTo(new AdministrationView(currentUser));
        }

        // Handler for the '+' button to open a new ERP window (MainWindow)
        private void AddNewMainWindow_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.OpenWindows.Count >= 6)
            {
                System.Windows.MessageBox.Show("You cannot open more than 6 ERP shells at the same time.", "Window Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Multi-monitor support: open on the monitor where the mouse is
            var mousePos = System.Windows.Forms.Control.MousePosition;
            var targetScreen = System.Windows.Forms.Screen.FromPoint(mousePos);
            var newWindow = new MainWindow();
            newWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            newWindow.Left = targetScreen.WorkingArea.Left;
            newWindow.Top = targetScreen.WorkingArea.Top;
            newWindow.Width = targetScreen.WorkingArea.Width;
            newWindow.Height = targetScreen.WorkingArea.Height;
            newWindow.Show();
            newWindow.WindowState = WindowState.Maximized;
        }

        // Keyboard shortcut handling
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var vm = this.DataContext as MainDashboardViewModel;
            if (vm == null) return;

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.T)
            {
                // Ctrl+T: Open a new ERP window (not a tab)
                AddNewMainWindow_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.W)
            {
                // Ctrl+W: Close current tab
                if (vm.SelectedTab != null)
                    vm.SelectedTab.CloseTab();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Tab)
            {
                // Ctrl+Tab: Next tab
                int idx = vm.OpenTabs.IndexOf(vm.SelectedTab!);
                if (idx >= 0 && vm.OpenTabs.Count > 1)
                {
                    int next = (idx + 1) % vm.OpenTabs.Count;
                    vm.SelectedTab = vm.OpenTabs[next];
                }
                e.Handled = true;
            }
        }

        // Handler for context menu 'New Tab'
        private void NewTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenNewTab();
        }

        // Open a new dashboard tab (or whatever is default)
        private void OpenNewTab()
        {
            var vm = this.DataContext as MainDashboardViewModel;
            if (vm != null)
            {
                var dashboard = new MainDashboard(_navigationService!);
                var tab = new TabItemViewModel("Dashboard", dashboard) { ParentWindow = this };
                vm.OpenTabs.Add(tab);
                vm.SelectedTab = tab;
            }
        }
    }
}