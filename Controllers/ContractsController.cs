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

        // 1. DANH SÁCH HỢP ĐỒNG (INDEX)
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

        // 2. CHI TIẾT HỢP ĐỒNG (DETAILS) - THỂ HIỆN PHÒNG CÓ NHIỀU NGƯỜI
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.Tenant)
                .Include(c => c.ContractDetails).ThenInclude(d => d.Room)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();

            var roomId = contract.ContractDetails.FirstOrDefault()?.RoomId;

            // Truy vấn lấy toàn bộ thành viên đang ở phòng này từ bảng trung gian TenantRoom
            var membersInRoom = await _context.TenantRooms
                .Include(tr => tr.Tenant)
                .Where(tr => tr.RoomId == roomId && tr.IsCurrent == true)
                .ToListAsync();

            ViewBag.Members = membersInRoom;

            return View(contract);
        }

        // 3. THÊM MỚI HỢP ĐỒNG (GET)
        public async Task<IActionResult> Create()
        {
            await LoadSelections();
            return View(new ContractFormViewModel());
        }

        // 4. THÊM MỚI HỢP ĐỒNG (POST)
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

            // Khởi tạo thực thể Contract
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

            // ĐỒNG BỘ: Thêm ngay người đại diện này vào danh sách phòng (TenantRoom)
            var tenantRoomRelation = new TenantRoom
            {
                TenantId = model.TenantId,
                RoomId = model.RoomId,
                IsRepresentative = true,
                IsCurrent = true
            };
            _context.TenantRooms.Add(tenantRoomRelation);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã thêm hợp đồng {finalContractNumber} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // 5. CHỈNH SỬA HỢP ĐỒNG (GET)
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

        // 6. CHỈNH SỬA HỢP ĐỒNG (POST)
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
            TempData["Success"] = "Đã cập nhật thay đổi hợp đồng.";
            return RedirectToAction(nameof(Index));
        }

        // 7. XÓA HỢP ĐỒNG
        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.Tenant)
                .Include(c => c.ContractDetails).ThenInclude(d => d.Room)
                .FirstOrDefaultAsync(c => c.Id == id);

            return contract == null ? NotFound() : View(contract);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.Include(c => c.ContractDetails).FirstOrDefaultAsync(c => c.Id == id);
            if (contract == null) return NotFound();

            if (await _context.Invoices.AnyAsync(i => i.ContractId == id))
            {
                TempData["Error"] = "Không được xóa hợp đồng đã phát sinh hóa đơn tiền phòng!";
                return RedirectToAction(nameof(Index));
            }

            _context.ContractDetails.RemoveRange(contract.ContractDetails);
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa bỏ dữ liệu hợp đồng.";
            return RedirectToAction(nameof(Index));
        }

        // CÁC HÀM PHỤ TRỢ (VALIDATE & LOAD DROPDOWN)
        private async Task ValidateContract(ContractFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ContractNumber))
            {
                ModelState.AddModelError(nameof(model.ContractNumber), "Mã hợp đồng không được để trống.");
            }
            else if (await _context.Contracts.AnyAsync(c => c.ContractNumber.Trim() == model.ContractNumber.Trim() && c.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.ContractNumber), "Mã hợp đồng này đã tồn tại trên hệ thống.");
            }

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
            }
        }

        private async Task LoadSelections(int? tenantId = null, int? roomId = null)
        {
            ViewBag.Tenants = new SelectList(await _context.Tenants.AsNoTracking().OrderBy(t => t.FullName).ToListAsync(), "Id", "FullName", tenantId);
            ViewBag.Rooms = new SelectList(await _context.Rooms.AsNoTracking().OrderBy(r => r.RoomNumber).ToListAsync(), "Id", "RoomNumber", roomId);
        }
    }
}