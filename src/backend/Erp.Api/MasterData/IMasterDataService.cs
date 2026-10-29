namespace Erp.Api.MasterData;

internal interface IMasterDataService
{
    Task<PageResponse<CustomerResponse>> ListCustomersAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<CustomerResponse>> SaveCustomerAsync(Guid companyId, MasterDataActor actor, Guid? id, CustomerRequest request, CancellationToken cancellationToken);
    Task<PageResponse<SupplierResponse>> ListSuppliersAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<SupplierResponse>> SaveSupplierAsync(Guid companyId, MasterDataActor actor, Guid? id, SupplierRequest request, CancellationToken cancellationToken);
    Task<PageResponse<ItemCategoryResponse>> ListItemCategoriesAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<ItemCategoryResponse>> SaveItemCategoryAsync(Guid companyId, MasterDataActor actor, Guid? id, ItemCategoryRequest request, CancellationToken cancellationToken);
    Task<PageResponse<UnitOfMeasureResponse>> ListUnitsAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<UnitOfMeasureResponse>> SaveUnitAsync(Guid companyId, MasterDataActor actor, Guid? id, UnitOfMeasureRequest request, CancellationToken cancellationToken);
    Task<PageResponse<ItemResponse>> ListItemsAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<ItemResponse>> SaveItemAsync(Guid companyId, MasterDataActor actor, Guid? id, ItemRequest request, CancellationToken cancellationToken);
    Task<ItemCodeRuleResponse> GetItemCodeRuleAsync(Guid companyId, CancellationToken cancellationToken);
    Task<MasterDataResult<ItemCodeRuleResponse>> SaveItemCodeRuleAsync(Guid companyId, MasterDataActor actor, ItemCodeRuleRequest request, CancellationToken cancellationToken);
    Task<PageResponse<WarehouseResponse>> ListWarehousesAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<WarehouseResponse>> SaveWarehouseAsync(Guid companyId, MasterDataActor actor, Guid? id, WarehouseRequest request, CancellationToken cancellationToken);
    Task<PageResponse<WarehouseLocationResponse>> ListWarehouseLocationsAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<WarehouseLocationResponse>> SaveWarehouseLocationAsync(Guid companyId, MasterDataActor actor, Guid? id, WarehouseLocationRequest request, CancellationToken cancellationToken);
    Task<PageResponse<ProductResponse>> ListProductsAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<ProductResponse>> SaveProductAsync(Guid companyId, MasterDataActor actor, Guid? id, ProductRequest request, CancellationToken cancellationToken);
    Task<PageResponse<ProductVariantResponse>> ListVariantsAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<ProductVariantResponse>> SaveVariantAsync(Guid companyId, MasterDataActor actor, Guid? id, ProductVariantRequest request, CancellationToken cancellationToken);
    Task<PageResponse<MachineResponse>> ListMachinesAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<MachineResponse>> SaveMachineAsync(Guid companyId, MasterDataActor actor, Guid? id, MachineRequest request, CancellationToken cancellationToken);
    Task<PageResponse<ProductionOperationResponse>> ListOperationsAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<ProductionOperationResponse>> SaveOperationAsync(Guid companyId, MasterDataActor actor, Guid? id, ProductionOperationRequest request, CancellationToken cancellationToken);
    Task<PageResponse<BomResponse>> ListBomsAsync(Guid companyId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<BomResponse>> SaveBomAsync(Guid companyId, MasterDataActor actor, Guid? id, BomRequest request, CancellationToken cancellationToken);
    Task<PageResponse<BomVersionResponse>> ListBomVersionsAsync(Guid companyId, Guid bomId, MasterDataQuery query, CancellationToken cancellationToken);
    Task<MasterDataResult<BomVersionResponse>> CreateBomVersionAsync(Guid companyId, MasterDataActor actor, Guid bomId, BomVersionRequest request, CancellationToken cancellationToken);
    Task<MasterDataResult<BomVersionResponse>> SaveBomComponentsAsync(Guid companyId, MasterDataActor actor, Guid versionId, IReadOnlyList<BomComponentRequest> request, CancellationToken cancellationToken);
    Task<MasterDataResult<BomVersionResponse>> ApproveBomVersionAsync(Guid companyId, MasterDataActor actor, Guid versionId, CancellationToken cancellationToken);
    Task<MasterDataResult<bool>> SetActiveAsync(Guid companyId, MasterDataActor actor, string resource, Guid id, bool isActive, CancellationToken cancellationToken);
}
