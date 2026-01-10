using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Api.Database.Configurations;

public sealed class GitHubPatTypeConfiguration : IEntityTypeConfiguration<GitHubPat>
{
    public void Configure(EntityTypeBuilder<GitHubPat> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(500);

        builder.Property(x => x.Token).HasMaxLength(1000);

        builder.Property(x => x.UserId).HasMaxLength(500);

        builder.HasIndex(x => x.UserId).IsUnique();

        builder
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<GitHubPat>(x => x.UserId);
    }
}
