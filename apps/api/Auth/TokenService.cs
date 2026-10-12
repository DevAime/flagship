using System.Security.Claims;
using System.Text;
using FlagForge.Api.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FlagForge.Api.Auth;

public class TokenService(IOptions<JwtOptions> options)
{
    public string Create(AdminUser user)
    {
        var o = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email)
            }),
            Issuer = o.Issuer,
            Audience = o.Audience,
            Expires = DateTime.UtcNow.AddMinutes(o.ExpiryMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
