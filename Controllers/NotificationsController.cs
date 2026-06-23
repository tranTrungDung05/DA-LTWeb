using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var notifications = await _context.Notifications
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
            return View(notifications);
        }

        // GET: Notifications/Create
        public IActionResult Create()
        {
            return View(new Notification());
        }

        // POST: Notifications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Notification notification)
        {
            if (string.IsNullOrWhiteSpace(notification.Title))
            {
                ModelState.AddModelError(nameof(Notification.Title), "Vui lòng nhập tiêu đề.");
            }
            if (string.IsNullOrWhiteSpace(notification.Content))
            {
                ModelState.AddModelError(nameof(Notification.Content), "Vui lòng nhập nội dung.");
            }

            if (ModelState.IsValid)
            {
                notification.CreatedDate = DateTime.Now;
                notification.TargetType = "All"; // Mặc định gửi cho tất cả
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã gửi thông báo thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(notification);
        }

        // GET: Notifications/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);
            return notification == null ? NotFound() : View(notification);
        }

        // POST: Notifications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa thông báo thành công.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
