using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditProcessServerSearchWindow : Window
    {
        private readonly string _connectionString;
        private readonly string _initialValue;

        public string SelectedProcessServer => lstServers.SelectedItem?.ToString() ?? "";
        public bool IsNewProcessServer { get; set; } = false;
        public string NewProcessServer { get; set; } = string.Empty;

        public EditProcessServerSearchWindow(string connectionString, string initialValue = "")
        {
            InitializeComponent();
            _connectionString = connectionString;
            _initialValue = initialValue;

            txtSearch.Text = initialValue;
            _ = LoadServersAsync(initialValue); // Fire-and-forget
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadServersAsync(txtSearch.Text.Trim());
        }

        private async Task LoadServersAsync(string filter)
        {
            lstServers.Items.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(
                    string.IsNullOrWhiteSpace(filter)
                        ? @"SELECT ""FirstName"", ""LastName"" 
                            FROM entity 
                            ORDER BY ""FirstName"" 
                            LIMIT 100"
                        : @"SELECT ""FirstName"", ""LastName"" 
                            FROM entity 
                            WHERE LOWER(""FirstName"") LIKE @filter OR LOWER(""LastName"") LIKE @filter
                            ORDER BY ""FirstName"" 
                            LIMIT 100", conn);

                if (!string.IsNullOrWhiteSpace(filter))
                    cmd.Parameters.AddWithValue("filter", $"%{filter.ToLower()}%");

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var first = reader["FirstName"]?.ToString()?.Trim();
                    var last = reader["LastName"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(first))
                        lstServers.Items.Add($"{first} {last}".Trim());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading process servers: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Auto-select if exact match
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                foreach (var item in lstServers.Items)
                {
                    if (item.ToString().Equals(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        lstServers.SelectedItem = item;
                        lstServers.ScrollIntoView(item);
                        break;
                    }
                }
            }
        }

        private void lstServers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstServers.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstServers.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a server before clicking Select.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Add a handler for the 'Other' button (if not present)
        private void Other_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Enter new process server name:", "Add New Process Server", txtSearch.Text);
            if (!string.IsNullOrWhiteSpace(input))
            {
                IsNewProcessServer = true;
                NewProcessServer = input.Trim();
                DialogResult = true;
                Close();
            }
        }
    }
}
