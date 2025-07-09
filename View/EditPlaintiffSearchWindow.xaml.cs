using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditPlaintiffSearchWindow : Window
    {
        private readonly string _connectionString;
        public string SelectedPlaintiffFullName => lstPlaintiffs.SelectedItem?.ToString() ?? "";
        public bool IsNewPlaintiff { get; set; } = false;
        public string NewPlaintiffFullName { get; set; } = string.Empty;

        public EditPlaintiffSearchWindow(string connectionString, string currentPlaintiff = "")
        {
            InitializeComponent();
            _connectionString = connectionString;
            txtSearch.Text = currentPlaintiff;
            _ = LoadPlaintiffsAsync(currentPlaintiff); // fire and forget
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadPlaintiffsAsync(txtSearch.Text.Trim());
        }

        private async Task LoadPlaintiffsAsync(string filter)
        {
            lstPlaintiffs.Items.Clear();

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
                    string first = reader["FirstName"]?.ToString();
                    string last = reader["LastName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(first))
                        lstPlaintiffs.Items.Add($"{first} {last}".Trim());
                }

                // Reselect current
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    foreach (var item in lstPlaintiffs.Items)
                    {
                        if (item.ToString().Equals(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            lstPlaintiffs.SelectedItem = item;
                            lstPlaintiffs.ScrollIntoView(item);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading plaintiffs: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lstPlaintiffs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstPlaintiffs.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstPlaintiffs.SelectedItem == null)
            {
                MessageBox.Show("Please select a plaintiff.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Add a handler for the 'Other' button (if not present)
        private void Other_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Enter new plaintiff name:", "Add New Plaintiff", txtSearch.Text);
            if (!string.IsNullOrWhiteSpace(input))
            {
                IsNewPlaintiff = true;
                NewPlaintiffFullName = input.Trim();
                DialogResult = true;
                Close();
            }
        }
    }
}
