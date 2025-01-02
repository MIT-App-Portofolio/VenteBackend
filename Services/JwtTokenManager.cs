using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Config;

namespace Server.Services;

public class JwtTokenManager(IOptions<JwtConfig> config)
{
    private readonly JwtConfig _config = config.Value;
    
    public string GenerateToken(string username, string email, string id)
    {
        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(_config.Issuer, _config.Audience, [
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(JwtRegisteredClaimNames.Sub, id),
        ], expires: null, signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}