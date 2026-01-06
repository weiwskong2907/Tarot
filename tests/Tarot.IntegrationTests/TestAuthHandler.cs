using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Collections.Generic;

namespace Tarot.IntegrationTests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userIdHeader = Request.Headers["X-Test-UserId"].FirstOrDefault();
        var idValue = Guid.TryParse(userIdHeader, out var parsed) ? parsed.ToString() : Guid.NewGuid().ToString();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, idValue) };
        var permsHeader = Request.Headers["X-Test-Permissions"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(permsHeader))
        {
            foreach (var p in permsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("permission", p));
            }
        }
        var rolesHeader = Request.Headers["X-Test-Roles"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(rolesHeader))
        {
            foreach (var r in rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }
        }
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
