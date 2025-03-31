using System;

namespace CivilProcessERP.Models
{
    public class ChangeEntryModel
    {
        public DateTime Date { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string ChangedBy { get; set; }
    }
}
