namespace CivilProcessERP.Models
{
    public class AttachmentModel
    {
        public string Purpose { get; set; }
        public string Format { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string FilePath { get; set; } // Needed to open the file
    }
}
