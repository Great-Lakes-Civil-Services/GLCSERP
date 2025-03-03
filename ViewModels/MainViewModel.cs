using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CivilProcessERP.Models;
using CivilProcessERP.Data;
using Microsoft.EntityFrameworkCore;
using CivilProcessERP.Helpers;

namespace CivilProcessERP.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly OfficeDbContext _context;

        public ObservableCollection<LeaseAgreement> LeaseAgreements { get; set; }
        public ICommand RefreshCommand { get; }

        public MainViewModel(OfficeDbContext context)
        {
            _context = context;
            LeaseAgreements = new ObservableCollection<LeaseAgreement>();
            RefreshCommand = new RelayCommand(async (param) => await LoadLeaseAgreements());

            // Optionally load data on startup.
            Task.Run(async () => await LoadLeaseAgreements());
        }

        public async Task LoadLeaseAgreements()
        {
            var agreements = await _context.LeaseAgreements.ToListAsync();
            LeaseAgreements.Clear();
            foreach (var agreement in agreements)
            {
                LeaseAgreements.Add(agreement);
            }
        }
    }
}
