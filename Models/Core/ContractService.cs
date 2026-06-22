namespace smart_hostel_management_system.Models.Core
{
    public class ContractService
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public virtual Contract? Contract { get; set; }
        
        public int ServiceId { get; set; }
        public virtual Service? Service { get; set; }
        
        public decimal PriceAtContract { get; set; } // Giá chốt khi ký hợp đồng
    }
}