using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Data;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.ViewModels;

namespace smart_hostel_management_system.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(user => user.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "Email nay da duoc su dung.");
            return View(model);
        }

        var ownerRole = await _context.Roles.FirstAsync(role => role.Name == "Owner");
        var user = new AppUser
        {
            FullName = model.FullName.Trim(),
            Email = email,
            PhoneNumber = model.PhoneNumber,
            RoleId = ownerRole.Id
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await SignInAsync(user, ownerRole.Name, false);

        return RedirectToAction("Index", "Rooms");
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.Email == email);

        if (user is null || !user.IsActive || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email hoac mat khau khong dung.");
            return View(model);
        }

        await SignInAsync(user, user.Role?.Name ?? "Owner", model.RememberMe);
        return LocalRedirect(returnUrl ?? Url.Action("Index", "Rooms")!);
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await CurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await CurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Da cap nhat ho so.";
        return RedirectToAction(nameof(Profile));
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(item => item.Email == email && item.IsActive);
        if (user is not null)
        {
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = CreateSecureToken(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            TempData["ResetLink"] = Url.Action(nameof(ResetPassword), "Account", new { token = resetToken.Token }, Request.Scheme);
        }

        TempData["Success"] = "Neu email ton tai, he thong da tao lien ket dat lai mat khau.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string token)
    {
        var resetToken = await ValidResetTokenQuery(token).FirstOrDefaultAsync();
        if (resetToken is null)
        {
            TempData["Success"] = "Lien ket dat lai mat khau khong hop le hoac da het han.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resetToken = await ValidResetTokenQuery(model.Token)
            .Include(item => item.User)
            .FirstOrDefaultAsync();

        if (resetToken?.User is null)
        {
            ModelState.AddModelError(string.Empty, "Lien ket dat lai mat khau khong hop le hoac da het han.");
            return View(model);
        }

        resetToken.User.PasswordHash = _passwordHasher.HashPassword(resetToken.User, model.NewPassword);
        resetToken.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Da dat lai mat khau. Ban co the dang nhap lai.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await CurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mat khau hien tai khong dung.");
            return View(model);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Da doi mat khau.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task<AppUser?> CurrentUserAsync()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var userId)
            ? await _context.Users.FindAsync(userId)
            : null;
    }

    private async Task SignInAsync(AppUser user, string roleName, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = rememberMe });
    }

    private IQueryable<PasswordResetToken> ValidResetTokenQuery(string token)
    {
        return _context.PasswordResetTokens
            .Where(item => item.Token == token && item.UsedAt == null && item.ExpiresAt > DateTime.UtcNow);
    }

    private static string CreateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }
}
