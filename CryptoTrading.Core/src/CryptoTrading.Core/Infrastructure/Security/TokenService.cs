using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Infrastructure.Security;

public interface ITokenService
{
    string GenerateToken(User user, int expiryMinutes = 1440);
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtTokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey is missing.");
        _issuer = configuration["Jwt:Issuer"] ?? "CryptoTradingCore";
        _audience = configuration["Jwt:Audience"] ?? "CryptoTradingReact";
    }

    public string GenerateToken(User user, int expiryMinutes = 1440)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("given_name", user.FirstName ?? string.Empty),
                new Claim("family_name", user.LastName ?? string.Empty)
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
