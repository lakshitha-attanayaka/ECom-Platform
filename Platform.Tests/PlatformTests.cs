using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Context;
using Platform.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Platform.Tests;

public class PlatformTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PlatformTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Request_WithValidSubdomain_ResolvesToCorrectTenantContext()
    {
        // Arrange
        using var factory = _fixture.CreateWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = "tenant-a.platform.com";

        // Act
        var response = await client.GetAsync("/weatherforecast");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<TenantResponse>();
        content.Should().NotBeNull();
        content!.TenantId.Should().Be(_fixture.TenantAId.ToString());
        content.Slug.Should().Be("tenant-a");
        content.Tier.Should().Be("Basic");
        content.Realm.Should().Be("realm-tenant-a");
    }

    [Fact]
    public async Task Request_WithUnknownSubdomain_Returns404()
    {
        // Arrange
        using var factory = _fixture.CreateWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = "unknown.platform.com";

        // Act
        var response = await client.GetAsync("/weatherforecast");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        err!.Error.Should().Be("Unknown tenant");
    }

    [Fact]
    public async Task Request_WithInactiveTenant_Returns404()
    {
        // Arrange
        using var factory = _fixture.CreateWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = "tenant-c.platform.com";

        // Act
        var response = await client.GetAsync("/weatherforecast");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantContext_Resolution_IsIsolatedPerRequest()
    {
        // Arrange
        using var factory = _fixture.CreateWebApplicationFactory();
        var clientA = factory.CreateClient();
        clientA.DefaultRequestHeaders.Host = "tenant-a.platform.com";

        var clientB = factory.CreateClient();
        clientB.DefaultRequestHeaders.Host = "tenant-b.platform.com";

        // Act
        var responseA = await clientA.GetAsync("/weatherforecast");
        var responseB = await clientB.GetAsync("/weatherforecast");

        // Assert
        var contentA = await responseA.Content.ReadFromJsonAsync<TenantResponse>();
        var contentB = await responseB.Content.ReadFromJsonAsync<TenantResponse>();

        contentA!.TenantId.Should().Be(_fixture.TenantAId.ToString());
        contentB!.TenantId.Should().Be(_fixture.TenantBId.ToString());
        contentA.TenantId.Should().NotBe(contentB.TenantId);
    }

    [Fact]
    public async Task RateLimiting_IsEnforced_PerTenant()
    {
        // Arrange
        using var factory = _fixture.CreateWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = "tenant-a.platform.com";

        // Act & Assert: send 100 requests which should succeed
        for (int i = 0; i < 100; i++)
        {
            var res = await client.GetAsync("/weatherforecast");
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 101st request should be rate-limited
        var rejectedResponse = await client.GetAsync("/weatherforecast");
        rejectedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejectedResponse.Headers.Contains("Retry-After").Should().BeTrue();
    }

    private record TenantResponse(string Message, string TenantId, string Slug, string Tier, string Realm);
    private record ErrorResponse(string Error);
}

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithDatabase("saasdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Guid TenantAId { get; } = Guid.NewGuid();
    public Guid TenantBId { get; } = Guid.NewGuid();
    public Guid TenantCId { get; } = Guid.NewGuid(); // Inactive

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Seed data
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        using var db = new TenantDbContext(options);
        await db.Database.MigrateAsync();

        db.Tenants.AddRange(
            new TenantEntity { Id = TenantAId, Slug = "tenant-a", SubscriptionTier = "Basic", KeycloakRealm = "realm-tenant-a", IsActive = true },
            new TenantEntity { Id = TenantBId, Slug = "tenant-b", SubscriptionTier = "Premium", KeycloakRealm = "realm-tenant-b", IsActive = true },
            new TenantEntity { Id = TenantCId, Slug = "tenant-c", SubscriptionTier = "Basic", KeycloakRealm = "realm-tenant-c", IsActive = false }
        );
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    public WebApplicationFactory<Program> CreateWebApplicationFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:saasdb", _postgresContainer.GetConnectionString());
            });
    }
}
