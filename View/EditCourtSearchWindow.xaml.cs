using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditCourtSearchWindow : Window
    {
        private readonly string _connectionString;

        public string SelectedCourt => lstCourts.SelectedItem?.ToString() ?? "";

        public EditCourtSearchWindow(string connectionString, string initialValue = "")
        {
            InitializeComponent();
            _connectionString = connectionString;

            txtSearch.Text = initialValue; // ✅ Pre-fill the search box
            Loaded += async (_, __) => await LoadCourtsAsync(initialValue); // ✅ Load filtered list on window load
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadCourtsAsync(txtSearch.Text.Trim());
        }

        private async Task LoadCourtsAsync(string filter)
        {
            lstCourts.Items.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = string.IsNullOrWhiteSpace(filter)
                    ? "SELECT name FROM courts ORDER BY name LIMIT 100"
                    : "SELECT name FROM courts WHERE LOWER(name) LIKE @search ORDER BY name LIMIT 100";

                await using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(filter))
                    cmd.Parameters.AddWithValue("search", $"%{filter.ToLower()}%");

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader["name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                        lstCourts.Items.Add(name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading courts: " + ex.Message);
            }

            // ✅ Retain previous selection
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                foreach (var item in lstCourts.Items)
                {
                    if (item.ToString().Equals(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        lstCourts.SelectedItem = item;
                        lstCourts.ScrollIntoView(item);
                        break;
                    }
                }
            }
        }

        private void lstCourts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstCourts.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstCourts.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a court before clicking OK.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (lstCourts.SelectedItem == null)
            {
                MessageBox.Show("Please select a court from the list.");
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
