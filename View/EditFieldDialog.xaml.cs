using System.Windows;

namespace CivilProcessERP.Views
{
    public partial class EditFieldDialog : Window
    {
        private string originalFirstName = "";
        private string originalLastName = "";

        // ✅ These properties are now exposed to be accessed from outside
        public string FirstName => txtFirstName.Text.Trim();
        public string LastName => txtLastName.Text.Trim();
        public string EditedFullName => $"{FirstName} {LastName}".Trim();

        // ✅ Original constructor (2 args - keeps backward compatibility)
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

        // ✅ NEW constructor with 3 args (this is what your code is expecting)
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
            // Optional: this block is useful if you modify the fields and press X (close)
            if (DialogResult != true)
            {
                txtFirstName.Text = originalFirstName;
                txtLastName.Text = originalLastName;
            }
        }
    }
}
