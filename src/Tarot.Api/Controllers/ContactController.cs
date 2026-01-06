using Microsoft.AspNetCore.Mvc;
using Tarot.Api.ViewModels;
using Tarot.Infrastructure.Data;
using Tarot.Core.Entities;

namespace Tarot.Api.Controllers;

public class ContactController : Controller
{
    private readonly AppDbContext _db;

    public ContactController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactViewModel model)
    {
        if (ModelState.IsValid)
        {
            var message = new ContactMessage
            {
                Name = model.Name,
                Email = model.Email,
                Message = model.Message,
                CreatedAt = DateTime.UtcNow
            };

            _db.ContactMessages.Add(message);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for your message! We will get back to you soon.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }
}
