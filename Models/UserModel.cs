using System;

namespace CivilProcessERP.Models
{
    public class UserModel
    {
        public int UserNumber { get; set; }

        public string LoginName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public int RoleNumber { get; set; }

        public int EntityNumber { get; set; }

        // ✅ Password for login
        public string Password { get; set; } = string.Empty;

        // ✅ Required for tracking changes
        public int ChangeNumber { get; set; }

        public Guid UpdateId { get; set; }

        public DateTime Timestamp { get; set; }

        // ✅ Optional: MFA-related support
        public bool Enabled { get; set; }

        public bool MfaEnabled { get; set; }

        public string MfaSecret { get; set; } = string.Empty;
        
public DateTime? MfaLastVerifiedAt { get; set; }

    }
}
