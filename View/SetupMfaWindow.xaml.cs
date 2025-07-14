using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using OtpNet;
using QRCoder;

namespace CivilProcessERP.Views
{
    public partial class SetupMfaWindow : Window
    {
        public string GeneratedSecret { get; private set; }
        private readonly string _username;

        public SetupMfaWindow(string username)
        {
            InitializeComponent();
            _username = username;

            GeneratedSecret = GenerateMfaSecret();
            SecretText.Text = GeneratedSecret;

            // Async-capable pattern even though QR generation is fast
            LoadQrCode(GeneratedSecret);
        }

        private string GenerateMfaSecret()
        {
            var secretBytes = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(secretBytes); // 32-character secret
        }

        private void LoadQrCode(string base32Secret)
        {
            try
            {
                string otpUrl = $"otpauth://totp/GLCS:{_username}?secret={base32Secret}&issuer=GLCS";

                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(otpUrl, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrBytes = qrCode.GetGraphic(5); // Size multiplier

                using var ms = new MemoryStream(qrBytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = ms;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze(); // 🔐 Prevents UI-thread locking issues

                QrImage.Source = image;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to generate QR code: " + ex.Message, "QR Code Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableMfa_Click(object sender, RoutedEventArgs e)
        {
            // Let the parent window handle saving `GeneratedSecret`
            this.DialogResult = true;
            Close();
        }
    }
}
