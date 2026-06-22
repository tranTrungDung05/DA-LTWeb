using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.ViewModels;

namespace smart_hostel_management_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContractsController : Controller
    {
        private readonly AppDbContext _context;
        public ContractsController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.Contracts.AsNoTracking().Include(c => c.Tenant).Include(c => c.ContractDetails).ThenInclude(d => d.Room).OrderByDescending(c => c.Date).ToListAsync());

        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.Tenant)
                .Include(c => c.ContractDetails).ThenInclude(d => d.Room)
                .Include(c => c.ContractServices).ThenInclude(cs => cs.Service)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            // Lấy danh sách thành viên đang ở trong phòng này
            ViewBag.Members = await _context.TenantRooms
                .Include(tr => tr.Tenant)
                .Where(tr => tr.RoomId == contract.ContractDetails.First().RoomId && tr.IsCurrent)
                .ToListAsync();

            // Dữ liệu cho dropdown: Danh sách tất cả cư dân để Admin chọn thêm vào phòng
            var allTenants = await _context.Tenants.AsNoTracking().OrderBy(t => t.FullName).ToListAsync();
            ViewBag.AvailableTenants = new SelectList(allTenants, "Id", "FullName");

            return View(contract);
        }

        public async Task<IActionResult> Create() { await LoadSelections(); return View(new ContractFormViewModel()); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractFormViewModel model, List<int> SelectedServiceIds)
        {
            bool isAutoCode = string.IsNullOrWhiteSpace(model.ContractNumber) || model.ContractNumber.Trim().ToUpper() == "AUTO";
            if (isAutoCode) model.ContractNumber = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 8);
            await ValidateContract(model);
            if (!ModelState.IsValid) { await LoadSelections(model.TenantId, model.RoomId); return View(model); }

            var contract = new Contract {
                ContractNumber = isAutoCode ? $"HD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}" : model.ContractNumber.Trim(),
                Date = model.Date, Status = model.Status, TenantId = model.TenantId, Note = model.Note,
                ContractDetails = { new ContractDetail { RoomId = model.RoomId, StartDate = model.StartDate, EndDate = model.EndDate, Content = model.Content } }
            };
            _context.Contracts.Add(contract);
            _context.TenantRooms.Add(new TenantRoom { TenantId = model.TenantId, RoomId = model.RoomId, IsRepresentative = true, IsCurrent = true });
            await _context.SaveChangesAsync();

            if (SelectedServiceIds != null)
            {
                foreach (var sId in SelectedServiceIds)
                {
                    var s = await _context.Services.FindAsync(sId);
                    if (s != null) _context.ContractServices.Add(new ContractService { ContractId = contract.Id, ServiceId = sId, PriceAtContract = s.Price });
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _context.Contracts.Include(c => c.ContractDetails).Include(c => c.ContractServices).FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null) return NotFound();
            var detail = contract.ContractDetails.FirstOrDefault();
            var model = new ContractFormViewModel {
                Id = contract.Id, ContractNumber = contract.ContractNumber, Date = contract.Date, Status = contract.Status,
                TenantId = contract.TenantId, RoomId = detail?.RoomId ?? 0, StartDate = detail?.StartDate ?? DateTime.Today,
                EndDate = detail?.EndDate ?? DateTime.Today.AddMonths(12), Content = detail?.Content, Note = contract.Note,
                SelectedServiceIds = contract.ContractServices.Select(cs => cs.ServiceId).ToList()
            };
            await LoadSelections(model.TenantId, model.RoomId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContractFormViewModel model, List<int> SelectedServiceIds)
        {
            var contract = await _context.Contracts.Include(c => c.ContractDetails).Include(c => c.ContractServices).FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null) return NotFound();
            contract.ContractNumber = model.ContractNumber.Trim(); contract.Date = model.Date; contract.Status = model.Status; contract.TenantId = model.TenantId; contract.Note = model.Note;
            var detail = contract.ContractDetails.FirstOrDefault() ?? new ContractDetail { ContractId = id };
            detail.RoomId = model.RoomId; detail.StartDate = model.StartDate; detail.EndDate = model.EndDate; detail.Content = model.Content;
            
            _context.ContractServices.RemoveRange(contract.ContractServices);
            if (SelectedServiceIds != null)
            {
                foreach (var sId in SelectedServiceIds)
                {
                    var s = await _context.Services.FindAsync(sId);
                    if (s != null) _context.ContractServices.Add(new ContractService { ContractId = id, ServiceId = sId, PriceAtContract = s.Price });
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id) => View(await _context.Contracts.Include(c => c.Tenant).FirstOrDefaultAsync(c => c.Id == id));

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.ContractDetails)
                .Include(c => c.ContractServices)
                // Bao gồm luôn cả Invoices nếu có
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            // 1. Xóa các bản ghi liên quan trước
            var invoices = _context.Invoices.Where(i => i.ContractId == id);
            _context.Invoices.RemoveRange(invoices);

            var details = _context.ContractDetails.Where(d => d.ContractId == id);
            _context.ContractDetails.RemoveRange(details);

            var services = _context.ContractServices.Where(s => s.ContractId == id);
            _context.ContractServices.RemoveRange(services);

            // 2. Xóa chính hợp đồng
            _context.Contracts.Remove(contract);
            
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa hợp đồng và các dữ liệu liên quan thành công!";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateContract(ContractFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ContractNumber)) ModelState.AddModelError(nameof(model.ContractNumber), "Mã không trống.");
            else if (await _context.Contracts.AnyAsync(c => c.ContractNumber.Trim() == model.ContractNumber.Trim() && c.Id != model.Id)) ModelState.AddModelError(nameof(model.ContractNumber), "Mã đã tồn tại.");
        }

        private async Task LoadSelections(int? tenantId = null, int? roomId = null)
        {
            var tenants = await _context.Tenants.AsNoTracking().OrderBy(t => t.FullName).ToListAsync();
            // Đổi tên thành TenantId và RoomId để khớp hoàn toàn với Property của Model
            ViewBag.TenantId = new SelectList(tenants, "Id", "FullName", tenantId);
            ViewBag.RoomId = new SelectList(await _context.Rooms.AsNoTracking().OrderBy(r => r.RoomNumber).ToListAsync(), "Id", "RoomNumber", roomId);
            
            ViewBag.AllServices = await _context.Services.AsNoTracking().ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExistingTenant(int contractId, int roomId, int tenantId)
        {
            // 1. Kiểm tra xem cư dân đã tồn tại trong phòng này chưa
            var exists = await _context.TenantRooms
                .AnyAsync(tr => tr.TenantId == tenantId && tr.RoomId == roomId && tr.IsCurrent);

            if (exists)
            {
                TempData["Error"] = "Cư dân này đã có trong phòng rồi!";
            }
            else
            {
                // 2. Thêm cư dân mới vào phòng với vai trò là thành viên ở ghép (IsRepresentative = false)
                var newTenantRoom = new TenantRoom
                {
                    TenantId = tenantId,
                    RoomId = roomId,
                    IsRepresentative = false,
                    IsCurrent = true
                };

                _context.TenantRooms.Add(newTenantRoom);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm cư dân vào phòng thành công!";
            }

            // 3. Quay lại trang chi tiết hợp đồng
            return RedirectToAction("Details", new { id = contractId });
        }
    }
}