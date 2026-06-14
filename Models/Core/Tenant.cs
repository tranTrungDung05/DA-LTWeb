using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.Core;

public enum TenantStatus
{
    Active = 1,
    Left = 2,
    InDebt = 3,
    Locked = 4
}

public class Tenant
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(180)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? IdentityNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(240)]
    public string? PermanentAddress { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    [StringLength(260)]
    public string? DocumentPath { get; set; }

    public ICollection<TenantRoom> TenantRooms { get; set; } = new List<TenantRoom>();

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
