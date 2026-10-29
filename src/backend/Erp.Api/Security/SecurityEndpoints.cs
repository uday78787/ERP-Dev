using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Erp.Domain.MasterData;
using Erp.Domain.Platform;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Security;

internal static class SecurityEndpoints
{
    private const int MinimumPasswordLength = 12;
    private const int MaximumPasswordLength = 1024;
    private const int MaximumFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly EmailAddressAttribute EmailValidator = new();

    public static void MapSecurityEndpoints(this WebApplication app, IConfiguration configuration)
    {
        var auth = app.MapGroup("/api/v1/auth");
        auth.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { requestToken = tokens.RequestToken });
        }).AllowAnonymous();
        auth.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting("login");
        auth.MapPost("/logout", LogoutAsync).RequireAuthorization();
        auth.MapGet("/me", GetCurrentSessionAsync).RequireAuthorization();

        var admin = app.MapGroup("/api/v1/admin");
        admin.MapGet("/users", ListUsersAsync)
            .RequireAuthorization(PermissionPolicies.For("security.users.read"));
        admin.MapPost("/users", CreateUserAsync)
            .RequireAuthorization(PermissionPolicies.For("security.users.create"));
        admin.MapPut("/users/{userId:guid}/roles", SetUserRolesAsync)
            .RequireAuthorization(PermissionPolicies.For("security.users.update"));
        admin.MapPut("/users/{userId:guid}/active", SetUserActiveAsync)
            .RequireAuthorization(PermissionPolicies.For("security.users.deactivate"));
        admin.MapPost("/users/{userId:guid}/password", ResetUserPasswordAsync)
            .RequireAuthorization(PermissionPolicies.For("security.users.reset_password"));
        admin.MapGet("/roles", ListRolesAsync)
            .RequireAuthorization(PermissionPolicies.For("security.roles.read"));
        admin.MapPost("/roles", CreateRoleAsync)
            .RequireAuthorization(PermissionPolicies.For("security.roles.create"));
        admin.MapPut("/roles/{roleId:guid}/permissions", SetRolePermissionsAsync)
            .RequireAuthorization(PermissionPolicies.For("security.roles.update"));
        admin.MapGet("/permissions", ListPermissionsAsync)
            .RequireAuthorization(PermissionPolicies.For("security.permissions.read"));
        admin.MapGet("/security-audit", ListSecurityAuditAsync)
            .RequireAuthorization(PermissionPolicies.For("security.audit.read"));

        var bootstrapToken = configuration["Security:BootstrapToken"];
        if (!string.IsNullOrWhiteSpace(bootstrapToken))
        {
            if (bootstrapToken.Length < 32)
            {
                throw new InvalidOperationException(
                    "Security:BootstrapToken must contain at least 32 characters.");
            }

            app.MapPost("/api/v1/setup/bootstrap", (HttpContext context, ErpDbContext db,
                IPasswordHasher<UserAccount> passwordHasher, BootstrapRequest request,
                CancellationToken cancellationToken) =>
                BootstrapAsync(context, db, passwordHasher, request, bootstrapToken, cancellationToken))
                .AllowAnonymous()
                .RequireRateLimiting("bootstrap");
        }
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        ErpDbContext db,
        IPasswordHasher<UserAccount> passwordHasher,
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CompanyId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Email)
            || !EmailValidator.IsValid(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || request.Password.Length > MaximumPasswordLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["A valid email, password, and company are required."],
            });
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await db.UserAccounts.SingleOrDefaultAsync(
            account => account.NormalizedEmail == normalizedEmail,
            cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (user is null)
        {
            await RecordFailedLoginAsync(db, request.CompanyId, null, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return InvalidCredentials();
        }

        var isLocked = user.LockoutEnd is { } lockoutEnd && lockoutEnd > now;
        var verification = user.IsActive && !isLocked && user.PasswordHash is not null
            ? passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            : PasswordVerificationResult.Failed;

        if (verification == PasswordVerificationResult.Failed)
        {
            if (user.IsActive && !isLocked)
            {
                user.AccessFailedCount++;
                if (user.AccessFailedCount >= MaximumFailedAttempts)
                {
                    user.LockoutEnd = now.Add(LockoutDuration);
                }
            }

            await RecordFailedLoginAsync(db, request.CompanyId, user.Id, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return InvalidCredentials();
        }

        var hasCompanyAccess = await db.UserCompanyRoles.AnyAsync(membership =>
            membership.UserId == user.Id
            && membership.CompanyId == request.CompanyId
            && membership.User.IsActive
            && membership.Role.IsActive
            && db.Companies.Any(company =>
                company.Id == membership.CompanyId && company.IsActive),
            cancellationToken);
        if (!hasCompanyAccess)
        {
            await RecordFailedLoginAsync(db, request.CompanyId, user.Id, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return InvalidCredentials();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            request.CompanyId, user.Id, user.Id, "auth.login.succeeded"));
        await db.SaveChangesAsync(cancellationToken);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(SecurityClaims.CompanyId, request.CompanyId.ToString()),
            new Claim(SecurityClaims.SecurityStamp, user.SecurityStamp),
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                IssuedUtc = now,
            });

        return Results.Ok(new
        {
            userId = user.Id,
            displayName = user.DisplayName,
            email = user.Email,
            companyId = request.CompanyId,
        });
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(context.User);
        var companyId = GetCompanyId(context.User);
        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            companyId, userId, userId, "auth.logout"));
        await db.SaveChangesAsync(cancellationToken);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentSessionAsync(
        ClaimsPrincipal principal,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var companyId = GetCompanyId(principal);
        var roles = await db.UserCompanyRoles
            .AsNoTracking()
            .Where(membership => membership.UserId == userId
                && membership.CompanyId == companyId
                && membership.Role.IsActive)
            .Select(membership => membership.Role.Name)
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            userId,
            companyId,
            displayName = principal.Identity?.Name,
            email = principal.FindFirstValue(ClaimTypes.Email),
            roles,
        });
    }

    private static async Task<IResult> BootstrapAsync(
        HttpContext context,
        ErpDbContext db,
        IPasswordHasher<UserAccount> passwordHasher,
        BootstrapRequest request,
        string bootstrapToken,
        CancellationToken cancellationToken)
    {
        var suppliedToken = context.Request.Headers["X-ERP-Bootstrap-Token"].ToString();
        if (!TokensMatch(suppliedToken, bootstrapToken))
        {
            await RecordFailedLoginAsync(db, null, null, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Unauthorized();
        }

        if (!IsValidAccountInput(request.AdminEmail, request.AdminDisplayName, request.AdminPassword)
            || !IsValidCode(request.CompanyCode)
            || string.IsNullOrWhiteSpace(request.CompanyName)
            || request.CompanyName.Trim().Length > 160)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bootstrap"] = ["A valid company and administrator account are required."],
            });
        }

        if (await db.UserAccounts.AnyAsync(cancellationToken))
        {
            return Results.Conflict(new { error = "Initial setup has already been completed." });
        }

        var companyCode = request.CompanyCode.Trim().ToUpperInvariant();
        var company = await db.Companies.SingleOrDefaultAsync(
            existing => existing.Code == companyCode,
            cancellationToken);
        if (company is null)
        {
            if (await db.Companies.AnyAsync(cancellationToken))
            {
                return Results.Conflict(new
                {
                    error = "The requested company does not exist in this initialized database.",
                });
            }

            company = new Company
            {
                Id = Guid.NewGuid(),
                Code = companyCode,
                Name = request.CompanyName.Trim(),
            };
            db.Companies.Add(company);
        }

        var normalizedEmail = NormalizeEmail(request.AdminEmail);
        if (await db.UserAccounts.AnyAsync(
                account => account.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return Results.Conflict(new { error = "An account with this email already exists." });
        }

        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            ExternalSubject = $"local:{Guid.NewGuid():N}",
            DisplayName = request.AdminDisplayName.Trim(),
            Email = request.AdminEmail.Trim(),
            NormalizedEmail = normalizedEmail,
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.AdminPassword);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.UserAccounts.Add(account);
        await SecurityCatalog.SeedCompanyAsync(db, company.Id, cancellationToken);
        var administratorRole = await db.Roles.SingleAsync(role =>
            role.CompanyId == company.Id && role.Code == "ADMINISTRATION", cancellationToken);
        db.UserCompanyRoles.Add(new UserCompanyRole
        {
            CompanyId = company.Id,
            UserId = account.Id,
            RoleId = administratorRole.Id,
        });
        db.ItemCodeRules.Add(new ItemCodeRule
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Prefix = "ITEM",
            Separator = "-",
            Padding = 6,
            NextNumber = 1,
        });
        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            company.Id, account.Id, account.Id, "security.bootstrap.completed"));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Created("/api/v1/admin/users", new
        {
            companyId = company.Id,
            administratorUserId = account.Id,
            initialRoleCodes = SecurityCatalog.Roles.Select(role => role.Code),
        });
    }

    private static async Task<IResult> ListUsersAsync(
        ClaimsPrincipal principal,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId(principal);
        var memberships = await db.UserCompanyRoles
            .AsNoTracking()
            .Where(membership => membership.CompanyId == companyId)
            .Select(membership => new
            {
                membership.UserId,
                membership.User.DisplayName,
                membership.User.Email,
                membership.User.IsActive,
                membership.User.CreatedAt,
                RoleId = membership.Role.Id,
                RoleCode = membership.Role.Code,
                RoleName = membership.Role.Name,
            })
            .ToListAsync(cancellationToken);

        var users = memberships.GroupBy(membership => membership.UserId)
            .Select(group => new
            {
                userId = group.Key,
                displayName = group.First().DisplayName,
                email = group.First().Email,
                isActive = group.First().IsActive,
                createdAt = group.First().CreatedAt,
                roles = group.Select(membership => new
                {
                    id = membership.RoleId,
                    code = membership.RoleCode,
                    name = membership.RoleName,
                }).ToArray(),
            });
        return Results.Ok(users);
    }

    private static async Task<IResult> CreateUserAsync(
        ClaimsPrincipal principal,
        ErpDbContext db,
        IPasswordHasher<UserAccount> passwordHasher,
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidAccountInput(request.Email, request.DisplayName, request.Password)
            || request.RoleIds is null
            || request.RoleIds.Count == 0
            || request.RoleIds.Any(roleId => roleId == Guid.Empty))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["user"] = ["Valid account details, a password of at least 12 characters, and one or more roles are required."],
            });
        }

        var companyId = GetCompanyId(principal);
        var normalizedEmail = NormalizeEmail(request.Email);
        if (await db.UserAccounts.AnyAsync(
                account => account.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return Results.Conflict(new { error = "An account with this email already exists." });
        }

        var roleIds = request.RoleIds.Distinct().ToArray();
        var roles = await db.Roles
            .Where(role => role.CompanyId == companyId && role.IsActive && roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Length)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["One or more roles do not exist or are inactive in this company."],
            });
        }

        var callerPermissions = await GetEffectivePermissionCodesAsync(
            db, GetUserId(principal), companyId, cancellationToken);
        var requestedPermissions = await GetRolePermissionCodesAsync(
            db, companyId, roleIds, cancellationToken);
        if (!requestedPermissions.IsSubsetOf(callerPermissions))
        {
            return Results.Forbid();
        }

        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            ExternalSubject = $"local:{Guid.NewGuid():N}",
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.UserAccounts.Add(account);
        foreach (var role in roles)
        {
            db.UserCompanyRoles.Add(new UserCompanyRole
            {
                CompanyId = companyId,
                UserId = account.Id,
                RoleId = role.Id,
            });
        }

        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            companyId, GetUserId(principal), account.Id, "security.user.created",
            new { roleCodes = roles.Select(role => role.Code).ToArray() }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Created($"/api/v1/admin/users/{account.Id}", new
        {
            userId = account.Id,
            account.Email,
            account.DisplayName,
            roleIds,
        });
    }

    private static async Task<IResult> SetUserRolesAsync(
        HttpContext context,
        Guid userId,
        SetUserRolesRequest request,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId(context.User);
        if (request.RoleIds is null
            || request.RoleIds.Count == 0
            || request.RoleIds.Any(roleId => roleId == Guid.Empty))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["Role identifiers must be valid."],
            });
        }

        var target = await db.UserAccounts.SingleOrDefaultAsync(
            user => user.Id == userId
                && user.CompanyRoles.Any(membership => membership.CompanyId == companyId),
            cancellationToken);
        if (target is null)
        {
            return Results.NotFound();
        }

        var roleIds = request.RoleIds.Distinct().ToArray();
        var roles = await db.Roles
            .Where(role => role.CompanyId == companyId && role.IsActive && roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Length)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["One or more roles do not exist or are inactive in this company."],
            });
        }

        var callerPermissions = await GetEffectivePermissionCodesAsync(
            db, GetUserId(context.User), companyId, cancellationToken);
        var requestedPermissions = await GetRolePermissionCodesAsync(
            db, companyId, roleIds, cancellationToken);
        if (!requestedPermissions.IsSubsetOf(callerPermissions))
        {
            return Results.Forbid();
        }

        var currentMemberships = await db.UserCompanyRoles
            .Where(membership => membership.CompanyId == companyId && membership.UserId == userId)
            .ToListAsync(cancellationToken);
        var currentRoleIds = currentMemberships.Select(membership => membership.RoleId).ToHashSet();
        var isCurrentlyAdministrator = await db.Roles.AnyAsync(role =>
            role.CompanyId == companyId
            && role.Code == "ADMINISTRATION"
            && currentRoleIds.Contains(role.Id),
            cancellationToken);
        var willRemainAdministrator = roles.Any(role => role.Code == "ADMINISTRATION");
        var activeAdministratorIds = await GetActiveAdministratorIdsAsync(
            db, companyId, cancellationToken);
        if (target.IsActive
            && isCurrentlyAdministrator
            && !willRemainAdministrator
            && activeAdministratorIds.Count == 1
            && activeAdministratorIds.Contains(userId))
        {
            return Results.Conflict(new
            {
                error = "The company must retain at least one active Administration user.",
            });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var selectedRoleIds = roles.Select(role => role.Id).ToHashSet();
        db.UserCompanyRoles.RemoveRange(currentMemberships
            .Where(membership => !selectedRoleIds.Contains(membership.RoleId)));
        foreach (var role in roles.Where(role => !currentRoleIds.Contains(role.Id)))
        {
            db.UserCompanyRoles.Add(new UserCompanyRole
            {
                CompanyId = companyId,
                UserId = userId,
                RoleId = role.Id,
            });
        }

        target.SecurityStamp = Guid.NewGuid().ToString("N");
        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            companyId, GetUserId(context.User), userId, "security.user.roles_changed",
            new { roleCodes = roles.Select(role => role.Code).ToArray() }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> SetUserActiveAsync(
        HttpContext context,
        Guid userId,
        SetUserActiveRequest request,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId(context.User);
        var target = await db.UserAccounts.SingleOrDefaultAsync(
            user => user.Id == userId
                && user.CompanyRoles.Any(membership => membership.CompanyId == companyId),
            cancellationToken);
        if (target is null)
        {
            return Results.NotFound();
        }

        if (!request.IsActive
            && WouldDeactivateLastAdministrator(
                target,
                await GetActiveAdministratorIdsAsync(db, companyId, cancellationToken)))
        {
            return Results.Conflict(new
            {
                error = "The company must retain at least one active Administration user.",
            });
        }

        target.IsActive = request.IsActive;
        target.SecurityStamp = Guid.NewGuid().ToString("N");
        if (request.IsActive)
        {
            target.AccessFailedCount = 0;
            target.LockoutEnd = null;
        }

        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            companyId, GetUserId(context.User), userId,
            request.IsActive ? "security.user.activated" : "security.user.deactivated"));
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ResetUserPasswordAsync(
        HttpContext context,
        Guid userId,
        ResetPasswordRequest request,
        ErpDbContext db,
        IPasswordHasher<UserAccount> passwordHasher,
        CancellationToken cancellationToken)
    {
        if (!IsValidPassword(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = [$"Password must contain at least {MinimumPasswordLength} characters and no more than {MaximumPasswordLength}."],
            });
        }

        var companyId = GetCompanyId(context.User);
        var target = await db.UserAccounts.SingleOrDefaultAsync(
            user => user.Id == userId
                && user.CompanyRoles.Any(membership => membership.CompanyId == companyId),
            cancellationToken);
        if (target is null)
        {
            return Results.NotFound();
        }

        target.PasswordHash = passwordHasher.HashPassword(target, request.Password);
        target.SecurityStamp = Guid.NewGuid().ToString("N");
        target.AccessFailedCount = 0;
        target.LockoutEnd = null;
        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            companyId, GetUserId(context.User), userId, "security.user.password_reset"));
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListRolesAsync(
        ClaimsPrincipal principal,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId(principal);
        var roles = await db.Roles
            .AsNoTracking()
            .Where(role => role.CompanyId == companyId)
            .OrderBy(role => role.Name)
            .Select(role => new
            {
                role.Id,
                role.Code,
                role.Name,
                role.IsSystemRole,
                role.IsActive,
                Permissions = role.RolePermissions
                    .Select(rolePermission => rolePermission.Permission.Code)
                    .OrderBy(code => code)
                    .ToArray(),
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(roles);
    }

    private static async Task<IResult> CreateRoleAsync(
        HttpContext context,
        CreateRoleRequest request,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        if (!IsValidCode(request.Code)
            || string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 160)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["role"] = ["A role code (2-40 uppercase letters, digits, or underscores) and name are required."],
            });
        }

        var companyId = GetCompanyId(context.User);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Roles.AnyAsync(
                role => role.CompanyId == companyId && role.Code == code,
                cancellationToken))
        {
            return Results.Conflict(new { error = "A role with this code already exists." });
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code,
            Name = request.Name.Trim(),
        };
        db.Roles.Add(role);
        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            companyId, GetUserId(context.User), null, "security.role.created",
            new { roleCode = role.Code }));
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/admin/roles/{role.Id}", new
        {
            role.Id,
            role.Code,
            role.Name,
        });
    }

    private static async Task<IResult> SetRolePermissionsAsync(
        HttpContext context,
        Guid roleId,
        SetRolePermissionsRequest request,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.PermissionIds is null
            || request.PermissionIds.Any(permissionId => permissionId == Guid.Empty))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["permissionIds"] = ["Permission identifiers must be valid."],
            });
        }

        var companyId = GetCompanyId(context.User);
        var role = await db.Roles.SingleOrDefaultAsync(
            existing => existing.Id == roleId && existing.CompanyId == companyId,
            cancellationToken);
        if (role is null)
        {
            return Results.NotFound();
        }

        var permissionIds = request.PermissionIds.Distinct().ToArray();
        var permissions = await db.Permissions
            .Where(permission => permissionIds.Contains(permission.Id))
            .ToListAsync(cancellationToken);
        if (permissions.Count != permissionIds.Length)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["permissionIds"] = ["One or more permissions do not exist."],
            });
        }

        var permissionCodes = permissions.Select(permission => permission.Code)
            .ToHashSet(StringComparer.Ordinal);
        var current = await db.RolePermissions
            .Where(rolePermission => rolePermission.CompanyId == companyId
                && rolePermission.RoleId == roleId)
            .ToListAsync(cancellationToken);
        var currentPermissionCodes = await db.RolePermissions
            .AsNoTracking()
            .Where(rolePermission => rolePermission.CompanyId == companyId
                && rolePermission.RoleId == roleId)
            .Select(rolePermission => rolePermission.Permission.Code)
            .ToHashSetAsync(cancellationToken);
        var callerPermissions = await GetEffectivePermissionCodesAsync(
            db, GetUserId(context.User), companyId, cancellationToken);
        if (!currentPermissionCodes.Union(permissionCodes).All(callerPermissions.Contains))
        {
            return Results.Forbid();
        }

        if (role.Code == "ADMINISTRATION"
            && (!permissionCodes.Contains("security.users.create")
                || !permissionCodes.Contains("security.users.update")
                || !permissionCodes.Contains("security.roles.create")
                || !permissionCodes.Contains("security.roles.update")))
        {
            return Results.Conflict(new
            {
                error = "The Administration role must retain user and role management permissions.",
            });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var selectedPermissionIds = permissions.Select(permission => permission.Id).ToHashSet();
        db.RolePermissions.RemoveRange(current
            .Where(rolePermission => !selectedPermissionIds.Contains(rolePermission.PermissionId)));
        var currentPermissionIds = current.Select(rolePermission => rolePermission.PermissionId).ToHashSet();
        foreach (var permission in permissions.Where(permission =>
                     !currentPermissionIds.Contains(permission.Id)))
        {
            db.RolePermissions.Add(new RolePermission
            {
                CompanyId = companyId,
                RoleId = roleId,
                PermissionId = permission.Id,
            });
        }

        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            companyId, GetUserId(context.User), null, "security.role.permissions_changed",
            new { roleCode = role.Code, permissionCodes = permissionCodes.Order().ToArray() }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListPermissionsAsync(
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Code)
            .Select(permission => new { permission.Id, permission.Code, permission.Name })
            .ToListAsync(cancellationToken);
        return Results.Ok(permissions);
    }

    private static async Task<IResult> ListSecurityAuditAsync(
        ClaimsPrincipal principal,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var companyId = GetCompanyId(principal);
        var events = await db.SecurityAuditEvents
            .AsNoTracking()
            .Where(auditEvent => auditEvent.CompanyId == companyId)
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .Take(100)
            .Select(auditEvent => new
            {
                auditEvent.Id,
                auditEvent.ActorUserId,
                auditEvent.TargetUserId,
                auditEvent.Action,
                auditEvent.DetailsJson,
                auditEvent.OccurredAt,
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(events);
    }

    private static async Task RecordFailedLoginAsync(
        ErpDbContext db,
        Guid? requestedCompanyId,
        Guid? targetUserId,
        CancellationToken cancellationToken)
    {
        var companyId = requestedCompanyId is { } id
            && await db.Companies.AnyAsync(company => company.Id == id, cancellationToken)
                ? requestedCompanyId
                : null;
        db.SecurityAuditEvents.Add(CreateSecurityEvent(
            companyId, null, targetUserId, "auth.login.failed",
            new { result = "denied" }));
    }

    private static async Task<HashSet<string>> GetEffectivePermissionCodesAsync(
        ErpDbContext db,
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken) =>
        await db.UserCompanyRoles
            .AsNoTracking()
            .Where(membership => membership.UserId == userId
                && membership.CompanyId == companyId
                && membership.User.IsActive
                && membership.Role.IsActive)
            .SelectMany(membership => membership.Role.RolePermissions
                .Select(rolePermission => rolePermission.Permission.Code))
            .ToHashSetAsync(cancellationToken);

    private static async Task<HashSet<string>> GetRolePermissionCodesAsync(
        ErpDbContext db,
        Guid companyId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken) =>
        await db.RolePermissions
            .AsNoTracking()
            .Where(rolePermission => rolePermission.CompanyId == companyId
                && roleIds.Contains(rolePermission.RoleId))
            .Select(rolePermission => rolePermission.Permission.Code)
            .ToHashSetAsync(cancellationToken);

    private static async Task<HashSet<Guid>> GetActiveAdministratorIdsAsync(
        ErpDbContext db,
        Guid companyId,
        CancellationToken cancellationToken) =>
        await db.UserCompanyRoles
            .AsNoTracking()
            .Where(membership => membership.CompanyId == companyId
                && membership.User.IsActive
                && membership.Role.IsActive
                && membership.Role.Code == "ADMINISTRATION")
            .Select(membership => membership.UserId)
            .ToHashSetAsync(cancellationToken);

    private static bool WouldDeactivateLastAdministrator(
        UserAccount target,
        IReadOnlySet<Guid> activeAdministrators) =>
        activeAdministrators.Count == 1 && activeAdministrators.Contains(target.Id);

    private static SecurityAuditEvent CreateSecurityEvent(
        Guid? companyId,
        Guid? actorUserId,
        Guid? targetUserId,
        string action,
        object? details = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = action,
            DetailsJson = JsonSerializer.Serialize(details ?? new { }),
        };

    private static IResult InvalidCredentials() =>
        Results.Unauthorized();

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static Guid GetCompanyId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(SecurityClaims.CompanyId)!);

    private static bool IsValidAccountInput(string email, string displayName, string password) =>
        !string.IsNullOrWhiteSpace(email)
        && EmailValidator.IsValid(email)
        && !string.IsNullOrWhiteSpace(displayName)
        && displayName.Trim().Length <= 160
        && IsValidPassword(password);

    private static bool IsValidPassword(string password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= MinimumPasswordLength
        && password.Length <= MaximumPasswordLength;

    private static bool IsValidCode(string code) =>
        !string.IsNullOrWhiteSpace(code)
        &&
        code.Trim().Length is >= 2 and <= 40
        && code.Trim().All(character =>
            character is >= 'A' and <= 'Z'
            or >= '0' and <= '9'
            or '_');

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static bool TokensMatch(string supplied, string expected)
    {
        var suppliedBytes = System.Text.Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                suppliedBytes, expectedBytes);
    }

    private sealed record LoginRequest(string Email, string Password, Guid CompanyId);
    private sealed record BootstrapRequest(
        string CompanyCode,
        string CompanyName,
        string AdminEmail,
        string AdminDisplayName,
        string AdminPassword);
    private sealed record CreateUserRequest(
        string Email,
        string DisplayName,
        string Password,
        IReadOnlyList<Guid>? RoleIds);
    private sealed record SetUserRolesRequest(IReadOnlyList<Guid>? RoleIds);
    private sealed record SetUserActiveRequest(bool IsActive);
    private sealed record ResetPasswordRequest(string Password);
    private sealed record CreateRoleRequest(string Code, string Name);
    private sealed record SetRolePermissionsRequest(IReadOnlyList<Guid>? PermissionIds);
}
