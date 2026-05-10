using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Autopartspro.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryDays;

        public JwtService(IConfiguration config)
        {
            _config = config;
            _secretKey = config["JwtSettings:SecretKey"]
                ?? throw new Exception("JWT SecretKey is missing in configuration.");
            _issuer = config["JwtSettings:Issuer"] ?? "Autopartspro";
            _audience = config["JwtSettings:Audience"] ?? "AutopartsproUsers";
            _expiryDays = int.Parse(config["JwtSettings:ExpiryDays"] ?? "7");
        }

        public string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("city", user.City),
                new Claim("status", user.Status.ToString()),
                new Claim("isEmailVerified", user.IsEmailVerified.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_expiryDays),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                return jwtToken.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            }
            catch
            {
                return null;
            }
        }

        public string? GetEmailFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}