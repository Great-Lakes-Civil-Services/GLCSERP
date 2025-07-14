using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;
using CivilProcessERP.Services;

namespace CivilProcessERP.Views
{
    public partial class DeleteUserView : System.Windows.Controls.UserControl
    {
        private static readonly string connString = "Host=localhost;Port=5432;Username=postgres;Password=7866;Database=mypg_database";
        private readonly UserSearchService _userSearchService = new UserSearchService();

        public DeleteUserView()
        {
            InitializeComponent();
        }

        private async void UsernameSearchComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string query = UsernameSearchComboBox.Text.Trim();

            if (query.Length >= 2)
            {
                var results = await Task.Run(() => _userSearchService.SearchUsersAsync(query));
                UsernameSearchComboBox.ItemsSource = results;
                UsernameSearchComboBox.IsDropDownOpen = true;
            }
        }

       private async void ConfirmDeleteButton_Click(object sender, RoutedEventArgs e)
{
    string usernameToDelete = UsernameSearchComboBox.Text.Trim();

    if (string.IsNullOrWhiteSpace(usernameToDelete))
    {
        System.Windows.MessageBox.Show("Please enter a username to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    var confirm = System.Windows.MessageBox.Show(
        $"Are you sure you want to delete user '{usernameToDelete}'?",
        "Confirm Deletion",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    if (confirm != MessageBoxResult.Yes)
        return;

    try
    {
        bool deleted = await Task.Run(async () =>
        {
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            int? userNumber = null;

            // 🔍 Get usernumber from loginname
            await using (var getUserCmd = new NpgsqlCommand("SELECT usernumber FROM users WHERE loginname = @login", conn))
            {
                getUserCmd.Parameters.AddWithValue("@login", usernameToDelete);
                var result = await getUserCmd.ExecuteScalarAsync();
                if (result != null && result is int num)
                    userNumber = num;
            }

            if (userNumber == null)
                return false; // User not found

            // 🗑️ Delete related permissions
            await using (var deletePermsCmd = new NpgsqlCommand("DELETE FROM userpermissions WHERE usernumber = @uid", conn))
            {
                deletePermsCmd.Parameters.AddWithValue("@uid", userNumber.Value);
                await deletePermsCmd.ExecuteNonQueryAsync();
            }

            // 🗑️ Delete the user
            await using (var deleteUserCmd = new NpgsqlCommand("DELETE FROM users WHERE loginname = @login", conn))
            {
                deleteUserCmd.Parameters.AddWithValue("@login", usernameToDelete);
                int rowsDeleted = await deleteUserCmd.ExecuteNonQueryAsync();

                if (rowsDeleted > 0)
                {
                    var audit = new AuditLogService(connString);
                    await audit.LogActionAsync("DeleteUser", usernameToDelete, "User deleted", SessionManager.CurrentUser.LoginName);
                    return true;
                }
            }

            return false;
        });

        if (deleted)
        {
            System.Windows.MessageBox.Show($"User '{usernameToDelete}' deleted successfully.", "User Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            System.Windows.MessageBox.Show($"No user found with username '{usernameToDelete}' or deletion failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        System.Windows.MessageBox.Show($"Unexpected error: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameSearchComboBox.Text = string.Empty;
            UsernameSearchComboBox.SelectedItem = null;
            UsernameSearchComboBox.ItemsSource = null;
            UsernameSearchComboBox.IsDropDownOpen = false;
        }
    }
}
