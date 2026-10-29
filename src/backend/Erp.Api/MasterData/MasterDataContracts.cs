using System.ComponentModel.DataAnnotations;

namespace Erp.Api.MasterData;

internal sealed record MasterDataQuery
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public bool? Active { get; init; }
    public string? Type { get; init; }
    public Guid? ParentId { get; init; }
    public Guid? ProductId { get; init; }
    public string? Size { get; init; }
    public string? Color { get; init; }
}

internal sealed record PageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

internal sealed record MasterDataActor(Guid UserId, string Subject, string? CorrelationId);

internal sealed record MasterDataResult<T>(
    T? Value,
    int StatusCode = StatusCodes.Status200OK,
    string? Error = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    Guid? ResourceId = null);

internal sealed record CustomerRequest
{
    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [StringLength(200)]
    public string? LegalName { get; init; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; init; }

    [StringLength(40)]
    public string? Phone { get; init; }

    [StringLength(200)]
    public string? AddressLine1 { get; init; }

    [StringLength(200)]
    public string? AddressLine2 { get; init; }

    [StringLength(100)]
    public string? City { get; init; }

    [StringLength(100)]
    public string? StateOrProvince { get; init; }

    [StringLength(24)]
    public string? PostalCode { get; init; }

    [StringLength(2, MinimumLength = 2)]
    public string? CountryCode { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record CustomerResponse(
    Guid Id,
    string Code,
    string Name,
    string? LegalName,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    bool IsActive,
    DateTimeOffset CreatedAt);

internal sealed record SupplierRequest
{
    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [StringLength(200)]
    public string? LegalName { get; init; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; init; }

    [StringLength(40)]
    public string? Phone { get; init; }

    [StringLength(200)]
    public string? AddressLine1 { get; init; }

    [StringLength(200)]
    public string? AddressLine2 { get; init; }

    [StringLength(100)]
    public string? City { get; init; }

    [StringLength(100)]
    public string? StateOrProvince { get; init; }

    [StringLength(24)]
    public string? PostalCode { get; init; }

    [StringLength(2, MinimumLength = 2)]
    public string? CountryCode { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record SupplierResponse(
    Guid Id,
    string Code,
    string Name,
    string? LegalName,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    bool IsActive,
    DateTimeOffset CreatedAt);

internal sealed record ItemCategoryRequest
{
    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [Required, RegularExpression("^(none|lot|serial)$")]
    public required string TrackingMode { get; init; }

    [Required, RegularExpression("^(general|fabric|trims|accessory|packaging|finished_good)$")]
    public required string CategoryType { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record ItemCategoryResponse(
    Guid Id,
    string Code,
    string Name,
    string TrackingMode,
    string CategoryType,
    string? Description,
    bool IsActive);

internal sealed record UnitOfMeasureRequest
{
    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [Required, StringLength(60)]
    public required string Dimension { get; init; }

    [Range(0, 6)]
    public int DecimalPlaces { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record UnitOfMeasureResponse(
    Guid Id,
    string Code,
    string Name,
    string Dimension,
    int DecimalPlaces,
    bool IsActive);

internal sealed record ItemRequest
{
    [StringLength(40)]
    public string? Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [Required]
    public Guid CategoryId { get; init; }

    [Required]
    public Guid StockUnitId { get; init; }

    [Required]
    public Guid PurchaseUnitId { get; init; }

    [Range(typeof(decimal), "0.0000000001", "999999999999")]
    public decimal PurchaseToStockFactor { get; init; } = 1m;

    [StringLength(1000)]
    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record ItemResponse(
    Guid Id,
    string Code,
    string Name,
    Guid CategoryId,
    string CategoryCode,
    string CategoryName,
    Guid StockUnitId,
    string StockUnitCode,
    Guid PurchaseUnitId,
    string PurchaseUnitCode,
    decimal PurchaseToStockFactor,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt);

internal sealed record ItemCodeRuleRequest
{
    [Required, StringLength(20)]
    public required string Prefix { get; init; }

    [Required, StringLength(4)]
    public string Separator { get; init; } = "-";

    [Range(1, 12)]
    public int Padding { get; init; } = 6;

    [Range(1, long.MaxValue)]
    public long NextNumber { get; init; } = 1;

    public int? ExpectedRevision { get; init; }
}

internal sealed record ItemCodeRuleResponse(
    string Prefix,
    string Separator,
    int Padding,
    long NextNumber,
    int Revision,
    bool IsActive,
    string Preview);

internal sealed record WarehouseRequest
{
    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record WarehouseResponse(Guid Id, string Code, string Name, bool IsActive);

internal sealed record WarehouseLocationRequest
{
    [Required]
    public Guid WarehouseId { get; init; }

    public Guid? ParentLocationId { get; init; }

    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record WarehouseLocationResponse(
    Guid Id,
    Guid WarehouseId,
    string WarehouseCode,
    Guid? ParentLocationId,
    string Code,
    string Name,
    bool IsActive);

internal sealed record ProductRequest
{
    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    [Required, StringLength(40)]
    public string IndustryCode { get; init; } = "general";

    public bool IsActive { get; init; } = true;
}

internal sealed record ProductResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string IndustryCode,
    bool IsActive,
    DateTimeOffset CreatedAt);

internal sealed record ProductVariantRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    public Guid ItemId { get; init; }

    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [StringLength(40)]
    public string? SizeCode { get; init; }

    [StringLength(40)]
    public string? ColorCode { get; init; }

    [StringLength(100)]
    public string? ColorName { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record ProductVariantResponse(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    Guid ItemId,
    string ItemCode,
    string Code,
    string Name,
    string? SizeCode,
    string? ColorCode,
    string? ColorName,
    bool IsActive);

internal sealed record MachineRequest
{
    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    [StringLength(100)]
    public string? ProductionLine { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record MachineResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? ProductionLine,
    bool IsActive);

internal sealed record ProductionOperationRequest
{
    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    public bool IsOutsourced { get; init; }
    public bool IsActive { get; init; } = true;
}

internal sealed record ProductionOperationResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsOutsourced,
    bool IsActive);

internal sealed record BomRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Required, StringLength(40)]
    public required string Code { get; init; }

    [Required, StringLength(160)]
    public required string Name { get; init; }

    public bool IsActive { get; init; } = true;
}

internal sealed record BomResponse(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string Code,
    string Name,
    bool IsActive,
    int VersionCount);

internal sealed record BomComponentRequest
{
    [Required]
    public Guid ItemId { get; init; }

    [Required]
    public Guid UnitOfMeasureId { get; init; }

    [Range(1, int.MaxValue)]
    public int LineNumber { get; init; }

    [Range(typeof(decimal), "0.000001", "999999999999")]
    public decimal Quantity { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal WastePercent { get; init; }

    [StringLength(60)]
    public string? AlternativeGroupCode { get; init; }

    public bool IsOptional { get; init; }
}

internal sealed record BomComponentResponse(
    Guid Id,
    int LineNumber,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    Guid UnitOfMeasureId,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal WastePercent,
    string? AlternativeGroupCode,
    bool IsOptional);

internal sealed record BomVersionRequest
{
    [Required]
    public DateOnly EffectiveFrom { get; init; }

    public DateOnly? EffectiveTo { get; init; }
}

internal sealed record BomVersionResponse(
    Guid Id,
    Guid BomId,
    int VersionNumber,
    string Status,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ApprovedBySubject,
    DateTimeOffset? ApprovedAt,
    IReadOnlyList<BomComponentResponse> Components);
