using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditAreaSearchWindow : Window
    {
        private readonly string _connectionString;

        public string SelectedArea => lstAreas.SelectedItem?.ToString() ?? "";

        public EditAreaSearchWindow(string connectionString, string initialValue = "")
        {
            InitializeComponent();
            _connectionString = connectionString;

            txtSearch.Text = initialValue;

            Loaded += async (_, __) => await LoadAreasAsync(initialValue); // Async on load
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadAreasAsync(txtSearch.Text.Trim());
        }

        private async Task LoadAreasAsync(string filter)
        {
            lstAreas.Items.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    string.IsNullOrWhiteSpace(filter)
                        ? @"SELECT DISTINCT zone 
                            FROM papers 
                            WHERE zone IS NOT NULL 
                            ORDER BY zone 
                            LIMIT 100"
                        : @"SELECT DISTINCT zone 
                            FROM papers 
                            WHERE zone IS NOT NULL 
                              AND LOWER(zone) LIKE @filter 
                            ORDER BY zone 
                            LIMIT 100", conn);

                if (!string.IsNullOrWhiteSpace(filter))
                    cmd.Parameters.AddWithValue("filter", $"%{filter.ToLower()}%");

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var zone = reader["zone"]?.ToString();
                    if (lstAreas != null && !string.IsNullOrWhiteSpace(zone))
                        lstAreas.Items.Add(zone);
                }

                // Automatically reselect previous value if it exists
                if (lstAreas != null && !string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    foreach (var item in lstAreas.Items)
                    {
                        if (item.ToString().Equals(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            lstAreas.SelectedItem = item;
                            lstAreas.ScrollIntoView(item);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading area list: " + ex.Message);
            }
        }

        private void lstAreas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstAreas.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstAreas.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please select an area.");
            }
        }

        // Add this method for the Other button
        private void Other_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CivilProcessERP.Views.SingleFieldDialog("Area", txtSearch.Text);
            if (dialog.ShowDialog() == true)
            {
                string newArea = dialog.Value;
                if (!string.IsNullOrWhiteSpace(newArea))
                {
                    lstAreas.SelectedItem = null;
                    txtSearch.Text = newArea;
                    if (!lstAreas.Items.Contains(newArea))
                        lstAreas.Items.Insert(0, newArea);
                    DialogResult = true;
                    Close();
                }
            }
        }
    }
}