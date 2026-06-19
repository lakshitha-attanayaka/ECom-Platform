using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform;
using Platform.Context;
using Platform.Data;
using Platform.Middleware;
using Platform.RateLimiting;
using System;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Add Platform Services
builder.Services.AddTenantPlatform(builder.Configuration);
builder.Services.AddTenantRateLimiter();

var app = builder.Build();

app.MapDefaultEndpoints();

// Automatically apply migrations and seed test data in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

    int retryCount = 0;
    while (true)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (retryCount < 15)
        {
            retryCount++;
            Console.WriteLine($"[BFF] Database not ready yet. Retrying migration in 2 seconds... (Attempt {retryCount}/15). Error: {ex.Message}");
            System.Threading.Thread.Sleep(2000);
        }
    }

    if (!db.Tenants.Any())
    {
        db.Tenants.AddRange(
            new TenantEntity { Id = Guid.NewGuid(), Slug = "tenant-a", SubscriptionTier = "Basic", KeycloakRealm = "realm-tenant-a", IsActive = true },
            new TenantEntity { Id = Guid.NewGuid(), Slug = "tenant-b", SubscriptionTier = "Premium", KeycloakRealm = "realm-tenant-b", IsActive = true }
        );
        db.SaveChanges();
    }
}

app.UseRouting();

// Order of execution in pipeline is critical
app.UseMiddleware<TenantContextMiddleware>();
app.UseRateLimiter();

app.MapGet("/", (ITenantContext tenantContext) =>
{
    return Results.Ok(new
    {
        Message = "Welcome to SaaS Commerce BFF Gateway!",
        TenantId = tenantContext.TenantId,
        Slug = tenantContext.Slug,
        Tier = tenantContext.SubscriptionTier,
        Realm = tenantContext.KeycloakRealm
    });
});

app.Run();

// Required for E2E testing references
public partial class Program { }
