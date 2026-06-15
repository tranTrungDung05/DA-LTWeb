using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace smart_hostel_management_system.Models.Core;

public class Role : IdentityRole<int>
{
    [StringLength(160)]
    public string? Description { get; set; }
}
