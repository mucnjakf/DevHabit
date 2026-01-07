using DevHabit.Api.Database;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Services;

public sealed class GitHubAccessTokenService(ApplicationDbContext dbContext, EncryptionService encryptionService)
{
    public async Task StoreAsync(
        string userId,
        StoreGitHubAccessTokenDto accessTokenDto,
        CancellationToken cancellationToken = default)
    {
        GitHubAccessToken? existingAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

        string encryptedAccessToken = encryptionService.Encrypt(accessTokenDto.AccessToken);

        if (existingAccessToken is not null)
        {
            existingAccessToken.Token = encryptedAccessToken;
            existingAccessToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays);
        }
        else
        {
            await dbContext.GitHubAccessTokens.AddAsync(new GitHubAccessToken
            {
                Id = $"gh_{Guid.CreateVersion7()}",
                UserId = userId,
                Token = encryptedAccessToken,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays),
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        GitHubAccessToken? accessToken = await GetAccessTokenAsync(userId, cancellationToken);

        return accessToken is null ? null : encryptionService.Decrypt(accessToken.Token);
    }

    public async Task RevokeAsync(string userId, CancellationToken cancellationToken = default)
    {
        GitHubAccessToken? accessToken = await GetAccessTokenAsync(userId, cancellationToken);

        if (accessToken is null)
        {
            return;
        }

        dbContext.GitHubAccessTokens.Remove(accessToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GitHubAccessToken?> GetAccessTokenAsync(string userId, CancellationToken cancellationToken)
    {
        return await dbContext.GitHubAccessTokens.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }
}
