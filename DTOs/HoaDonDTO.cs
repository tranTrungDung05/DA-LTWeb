using System;

namespace smart_hostel_management_system.DTOs
{
    public class HoaDonDTO
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string TenantEmail { get; set; } = string.Empty;

        public decimal RoomPrice { get; set; }
        public decimal ElectricityPrice { get; set; }
        public decimal WaterPrice { get; set; }
        public decimal ServicePrice { get; set; }
        
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Chua thanh toan"; // Chua thanh toan, Da thanh toan, Qua han
        public string PaymentMethod { get; set; } = "Chua cau hinh";
        
        public DateTime CreatedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }

        //For statistics
        public int Month { get; set; }
        public int Year { get; set; }

        // =================================================================================
        // ⚡ THÊM MỚI ĐỂ PHỤC VỤ BÀI TOÁN NGƯỜI ĐẠI DIỆN VÀ PHÂN QUYỀN TRÊN UI
        // =================================================================================
        
        // Tên của người đại diện/trưởng phòng (để thành viên ở ghép biết ai sẽ đi đóng tiền)
        public string RepresentativeName { get; set; } = string.Empty;

        // Cờ hiệu để Frontend kiểm tra: 
        // Đạt giá trị `true` nếu User đang đăng nhập CHÍNH LÀ người đại diện của phòng đó
        public bool IsRepresentative { get; set; } = false;
    }
}