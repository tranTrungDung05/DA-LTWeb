using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Models.ViewModels
{
    public class ContractFormViewModel
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public ContractStatus Status { get; set; } = ContractStatus.Active;
        public int TenantId { get; set; }
        public int RoomId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(12);
        public string? Content { get; set; }
        public string? Note { get; set; }
    }
}
