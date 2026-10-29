using Erp.Domain.MasterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "Customers");
        MappingRules.ConfigureCodeAndName(entity);
        entity.Property(x => x.LegalName).HasMaxLength(200);
        entity.Property(x => x.Email).HasMaxLength(254);
        entity.Property(x => x.Phone).HasMaxLength(40);
        entity.Property(x => x.AddressLine1).HasMaxLength(200);
        entity.Property(x => x.AddressLine2).HasMaxLength(200);
        entity.Property(x => x.City).HasMaxLength(100);
        entity.Property(x => x.StateOrProvince).HasMaxLength(100);
        entity.Property(x => x.PostalCode).HasMaxLength(24);
        entity.Property(x => x.CountryCode).HasMaxLength(2);
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "Suppliers");
        MappingRules.ConfigureCodeAndName(entity);
        entity.Property(x => x.LegalName).HasMaxLength(200);
        entity.Property(x => x.Email).HasMaxLength(254);
        entity.Property(x => x.Phone).HasMaxLength(40);
        entity.Property(x => x.AddressLine1).HasMaxLength(200);
        entity.Property(x => x.AddressLine2).HasMaxLength(200);
        entity.Property(x => x.City).HasMaxLength(100);
        entity.Property(x => x.StateOrProvince).HasMaxLength(100);
        entity.Property(x => x.PostalCode).HasMaxLength(24);
        entity.Property(x => x.CountryCode).HasMaxLength(2);
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "UnitsOfMeasure");
        MappingRules.ConfigureCodeAndName(entity);
        entity.Property(x => x.Dimension).HasMaxLength(60).IsRequired();
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        entity.ToTable(table => table.HasCheckConstraint(
            "CK_UnitsOfMeasure_DecimalPlaces",
            """ "DecimalPlaces" >= 0 AND "DecimalPlaces" <= 6 """));
    }
}

internal sealed class UomConversionConfiguration : IEntityTypeConfiguration<UomConversion>
{
    public void Configure(EntityTypeBuilder<UomConversion> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "UomConversions");
        entity.Property(x => x.Factor).HasPrecision(20, 10);
        entity.HasIndex(x => new
        {
            x.CompanyId,
            x.FromUnitId,
            x.ToUnitId,
            x.EffectiveFrom,
        }).IsUnique();
        entity.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.FromUnitId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.ToUnitId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_UomConversions_Factor",
                """ CAST("Factor" AS NUMERIC) > 0 """);
            table.HasCheckConstraint("CK_UomConversions_DifferentUnits", """ "FromUnitId" <> "ToUnitId" """);
            table.HasCheckConstraint(
                "CK_UomConversions_EffectiveDates",
                """ "EffectiveTo" IS NULL OR "EffectiveTo" >= "EffectiveFrom" """);
        });
    }
}

internal sealed class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "ItemCategories");
        MappingRules.ConfigureCodeAndName(entity);
        entity.Property(x => x.TrackingMode).HasMaxLength(16).IsRequired();
        entity.Property(x => x.CategoryType).HasMaxLength(24).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(500);
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ItemCategories_TrackingMode",
                """ "TrackingMode" IN ('none', 'lot', 'serial') """);
            table.HasCheckConstraint(
                "CK_ItemCategories_CategoryType",
                """ "CategoryType" IN ('general', 'fabric', 'trims', 'accessory', 'packaging', 'finished_good') """);
        });
    }
}

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "Items");
        MappingRules.ConfigureCodeAndName(entity);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.PurchaseToStockFactor).HasPrecision(20, 10);
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        entity.HasOne<ItemCategory>()
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.CategoryId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.StockUnitId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PurchaseUnitId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table => table.HasCheckConstraint(
            "CK_Items_PurchaseToStockFactor",
            """ CAST("PurchaseToStockFactor" AS NUMERIC) > 0 """));
    }
}

internal sealed class ItemCodeRuleConfiguration : IEntityTypeConfiguration<ItemCodeRule>
{
    public void Configure(EntityTypeBuilder<ItemCodeRule> entity)
    {
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.CompanyId).IsUnique();
        entity.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
        entity.Property(x => x.Separator).HasMaxLength(4).IsRequired();
        entity.Property(x => x.Revision).IsConcurrencyToken();
        entity.HasOne<Erp.Domain.Platform.Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ItemCodeRules_Prefix_NotBlank",
                """length(trim("Prefix")) > 0""");
            table.HasCheckConstraint(
                "CK_ItemCodeRules_Padding",
                """ "Padding" >= 1 AND "Padding" <= 12 """);
            table.HasCheckConstraint(
                "CK_ItemCodeRules_NextNumber",
                """ "NextNumber" >= 1 """);
            table.HasCheckConstraint(
                "CK_ItemCodeRules_Revision",
                """ "Revision" >= 0 """);
        });
    }
}

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "Warehouses");
        MappingRules.ConfigureCodeAndName(entity);
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "Machines");
        MappingRules.ConfigureCodeAndName(entity);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.ProductionLine).HasMaxLength(100);
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class ProductionOperationConfiguration : IEntityTypeConfiguration<ProductionOperation>
{
    public void Configure(EntityTypeBuilder<ProductionOperation> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "ProductionOperations");
        MappingRules.ConfigureCodeAndName(entity);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
    }
}

internal sealed class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseLocation> entity)
    {
        MappingRules.ConfigureCompanyScoped(entity, "WarehouseLocations");
        MappingRules.ConfigureCodeAndName(entity);
        entity.HasAlternateKey(x => new { x.CompanyId, x.WarehouseId, x.Id });
        entity.HasIndex(x => new { x.CompanyId, x.WarehouseId, x.Code }).IsUnique();
        entity.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.WarehouseId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<WarehouseLocation>()
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.WarehouseId, x.ParentLocationId })
            .HasPrincipalKey(x => new { x.CompanyId, x.WarehouseId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable("WarehouseLocations", table =>
            table.HasCheckConstraint(
                "CK_WarehouseLocations_NotOwnParent",
                """ "ParentLocationId" IS NULL OR "ParentLocationId" <> "Id" """));
    }
}
