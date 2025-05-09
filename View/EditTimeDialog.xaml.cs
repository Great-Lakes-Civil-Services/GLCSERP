using System;
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

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (TimeSpan.TryParse(txtTime.Text, out var parsed))
            {
                SelectedTime = parsed;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid time format. Use HH:MM or HH:MM AM/PM.");
            }
        }
    }
}
