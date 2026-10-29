using Erp.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.ToTable("Companies", table =>
            table.HasCheckConstraint("CK_Companies_Code_NotBlank", """length(trim("Code")) > 0"""));
    }
}

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ExternalSubject).HasMaxLength(200).IsRequired();
        entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        entity.Property(x => x.Email).HasMaxLength(254);
        entity.Property(x => x.NormalizedEmail).HasMaxLength(254);
        entity.Property(x => x.PasswordHash).HasMaxLength(500);
        entity.Property(x => x.SecurityStamp).HasMaxLength(64).IsRequired().IsConcurrencyToken();
        entity.Property(x => x.AccessFailedCount).IsRequired();
        entity.HasIndex(x => x.ExternalSubject).IsUnique();
        entity.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");
        entity.ToTable("UserAccounts", table =>
        {
            table.HasCheckConstraint("CK_UserAccounts_Subject_NotBlank", """length(trim("ExternalSubject")) > 0""");
            table.HasCheckConstraint(
                "CK_UserAccounts_AccessFailedCount",
                """ "AccessFailedCount" >= 0 """);
        });
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "Roles");
        MappingRules.ConfigureCodeAndName(entity);
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.ToTable("Permissions", table =>
            table.HasCheckConstraint("CK_Permissions_Code_NotBlank", """length(trim("Code")) > 0"""));
    }
}

internal sealed class UserCompanyRoleConfiguration : IEntityTypeConfiguration<UserCompanyRole>
{
    public void Configure(EntityTypeBuilder<UserCompanyRole> entity)
    {
        entity.HasKey(x => new { x.CompanyId, x.UserId, x.RoleId });
        entity.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.User)
            .WithMany(x => x.CompanyRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.RoleId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("UserCompanyRoles");
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> entity)
    {
        entity.HasKey(x => new { x.CompanyId, x.RoleId, x.PermissionId });
        entity.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => new { x.CompanyId, x.RoleId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("RolePermissions");
    }
}

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ActorSubjectSnapshot).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
        entity.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
        entity.Property(x => x.CorrelationId).HasMaxLength(100);
        entity.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
        entity.HasIndex(x => new { x.CompanyId, x.OccurredAt });
        entity.HasIndex(x => new { x.EntityType, x.EntityId });
        entity.HasIndex(x => new { x.ActorUserId, x.OccurredAt });
        entity.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("AuditEvents", table =>
            table.HasCheckConstraint("CK_AuditEvents_Action_NotBlank", """length(trim("Action")) > 0"""));
    }
}

internal sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> entity)
    {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
        entity.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
        entity.HasIndex(x => new { x.CompanyId, x.OccurredAt });
        entity.HasIndex(x => new { x.ActorUserId, x.OccurredAt });
        entity.HasIndex(x => x.TargetUserId);
        entity.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("SecurityAuditEvents", table =>
        {
            table.HasCheckConstraint(
                "CK_SecurityAuditEvents_Action_NotBlank",
                """length(trim("Action")) > 0""");
        });
    }
}
