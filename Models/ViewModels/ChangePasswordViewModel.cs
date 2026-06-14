using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.ViewModels;

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
