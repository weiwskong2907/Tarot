using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Api.ViewModels;

namespace Tarot.Api.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        // Mock data - replace with DB queries
        var model = new DashboardViewModel
        {
            LoyaltyPoints = 150,
            UpcomingAppointments = new List<AppointmentViewModel>
            {
                new() { ServiceName = "Standard Reading", Date = DateTime.Today.AddDays(2), Status = "Confirmed" }
            },
            RecentHistory = new List<AppointmentViewModel>
            {
                new() { ServiceName = "Quick Draw", Date = DateTime.Today.AddDays(-5), Status = "Completed" }
            }
        };
        return View(model);
    }
}
