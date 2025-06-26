using System;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;
using System.Drawing;
using System.IO;
using OtpNet;
using CivilProcessERP.Models;
using CivilProcessERP.Services;

namespace CivilProcessERP.Views
{
    public partial class MfaPromptWindow : Window
    {
        private readonly string _secret;

        public MfaPromptWindow(string mfaSecret)
        {
            InitializeComponent();
            _secret = mfaSecret;
            GenerateQrImage();
        }

        private void GenerateQrImage()
        {
            string user = SessionManager.CurrentUser?.LoginName ?? "user";
            string issuer = "CivilProcessERP";
            string uri = $"otpauth://totp/{issuer}:{user}?secret={_secret}&issuer={issuer}&digits=6";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using Bitmap qrBitmap = qrCode.GetGraphic(20);

            using var memory = new MemoryStream();
            qrBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            QrImage.Source = bitmapImage;
        }

        private async void Verify_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeBox.Text.Trim();
            var totp = new Totp(Base32Encoding.ToBytes(_secret));
            bool isValid = totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);

            if (isValid)
            {
                var user = SessionManager.CurrentUser;
                Console.WriteLine($"MFA code verified for user: {user?.LoginName ?? "unknown"}");

                try
                {
                    if (user != null)
                    {
                        var userService = new UserSearchService();
                        await userService.UpdateMfaLastVerifiedAsync(user.LoginName, DateTime.UtcNow); // ✅ async call
                    }

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    ErrorText.Text = $"Unexpected error: {ex.Message}";
                    ErrorText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ErrorText.Text = "Invalid code. Try again.";
                ErrorText.Visibility = Visibility.Visible;
            }
        }
    }
}
