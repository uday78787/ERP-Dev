using Erp.Domain.MasterData;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.MasterData;

internal sealed partial class MasterDataService
{
    public Task<PageResponse<CustomerResponse>> ListCustomersAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var customers = db.Customers.AsNoTracking().Where(customer => customer.CompanyId == companyId);
        if (query.Active is { } active)
        {
            customers = customers.Where(customer => customer.IsActive == active);
        }

        if (search.Length > 0)
        {
            customers = customers.Where(customer =>
                customer.Code.ToLower().Contains(search)
                || customer.Name.ToLower().Contains(search)
                || (customer.LegalName != null && customer.LegalName.ToLower().Contains(search))
                || (customer.Email != null && customer.Email.ToLower().Contains(search)));
        }

        return ToPageAsync(customers.OrderBy(customer => customer.Code).Select(customer =>
            new CustomerResponse(
                customer.Id, customer.Code, customer.Name, customer.LegalName, customer.Email,
                customer.Phone, customer.AddressLine1, customer.AddressLine2, customer.City,
                customer.StateOrProvince, customer.PostalCode, customer.CountryCode,
                customer.IsActive, customer.CreatedAt)), query, cancellationToken);
    }

    public async Task<MasterDataResult<CustomerResponse>> SaveCustomerAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        CustomerRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<CustomerResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<CustomerResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var code = NormalizeCode(request.Code);
        var customer = id is { } existingId
            ? await db.Customers.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && customer is null)
        {
            return NotFound<CustomerResponse>();
        }

        if (await db.Customers.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<CustomerResponse>("A customer with this code already exists.");
        }

        var created = customer is null;
        customer ??= new Customer { Id = Guid.NewGuid(), CompanyId = companyId };
        customer.Code = code;
        customer.Name = request.Name.Trim();
        customer.LegalName = NormalizeOptional(request.LegalName);
        customer.Email = NormalizeOptional(request.Email);
        customer.Phone = NormalizeOptional(request.Phone);
        customer.AddressLine1 = NormalizeOptional(request.AddressLine1);
        customer.AddressLine2 = NormalizeOptional(request.AddressLine2);
        customer.City = NormalizeOptional(request.City);
        customer.StateOrProvince = NormalizeOptional(request.StateOrProvince);
        customer.PostalCode = NormalizeOptional(request.PostalCode);
        customer.CountryCode = NormalizeOptional(request.CountryCode)?.ToUpperInvariant();
        customer.IsActive = request.IsActive;
        if (created)
        {
            db.Customers.Add(customer);
        }

        return await PersistAsync(
            customer, companyId, actor,
            $"masterdata.customer.{(created ? "created" : "updated")}",
            new { customer.Code, customer.Name, customer.IsActive },
            () => new CustomerResponse(
                customer.Id, customer.Code, customer.Name, customer.LegalName, customer.Email,
                customer.Phone, customer.AddressLine1, customer.AddressLine2, customer.City,
                customer.StateOrProvince, customer.PostalCode, customer.CountryCode,
                customer.IsActive, customer.CreatedAt),
            cancellationToken);
    }

    public Task<PageResponse<SupplierResponse>> ListSuppliersAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var suppliers = db.Suppliers.AsNoTracking().Where(supplier => supplier.CompanyId == companyId);
        if (query.Active is { } active)
        {
            suppliers = suppliers.Where(supplier => supplier.IsActive == active);
        }

        if (search.Length > 0)
        {
            suppliers = suppliers.Where(supplier =>
                supplier.Code.ToLower().Contains(search)
                || supplier.Name.ToLower().Contains(search)
                || (supplier.LegalName != null && supplier.LegalName.ToLower().Contains(search))
                || (supplier.Email != null && supplier.Email.ToLower().Contains(search)));
        }

        return ToPageAsync(suppliers.OrderBy(supplier => supplier.Code).Select(supplier =>
            new SupplierResponse(
                supplier.Id, supplier.Code, supplier.Name, supplier.LegalName, supplier.Email,
                supplier.Phone, supplier.AddressLine1, supplier.AddressLine2, supplier.City,
                supplier.StateOrProvince, supplier.PostalCode, supplier.CountryCode,
                supplier.IsActive, supplier.CreatedAt)), query, cancellationToken);
    }

    public async Task<MasterDataResult<SupplierResponse>> SaveSupplierAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        SupplierRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<SupplierResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<SupplierResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var code = NormalizeCode(request.Code);
        var supplier = id is { } existingId
            ? await db.Suppliers.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && supplier is null)
        {
            return NotFound<SupplierResponse>();
        }

        if (await db.Suppliers.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<SupplierResponse>("A supplier with this code already exists.");
        }

        var created = supplier is null;
        supplier ??= new Supplier { Id = Guid.NewGuid(), CompanyId = companyId };
        supplier.Code = code;
        supplier.Name = request.Name.Trim();
        supplier.LegalName = NormalizeOptional(request.LegalName);
        supplier.Email = NormalizeOptional(request.Email);
        supplier.Phone = NormalizeOptional(request.Phone);
        supplier.AddressLine1 = NormalizeOptional(request.AddressLine1);
        supplier.AddressLine2 = NormalizeOptional(request.AddressLine2);
        supplier.City = NormalizeOptional(request.City);
        supplier.StateOrProvince = NormalizeOptional(request.StateOrProvince);
        supplier.PostalCode = NormalizeOptional(request.PostalCode);
        supplier.CountryCode = NormalizeOptional(request.CountryCode)?.ToUpperInvariant();
        supplier.IsActive = request.IsActive;
        if (created)
        {
            db.Suppliers.Add(supplier);
        }

        return await PersistAsync(
            supplier, companyId, actor,
            $"masterdata.supplier.{(created ? "created" : "updated")}",
            new { supplier.Code, supplier.Name, supplier.IsActive },
            () => new SupplierResponse(
                supplier.Id, supplier.Code, supplier.Name, supplier.LegalName, supplier.Email,
                supplier.Phone, supplier.AddressLine1, supplier.AddressLine2, supplier.City,
                supplier.StateOrProvince, supplier.PostalCode, supplier.CountryCode,
                supplier.IsActive, supplier.CreatedAt),
            cancellationToken);
    }

    public Task<PageResponse<ItemCategoryResponse>> ListItemCategoriesAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var categories = db.ItemCategories.AsNoTracking()
            .Where(category => category.CompanyId == companyId);
        if (query.Active is { } active)
        {
            categories = categories.Where(category => category.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            categories = categories.Where(category => category.CategoryType == query.Type);
        }

        if (search.Length > 0)
        {
            categories = categories.Where(category =>
                category.Code.ToLower().Contains(search)
                || category.Name.ToLower().Contains(search)
                || (category.Description != null && category.Description.ToLower().Contains(search)));
        }

        return ToPageAsync(categories.OrderBy(category => category.Code).Select(category =>
            new ItemCategoryResponse(
                category.Id, category.Code, category.Name, category.TrackingMode,
                category.CategoryType, category.Description, category.IsActive)),
            query, cancellationToken);
    }

    public async Task<MasterDataResult<ItemCategoryResponse>> SaveItemCategoryAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        ItemCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<ItemCategoryResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<ItemCategoryResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var code = NormalizeCode(request.Code);
        var category = id is { } existingId
            ? await db.ItemCategories.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && category is null)
        {
            return NotFound<ItemCategoryResponse>();
        }

        if (await db.ItemCategories.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<ItemCategoryResponse>("An item category with this code already exists.");
        }

        if (category is not null
            && (category.CategoryType != request.CategoryType
                || category.TrackingMode != request.TrackingMode)
            && await db.Items.AnyAsync(item => item.CompanyId == companyId
                && item.CategoryId == category.Id, cancellationToken))
        {
            return Conflict<ItemCategoryResponse>(
                "Category type or tracking mode cannot change while items reference this category.");
        }

        var created = category is null;
        category ??= new ItemCategory { Id = Guid.NewGuid(), CompanyId = companyId };
        category.Code = code;
        category.Name = request.Name.Trim();
        category.TrackingMode = request.TrackingMode;
        category.CategoryType = request.CategoryType;
        category.Description = NormalizeOptional(request.Description);
        category.IsActive = request.IsActive;
        if (created)
        {
            db.ItemCategories.Add(category);
        }

        return await PersistAsync(
            category, companyId, actor,
            $"masterdata.item-category.{(created ? "created" : "updated")}",
            new
            {
                category.Code,
                category.Name,
                category.CategoryType,
                category.TrackingMode,
                category.IsActive,
            },
            () => new ItemCategoryResponse(
                category.Id, category.Code, category.Name, category.TrackingMode,
                category.CategoryType, category.Description, category.IsActive),
            cancellationToken);
    }

    public Task<PageResponse<UnitOfMeasureResponse>> ListUnitsAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var units = db.UnitsOfMeasure.AsNoTracking()
            .Where(unit => unit.CompanyId == companyId);
        if (query.Active is { } active)
        {
            units = units.Where(unit => unit.IsActive == active);
        }

        if (search.Length > 0)
        {
            units = units.Where(unit =>
                unit.Code.ToLower().Contains(search)
                || unit.Name.ToLower().Contains(search)
                || unit.Dimension.ToLower().Contains(search));
        }

        return ToPageAsync(units.OrderBy(unit => unit.Code).Select(unit =>
            new UnitOfMeasureResponse(
                unit.Id, unit.Code, unit.Name, unit.Dimension, unit.DecimalPlaces, unit.IsActive)),
            query, cancellationToken);
    }

    public async Task<MasterDataResult<UnitOfMeasureResponse>> SaveUnitAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        UnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<UnitOfMeasureResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<UnitOfMeasureResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var code = NormalizeCode(request.Code);
        var unit = id is { } existingId
            ? await db.UnitsOfMeasure.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && unit is null)
        {
            return NotFound<UnitOfMeasureResponse>();
        }

        if (await db.UnitsOfMeasure.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<UnitOfMeasureResponse>("A unit of measure with this code already exists.");
        }

        if (unit is not null
            && (unit.Dimension != request.Dimension.Trim()
                || unit.DecimalPlaces != request.DecimalPlaces)
            && (await db.Items.AnyAsync(item => item.CompanyId == companyId
                    && (item.StockUnitId == unit.Id || item.PurchaseUnitId == unit.Id),
                cancellationToken)
                || await db.BomComponents.AnyAsync(component =>
                    component.CompanyId == companyId && component.UnitOfMeasureId == unit.Id,
                    cancellationToken)))
        {
            return Conflict<UnitOfMeasureResponse>(
                "Dimension and precision cannot change while items or BOMs reference this unit.");
        }

        var created = unit is null;
        unit ??= new UnitOfMeasure { Id = Guid.NewGuid(), CompanyId = companyId };
        unit.Code = code;
        unit.Name = request.Name.Trim();
        unit.Dimension = request.Dimension.Trim();
        unit.DecimalPlaces = request.DecimalPlaces;
        unit.IsActive = request.IsActive;
        if (created)
        {
            db.UnitsOfMeasure.Add(unit);
        }

        return await PersistAsync(
            unit, companyId, actor,
            $"masterdata.unit-of-measure.{(created ? "created" : "updated")}",
            new { unit.Code, unit.Name, unit.Dimension, unit.DecimalPlaces, unit.IsActive },
            () => new UnitOfMeasureResponse(
                unit.Id, unit.Code, unit.Name, unit.Dimension, unit.DecimalPlaces, unit.IsActive),
            cancellationToken);
    }
}
