using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevHabit.Api.Constants;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Options;
using DevHabit.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevHabit.UnitTests.Services;

public sealed class TokenProviderTests
{
    private readonly TokenProvider _sut;
    private readonly JwtAuthOptions _jwtAuthOptions;

    public TokenProviderTests()
    {
        IOptions<JwtAuthOptions> options = Options.Create(new JwtAuthOptions
        {
            Key = "your-secret-key-here-that-should-also-be-fairly-long",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationInMinutes = 30,
            RefreshTokenExpirationInDays = 7,
        });

        _jwtAuthOptions = options.Value;
        _sut = new TokenProvider(options);
    }

    [Fact]
    public void Create_ShouldReturnAccessTokens()
    {
        CreateTokenDto dto = new()
        {
            UserId = $"u_{Guid.CreateVersion7()}",
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        TokenDto accessTokensDto = _sut.Create(dto);

        Assert.NotNull(accessTokensDto.AccessToken);
        Assert.NotNull(accessTokensDto.RefreshToken);
    }

    [Fact]
    public void Create_ShouldGenerateValidAccessToken()
    {
        CreateTokenDto dto = new()
        {
            UserId = $"u_{Guid.CreateVersion7()}",
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        TokenDto accessTokensDto = _sut.Create(dto);

        JwtSecurityTokenHandler handler = new()
        {
            MapInboundClaims = false,
        };

        TokenValidationParameters validationParameters = new()
        {
            ValidIssuer = _jwtAuthOptions.Issuer,
            ValidAudience = _jwtAuthOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtAuthOptions.Key)),
        };

        ClaimsPrincipal claimsPrincipal = handler.ValidateToken(
            accessTokensDto.AccessToken,
            validationParameters,
            out SecurityToken validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal(dto.UserId, claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal(dto.Email, claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Email));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueRefreshTokens()
    {
        CreateTokenDto dto = new()
        {
            UserId = $"u_{Guid.CreateVersion7()}",
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        TokenDto accessTokensDto1 = _sut.Create(dto);
        TokenDto accessTokensDto2 = _sut.Create(dto);

        Assert.NotEqual(accessTokensDto1.RefreshToken, accessTokensDto2.RefreshToken);
    }

    [Fact]
    public void Create_ShouldGenerateAccessTokenWithCorrectExpiration()
    {
        CreateTokenDto dto = new()
        {
            UserId = $"u_{Guid.CreateVersion7()}",
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        TokenDto accessTokensDto = _sut.Create(dto);

        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken jwtSecurityToken = handler.ReadJwtToken(accessTokensDto.AccessToken);

        DateTime expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtAuthOptions.ExpirationInMinutes);
        DateTime actualExpiration = jwtSecurityToken.ValidTo;

        Assert.True(Math.Abs((expectedExpiration - actualExpiration).TotalSeconds) < 3);
    }

    [Fact]
    public void Create_ShouldGenerateBase64RefreshToken()
    {
        CreateTokenDto dto = new()
        {
            UserId = $"u_{Guid.CreateVersion7()}",
            Email = "test@example.com",
            Roles = [Roles.Member],
        };

        TokenDto accessTokensDto = _sut.Create(dto);

        Assert.True(IsBase64String(accessTokensDto.RefreshToken));
    }

    private static bool IsBase64String(string base64)
    {
        Span<byte> buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64, buffer, out _);
    }
}
