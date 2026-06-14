using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smart_hostel_management_system.Models.Core;

public enum ContractStatus
{
    Active = 1,
    ExpiringSoon = 2,
    Extended = 3,
    Terminated = 4
}

public class Contract
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string ContractNumber { get; set; } = string.Empty;

    public int RoomId { get; set; }

    public Room? Room { get; set; }

    public int TenantId { get; set; }

    public Tenant? Tenant { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(12);

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1_000_000_000)]
    public decimal DepositAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1_000_000_000)]
    public decimal MonthlyPrice { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Active;

    [StringLength(500)]
    public string? Note { get; set; }

    public ICollection<ContractFile> Files { get; set; } = new List<ContractFile>();
}
