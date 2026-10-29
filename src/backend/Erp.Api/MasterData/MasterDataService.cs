using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Erp.Domain.MasterData;
using Erp.Domain.Platform;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Erp.Infrastructure.Persistence;
using Npgsql;

namespace Erp.Api.MasterData;

internal sealed partial class MasterDataService : IMasterDataService
{
    private readonly ErpDbContext db;
    private readonly ILogger<MasterDataService> logger;

    public MasterDataService(ErpDbContext db, ILogger<MasterDataService> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    private static Dictionary<string, string[]> Validate(object request)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        return validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (member, error: result.ErrorMessage ?? "The value is invalid.")))
            .GroupBy(item => item.member, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.error).Distinct().ToArray(),
                StringComparer.Ordinal);
    }

    private static MasterDataResult<T> Invalid<T>(string field, string message) =>
        new(default, StatusCodes.Status400BadRequest,
            ValidationErrors: new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [field] = [message],
            });

    private static MasterDataResult<T> Invalid<T>(Dictionary<string, string[]> errors) =>
        new(default, StatusCodes.Status400BadRequest, ValidationErrors: errors);

    private static MasterDataResult<T> NotFound<T>() =>
        new(default, StatusCodes.Status404NotFound);

    private static MasterDataResult<T> Conflict<T>(string message) =>
        new(default, StatusCodes.Status409Conflict, message);

    private async Task<MasterDataResult<TResponse>> PersistAsync<TEntity, TResponse>(
        TEntity entity,
        Guid companyId,
        MasterDataActor actor,
        string action,
        object details,
        Func<TResponse> response,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ActorUserId = actor.UserId,
            ActorSubjectSnapshot = actor.Subject,
            Action = action,
            EntityType = typeof(TEntity).Name,
            EntityId = GetEntityId(entity),
            CorrelationId = actor.CorrelationId,
            DetailsJson = JsonSerializer.Serialize(details),
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new MasterDataResult<TResponse>(
                response(),
                action.EndsWith(".created", StringComparison.Ordinal)
                    ? StatusCodes.Status201Created
                    : StatusCodes.Status200OK,
                ResourceId: GetEntityId(entity));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogInformation(exception, "Concurrent master-data update rejected for {Entity}.",
                typeof(TEntity).Name);
            return Conflict<TResponse>("The record changed during this request. Reload it and retry.");
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            logger.LogInformation(exception, "Duplicate master-data code rejected for {Entity}.",
                typeof(TEntity).Name);
            return Conflict<TResponse>("A record with the same code or key already exists.");
        }
    }

    private static Guid GetEntityId<TEntity>(TEntity entity) where TEntity : class =>
        entity switch
        {
            Customer value => value.Id,
            Supplier value => value.Id,
            ItemCategory value => value.Id,
            UnitOfMeasure value => value.Id,
            Item value => value.Id,
            ItemCodeRule value => value.Id,
            Warehouse value => value.Id,
            WarehouseLocation value => value.Id,
            Product value => value.Id,
            ProductVariant value => value.Id,
            Machine value => value.Id,
            ProductionOperation value => value.Id,
            Bom value => value.Id,
            BomVersion value => value.Id,
            _ => throw new InvalidOperationException($"Audit entity type {typeof(TEntity).Name} is unsupported."),
        };

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };

    private static async Task<PageResponse<T>> ToPageAsync<T>(
        IQueryable<T> query,
        MasterDataQuery filter,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PageResponse<T>(items, filter.Page, filter.PageSize, totalCount);
    }

    private static IQueryable<TEntity> ApplyActive<TEntity>(
        IQueryable<TEntity> query,
        bool? active,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate) =>
        active is null ? query : query.Where(predicate);

    private static bool IsValidCode(string? code, int maxLength = 40) =>
        !string.IsNullOrWhiteSpace(code)
        && code.Trim().Length is >= 2
        && code.Trim().Length <= maxLength
        && code.Trim().All(character =>
            character is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '_' or '-' or '.');

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SearchText(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string ActorName(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name)
        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? "unknown";
}
