using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditPlaintiffsSearchWindow : Window
    {
        private readonly string _connectionString;
        public string SelectedPlaintiffsFullName => lstPlaintiffss.SelectedItem?.ToString() ?? "";
        public bool IsNewPlaintiffs { get; set; } = false;
        public string NewPlaintiffsFullName { get; set; } = string.Empty;

        public EditPlaintiffsSearchWindow(string connectionString, string currentPlaintiffs = "")
        {
            InitializeComponent();
            _connectionString = connectionString;
            txtSearchs.Text = currentPlaintiffs;
            _ = LoadPlaintiffssAsync(currentPlaintiffs); // fire and forget
        }

        private async void txtSearchs_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadPlaintiffssAsync(txtSearchs.Text.Trim());
        }

        private async Task LoadPlaintiffssAsync(string filter)
        {
            lstPlaintiffss.Items.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(
                    string.IsNullOrWhiteSpace(filter)
                        ? @"SELECT ""firstname"", ""lastname"" 
                            FROM serveedetails 
                            ORDER BY ""firstname"" 
                            LIMIT 100"
                        : @"SELECT ""firstname"", ""lastname"" 
                            FROM serveedetails 
                            WHERE LOWER(""firstname"") LIKE @filter OR LOWER(""lastname"") LIKE @filter
                            ORDER BY ""firstname"" 
                            LIMIT 100", conn);

                if (!string.IsNullOrWhiteSpace(filter))
                    cmd.Parameters.AddWithValue("filter", $"%{filter.ToLower()}%");

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string first = reader["FirstName"]?.ToString() ?? string.Empty;
                    string last = reader["LastName"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(first))
                        lstPlaintiffss.Items.Add($"{first} {last}".Trim());
                }

                // Reselect current
                if (!string.IsNullOrWhiteSpace(txtSearchs.Text))
                {
                    foreach (var item in lstPlaintiffss.Items)
                    {
                        if (item.ToString().Equals(txtSearchs.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            lstPlaintiffss.SelectedItem = item;
                            lstPlaintiffss.ScrollIntoView(item);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading plaintiffs: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lstPlaintiffss_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstPlaintiffss.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Selects_Click(object sender, RoutedEventArgs e)
        {
            if (lstPlaintiffss.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Please select a plaintiffs.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancels_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Other_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = new EditFieldDialog("Edit Plaintiffs", "");
            if (inputDialog.ShowDialog() == true)
            {
                IsNewPlaintiffs = true;
                NewPlaintiffsFullName = inputDialog.EditedFullName;
                DialogResult = true;
            }
        }
    }
}
