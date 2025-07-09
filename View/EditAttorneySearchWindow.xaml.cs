using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditAttorneySearchWindow : Window
    {
        private readonly string _connectionString;
        private readonly string _initialValue;

        public string SelectedAttorneyFullName => lstAttorneys.SelectedItem?.ToString() ?? "";
        public bool IsNewAttorney { get; set; } = false;
        public string NewAttorneyFullName { get; set; } = string.Empty;

        public EditAttorneySearchWindow(string connectionString, string initialValue = "")
        {
            InitializeComponent();
            _connectionString = connectionString;
            _initialValue = initialValue;

            txtSearch.Text = initialValue;

            Loaded += async (_, __) => await LoadAttorneysAsync(initialValue); // Async load on start
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadAttorneysAsync(txtSearch.Text.Trim());
        }

        private async Task LoadAttorneysAsync(string filter)
        {
            lstAttorneys.Items.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(@"
                    SELECT ""FirstName"", ""LastName""
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
                        lstAttorneys.Items.Add($"{first} {last}".Trim());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading attorneys: " + ex.Message);
            }

            // Restore selection if possible
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                foreach (var item in lstAttorneys.Items)
                {
                    if (item.ToString().Equals(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        lstAttorneys.SelectedItem = item;
                        lstAttorneys.ScrollIntoView(item);
                        break;
                    }
                }
            }
        }

        private void lstAttorneys_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstAttorneys.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstAttorneys.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select an attorney from the list.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var input = Microsoft.VisualBasic.Interaction.InputBox("Enter new attorney name:", "Add New Attorney", txtSearch.Text);
            if (!string.IsNullOrWhiteSpace(input))
            {
                IsNewAttorney = true;
                NewAttorneyFullName = input.Trim();
                DialogResult = true;
                Close();
            }
        }
    }
}
