using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.ViewModels;
using smart_hostel_management_system.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace smart_hostel_management_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContractsController : Controller
    {
        private readonly AppDbContext _context;

        public ContractsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Contracts
        public async Task<IActionResult> Index()
        {
            var contracts = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.Tenant)
                .Include(c => c.ContractDetails)
                .ThenInclude(d => d.Room)
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            return View(contracts);
        }

        // GET: Contracts/Details/5
        [HttpGet]
        [Route("Contracts/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0) return BadRequest();

            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.Tenant)
                .Include(c => c.ContractDetails).ThenInclude(d => d.Room)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            var roomId = contract.ContractDetails.FirstOrDefault()?.RoomId;

            // Truy vấn danh sách thành viên hiện tại của phòng từ bảng trung gian TenantRoom
            var membersInRoom = await _context.TenantRooms
                .Include(tr => tr.Tenant)
                .Where(tr => tr.RoomId == roomId && tr.IsCurrent == true)
                .ToListAsync();

            ViewBag.Members = membersInRoom;

            // Truy vấn danh sách cư dân chưa được xếp phòng trên hệ thống
            var assignedTenantIds = await _context.TenantRooms
                .Where(tr => tr.IsCurrent == true)
                .Select(tr => tr.TenantId)
                .ToListAsync();

            var availableTenants = await _context.Tenants
                .AsNoTracking()
                .Where(t => !assignedTenantIds.Contains(t.Id))
                .OrderBy(t => t.FullName)
                .ToListAsync();

            ViewBag.AvailableTenants = new SelectList(availableTenants, "Id", "FullName");

            return View(contract);
        }

        // POST: Contracts/AddExistingTenant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExistingTenant(int contractId, int roomId, int tenantId)
        {
            if (tenantId <= 0 || roomId <= 0)
            {
                TempData["Error"] = "Thông tin cư dân hoặc phòng không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            // Kiểm tra trùng lặp bản ghi lưu trú hiện tại
            bool isAlreadyInRoom = await _context.TenantRooms
                .AnyAsync(tr => tr.RoomId == roomId && tr.TenantId == tenantId && tr.IsCurrent == true);

            if (isAlreadyInRoom)
            {
                TempData["Error"] = "Cư dân này đã tồn tại trong phòng.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }

            // Tạo mới bản ghi lưu trú cho thành viên ở ghép
            var newGuest = new TenantRoom
            {
                TenantId = tenantId,
                RoomId = roomId,
                IsRepresentative = false,
                IsCurrent = true
            };

            _context.TenantRooms.Add(newGuest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm cư dân vào phòng thành công.";
            return RedirectToAction(nameof(Details), new { id = contractId });
        }

        // GET: Contracts/Create
        public async Task<IActionResult> Create()
        {
            await LoadSelections();
            return View(new ContractFormViewModel());
        }

        // POST: Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractFormViewModel model)
        {
            bool isAutoCode = string.IsNullOrWhiteSpace(model.ContractNumber) || model.ContractNumber.Trim().ToUpper() == "AUTO";
            
            if (isAutoCode)
            {
                model.ContractNumber = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 8);
            }

            await ValidateContract(model);
            
            if (!ModelState.IsValid)
            {
                if (isAutoCode) model.ContractNumber = "AUTO";
                await LoadSelections(model.TenantId, model.RoomId);
                return View(model);
            }

            string finalContractNumber = isAutoCode 
                ? $"HD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}" 
                : model.ContractNumber.Trim();

            var contract = new Contract
            {
                ContractNumber = finalContractNumber,
                Date = model.Date,
                Status = model.Status,
                TenantId = model.TenantId,
                Note = model.Note,
                ContractDetails = {
                    new ContractDetail {
                        RoomId = model.RoomId,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        Content = model.Content
                    }
                }
            };
            _context.Contracts.Add(contract);

            // Tự động thêm người đại diện hợp đồng vào bảng trung gian TenantRoom
            var tenantRoomRelation = new TenantRoom
            {
                TenantId = model.TenantId,
                RoomId = model.RoomId,
                IsRepresentative = true,
                IsCurrent = true
            };
            _context.TenantRooms.Add(tenantRoomRelation);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Thêm hợp đồng {finalContractNumber} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Contracts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.ContractDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            var detail = contract.ContractDetails.FirstOrDefault();
            var model = new ContractFormViewModel
            {
                Id = contract.Id,
                ContractNumber = contract.ContractNumber,
                Date = contract.Date,
                Status = contract.Status,
                TenantId = contract.TenantId,
                RoomId = detail?.RoomId ?? 0,
                StartDate = detail?.StartDate ?? DateTime.Today,
                EndDate = detail?.EndDate ?? DateTime.Today.AddMonths(12),
                Content = detail?.Content,
                Note = contract.Note
            };

            await LoadSelections(model.TenantId, model.RoomId);
            return View(model);
        }

        // POST: Contracts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContractFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            await ValidateContract(model);
            if (!ModelState.IsValid)
            {
                await LoadSelections(model.TenantId, model.RoomId);
                return View(model);
            }

            var contract = await _context.Contracts.Include(c => c.ContractDetails).FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null) return NotFound();

            contract.ContractNumber = model.ContractNumber.Trim();
            contract.Date = model.Date;
            contract.Status = model.Status;
            contract.TenantId = model.TenantId;
            contract.Note = model.Note;

            var detail = contract.ContractDetails.FirstOrDefault();
            if (detail == null)
            {
                detail = new ContractDetail { ContractId = contract.Id };
                contract.ContractDetails.Add(detail);
            }

            detail.RoomId = model.RoomId;
            detail.StartDate = model.StartDate;
            detail.EndDate = model.EndDate;
            detail.Content = model.Content;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thông tin hợp đồng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Contracts/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.Tenant)
                .Include(c => c.ContractDetails).ThenInclude(d => d.Room)
                .FirstOrDefaultAsync(c => c.Id == id);

            return contract == null ? NotFound() : View(contract);
        }

        // POST: Contracts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.Include(c => c.ContractDetails).FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null) return NotFound();

            if (await _context.Invoices.AnyAsync(i => i.ContractId == id))
            {
                TempData["Error"] = "Không thể xóa hợp đồng đã phát sinh hóa đơn tiền phòng.";
                return RedirectToAction(nameof(Index));
            }

            _context.ContractDetails.RemoveRange(contract.ContractDetails);
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa hợp đồng thành công.";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra logic ràng buộc của hợp đồng
        private async Task ValidateContract(ContractFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ContractNumber))
            {
                ModelState.AddModelError(nameof(model.ContractNumber), "Mã hợp đồng không được để trống.");
            }
            else if (await _context.Contracts.AnyAsync(c => c.ContractNumber.Trim() == model.ContractNumber.Trim() && c.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.ContractNumber), "Mã hợp đồng đã tồn tại trên hệ thống.");
            }

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
            }
        }

        // Nạp dữ liệu cấu hình cho các danh sách lựa chọn (Dropdown)
        private async Task LoadSelections(int? tenantId = null, int? roomId = null)
        {
            ViewBag.Tenants = new SelectList(await _context.Tenants.AsNoTracking().OrderBy(t => t.FullName).ToListAsync(), "Id", "FullName", tenantId);
            ViewBag.Rooms = new SelectList(await _context.Rooms.AsNoTracking().OrderBy(r => r.RoomNumber).ToListAsync(), "Id", "RoomNumber", roomId);
        }
    }
}