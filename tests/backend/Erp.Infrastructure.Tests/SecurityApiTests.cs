using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Erp.Domain.Platform;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Infrastructure.Tests;

public sealed class SecurityApiTests : IAsyncLifetime
{
    private const string BootstrapToken = "test-bootstrap-token-with-at-least-32-characters";
    private const string AdministratorEmail = "admin@example.test";
    private const string AdministratorPassword = "Manufacturing-admin-Password-01!";

    private readonly ErpApiFactory factory = new();
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        await factory.InitializeDatabaseAsync();
        client = CreateClient();
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
    }

    [Fact]
    public async Task ProtectedEndpointRejectsUnauthenticatedRequest()
    {
        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BootstrapAndLoginCreateHashedAccountAndSecureSession()
    {
        var companyId = await BootstrapAsync(client);
        var hash = await GetPasswordHashAsync();

        Assert.NotEqual(AdministratorPassword, hash);
        Assert.StartsWith("AQAAAA", hash, StringComparison.Ordinal);

        var login = await LoginAsync(client, AdministratorEmail, AdministratorPassword, companyId);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var sessionCookie = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        Assert.Contains("__Host-erp-session=", sessionCookie, StringComparison.Ordinal);
        Assert.Contains("secure", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", sessionCookie, StringComparison.OrdinalIgnoreCase);

        var currentSession = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, currentSession.StatusCode);

        var users = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);

        var logout = await PostWithCsrfAsync(client, "/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task ModuleRoleCanAccessSessionButCannotAccessAdministrationActions()
    {
        var companyId = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(client, AdministratorEmail, AdministratorPassword, companyId)).StatusCode);

        using var rolesResponse = await client.GetAsync("/api/v1/admin/roles");
        rolesResponse.EnsureSuccessStatusCode();
        using var rolesJson = JsonDocument.Parse(await rolesResponse.Content.ReadAsStringAsync());
        var salesRoleId = rolesJson.RootElement.EnumerateArray()
            .Single(role => role.GetProperty("code").GetString() == "SALES")
            .GetProperty("id")
            .GetGuid();

        using var createResponse = await PostWithCsrfAsync(client, "/api/v1/admin/users", new
        {
            email = "sales@example.test",
            displayName = "Sales User",
            password = "Sales-user-Password-02!",
            roleIds = new[] { salesRoleId },
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var salesClient = CreateClient();
        var salesLogin = await LoginAsync(
            salesClient, "sales@example.test", "Sales-user-Password-02!", companyId);
        Assert.Equal(HttpStatusCode.OK, salesLogin.StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await salesClient.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await salesClient.GetAsync("/api/v1/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await salesClient.GetAsync("/api/v1/admin/permissions")).StatusCode);
    }

    [Fact]
    public async Task MutatingCookieAuthenticatedEndpointRequiresAntiForgeryToken()
    {
        var companyId = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(client, AdministratorEmail, AdministratorPassword, companyId)).StatusCode);

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task ConfigurableRoleNeedsBothModuleAndActionPermissions()
    {
        var companyId = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(client, AdministratorEmail, AdministratorPassword, companyId)).StatusCode);

        using var roleResponse = await PostWithCsrfAsync(client, "/api/v1/admin/roles", new
        {
            code = "USER_VIEWER",
            name = "User Viewer",
        });
        Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
        using var roleJson = JsonDocument.Parse(await roleResponse.Content.ReadAsStringAsync());
        var roleId = roleJson.RootElement.GetProperty("id").GetGuid();

        using var permissionResponse = await client.GetAsync("/api/v1/admin/permissions");
        permissionResponse.EnsureSuccessStatusCode();
        using var permissionJson = JsonDocument.Parse(await permissionResponse.Content.ReadAsStringAsync());
        var permissionIds = permissionJson.RootElement.EnumerateArray()
            .ToDictionary(
                permission => permission.GetProperty("code").GetString()!,
                permission => permission.GetProperty("id").GetGuid(),
                StringComparer.Ordinal);
        var usersAccessId = permissionIds["security.users.access"];
        var usersReadId = permissionIds["security.users.read"];

        using var createUserResponse = await PostWithCsrfAsync(client, "/api/v1/admin/users", new
        {
            email = "viewer@example.test",
            displayName = "User Viewer",
            password = "User-viewer-Password-03!",
            roleIds = new[] { roleId },
        });
        Assert.Equal(HttpStatusCode.Created, createUserResponse.StatusCode);

        using var auditorClient = CreateClient();
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(auditorClient, "viewer@example.test",
                "User-viewer-Password-03!", companyId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await auditorClient.GetAsync("/api/v1/admin/users")).StatusCode);

        using var actionOnlyResponse = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/roles/{roleId}/permissions",
            new { permissionIds = new[] { usersReadId } });
        Assert.Equal(HttpStatusCode.NoContent, actionOnlyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await auditorClient.GetAsync("/api/v1/admin/users")).StatusCode);

        using var moduleAndActionResponse = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/roles/{roleId}/permissions",
            new { permissionIds = new[] { usersAccessId, usersReadId } });
        Assert.Equal(HttpStatusCode.NoContent, moduleAndActionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await auditorClient.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    [Fact]
    public async Task RoleAdministrationCannotGrantPermissionsCallerDoesNotHave()
    {
        var companyId = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(client, AdministratorEmail, AdministratorPassword, companyId)).StatusCode);

        using var rolesResponse = await client.GetAsync("/api/v1/admin/roles");
        rolesResponse.EnsureSuccessStatusCode();
        using var rolesJson = JsonDocument.Parse(await rolesResponse.Content.ReadAsStringAsync());
        var roles = rolesJson.RootElement.EnumerateArray()
            .ToDictionary(
                role => role.GetProperty("code").GetString()!,
                role => role.GetProperty("id").GetGuid(),
                StringComparer.Ordinal);

        using var permissionsResponse = await client.GetAsync("/api/v1/admin/permissions");
        permissionsResponse.EnsureSuccessStatusCode();
        using var permissionsJson = JsonDocument.Parse(await permissionsResponse.Content.ReadAsStringAsync());
        var permissions = permissionsJson.RootElement.EnumerateArray()
            .ToDictionary(
                permission => permission.GetProperty("code").GetString()!,
                permission => permission.GetProperty("id").GetGuid(),
                StringComparer.Ordinal);

        using var managerRoleResponse = await PostWithCsrfAsync(client, "/api/v1/admin/roles", new
        {
            code = "LIMITED_USER_MANAGER",
            name = "Limited User Manager",
        });
        Assert.Equal(HttpStatusCode.Created, managerRoleResponse.StatusCode);
        using var managerRoleJson = JsonDocument.Parse(
            await managerRoleResponse.Content.ReadAsStringAsync());
        var managerRoleId = managerRoleJson.RootElement.GetProperty("id").GetGuid();
        var managerPermissionCodes = new[]
        {
            "security.users.access",
            "security.users.create",
            "security.users.update",
            "security.roles.access",
            "security.roles.update",
        };
        using var grantManagerPermissions = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/roles/{managerRoleId}/permissions",
            new { permissionIds = managerPermissionCodes.Select(code => permissions[code]).ToArray() });
        Assert.Equal(HttpStatusCode.NoContent, grantManagerPermissions.StatusCode);

        using var limitedRoleResponse = await PostWithCsrfAsync(client, "/api/v1/admin/roles", new
        {
            code = "LIMITED_USER",
            name = "Limited User",
        });
        Assert.Equal(HttpStatusCode.Created, limitedRoleResponse.StatusCode);
        using var limitedRoleJson = JsonDocument.Parse(
            await limitedRoleResponse.Content.ReadAsStringAsync());
        var limitedRoleId = limitedRoleJson.RootElement.GetProperty("id").GetGuid();

        using var limitedRolePermissions = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/roles/{limitedRoleId}/permissions",
            new { permissionIds = new[] { permissions["security.users.access"] } });
        Assert.Equal(HttpStatusCode.NoContent, limitedRolePermissions.StatusCode);

        using var managerUserResponse = await PostWithCsrfAsync(client, "/api/v1/admin/users", new
        {
            email = "manager@example.test",
            displayName = "Limited User Manager",
            password = "Manager-user-Password-04!",
            roleIds = new[] { managerRoleId },
        });
        Assert.Equal(HttpStatusCode.Created, managerUserResponse.StatusCode);

        using var targetUserResponse = await PostWithCsrfAsync(client, "/api/v1/admin/users", new
        {
            email = "limited@example.test",
            displayName = "Limited User",
            password = "Limited-user-Password-05!",
            roleIds = new[] { limitedRoleId },
        });
        Assert.Equal(HttpStatusCode.Created, targetUserResponse.StatusCode);
        using var targetUserJson = JsonDocument.Parse(
            await targetUserResponse.Content.ReadAsStringAsync());
        var targetUserId = targetUserJson.RootElement.GetProperty("userId").GetGuid();

        using var managerClient = CreateClient();
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(managerClient, "manager@example.test",
                "Manager-user-Password-04!", companyId)).StatusCode);

        using var allowedCreate = await PostWithCsrfAsync(managerClient, "/api/v1/admin/users", new
        {
            email = "delegated@example.test",
            displayName = "Delegated User",
            password = "Delegated-user-Password-06!",
            roleIds = new[] { limitedRoleId },
        });
        Assert.Equal(HttpStatusCode.Created, allowedCreate.StatusCode);

        using var forbiddenCreate = await PostWithCsrfAsync(managerClient, "/api/v1/admin/users", new
        {
            email = "escalated@example.test",
            displayName = "Escalated User",
            password = "Escalated-user-Password-07!",
            roleIds = new[] { roles["ADMINISTRATION"] },
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);

        using var forbiddenRoleChange = await PutWithCsrfAsync(
            managerClient,
            $"/api/v1/admin/users/{targetUserId}/roles",
            new { roleIds = new[] { roles["ADMINISTRATION"] } });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenRoleChange.StatusCode);

        using var forbiddenPermissionChange = await PutWithCsrfAsync(
            managerClient,
            $"/api/v1/admin/roles/{managerRoleId}/permissions",
            new
            {
                permissionIds = managerPermissionCodes
                    .Append("security.users.deactivate")
                    .Select(code => permissions[code])
                    .ToArray(),
            });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPermissionChange.StatusCode);
    }

    [Fact]
    public async Task AdministratorPasswordAndActivationChangesRevokeExistingSessions()
    {
        var companyId = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(client, AdministratorEmail, AdministratorPassword, companyId)).StatusCode);

        using var rolesResponse = await client.GetAsync("/api/v1/admin/roles");
        rolesResponse.EnsureSuccessStatusCode();
        using var rolesJson = JsonDocument.Parse(await rolesResponse.Content.ReadAsStringAsync());
        var salesRoleId = rolesJson.RootElement.EnumerateArray()
            .Single(role => role.GetProperty("code").GetString() == "SALES")
            .GetProperty("id")
            .GetGuid();

        using var createUserResponse = await PostWithCsrfAsync(client, "/api/v1/admin/users", new
        {
            email = "managed-user@example.test",
            displayName = "Managed User",
            password = "Managed-user-Password-09!",
            roleIds = new[] { salesRoleId },
        });
        Assert.Equal(HttpStatusCode.Created, createUserResponse.StatusCode);
        using var userJson = JsonDocument.Parse(await createUserResponse.Content.ReadAsStringAsync());
        var userId = userJson.RootElement.GetProperty("userId").GetGuid();

        using var userClient = CreateClient();
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(userClient, "managed-user@example.test",
                "Managed-user-Password-09!", companyId)).StatusCode);

        const string resetPassword = "Managed-user-Reset-Password-10!";
        using var resetResponse = await PostWithCsrfAsync(
            client,
            $"/api/v1/admin/users/{userId}/password",
            new { password = resetPassword });
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await userClient.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(userClient, "managed-user@example.test",
                resetPassword, companyId)).StatusCode);

        using var deactivateResponse = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/users/{userId}/active",
            new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await userClient.GetAsync("/api/v1/auth/me")).StatusCode);

        using var activateResponse = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/users/{userId}/active",
            new { isActive = true });
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(userClient, "managed-user@example.test",
                resetPassword, companyId)).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var auditActions = await db.SecurityAuditEvents
            .Where(auditEvent => auditEvent.TargetUserId == userId)
            .Select(auditEvent => auditEvent.Action)
            .ToListAsync();
        Assert.Contains("security.user.password_reset", auditActions);
        Assert.Contains("security.user.deactivated", auditActions);
        Assert.Contains("security.user.activated", auditActions);
    }

    [Fact]
    public async Task LastAdministratorCannotBeDeactivatedOrRemovedFromRole()
    {
        var companyId = await BootstrapAsync(client);
        Assert.Equal(HttpStatusCode.OK,
            (await LoginAsync(client, AdministratorEmail, AdministratorPassword, companyId)).StatusCode);

        using var rolesResponse = await client.GetAsync("/api/v1/admin/roles");
        rolesResponse.EnsureSuccessStatusCode();
        using var rolesJson = JsonDocument.Parse(await rolesResponse.Content.ReadAsStringAsync());
        var roles = rolesJson.RootElement.EnumerateArray()
            .ToDictionary(
                role => role.GetProperty("code").GetString()!,
                role => role.GetProperty("id").GetGuid(),
                StringComparer.Ordinal);
        var administratorUserId = await GetAdministratorUserIdAsync();

        using var roleChangeResponse = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/users/{administratorUserId}/roles",
            new { roleIds = new[] { roles["SALES"] } });
        Assert.Equal(HttpStatusCode.Conflict, roleChangeResponse.StatusCode);

        using var deactivateResponse = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/users/{administratorUserId}/active",
            new { isActive = false });
        Assert.Equal(HttpStatusCode.Conflict, deactivateResponse.StatusCode);

        using var rolePermissionResponse = await PutWithCsrfAsync(
            client,
            $"/api/v1/admin/roles/{roles["ADMINISTRATION"]}/permissions",
            new { permissionIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.Conflict, rolePermissionResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidPasswordDoesNotAuthenticateAndIsAudited()
    {
        var companyId = await BootstrapAsync(client);
        var response = await LoginAsync(client, AdministratorEmail, "wrong-password", companyId);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        Assert.Contains(await db.SecurityAuditEvents.ToListAsync(),
            auditEvent => auditEvent.Action == "auth.login.failed");
    }

    [Fact]
    public async Task UnknownEmailLoginIsPersistedInSecurityAudit()
    {
        var companyId = await BootstrapAsync(client);
        var response = await LoginAsync(
            client, "unknown@example.test", "Unknown-user-Password-08!", companyId);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var failedLogin = await db.SecurityAuditEvents
            .SingleAsync(auditEvent => auditEvent.Action == "auth.login.failed");

        Assert.Equal(companyId, failedLogin.CompanyId);
        Assert.Null(failedLogin.TargetUserId);
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    private static async Task<Guid> BootstrapAsync(HttpClient client)
    {
        var response = await PostWithCsrfAsync(client, "/api/v1/setup/bootstrap", new
        {
            companyCode = "ACME",
            companyName = "Acme Manufacturing",
            adminEmail = AdministratorEmail,
            adminDisplayName = "ERP Administrator",
            adminPassword = AdministratorPassword,
        }, new Dictionary<string, string>
        {
            ["X-ERP-Bootstrap-Token"] = BootstrapToken,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return result.RootElement.GetProperty("companyId").GetGuid();
    }

    private async Task<string> GetPasswordHashAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        return await db.UserAccounts
            .Where(user => user.NormalizedEmail == AdministratorEmail.ToUpperInvariant())
            .Select(user => user.PasswordHash!)
            .SingleAsync();
    }

    private async Task<Guid> GetAdministratorUserIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        return await db.UserAccounts
            .Where(user => user.NormalizedEmail == AdministratorEmail.ToUpperInvariant())
            .Select(user => user.Id)
            .SingleAsync();
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password,
        Guid companyId) =>
        await PostWithCsrfAsync(client, "/api/v1/auth/login", new { email, password, companyId });

    private static async Task<HttpResponseMessage> PostWithCsrfAsync(
        HttpClient client,
        string path,
        object body,
        IReadOnlyDictionary<string, string>? additionalHeaders = null)
    {
        using var tokenResponse = await client.GetAsync("/api/v1/auth/csrf");
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var token = tokenJson.RootElement.GetProperty("requestToken").GetString()!;
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-ERP-CSRF", token);
        if (additionalHeaders is not null)
        {
            foreach (var header in additionalHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PutWithCsrfAsync(
        HttpClient client,
        string path,
        object body)
    {
        using var tokenResponse = await client.GetAsync("/api/v1/auth/csrf");
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add(
            "X-ERP-CSRF",
            tokenJson.RootElement.GetProperty("requestToken").GetString()!);
        return await client.SendAsync(request);
    }
}
