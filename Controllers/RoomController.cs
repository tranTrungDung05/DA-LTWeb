using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Controllers
{
    [Authorize(Roles = "Admin")] // 🌟 Chặn quyền, chỉ cho Admin vào quản lý phòng
    public class RoomController : Controller
    {
        private readonly AppDbContext _context;

        public RoomController(AppDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH PHÒNG (BẤT ĐỒNG BỘ + TỐI ƯU TRUY VẤN)
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .AsNoTracking() // Tối ưu tốc độ đọc dữ liệu
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            return View(rooms);
        }

        // 2. CHI TIẾT PHÒNG + THÀNH VIÊN
        public async Task<IActionResult> Details(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.TenantRooms)
                    .ThenInclude(tr => tr.Tenant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            // Lấy danh sách cư dân chưa ở bất kỳ phòng nào để nạp vào dropdown
            var tenantsInRooms = await _context.TenantRooms
                .Select(tr => tr.TenantId)
                .ToListAsync();

            var availableTenants = await _context.Tenants
                .AsNoTracking()
                .Where(t => !tenantsInRooms.Contains(t.Id))
                .OrderBy(t => t.FullName)
                .ToListAsync();

            ViewBag.AvailableTenants = new SelectList(availableTenants, "Id", "FullName");

            return View(room);
        }

        // 3. TẠO MỚI PHÒNG (GET)
        public IActionResult Create()
        {
            return View(new Room());
        }

        // TẠO MỚI PHÒNG (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            // Kiểm tra trùng số phòng
            if (!string.IsNullOrWhiteSpace(room.RoomNumber) && 
                await _context.Rooms.AnyAsync(r => r.RoomNumber.Trim() == room.RoomNumber.Trim()))
            {
                ModelState.AddModelError(nameof(room.RoomNumber), "Số phòng này đã tồn tại trong hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                return View(room);
            }

            room.RoomNumber = room.RoomNumber.Trim();
            room.Status = RoomStatus.Available; // Mặc định khi tạo mới là phòng trống

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm phòng trọ mới thành công.";
            return RedirectToAction(nameof(Index));
        }

        // 4. CHỈNH SỬA THÔNG TIN PHÒNG (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        // CHỈNH SỬA THÔNG TIN PHÒNG (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Room room)
        {
            if (id != room.Id)
            {
                return NotFound();
            }

            // Kiểm tra trùng số phòng với các phòng khác (ngoại trừ chính nó)
            if (!string.IsNullOrWhiteSpace(room.RoomNumber) && 
                await _context.Rooms.AnyAsync(r => r.RoomNumber.Trim() == room.RoomNumber.Trim() && r.Id != room.Id))
            {
                ModelState.AddModelError(nameof(room.RoomNumber), "Số phòng này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(room);
            }

            _context.Update(room);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // 5. XÓA PHÒNG (POST - Theo form đơn giản sinh viên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.TenantRooms)
                .Include(r => r.ContractDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            // Ràng buộc an toàn dữ liệu: Nếu đang có người ở hoặc đã làm hợp đồng thì không cho xóa bừa bãi
            if (room.TenantRooms.Any() || room.ContractDetails.Any())
            {
                TempData["Error"] = "Không thể xóa phòng này vì đang có khách thuê ở hoặc dính líu đến dữ liệu hợp đồng.";
                return RedirectToAction(nameof(Index));
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa phòng trọ ra khỏi hệ thống.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // ⚡ NGHIỆP VỤ BẢNG TRUNG GIAN (TENANT_ROOM)
        // ==========================================

        // THÊM THÀNH VIÊN VÀO PHÒNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTenant(int roomId, int tenantId)
        {
            if (tenantId == 0) return RedirectToAction(nameof(Details), new { id = roomId });

            var exists = await _context.TenantRooms.AnyAsync(tr => tr.RoomId == roomId && tr.TenantId == tenantId);
            if (!exists)
            {
                var tenantRoom = new TenantRoom
                {
                    RoomId = roomId,
                    TenantId = tenantId
                };

                _context.TenantRooms.Add(tenantRoom);

                // Nếu trạng thái phòng đang rảnh (Available), tự động đổi sang Đang ở (Occupied)
                var room = await _context.Rooms.FindAsync(roomId);
                if (room != null && room.Status == RoomStatus.Available)
                {
                    room.Status = RoomStatus.Occupied;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm thành viên vào phòng thành công.";
            }

            return RedirectToAction(nameof(Details), new { id = roomId });
        }

        // MỜI THÀNH VIÊN RA KHỎI PHÒNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTenant(int roomId, int tenantId)
        {
            var tenantRoom = await _context.TenantRooms
                .FirstOrDefaultAsync(tr => tr.RoomId == roomId && tr.TenantId == tenantId);

            if (tenantRoom != null)
            {
                _context.TenantRooms.Remove(tenantRoom);
                await _context.SaveChangesAsync();

                // Kiểm tra lại xem phòng còn ai ở không, nếu không còn ai thì đưa về trạng thái Available
                var anyLeft = await _context.TenantRooms.AnyAsync(tr => tr.RoomId == roomId);
                if (!anyLeft)
                {
                    var room = await _context.Rooms.FindAsync(roomId);
                    if (room != null)
                    {
                        room.Status = RoomStatus.Available;
                        await _context.SaveChangesAsync();
                    }
                }
                TempData["Success"] = "Đã xóa thành viên khỏi phòng.";
            }

            return RedirectToAction(nameof(Details), new { id = roomId });
        }
    }
}