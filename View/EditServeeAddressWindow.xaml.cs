using System.Windows;

namespace CivilProcessERP.Views
{
    public partial class EditServeeAddressWindow : Window
    {
        public string Address1 => txtAddress1.Text;
        public string Address2 => txtAddress2.Text;
        public string City => txtCity.Text;
        public string State => txtState.Text;
        public string Zip => txtZip.Text;

        public EditServeeAddressWindow(string address1, string address2, string city, string state, string zip)
        {
            InitializeComponent();
            txtAddress1.Text = address1;
            txtAddress2.Text = address2;
            txtCity.Text = city;
            txtState.Text = state;
            txtZip.Text = zip;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
