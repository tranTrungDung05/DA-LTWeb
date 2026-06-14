using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Data;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Controllers;

[Authorize(Roles = "Owner,Admin")]
public class RoomsController : Controller
{
    private readonly AppDbContext _context;

    public RoomsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, RoomStatus? status)
    {
        var rooms = _context.Rooms.Include(room => room.Property).Include(room => room.Assets).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            rooms = rooms.Where(room => room.Code.Contains(search) || (room.Note != null && room.Note.Contains(search)));
        }

        if (status.HasValue)
        {
            rooms = rooms.Where(room => room.Status == status.Value);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(await rooms.OrderBy(room => room.Code).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var room = await _context.Rooms
            .Include(item => item.Property)
            .Include(item => item.Assets)
            .Include(item => item.TenantRooms).ThenInclude(item => item.Tenant)
            .Include(item => item.Contracts).ThenInclude(item => item.Tenant)
            .FirstOrDefaultAsync(item => item.Id == id);

        return room is null ? NotFound() : View(room);
    }

    public async Task<IActionResult> Create()
    {
        await LoadPropertiesAsync();
        return View(new Room { PropertyId = 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Room room)
    {
        if (!ModelState.IsValid)
        {
            await LoadPropertiesAsync();
            return View(room);
        }

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null)
        {
            return NotFound();
        }

        await LoadPropertiesAsync();
        return View(room);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Room room)
    {
        if (id != room.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadPropertiesAsync();
            return View(room);
        }

        _context.Update(room);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var hasActiveTenant = await _context.TenantRooms.AnyAsync(item => item.RoomId == id && item.IsCurrent);
        var hasActiveContract = await _context.Contracts.AnyAsync(item => item.RoomId == id && item.Status != ContractStatus.Terminated);
        if (hasActiveTenant || hasActiveContract)
        {
            TempData["Error"] = "Khong the xoa phong dang co khach thue hoac hop dong con hieu luc.";
            return RedirectToAction(nameof(Index));
        }

        var room = await _context.Rooms.Include(item => item.Assets).FirstOrDefaultAsync(item => item.Id == id);
        if (room is not null)
        {
            _context.RoomAssets.RemoveRange(room.Assets);
            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAsset(int roomId, string name, int quantity, string? condition)
    {
        if (!string.IsNullOrWhiteSpace(name) && quantity > 0)
        {
            _context.RoomAssets.Add(new RoomAsset
            {
                RoomId = roomId,
                Name = name.Trim(),
                Quantity = quantity,
                Condition = condition
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = roomId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAsset(int id, int roomId)
    {
        var asset = await _context.RoomAssets.FindAsync(id);
        if (asset is not null)
        {
            _context.RoomAssets.Remove(asset);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = roomId });
    }

    private async Task LoadPropertiesAsync()
    {
        ViewBag.PropertyId = new SelectList(await _context.Properties.OrderBy(item => item.Name).ToListAsync(), "Id", "Name");
    }
}
