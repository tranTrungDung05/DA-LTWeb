namespace smart_hostel_management_system.Models.ViewModels
{
    public class InvoiceEditViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public decimal RoomMonthlyPrice { get; set; } // Giá phòng hàng tháng
        public string? MainContent { get; set; }
        public DateTime DueDate { get; set; }
        public List<InvoiceServiceViewModel> Services { get; set; } = new();
    }

    public class InvoiceServiceViewModel
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
