using System;
using System.Windows;
using CivilProcessERP.Models.Job;  // ✅ ensure correct namespace

namespace CivilProcessERP.Views
{
    public partial class EditPaymentWindow : Window
    {
        public EditPaymentWindow(PaymentModel payment)
        {
            InitializeComponent();
            txtDate.Text = payment.Date.ToString("yyyy-MM-dd");
            txtTime.Text = payment.TimeOnly;
            txtMethod.Text = payment.Method;
            txtDescription.Text = payment.Description;
            txtAmount.Text = payment.Amount.ToString();
        }

        public string Date => txtDate.Text;
        public string Time => txtTime.Text;
        public string Method => txtMethod.Text;
        public string Description => txtDescription.Text;
        public decimal Amount => decimal.TryParse(txtAmount.Text, out var a) ? a : 0m;

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
