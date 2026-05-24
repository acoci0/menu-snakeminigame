using Microsoft.EntityFrameworkCore;
using YemekliYilan.Api.Models;

namespace YemekliYilan.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<Score> Scores => Set<Score>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Email)
                .IsRequired();

            entity.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(18);

            entity.Property(x => x.NormalizedUsername)
                .IsRequired()
                .HasMaxLength(18);

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.HasIndex(x => x.NormalizedUsername)
                .IsUnique();
        });

        modelBuilder.Entity<Score>(entity =>
        {
            entity.ToTable("Scores");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Value)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasIndex(x => x.AppUserId)
                .IsUnique();

            entity.HasOne(x => x.AppUser)
                .WithOne(x => x.Score)
                .HasForeignKey<Score>(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}