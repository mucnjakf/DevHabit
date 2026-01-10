using DevHabit.Api.Database;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Services;

public sealed class GitHubPatService(DevHabitDbContext dbContext, EncryptionService encryptionService)
{
    public async Task StoreAsync(
        string userId,
        StoreGitHubPatRequest storeGitHubPatRequest,
        CancellationToken cancellationToken = default)
    {
        GitHubPat? existingGitHubPat = await GetGitHubPatAsync(userId, cancellationToken);

        string encryptedGitHubPat = encryptionService.Encrypt(storeGitHubPatRequest.Pat);

        if (existingGitHubPat is not null)
        {
            existingGitHubPat.Token = encryptedGitHubPat;
            existingGitHubPat.ExpiresAtUtc = DateTime.UtcNow.AddDays(storeGitHubPatRequest.ExpiresInDays);
        }
        else
        {
            await dbContext.GitHubPats.AddAsync(new GitHubPat
            {
                Id = $"gh_{Guid.CreateVersion7()}",
                UserId = userId,
                Token = encryptedGitHubPat,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(storeGitHubPatRequest.ExpiresInDays),
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        GitHubPat? gitHubPat = await GetGitHubPatAsync(userId, cancellationToken);

        if (gitHubPat is null)
        {
            return null;
        }

        string pat = encryptionService.Decrypt(gitHubPat.Token);

        return pat;
    }

    public async Task RevokeAsync(string userId, CancellationToken cancellationToken = default)
    {
        GitHubPat? gitHubPat = await GetGitHubPatAsync(userId, cancellationToken);

        if (gitHubPat is null)
        {
            return;
        }

        dbContext.GitHubPats.Remove(gitHubPat);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GitHubPat?> GetGitHubPatAsync(string userId, CancellationToken cancellationToken)
    {
        return await dbContext.GitHubPats.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }
}
