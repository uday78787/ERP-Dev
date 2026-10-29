namespace Erp.Domain.Products;

public static class BomVersionStatuses
{
    public const string Draft = "draft";
    public const string Approved = "approved";
    public const string Retired = "retired";
}

public sealed class Bom
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<BomVersion> Versions { get; set; } = [];
}

public sealed class BomVersion
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BomId { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = BomVersionStatuses.Draft;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? ApprovedBySubject { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public ICollection<BomComponent> Components { get; set; } = [];
}

public sealed class BomComponent
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BomVersionId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public decimal Quantity { get; set; }
    public decimal WastePercent { get; set; }
    public string? AlternativeGroupCode { get; set; }
    public bool IsOptional { get; set; }
}
