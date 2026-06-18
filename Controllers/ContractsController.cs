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

        public ContractsController(AppDbContext context)
        {
            _context = context;
        }

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

        public async Task<IActionResult> Create()
        {
            await LoadSelections();
            return View(new ContractFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractFormViewModel model)
        {
            await ValidateContract(model);
            if (!ModelState.IsValid)
            {
                await LoadSelections(model.TenantId, model.RoomId);
                return View(model);
            }

            var contract = new Contract
            {
                ContractNumber = model.ContractNumber.Trim(),
                Date = model.Date,
                Status = model.Status,
                TenantId = model.TenantId,
                Note = model.Note,
                ContractDetails =
                {
                    new ContractDetail
                    {
                        RoomId = model.RoomId,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        Content = model.Content
                    }
                }
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm hợp đồng.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.ContractDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return NotFound();
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContractFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            await ValidateContract(model);
            if (!ModelState.IsValid)
            {
                await LoadSelections(model.TenantId, model.RoomId);
                return View(model);
            }

            var contract = await _context.Contracts
                .Include(c => c.ContractDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return NotFound();
            }

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
            TempData["Success"] = "Đã cập nhật hợp đồng.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .Include(c => c.Tenant)
                .Include(c => c.ContractDetails)
                .ThenInclude(d => d.Room)
                .FirstOrDefaultAsync(c => c.Id == id);

            return contract == null ? NotFound() : View(contract);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.ContractDetails)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return NotFound();
            }

            if (await _context.Invoices.AnyAsync(i => i.ContractId == id))
            {
                TempData["Error"] = "Không thể xóa hợp đồng đã có hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            _context.ContractDetails.RemoveRange(contract.ContractDetails);
            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa hợp đồng.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateContract(ContractFormViewModel model)
        {
            model.ContractNumber = model.ContractNumber.Trim();

            if (string.IsNullOrWhiteSpace(model.ContractNumber))
            {
                ModelState.AddModelError(nameof(model.ContractNumber), "Vui lòng nhập mã hợp đồng.");
            }
            else if (await _context.Contracts.AnyAsync(c =>
                         c.ContractNumber == model.ContractNumber && c.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.ContractNumber), "Mã hợp đồng đã tồn tại.");
            }

            if (!await _context.Tenants.AnyAsync(t => t.Id == model.TenantId))
            {
                ModelState.AddModelError(nameof(model.TenantId), "Vui lòng chọn cư dân.");
            }

            if (!await _context.Rooms.AnyAsync(r => r.Id == model.RoomId))
            {
                ModelState.AddModelError(nameof(model.RoomId), "Vui lòng chọn phòng.");
            }

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải từ ngày bắt đầu trở đi.");
            }
        }

        private async Task LoadSelections(int? tenantId = null, int? roomId = null)
        {
            ViewBag.Tenants = new SelectList(
                await _context.Tenants.AsNoTracking().OrderBy(t => t.FullName).ToListAsync(),
                "Id", "FullName", tenantId);
            ViewBag.Rooms = new SelectList(
                await _context.Rooms.AsNoTracking().OrderBy(r => r.RoomNumber).ToListAsync(),
                "Id", "RoomNumber", roomId);
        }
    }
}
