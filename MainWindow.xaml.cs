using System.Windows;
using CivilProcessERP.ViewModels;

namespace CivilProcessERP
{
    public partial class MainWindow : Window
    {
        public MainWindow()  // <-- Default constructor
        {
            InitializeComponent();
        }

        public MainWindow(MainDashboardViewModel vm)  // <-- DI constructor
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
