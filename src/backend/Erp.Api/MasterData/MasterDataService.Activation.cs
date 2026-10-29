using Erp.Domain.MasterData;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.MasterData;

internal sealed partial class MasterDataService
{
    public async Task<MasterDataResult<bool>> SetActiveAsync(
        Guid companyId,
        MasterDataActor actor,
        string resource,
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        string entityType;
        bool prior;
        switch (resource)
        {
            case "customers":
                var customer = await db.Customers.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (customer is null) return NotFound<bool>();
                prior = customer.IsActive;
                customer.IsActive = isActive;
                entityType = nameof(Customer);
                break;
            case "suppliers":
                var supplier = await db.Suppliers.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (supplier is null) return NotFound<bool>();
                prior = supplier.IsActive;
                supplier.IsActive = isActive;
                entityType = nameof(Supplier);
                break;
            case "item-categories":
                var category = await db.ItemCategories.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (category is null) return NotFound<bool>();
                if (!isActive && await db.Items.AnyAsync(item =>
                        item.CompanyId == companyId && item.CategoryId == id && item.IsActive,
                        cancellationToken))
                {
                    return Conflict<bool>("Deactivate active items in this category first.");
                }

                prior = category.IsActive;
                category.IsActive = isActive;
                entityType = nameof(ItemCategory);
                break;
            case "units":
                var unit = await db.UnitsOfMeasure.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (unit is null) return NotFound<bool>();
                if (!isActive
                    && (await db.Items.AnyAsync(item => item.CompanyId == companyId
                            && item.IsActive
                            && (item.StockUnitId == id || item.PurchaseUnitId == id),
                        cancellationToken)
                        || await db.BomComponents.AnyAsync(component =>
                            component.CompanyId == companyId
                            && component.UnitOfMeasureId == id
                            && db.BomVersions.Any(version =>
                                version.Id == component.BomVersionId
                                && version.Status == BomVersionStatuses.Approved),
                            cancellationToken)))
                {
                    return Conflict<bool>("This unit is used by active items or approved BOMs.");
                }

                prior = unit.IsActive;
                unit.IsActive = isActive;
                entityType = nameof(UnitOfMeasure);
                break;
            case "items":
                var item = await db.Items.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (item is null) return NotFound<bool>();
                if (!isActive && await db.ProductVariants.AnyAsync(variant =>
                        variant.CompanyId == companyId && variant.ItemId == id && variant.IsActive,
                        cancellationToken))
                {
                    return Conflict<bool>("Deactivate active product variants using this item first.");
                }

                if (!isActive && await db.BomComponents.AnyAsync(component =>
                        component.CompanyId == companyId && component.ItemId == id
                        && db.BomVersions.Any(version =>
                            version.Id == component.BomVersionId
                            && version.Status == BomVersionStatuses.Approved),
                        cancellationToken))
                {
                    return Conflict<bool>("This item is required by an approved BOM version.");
                }

                if (isActive && !await db.ItemCategories.AnyAsync(value =>
                        value.CompanyId == companyId && value.Id == item.CategoryId
                        && value.IsActive, cancellationToken))
                {
                    return Conflict<bool>("Activate the item's category before activating this item.");
                }

                prior = item.IsActive;
                item.IsActive = isActive;
                entityType = nameof(Item);
                break;
            case "warehouses":
                var warehouse = await db.Warehouses.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (warehouse is null) return NotFound<bool>();
                if (!isActive && await db.WarehouseLocations.AnyAsync(location =>
                        location.CompanyId == companyId && location.WarehouseId == id
                        && location.IsActive, cancellationToken))
                {
                    return Conflict<bool>("Deactivate active locations in this warehouse first.");
                }

                prior = warehouse.IsActive;
                warehouse.IsActive = isActive;
                entityType = nameof(Warehouse);
                break;
            case "warehouse-locations":
                var location = await db.WarehouseLocations.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (location is null) return NotFound<bool>();
                if (!isActive && await db.WarehouseLocations.AnyAsync(child =>
                        child.CompanyId == companyId && child.ParentLocationId == id
                        && child.IsActive, cancellationToken))
                {
                    return Conflict<bool>("Deactivate active child locations first.");
                }

                if (isActive
                    && (!await db.Warehouses.AnyAsync(value =>
                            value.CompanyId == companyId && value.Id == location.WarehouseId
                            && value.IsActive, cancellationToken)
                        || location.ParentLocationId is { } parentId
                        && !await db.WarehouseLocations.AnyAsync(parent =>
                            parent.CompanyId == companyId && parent.Id == parentId && parent.IsActive,
                            cancellationToken)))
                {
                    return Conflict<bool>("Activate the warehouse and parent location first.");
                }

                prior = location.IsActive;
                location.IsActive = isActive;
                entityType = nameof(WarehouseLocation);
                break;
            case "products":
                var product = await db.Products.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (product is null) return NotFound<bool>();
                if (!isActive
                    && (await db.ProductVariants.AnyAsync(variant =>
                            variant.CompanyId == companyId && variant.ProductId == id
                            && variant.IsActive, cancellationToken)
                        || await db.Boms.AnyAsync(bom =>
                            bom.CompanyId == companyId && bom.ProductId == id && bom.IsActive,
                            cancellationToken)))
                {
                    return Conflict<bool>("Deactivate active variants and BOMs for this style first.");
                }

                prior = product.IsActive;
                product.IsActive = isActive;
                entityType = nameof(Product);
                break;
            case "variants":
                var variant = await db.ProductVariants.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (variant is null) return NotFound<bool>();
                if (isActive
                    && (!await db.Products.AnyAsync(value =>
                            value.CompanyId == companyId && value.Id == variant.ProductId
                            && value.IsActive, cancellationToken)
                        || !await db.Items.AnyAsync(value =>
                            value.CompanyId == companyId && value.Id == variant.ItemId
                            && value.IsActive, cancellationToken)))
                {
                    return Conflict<bool>("Activate the product and stock item before this variant.");
                }

                prior = variant.IsActive;
                variant.IsActive = isActive;
                entityType = nameof(ProductVariant);
                break;
            case "machines":
                var machine = await db.Machines.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (machine is null) return NotFound<bool>();
                prior = machine.IsActive;
                machine.IsActive = isActive;
                entityType = nameof(Machine);
                break;
            case "operations":
                var operation = await db.ProductionOperations.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (operation is null) return NotFound<bool>();
                prior = operation.IsActive;
                operation.IsActive = isActive;
                entityType = nameof(ProductionOperation);
                break;
            case "boms":
                var bom = await db.Boms.SingleOrDefaultAsync(
                    value => value.CompanyId == companyId && value.Id == id, cancellationToken);
                if (bom is null) return NotFound<bool>();
                if (isActive && !await db.Products.AnyAsync(value =>
                        value.CompanyId == companyId && value.Id == bom.ProductId
                        && value.IsActive, cancellationToken))
                {
                    return Conflict<bool>("Activate the product style before this BOM.");
                }

                prior = bom.IsActive;
                bom.IsActive = isActive;
                entityType = nameof(Bom);
                break;
            default:
                return Invalid<bool>("resource", "The requested resource cannot be activated.");
        }

        if (prior == isActive)
        {
            return new MasterDataResult<bool>(isActive);
        }

        db.AuditEvents.Add(CreateAudit(
            companyId, actor,
            $"masterdata.{resource}.{(isActive ? "activated" : "deactivated")}",
            entityType, id, new { isActive }));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new MasterDataResult<bool>(isActive);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogInformation(exception, "Concurrent activation update rejected for {Resource} {Id}.",
                resource, id);
            return Conflict<bool>("The record changed during this request. Reload it and retry.");
        }
    }
}
