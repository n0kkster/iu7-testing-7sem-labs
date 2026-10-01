namespace Analyzer.Infrastructure.Data;

using Analyzer.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class AnalyzerDbContext(DbContextOptions<AnalyzerDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();
    public DbSet<InviteEntity> Invites => Set<InviteEntity>();
    public DbSet<ITSystemEntity> ITSystems => Set<ITSystemEntity>();
    public DbSet<AvatarEntity> Avatars => Set<AvatarEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();

            builder.HasOne(u => u.Avatar)
                   .WithMany()
                   .HasForeignKey(u => u.AvatarId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(u => u.Team)
                   .WithMany(t => t.Members)
                   .HasForeignKey(u => u.TeamId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AvatarEntity>(builder =>
        {
            builder.ToTable("avatars");
            builder.HasKey(a => a.Id);
            builder.HasIndex(a => new { a.UserId, a.Hash }).IsUnique();

            builder.HasOne(a => a.User)
                   .WithMany()
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<TeamEntity>(builder =>
        {
            builder.ToTable("teams");
            builder.HasKey(t => t.Id);
        });

        modelBuilder.Entity<InviteEntity>(builder =>
        {
            builder.ToTable("invites");
            builder.HasKey(i => i.Id);
            builder.HasIndex(i => i.Code).IsUnique();

            builder.HasOne(i => i.Team)
                   .WithMany(t => t.Invites)
                   .HasForeignKey(i => i.TeamId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.ActivatedByUser)
                   .WithMany(u => u.ActivatedInvites)
                   .HasForeignKey(i => i.ActivatedByUserId)
                   .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ITSystemEntity>(builder =>
        {
            builder.ToTable("it_systems");
            builder.HasKey(s => s.Id);
            builder.HasIndex(s => new { s.TeamId, s.Name }).IsUnique();

            builder.HasOne(s => s.Team)
                   .WithMany(t => t.Systems)
                   .HasForeignKey(s => s.TeamId)
                   .OnDelete(DeleteBehavior.Cascade);
        });
    }
}