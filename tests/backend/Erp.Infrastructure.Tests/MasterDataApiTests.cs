using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Erp.Domain.Platform;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Infrastructure.Tests;

public sealed class MasterDataApiTests : IAsyncLifetime
{
    private const string BootstrapToken = "test-bootstrap-token-with-at-least-32-characters";
    private const string AdministratorEmail = "masterdata-admin@example.test";
    private const string AdministratorPassword = "Manufacturing-admin-Password-01!";

    private readonly ErpApiFactory factory = new();
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        await factory.InitializeDatabaseAsync();
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var companyId = await BootstrapAsync();
        using var login = await PostWithCsrfAsync("/api/v1/auth/login", new
        {
            email = AdministratorEmail,
            password = AdministratorPassword,
            companyId,
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
    }

    [Fact]
    public async Task CustomersSupportSearchPagingDuplicateProtectionAndAudit()
    {
        using var create = await PostWithCsrfAsync("/api/v1/master-data/customers", new
        {
            code = "buyer-001",
            name = "Northwind Apparel",
            email = "buying@northwind.example",
            countryCode = "US",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Contains("/api/v1/master-data/customers/", create.Headers.Location?.ToString());

        using var duplicate = await PostWithCsrfAsync("/api/v1/master-data/customers", new
        {
            code = "BUYER-001",
            name = "Duplicate Buyer",
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using var search = await client.GetAsync(
            "/api/v1/master-data/customers?search=northwind&page=1&pageSize=1");
        search.EnsureSuccessStatusCode();
        using var page = JsonDocument.Parse(await search.Content.ReadAsStringAsync());
        Assert.Equal(1, page.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, page.RootElement.GetProperty("items").GetArrayLength());

        using var invalidPage = await client.GetAsync("/api/v1/master-data/customers?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        Assert.Contains(await db.AuditEvents.ToListAsync(),
            auditEvent => auditEvent.Action == "masterdata.customer.created");
    }

    [Fact]
    public async Task ItemCreationUsesCompanyCodeRuleAndValidatesCompanyReferences()
    {
        using var categoryResponse = await PostWithCsrfAsync(
            "/api/v1/master-data/item-categories",
            new
            {
                code = "FABRIC",
                name = "Fabrics",
                trackingMode = "lot",
                categoryType = "fabric",
                isActive = true,
            });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        using var categoryJson = JsonDocument.Parse(
            await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryJson.RootElement.GetProperty("id").GetGuid();

        using var unitResponse = await PostWithCsrfAsync("/api/v1/master-data/units", new
        {
            code = "MTR",
            name = "Metre",
            dimension = "length",
            decimalPlaces = 3,
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.Created, unitResponse.StatusCode);
        using var unitJson = JsonDocument.Parse(await unitResponse.Content.ReadAsStringAsync());
        var unitId = unitJson.RootElement.GetProperty("id").GetGuid();

        using var createItem = await PostWithCsrfAsync("/api/v1/master-data/items", new
        {
            name = "Cotton Jersey",
            categoryId,
            stockUnitId = unitId,
            purchaseUnitId = unitId,
            purchaseToStockFactor = 1.0,
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.Created, createItem.StatusCode);
        using var itemJson = JsonDocument.Parse(await createItem.Content.ReadAsStringAsync());
        Assert.Equal("ITEM-000001", itemJson.RootElement.GetProperty("code").GetString());

        using var ruleResponse = await client.GetAsync("/api/v1/master-data/items/code-rule");
        ruleResponse.EnsureSuccessStatusCode();
        using var ruleJson = JsonDocument.Parse(await ruleResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, ruleJson.RootElement.GetProperty("nextNumber").GetInt64());

        using var invalidReference = await PostWithCsrfAsync("/api/v1/master-data/items", new
        {
            name = "Invalid Item",
            categoryId = Guid.NewGuid(),
            stockUnitId = unitId,
            purchaseUnitId = unitId,
            purchaseToStockFactor = 1.0,
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidReference.StatusCode);

        using var generatedSearch = await client.GetAsync(
            "/api/v1/master-data/items?search=cotton&page=1&pageSize=10");
        generatedSearch.EnsureSuccessStatusCode();
        using var searchJson = JsonDocument.Parse(
            await generatedSearch.Content.ReadAsStringAsync());
        Assert.Equal(1, searchJson.RootElement.GetProperty("totalCount").GetInt32());
    }

    private async Task<Guid> BootstrapAsync()
    {
        using var response = await PostWithCsrfAsync("/api/v1/setup/bootstrap", new
        {
            companyCode = "MDM",
            companyName = "Master Data Test Company",
            adminEmail = AdministratorEmail,
            adminDisplayName = "Master Data Administrator",
            adminPassword = AdministratorPassword,
        }, new Dictionary<string, string>
        {
            ["X-ERP-Bootstrap-Token"] = BootstrapToken,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return result.RootElement.GetProperty("companyId").GetGuid();
    }

    private async Task<HttpResponseMessage> PostWithCsrfAsync(
        string path,
        object body,
        IReadOnlyDictionary<string, string>? additionalHeaders = null)
    {
        using var tokenResponse = await client.GetAsync("/api/v1/auth/csrf");
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add(
            "X-ERP-CSRF",
            tokenJson.RootElement.GetProperty("requestToken").GetString()!);
        if (additionalHeaders is not null)
        {
            foreach (var header in additionalHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        return await client.SendAsync(request);
    }
}
