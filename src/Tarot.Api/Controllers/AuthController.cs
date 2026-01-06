using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tarot.Api.Dtos;
using Tarot.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.RateLimiting;

namespace Tarot.Api.Controllers;

[EnableRateLimiting("strict")]
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IConfiguration config
) : ControllerBase
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly SignInManager<AppUser> _signInManager = signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
    private readonly IConfiguration _config = config;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (result.Succeeded)
        {
            var roleName = "Customer";
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
            await _userManager.AddToRoleAsync(user, roleName);
            var token = await GenerateJwtAsync(user);
            return Ok(new { token });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized(new { Message = "Invalid login attempt" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var token = await GenerateJwtAsync(user);
            return Ok(new { token });
        }

        return Unauthorized(new { Message = "Invalid login attempt" });
    }

    private async Task<string> GenerateJwtAsync(AppUser user)
    {
        var key = _config["Jwt:Key"] ?? "dev-secret-key-change-me";
        var issuer = _config["Jwt:Issuer"] ?? "TarotIssuer";
        var audience = _config["Jwt:Audience"] ?? "TarotAudience";
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        
        if (!string.IsNullOrWhiteSpace(user.Permissions))
        {
            try
            {
                var perms = JsonSerializer.Deserialize<List<string>>(user.Permissions) ?? [];
                foreach (var p in perms)
                {
                    claims.Add(new("permission", p));
                }
            }
            catch
            {
            }
        }

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
