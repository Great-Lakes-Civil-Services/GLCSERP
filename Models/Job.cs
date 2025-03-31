
namespace CivilProcessERP.Models.Job
{
    public class Job
    {
        public string JobId { get; set; }
        public string Court { get; set; }
        public string Plaintiff { get; set; }
        public string Defendant { get; set; }
        public string Address { get; set; }
        public string? Status { get; set; }
        public string CaseNumber { get; set; }
        public string ClientReference { get; set; }
        public string TypeOfWrit { get; set; }
        public string ServiceType { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string InvoiceDue { get; set; }
        public string ClientStatus { get; set; }
        public string Zone { get; set; }
        public string LastServiceDate { get; set; }

        public string JobNumber { get; set; }
        public string ClientRef { get; set; }
        public string Attorney { get; set; }
        public string TypeOfService { get; set; }
        public DateTime? DateOfService { get; set; }
        public List<AttachmentModel> Attachments { get; set; } = new List<AttachmentModel>();
        public List<ChangeEntryModel> ChangeHistory { get; set; } = new List<ChangeEntryModel>();
    }
}
