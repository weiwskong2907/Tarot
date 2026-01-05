using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using Tarot.Core.Services;
using Tarot.Infrastructure.Data;
using Tarot.Infrastructure.Services;

using Tarot.Infrastructure.Plugins;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
    {
        options.UseInMemoryDatabase("TarotDb");
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// Configure Identity
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Register Services
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IPaymentService, MockPaymentService>();

// Register Plugin Manager
builder.Services.AddSingleton<IPluginManager>(sp =>
{
    var manager = new PluginManager();
    // Load plugins from a 'plugins' directory in the running path
    var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
    manager.LoadPlugins(pluginPath);
    
    // Optionally register built-in sample directly for testing
    // In a real scenario, these would be separate DLLs
    return manager;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        if (context.Database.IsRelational())
        {
            context.Database.Migrate(); // Ensure DB is created
        }
        else
        {
             context.Database.EnsureCreated();
        }
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.Run();

public partial class Program { }
