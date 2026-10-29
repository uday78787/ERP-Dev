using Erp.Domain.MasterData;
using Erp.Domain.Platform;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.MasterData;

internal sealed partial class MasterDataService
{
    public Task<PageResponse<BomResponse>> ListBomsAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var boms = db.Boms.AsNoTracking().Where(bom => bom.CompanyId == companyId);
        if (query.Active is { } active)
        {
            boms = boms.Where(bom => bom.IsActive == active);
        }

        if (query.ProductId is { } productId)
        {
            boms = boms.Where(bom => bom.ProductId == productId);
        }

        if (search.Length > 0)
        {
            boms = boms.Where(bom =>
                bom.Code.ToLower().Contains(search)
                || bom.Name.ToLower().Contains(search)
                || db.Products.Any(product =>
                    product.CompanyId == companyId
                    && product.Id == bom.ProductId
                    && (product.Code.ToLower().Contains(search)
                        || product.Name.ToLower().Contains(search))));
        }

        var projected =
            from bom in boms
            join product in db.Products.AsNoTracking()
                on new { bom.CompanyId, Id = bom.ProductId }
                equals new { product.CompanyId, product.Id }
            orderby bom.Code
            select new BomResponse(
                bom.Id, bom.ProductId, product.Code, bom.Code, bom.Name, bom.IsActive,
                bom.Versions.Count);
        return ToPageAsync(projected, query, cancellationToken);
    }

    public async Task<MasterDataResult<BomResponse>> SaveBomAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        BomRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<BomResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<BomResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var product = await db.Products.SingleOrDefaultAsync(value =>
            value.Id == request.ProductId && value.CompanyId == companyId, cancellationToken);
        if (product is null || request.IsActive && !product.IsActive)
        {
            return Invalid<BomResponse>(
                "productId", "Choose an active product belonging to this company.");
        }

        var code = NormalizeCode(request.Code);
        var bom = id is { } existingId
            ? await db.Boms.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && bom is null)
        {
            return NotFound<BomResponse>();
        }

        if (await db.Boms.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<BomResponse>("A BOM with this code already exists.");
        }

        var created = bom is null;
        bom ??= new Bom { Id = Guid.NewGuid(), CompanyId = companyId };
        bom.ProductId = request.ProductId;
        bom.Code = code;
        bom.Name = request.Name.Trim();
        bom.IsActive = request.IsActive;
        if (created)
        {
            db.Boms.Add(bom);
        }

        return await PersistAsync(
            bom, companyId, actor,
            $"masterdata.bom.{(created ? "created" : "updated")}",
            new { bom.ProductId, bom.Code, bom.Name, bom.IsActive },
            () => new BomResponse(
                bom.Id, product.Id, product.Code, bom.Code, bom.Name, bom.IsActive,
                bom.Versions.Count),
            cancellationToken);
    }

    public async Task<PageResponse<BomVersionResponse>> ListBomVersionsAsync(
        Guid companyId,
        Guid bomId,
        MasterDataQuery query,
        CancellationToken cancellationToken)
    {
        var versions = db.BomVersions.AsNoTracking()
            .Where(version => version.CompanyId == companyId && version.BomId == bomId);
        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            versions = versions.Where(version => version.Status == query.Type);
        }

        var totalCount = await versions.CountAsync(cancellationToken);
        var pageVersions = await versions.OrderByDescending(version => version.VersionNumber)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var ids = pageVersions.Select(version => version.Id).ToArray();
        var components = await LoadBomComponentsAsync(companyId, ids, cancellationToken);
        var componentsByVersion = components.GroupBy(component => component.VersionId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<BomComponentResponse>)group
                .OrderBy(component => component.LineNumber)
                .Select(component => component.Response)
                .ToArray());
        var rows = pageVersions.Select(version => new BomVersionResponse(
            version.Id,
            version.BomId,
            version.VersionNumber,
            version.Status,
            version.EffectiveFrom,
            version.EffectiveTo,
            version.ApprovedBySubject,
            version.ApprovedAt,
            componentsByVersion.GetValueOrDefault(version.Id, []))).ToArray();
        return new PageResponse<BomVersionResponse>(rows, query.Page, query.PageSize, totalCount);
    }

    public async Task<MasterDataResult<BomVersionResponse>> CreateBomVersionAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid bomId,
        BomVersionRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<BomVersionResponse>(errors);
        }

        if (request.EffectiveTo is { } effectiveTo && effectiveTo < request.EffectiveFrom)
        {
            return Invalid<BomVersionResponse>(
                "effectiveTo", "Effective-to date must be on or after the effective-from date.");
        }

        var bom = await db.Boms.SingleOrDefaultAsync(value =>
            value.Id == bomId && value.CompanyId == companyId, cancellationToken);
        if (bom is null)
        {
            return NotFound<BomVersionResponse>();
        }

        if (!bom.IsActive)
        {
            return Conflict<BomVersionResponse>("A version cannot be created for an inactive BOM.");
        }

        var existingVersions = await db.BomVersions
            .Where(version => version.CompanyId == companyId && version.BomId == bomId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);
        var versionNumber = existingVersions.Count == 0 ? 1 : existingVersions[0].VersionNumber + 1;
        var version = new BomVersion
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BomId = bomId,
            VersionNumber = versionNumber,
            Status = BomVersionStatuses.Draft,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
        };
        db.BomVersions.Add(version);

        var priorVersion = existingVersions.FirstOrDefault();
        if (priorVersion is not null)
        {
            var priorComponents = await db.BomComponents.AsNoTracking()
                .Where(component => component.CompanyId == companyId
                    && component.BomVersionId == priorVersion.Id)
                .ToListAsync(cancellationToken);
            foreach (var component in priorComponents)
            {
                db.BomComponents.Add(new BomComponent
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    BomVersionId = version.Id,
                    LineNumber = component.LineNumber,
                    ItemId = component.ItemId,
                    UnitOfMeasureId = component.UnitOfMeasureId,
                    Quantity = component.Quantity,
                    WastePercent = component.WastePercent,
                    AlternativeGroupCode = component.AlternativeGroupCode,
                    IsOptional = component.IsOptional,
                });
            }
        }

        db.AuditEvents.Add(CreateAudit(
            companyId, actor, "masterdata.bom-version.created", nameof(BomVersion), version.Id,
            new { bomId, version.VersionNumber, version.EffectiveFrom, version.EffectiveTo }));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            var response = await MapBomVersionAsync(companyId, version, cancellationToken);
            return new MasterDataResult<BomVersionResponse>(
                response, StatusCodes.Status201Created, ResourceId: version.Id);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            logger.LogInformation(exception, "Concurrent BOM version creation rejected for {BomId}.", bomId);
            return Conflict<BomVersionResponse>("The BOM version changed concurrently. Reload and retry.");
        }
    }

    public async Task<MasterDataResult<BomVersionResponse>> SaveBomComponentsAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid versionId,
        IReadOnlyList<BomComponentRequest> request,
        CancellationToken cancellationToken)
    {
        var version = await db.BomVersions.SingleOrDefaultAsync(value =>
            value.Id == versionId && value.CompanyId == companyId, cancellationToken);
        if (version is null)
        {
            return NotFound<BomVersionResponse>();
        }

        if (version.Status != BomVersionStatuses.Draft)
        {
            return Conflict<BomVersionResponse>(
                "Approved or retired BOM versions are immutable. Create a new draft version.");
        }

        if (request.Count > 500)
        {
            return Invalid<BomVersionResponse>(
                "components", "A BOM version cannot contain more than 500 component lines.");
        }

        var errors = request.SelectMany((component, index) =>
                Validate(component).SelectMany(error => error.Value.Select(message =>
                    (field: $"components[{index}].{error.Key}", message))))
            .GroupBy(error => error.field, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.message).Distinct().ToArray(),
                StringComparer.Ordinal);
        if (errors.Count > 0)
        {
            return Invalid<BomVersionResponse>(errors);
        }

        if (request.Select(component => component.LineNumber).Distinct().Count() != request.Count)
        {
            return Invalid<BomVersionResponse>(
                "components", "Component line numbers must be unique within the version.");
        }

        var itemIds = request.Select(component => component.ItemId).Distinct().ToArray();
        var items = await db.Items.Where(item => item.CompanyId == companyId && itemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var unitIds = request.Select(component => component.UnitOfMeasureId).Distinct().ToArray();
        var units = await db.UnitsOfMeasure
            .Where(unit => unit.CompanyId == companyId && unitIds.Contains(unit.Id))
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        var stockUnitIds = items.Values.Select(item => item.StockUnitId).Distinct().ToArray();
        var stockUnits = await db.UnitsOfMeasure
            .Where(unit => unit.CompanyId == companyId && stockUnitIds.Contains(unit.Id))
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);

        foreach (var component in request)
        {
            if (!items.TryGetValue(component.ItemId, out var item)
                || !units.TryGetValue(component.UnitOfMeasureId, out var unit))
            {
                return Invalid<BomVersionResponse>(
                    "components", "Components must reference items and units in this company.");
            }

            if (!item.IsActive || !unit.IsActive)
            {
                return Invalid<BomVersionResponse>(
                    "components", "BOM components must reference active items and units.");
            }

            if (!stockUnits.TryGetValue(item.StockUnitId, out var stockUnit)
                || !string.Equals(unit.Dimension, stockUnit.Dimension, StringComparison.Ordinal))
            {
                return Invalid<BomVersionResponse>(
                    "components", "Component units must match the item's stock-unit dimension.");
            }
        }

        var priorComponents = await db.BomComponents
            .Where(component => component.CompanyId == companyId
                && component.BomVersionId == versionId)
            .ToListAsync(cancellationToken);
        db.BomComponents.RemoveRange(priorComponents);
        var replacement = request.OrderBy(component => component.LineNumber).Select(component =>
            new BomComponent
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BomVersionId = versionId,
                LineNumber = component.LineNumber,
                ItemId = component.ItemId,
                UnitOfMeasureId = component.UnitOfMeasureId,
                Quantity = component.Quantity,
                WastePercent = component.WastePercent,
                AlternativeGroupCode = NormalizeOptional(component.AlternativeGroupCode),
                IsOptional = component.IsOptional,
            }).ToArray();
        db.BomComponents.AddRange(replacement);
        db.AuditEvents.Add(CreateAudit(
            companyId, actor, "masterdata.bom-version.components_updated",
            nameof(BomVersion), version.Id,
            new { version.BomId, version.VersionNumber, componentCount = replacement.Length }));
        await db.SaveChangesAsync(cancellationToken);
        return new MasterDataResult<BomVersionResponse>(
            await MapBomVersionAsync(companyId, version, cancellationToken));
    }

    public async Task<MasterDataResult<BomVersionResponse>> ApproveBomVersionAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var version = await db.BomVersions.SingleOrDefaultAsync(value =>
            value.Id == versionId && value.CompanyId == companyId, cancellationToken);
        if (version is null)
        {
            return NotFound<BomVersionResponse>();
        }

        if (version.Status != BomVersionStatuses.Draft)
        {
            return Conflict<BomVersionResponse>("Only a draft BOM version can be approved.");
        }

        var componentCount = await db.BomComponents.CountAsync(component =>
            component.CompanyId == companyId && component.BomVersionId == versionId,
            cancellationToken);
        if (componentCount == 0)
        {
            return Invalid<BomVersionResponse>(
                "components", "A BOM version must contain at least one component before approval.");
        }

        if (await db.BomVersions.AnyAsync(existing =>
                existing.CompanyId == companyId
                && existing.BomId == version.BomId
                && existing.Id != version.Id
                && existing.Status == BomVersionStatuses.Approved
                && existing.EffectiveFrom <= (version.EffectiveTo ?? DateOnly.MaxValue)
                && version.EffectiveFrom <= (existing.EffectiveTo ?? DateOnly.MaxValue),
                cancellationToken))
        {
            return Conflict<BomVersionResponse>(
                "The effective period overlaps another approved BOM version.");
        }

        version.Status = BomVersionStatuses.Approved;
        version.ApprovedAt = DateTimeOffset.UtcNow;
        version.ApprovedBySubject = actor.Subject;
        db.AuditEvents.Add(CreateAudit(
            companyId, actor, "masterdata.bom-version.approved",
            nameof(BomVersion), version.Id,
            new { version.BomId, version.VersionNumber, version.ApprovedAt }));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new MasterDataResult<BomVersionResponse>(
                await MapBomVersionAsync(companyId, version, cancellationToken));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            logger.LogInformation(exception,
                "BOM version approval conflict for {BomId}, version {VersionNumber}.",
                version.BomId, version.VersionNumber);
            return Conflict<BomVersionResponse>(
                "The BOM version changed concurrently. Reload and retry.");
        }
    }

    private async Task<BomVersionResponse> MapBomVersionAsync(
        Guid companyId, BomVersion version, CancellationToken cancellationToken)
    {
        var components = await LoadBomComponentsAsync(
            companyId, [version.Id], cancellationToken);
        return new BomVersionResponse(
            version.Id,
            version.BomId,
            version.VersionNumber,
            version.Status,
            version.EffectiveFrom,
            version.EffectiveTo,
            version.ApprovedBySubject,
            version.ApprovedAt,
            components.OrderBy(component => component.LineNumber)
                .Select(component => component.Response)
                .ToArray());
    }

    private async Task<List<(Guid VersionId, int LineNumber, BomComponentResponse Response)>>
        LoadBomComponentsAsync(
            Guid companyId, Guid[] versionIds, CancellationToken cancellationToken)
    {
        if (versionIds.Length == 0)
        {
            return [];
        }

        var rows =
            from component in db.BomComponents.AsNoTracking()
            where component.CompanyId == companyId && versionIds.Contains(component.BomVersionId)
            join item in db.Items.AsNoTracking()
                on new { component.CompanyId, Id = component.ItemId }
                equals new { item.CompanyId, item.Id }
            join unit in db.UnitsOfMeasure.AsNoTracking()
                on new { component.CompanyId, Id = component.UnitOfMeasureId }
                equals new { unit.CompanyId, unit.Id }
            orderby component.LineNumber
            select new
            {
                component.BomVersionId,
                component.LineNumber,
                Response = new BomComponentResponse(
                    component.Id, component.LineNumber, item.Id, item.Code, item.Name,
                    unit.Id, unit.Code, component.Quantity, component.WastePercent,
                    component.AlternativeGroupCode, component.IsOptional),
            };
        var values = await rows.ToListAsync(cancellationToken);
        return values.Select(value =>
            (value.BomVersionId, value.LineNumber, value.Response)).ToList();
    }

    private static AuditEvent CreateAudit(
        Guid companyId,
        MasterDataActor actor,
        string action,
        string entityType,
        Guid entityId,
        object details) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ActorUserId = actor.UserId,
            ActorSubjectSnapshot = actor.Subject,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            CorrelationId = actor.CorrelationId,
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(details),
        };
}
