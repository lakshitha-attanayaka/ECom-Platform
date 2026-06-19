using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Context;
using Platform.Data;
using Platform.Middleware;

namespace Platform;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        
        services.AddDbContextPool<TenantDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("saasdb")));

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<TenantContextMiddleware>();

        return services;
    }
}
