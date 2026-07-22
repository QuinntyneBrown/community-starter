using CommunityStarter.Domain.Communities;
using CommunityStarter.Domain.Content;
using CommunityStarter.Domain.Identity;
using CommunityStarter.Domain.Moderation;
using CommunityStarter.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace CommunityStarter.Infrastructure.Persistence;

public sealed class CommunityDbContext(DbContextOptions<CommunityDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ActionSecret> ActionSecrets => Set<ActionSecret>();
    public DbSet<AccountSession> Sessions => Set<AccountSession>();
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<CommunityInvitation> Invitations => Set<CommunityInvitation>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ModerationCase> ModerationCases => Set<ModerationCase>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<DurableJob> Jobs => Set<DurableJob>();
    public DbSet<FeatureStateRow> FeatureStates => Set<FeatureStateRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("community");

        modelBuilder.Entity<Account>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => value.EmailNormalized).IsUnique();
            entity.Property(value => value.EmailNormalized).HasMaxLength(320);
            entity.Property(value => value.EmailDisplay).HasMaxLength(320);
            entity.Property(value => value.PasswordHash).HasMaxLength(1_024);
            entity.Property(value => value.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(value => value.Locale).HasMaxLength(35);
            entity.Property(value => value.TimeZone).HasMaxLength(100);
        });
        modelBuilder.Entity<ActionSecret>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.Purpose, value.VerifierHash }).IsUnique();
            entity.Property(value => value.Purpose).HasMaxLength(80);
            entity.Property(value => value.VerifierHash).HasMaxLength(128);
            entity.HasOne<Account>().WithMany().HasForeignKey(value => value.AccountId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AccountSession>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => value.TokenHash).IsUnique();
            entity.HasIndex(value => new { value.AccountId, value.FamilyId });
            entity.Property(value => value.TokenHash).HasMaxLength(128);
            entity.Property(value => value.DeviceLabel).HasMaxLength(120);
            entity.HasOne<Account>().WithMany().HasForeignKey(value => value.AccountId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Community>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => value.Slug).IsUnique();
            entity.Property(value => value.Slug).HasMaxLength(64);
            entity.Property(value => value.Name).HasMaxLength(120);
            entity.Property(value => value.Description).HasMaxLength(2_000);
            entity.Property(value => value.AccessMode).HasConversion<string>().HasMaxLength(40);
        });
        modelBuilder.Entity<Membership>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.CommunityId, value.AccountId }).IsUnique();
            entity.Property(value => value.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(value => value.Role).HasConversion<string>().HasMaxLength(40);
            entity.HasOne<Community>().WithMany().HasForeignKey(value => value.CommunityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(value => value.AccountId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CommunityInvitation>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => value.TokenHash).IsUnique();
            entity.HasIndex(value => new { value.CommunityId, value.EmailNormalized });
            entity.Property(value => value.EmailNormalized).HasMaxLength(320);
            entity.Property(value => value.TokenHash).HasMaxLength(128);
            entity.HasOne<Community>().WithMany().HasForeignKey(value => value.CommunityId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Post>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.CommunityId, value.PublishedAt, value.Id });
            entity.Property(value => value.Body).HasMaxLength(20_000);
            entity.Property(value => value.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasOne<Community>().WithMany().HasForeignKey(value => value.CommunityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(value => value.AuthorAccountId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Reaction>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.PostId, value.AccountId, value.Kind }).IsUnique();
            entity.Property(value => value.Kind).HasMaxLength(40);
            entity.HasOne<Post>().WithMany().HasForeignKey(value => value.PostId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Report>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.CommunityId, value.TargetType, value.TargetId });
            entity.Property(value => value.TargetType).HasMaxLength(80);
            entity.Property(value => value.Reason).HasMaxLength(2_000);
        });
        modelBuilder.Entity<ModerationCase>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => value.ReportId).IsUnique();
            entity.Property(value => value.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasOne<Report>().WithMany().HasForeignKey(value => value.ReportId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ModerationAction>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.CommunityId, value.TargetType, value.TargetId });
            entity.Property(value => value.TargetType).HasMaxLength(80);
            entity.Property(value => value.Kind).HasMaxLength(80);
            entity.Property(value => value.Rationale).HasMaxLength(4_000);
            entity.HasOne<ModerationCase>().WithMany().HasForeignKey(value => value.CaseId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.CommunityId, value.CreatedAt });
            entity.Property(value => value.Kind).HasMaxLength(120);
            entity.Property(value => value.TargetType).HasMaxLength(80);
            entity.Property(value => value.SafeDetailsJson).HasColumnType("jsonb");
            entity.Property(value => value.CorrelationId).HasMaxLength(64);
        });
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.ProcessedAt, value.CreatedAt });
            entity.Property(value => value.Kind).HasMaxLength(120);
            entity.Property(value => value.PayloadJson).HasColumnType("jsonb");
        });
        modelBuilder.Entity<DurableJob>(entity =>
        {
            ConfigureEntity(entity);
            entity.HasIndex(value => new { value.Status, value.AvailableAt });
            entity.Property(value => value.Kind).HasMaxLength(120);
            entity.Property(value => value.PayloadJson).HasColumnType("jsonb");
            entity.Property(value => value.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(value => value.LeaseOwner).HasMaxLength(200);
            entity.Property(value => value.SafeError).HasMaxLength(1_000);
        });
        modelBuilder.Entity<FeatureStateRow>(entity =>
        {
            entity.ToTable("feature_states");
            entity.HasKey(value => value.Id);
            entity.HasIndex(value => new { value.FeatureSlug, value.SubjectId, value.CommunityId }).IsUnique();
            entity.Property(value => value.FeatureSlug).HasMaxLength(120);
            entity.Property(value => value.State).HasMaxLength(120);
            entity.Property(value => value.Version).IsConcurrencyToken();
        });
    }

    private static void ConfigureEntity<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : Domain.Common.Entity
    {
        entity.HasKey(value => value.Id);
        entity.Property(value => value.Version).IsConcurrencyToken();
    }
}

public sealed class FeatureStateRow
{
    public Guid Id { get; set; }
    public string FeatureSlug { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public Guid? CommunityId { get; set; }
    public string State { get; set; } = "initial";
    public long Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

