using Erp.Domain.MasterData;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.MasterData;

internal sealed partial class MasterDataService
{
    public Task<PageResponse<ItemResponse>> ListItemsAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var items = db.Items.AsNoTracking().Where(item => item.CompanyId == companyId);
        if (query.Active is { } active)
        {
            items = items.Where(item => item.IsActive == active);
        }

        if (query.ParentId is { } categoryId)
        {
            items = items.Where(item => item.CategoryId == categoryId);
        }

        if (search.Length > 0)
        {
            items = items.Where(item =>
                item.Code.ToLower().Contains(search) || item.Name.ToLower().Contains(search));
        }

        var projected =
            from item in items
            join category in db.ItemCategories.AsNoTracking()
                on new { item.CompanyId, Id = item.CategoryId }
                equals new { category.CompanyId, category.Id }
            join stockUnit in db.UnitsOfMeasure.AsNoTracking()
                on new { item.CompanyId, Id = item.StockUnitId }
                equals new { stockUnit.CompanyId, stockUnit.Id }
            join purchaseUnit in db.UnitsOfMeasure.AsNoTracking()
                on new { item.CompanyId, Id = item.PurchaseUnitId }
                equals new { purchaseUnit.CompanyId, purchaseUnit.Id }
            orderby item.Code
            select new ItemResponse(
                item.Id, item.Code, item.Name, item.CategoryId, category.Code, category.Name,
                item.StockUnitId, stockUnit.Code, item.PurchaseUnitId, purchaseUnit.Code,
                item.PurchaseToStockFactor, item.Description, item.IsActive, item.CreatedAt);
        return ToPageAsync(projected, query, cancellationToken);
    }

    public async Task<MasterDataResult<ItemResponse>> SaveItemAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        ItemRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<ItemResponse>(errors);
        }

        if (request.PurchaseToStockFactor <= 0m
            || request.PurchaseToStockFactor > 999999999999m)
        {
            return Invalid<ItemResponse>("purchaseToStockFactor", "The conversion factor must be positive.");
        }

        var item = id is { } existingId
            ? await db.Items.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && item is null)
        {
            return NotFound<ItemResponse>();
        }

        var category = await db.ItemCategories.SingleOrDefaultAsync(
            value => value.Id == request.CategoryId && value.CompanyId == companyId,
            cancellationToken);
        var units = await db.UnitsOfMeasure.Where(value => value.CompanyId == companyId
                && (value.Id == request.StockUnitId || value.Id == request.PurchaseUnitId))
            .ToListAsync(cancellationToken);
        var stockUnit = units.SingleOrDefault(value => value.Id == request.StockUnitId);
        var purchaseUnit = units.SingleOrDefault(value => value.Id == request.PurchaseUnitId);
        if (category is null || stockUnit is null || purchaseUnit is null)
        {
            return Invalid<ItemResponse>(
                "references", "Choose a category and units belonging to this company.");
        }

        if (request.IsActive && (!category.IsActive || !stockUnit.IsActive || !purchaseUnit.IsActive))
        {
            return Invalid<ItemResponse>(
                "references", "Active items require active categories and units of measure.");
        }

        if (!string.Equals(stockUnit.Dimension, purchaseUnit.Dimension, StringComparison.Ordinal))
        {
            return Invalid<ItemResponse>(
                "purchaseUnitId", "Purchase and stock units must use the same dimension.");
        }

        var code = request.Code is null && item is not null
            ? item.Code
            : NormalizeOptional(request.Code) is { } providedCode
                ? NormalizeCode(providedCode)
                : null;
        var codeRule = default(ItemCodeRule);
        if (code is null)
        {
            codeRule = await db.ItemCodeRules.SingleOrDefaultAsync(
                rule => rule.CompanyId == companyId, cancellationToken);
            codeRule ??= new ItemCodeRule { Id = Guid.NewGuid(), CompanyId = companyId };
            if (!codeRule.IsActive)
            {
                return Conflict<ItemResponse>("Automatic item-code generation is disabled for this company.");
            }

            code = FormatItemCode(codeRule);
            codeRule.NextNumber++;
            codeRule.Revision++;
            if (db.Entry(codeRule).State == EntityState.Detached)
            {
                db.ItemCodeRules.Add(codeRule);
            }
        }
        else if (!IsValidCode(code))
        {
            return Invalid<ItemResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        if (await db.Items.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<ItemResponse>("An item with this code already exists.");
        }

        var created = item is null;
        item ??= new Item { Id = Guid.NewGuid(), CompanyId = companyId };
        item.Code = code;
        item.Name = request.Name.Trim();
        item.CategoryId = request.CategoryId;
        item.StockUnitId = request.StockUnitId;
        item.PurchaseUnitId = request.PurchaseUnitId;
        item.PurchaseToStockFactor = request.PurchaseToStockFactor;
        item.Description = NormalizeOptional(request.Description);
        item.IsActive = request.IsActive;
        if (created)
        {
            db.Items.Add(item);
        }

        return await PersistAsync(
            item, companyId, actor,
            $"masterdata.item.{(created ? "created" : "updated")}",
            new
            {
                item.Code,
                item.Name,
                item.CategoryId,
                item.StockUnitId,
                item.PurchaseUnitId,
                item.PurchaseToStockFactor,
                item.IsActive,
            },
            () => new ItemResponse(
                item.Id, item.Code, item.Name, category.Id, category.Code, category.Name,
                stockUnit.Id, stockUnit.Code, purchaseUnit.Id, purchaseUnit.Code,
                item.PurchaseToStockFactor, item.Description, item.IsActive, item.CreatedAt),
            cancellationToken);
    }

    public async Task<ItemCodeRuleResponse> GetItemCodeRuleAsync(
        Guid companyId, CancellationToken cancellationToken)
    {
        var rule = await db.ItemCodeRules.AsNoTracking()
            .SingleOrDefaultAsync(value => value.CompanyId == companyId, cancellationToken);
        rule ??= new ItemCodeRule { CompanyId = companyId };
        return MapItemCodeRule(rule);
    }

    public async Task<MasterDataResult<ItemCodeRuleResponse>> SaveItemCodeRuleAsync(
        Guid companyId,
        MasterDataActor actor,
        ItemCodeRuleRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<ItemCodeRuleResponse>(errors);
        }

        if (!IsValidCode(request.Prefix, 20)
            || request.Separator.Any(character =>
                character is not ('_' or '-' or '.')))
        {
            return Invalid<ItemCodeRuleResponse>(
                "prefix", "Prefix and separator may contain only letters, numbers, dots, underscores, and hyphens.");
        }

        var rule = await db.ItemCodeRules.SingleOrDefaultAsync(
            value => value.CompanyId == companyId, cancellationToken);
        if (rule is not null && request.ExpectedRevision != rule.Revision)
        {
            return Conflict<ItemCodeRuleResponse>(
                "The code rule changed. Reload it before saving your changes.");
        }

        if (rule is not null && request.NextNumber < rule.NextNumber)
        {
            return Invalid<ItemCodeRuleResponse>(
                "nextNumber", "The next number cannot be reduced below its current value.");
        }

        var created = rule is null;
        rule ??= new ItemCodeRule { Id = Guid.NewGuid(), CompanyId = companyId };
        rule.Prefix = NormalizeCode(request.Prefix);
        rule.Separator = request.Separator;
        rule.Padding = request.Padding;
        rule.NextNumber = request.NextNumber;
        rule.IsActive = true;
        if (created)
        {
            db.ItemCodeRules.Add(rule);
        }
        else
        {
            rule.Revision++;
        }

        return await PersistAsync(
            rule, companyId, actor,
            $"masterdata.item-code-rule.{(created ? "created" : "updated")}",
            new { rule.Prefix, rule.Separator, rule.Padding, rule.NextNumber, rule.Revision },
            () => MapItemCodeRule(rule),
            cancellationToken);
    }

    private static string FormatItemCode(ItemCodeRule rule) =>
        string.Concat(
            rule.Prefix,
            rule.Separator,
            rule.NextNumber.ToString($"D{rule.Padding}", System.Globalization.CultureInfo.InvariantCulture));

    private static ItemCodeRuleResponse MapItemCodeRule(ItemCodeRule rule) =>
        new(rule.Prefix, rule.Separator, rule.Padding, rule.NextNumber, rule.Revision,
            rule.IsActive, FormatItemCode(rule));
}
