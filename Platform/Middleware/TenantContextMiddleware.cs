using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Platform.Context;
using Platform.Data;

namespace Platform.Middleware;

public class TenantContextMiddleware : IMiddleware
{
    private readonly IMemoryCache _cache;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public TenantContextMiddleware(IMemoryCache cache, TenantDbContext dbContext, ITenantContext tenantContext)
    {
        _cache = cache;
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var host = context.Request.Host.Host;
        var segments = host.Split('.');

        if (segments.Length < 2)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Unknown tenant" });
            return;
        }

        var slug = segments[0].ToLowerInvariant();
        var cacheKey = $"tenant:{slug}";
        TenantEntity? tenant = null;

        if (_cache.TryGetValue(cacheKey, out object? cachedValue))
        {
            if (cachedValue is string str && str == "NOT_FOUND")
            {
                tenant = null;
            }
            else
            {
                tenant = cachedValue as TenantEntity;
            }
        }
        else
        {
            tenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Slug == slug && t.IsActive);

            if (tenant != null)
            {
                _cache.Set(cacheKey, tenant, TimeSpan.FromMinutes(5));
            }
            else
            {
                _cache.Set(cacheKey, "NOT_FOUND", TimeSpan.FromMinutes(1));
            }
        }

        if (tenant == null)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Unknown tenant" });
            return;
        }

        // Populate context
        if (_tenantContext is TenantContext tc)
        {
            tc.TenantId = tenant.Id.ToString();
            tc.Slug = tenant.Slug;
            tc.SubscriptionTier = tenant.SubscriptionTier;
            tc.KeycloakRealm = tenant.KeycloakRealm;
        }

        // Enrich trace activity
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag("tenant.id", tenant.Id.ToString());
            activity.SetTag("tenant.slug", tenant.Slug);
        }

        await next(context);
    }
}
