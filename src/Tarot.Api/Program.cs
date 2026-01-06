using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using Tarot.Core.Services;
using Tarot.Infrastructure.Data;
using Tarot.Infrastructure.Services;

using Tarot.Infrastructure.Plugins;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Health Checks
var healthChecks = builder.Services.AddHealthChecks();
if (!builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
{
    var dbConn = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(dbConn))
    {
        healthChecks.AddNpgSql(dbConn, name: "Database");
    }
}

// Redis Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost";
try
{
    var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
    builder.Services.AddScoped<IRedisService, RedisService>();
}
catch
{
    // Fallback to In-Memory if Redis is not available (for dev/testing)
    Console.WriteLine("Redis not available, using In-Memory fallback.");
    builder.Services.AddSingleton<IRedisService, InMemoryRedisService>();
}

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

var authBuilder = builder.Services.AddAuthorizationBuilder();
authBuilder.AddPolicy("SCHEDULE_MANAGE", policy => policy.RequireClaim("permission", "SCHEDULE_MANAGE"));
authBuilder.AddPolicy("CONSULTATION_REPLY", policy => policy.RequireClaim("permission", "CONSULTATION_REPLY"));
authBuilder.AddPolicy("FINANCE_VIEW", policy => policy.RequireClaim("permission", "FINANCE_VIEW"));
authBuilder.AddPolicy("BLOG_MANAGE", policy => policy.RequireClaim("permission", "BLOG_MANAGE"));
authBuilder.AddPolicy("KNOWLEDGE_EDIT", policy => policy.RequireClaim("permission", "KNOWLEDGE_EDIT"));
authBuilder.AddPolicy("DESIGN_EDIT", policy => policy.RequireClaim("permission", "DESIGN_EDIT"));
authBuilder.AddPolicy("INBOX_MANAGE", policy => policy.RequireClaim("permission", "INBOX_MANAGE"));
authBuilder.AddPolicy("TRASH_MANAGE", policy => policy.RequireClaim("permission", "TRASH_MANAGE"));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-change-me";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TarotIssuer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TarotAudience";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Register Services
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// Mock Payment Service (as requested by user in previous turn)
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
app.MapHealthChecks("/health");
app.MapHealthChecks("/api/v1/health");

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
            await DbInitializer.SeedAsync(context);
            await DbInitializer.SeedIdentityAsync(services);
        }
        else
        {
             context.Database.EnsureCreated();
             await DbInitializer.SeedAsync(context);
             // Skip Identity seeding for InMemory (test/dev) to avoid duplicate role issues
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.Run();

public partial class Program { }
