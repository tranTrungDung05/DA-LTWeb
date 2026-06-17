namespace smart_hostel_management_system.Models.Core
{
    public class ContractDetail
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public virtual Contract? Contract { get; set; }

        public int RoomId { get; set; }
        public virtual Room? Room { get; set; }

        public string? Content { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}