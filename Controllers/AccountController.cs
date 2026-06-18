using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Account> _signInManager;
        private readonly UserManager<Account> _userManager;

        public AccountController(SignInManager<Account> signInManager, UserManager<Account> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // [GET]: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // [POST]: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Tài khoản và mật khẩu không được bỏ trống.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(username, password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(username);
                if (user != null && user.Role == smart_hostel_management_system.Models.Enums.RoleType.Admin)
                {
                    return RedirectToAction("Index", "Admin");
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác.");
            return View();
        }

        // [GET]: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // [POST]: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string fullName, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu nhập lại không trùng khớp.");
                return View();
            }

            var userExists = await _userManager.FindByNameAsync(username);
            if (userExists != null)
            {
                ModelState.AddModelError("", "Tài khoản này đã tồn tại.");
                return View();
            }

            var newAccount = new Account
            {
                UserName = username,
                Email = email,
                FullName = fullName,
                Role = smart_hostel_management_system.Models.Enums.RoleType.User, // Mặc định luôn là User thường
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newAccount, password);

            if (result.Succeeded)
            {
                await _userManager.AddClaimAsync(newAccount, new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "User"));
                await _signInManager.SignInAsync(newAccount, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View();
        }

        // [POST]: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}