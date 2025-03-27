using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Config;

namespace Server.Services;

public class AppleTokenValidatorService(IOptions<AppleConfig> config)
{
    private JsonWebKeySet? _appleKeys;
    private DateTime? _lastKeysFetch;

    public async Task<JwtSecurityToken> ValidateToken(string idToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://appleid.apple.com",
            ValidateAudience = true,
            ValidAudience = config.Value.ClientId,
            ValidateLifetime = true,
            IssuerSigningKeys = (await GetKeys()).Keys
        };

        handler.ValidateToken(idToken, validationParameters, out var validatedToken);
        return (JwtSecurityToken)validatedToken;
    }

    private async Task<JsonWebKeySet> GetKeys()
    {
        var httpClient = new HttpClient();

        // ReSharper disable once InvertIf
        if (_appleKeys == null || (_lastKeysFetch != null && (DateTime.Today - _lastKeysFetch.Value).Days >= 1))
        {
            var appleKeysJson = await httpClient.GetStringAsync("https://appleid.apple.com/auth/keys");
            _appleKeys = new JsonWebKeySet(appleKeysJson);
            _lastKeysFetch = DateTime.Now;
        }

        return _appleKeys;
    }
}
