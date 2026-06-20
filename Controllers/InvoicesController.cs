using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.ViewModels;
using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Account> _userManager;

        public InvoicesController(AppDbContext context, UserManager<Account> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

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

        // TÍNH NĂNG DEMO: Sinh hóa đơn cho cả tháng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthlyInvoices(int month, int year)
        {
            // Xóa nháp cũ của tháng đó để tránh trùng lặp khi test
            var existingDrafts = await _context.Invoices
                .Where(i => i.Month == month && i.Year == year && !i.IsPublished)
                .ToListAsync();

            if (existingDrafts.Any())
            {
                _context.Invoices.RemoveRange(existingDrafts);
            }

            var contracts = await _context.Contracts
                .Include(c => c.Tenant)
                .Include(c => c.ContractDetails)
                .ThenInclude(d => d.Room)
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            foreach (var contract in contracts)
            {
                var invoice = new Invoice
                {
                    ContractId = contract.Id,
                    TenantId = contract.TenantId,
                    RoomId = contract.ContractDetails.FirstOrDefault()?.RoomId ?? 0,
                    Month = month,
                    Year = year,
                    InvoiceNumber = $"HD-{contract.Id}-{month}-{year}",
                    Status = InvoiceStatus.Unpaid,
                    DueDate = DateTime.Now.AddDays(7),
                    IsPublished = false
                };
                _context.Invoices.Add(invoice);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo mới hóa đơn nháp tháng {month}/{year}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound();

            invoice.Status = InvoiceStatus.Paid;
            _context.Payments.Add(new Payment {
                InvoiceId = invoice.Id,
                Amount = invoice.TotalAmount,
                DateTimePaidAt = DateTime.Now,
                Note = "Admin xác nhận thủ công"
            });
            
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xác nhận thanh toán!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (await _context.Invoices.AnyAsync(i => i.Id == id && i.IsPublished))
            {
                TempData["Error"] = "Hóa đơn đã phát hành, không thể sửa.";
                return RedirectToAction(nameof(Index));
            }
            var model = await BuildEditModel(id);
            return model == null ? NotFound() : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InvoiceEditViewModel model)
        {
            var invoice = await _context.Invoices.Include(i => i.InvoiceDetails).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null || invoice.IsPublished) return NotFound();

            _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
            invoice.MainContent = model.MainContent;
            invoice.DueDate = model.DueDate;
            invoice.TotalAmount = 0;

            var services = await _context.Services.ToDictionaryAsync(s => s.Id);
            foreach (var s in model.Services.Where(s => s.Quantity > 0))
            {
                var subTotal = s.Quantity * s.UnitPrice;
                invoice.InvoiceDetails.Add(new InvoiceDetail { ServiceId = s.ServiceId, Unit = services[s.ServiceId].Unit, Quantity = s.Quantity, UnitPrice = s.UnitPrice, SubTotal = subTotal });
                invoice.TotalAmount += subTotal;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null || invoice.IsPublished) return NotFound();

            invoice.IsPublished = true;
            invoice.PublishedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<InvoiceEditViewModel?> BuildEditModel(int id)
        {
            var invoice = await _context.Invoices.Include(i => i.InvoiceDetails).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return null;
            var services = await _context.Services.ToListAsync();
            return new InvoiceEditViewModel {
                Id = invoice.Id,
                Services = services.Select(s => {
                    var d = invoice.InvoiceDetails.FirstOrDefault(x => x.ServiceId == s.Id);
                    return new InvoiceServiceViewModel { ServiceId = s.Id, ServiceName = s.Name, Unit = s.Unit, Quantity = d?.Quantity ?? 0, UnitPrice = d?.UnitPrice ?? s.Price };
                }).ToList()
            };
        }
    }
}