using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using CivilProcessERP.Views;
using CivilProcessERP.ViewModels;
using CivilProcessERP.Services;
using CivilProcessERP.Models.Job;

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

        public bool CanGoBack => _navigationHistory.Count > 0;
        public bool CanGoForward => _forwardHistory.Count > 0;

        public MainWindow()
        {
            InitializeComponent();
            _navigationService = new NavigationService(); // Initialize Navigation Service
            DataContext = new MainDashboardViewModel(_navigationService);
            NavigateTo((UserControl)_navigationService.GetView("Dashboard"));
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

                // ✅ Ensure MainContentArea updates to reflect the selected tab
                MainContentArea.Content = viewModel.SelectedTab.Content;
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

        private void TabControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var tabControl = FindName("MainTabControl") as TabControl;
            if (tabControl != null && e.LeftButton == MouseButtonState.Pressed && tabControl.SelectedItem is TabItemViewModel tabVM)
            {
                DragDrop.DoDragDrop(tabControl, tabVM, DragDropEffects.Move);
                isDraggingTab = true;
            }
        }

        private void TabControl_DragOver(object sender, DragEventArgs e)
        {
            if (!(FindName("MainTabControl") is TabControl tabControl))
                return; // Ensure MainTabControl exists before proceeding

            if (isDraggingTab && e.GetPosition(this).X > this.ActualWidth)
            {
                if (tabControl.SelectedItem is TabItemViewModel tabVM)
                {
                    DetachTab(tabVM);
                }
            }
        }

        private void TabControl_Drop(object sender, DragEventArgs e)
        {
            isDraggingTab = false;
        }

        private void DetachTab(TabItemViewModel tabVM)
        {
            var newWindow = new Window
            {
                Title = tabVM.Title,
                Content = new ContentControl { Content = tabVM.Content },
                Width = 800,
                Height = 600
            };

            newWindow.Show();
        }

        public void AddJobTab(Job job)
        {
            if (DataContext is MainDashboardViewModel viewModel)
            {
                string tabTitle = $"Job {job.JobNumber}";

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
            }
        }
    }
}
