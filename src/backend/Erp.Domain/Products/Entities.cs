namespace Erp.Domain.Products;

public sealed class Product
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IndustryCode { get; set; } = "general";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ProductVariant> Variants { get; set; } = [];
}

public sealed class ProductVariant
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SizeCode { get; set; }
    public string? ColorCode { get; set; }
    public string? ColorName { get; set; }
    public bool IsActive { get; set; } = true;
    public Product Product { get; set; } = null!;
    public ICollection<ProductVariantAttribute> Attributes { get; set; } = [];
}

public sealed class ProductVariantAttribute
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductVariantId { get; set; }
    public string AttributeCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
