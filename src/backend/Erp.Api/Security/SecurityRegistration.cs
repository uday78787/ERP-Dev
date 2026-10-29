using System.Security.Claims;
using Erp.Domain.Platform;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Erp.Api.Security;

internal static class SecurityRegistration
{
    public static IServiceCollection AddErpSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-ERP-CSRF";
            options.Cookie.Name = "__Host-erp-csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
        });

        services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-erp-session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    },
                    OnValidatePrincipal = ValidateCookiePrincipalAsync,
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                GetRateLimitKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
            options.AddPolicy("bootstrap", context => RateLimitPartition.GetFixedWindowLimiter(
                GetRateLimitKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                rejected.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await rejected.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many requests. Try again later." },
                    cancellationToken);
            };
        });

        return services;
    }

    private static string GetRateLimitKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async Task ValidateCookiePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var userIdValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var companyIdValue = principal?.FindFirstValue(SecurityClaims.CompanyId);
        var securityStamp = principal?.FindFirstValue(SecurityClaims.SecurityStamp);

        if (!Guid.TryParse(userIdValue, out var userId)
            || !Guid.TryParse(companyIdValue, out var companyId)
            || string.IsNullOrWhiteSpace(securityStamp))
        {
            await RejectPrincipalAsync(context);
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<ErpDbContext>();
        var isSessionValid = await db.UserAccounts.AsNoTracking().AnyAsync(
            user => user.Id == userId
                && user.IsActive
                && user.SecurityStamp == securityStamp
                && db.Companies.Any(company => company.Id == companyId && company.IsActive)
                && user.CompanyRoles.Any(membership =>
                    membership.CompanyId == companyId
                    && membership.Role.IsActive),
            context.HttpContext.RequestAborted);

        if (!isSessionValid)
        {
            await RejectPrincipalAsync(context);
        }
    }

    private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
