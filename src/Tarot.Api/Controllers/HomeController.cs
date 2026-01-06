using Microsoft.AspNetCore.Mvc;
using Tarot.Api.ViewModels;

namespace Tarot.Api.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new HomeViewModel
        {
            Title = "Welcome to Tarot Platform",
            FeaturedServices = new List<ServiceViewModel>
            {
                new() { Name = "Standard Reading", Description = "A comprehensive 3-card spread.", Price = 50, Duration = "30 min" },
                new() { Name = "Full Life Path", Description = "Deep dive into your past, present, and future.", Price = 100, Duration = "60 min" }
            }
        };
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
