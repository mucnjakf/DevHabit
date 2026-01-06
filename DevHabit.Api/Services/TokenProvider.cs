using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DevHabit.Api.Services;

public sealed class TokenProvider(IOptions<JwtAuthOptions> jwtAuthOptions)
{
    public TokenDto Create(GetTokenDto getTokenDto)
    {
        return new TokenDto(GenerateAccessToken(getTokenDto), GenerateRefreshToken());
    }

    private string GenerateAccessToken(GetTokenDto getTokenDto)
    {
        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthOptions.Value.Key));
        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, getTokenDto.UserId),
            new(JwtRegisteredClaimNames.Email, getTokenDto.Email)
        ];

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtAuthOptions.Value.Issuer,
            Audience = jwtAuthOptions.Value.Audience,
            Expires = DateTime.UtcNow.AddMinutes(jwtAuthOptions.Value.ExpirationInMinutes),
            SigningCredentials = signingCredentials,
            Subject = new ClaimsIdentity(claims)
        };

        var jsonWebTokenHandler = new JsonWebTokenHandler();

        return jsonWebTokenHandler.CreateToken(securityTokenDescriptor);
    }

    private static string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(randomBytes);
    }
}
