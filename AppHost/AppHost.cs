var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL container and register saasdb database
var postgres = builder.AddPostgres("postgres")
                      .WithPgAdmin()
                      .AddDatabase("saasdb");

// Add Keycloak container
var keycloak = builder.AddKeycloak("keycloak");

// Add Catalog API
var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
                        .WithReference(postgres)
                        .WithReference(keycloak);

// Add BFF gateway
var bff = builder.AddProject<Projects.Bff>("bff")
                 .WithReference(catalogApi)
                 .WithReference(keycloak)
                 .WithReference(postgres);

builder.Build().Run();
