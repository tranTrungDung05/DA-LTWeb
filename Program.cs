using DACS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Models.Core;
using smart_hostel_management_system.Models.Payments;
using smart_hostel_management_system.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Kết nối Cơ sở dữ liệu SQL Server(hoặc LocalDB) thông qua Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình Identity 
builder.Services.AddIdentity<Account, IdentityRole<int>>(options => {
    options.Password.RequireDigit = false; 
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddControllersWithViews();
builder.Services.Configure<PaymentGatewayOptions>(
    builder.Configuration.GetSection("PaymentGateways"));
builder.Services.AddHttpClient<PaymentGatewayService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Automatically add authentication and authorization middleware to the request pipeline
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ====================================================================
// MẶC ĐỊNH SẴN TÀI KHOẢN ADMIN CHẠY NGẦM 
// ====================================================================
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.EnsureCreated();

    if (!userManager.Users.Any(u => u.UserName == "admin"))
    {
        var adminAccount = new Account
        {
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@hostel.com",
            NormalizedEmail = "ADMIN@HOSTEL.COM",
            FullName = "Chủ Trọ",
            Role = smart_hostel_management_system.Models.Enums.RoleType.Admin,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(adminAccount, "Admin@123");

        if (result.Succeeded)
        {
            await userManager.AddClaimAsync(adminAccount, new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"));
        }
    }
}
// ====================================================================

app.Run();
