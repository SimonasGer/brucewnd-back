using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class JwtHelper
{
    public static string GenerateToken(User user, IConfiguration config)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var keyString = config["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(keyString))
            throw new InvalidOperationException("Jwt:Key is missing.");

        // HS256 requires >= 256-bit (32-byte) key
        if (Encoding.UTF8.GetByteCount(keyString) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (256 bits).");

        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        // Add roles (dedup just in case)
        var roles = user.UserRoles?
            .Select(ur => ur.Role?.Name)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase) ?? [];

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role!)); // Keep consistent with RoleClaimType = ClaimTypes.Role

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddHours(6),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}