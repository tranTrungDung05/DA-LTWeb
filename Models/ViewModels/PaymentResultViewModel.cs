namespace smart_hostel_management_system.Models.ViewModels
{
    public class PaymentResultViewModel
    {
        public bool IsValid { get; set; }
        public bool IsSuccessful { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionCode { get; set; }
    }
}
