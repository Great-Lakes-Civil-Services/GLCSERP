using System.Windows;

namespace CivilProcessERP.Views
{
    public partial class EditFieldDialog : Window
    {
        private string originalFirstName = "";
        private string originalLastName = "";

        public string FirstName => txtFirstName.Text.Trim();
        public string LastName => txtLastName.Text.Trim();
        public string EditedFullName => $"{FirstName} {LastName}".Trim();

        // Constructor with full name string
        public EditFieldDialog(string fieldTitle, string currentFullName)
        {
            InitializeComponent();
            lblTitle.Text = $"Edit {fieldTitle}";

            var parts = currentFullName.Split(' ', 2);
            originalFirstName = parts.Length > 0 ? parts[0] : "";
            originalLastName = parts.Length > 1 ? parts[1] : "";

            txtFirstName.Text = originalFirstName;
            txtLastName.Text = originalLastName;
        }

        // Constructor with separate first and last name
        public EditFieldDialog(string fieldTitle, string firstName, string lastName)
        {
            InitializeComponent();
            lblTitle.Text = $"Edit {fieldTitle}";

            originalFirstName = firstName;
            originalLastName = lastName;

            txtFirstName.Text = firstName;
            txtLastName.Text = lastName;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult != true)
            {
                // Restore original values if user presses X or cancels
                txtFirstName.Text = originalFirstName;
                txtLastName.Text = originalLastName;
            }
        }
    }
}