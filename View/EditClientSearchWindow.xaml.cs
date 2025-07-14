using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class EditClientSearchWindow : Window
    {
        private readonly string _connectionString;
        private readonly string _initialValue;

        public string SelectedClientFullName => lstClients.SelectedItem?.ToString() ?? "";
        public bool IsNewClient { get; set; } = false;
        public string NewClientFullName { get; set; } = string.Empty;

        public EditClientSearchWindow(string connectionString, string initialValue = "")
        {
            InitializeComponent();
            _connectionString = connectionString;
            _initialValue = initialValue;

            txtSearch.Text = initialValue;
            LoadClients(initialValue);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadClients(txtSearch.Text.Trim());
        }

        private void LoadClients(string filter)
        {
            lstClients.Items.Clear();

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

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
    cmd.Parameters.AddWithValue("filter", "%" + filter.ToLower() + "%");

                if (!string.IsNullOrWhiteSpace(filter))
                    cmd.Parameters.AddWithValue("filter", $"%{filter.ToLower()}%");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var first = reader["FirstName"]?.ToString()?.Trim();
                    var last = reader["LastName"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(first))
                        lstClients.Items.Add($"{first} {last}".Trim());
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error loading clients: " + ex.Message);
            }

            // Select if match exists
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                foreach (var item in lstClients.Items)
                {
                    if (item.ToString().Equals(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        lstClients.SelectedItem = item;
                        lstClients.ScrollIntoView(item);
                        break;
                    }
                }
            }
        }

        private void lstClients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstClients.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (lstClients.SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Please select a client from the list.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var input = Microsoft.VisualBasic.Interaction.InputBox("Enter new client name:", "Add New Client", txtSearch.Text);
            if (!string.IsNullOrWhiteSpace(input))
            {
                IsNewClient = true;
                NewClientFullName = input.Trim();
                DialogResult = true;
                Close();
            }
        }
    }
}
