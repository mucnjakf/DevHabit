using System.Security.Cryptography;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Entities;
using DevHabit.Api.Options;
using DevHabit.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevHabit.UnitTests.Services;

public sealed class GitHubAccessTokenServiceTests : IDisposable
{
    private readonly GitHubPatService _sut;
    private readonly DevHabitDbContext _dbContext;
    private readonly EncryptionService _encryptionService;

    public GitHubAccessTokenServiceTests()
    {
        DbContextOptions<DevHabitDbContext> dbContextOptions = new DbContextOptionsBuilder<DevHabitDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DevHabitDbContext(dbContextOptions);

        IOptions<EncryptionOptions> encryptionOptions = Options.Create(new EncryptionOptions()
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        });

        _encryptionService = new EncryptionService(encryptionOptions);

        _sut = new GitHubPatService(_dbContext, _encryptionService);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task StoreAsync_ShouldCreateNewToken_WhenUserDoesNotHaveOne()
    {
        string userId = $"u_{Guid.CreateVersion7()}";

        StoreGitHubPatRequest dto = new()
        {
            Pat = "github-token",
            ExpiresInDays = 30,
        };

        await _sut.StoreAsync(userId, dto);

        GitHubPat? token = await _dbContext.GitHubPats.FirstOrDefaultAsync(x => x.UserId == userId);

        Assert.NotNull(token);
        Assert.Equal(userId, token.UserId);
        Assert.NotEqual(dto.Pat, token.Token);
        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task StoreAsync_ShouldUpdateExistingToken_WhenUserHaveOne()
    {
        string userId = $"u_{Guid.CreateVersion7()}";

        GitHubPat existingToken = new()
        {
            Id = $"gh_{Guid.CreateVersion7()}",
            UserId = userId,
            Token = "github-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
        };

        _dbContext.GitHubPats.Add(existingToken);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        StoreGitHubPatRequest dto = new()
        {
            Pat = "new-github-token",
            ExpiresInDays = 30,
        };

        await _sut.StoreAsync(userId, dto);

        GitHubPat? token = await _dbContext.GitHubPats.FirstOrDefaultAsync(x => x.UserId == userId);

        Assert.NotNull(token);
        Assert.Equal(existingToken.Id, token.Id);
        Assert.Equal(existingToken.UserId, token.UserId);
        Assert.NotEqual(existingToken.Token, token.Token);
        Assert.True(token.ExpiresAtUtc > existingToken.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDecryptedToken_WhenTokenExists()
    {
        string userId = $"u_{Guid.CreateVersion7()}";
        string originalToken = "github-token";

        GitHubPat existingToken = new()
        {
            Id = $"gh_{Guid.CreateVersion7()}",
            UserId = userId,
            Token = _encryptionService.Encrypt(originalToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
        };

        _dbContext.GitHubPats.Add(existingToken);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        string? token = await _sut.GetAsync(userId);

        Assert.NotNull(token);
        Assert.Equal(originalToken, token);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenTokenDoesNotExist()
    {
        string userId = $"u_{Guid.CreateVersion7()}";

        string? token = await _sut.GetAsync(userId);

        Assert.Null(token);
    }

    [Fact]
    public async Task RevokeAsync_ShouldRemoveToken_WhenTokenExists()
    {
        string userId = $"u_{Guid.CreateVersion7()}";

        GitHubPat existingToken = new()
        {
            Id = $"gh_{Guid.CreateVersion7()}",
            UserId = userId,
            Token = "github-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(29),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
        };

        _dbContext.GitHubPats.Add(existingToken);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        await _sut.RevokeAsync(userId);

        bool tokenExists = await _dbContext.GitHubPats.AnyAsync(x => x.UserId == userId);
        Assert.False(tokenExists);
    }

    [Fact]
    public async Task RevokeAsync_ShouldNotThrow_WhenTokenDoesNotExist()
    {
        string userId = $"u_{Guid.CreateVersion7()}";

        await _sut.RevokeAsync(userId);

        bool tokenExists = await _dbContext.GitHubPats.AnyAsync(x => x.UserId == userId);
        Assert.False(tokenExists);
    }
}
