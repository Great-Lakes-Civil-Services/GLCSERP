using System.Windows;

namespace CivilProcessERP.Views
{
    public partial class SingleFieldDialog : Window
    {
        private string originalValue;

        public string Value => txtValue.Text.Trim();

        public SingleFieldDialog(string fieldTitle, string currentValue)
        {
            InitializeComponent();
            lblTitle.Text = $"Edit {fieldTitle}";
            txtValue.Text = currentValue ?? "";
            originalValue = txtValue.Text;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult != true)
                txtValue.Text = originalValue;
        }
    }
}
