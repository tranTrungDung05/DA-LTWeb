using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.ViewModels;

namespace smart_hostel_management_system.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Account> _userManager;

        public InvoicesController(AppDbContext context, UserManager<Account> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Tenant)
                .Include(i => i.Room)
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Month)
                .ToListAsync();

            return View(invoices);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            if (await _context.Invoices.AnyAsync(i => i.Id == id && i.IsPublished))
            {
                TempData["Error"] = "Hóa đơn đã phát hành nên không thể sửa.";
                return RedirectToAction(nameof(Index));
            }

            var model = await BuildEditModel(id);
            return model == null ? NotFound() : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, InvoiceEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.IsPublished)
            {
                TempData["Error"] = "Hóa đơn đã phát hành nên không thể sửa.";
                return RedirectToAction(nameof(Index));
            }

            if (model.DueDate == default)
            {
                ModelState.AddModelError(nameof(model.DueDate), "Vui lòng nhập hạn thanh toán.");
            }

            if (model.Services.Any(s => s.Quantity < 0 || s.UnitPrice < 0))
            {
                ModelState.AddModelError("", "Số lượng và đơn giá dịch vụ không được âm.");
            }

            var services = await _context.Services
                .AsNoTracking()
                .ToDictionaryAsync(s => s.Id);
            if (model.Services.Any(s => !services.ContainsKey(s.ServiceId))
                || model.Services.GroupBy(s => s.ServiceId).Any(group => group.Count() > 1))
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var rebuilt = await BuildEditModel(id);
                if (rebuilt == null)
                {
                    return NotFound();
                }

                rebuilt.MainContent = model.MainContent;
                rebuilt.DueDate = model.DueDate;
                foreach (var service in rebuilt.Services)
                {
                    var submitted = model.Services.FirstOrDefault(s => s.ServiceId == service.ServiceId);
                    if (submitted != null)
                    {
                        service.Quantity = submitted.Quantity;
                        service.UnitPrice = submitted.UnitPrice;
                    }
                }

                return View(rebuilt);
            }

            _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
            invoice.MainContent = model.MainContent;
            invoice.DueDate = model.DueDate;
            invoice.TotalAmount = 0;

            foreach (var service in model.Services.Where(s => s.Quantity > 0))
            {
                var subTotal = service.Quantity * service.UnitPrice;
                invoice.InvoiceDetails.Add(new InvoiceDetail
                {
                    ServiceId = service.ServiceId,
                    Unit = services[service.ServiceId].Unit,
                    Quantity = service.Quantity,
                    UnitPrice = service.UnitPrice,
                    SubTotal = subTotal
                });
                invoice.TotalAmount += subTotal;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật tiền dịch vụ của hóa đơn.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Publish(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Tenant)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.IsPublished)
            {
                TempData["Error"] = "Hóa đơn này đã được phát hành.";
                return RedirectToAction(nameof(Index));
            }

            if (!invoice.InvoiceDetails.Any() && invoice.TotalAmount <= 0)
            {
                TempData["Error"] = "Vui lòng nhập tiền dịch vụ trước khi phát hành hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            invoice.IsPublished = true;
            invoice.PublishedAt = DateTime.Now;
            _context.Notifications.Add(new Notification
            {
                TenantId = invoice.TenantId,
                RoomId = invoice.RoomId,
                TargetType = "Tenant",
                Title = $"Hóa đơn {invoice.InvoiceNumber}",
                Content = $"Hóa đơn tháng {invoice.Month}/{invoice.Year} đã được phát hành. Tổng tiền: {invoice.TotalAmount:N0} đ.",
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã phát hành hóa đơn cho {invoice.Tenant?.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MyInvoices()
        {
            var tenantId = await GetCurrentTenantId();
            if (tenantId == null)
            {
                return View(new List<Invoice>());
            }

            var invoices = await _context.Invoices
                .AsNoTracking()
                .Where(i => i.TenantId == tenantId && i.IsPublished)
                .Include(i => i.Room)
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Month)
                .ToListAsync();

            return View(invoices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Tenant)
                .Include(i => i.Room)
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var tenantId = await GetCurrentTenantId();
                if (!invoice.IsPublished || tenantId != invoice.TenantId)
                {
                    return Forbid();
                }
            }

            return View(invoice);
        }

        private async Task<int?> GetCurrentTenantId()
        {
            var account = await _userManager.GetUserAsync(User);
            if (account == null)
            {
                return null;
            }

            return await _context.Tenants
                .Where(t => t.AccountId == account.Id)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<InvoiceEditViewModel?> BuildEditModel(int id)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Tenant)
                .Include(i => i.Room)
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return null;
            }

            var services = await _context.Services.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
            return new InvoiceEditViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                TenantName = invoice.Tenant?.FullName ?? string.Empty,
                RoomNumber = invoice.Room?.RoomNumber ?? string.Empty,
                MainContent = invoice.MainContent,
                DueDate = invoice.DueDate,
                Services = services.Select(service =>
                {
                    var detail = invoice.InvoiceDetails.FirstOrDefault(d => d.ServiceId == service.Id);
                    return new InvoiceServiceViewModel
                    {
                        ServiceId = service.Id,
                        ServiceName = service.Name,
                        Unit = service.Unit,
                        Quantity = detail?.Quantity ?? 0,
                        UnitPrice = detail?.UnitPrice ?? service.Price
                    };
                }).ToList()
            };
        }
    }
}
