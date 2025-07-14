using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;
using CivilProcessERP.Services;
using CivilProcessERP.Models;

namespace CivilProcessERP.Views
{
    public partial class EnableDisableUserView : System.Windows.Controls.UserControl
    {
        private static readonly string connString = "Host=localhost;Port=5432;Username=postgres;Password=7866;Database=mypg_database";

        private readonly UserSearchService _userSearchService = new UserSearchService();
        private List<UserModel> _searchResults = new();

        public EnableDisableUserView()
        {
            InitializeComponent();
        }

        private async void UsernameSearchComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string query = UsernameSearchComboBox.Text.Trim();

            if (query.Length >= 2)
            {
                _searchResults = await Task.Run(() => _userSearchService.SearchUsersAsync(query));
                UsernameSearchComboBox.ItemsSource = _searchResults;
                UsernameSearchComboBox.IsDropDownOpen = true;
            }
        }

        private async void ToggleStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameSearchComboBox.SelectedItem is not UserModel selectedUser)
            {
                System.Windows.MessageBox.Show("Please select a valid user from the dropdown.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string username = selectedUser.LoginName;

            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync().ConfigureAwait(false);

                // 1. Get current status
                await using var getCmd = new NpgsqlCommand("SELECT enabled FROM users WHERE loginname = @login", conn);
                getCmd.Parameters.AddWithValue("@login", username);

                object result = await getCmd.ExecuteScalarAsync().ConfigureAwait(false);

                if (result == null)
                {
                    Dispatcher.Invoke(() =>
                        System.Windows.MessageBox.Show($"No user found with username: {username}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }

                bool currentStatus = Convert.ToBoolean(result);
                bool newStatus = !currentStatus;

                // 2. Update the status
                await using var updateCmd = new NpgsqlCommand(@"
                    UPDATE users 
                    SET enabled = @newstatus, changenumber = changenumber + 1, updateid = @updateid, ts = @ts 
                    WHERE loginname = @login", conn);

                updateCmd.Parameters.AddWithValue("@newstatus", newStatus);
                updateCmd.Parameters.AddWithValue("@updateid", Guid.NewGuid());
                updateCmd.Parameters.AddWithValue("@ts", DateTime.Now);
                updateCmd.Parameters.AddWithValue("@login", username);

                int affected = await updateCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                Dispatcher.Invoke(() =>
                {
                    if (affected > 0)
                    {
                        System.Windows.MessageBox.Show(
                            $"User '{username}' is now {(newStatus ? "enabled" : "disabled")}.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        new AuditLogService(connString).LogActionAsync(
                            "ToggleUserStatus",
                            username,
                            $"User status toggled to {(newStatus ? "enabled" : "disabled")}",
                            SessionManager.CurrentUser.LoginName);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to update user status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show($"Unexpected error: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameSearchComboBox.Text = "";
            UsernameSearchComboBox.SelectedItem = null;
            UsernameSearchComboBox.ItemsSource = null;
        }
    }
}
