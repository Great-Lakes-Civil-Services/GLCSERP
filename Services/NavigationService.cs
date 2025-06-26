using System.Windows.Controls;
using System.Windows; // Add this line to use MessageBoxButton and MessageBoxImage
using CivilProcessERP.Views; // Add this line to include the namespace where MainDashboard is defined
using CivilProcessERP.ViewModels; // Add this line to include the namespace where LandlordTenantViewModel is defined
using CivilProcessERP.Models.Job; // Add this line to include the namespace where Job is defined
using CivilProcessERP.ViewModels; // Add this line to include the namespace where LandlordTenantViewModel is defined

public class NavigationService
{
    private readonly Dictionary<string, Func<UserControl>> _viewMappings;
    private MainDashboard? _dashboardInstance; // ✅ Singleton instance

    public NavigationService()
    {
        _viewMappings = new Dictionary<string, Func<UserControl>>
        {
            { "Dashboard", GetDashboardInstance }, // ✅ Use the singleton instance method
            { "Executions", () => new ExecutionsView() },
            { "GeneralCivil", () => new GeneralCivilView() },
            { "LandlordTenant", () => new LandlordTenantView(new Job()) { DataContext = new LandlordTenantViewModel(new Job(), false) } },
            { "Evictions", () => new EvictionsView() },
            { "ServerMGT", () => new ServerManagementView() },
            { "Logistics", () => new LogisticsView() },
             { "Administration", () => 
        {
            if (SessionManager.CurrentUser == null)
            {
                MessageBox.Show("No user session. Please login again.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new UserControl(); // Or redirect to login
            }

            return new AdministrationView(SessionManager.CurrentUser);
        }
    },
            { "HuronPortal", () => new HuronPortalView() },
            { "Courts", () => new CourtsView() },
            { "Clients", () => new ClientsView() },
            { "EmployeesCorner", () => new EmployessCornerView() }
        };
    }

    // ✅ Ensure only ONE instance of MainDashboard exists
    private UserControl GetDashboardInstance()
    {
        if (_dashboardInstance == null)
        {
            Console.WriteLine("[INFO] Creating MainDashboard instance.");
            _dashboardInstance = new MainDashboard(this);
        }
        else
        {
            Console.WriteLine("[INFO] Returning existing MainDashboard instance.");
        }
        return _dashboardInstance;
    }

    public UserControl GetView(string viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName))
        {
            Console.WriteLine("[ERROR] View name is null or empty.");
            return new UserControl { Content = new TextBlock { Text = "Error: View name is missing!", Foreground = System.Windows.Media.Brushes.Red } };
        }

        if (_viewMappings.TryGetValue(viewName, out var createView))
        {
            Console.WriteLine($"[DEBUG] Navigating to View: {viewName}");
            return createView();
        }
        else
        {
            Console.WriteLine($"[ERROR] View Not Found: {viewName}");
            return new UserControl { Content = new TextBlock { Text = $"Error: {viewName} not implemented!", Foreground = System.Windows.Media.Brushes.Red } };
        }
    }

}
