using Erp.Domain.MasterData;
using Erp.Domain.Platform;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserCompanyRole> UserCompanyRoles => Set<UserCompanyRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<UomConversion> UomConversions => Set<UomConversion>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemCodeRule> ItemCodeRules => Set<ItemCodeRule>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<ProductionOperation> ProductionOperations => Set<ProductionOperation>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantAttribute> ProductVariantAttributes => Set<ProductVariantAttribute>();
    public DbSet<Bom> Boms => Set<Bom>();
    public DbSet<BomVersion> BomVersions => Set<BomVersion>();
    public DbSet<BomComponent> BomComponents => Set<BomComponent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);
    }
}
