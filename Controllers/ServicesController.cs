using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServicesController : Controller
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        
        // 1. Hàm GET: LẤY DANH SÁCH tất cả các dịch vụ (Tiền điện, nước...) hiển thị ra màn hình
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .AsNoTracking() // Dùng AsNoTracking để tối ưu tốc độ vì mình chỉ XEM dữ liệu chứ không chỉnh sửa gì ở trang này
                .OrderBy(s => s.Name) // Sắp xếp theo tên A-Z
                .ToListAsync(); // Lấy tất cả đổ vào danh sách List
            return View(services); // Gửi dữ liệu vừa lấy sang file Index.cshtml để hiển thị
        }

        public IActionResult Create()
        {
            return View(new Service());
        }

        
        // 2. Hàm POST: Chạy khi người dùng bấm nút Submit "Lưu" ở màn hình THÊM MỚI
        [HttpPost]
        [ValidateAntiForgeryToken] // Chống hack giả mạo form từ trang web khác
        public async Task<IActionResult> Create(Service service)
        {
            await ValidateService(service); // Gọi hàm kiểm tra dữ liệu xem có bị trùng tên hay giá tiền bị âm không
            if (!ModelState.IsValid)
            {
                // Nếu dữ liệu bị lỗi (ví dụ không nhập tên), trả lại trang View kèm theo thông báo lỗi màu đỏ để bắt người dùng sửa
                return View(service);
            }

            _context.Services.Add(service); // Chuẩn bị thêm dữ liệu mới vào bộ nhớ EF
            await _context.SaveChangesAsync(); // Chính thức lưu dữ liệu xuống Database (SQL Server)
            TempData["Success"] = "Đã thêm dịch vụ thành công."; // Tạo thông báo màu xanh trên góc màn hình
            return RedirectToAction(nameof(Index)); // Xong việc thì tự động điều hướng quay về trang Danh sách
        }

        
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        // 3. Hàm POST: Chạy khi người dùng bấm nút Submit "Lưu" ở màn hình CHỈNH SỬA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id)
            {
                // Kiểm tra xem ID truyền trên thanh địa chỉ URL có khớp với ID ẩn bên trong form không (tránh lỗi ngớ ngẩn)
                return NotFound();
            }

            await ValidateService(service); // Kiểm tra xem tên mới sửa có vô tình bị trùng với dịch vụ nào khác không
            if (!ModelState.IsValid)
            {
                return View(service); // Bị lỗi thì trả về form cũ để sửa lại
            }

            // Lấy thông tin dịch vụ cũ ở trong Database lên trước
            var existing = await _context.Services.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Gán dữ liệu mới đè lên dữ liệu cũ (.Trim() dùng để cắt bỏ các khoảng trắng dư thừa ở 2 đầu chữ)
            existing.Name = service.Name.Trim();
            existing.Unit = service.Unit.Trim();
            existing.Price = service.Price;
            existing.Description = service.Description?.Trim();

            await _context.SaveChangesAsync(); // Cập nhật thay đổi vào Database
            TempData["Success"] = "Đã cập nhật dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            return service == null ? NotFound() : View(service);
        }

        // 4. Hàm POST: Chạy khi người dùng nhấn nút "XÁC NHẬN XÓA"
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Tìm dịch vụ cần xóa
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            // LƯU Ý QUAN TRỌNG: Kiểm tra xem Dịch vụ này đã được dùng ở Hóa Đơn (InvoiceDetails) nào chưa?
            var isInUse = await _context.InvoiceDetails.AnyAsync(d => d.ServiceId == id);
            if (isInUse)
            {
                // Nếu đã được dùng thì cấm xóa! Vì xóa đi sẽ làm hỏng lịch sử tính tiền của Hóa Đơn cũ.
                TempData["Error"] = "Không thể xóa dịch vụ đã được sử dụng trong hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            _context.Services.Remove(service); // Lệnh Chuẩn bị xóa
            await _context.SaveChangesAsync(); // Chạy thật lệnh Xóa xuống Database
            TempData["Success"] = "Đã xóa dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // 5. Hàm dùng chung để KIỂM TRA LỖI dữ liệu đầu vào (cả lúc Create và Edit đều xài chung hàm này)
        private async Task ValidateService(Service service)
        {
            service.Name = service.Name?.Trim() ?? string.Empty; // Xóa khoảng trắng 2 đầu
            service.Unit = service.Unit?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(service.Name))
            {
                ModelState.AddModelError(nameof(Service.Name), "Vui lòng nhập tên dịch vụ.");
            }
            // Logic bắt lỗi trùng tên: Nếu trong Database đã có 'Tên dịch vụ' này VÀ nó mang ID khác với ID đang xét -> Là bị trùng
            else if (await _context.Services.AnyAsync(s => s.Name == service.Name && s.Id != service.Id))
            {
                ModelState.AddModelError(nameof(Service.Name), "Tên dịch vụ đã tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(service.Unit))
            {
                ModelState.AddModelError(nameof(Service.Unit), "Vui lòng nhập đơn vị tính.");
            }

            if (service.Price < 0)
            {
                ModelState.AddModelError(nameof(Service.Price), "Giá dịch vụ không được âm.");
            }
        }
    }
}
