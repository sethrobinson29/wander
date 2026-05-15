using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Wander.Client.Services;

public class JwtAuthStateProvider(LocalStorage localStorage) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private AuthenticationState? _cached;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cached is not null) return _cached;

        var token = await localStorage.GetAsync("accessToken");
        if (string.IsNullOrWhiteSpace(token))
            return _cached = Anonymous;

        var claims = ParseClaimsFromJwt(token);
        var expiry = claims.FirstOrDefault(c => c.Type == "exp");
        if (expiry != null)
        {
            var expUnix = long.Parse(expiry.Value);
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
            {
                await localStorage.RemoveAsync("accessToken");
                return _cached = Anonymous;
            }
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        return _cached = new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserChanged()
    {
        _cached = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        var map = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

        var result = new List<Claim>();
        foreach (var kvp in map)
        {
            var type = kvp.Key switch
            {
                "sub" => ClaimTypes.NameIdentifier,
                "email" => ClaimTypes.Email,
                "unique_name" => ClaimTypes.Name,
                "role" => ClaimTypes.Role,
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" => ClaimTypes.NameIdentifier,
                _ => kvp.Key,
            };

            if (kvp.Value.ValueKind == JsonValueKind.Array)
                result.AddRange(kvp.Value.EnumerateArray().Select(v => new Claim(type, v.ToString())));
            else
                result.Add(new Claim(type, kvp.Value.ToString()));
        }
        return result;
    }
}