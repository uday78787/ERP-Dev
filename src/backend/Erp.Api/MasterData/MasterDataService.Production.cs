using Erp.Domain.MasterData;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.MasterData;

internal sealed partial class MasterDataService
{
    public Task<PageResponse<MachineResponse>> ListMachinesAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var machines = db.Machines.AsNoTracking().Where(machine => machine.CompanyId == companyId);
        if (query.Active is { } active)
        {
            machines = machines.Where(machine => machine.IsActive == active);
        }

        if (search.Length > 0)
        {
            machines = machines.Where(machine =>
                machine.Code.ToLower().Contains(search)
                || machine.Name.ToLower().Contains(search)
                || (machine.ProductionLine != null && machine.ProductionLine.ToLower().Contains(search)));
        }

        return ToPageAsync(machines.OrderBy(machine => machine.Code).Select(machine =>
            new MachineResponse(
                machine.Id, machine.Code, machine.Name, machine.Description,
                machine.ProductionLine, machine.IsActive)), query, cancellationToken);
    }

    public async Task<MasterDataResult<MachineResponse>> SaveMachineAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        MachineRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<MachineResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<MachineResponse>("code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var code = NormalizeCode(request.Code);
        var machine = id is { } existingId
            ? await db.Machines.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && machine is null)
        {
            return NotFound<MachineResponse>();
        }

        if (await db.Machines.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<MachineResponse>("A machine with this code already exists.");
        }

        var created = machine is null;
        machine ??= new Machine { Id = Guid.NewGuid(), CompanyId = companyId };
        machine.Code = code;
        machine.Name = request.Name.Trim();
        machine.Description = NormalizeOptional(request.Description);
        machine.ProductionLine = NormalizeOptional(request.ProductionLine);
        machine.IsActive = request.IsActive;
        if (created)
        {
            db.Machines.Add(machine);
        }

        return await PersistAsync(
            machine, companyId, actor,
            $"masterdata.machine.{(created ? "created" : "updated")}",
            new { machine.Code, machine.Name, machine.ProductionLine, machine.IsActive },
            () => new MachineResponse(
                machine.Id, machine.Code, machine.Name, machine.Description,
                machine.ProductionLine, machine.IsActive),
            cancellationToken);
    }

    public Task<PageResponse<ProductionOperationResponse>> ListOperationsAsync(
        Guid companyId, MasterDataQuery query, CancellationToken cancellationToken)
    {
        var search = SearchText(query.Search);
        var operations = db.ProductionOperations.AsNoTracking()
            .Where(operation => operation.CompanyId == companyId);
        if (query.Active is { } active)
        {
            operations = operations.Where(operation => operation.IsActive == active);
        }

        if (search.Length > 0)
        {
            operations = operations.Where(operation =>
                operation.Code.ToLower().Contains(search)
                || operation.Name.ToLower().Contains(search)
                || (operation.Description != null && operation.Description.ToLower().Contains(search)));
        }

        return ToPageAsync(operations.OrderBy(operation => operation.Code).Select(operation =>
            new ProductionOperationResponse(
                operation.Id, operation.Code, operation.Name, operation.Description,
                operation.IsOutsourced, operation.IsActive)), query, cancellationToken);
    }

    public async Task<MasterDataResult<ProductionOperationResponse>> SaveOperationAsync(
        Guid companyId,
        MasterDataActor actor,
        Guid? id,
        ProductionOperationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Invalid<ProductionOperationResponse>(errors);
        }

        if (!IsValidCode(request.Code))
        {
            return Invalid<ProductionOperationResponse>(
                "code", "Use 2-40 letters, numbers, dots, underscores, or hyphens.");
        }

        var code = NormalizeCode(request.Code);
        var operation = id is { } existingId
            ? await db.ProductionOperations.SingleOrDefaultAsync(
                value => value.Id == existingId && value.CompanyId == companyId, cancellationToken)
            : null;
        if (id is not null && operation is null)
        {
            return NotFound<ProductionOperationResponse>();
        }

        if (await db.ProductionOperations.AnyAsync(value =>
                value.CompanyId == companyId
                && value.Code.ToUpper() == code
                && (id == null || value.Id != id), cancellationToken))
        {
            return Conflict<ProductionOperationResponse>(
                "A production operation with this code already exists.");
        }

        var created = operation is null;
        operation ??= new ProductionOperation { Id = Guid.NewGuid(), CompanyId = companyId };
        operation.Code = code;
        operation.Name = request.Name.Trim();
        operation.Description = NormalizeOptional(request.Description);
        operation.IsOutsourced = request.IsOutsourced;
        operation.IsActive = request.IsActive;
        if (created)
        {
            db.ProductionOperations.Add(operation);
        }

        return await PersistAsync(
            operation, companyId, actor,
            $"masterdata.production-operation.{(created ? "created" : "updated")}",
            new
            {
                operation.Code,
                operation.Name,
                operation.IsOutsourced,
                operation.IsActive,
            },
            () => new ProductionOperationResponse(
                operation.Id, operation.Code, operation.Name, operation.Description,
                operation.IsOutsourced, operation.IsActive),
            cancellationToken);
    }
}
