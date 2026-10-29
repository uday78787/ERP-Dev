using Erp.Domain.MasterData;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.MasterData;

internal sealed partial class MasterDataService
{
    public Task<PageResponse<ProductResponse>> ListProductsAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var products = db.Products.AsNoTracking().Where(product => product.CompanyId == companyId);
        if (query.Active is { } active)
        {
            products = products.Where(product => product.IsActive == active);
        }

        if (search.Length > 0)
        {
            products = products.Where(product =>
                product.Code.ToLower().Contains(search) || product.Name.ToLower().Contains(search));
        }

        return ToPageAsync(products.OrderBy(product => product.Code).Select(product =>
            new ProductResponse(
                product.Id, product.Code, product.Name, product.Description, product.IndustryCode,
                product.IsActive, product.CreatedAt)), query, cancellationToken);
    }

    public async Task<MasterDataResult<ProductResponse>> SaveProductAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<ProductResponse>(errors);
        }

        if (!IsValidCode(request.Code)
            || string.IsNullOrWhiteSpace(request.IndustryCode)
            || request.IndustryCode.Trim().Length > 40)
        {
            return Invalid<ProductResponse>(
                "product", "A valid product code and industry code are required.");
        }

        var code = NormalizeCode(request.Code);
        var product = id is { } existingId
            ? await db.Products.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && product is null)
        {
            return NotFound<ProductResponse>();
        }

        if (await db.Products.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<ProductResponse>("A product or style with this code already exists.");
        }

        if (product is not null && product.IndustryCode != request.IndustryCode.Trim()
            && await db.ProductVariants.AnyAsync(value =>
                value.CompanyId == companyId && value.ProductId == product.Id, cancellationToken))
        {
            return Conflict<ProductResponse>(
                "Industry code cannot change after variants have been created.");
        }

        var created = product is null;
        product ??= new Product { Id = Guid.NewGuid(), CompanyId = companyId };
        product.Code = code;
        product.Name = request.Name.Trim();
        product.Description = NormalizeOptional(request.Description);
        product.IndustryCode = request.IndustryCode.Trim().ToLowerInvariant();
        product.IsActive = request.IsActive;
        if (created)
        {
            db.Products.Add(product);
        }

        return await PersistAsync(
            product, companyId, actor,
            $"masterdata.product.{(created ? "created" : "updated")}",
            new
            {
                product.Code,
                product.Name,
                product.IndustryCode,
                product.IsActive,
            },
            () => new ProductResponse(
                product.Id, product.Code, product.Name, product.Description, product.IndustryCode,
                product.IsActive, product.CreatedAt),
            cancellationToken);
    }

    public Task<PageResponse<ProductVariantResponse>> ListVariantsAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var variants = db.ProductVariants.AsNoTracking()
            .Where(variant => variant.CompanyId == companyId);
        if (query.Active is { } active)
        {
            variants = variants.Where(variant => variant.IsActive == active);
        }

        if (query.ProductId is { } productId)
        {
            variants = variants.Where(variant => variant.ProductId == productId);
        }

        if (!string.IsNullOrWhiteSpace(query.Size))
        {
            var size = SearchText(query.Size);
            variants = variants.Where(variant =>
                variant.SizeCode != null && variant.SizeCode.ToLower() == size);
        }

        if (!string.IsNullOrWhiteSpace(query.Color))
        {
            var color = SearchText(query.Color);
            variants = variants.Where(variant =>
                variant.ColorCode != null && variant.ColorCode.ToLower() == color);
        }

        if (search.Length > 0)
        {
            variants = variants.Where(variant =>
                variant.Code.ToLower().Contains(search)
                || variant.Name.ToLower().Contains(search)
                || (variant.SizeCode != null && variant.SizeCode.ToLower().Contains(search))
                || (variant.ColorCode != null && variant.ColorCode.ToLower().Contains(search))
                || (variant.ColorName != null && variant.ColorName.ToLower().Contains(search)));
        }

        var projected =
            from variant in variants
            join product in db.Products.AsNoTracking()
                on new { variant.CompanyId, Id = variant.ProductId }
                equals new { product.CompanyId, product.Id }
            join item in db.Items.AsNoTracking()
                on new { variant.CompanyId, Id = variant.ItemId }
                equals new { item.CompanyId, item.Id }
            orderby product.Code, variant.SizeCode, variant.ColorCode, variant.Code
            select new ProductVariantResponse(
                variant.Id, variant.ProductId, product.Code, variant.ItemId, item.Code,
                variant.Code, variant.Name, variant.SizeCode, variant.ColorCode,
                variant.ColorName, variant.IsActive);
        return ToPageAsync(projected, query, cancellationToken);
    }

    public async Task<MasterDataResult<ProductVariantResponse>> SaveVariantAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        ProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<ProductVariantResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<ProductVariantResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var code = NormalizeCode(request.Code);
        var product = await db.Products.SingleOrDefaultAsync(value =>
            value.Id == request.ProductId && value.CompanyId == companyId, cancellationToken);
        var item = await db.Items.SingleOrDefaultAsync(value =>
            value.Id == request.ItemId && value.CompanyId == companyId, cancellationToken);
        if (product is null || item is null)
        {
            return Invalid<ProductVariantResponse>(
                "references", "Choose a product and stock item belonging to this company.");
        }

        if (request.IsActive && (!product.IsActive || !item.IsActive))
        {
            return Invalid<ProductVariantResponse>(
                "references", "Active variants require an active product and item.");
        }

        var isApparel = product.IndustryCode is "apparel" or "garment";
        if (isApparel && (string.IsNullOrWhiteSpace(request.SizeCode)
            || string.IsNullOrWhiteSpace(request.ColorCode)))
        {
            return Invalid<ProductVariantResponse>(
                "sizeCode", "Garment variants require both a size and a color.");
        }

        if (request.SizeCode is { Length: > 40 }
            || request.ColorCode is { Length: > 40 }
            || request.ColorName is { Length: > 100 })
        {
            return Invalid<ProductVariantResponse>("attributes", "Size and color values exceed their allowed lengths.");
        }

        var variant = id is { } existingId
            ? await db.ProductVariants.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && variant is null)
        {
            return NotFound<ProductVariantResponse>();
        }

        if (await db.ProductVariants.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<ProductVariantResponse>("A product variant with this code already exists.");
        }

        if (await db.ProductVariants.AnyAsync(value =>
                value.CompanyId == companyId
                && value.ItemId == request.ItemId
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<ProductVariantResponse>(
                "Each stock item can be assigned to only one product variant.");
        }

        var sizeCode = NormalizeOptional(request.SizeCode)?.ToUpperInvariant();
        var colorCode = NormalizeOptional(request.ColorCode)?.ToUpperInvariant();
        if (sizeCode is not null && colorCode is not null
            && await db.ProductVariants.AnyAsync(value =>
                value.CompanyId == companyId
                && value.ProductId == request.ProductId
                && value.SizeCode == sizeCode
                && value.ColorCode == colorCode
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<ProductVariantResponse>(
                "This style already has a variant for the selected size and color.");
        }

        var created = variant is null;
        variant ??= new ProductVariant { Id = Guid.NewGuid(), CompanyId = companyId };
        variant.ProductId = request.ProductId;
        variant.ItemId = request.ItemId;
        variant.Code = code;
        variant.Name = request.Name.Trim();
        variant.SizeCode = sizeCode;
        variant.ColorCode = colorCode;
        variant.ColorName = NormalizeOptional(request.ColorName);
        variant.IsActive = request.IsActive;
        if (created)
        {
            db.ProductVariants.Add(variant);
        }

        return await PersistAsync(
            variant, companyId, actor,
            $"masterdata.product-variant.{(created ? "created" : "updated")}",
            new
            {
                variant.ProductId,
                variant.ItemId,
                variant.Code,
                variant.SizeCode,
                variant.ColorCode,
                variant.IsActive,
            },
            () => new ProductVariantResponse(
                variant.Id, product.Id, product.Code, item.Id, item.Code, variant.Code,
                variant.Name, variant.SizeCode, variant.ColorCode, variant.ColorName,
                variant.IsActive),
            cancellationToken);
    }
}
