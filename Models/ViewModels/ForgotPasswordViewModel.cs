using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.ViewModels;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
