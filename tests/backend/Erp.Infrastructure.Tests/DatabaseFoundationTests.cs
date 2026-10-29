using Erp.Domain.MasterData;
using Erp.Domain.Platform;
using Erp.Domain.Products;
using Erp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Erp.Infrastructure.Tests;

public sealed class DatabaseFoundationTests
{
    [Fact]
    public void ItemCodesAreUniqueWithinACompany()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        var (company, category, unit) = AddItemDependencies(context);

        context.Items.Add(CreateItem(company.Id, category.Id, unit.Id, "FAB-001"));
        context.SaveChanges();

        context.Items.Add(CreateItem(company.Id, category.Id, unit.Id, "FAB-001"));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void ItemCannotReferenceMasterDataFromAnotherCompany()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        var (company, _, unit) = AddItemDependencies(context);
        var otherCompany = new Company
        {
            Id = Guid.NewGuid(),
            Code = "OTHER",
            Name = "Other company",
        };
        context.Companies.Add(otherCompany);
        var otherCategory = new ItemCategory
        {
            Id = Guid.NewGuid(),
            CompanyId = otherCompany.Id,
            Code = "FABRIC",
            Name = "Fabric",
        };
        context.ItemCategories.Add(otherCategory);
        context.SaveChanges();

        context.Items.Add(CreateItem(company.Id, otherCategory.Id, unit.Id, "FAB-001"));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void DatabaseRejectsNonPositiveConversionAndBomQuantities()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        var (company, category, unit) = AddItemDependencies(context);
        var alternateUnit = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Code = "CM",
            Name = "Centimeter",
            Dimension = "length",
            DecimalPlaces = 2,
        };
        context.UnitsOfMeasure.Add(alternateUnit);
        context.SaveChanges();
        var conversion = new UomConversion
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            FromUnitId = unit.Id,
            ToUnitId = alternateUnit.Id,
            Factor = 0,
            EffectiveFrom = new DateOnly(2026, 1, 1),
        };
        context.UomConversions.Add(conversion);

        var conversionError = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        Assert.Contains("CK_UomConversions_Factor", conversionError.InnerException?.Message);

        context.ChangeTracker.Clear();
        var item = CreateItem(company.Id, category.Id, unit.Id, "FAB-001");
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Code = "STYLE-001",
            Name = "Style",
            IndustryCode = "apparel",
        };
        var bom = new Bom
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ProductId = product.Id,
            Code = "BOM-001",
            Name = "Style BOM",
        };
        var version = new BomVersion
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            BomId = bom.Id,
            VersionNumber = 1,
            EffectiveFrom = new DateOnly(2026, 1, 1),
        };
        var component = new BomComponent
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            BomVersionId = version.Id,
            LineNumber = 1,
            ItemId = item.Id,
            UnitOfMeasureId = unit.Id,
            Quantity = 0,
        };

        context.Items.Add(item);
        context.Products.Add(product);
        context.Boms.Add(bom);
        context.BomVersions.Add(version);
        context.BomComponents.Add(component);

        var quantityError = Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        Assert.Contains("CK_BomComponents_Quantity", quantityError.InnerException?.Message);
    }

    [Fact]
    public void ReferencedMasterDataCannotBeDeleted()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        var (_, category, unit) = AddItemDependencies(context);
        context.Items.Add(CreateItem(category.CompanyId, category.Id, unit.Id, "FAB-001"));
        context.SaveChanges();
        context.ChangeTracker.Clear();
        category = context.ItemCategories.Single(x => x.Id == category.Id);

        context.ItemCategories.Remove(category);

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void RelatedWritesCanBeRolledBackAtomically()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        var companyId = Guid.NewGuid();

        using (var transaction = context.Database.BeginTransaction())
        {
            context.Companies.Add(new Company
            {
                Id = companyId,
                Code = "ACME",
                Name = "Acme Manufacturing",
            });
            context.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Code = "PLANNER",
                Name = "Planner",
            });
            context.SaveChanges();
            transaction.Rollback();
        }

        Assert.Empty(context.Companies);
        Assert.Empty(context.Roles);
    }

    [Fact]
    public void QuantityAndConversionPrecisionAreConfigured()
    {
        using var connection = CreateConnection();
        using var context = CreateContext(connection);
        var bomQuantity = context.Model.FindEntityType(typeof(BomComponent))!
            .FindProperty(nameof(BomComponent.Quantity))!;
        var conversionFactor = context.Model.FindEntityType(typeof(UomConversion))!
            .FindProperty(nameof(UomConversion.Factor))!;

        Assert.Equal(20, bomQuantity.GetPrecision());
        Assert.Equal(6, bomQuantity.GetScale());
        Assert.Equal(20, conversionFactor.GetPrecision());
        Assert.Equal(10, conversionFactor.GetScale());
    }

    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static ErpDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ErpDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static (Company Company, ItemCategory Category, UnitOfMeasure Unit)
        AddItemDependencies(ErpDbContext context)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Code = "ACME",
            Name = "Acme Manufacturing",
        };
        var category = new ItemCategory
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Code = "FABRIC",
            Name = "Fabric",
        };
        var unit = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Code = "M",
            Name = "Meter",
            Dimension = "length",
            DecimalPlaces = 3,
        };
        context.AddRange(company, category, unit);
        context.SaveChanges();
        return (company, category, unit);
    }

    private static Item CreateItem(Guid companyId, Guid categoryId, Guid unitId, string code)
    {
        return new Item
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CategoryId = categoryId,
            StockUnitId = unitId,
            PurchaseUnitId = unitId,
            Code = code,
            Name = "Fabric",
        };
    }
}
