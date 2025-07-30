using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CivilProcessERP.Services;
using System.Windows.Forms;

namespace CivilProcessERP.Views
{
    public partial class ManageProcessServerStatusView : System.Windows.Controls.UserControl
    {
        private readonly ProcessServerStatusService _processServerStatusService = new ProcessServerStatusService();
        private List<string> _allProcessServers = new List<string>();
        private List<ProcessServerJobInfo> _currentJobs = new List<ProcessServerJobInfo>();
        private string? _selectedProcessServer;

        public ManageProcessServerStatusView()
        {
            InitializeComponent();
            Console.WriteLine("[INFO] ManageProcessServerStatusView initialized");
            _ = LoadAllProcessServersAsync();
        }

        private async Task LoadAllProcessServersAsync()
        {
            try
            {
                _allProcessServers = await _processServerStatusService.GetAllDistinctProcessServersAsync();
                ProcessServerComboBox.ItemsSource = _allProcessServers;
                Console.WriteLine($"[INFO] Loaded {_allProcessServers.Count} distinct process servers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load process servers: {ex.Message}");
                System.Windows.MessageBox.Show("Failed to load process servers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ProcessServerComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string query = ProcessServerComboBox.Text.Trim();
            if (query.Length >= 2)
            {
                var filteredServers = _allProcessServers.Where(ps => 
                    ps.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
                ProcessServerComboBox.ItemsSource = filteredServers;
                ProcessServerComboBox.IsDropDownOpen = true;
            }
        }

        private async void ProcessServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine($"[DEBUG] ProcessServerComboBox_SelectionChanged triggered");
            Console.WriteLine($"[DEBUG] SelectedItem: {ProcessServerComboBox.SelectedItem}");
            Console.WriteLine($"[DEBUG] SelectedItem type: {ProcessServerComboBox.SelectedItem?.GetType()}");
            
            if (ProcessServerComboBox.SelectedItem is string selectedProcessServer)
            {
                Console.WriteLine($"[DEBUG] Selected process server: {selectedProcessServer}");
                _selectedProcessServer = selectedProcessServer;
                await LoadJobsForProcessServerAsync(selectedProcessServer);
            }
            else
            {
                Console.WriteLine($"[DEBUG] SelectedItem is not a string: {ProcessServerComboBox.SelectedItem}");
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = ProcessServerComboBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                System.Windows.MessageBox.Show("Please enter a process server name to search.", "Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var filteredServers = _allProcessServers.Where(ps => 
                ps.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            ProcessServerComboBox.ItemsSource = filteredServers;
            
            if (filteredServers.Count == 0)
            {
                System.Windows.MessageBox.Show("No process servers found matching your search criteria.", "No Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task LoadJobsForProcessServerAsync(string processServerName)
        {
            Console.WriteLine($"[DEBUG] LoadJobsForProcessServerAsync called with: {processServerName}");
            try
            {
                _currentJobs = await _processServerStatusService.GetJobsForProcessServerAsync(processServerName);
                Console.WriteLine($"[DEBUG] Service returned {_currentJobs.Count} jobs");
                
                JobsDataGrid.ItemsSource = _currentJobs;
                Console.WriteLine($"[DEBUG] Set JobsDataGrid.ItemsSource with {_currentJobs.Count} items");

                // Update process server info
                SelectedProcessServerTextBlock.Text = processServerName;
                TotalJobsTextBlock.Text = _currentJobs.Count.ToString();
                ActiveJobsTextBlock.Text = _currentJobs.Count(j => j.IsActive).ToString();
                InactiveJobsTextBlock.Text = _currentJobs.Count(j => !j.IsActive).ToString();

                StatusMessage.Text = $"Loaded {_currentJobs.Count} jobs for process server '{processServerName}'";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Green;

                Console.WriteLine($"[INFO] Loaded {_currentJobs.Count} jobs for process server: {processServerName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load jobs for process server {processServerName}: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"Failed to load jobs for process server '{processServerName}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ToggleJobStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string jobId && !string.IsNullOrEmpty(_selectedProcessServer))
            {
                try
                {
                    var job = _currentJobs.FirstOrDefault(j => j.JobId == jobId);
                    if (job != null)
                    {
                        var newStatus = !job.IsActive;
                        var currentUser = SessionManager.CurrentUser?.LoginName ?? "System";

                        var result = System.Windows.MessageBox.Show(
                            $"Are you sure you want to {(newStatus ? "activate" : "deactivate")} process server '{_selectedProcessServer}' for job '{jobId}'?",
                            "Confirm Status Change",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            var success = await _processServerStatusService.ToggleProcessServerStatusForJobAsync(
                                jobId, _selectedProcessServer, newStatus, currentUser);

                            if (success)
                            {
                                job.IsActive = newStatus;
                                // Fix: Use a new list instead of Items.Refresh() to avoid edit mode issues
                                JobsDataGrid.ItemsSource = null;
                                JobsDataGrid.ItemsSource = _currentJobs;
                                
                                // Update summary
                                TotalJobsTextBlock.Text = _currentJobs.Count.ToString();
                                ActiveJobsTextBlock.Text = _currentJobs.Count(j => j.IsActive).ToString();
                                InactiveJobsTextBlock.Text = _currentJobs.Count(j => !j.IsActive).ToString();

                                StatusMessage.Text = $"Job '{jobId}' status updated successfully.";
                                StatusMessage.Foreground = System.Windows.Media.Brushes.Green;

                                Console.WriteLine($"[SUCCESS] Toggled job {jobId} status to {newStatus}");
                            }
                            else
                            {
                                StatusMessage.Text = "Failed to update job status. Please try again.";
                                StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Exception in ToggleJobStatus_Click: {ex.Message}");
                    System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ActivateAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProcessServer) || _currentJobs.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select a process server first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to activate ALL jobs for process server '{_selectedProcessServer}'?",
                "Confirm Bulk Action",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await PerformBulkActionAsync(true);
            }
        }

        private async void DeactivateAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProcessServer) || _currentJobs.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select a process server first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to deactivate ALL jobs for process server '{_selectedProcessServer}'?",
                "Confirm Bulk Action",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await PerformBulkActionAsync(false);
            }
        }

        private async Task PerformBulkActionAsync(bool activate)
        {
            try
            {
                var currentUser = SessionManager.CurrentUser?.LoginName ?? "System";
                int successCount = 0;
                int totalCount = _currentJobs.Count;

                foreach (var job in _currentJobs)
                {
                    if (job.IsActive != activate)
                    {
                        var success = await _processServerStatusService.ToggleProcessServerStatusForJobAsync(
                            job.JobId, _selectedProcessServer!, activate, currentUser);

                        if (success)
                        {
                            job.IsActive = activate;
                            successCount++;
                        }
                    }
                    else
                    {
                        successCount++; // Already in desired state
                    }
                }

                // Fix: Use the same approach as individual toggle to avoid edit mode issues
                JobsDataGrid.ItemsSource = null;
                JobsDataGrid.ItemsSource = _currentJobs;
                
                // Update summary
                TotalJobsTextBlock.Text = _currentJobs.Count.ToString();
                ActiveJobsTextBlock.Text = _currentJobs.Count(j => j.IsActive).ToString();
                InactiveJobsTextBlock.Text = _currentJobs.Count(j => !j.IsActive).ToString();

                StatusMessage.Text = $"Bulk action completed: {successCount}/{totalCount} jobs {(activate ? "activated" : "deactivated")}.";
                StatusMessage.Foreground = System.Windows.Media.Brushes.Green;

                Console.WriteLine($"[SUCCESS] Bulk action completed: {successCount}/{totalCount} jobs {(activate ? "activated" : "deactivated")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in PerformBulkActionAsync: {ex.Message}");
                System.Windows.MessageBox.Show($"An error occurred during bulk action: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddProcessServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.Form
                {
                    Text = "Add New Process Server",
                    Size = new System.Drawing.Size(400, 200),
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var firstNameLabel = new System.Windows.Forms.Label { Text = "First Name:", Location = new System.Drawing.Point(20, 20) };
                var firstNameTextBox = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(120, 20), Width = 200 };
                
                var lastNameLabel = new System.Windows.Forms.Label { Text = "Last Name:", Location = new System.Drawing.Point(20, 50) };
                var lastNameTextBox = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(120, 50), Width = 200 };

                var okButton = new System.Windows.Forms.Button 
                { 
                    Text = "Add", 
                    Location = new System.Drawing.Point(200, 120), 
                    Width = 80,
                    DialogResult = System.Windows.Forms.DialogResult.OK
                };
                
                var cancelButton = new System.Windows.Forms.Button 
                { 
                    Text = "Cancel", 
                    Location = new System.Drawing.Point(290, 120), 
                    Width = 80,
                    DialogResult = System.Windows.Forms.DialogResult.Cancel
                };

                dialog.Controls.AddRange(new System.Windows.Forms.Control[] 
                { 
                    firstNameLabel, firstNameTextBox, 
                    lastNameLabel, lastNameTextBox, 
                    okButton, cancelButton 
                });

                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var firstName = firstNameTextBox.Text.Trim();
                    var lastName = lastNameTextBox.Text.Trim();
                    
                    if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                    {
                        System.Windows.MessageBox.Show("Please enter at least a first name or last name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string fullName;
                    if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                    {
                        fullName = $"{firstName} {lastName}";
                    }
                    else if (!string.IsNullOrWhiteSpace(lastName))
                    {
                        fullName = lastName;
                        firstName = ""; // Set to empty for single-name format
                    }
                    else
                    {
                        fullName = firstName;
                        lastName = firstName; // Use firstName as lastName for single-name format
                        firstName = ""; // Set to empty for single-name format
                    }
                    
                    // Check if process server already exists
                    if (_allProcessServers.Contains(fullName, StringComparer.OrdinalIgnoreCase))
                    {
                        System.Windows.MessageBox.Show($"Process server '{fullName}' already exists.", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Add to entity table (this would need to be implemented in the service)
                    var success = await _processServerStatusService.AddProcessServerAsync(firstName, lastName);
                    
                    if (success)
                    {
                        // Refresh the list
                        await LoadAllProcessServersAsync();
                        
                        StatusMessage.Text = $"Process server '{fullName}' added successfully.";
                        StatusMessage.Foreground = System.Windows.Media.Brushes.Green;
                        
                        Console.WriteLine($"[SUCCESS] Added new process server: {fullName}");
                    }
                    else
                    {
                        StatusMessage.Text = "Failed to add process server. Please check the console for details.";
                        StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
                        
                        // Show more detailed error in console
                        Console.WriteLine($"[ERROR] Failed to add process server '{fullName}'. Check database connection and table structure.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in AddProcessServer_Click: {ex.Message}");
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteProcessServer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProcessServer))
            {
                System.Windows.MessageBox.Show("Please select a process server to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete process server '{_selectedProcessServer}'?\n\nThis will remove them from the system but will not affect existing job records.",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var success = await _processServerStatusService.DeleteProcessServerAsync(_selectedProcessServer);
                    
                    if (success)
                    {
                        // Refresh the list
                        await LoadAllProcessServersAsync();
                        
                        // Clear current selection
                        ProcessServerComboBox.SelectedItem = null;
                        _selectedProcessServer = null;
                        JobsDataGrid.ItemsSource = null;
                        
                        // Clear info
                        SelectedProcessServerTextBlock.Text = "";
                        TotalJobsTextBlock.Text = "0";
                        ActiveJobsTextBlock.Text = "0";
                        InactiveJobsTextBlock.Text = "0";
                        
                        StatusMessage.Text = $"Process server '{_selectedProcessServer}' deleted successfully.";
                        StatusMessage.Foreground = System.Windows.Media.Brushes.Green;
                        
                        Console.WriteLine($"[SUCCESS] Deleted process server: {_selectedProcessServer}");
                    }
                    else
                    {
                        StatusMessage.Text = "Failed to delete process server. Please try again.";
                        StatusMessage.Foreground = System.Windows.Media.Brushes.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in DeleteProcessServer_Click: {ex.Message}");
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentControl.Content = new AdministrationView(SessionManager.CurrentUser);
            }
        }
    }
} 