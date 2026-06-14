using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Data;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Controllers;

[Authorize(Roles = "Owner,Admin")]
public class ContractsController : Controller
{
    private readonly AppDbContext _context;

    public ContractsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, ContractStatus? status)
    {
        await RefreshExpiringContractsAsync();

        var contracts = _context.Contracts
            .Include(item => item.Room)
            .Include(item => item.Tenant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            contracts = contracts.Where(item =>
                item.ContractNumber.Contains(search) ||
                item.Room!.Code.Contains(search) ||
                item.Tenant!.FullName.Contains(search));
        }

        if (status.HasValue)
        {
            contracts = contracts.Where(item => item.Status == status.Value);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(await contracts.OrderByDescending(item => item.StartDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var contract = await _context.Contracts
            .Include(item => item.Room)
            .Include(item => item.Tenant)
            .Include(item => item.Files)
            .FirstOrDefaultAsync(item => item.Id == id);

        return contract is null ? NotFound() : View(contract);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View(new Contract { ContractNumber = $"HD-{DateTime.Now:yyyyMMddHHmm}" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Contract contract, IFormFile? file)
    {
        await ValidateContractAsync(contract);
        ValidateContractFile(file);
        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(contract);
        }

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        await SaveContractFileAsync(contract.Id, file);

        var room = await _context.Rooms.FindAsync(contract.RoomId);
        if (room is not null)
        {
            room.Status = RoomStatus.Occupied;
        }

        await AssignTenantToRoomFromContractAsync(contract);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract is null)
        {
            return NotFound();
        }

        await LoadListsAsync();
        return View(contract);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Contract contract, IFormFile? file)
    {
        if (id != contract.Id)
        {
            return BadRequest();
        }

        await ValidateContractAsync(contract, id);
        ValidateContractFile(file);
        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(contract);
        }

        _context.Update(contract);
        await SaveContractFileAsync(contract.Id, file);

        if (contract.Status != ContractStatus.Terminated)
        {
            await AssignTenantToRoomFromContractAsync(contract);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(int id, DateTime endDate)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract is not null && endDate > contract.EndDate)
        {
            contract.EndDate = endDate;
            contract.Status = ContractStatus.Extended;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Terminate(int id)
    {
        var contract = await _context.Contracts.Include(item => item.Room).FirstOrDefaultAsync(item => item.Id == id);
        if (contract is not null)
        {
            contract.Status = ContractStatus.Terminated;
            contract.EndDate = DateTime.Today;
            var tenantRooms = await _context.TenantRooms
                .Where(item => item.TenantId == contract.TenantId && item.RoomId == contract.RoomId && item.IsCurrent)
                .ToListAsync();

            foreach (var tenantRoom in tenantRooms)
            {
                tenantRoom.IsCurrent = false;
                tenantRoom.MoveOutDate = DateTime.Today;
            }

            if (contract.Room is not null)
            {
                contract.Room.Status = RoomStatus.Available;
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract is not null)
        {
            if (contract.Status != ContractStatus.Terminated)
            {
                TempData["Error"] = "Chi nen xoa hop dong da thanh ly. Hay thanh ly hop dong truoc.";
                return RedirectToAction(nameof(Index));
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(int id, int contractId)
    {
        var file = await _context.ContractFiles.FindAsync(id);
        if (file is not null)
        {
            _context.ContractFiles.Remove(file);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = contractId });
    }

    private async Task LoadListsAsync()
    {
        ViewBag.RoomId = new SelectList(await _context.Rooms.OrderBy(item => item.Code).ToListAsync(), "Id", "Code");
        ViewBag.TenantId = new SelectList(await _context.Tenants.OrderBy(item => item.FullName).ToListAsync(), "Id", "FullName");
    }

    private async Task SaveContractFileAsync(int contractId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts");
        Directory.CreateDirectory(root);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(root, fileName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        _context.ContractFiles.Add(new ContractFile
        {
            ContractId = contractId,
            FileName = file.FileName,
            FilePath = $"/uploads/contracts/{fileName}"
        });
    }

    private void ValidateContractFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        var allowedExtensions = new[] { ".doc", ".docx", ".pdf", ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(string.Empty, "File hop dong chi chap nhan DOC, DOCX, PDF, JPG hoac PNG.");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            ModelState.AddModelError(string.Empty, "File hop dong khong duoc vuot qua 10MB.");
        }
    }

    private async Task ValidateContractAsync(Contract contract, int? existingContractId = null)
    {
        if (contract.EndDate <= contract.StartDate)
        {
            ModelState.AddModelError(nameof(contract.EndDate), "Ngay ket thuc phai sau ngay bat dau.");
        }

        var roomExists = await _context.Rooms.AnyAsync(item => item.Id == contract.RoomId);
        if (!roomExists)
        {
            ModelState.AddModelError(nameof(contract.RoomId), "Phong khong ton tai.");
        }

        var tenantExists = await _context.Tenants.AnyAsync(item => item.Id == contract.TenantId);
        if (!tenantExists)
        {
            ModelState.AddModelError(nameof(contract.TenantId), "Khach thue khong ton tai.");
        }

        var activeContractExists = await _context.Contracts.AnyAsync(item =>
            item.RoomId == contract.RoomId &&
            item.Status != ContractStatus.Terminated &&
            (!existingContractId.HasValue || item.Id != existingContractId.Value));

        if (activeContractExists && contract.Status != ContractStatus.Terminated)
        {
            ModelState.AddModelError(nameof(contract.RoomId), "Phong nay da co hop dong con hieu luc.");
        }
    }

    private async Task AssignTenantToRoomFromContractAsync(Contract contract)
    {
        var currentAssignments = await _context.TenantRooms
            .Where(item => item.TenantId == contract.TenantId && item.IsCurrent)
            .ToListAsync();

        foreach (var assignment in currentAssignments.Where(item => item.RoomId != contract.RoomId))
        {
            assignment.IsCurrent = false;
            assignment.MoveOutDate = contract.StartDate.AddDays(-1);
        }

        var existingAssignment = currentAssignments.FirstOrDefault(item => item.RoomId == contract.RoomId);
        if (existingAssignment is null)
        {
            _context.TenantRooms.Add(new TenantRoom
            {
                TenantId = contract.TenantId,
                RoomId = contract.RoomId,
                MoveInDate = contract.StartDate,
                IsCurrent = true
            });
        }
        else
        {
            existingAssignment.MoveInDate = contract.StartDate;
            existingAssignment.MoveOutDate = null;
            existingAssignment.IsCurrent = true;
        }
    }

    private async Task RefreshExpiringContractsAsync()
    {
        var soon = DateTime.Today.AddDays(30);
        var contracts = await _context.Contracts
            .Where(item => item.Status == ContractStatus.Active && item.EndDate <= soon)
            .ToListAsync();

        foreach (var contract in contracts)
        {
            contract.Status = ContractStatus.ExpiringSoon;
        }

        if (contracts.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }
}
