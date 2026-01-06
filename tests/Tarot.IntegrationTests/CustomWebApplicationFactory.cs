using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tarot.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Tarot.Api.Controllers;
using Microsoft.Extensions.Diagnostics.HealthChecks; // Added

namespace Tarot.IntegrationTests;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseWebRoot("wwwroot"); // Ensure WebRootPath is set
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "UseInMemoryDatabase", "true" },
                { "Jwt:Key", "01234567890123456789012345678901" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the NpgSql health check that Program.cs adds
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                var dbCheck = options.Registrations.FirstOrDefault(r => r.Name == "Database");
                if (dbCheck != null)
                {
                    options.Registrations.Remove(dbCheck);
                }
            });

            // Force load controllers from the API assembly
            services.AddControllers()
                .AddApplicationPart(typeof(AppointmentsController).Assembly);

            // Configure Authentication to use "Test" scheme by default
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                "Test", options => { });

            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                
                db.Database.EnsureCreated();
            }
        });
    }
}
