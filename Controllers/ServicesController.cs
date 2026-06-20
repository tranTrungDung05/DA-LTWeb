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

        // GET: Services
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(services);
        }

        // GET: Services/Create
        public IActionResult Create()
        {
            return View(new Service());
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            await ValidateService(service);
            if (!ModelState.IsValid)
            {
                return View(service);
            }

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Services/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            await ValidateService(service);
            if (!ModelState.IsValid)
            {
                return View(service);
            }

            var existing = await _context.Services.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = service.Name.Trim();
            existing.Unit = service.Unit.Trim();
            existing.Price = service.Price;
            existing.Description = service.Description?.Trim();

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Services/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            return service == null ? NotFound() : View(service);
        }

        // POST: Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            var isInUse = await _context.InvoiceDetails.AnyAsync(d => d.ServiceId == id);
            if (isInUse)
            {
                TempData["Error"] = "Không thể xóa dịch vụ đã được sử dụng trong hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateService(Service service)
        {
            service.Name = service.Name?.Trim() ?? string.Empty;
            service.Unit = service.Unit?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(service.Name))
            {
                ModelState.AddModelError(nameof(Service.Name), "Vui lòng nhập tên dịch vụ.");
            }
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
