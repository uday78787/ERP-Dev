namespace Erp.Domain.MasterData;

public sealed class Customer
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateOrProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Supplier
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateOrProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UnitOfMeasure
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UomConversion
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FromUnitId { get; set; }
    public Guid ToUnitId { get; set; }
    public decimal Factor { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class ItemCategory
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TrackingMode { get; set; } = "none";
    public string CategoryType { get; set; } = "general";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Item
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid StockUnitId { get; set; }
    public Guid PurchaseUnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchaseToStockFactor { get; set; } = 1m;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ItemCodeRule
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Prefix { get; set; } = "ITEM";
    public string Separator { get; set; } = "-";
    public int Padding { get; set; } = 6;
    public long NextNumber { get; set; } = 1;
    public int Revision { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Warehouse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class WarehouseLocation
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? ParentLocationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Machine
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProductionLine { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductionOperation
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsOutsourced { get; set; }
    public bool IsActive { get; set; } = true;
}
