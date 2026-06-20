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

        /// <summary>
        /// Tự động tạo hóa đơn hàng tháng từ các hợp đồng đang active
        /// </summary>
        /// <param name="month">Tháng (1-12)</param>
        /// <param name="year">Năm</param>
        /// <returns>Redirect về Index</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthlyInvoices(int month, int year)
        {
            try
            {
                // Lấy tất cả hợp đồng đang Active kèm chi tiết
                var contracts = await _context.Contracts
                    .Include(c => c.Tenant)
                    .Include(c => c.ContractDetails)
                    .ThenInclude(cd => cd.Room)
                    .Include(c => c.ContractServices)
                    .ThenInclude(cs => cs.Service)
                    .Where(c => c.Status == ContractStatus.Active)
                    .ToListAsync();

                int generatedCount = 0;

                foreach (var contract in contracts)
                {
                    // Validation 1: Kiểm tra xem hóa đơn đã tồn tại cho hợp đồng này trong tháng/năm chưa
                    bool invoiceExists = await _context.Invoices
                        .AnyAsync(i => i.ContractId == contract.Id && i.Month == month && i.Year == year);

                    if (invoiceExists)
                    {
                        continue; // Bỏ qua nếu đã tạo rồi (không tạo trùng)
                    }

                    // Lấy phòng từ ContractDetail
                    var room = contract.ContractDetails.FirstOrDefault()?.Room;
                    if (room == null)
                    {
                        continue; // Bỏ qua nếu không có phòng liên kết
                    }

                    // 1. Tạo hóa đơn mới
                    var invoice = new Invoice
                    {
                        ContractId = contract.Id,
                        TenantId = contract.TenantId,
                        RoomId = room.Id,
                        Month = month,
                        Year = year,
                        InvoiceNumber = $"HD-{contract.Id:D4}-{month:D2}-{year}",
                        Status = InvoiceStatus.Unpaid,
                        IsPublished = false,
                        IssueDate = DateTime.Now,
                        // 4. Thiết lập ngày hạn thanh toán (cuối tháng + 7 ngày)
                        DueDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)).AddDays(7),
                        // 4. Khởi tạo TotalAmount với giá phòng
                        TotalAmount = room.MonthlyPrice
                    };

                    // 2. Duyệt qua danh sách ContractServices và thêm vào InvoiceDetails
                    if (contract.ContractServices.Any())
                    {
                        foreach (var contractService in contract.ContractServices)
                        {
                            if (contractService.Service == null)
                                continue;

                            // 3. Tạo InvoiceDetail với Quantity = 0 (mặc định để admin nhập sau)
                            var invoiceDetail = new InvoiceDetail
                            {
                                ServiceId = contractService.ServiceId,
                                Unit = contractService.Service.Unit,
                                Quantity = 0, // Mặc định = 0 để admin nhập số điện/nước sau
                                UnitPrice = contractService.PriceAtContract, // Lưu giá chốt từ hợp đồng
                                SubTotal = 0 // Tính lại khi admin nhập Quantity
                            };

                            invoice.InvoiceDetails.Add(invoiceDetail);

                            // 4. Cộng giá dịch vụ cố định vào TotalAmount
                            // (Nếu là dịch vụ có giá cố định, không phụ thuộc Quantity)
                            invoice.TotalAmount += contractService.PriceAtContract;
                        }
                    }

                    _context.Invoices.Add(invoice);
                    generatedCount++;
                }

                await _context.SaveChangesAsync();

                if (generatedCount > 0)
                {
                    TempData["Success"] = $"✓ Đã tạo thành công {generatedCount} hóa đơn nháp cho tháng {month}/{year}.";
                }
                else
                {
                    TempData["Info"] = $"Không có hóa đơn mới nào để tạo (tất cả đã tồn tại hoặc không có hợp đồng active).";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
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
                CreatedAt = DateTime.Now,
                DateTimePaidAt = DateTime.Now,
                PaymentMethod = "Tiền mặt/Chuyển khoản",
                ProviderOrderId = $"MANUAL-{invoice.Id}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Status = PaymentStatus.Succeeded,
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
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.Id == id);
            
            if (invoice == null || invoice.IsPublished) 
                return NotFound();

            if (invoice.Room == null)
            {
                TempData["Error"] = "Hóa đơn không có phòng liên kết.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa các chi tiết hóa đơn cũ
            _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
            invoice.InvoiceDetails.Clear();

            // Cập nhật thông tin hóa đơn
            invoice.MainContent = model.MainContent;
            invoice.DueDate = model.DueDate;

            // 4. Khởi tạo TotalAmount = giá phòng
            invoice.TotalAmount = invoice.Room.MonthlyPrice;

            var services = await _context.Services.ToDictionaryAsync(s => s.Id);

            // 2 & 3. Cập nhật InvoiceDetails từ model (bao gồm cả Quantity = 0)
            foreach (var serviceModel in model.Services)
            {
                if (!services.TryGetValue(serviceModel.ServiceId, out var service))
                    continue;

                // 3. Tính SubTotal = Quantity * UnitPrice
                decimal subTotal = serviceModel.Quantity * serviceModel.UnitPrice;

                var invoiceDetail = new InvoiceDetail
                {
                    ServiceId = serviceModel.ServiceId,
                    Unit = service.Unit,
                    Quantity = serviceModel.Quantity,
                    UnitPrice = serviceModel.UnitPrice,
                    SubTotal = subTotal
                };

                invoice.InvoiceDetails.Add(invoiceDetail);

                // 4. Cộng SubTotal vào TotalAmount (chỉ khi Quantity > 0)
                if (serviceModel.Quantity > 0)
                {
                    invoice.TotalAmount += subTotal;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật hóa đơn thành công!";
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
            TempData["Success"] = "Hóa đơn đã được phát hành!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Hiển thị chi tiết hóa đơn
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Tenant)
                .Include(i => i.Room)
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!User.IsInRole("Admin"))
            {
                var account = await _userManager.GetUserAsync(User);
                var canView = account != null
                    && invoice.IsPublished
                    && await _context.Tenants.AnyAsync(
                        tenant => tenant.Id == invoice.TenantId && tenant.AccountId == account.Id);

                if (!canView)
                {
                    return Forbid();
                }
            }

            return View(invoice);
        }

        [AllowAnonymous]
        public async Task<IActionResult> MyInvoices()
        {
            // Allow only authenticated users to see their invoices; if not authenticated, redirect to login
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _userManager.GetUserAsync(User);
            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Try to find tenant id linked to current account
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.AccountId == account.Id);
            if (tenant == null)
            {
                // No tenant linked to this account
                TempData["Info"] = "Tài khoản của bạn chưa được liên kết với cư dân nào.";
                return View(new List<Invoice>());
            }

            var invoices = await _context.Invoices
                .AsNoTracking()
                .Where(i => i.TenantId == tenant.Id && i.IsPublished)
                .Include(i => i.Room)
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Month)
                .ToListAsync();

            return View(invoices);
        }

        private async Task<InvoiceEditViewModel?> BuildEditModel(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Room)
                .Include(i => i.Tenant)
                .FirstOrDefaultAsync(i => i.Id == id);
            
            if (invoice == null) 
                return null;

            var services = await _context.Services.ToListAsync();
            
            return new InvoiceEditViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                TenantName = invoice.Tenant?.FullName ?? "N/A",
                RoomNumber = invoice.Room?.RoomNumber ?? "N/A",
                RoomMonthlyPrice = invoice.Room?.MonthlyPrice ?? 0,
                MainContent = invoice.MainContent,
                DueDate = invoice.DueDate,
                Services = services.Select(s => {
                    var d = invoice.InvoiceDetails.FirstOrDefault(x => x.ServiceId == s.Id);
                    return new InvoiceServiceViewModel
                    {
                        ServiceId = s.Id,
                        ServiceName = s.Name,
                        Unit = s.Unit,
                        Quantity = d?.Quantity ?? 0,
                        UnitPrice = d?.UnitPrice ?? s.Price
                    };
                }).ToList()
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPayment(int id)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null || invoice.Status == InvoiceStatus.Paid) 
                return NotFound();

            // Cập nhật trạng thái thành "Chờ xác nhận" (Pending)
            invoice.Status = InvoiceStatus.Pending; 
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Yêu cầu thanh toán của bạn đã được gửi. Admin sẽ kiểm tra và xác nhận sớm.";
            return RedirectToAction(nameof(MyInvoices));
        }
    }
}
