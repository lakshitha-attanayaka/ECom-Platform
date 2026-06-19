using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform;
using Platform.Context;
using Platform.Middleware;
using Platform.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & platform services
builder.AddServiceDefaults();

// Add platform DbContext, Middleware, and Rate Limiting
builder.Services.AddTenantPlatform(builder.Configuration);
builder.Services.AddTenantRateLimiter();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseRouting();

// Order of execution in pipeline is critical
app.UseMiddleware<TenantContextMiddleware>();
app.UseRateLimiter();

app.MapGet("/weatherforecast", (ITenantContext tenantContext) =>
{
    return Results.Ok(new
    {
        Message = "Hello from Catalog API!",
        TenantId = tenantContext.TenantId,
        Slug = tenantContext.Slug,
        Tier = tenantContext.SubscriptionTier,
        Realm = tenantContext.KeycloakRealm
    });
});

app.Run();

// Required for WebApplicationFactory reference in tests
public partial class Program { }
