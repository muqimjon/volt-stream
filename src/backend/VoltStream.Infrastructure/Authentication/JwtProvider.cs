namespace VoltStream.Infrastructure.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;

public static class JwtSecretResolver
{
    private static string? cached;

    public static byte[] Resolve(IConfiguration configuration)
    {
        cached ??= configuration["Jwt:Secret"] ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        return Encoding.UTF8.GetBytes(cached);
    }
}

public class JwtProvider(IConfiguration configuration) : IJwtProvider
{
    public string Generate(User user)
    {
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(JwtSecretResolver.Resolve(configuration));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "VoltStream",
            audience: configuration["Jwt:Audience"] ?? "VoltStreamUser",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
