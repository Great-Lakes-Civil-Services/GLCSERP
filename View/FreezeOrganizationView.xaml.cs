using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class FreezeOrganizationView : UserControl
    {
        private static readonly string connString = "Host=localhost;Port=5432;Username=postgres;Password=7866;Database=mypg_database";

        public FreezeOrganizationView()
        {
            InitializeComponent();
        }

        private async void ToggleFreezeButton_Click(object sender, RoutedEventArgs e)
        {
            string orgName = OrganizationComboBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(orgName))
            {
                MessageBox.Show("Please enter a valid organization name or ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                // Check if the organization is already frozen
                await using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM frozen_organizations WHERE LOWER(\"FirmName\") = LOWER(@firm)", conn);
                checkCmd.Parameters.AddWithValue("@firm", orgName);
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                string message;

                if (count > 0)
                {
                    await using var deleteCmd = new NpgsqlCommand("DELETE FROM frozen_organizations WHERE LOWER(\"FirmName\") = LOWER(@firm)", conn);
                    deleteCmd.Parameters.AddWithValue("@firm", orgName);
                    await deleteCmd.ExecuteNonQueryAsync();
                    message = $"Organization '{orgName}' has been unfrozen.";
                }
                else
                {
                    await using var insertCmd = new NpgsqlCommand("INSERT INTO frozen_organizations (\"FirmName\") VALUES (@firm)", conn);
                    insertCmd.Parameters.AddWithValue("@firm", orgName);
                    await insertCmd.ExecuteNonQueryAsync();
                    message = $"Organization '{orgName}' has been frozen.";
                }

                MessageBox.Show(message, "Action Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Failed to toggle freeze: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OrganizationComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            string input = OrganizationComboBox.Text.Trim();
            if (input.Length < 2) return;

            try
            {
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    "SELECT DISTINCT \"FirmName\" FROM entity WHERE \"FirmName\" IS NOT NULL AND LOWER(\"FirmName\") LIKE @pattern ORDER BY \"FirmName\" LIMIT 10", conn);
                cmd.Parameters.AddWithValue("@pattern", input.ToLower() + "%");

                var matches = new List<string>();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    matches.Add(reader.GetString(0));
                }

                OrganizationComboBox.ItemsSource = matches;
                OrganizationComboBox.IsDropDownOpen = matches.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Auto-suggest failed: {ex.Message}");
            }
        }

        private void OrganizationComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            OrganizationComboBox.IsDropDownOpen = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OrganizationComboBox.Text = string.Empty;
            OrganizationComboBox.ItemsSource = null;
            OrganizationComboBox.IsDropDownOpen = false;
        }
    }
}
