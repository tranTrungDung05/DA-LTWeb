using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TenantsController : Controller
    {
        private readonly AppDbContext _context;

        public TenantsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Tenants.AsNoTracking().OrderBy(t => t.FullName).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await LoadAccounts();
            return View(new Tenant());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tenant tenant)
        {
            await ValidateTenant(tenant);
            if (!ModelState.IsValid)
            {
                await LoadAccounts(tenant.AccountId);
                return View(tenant);
            }

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm cư dân.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            await LoadAccounts(tenant.AccountId, tenant.Id);
            return View(tenant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tenant tenant)
        {
            if (id != tenant.Id)
            {
                return NotFound();
            }

            await ValidateTenant(tenant);
            if (!ModelState.IsValid)
            {
                await LoadAccounts(tenant.AccountId, tenant.Id);
                return View(tenant);
            }

            var existing = await _context.Tenants.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.IdentityNumber = tenant.IdentityNumber;
            existing.FullName = tenant.FullName;
            existing.Phone = tenant.Phone;
            existing.Email = tenant.Email;
            existing.DateOfBirth = tenant.DateOfBirth;
            existing.PermanentAddress = tenant.PermanentAddress;
            existing.Status = tenant.Status;
            existing.AccountId = tenant.AccountId;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật cư dân.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            return tenant == null ? NotFound() : View(tenant);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            var isInUse = await _context.TenantRooms.AnyAsync(x => x.TenantId == id)
                || await _context.Contracts.AnyAsync(x => x.TenantId == id)
                || await _context.Invoices.AnyAsync(x => x.TenantId == id)
                || await _context.Notifications.AnyAsync(x => x.TenantId == id);

            if (isInUse)
            {
                TempData["Error"] = "Không thể xóa cư dân đã có phòng, hợp đồng, hóa đơn hoặc thông báo.";
                return RedirectToAction(nameof(Index));
            }

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa cư dân.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateTenant(Tenant tenant)
        {
            tenant.IdentityNumber = tenant.IdentityNumber.Trim();
            tenant.FullName = tenant.FullName.Trim();

            if (string.IsNullOrWhiteSpace(tenant.IdentityNumber))
            {
                ModelState.AddModelError(nameof(Tenant.IdentityNumber), "Vui lòng nhập CCCD/CMND.");
            }
            else if (await _context.Tenants.AnyAsync(t =>
                         t.IdentityNumber == tenant.IdentityNumber && t.Id != tenant.Id))
            {
                ModelState.AddModelError(nameof(Tenant.IdentityNumber), "CCCD/CMND đã tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(tenant.FullName))
            {
                ModelState.AddModelError(nameof(Tenant.FullName), "Vui lòng nhập họ tên.");
            }

            if (tenant.AccountId.HasValue)
            {
                var accountIsValid = await _context.Users.AnyAsync(a =>
                    a.Id == tenant.AccountId && a.Role == RoleType.User);
                var accountIsUsed = await _context.Tenants.AnyAsync(t =>
                    t.AccountId == tenant.AccountId && t.Id != tenant.Id);

                if (!accountIsValid || accountIsUsed)
                {
                    ModelState.AddModelError(nameof(Tenant.AccountId), "Tài khoản không hợp lệ hoặc đã gắn với cư dân khác.");
                }
            }
        }

        private async Task LoadAccounts(int? accountId = null, int? tenantId = null)
        {
            var usedAccountIds = _context.Tenants
                .Where(t => t.Id != tenantId && t.AccountId.HasValue)
                .Select(t => t.AccountId!.Value);

            var accounts = await _context.Users
                .AsNoTracking()
                .Where(a => a.Role == RoleType.User && !usedAccountIds.Contains(a.Id))
                .OrderBy(a => a.FullName)
                .Select(a => new { a.Id, Name = a.FullName + " (" + a.UserName + ")" })
                .ToListAsync();

            ViewBag.Accounts = new SelectList(accounts, "Id", "Name", accountId);
        }
    }
}
