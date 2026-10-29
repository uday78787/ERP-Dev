namespace Erp.Domain.Platform;

public sealed class Company
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public string ExternalSubject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? PasswordHash { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<UserCompanyRole> CompanyRoles { get; set; } = [];
}

public sealed class Role
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public sealed class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class UserCompanyRole
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public UserAccount User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public sealed class RolePermission
{
    public Guid CompanyId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

public sealed class AuditEvent
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorSubjectSnapshot { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public string DetailsJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SecurityAuditEvent
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string DetailsJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
