using Analyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Data;

public class AnalyzerDbContext(DbContextOptions<AnalyzerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<ITSystem> ITSystems => Set<ITSystem>();
    public DbSet<Avatar> Avatars => Set<Avatar>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.Role).HasConversion<int>();

            builder.HasOne<Avatar>()
               .WithMany()
               .HasForeignKey(u => u.AvatarId)
               .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne<Team>()
                   .WithMany()
                   .HasForeignKey(u => u.TeamId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Avatar>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.HasIndex(a => new { a.UserId, a.Hash })
                .IsUnique();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<Team>(builder =>
        {
            builder.HasKey(t => t.Id);

            builder.Ignore(t => t.MemberIds);
            builder.Ignore("_memberIds");
        });

        modelBuilder.Entity<Invite>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.HasIndex(i => i.Code).IsUnique();

            builder.Property(i => i.Status).HasConversion<int>();
            builder.Property(i => i.Role).HasConversion<int>();

            builder.HasOne<Team>()
                   .WithMany()
                   .HasForeignKey(i => i.TeamId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(i => i.ActivatedByUserId)
                   .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ITSystem>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.HasIndex(s => new { s.TeamId, s.Name }).IsUnique();

            builder.HasOne<Team>()
                   .WithMany()
                   .HasForeignKey(s => s.TeamId)
                   .OnDelete(DeleteBehavior.Cascade);
        });
    }
}