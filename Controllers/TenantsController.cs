using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Data;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Controllers;

[Authorize(Roles = "Owner,Admin")]
public class TenantsController : Controller
{
    private readonly AppDbContext _context;

    public TenantsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, TenantStatus? status)
    {
        var tenants = _context.Tenants
            .Include(item => item.TenantRooms).ThenInclude(item => item.Room)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            tenants = tenants.Where(item =>
                item.FullName.Contains(search) ||
                item.PhoneNumber.Contains(search) ||
                (item.IdentityNumber != null && item.IdentityNumber.Contains(search)));
        }

        if (status.HasValue)
        {
            tenants = tenants.Where(item => item.Status == status.Value);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(await tenants.OrderBy(item => item.FullName).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var tenant = await _context.Tenants
            .Include(item => item.TenantRooms).ThenInclude(item => item.Room)
            .Include(item => item.Contracts).ThenInclude(item => item.Room)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (tenant is null)
        {
            return NotFound();
        }

        await LoadRoomsAsync();
        return View(tenant);
    }

    public IActionResult Create()
    {
        return View(new Tenant());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Tenant tenant, IFormFile? document)
    {
        if (!ModelState.IsValid)
        {
            return View(tenant);
        }

        tenant.DocumentPath = await SaveUploadAsync(document, "tenants");
        if (!ModelState.IsValid)
        {
            return View(tenant);
        }

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        return tenant is null ? NotFound() : View(tenant);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Tenant tenant, IFormFile? document)
    {
        if (id != tenant.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(tenant);
        }

        var existing = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        tenant.DocumentPath = document is null ? existing?.DocumentPath : await SaveUploadAsync(document, "tenants");
        if (!ModelState.IsValid)
        {
            return View(tenant);
        }

        _context.Update(tenant);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var hasCurrentRoom = await _context.TenantRooms.AnyAsync(item => item.TenantId == id && item.IsCurrent);
        var hasActiveContract = await _context.Contracts.AnyAsync(item => item.TenantId == id && item.Status != ContractStatus.Terminated);
        if (hasCurrentRoom || hasActiveContract)
        {
            TempData["Error"] = "Khong the xoa khach thue dang o phong hoac co hop dong con hieu luc.";
            return RedirectToAction(nameof(Index));
        }

        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant is not null)
        {
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoom(int tenantId, int roomId, DateTime moveInDate)
    {
        var roomInUse = await _context.TenantRooms
            .AnyAsync(item => item.RoomId == roomId && item.IsCurrent && item.TenantId != tenantId);
        if (roomInUse)
        {
            TempData["Error"] = "Phong nay dang co khach thue khac.";
            return RedirectToAction(nameof(Details), new { id = tenantId });
        }

        var currentAssignments = await _context.TenantRooms
            .Where(item => item.TenantId == tenantId && item.IsCurrent)
            .ToListAsync();

        foreach (var assignment in currentAssignments)
        {
            assignment.IsCurrent = false;
            assignment.MoveOutDate = moveInDate.AddDays(-1);
        }

        _context.TenantRooms.Add(new TenantRoom
        {
            TenantId = tenantId,
            RoomId = roomId,
            MoveInDate = moveInDate == default ? DateTime.Today : moveInDate,
            IsCurrent = true
        });

        var room = await _context.Rooms.FindAsync(roomId);
        if (room is not null)
        {
            room.Status = RoomStatus.Occupied;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = tenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LeaveRoom(int tenantRoomId, int tenantId)
    {
        var assignment = await _context.TenantRooms.Include(item => item.Room).FirstOrDefaultAsync(item => item.Id == tenantRoomId);
        if (assignment is not null)
        {
            assignment.IsCurrent = false;
            assignment.MoveOutDate = DateTime.Today;
            if (assignment.Room is not null)
            {
                assignment.Room.Status = RoomStatus.Available;
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = tenantId });
    }

    private async Task LoadRoomsAsync()
    {
        var rooms = await _context.Rooms.OrderBy(item => item.Code).ToListAsync();
        ViewBag.RoomId = new SelectList(rooms, "Id", "Code");
    }

    private async Task<string?> SaveUploadAsync(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(string.Empty, "Chi chap nhan file JPG, PNG hoac PDF.");
            return null;
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File khong duoc vuot qua 5MB.");
            return null;
        }

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
        Directory.CreateDirectory(root);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(root, fileName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return $"/uploads/{folder}/{fileName}";
    }
}
