using Erp.Api.Security;
using Erp.Api.MasterData;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Erp");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Set ConnectionStrings__Erp to a local PostgreSQL connection string.");
}

builder.Services.AddDbContext<ErpDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddErpSecurity(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AntiforgeryMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapSecurityEndpoints(builder.Configuration);
app.MapMasterDataEndpoints();

app.Run();

public partial class Program
{
}
