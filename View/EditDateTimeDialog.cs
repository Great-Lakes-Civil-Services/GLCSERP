using System;
using System.Globalization;
using System.Windows;

namespace CivilProcessERP.Views
{
    public partial class EditDateTimeDialog : Window
    {
        public DateTime? SelectedDateTime { get; private set; }

        public EditDateTimeDialog(string fieldTitle, DateTime? currentDateTime)
        {
            InitializeComponent();
            lblTitle.Text = $"Edit {fieldTitle}";

            datePicker.SelectedDate = currentDateTime?.Date ?? DateTime.Today;
            txtTime.Text = currentDateTime?.ToString("HH:mm") ?? DateTime.Now.ToString("HH:mm");
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (datePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select a valid date.");
                return;
            }

            if (!TimeSpan.TryParseExact(txtTime.Text, "hh\\:mm", CultureInfo.InvariantCulture, out var time))
            {
                MessageBox.Show("Please enter a valid time in HH:mm format.");
                return;
            }

            SelectedDateTime = datePicker.SelectedDate.Value.Date + time;
            DialogResult = true;
            Close();
        }
    }
}