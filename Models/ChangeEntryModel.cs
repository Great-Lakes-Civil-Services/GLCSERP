using System;

namespace CivilProcessERP.Models
{
    public class ChangeEntryModel
    {
        public DateTime Date { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
    }
}
