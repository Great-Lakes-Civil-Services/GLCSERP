using System;
using System.Collections.Generic;
using System.Windows.Controls;
using CivilProcessERP.Views;  // Correct namespace

namespace CivilProcessERP.Services
{
    public class NavigationService
    {
        private readonly Dictionary<string, UserControl> _views = new Dictionary<string, UserControl>();

        public NavigationService()
        {
            // Register UserControls for different modules
            _views.Add("MainDashboard", new MainDashboard());
            _views.Add("LandlordTenant", new LandlordTenantView());
        }

        public UserControl GetView(string viewName)
        {
            if (_views.ContainsKey(viewName))
            {
                return _views[viewName];
            }

            throw new InvalidOperationException($"View '{viewName}' not found.");
        }
    }
}
