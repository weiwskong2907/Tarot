using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Api.ViewModels;

namespace Tarot.Api.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("admin-panel")] // Custom route to avoid conflict with API
public class AdminWebController : Controller
{
    public IActionResult Index()
    {
        var model = new AdminDashboardViewModel
        {
            PendingOrders = 5,
            TodaysAppointments = 3,
            TotalRevenue = 1250.00m,
            Tasks = new List<AdminTaskViewModel>
            {
                new() { Title = "Reply to John Doe", Priority = "High", DueTime = "10:00 AM" },
                new() { Title = "Confirm payments", Priority = "Medium", DueTime = "12:00 PM" }
            }
        };
        return View(model);
    }
}
