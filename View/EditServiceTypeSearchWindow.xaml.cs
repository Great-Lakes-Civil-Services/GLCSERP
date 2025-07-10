using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditServiceTypeSearchWindow : Window
    {
        private readonly string _connectionString;

        public string SelectedServiceType => lstServiceTypes.SelectedItem?.ToString() ?? "";

        public EditServiceTypeSearchWindow(string connectionString, string initialValue = "")
        {
            InitializeComponent();
            _connectionString = connectionString;

            txtSearch.Text = initialValue;
            _ = LoadServiceTypesAsync(initialValue); // Fire-and-forget async call
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadServiceTypesAsync(txtSearch.Text.Trim());
        }

        private async Task LoadServiceTypesAsync(string filter = "")
        {
            lstServiceTypes.Items.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    string.IsNullOrWhiteSpace(filter)
                        ? "SELECT DISTINCT servicename FROM typeservice ORDER BY servicename LIMIT 100"
                        : "SELECT DISTINCT servicename FROM typeservice WHERE LOWER(servicename) LIKE @filter ORDER BY servicename LIMIT 100",
                    conn);

                if (!string.IsNullOrWhiteSpace(filter))
                    cmd.Parameters.AddWithValue("filter", $"%{filter.ToLower()}%");

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader["servicename"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                        lstServiceTypes.Items.Add(name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading service types: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstServiceTypes.SelectedItem == null)
            {
                MessageBox.Show("Please select a service type.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void lstServiceTypes_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstServiceTypes.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Add this method for the Other button
        private void Other_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CivilProcessERP.Views.SingleFieldDialog("Service Type", txtSearch.Text);
            if (dialog.ShowDialog() == true)
            {
                string newServiceType = dialog.Value;
                if (!string.IsNullOrWhiteSpace(newServiceType))
                {
                    lstServiceTypes.SelectedItem = null;
                    txtSearch.Text = newServiceType;
                    if (!lstServiceTypes.Items.Contains(newServiceType))
                        lstServiceTypes.Items.Insert(0, newServiceType);
                    DialogResult = true;
                    Close();
                }
            }
        }
    }
}
