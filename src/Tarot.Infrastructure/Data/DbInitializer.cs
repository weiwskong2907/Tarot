using Tarot.Core.Entities;
using Tarot.Core.Enums;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Tarot.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!context.Set<Service>().Any())
        {
            var services = new List<Service>
            {
                new() { Name = "General Tarot Reading", Price = 50.00m, DurationMin = 30, IsActive = true },
                new() { Name = "Love & Relationship Reading", Price = 75.00m, DurationMin = 45, IsActive = true },
                new() { Name = "Career & Finance Reading", Price = 60.00m, DurationMin = 40, IsActive = true },
                new() { Name = "Full Celtic Cross", Price = 120.00m, DurationMin = 60, IsActive = true }
            };
            await context.Set<Service>().AddRangeAsync(services);
            await context.SaveChangesAsync();
        }

        if (!context.Set<Card>().Any())
        {
            var loaded = await ReloadCardsAsync(context);
            if (!loaded)
            {
                await AddMinimalCardsAsync(context);
            }
        }

        if (!context.EmailTemplates.Any(e => e.Slug == "consultation-reply"))
        {
            var tpl = new EmailTemplate
            {
                Slug = "consultation-reply",
                SubjectTpl = "Your Consultation Reply",
                BodyHtml = "<h2>Hello @Model.UserName</h2><p>Your consultation reply:</p><blockquote>@Model.Reply</blockquote><p>Appointment time: @Model.AppointmentTime</p>"
            };
            await context.EmailTemplates.AddAsync(tpl);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedIdentityAsync(IServiceProvider services)
    {
        var userManager = services.GetService(typeof(Microsoft.AspNetCore.Identity.UserManager<AppUser>)) as Microsoft.AspNetCore.Identity.UserManager<AppUser>;
        var roleManager = services.GetService(typeof(Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>)) as Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>;
        if (userManager == null || roleManager == null) return;

        // If no users exist, create default Super Admin from environment variables
        if (!(await userManager.Users.AnyAsync()))
        {
            var email = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_EMAIL") ?? "admin@example.com";
            var password = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD") ?? "Passw0rd!";
            var fullName = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_NAME") ?? "Super Admin";
            var permissionsEnv = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PERMISSIONS");
            var permissions = string.IsNullOrWhiteSpace(permissionsEnv)
                ? new List<string> { "DESIGN_EDIT", "KNOWLEDGE_EDIT", "SCHEDULE_MANAGE", "CONSULTATION_REPLY", "FINANCE_VIEW", "BLOG_MANAGE", "TRASH_MANAGE", "INBOX_MANAGE" }
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(permissionsEnv) ?? new List<string>();

            var roleName = "SuperAdmin";
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole<Guid>(roleName));
            }

            var admin = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                CreatedAt = DateTimeOffset.UtcNow,
                Permissions = System.Text.Json.JsonSerializer.Serialize(permissions)
            };
            var result = await userManager.CreateAsync(admin, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, roleName);
            }
        }
    }

    public static async Task<bool> ReloadCardsAsync(AppDbContext context)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(baseDir, "config");
        var jsonPath = Path.Combine(configDir, "cards.json");
        var csvPath = Path.Combine(configDir, "cards.csv");

        List<CardSeedItem> items = new();

        try
        {
            if (File.Exists(jsonPath))
            {
                var json = await File.ReadAllTextAsync(jsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                items = JsonSerializer.Deserialize<List<CardSeedItem>>(json, options) ?? new List<CardSeedItem>();
            }
            else if (File.Exists(csvPath))
            {
                items = await ParseCsvAsync(csvPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading cards: {ex.Message}");
            return false;
        }

        if (items.Count == 0) return false;

        foreach (var item in items)
        {
            var existing = await context.Set<Card>().FirstOrDefaultAsync(c => c.Name == item.Name);
            var suit = Enum.TryParse<Suit>(item.Suit, true, out var s) ? s : Suit.MajorArcana;
            var arcana = Enum.TryParse<ArcanaType>(item.ArcanaType, true, out var a) ? a : ArcanaType.Major;

            if (existing != null)
                {
                    existing.NameCn = item.NameCn;
                    existing.ImageUrl = item.ImageUrl;
                    existing.Suit = suit;
                    existing.ArcanaType = arcana;
                    existing.MeaningUpright = item.MeaningUpright;
                    existing.MeaningUprightCn = item.MeaningUprightCn;
                    existing.MeaningReversed = item.MeaningReversed;
                    existing.MeaningReversedCn = item.MeaningReversedCn;
                    existing.Keywords = item.Keywords != null ? JsonSerializer.Serialize(item.Keywords) : null;
                    existing.AdminNotes = item.AdminNotes;
                }
                else
                {
                    var card = new Card
                    {
                        Name = item.Name,
                        NameCn = item.NameCn,
                        ImageUrl = item.ImageUrl,
                        Suit = suit,
                        ArcanaType = arcana,
                        MeaningUpright = item.MeaningUpright,
                        MeaningUprightCn = item.MeaningUprightCn,
                        MeaningReversed = item.MeaningReversed,
                        MeaningReversedCn = item.MeaningReversedCn,
                        Keywords = item.Keywords != null ? JsonSerializer.Serialize(item.Keywords) : null,
                        AdminNotes = item.AdminNotes
                    };
                    await context.Set<Card>().AddAsync(card);
                }
        }
        
        await context.SaveChangesAsync();
        return true;
    }

    private static async Task<List<CardSeedItem>> ParseCsvAsync(string path)
    {
        var lines = await File.ReadAllLinesAsync(path);
        var items = new List<CardSeedItem>();
        // Skip header if present (assume header if first line has "Name")
        var start = 0;
        if (lines.Length > 0 && lines[0].StartsWith("Name", StringComparison.OrdinalIgnoreCase)) start = 1;

        for (int i = start; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            // Simple CSV split (handling quotes is complex, assume simple format for now)
            var parts = line.Split(',');
            if (parts.Length < 1) continue;

            var item = new CardSeedItem
            {
                Name = parts[0].Trim(),
                Suit = parts.Length > 1 ? parts[1].Trim() : "MajorArcana",
                ArcanaType = parts.Length > 2 ? parts[2].Trim() : "Major",
                MeaningUpright = parts.Length > 3 ? parts[3].Trim() : null,
                MeaningReversed = parts.Length > 4 ? parts[4].Trim() : null,
                ImageUrl = parts.Length > 5 ? parts[5].Trim() : null
            };

            if (parts.Length > 6)
            {
                var keywords = parts[6].Trim();
                if (!string.IsNullOrEmpty(keywords))
                {
                    item.Keywords = keywords.Split(';').Select(k => k.Trim()).ToList();
                }
            }
            items.Add(item);
        }
        return items;
    }

    private static async Task AddMinimalCardsAsync(AppDbContext context)
    {
        var cards = new List<Card>
        {
            new() { Name = "The Fool", Suit = Suit.MajorArcana, ArcanaType = ArcanaType.Major, MeaningUpright = "Beginnings, innocence", MeaningReversed = "Recklessness" },
            new() { Name = "The Magician", Suit = Suit.MajorArcana, ArcanaType = ArcanaType.Major, MeaningUpright = "Manifestation, power", MeaningReversed = "Manipulation" },
            new() { Name = "Ace of Wands", Suit = Suit.Wands, ArcanaType = ArcanaType.Minor, MeaningUpright = "Inspiration, opportunities", MeaningReversed = "Delays" },
            new() { Name = "Two of Wands", Suit = Suit.Wands, ArcanaType = ArcanaType.Minor, MeaningUpright = "Planning, decisions", MeaningReversed = "Fear of change" },
            new() { Name = "Ace of Cups", Suit = Suit.Cups, ArcanaType = ArcanaType.Minor, MeaningUpright = "Love, compassion", MeaningReversed = "Emotional loss" },
            new() { Name = "Two of Cups", Suit = Suit.Cups, ArcanaType = ArcanaType.Minor, MeaningUpright = "Partnership, unity", MeaningReversed = "Imbalance" },
            new() { Name = "Ace of Swords", Suit = Suit.Swords, ArcanaType = ArcanaType.Minor, MeaningUpright = "Clarity, truth", MeaningReversed = "Confusion" },
            new() { Name = "Two of Swords", Suit = Suit.Swords, ArcanaType = ArcanaType.Minor, MeaningUpright = "Difficult choices", MeaningReversed = "Indecision" },
            new() { Name = "Ace of Pentacles", Suit = Suit.Pentacles, ArcanaType = ArcanaType.Minor, MeaningUpright = "New financial opportunity", MeaningReversed = "Lost opportunity" },
            new() { Name = "Two of Pentacles", Suit = Suit.Pentacles, ArcanaType = ArcanaType.Minor, MeaningUpright = "Balance, adaptability", MeaningReversed = "Overcommitted" }
        };
        await context.Set<Card>().AddRangeAsync(cards);
        await context.SaveChangesAsync();
    }

    internal class CardSeedItem
    {
        public string Name { get; set; } = string.Empty;
        public string? NameCn { get; set; }
        public string? ImageUrl { get; set; }
        public string Suit { get; set; } = "MajorArcana";
        public string ArcanaType { get; set; } = "Major";
        public string? MeaningUpright { get; set; }
        public string? MeaningUprightCn { get; set; }
        public string? MeaningReversed { get; set; }
        public string? MeaningReversedCn { get; set; }
        public List<string>? Keywords { get; set; }
        public string? AdminNotes { get; set; }
    }
}
