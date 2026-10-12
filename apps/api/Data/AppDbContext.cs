using FlagForge.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectEnvironment> Environments => Set<ProjectEnvironment>();
    public DbSet<SdkKey> SdkKeys => Set<SdkKey>();
    public DbSet<Flag> Flags => Set<Flag>();
    public DbSet<Variation> Variations => Set<Variation>();
    public DbSet<FlagEnvironmentConfig> FlagConfigs => Set<FlagEnvironmentConfig>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(e =>
        {
            e.ToTable("admin_users");
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.Property(x => x.Key).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<ProjectEnvironment>(e =>
        {
            e.ToTable("environments");
            e.Property(x => x.Key).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique();
            e.HasOne(x => x.Project).WithMany(p => p.Environments).HasForeignKey(x => x.ProjectId);
        });

        modelBuilder.Entity<SdkKey>(e =>
        {
            e.ToTable("sdk_keys");
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.KeyPrefix).HasMaxLength(16);
            e.Property(x => x.KeyHash).HasMaxLength(64);
            e.HasIndex(x => x.KeyHash).IsUnique();
            e.HasOne(x => x.Environment).WithMany(env => env.SdkKeys).HasForeignKey(x => x.EnvironmentId);
        });

        modelBuilder.Entity<Flag>(e =>
        {
            e.ToTable("flags");
            e.Property(x => x.Key).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique();
            e.HasOne(x => x.Project).WithMany(p => p.Flags).HasForeignKey(x => x.ProjectId);
        });

        modelBuilder.Entity<Variation>(e =>
        {
            e.ToTable("variations");
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.ValueJson).HasColumnType("jsonb");
            e.HasOne<Flag>().WithMany(f => f.Variations).HasForeignKey(x => x.FlagId);
        });

        modelBuilder.Entity<FlagEnvironmentConfig>(e =>
        {
            e.ToTable("flag_environment_configs");
            e.Property(x => x.FallthroughJson).HasColumnType("jsonb");
            e.Property(x => x.RulesJson).HasColumnType("jsonb");
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.FlagId, x.EnvironmentId }).IsUnique();
            e.HasOne(x => x.Flag).WithMany(f => f.Configs).HasForeignKey(x => x.FlagId);
            e.HasOne(x => x.Environment).WithMany().HasForeignKey(x => x.EnvironmentId);
        });

        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.ToTable("audit_entries");
            e.Property(x => x.ActorEmail).HasMaxLength(256);
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.ResourceType).HasMaxLength(32);
            e.Property(x => x.ResourceKey).HasMaxLength(128);
            e.Property(x => x.EnvironmentKey).HasMaxLength(64);
            e.Property(x => x.BeforeJson).HasColumnType("jsonb");
            e.Property(x => x.AfterJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.ProjectId, x.Id });
        });
    }
}
