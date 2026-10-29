using Erp.Domain.MasterData;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.MasterData;

internal sealed partial class MasterDataService
{
    public Task<PageResponse<WarehouseResponse>> ListWarehousesAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var warehouses = db.Warehouses.AsNoTracking()
            .Where(warehouse => warehouse.CompanyId == companyId);
        if (query.Active is { } active)
        {
            warehouses = warehouses.Where(warehouse => warehouse.IsActive == active);
        }

        if (search.Length > 0)
        {
            warehouses = warehouses.Where(warehouse =>
                warehouse.Code.ToLower().Contains(search)
                || warehouse.Name.ToLower().Contains(search));
        }

        return ToPageAsync(warehouses.OrderBy(warehouse => warehouse.Code).Select(warehouse =>
            new WarehouseResponse(
                warehouse.Id, warehouse.Code, warehouse.Name, warehouse.IsActive)),
            query, cancellationToken);
    }

    public async Task<MasterDataResult<WarehouseResponse>> SaveWarehouseAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        WarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<WarehouseResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<WarehouseResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var code = NormalizeCode(request.Code);
        var warehouse = id is { } existingId
            ? await db.Warehouses.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && warehouse is null)
        {
            return NotFound<WarehouseResponse>();
        }

        if (await db.Warehouses.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<WarehouseResponse>("A warehouse with this code already exists.");
        }

        var created = warehouse is null;
        warehouse ??= new Warehouse { Id = Guid.NewGuid(), CompanyId = companyId };
        warehouse.Code = code;
        warehouse.Name = request.Name.Trim();
        warehouse.IsActive = request.IsActive;
        if (created)
        {
            db.Warehouses.Add(warehouse);
        }

        return await PersistAsync(
            warehouse, companyId, actor,
            $"masterdata.warehouse.{(created ? "created" : "updated")}",
            new { warehouse.Code, warehouse.Name, warehouse.IsActive },
            () => new WarehouseResponse(
                warehouse.Id, warehouse.Code, warehouse.Name, warehouse.IsActive),
            cancellationToken);
    }

    public Task<PageResponse<WarehouseLocationResponse>> ListWarehouseLocationsAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var locations = db.WarehouseLocations.AsNoTracking()
            .Where(location => location.CompanyId == companyId);
        if (query.Active is { } active)
        {
            locations = locations.Where(location => location.IsActive == active);
        }

        if (query.ParentId is { } warehouseId)
        {
            locations = locations.Where(location => location.WarehouseId == warehouseId);
        }

        if (search.Length > 0)
        {
            locations = locations.Where(location =>
                location.Code.ToLower().Contains(search)
                || location.Name.ToLower().Contains(search));
        }

        var projected =
            from location in locations
            join warehouse in db.Warehouses.AsNoTracking()
                on new { location.CompanyId, Id = location.WarehouseId }
                equals new { warehouse.CompanyId, warehouse.Id }
            orderby warehouse.Code, location.Code
            select new WarehouseLocationResponse(
                location.Id, location.WarehouseId, warehouse.Code, location.ParentLocationId,
                location.Code, location.Name, location.IsActive);
        return ToPageAsync(projected, query, cancellationToken);
    }

    public async Task<MasterDataResult<WarehouseLocationResponse>> SaveWarehouseLocationAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        WarehouseLocationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<WarehouseLocationResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<WarehouseLocationResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var warehouse = await db.Warehouses.SingleOrDefaultAsync(value =>
            value.Id == request.WarehouseId && value.CompanyId == companyId, cancellationToken);
        if (warehouse is null || request.IsActive && !warehouse.IsActive)
        {
            return Invalid<WarehouseLocationResponse>(
                "warehouseId", "Choose an active warehouse belonging to this company.");
        }

        var location = id is { } existingId
            ? await db.WarehouseLocations.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && location is null)
        {
            return NotFound<WarehouseLocationResponse>();
        }

        if (location is not null
            && location.WarehouseId != request.WarehouseId
            && await db.WarehouseLocations.AnyAsync(value =>
                value.CompanyId == companyId && value.ParentLocationId == location.Id,
                cancellationToken))
        {
            return Conflict<WarehouseLocationResponse>(
                "A location with child locations cannot be moved to another warehouse.");
        }

        if (await db.WarehouseLocations.AnyAsync(value =>
                value.CompanyId == companyId
                && value.WarehouseId == request.WarehouseId
                && value.Code.ToUpper() == NormalizeCode(request.Code)
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<WarehouseLocationResponse>(
                "A location with this code already exists in the selected warehouse.");
        }

        if (request.ParentLocationId is { } parentId)
        {
            var visited = new HashSet<Guid>();
            var cursor = (Guid?)parentId;
            while (cursor is { } currentId)
            {
                if (id == currentId || !visited.Add(currentId))
                {
                    return Invalid<WarehouseLocationResponse>(
                        "parentLocationId", "Warehouse location hierarchy cannot contain a cycle.");
                }

                var parent = await db.WarehouseLocations.AsNoTracking()
                    .SingleOrDefaultAsync(value =>
                        value.Id == currentId
                        && value.CompanyId == companyId
                        && value.WarehouseId == request.WarehouseId,
                        cancellationToken);
                if (parent is null)
                {
                    return Invalid<WarehouseLocationResponse>(
                        "parentLocationId", "The parent must be in the selected warehouse.");
                }

                if (request.IsActive && !parent.IsActive)
                {
                    return Invalid<WarehouseLocationResponse>(
                        "parentLocationId", "An active location requires an active parent.");
                }

                cursor = parent.ParentLocationId;
            }
        }

        var created = location is null;
        location ??= new WarehouseLocation { Id = Guid.NewGuid(), CompanyId = companyId };
        location.WarehouseId = request.WarehouseId;
        location.ParentLocationId = request.ParentLocationId;
        location.Code = NormalizeCode(request.Code);
        location.Name = request.Name.Trim();
        location.IsActive = request.IsActive;
        if (created)
        {
            db.WarehouseLocations.Add(location);
        }

        return await PersistAsync(
            location, companyId, actor,
            $"masterdata.warehouse-location.{(created ? "created" : "updated")}",
            new
            {
                location.WarehouseId,
                location.ParentLocationId,
                location.Code,
                location.Name,
                location.IsActive,
            },
            () => new WarehouseLocationResponse(
                location.Id, warehouse.Id, warehouse.Code, location.ParentLocationId,
                location.Code, location.Name, location.IsActive),
            cancellationToken);
    }
}
