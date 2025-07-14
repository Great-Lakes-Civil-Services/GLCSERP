using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CivilProcessERP.Models;
using CivilProcessERP.Data;
using Microsoft.EntityFrameworkCore;
using CivilProcessERP.Helpers;
using System.Windows;

namespace CivilProcessERP.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly OfficeDbContext _context;

        public ObservableCollection<LeaseAgreement> LeaseAgreements { get; private set; }

        public ICommand RefreshCommand { get; }

        public MainViewModel(OfficeDbContext context)
        {
            _context = context;
            LeaseAgreements = new ObservableCollection<LeaseAgreement>();
            RefreshCommand = new RelayCommand(async (param) => await LoadLeaseAgreementsAsync());

            // Fire-and-forget async load on startup (safe inside constructor)
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await LoadLeaseAgreementsAsync());
        }

        public async Task LoadLeaseAgreementsAsync()
        {
            try
            {
                var agreements = await _context.LeaseAgreements.ToListAsync();

                // Ensure updates happen on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LeaseAgreements.Clear();
                    foreach (var agreement in agreements)
                    {
                        LeaseAgreements.Add(agreement);
                    }
                });
            }
            catch (System.Exception ex)
            {
                // Handle DB or UI exceptions gracefully
                System.Diagnostics.Debug.WriteLine($"[ERROR] Loading agreements: {ex.Message}");
            }
        }
    }
}
