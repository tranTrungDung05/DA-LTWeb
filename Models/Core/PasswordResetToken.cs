using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.Core;

public class PasswordResetToken
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public int UserId { get; set; }

    public AppUser? User { get; set; }
}
