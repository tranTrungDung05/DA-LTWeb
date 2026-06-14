using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.Core;

public class ContractFile
{
    public int Id { get; set; }

    [Required, StringLength(180)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(260)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int ContractId { get; set; }

    public Contract? Contract { get; set; }
}
