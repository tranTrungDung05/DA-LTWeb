using DACS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.Enums;
using smart_hostel_management_system.Models.Payments;
using smart_hostel_management_system.Models.ViewModels;
using smart_hostel_management_system.Services;

namespace smart_hostel_management_system.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Account> _userManager;
        private readonly PaymentGatewayService _gateway;
        private readonly PaymentGatewayOptions _options;

        public PaymentsController(
            AppDbContext context,
            UserManager<Account> userManager,
            PaymentGatewayService gateway,
            IOptions<PaymentGatewayOptions> options)
        {
            _context = context;
            _userManager = userManager;
            _gateway = gateway;
            _options = options.Value;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int invoiceId, string provider)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                return NotFound();
            }

            if (!await CanAccessInvoice(invoice))
            {
                return Forbid();
            }

            if (!invoice.IsPublished || invoice.Status == InvoiceStatus.Paid)
            {
                TempData["Error"] = "Hóa đơn chưa phát hành hoặc đã được thanh toán.";
                return RedirectToAction("Details", "Invoices", new { id = invoice.Id });
            }

            var paidAmount = invoice.Payments
                .Where(payment => payment.Status == PaymentStatus.Succeeded)
                .Sum(payment => payment.Amount);
            var amount = invoice.TotalAmount - paidAmount;

            if (amount <= 0 || amount != decimal.Truncate(amount))
            {
                TempData["Error"] = "Số tiền thanh toán không hợp lệ.";
                return RedirectToAction("Details", "Invoices", new { id = invoice.Id });
            }

            provider = provider.Trim().ToUpperInvariant();
            if (provider != "VNPAY")
            {
                return BadRequest();
            }

            var existingPayment = invoice.Payments
                .Where(payment =>
                    payment.PaymentMethod == provider
                    && payment.Status == PaymentStatus.Pending
                    && !string.IsNullOrWhiteSpace(payment.PaymentUrl))
                .OrderByDescending(payment => payment.CreatedAt)
                .FirstOrDefault();
            if (existingPayment != null)
            {
                return RedirectToAction(nameof(Checkout), new { id = existingPayment.Id });
            }

            var orderId = $"INV{invoice.Id}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                Amount = amount,
                CreatedAt = DateTime.Now,
                PaymentMethod = provider,
                ProviderOrderId = orderId,
                Status = PaymentStatus.Pending,
                Note = "Đang chờ kết quả từ cổng thanh toán"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            try
            {
                var orderInfo = $"Thanh toán hóa đơn {invoice.InvoiceNumber}";
                var result = _gateway.CreateVnPayPayment(
                    orderId,
                    decimal.ToInt64(amount),
                    orderInfo,
                    BuildAbsoluteUrl(nameof(VnPayReturn)),
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");

                payment.RequestId = result.RequestId;
                payment.PaymentUrl = result.PaymentUrl;
                payment.QrCodeData = result.QrCodeData;
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Checkout), new { id = payment.Id });
            }
            catch (Exception ex)
            {
                payment.Status = PaymentStatus.Failed;
                payment.Note = ex.Message;
                await _context.SaveChangesAsync();
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "Invoices", new { id = invoice.Id });
            }
        }

        public async Task<IActionResult> Checkout(int id)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            if (!await CanAccessInvoice(payment.Invoice!))
            {
                return Forbid();
            }

            return View(payment);
        }

        public async Task<IActionResult> QrCode(int id)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null || string.IsNullOrWhiteSpace(payment.PaymentUrl))
            {
                return NotFound();
            }

            if (!await CanAccessInvoice(payment.Invoice!))
            {
                return Forbid();
            }

            var content = string.IsNullOrWhiteSpace(payment.QrCodeData)
                ? payment.PaymentUrl
                : payment.QrCodeData;
            var image = PngByteQRCodeHelper.GetQRCode(content, QRCodeGenerator.ECCLevel.Q, 10);
            return File(image, "image/png");
        }

        [AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            if (!_gateway.ValidateVnPayCallback(Request.Query))
            {
                return Json(new { RspCode = "97", Message = "Invalid Checksum" });
            }

            var orderId = Request.Query["vnp_TxnRef"].ToString();
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.ProviderOrderId == orderId);

            if (payment == null)
            {
                return Json(new { RspCode = "01", Message = "Order not found" });
            }

            if (!long.TryParse(Request.Query["vnp_Amount"], out var returnedAmount)
                || payment.Amount * 100 != returnedAmount)
            {
                return Json(new { RspCode = "04", Message = "Invalid amount" });
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                return Json(new { RspCode = "02", Message = "Order already confirmed" });
            }

            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var transactionStatus = Request.Query["vnp_TransactionStatus"].ToString();
            await ApplyGatewayResult(
                payment,
                responseCode == "00" && transactionStatus == "00",
                responseCode,
                Request.Query["vnp_TransactionNo"].ToString(),
                $"VNPay response: {responseCode}");

            return Json(new { RspCode = "00", Message = "Confirm Success" });
        }

        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var isValid = _gateway.ValidateVnPayCallback(Request.Query);
            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var transactionStatus = Request.Query["vnp_TransactionStatus"].ToString();
            var isSuccessful = isValid
                && responseCode == "00"
                && transactionStatus == "00";

            if (isSuccessful)
            {
                var orderId = Request.Query["vnp_TxnRef"].ToString();
                var payment = await _context.Payments
                    .Include(p => p.Invoice)
                    .FirstOrDefaultAsync(p => p.ProviderOrderId == orderId);

                if (payment == null
                    || !long.TryParse(Request.Query["vnp_Amount"], out var returnedAmount)
                    || payment.Amount * 100 != returnedAmount)
                {
                    isSuccessful = false;
                }
                else
                {
                    await ApplyGatewayResult(
                        payment,
                        true,
                        responseCode,
                        Request.Query["vnp_TransactionNo"].ToString(),
                        "VNPay xác nhận thanh toán qua Return URL");
                }
            }

            return View("Result", new PaymentResultViewModel
            {
                IsValid = isValid,
                IsSuccessful = isSuccessful,
                Message = isValid
                    ? isSuccessful
                        ? "Thanh toán thành công. Hóa đơn đã được cập nhật."
                        : $"VNPay trả về mã {responseCode}."
                    : "Chữ ký VNPay không hợp lệ.",
                TransactionCode = Request.Query["vnp_TransactionNo"].ToString()
            });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                .ThenInclude(i => i!.Tenant)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }

        private async Task ApplyGatewayResult(
            Payment payment,
            bool succeeded,
            string responseCode,
            string transactionCode,
            string note)
        {
            if (payment.Status != PaymentStatus.Pending)
            {
                return;
            }

            payment.ProviderResponseCode = responseCode;
            payment.TransactionCode = transactionCode;
            payment.Note = note;
            payment.Status = succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed;

            if (succeeded)
            {
                payment.DateTimePaidAt = DateTime.Now;
                payment.Invoice!.Status = InvoiceStatus.Paid;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> CanAccessInvoice(Invoice invoice)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var account = await _userManager.GetUserAsync(User);
            if (account == null)
            {
                return false;
            }

            return await _context.Tenants.AnyAsync(
                tenant => tenant.Id == invoice.TenantId && tenant.AccountId == account.Id);
        }

        private string BuildAbsoluteUrl(string action)
        {
            var path = Url.Action(action, "Payments")!;
            if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            {
                return $"{_options.PublicBaseUrl.TrimEnd('/')}{path}";
            }

            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{path}";
        }
    }
}
