using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using Platform.Context;
using Platform.Data;
using Platform.Middleware;
using Platform.RateLimiting;
using Testcontainers.PostgreSql;
using Xunit;
using FluentAssertions;

namespace Platform.E2ETests;

public class BffPlaywrightTests : IClassFixture<BffServerFixture>
{
    private readonly BffServerFixture _fixture;

    public BffPlaywrightTests(BffServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Navigate_ToBffRoot_ReturnsResponseFromMiddleware()
    {
        // Arrange
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--host-resolver-rules=MAP *.localhost 127.0.0.1" }
        });
        
        var page = await browser.NewPageAsync();

        // Act
        var response = await page.GotoAsync("http://tenant-a.localhost:5001");

        // Assert
        response.Should().NotBeNull();
        response!.Status.Should().Be(200);

        var bodyText = await page.ContentAsync();
        bodyText.Should().Contain("tenant-a");
    }
}

public class BffServerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("saasdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private IHost? _host;

    public string ServerAddress => "http://127.0.0.1:5001";

    public async Task InitializeAsync()
    {
        // 1. Start database container
        await _postgresContainer.StartAsync();

        // 2. Run migrations and seed data
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        using var db = new TenantDbContext(options);
        await db.Database.MigrateAsync();

        db.Tenants.Add(new TenantEntity
        {
            Id = Guid.NewGuid(),
            Slug = "tenant-a",
            SubscriptionTier = "Basic",
            KeycloakRealm = "realm-tenant-a",
            IsActive = true
        });
        await db.SaveChangesAsync();

        // 3. Start local Kestrel server for Bff
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ConnectionStrings:saasdb", _postgresContainer.GetConnectionString() }
        });
        var config = configBuilder.Build();

        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(ServerAddress);
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(config);
                    services.AddTenantPlatform(config);
                    services.AddTenantRateLimiter();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseMiddleware<TenantContextMiddleware>();
                    app.UseRateLimiter();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", async context =>
                        {
                            var tc = context.RequestServices.GetRequiredService<ITenantContext>();
                            await context.Response.WriteAsJsonAsync(new
                            {
                                Message = "Welcome to SaaS Commerce BFF Gateway!",
                                TenantId = tc.TenantId,
                                Slug = tc.Slug,
                                Tier = tc.SubscriptionTier,
                                Realm = tc.KeycloakRealm
                            });
                        });
                    });
                });
            })
            .Build();

        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        await _postgresContainer.DisposeAsync();
    }
}
