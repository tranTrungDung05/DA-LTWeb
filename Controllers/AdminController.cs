using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace smart_hostel_management_system.Controllers
{
    [Authorize(Roles = "Admin")] //cai nay quan trong
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}