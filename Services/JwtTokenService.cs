using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GhanaHybridRentalApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GhanaHybridRentalApi.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var signingKey = _configuration["Jwt:SigningKey"] ?? "dev-only-signing-key-change-me";
        var issuer = _configuration["Jwt:Issuer"] ?? "GhanaHybridRental";
        var audience = _configuration["Jwt:Audience"] ?? "GhanaHybridRental";
        var lifetimeMinutes = int.TryParse(_configuration["Jwt:TokenLifetimeMinutes"], out var tl) ? tl : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.Phone!));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email!));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
