using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using CivilProcessERP.Models;
using CivilProcessERP.Services;

namespace CivilProcessERP.Views
{
    public partial class CreateUserView : UserControl
    {
        private static readonly string connString = "Host=localhost;Port=5432;Username=postgres;Password=7866;Database=mypg_database";

        public CreateUserView()
        {
            InitializeComponent();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = FullNameTextBox.Text.Trim();
            string[] nameParts = fullName.Split(' ', 2);
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[1] : "";

            string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string entity = (EntityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string loginName = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (password != confirmPassword)
            {
                MessageBox.Show("Passwords do not match.");
                return;
            }

            int roleNumber = await GetRoleNumberByNameAsync(role);
            int entityNumber = GetEntityNumber(entity);
            int changeNumber = await GetLatestChangeNumberAsync() + 1;

            var user = new UserModel
            {
                LoginName = loginName,
                FirstName = firstName,
                LastName = lastName,
                Password = password,
                RoleNumber = roleNumber,
                EntityNumber = entityNumber,
                Enabled = true,
                ChangeNumber = changeNumber,
                UpdateId = Guid.NewGuid(),
                Timestamp = DateTime.Now
            };

            bool success = await CreateUserAsync(user);

            if (success)
            {
                MessageBox.Show("User created successfully!");
                var auditLogService = new AuditLogService(connString);
                auditLogService.LogActionAsync("CreateUser", loginName, "New user created", SessionManager.CurrentUser.LoginName);
            }
            else
            {
                MessageBox.Show("Failed to create user.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FullNameTextBox.Clear();
            UsernameTextBox.Clear();
            PasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            RoleComboBox.SelectedIndex = -1;
            EntityComboBox.SelectedIndex = -1;
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

        private static async Task<int> GetRoleNumberByNameAsync(string roleName)
        {
            return await Task.Run(() =>
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT rolenumber FROM roles WHERE rolename = @role", conn);
                cmd.Parameters.AddWithValue("@role", roleName);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            });
        }

        private static async Task<int> GetLatestChangeNumberAsync()
        {
            return await Task.Run(() =>
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT MAX(changenumber) FROM users", conn);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 100;
            });
        }

        private static async Task<int> GetNextUserNumberAsync()
        {
            return await Task.Run(() =>
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT COALESCE(MAX(usernumber), 0) + 1 FROM users", conn);
                return Convert.ToInt32(cmd.ExecuteScalar());
            });
        }

        private static async Task<bool> CreateUserAsync(UserModel user)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();

                    using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE loginname = @loginname", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@loginname", user.LoginName);
                        var exists = (long)(await checkCmd.ExecuteScalarAsync());
                        if (exists > 0)
                        {
                            MessageBox.Show("A user with this login name already exists.");
                            return false;
                        }
                    }

                    int nextUserNumber = await GetNextUserNumberAsync();

                    using var cmd = new NpgsqlCommand(@"
                        INSERT INTO users (usernumber, loginname, firstname, lastname, password, rolenumber, entitynumber, enabled, changenumber, updateid, ts)
                        VALUES (@usernumber, @loginname, @firstname, @lastname, @password, @rolenumber, @entitynumber, @enabled, @changenumber, @updateid, @ts)", conn);

                    cmd.Parameters.AddWithValue("@usernumber", nextUserNumber);
                    cmd.Parameters.AddWithValue("@loginname", user.LoginName);
                    cmd.Parameters.AddWithValue("@firstname", user.FirstName);
                    cmd.Parameters.AddWithValue("@lastname", user.LastName);
                    cmd.Parameters.AddWithValue("@password", user.Password);
                    cmd.Parameters.AddWithValue("@rolenumber", user.RoleNumber);
                    cmd.Parameters.AddWithValue("@entitynumber", user.EntityNumber);
                    cmd.Parameters.AddWithValue("@enabled", user.Enabled);
                    cmd.Parameters.AddWithValue("@changenumber", user.ChangeNumber);
                    cmd.Parameters.AddWithValue("@updateid", user.UpdateId);
                    cmd.Parameters.AddWithValue("@ts", user.Timestamp);

                    return await cmd.ExecuteNonQueryAsync() > 0;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to insert user: {ex.Message}");
                return false;
            }
        }
    }
}
