using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.Services;
using CivilProcessERP.Models;
using Npgsql;

namespace CivilProcessERP.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;

            var loginService = new LoginService();

            try
            {
                var user = await loginService.AuthenticateUserAsync(username, password);

                if (user != null && user.Enabled)
                {
                    SessionManager.CurrentUser = user;
                    Console.WriteLine($"[INFO] Login attempt for user: {user.LoginName}");

                    // ✅ Setup MFA if not configured
                    if (string.IsNullOrWhiteSpace(user.MfaSecret))
                    {
                        user.MfaEnabled = true;
                        user.MfaSecret = MfaHelper.GenerateSecret();
                        await loginService.UpdateUserMfaAsync(user);

                        var setupWindow = new MfaPromptWindow(user.MfaSecret)
                        {
                            Title = "MFA Setup"
                        };
                        setupWindow.ShowDialog();
                    }

                    // ⏱️ ✅ Conditional MFA prompt
                    bool shouldPromptMfa = user.MfaEnabled && !string.IsNullOrWhiteSpace(user.MfaSecret);
                    if (shouldPromptMfa)
                    {
                        if (user.MfaLastVerifiedAt.HasValue)
                        {
                            TimeSpan sinceLast = DateTime.UtcNow - user.MfaLastVerifiedAt.Value;
                            if (sinceLast.TotalMinutes <= 60)
                            {
                                Console.WriteLine($"[INFO] Skipping MFA — last verified {sinceLast.TotalMinutes:F1} mins ago.");
                                shouldPromptMfa = false;
                            }
                        }
                    }

                    if (shouldPromptMfa)
                    {
                        var mfaPrompt = new MfaPromptWindow(user.MfaSecret);
                        bool? result = mfaPrompt.ShowDialog();

                        if (result != true)
                        {
                            MessageBox.Show("MFA verification failed.", "MFA Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // ✅ Navigate to dashboard
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    mainWindow?.LoadMainDashboardAfterLogin(user.LoginName);
                }
                else
                {
                    LoginError.Text = "Invalid credentials or user not enabled.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Login failed: {ex.Message}");
                LoginError.Text = "Login failed. Please try again.";
            }
        }
    }
}
