namespace CivilProcessERP.ViewModels
{
    public class MainViewModels : BaseViewModel
    {
        // This container class can be expanded to include additional view models.
        public MainViewModel LeaseAgreementViewModel { get; set; }

        public MainViewModels(MainViewModel leaseAgreementViewModel)
        {
            this.LeaseAgreementViewModel = leaseAgreementViewModel;
        }
    }
}
