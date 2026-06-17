using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using smart_hostel_management_system.Features.HoaDonModule;

namespace smart_hostel_management_system.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Đường dẫn mặc định: api/HoaDon
    public class HoaDonController : Controller 
    {
        private readonly IHoaDonService _hoaDonService;

        public HoaDonController(IHoaDonService hoaDonService)
        {
            _hoaDonService = hoaDonService;
        }

        // =================================================================================
        // 1. ĐỊNH TUYẾN GIAO DIỆN (RETURNS HTML VIEWS) - CÁCH 2: FILE NGANG HÀNG
        // =================================================================================
        
        // Đường dẫn mở giao diện Admin: GET /api/HoaDon/admin-page
        [HttpGet("admin-page")]
        public IActionResult AdminPage()
        {
            // Tìm và trả về file Views/HoaDon/Admin.cshtml
            return View("Admin"); 
        }

        // Đường dẫn mở giao diện User: GET /api/HoaDon/user-page
        [HttpGet("user-page")]
        public IActionResult UserPage()
        {
            // Tìm và trả về file Views/HoaDon/User.cshtml
            return View("User"); 
        }

        // =================================================================================
        // 2. CÁC ĐƯỜNG DẪN API XỬ LÝ DỮ LIỆU JSON
        // =================================================================================
        
        // API nhận dữ liệu nhập tay từ Admin, tính toán và lưu trạng thái để USER có dữ liệu đóng tiền
        // JSON Body gửi lên vd: { "roomId": 101, "roomPrice": 3500000, "electricityPrice": 150000, "waterPrice": 50000, "trashPrice": 20000, "month": 6, "year": 2026 }
        // Link: POST /api/HoaDon/public-hoa-don
        [HttpPost("public-hoa-don")]
        public async Task<IActionResult> PublicHoaDon([FromBody] Requests.CreateInvoiceRequest request)
        {
            // Tính toán tổng tiền ngay tại Backend trước khi lưu xuống DB cho an toàn
            decimal totalAmount = request.RoomPrice + request.ElectricityPrice + request.WaterPrice + request.TrashPrice;

            // Logic thực tế: Gọi Service xử lý lưu vào SQL Server qua EF Core
            // var result = await _hoaDonService.TaoVaPublicHoaDonAsync(request, totalAmount);
            
            return Ok(new { 
                message = $"Da chot va public thanh cong hoa don thang {request.Month}/{request.Year} cho phong {request.RoomId}!",
                tongTien = totalAmount,
                data = request
            });
        }

        [HttpGet("lich-su/{roomId}")]
        public async Task<IActionResult> GetLichSu(int roomId, [FromQuery] int userId = 1)
        {
            // Truyền cả roomId và userId (mặc định là 1 nếu không truyền) xuống Service tính toán
            var result = await _hoaDonService.GetLichSuHoaDonTheoPhongAsync(roomId, userId);
            return Ok(result);
        }

        // vd: POST /api/HoaDon/thanh-toan/101?phuongThuc=ChuyenKhoan
        [HttpPost("thanh-toan/{roomId}")]
        public async Task<IActionResult> ThanhToan(int roomId, [FromQuery] string phuongThuc)
        {
            var thanhToanThanhCong = await _hoaDonService.XuLyThanhToanAsync(roomId, phuongThuc);
            
            if (!thanhToanThanhCong)
            {
                return NotFound($"Khong tim thay hop dong dang hoat dong cua phong {roomId} de thanh toan.");
            }

            return Ok(new { message = $"Phong {roomId} da thanh toan thanh cong qua {phuongThuc}." });
        }

        // =================================================================================
        // TÍNH NĂNG CHƯA LÀM (COMMENT LẠI HOẶC ĐỂ TẠM)
        // =================================================================================

        // vd: POST /api/HoaDon/quet-nhac-no
        [HttpPost("quet-nhac-no")]
        public async Task<IActionResult> QuetNhacNo()
        {
            int soEmailDaGui = await _hoaDonService.QuetVaGuiEmailNhacNoAsync();
            return Ok(new { message = $"Da quet xong he thong va gui email nhac no den {soEmailDaGui} cu dan." });
        }

        // vd: POST /api/HoaDon/gui-tin-tuc
        [HttpPost("gui-tin-tuc")]
        public async Task<IActionResult> GuiTinTuc([FromBody] Requests.TinTucRequest request)
        {
            var thanhCong = await _hoaDonService.GuiEmailTinTucToanBoTroAsync(request.TieuDe, request.NoiDung);
            return Ok(new { message = "Da gui thong bao tin tuc den toan bo cu dan trong khu tro." });
        }

        // vd: GET /api/HoaDon/thong-ke/2026
        [HttpGet("thong-ke/{year}")]
        public async Task<IActionResult> GetThongKe(int year)
        {
            var dataThongKe = await _hoaDonService.GetThongKeDoanhThuTheoThangAsync(year);
            return Ok(dataThongKe);
        }
    }
}

// =================================================================================
// CÁC CLASS REQUESTS ĐỂ HỨNG DATA JSON GỬI LÊN TỪ FRONTEND
// =================================================================================
namespace smart_hostel_management_system.Controllers.Requests
{
    public class CreateInvoiceRequest
    {
        public int RoomId { get; set; }
        public decimal RoomPrice { get; set; }
        public decimal ElectricityPrice { get; set; }
        public decimal WaterPrice { get; set; }
        public decimal TrashPrice { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class TinTucRequest
    {
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
    }
}