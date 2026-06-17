using smart_hostel_management_system.DTOs;

namespace smart_hostel_management_system.Features.HoaDonModule
{
    public interface IHoaDonService
    {
        // ⚡ CẬP NHẬT: Thêm tham số currentUserId vào đây để không bị lệch với file triển khai (Service)
        Task<IEnumerable<HoaDonDTO>> GetLichSuHoaDonTheoPhongAsync(int roomId, int currentUserId);
        
        Task<bool> XuLyThanhToanAsync(int roomId, string phuongThuc);
        Task<int> QuetVaGuiEmailNhacNoAsync();
        Task<bool> GuiEmailTinTucToanBoTroAsync(string tieuDe, string noiDung);
        Task<Dictionary<string, decimal>> GetThongKeDoanhThuTheoThangAsync(int year);
    }
}