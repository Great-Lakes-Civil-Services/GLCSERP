using System;
using System.Threading.Tasks;
using System.Windows;

namespace CivilProcessERP.Views
{
    public partial class EditTimeDialog : Window
    {
        public TimeSpan? SelectedTime { get; private set; }

        public EditTimeDialog(string fieldTitle, TimeSpan? currentTime)
        {
            InitializeComponent();
            lblTitle.Text = $"Edit {fieldTitle}";
            txtTime.Text = currentTime.HasValue ? currentTime.Value.ToString(@"hh\:mm") : "";
        }

        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            bool valid = await ValidateTimeAsync(txtTime.Text);
            if (!valid)
            {
                MessageBox.Show("Invalid time format. Use HH:MM or HH:MM AM/PM.", "Time Format Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedTime = TimeSpan.Parse(txtTime.Text); // safe to parse after validation
            DialogResult = true;
            Close();
        }

        private Task<bool> ValidateTimeAsync(string input)
        {
            return Task.FromResult(TimeSpan.TryParse(input, out _));
        }
    }
}
