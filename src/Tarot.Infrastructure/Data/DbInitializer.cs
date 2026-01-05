using Tarot.Core.Entities;

namespace Tarot.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (context.Set<Service>().Any())
        {
            return; // DB has been seeded
        }

        var services = new List<Service>
        {
            new()
            {
                Name = "General Tarot Reading",
                Price = 50.00m,
                DurationMin = 30,
                IsActive = true
            },
            new()
            {
                Name = "Love & Relationship Reading",
                Price = 75.00m,
                DurationMin = 45,
                IsActive = true
            },
            new()
            {
                Name = "Career & Finance Reading",
                Price = 60.00m,
                DurationMin = 40,
                IsActive = true
            },
            new()
            {
                Name = "Full Celtic Cross",
                Price = 120.00m,
                DurationMin = 60,
                IsActive = true
            }
        };

        await context.Set<Service>().AddRangeAsync(services);
        await context.SaveChangesAsync();
    }
}
