using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using CivilProcessERP.Models;
using CivilProcessERP.Services;

namespace CivilProcessERP.Views
{
    public partial class EditUserView : System.Windows.Controls.UserControl
    {
        private static readonly string connString = "Host=localhost;Port=5432;Username=postgres;Password=7866;Database=mypg_database";
        private readonly UserModel selectedUser;

        public EditUserView(UserModel userToEdit)
        {
            InitializeComponent();
            selectedUser = userToEdit;
            LoadUserData();
            _ = LoadPermissionsForRoleAsync(); // async fire and forget
        }

        private void LoadUserData()
        {
            FullNameTextBox.Text = $"{selectedUser.FirstName} {selectedUser.LastName}";
            UsernameTextBox.Text = selectedUser.LoginName;
            UsernameTextBox.IsReadOnly = true;

            RoleComboBox.SelectedItem = GetComboBoxItemByContent(RoleComboBox, GetRoleNameByNumber(selectedUser.RoleNumber));
            EntityComboBox.SelectedItem = GetComboBoxItemByContent(EntityComboBox, GetEntityNameByNumber(selectedUser.EntityNumber));

            EnableUserCheckbox.IsChecked = selectedUser.Enabled;
            MfaEnabledCheckBox.IsChecked = selectedUser.MfaEnabled;
        }

        private System.Windows.Controls.ComboBoxItem GetComboBoxItemByContent(System.Windows.Controls.ComboBox comboBox, string content)
        {
            foreach (System.Windows.Controls.ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString().Equals(content, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            string[] nameParts = fullName.Split(' ');
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[1] : "";

             // ✅ Validation: Role must be selected
    if (RoleComboBox.SelectedItem == null)
    {
        System.Windows.MessageBox.Show("❌ Please select a role before submitting.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

    string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

    // Optional: Entity dropdown validation too
    if (EntityComboBox.SelectedItem == null)
    {
        System.Windows.MessageBox.Show("❌ Please select an entity before submitting.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }

            //string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string entity = (EntityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            

            int roleNumber = await GetRoleNumberByNameAsync(role);
            int entityNumber = GetEntityNumber(entity);
            int changeNumber = await GetLatestChangeNumberAsync();

            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(@"
                    UPDATE users 
                    SET firstname = @firstname, lastname = @lastname, rolenumber = @rolenumber, entitynumber = @entitynumber, 
                        enabled = @enabled, mfa_enabled = @mfa, changenumber = @changenumber, updateid = @updateid, ts = @ts
                    WHERE loginname = @loginname", conn);

                cmd.Parameters.AddWithValue("@firstname", firstName);
                cmd.Parameters.AddWithValue("@lastname", lastName);
                cmd.Parameters.AddWithValue("@rolenumber", roleNumber);
                cmd.Parameters.AddWithValue("@entitynumber", entityNumber);
                cmd.Parameters.AddWithValue("@enabled", EnableUserCheckbox.IsChecked ?? false);
                cmd.Parameters.AddWithValue("@mfa", MfaEnabledCheckBox.IsChecked ?? false);
                cmd.Parameters.AddWithValue("@changenumber", changeNumber);
                cmd.Parameters.AddWithValue("@updateid", Guid.NewGuid());
                cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                cmd.Parameters.AddWithValue("@loginname", selectedUser.LoginName);

                int rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                Dispatcher.Invoke(() =>
                {
                    if (rows > 0)
                    {
                        System.Windows.MessageBox.Show("✅ User updated successfully.");
                        new AuditLogService(connString).LogActionAsync("EditUser", selectedUser.LoginName, "User info updated", SessionManager.CurrentUser.LoginName);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("⚠️ No changes were made.");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"❌ Failed to update user: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private async Task<int> GetRoleNumberByNameAsync(string roleName)
        {
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand("SELECT rolenumber FROM roles WHERE rolename = @role", conn);
            cmd.Parameters.AddWithValue("@role", roleName);
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private string GetRoleNameByNumber(int roleNumber)
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT rolename FROM roles WHERE rolenumber = @role", conn);
            cmd.Parameters.AddWithValue("@role", roleNumber);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }

        private int GetEntityNumber(string entityName) => entityName switch
        {
            "Landlord-Tenant" => 1,
            "General Civil" => 2,
            "Eviction" => 3,
            "Property Seizure" => 4,
            "Routing" => 5,
            "Server Management" => 6,
            _ => 0
        };

        private string GetEntityNameByNumber(int number) => number switch
        {
            1 => "Landlord-Tenant",
            2 => "General Civil",
            3 => "Eviction",
            4 => "Property Seizure",
            5 => "Routing",
            6 => "Server Management",
            _ => "Unknown"
        };

        private async Task<int> GetLatestChangeNumberAsync()
        {
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand("SELECT MAX(changenumber) FROM users", conn);
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return result != null ? Convert.ToInt32(result) : 100;
        }

        private async Task LoadPermissionsForRoleAsync()
        {
            try
            {
                var rolePermissionService = new RolePermissionService();
                var permissions = await Task.Run(() =>
                    rolePermissionService.GetPermissionsForRoleAsync(selectedUser.RoleNumber)).ConfigureAwait(false);

                Dispatcher.Invoke(() =>
                {
                    PermissionsDataGrid.ItemsSource = permissions;
                    Console.WriteLine($"[INFO] Loaded {permissions.Count} permissions for role #{selectedUser.RoleNumber}.");
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to load permissions: " + ex.Message);
            }
        }
    }
}
