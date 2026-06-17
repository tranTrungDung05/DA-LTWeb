using Microsoft.AspNetCore.Identity;
using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Models.Core
{
    public class Account : IdentityUser<int>
    {
        public string FullName { get; set; } = string.Empty;
        public RoleType Role { get; set; } = RoleType.User;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Tenant? Tenant { get; set; }
    }
}