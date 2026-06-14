using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Data;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Controllers;

[Authorize(Roles = "Owner,Admin")]
public class PropertiesController : Controller
{
    private readonly AppDbContext _context;

    public PropertiesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var properties = await _context.Properties
            .Include(item => item.Rooms)
            .OrderBy(item => item.Name)
            .ToListAsync();

        return View(properties);
    }

    public IActionResult Create()
    {
        return View(new Property());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Property property)
    {
        if (!ModelState.IsValid)
        {
            return View(property);
        }

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var property = await _context.Properties.FindAsync(id);
        return property is null ? NotFound() : View(property);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Property property)
    {
        if (id != property.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(property);
        }

        _context.Properties.Update(property);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var hasRooms = await _context.Rooms.AnyAsync(item => item.PropertyId == id);
        if (hasRooms)
        {
            TempData["Error"] = "Khong the xoa nha tro dang co phong.";
            return RedirectToAction(nameof(Index));
        }

        var property = await _context.Properties.FindAsync(id);
        if (property is not null)
        {
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
