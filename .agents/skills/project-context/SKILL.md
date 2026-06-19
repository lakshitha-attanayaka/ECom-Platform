---
name: project-context
description: >
  The ultimate project dictionary and operational overlay for the White-Label Multi-Tenant
  SaaS E-Commerce Platform. Defines exactly how every native Superpowers skill (TDD, debugging,
  planning) must execute within the context of this specific tech stack: .NET Aspire, Blazor BFF,
  Keycloak multi-realm, EF Core Global Query Filters on PostgreSQL, and Azure Container Apps.
  Load this skill at the start of every agent session before any other skill.
---

# PROJECT CONTEXT SKILL — MULTI-TENANT SAAS E-COMMERCE PLATFORM

## IRON LAW #1: CONTEXT BEFORE CODE
**You MUST read and internalize this entire SKILL.md before writing a single line of code,
generating a plan, or running any test. This is not optional. This is not a suggestion.**

---

## 🗺️ SYSTEM MAP — KNOW YOUR ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────┐
│                  tenant-a.platform.com                      │
│                  tenant-b.platform.com                      │
└──────────────────────────┬──────────────────────────────────┘
                           │ (Wildcard TLS — Azure Container Apps)
                           ▼
            ┌──────────────────────────┐
            │  Tenant Context Middleware│  ← Parses subdomain → TenantId
            │  (ASP.NET Core Pipeline) │  ← Injects Keycloak Realm config
            └──────────┬───────────────┘
                       │
          ┌────────────▼────────────┐
          │   Blazor BFF Layer      │  ← SSR for catalog/public pages
          │   (Cookie Auth, PKCE)   │  ← Proxies to domain APIs
          └────────────┬────────────┘
                       │
     ┌─────────────────┼─────────────────────┐
     │                 │                     │
┌────▼───┐       ┌─────▼────┐        ┌──────▼─────┐
│Catalog │       │  Cart    │        │ Inventory  │
│Domain  │       │  Domain  │        │  Domain    │
└────┬───┘       └─────┬────┘        └──────┬─────┘
     │                 │                    │
     └─────────────────▼────────────────────┘
                       │
            ┌──────────▼──────────┐
            │ PostgreSQL (Shared) │  ← EF Core Global Query Filters
            │ TenantId on ALL rows│  ← Composite indexes (TenantId, ...)
            └─────────────────────┘
                       │
            ┌──────────▼──────────┐
            │   Keycloak          │  ← One Realm per Tenant
            │   (Per-Tenant Realm)│  ← RBAC: Permission → Role → User
            └─────────────────────┘
```

---

## 🧪 HOW THE `test-driven-development` SKILL EXECUTES IN THIS PROJECT

### Guiding Philosophy: Integration-Driven Development (IDD)
This project does NOT use classical unit-level TDD as the primary loop.
The canonical loop here is:

```
1. WRITE FAILING INTEGRATION TEST (Red)
2. WRITE MINIMAL IMPLEMENTATION (Green)
3. REFACTOR + VERIFY NO REGRESSIONS (Refactor)
```

### Test Type Decision Matrix

| Scenario | Test Type | Framework | Key Tool |
|---|---|---|---|
| Tenant A cannot see Tenant B data | Integration | xUnit + WebApplicationFactory | Testcontainers (PostgreSQL) |
| EF Core Global Query Filter is active | Integration | xUnit + EF Core | Testcontainers (PostgreSQL) |
| Keycloak realm isolation | Integration | xUnit + WebApplicationFactory | Testcontainers (Keycloak) |
| BFF cookie auth flow (end-to-end) | E2E | Playwright for .NET | Playwright MCP |
| Subscription discount calculation | Unit | xUnit | NSubstitute, FluentAssertions |
| Stock count validation logic | Unit | xUnit | NSubstitute, FluentAssertions |
| Blazor UI component rendering (tier gates) | Component | bUnit | bUnit TestContext |
| Cross-tenant subdomain routing | E2E | Playwright for .NET | Playwright MCP |

### RED Phase — Exact Execution Steps
1. Identify the integration boundary being tested (EF filter? Keycloak realm? BFF cookie?).
2. Stand up required Testcontainers: `PostgreSqlContainer` and/or `KeycloakContainer`.
3. Seed the database with **two tenant datasets** (Tenant A and Tenant B) — ALWAYS.
4. Make an HTTP request **authenticated as Tenant A**.
5. Assert that the response contains **only Tenant A data** and that Tenant B records are absent.
6. Run the test — it MUST fail (Red) because no implementation exists yet.
7. Commit the failing test on its feature branch before writing any implementation code.

### GREEN Phase — Exact Execution Steps
1. Write ONLY the code required to make the failing test pass. No extra logic.
2. For tenant isolation: implement `TenantContextMiddleware` + EF Core Global Query Filter.
3. For Keycloak: call Keycloak Admin REST API via `HttpClient` in the onboarding background worker.
4. For BFF: configure OpenIdConnect middleware with `SaveTokens = false` (BFF holds tokens server-side).
5. Run all tests. The target test MUST now pass. All prior tests MUST still pass.

### REFACTOR Phase — Exact Execution Steps
1. Review DI registration in `AppHost` and domain `ServiceCollectionExtensions`.
2. Ensure Global Query Filters are registered in `OnModelCreating` via a convention (not per-entity).
3. Ensure `TenantId` is sourced from DI (scoped service), NOT from a static/global.
4. Ensure rate limiting policy is keyed by `TenantId` claim from the token, not by IP.
5. Run full test suite. Zero regressions permitted.

### Unit Test Protocol (for pure business logic ONLY)
- Use NSubstitute for interface mocking (e.g., `IStockRepository`, `ISubscriptionCalculator`).
- Use FluentAssertions for all assertions: `result.Should().Be(expected)`.
- NEVER mock `DbContext`, `IQueryable`, or `HttpContext` in integration scenarios.
- Test file naming: `[SystemUnderTest]Tests.cs` (e.g., `SubscriptionDiscountCalculatorTests.cs`).
- Test method naming: `[Method]_[Scenario]_[ExpectedOutcome]`
  (e.g., `Calculate_WhenPremiumTier_ReturnsCorrectDiscount`).

---

## 🔍 HOW THE `systematic-debugging` SKILL OPERATES IN THIS PROJECT

### IRON LAW #2: DO NOT GUESS. OBSERVE FIRST.
Before touching any code, you MUST collect evidence from at least 3 of the following sources:

### Step 1 — Observe Telemetry (Aspire Dashboard / OpenTelemetry)
- Open the .NET Aspire Dashboard: `http://localhost:15888`
- Filter all traces and logs by `TenantId` (it MUST be present on every request).
- If `TenantId` is missing from a trace — the bug is in `TenantContextMiddleware`.
- If a trace shows a DB query without a `WHERE TenantId = @p0` clause — the Global Query Filter is disabled or missing.

### Step 2 — Query the Live Database (PostgreSQL MCP)
Use the **PostgreSQL MCP** to run targeted diagnostics directly:
```sql
-- Verify Global Query Filter is working: should return 0 rows for wrong tenant
SELECT COUNT(*) FROM "Products"
WHERE "TenantId" = '<WRONG_TENANT_ID>';

-- Check composite indexes are being used
EXPLAIN ANALYZE
SELECT * FROM "Products"
WHERE "TenantId" = '<TENANT_ID>' AND "CategoryId" = '<CATEGORY_ID>';
-- Expected: Index Scan on idx_products_tenantid_categoryid (NOT Seq Scan)

-- Verify tenant isolation seeding in tests
SELECT "TenantId", COUNT(*) FROM "Products" GROUP BY "TenantId";
```

### Step 3 — Inspect Keycloak Realm Configuration
- Access Keycloak Admin Console: `http://localhost:8080/admin`
- Navigate to the specific tenant's realm.
- Verify: Client exists, PKCE is enabled, redirect URIs match BFF callback URL.
- Verify: Roles contain expected permissions, user is assigned the correct role.
- If auth fails silently — check Keycloak event logs (Admin Console → Events).

### Step 4 — Trace the BFF Auth Flow (Playwright MCP)
```csharp
// Use Playwright MCP to capture the actual browser flow
await Page.GotoAsync("https://tenant-a.platform.com");
// Assert redirect to Keycloak login
// Assert return URL is BFF callback
// Assert HttpOnly cookie is set (NO JWT in localStorage)
var cookies = await Context.CookiesAsync();
Assert.Contains(cookies, c => c.HttpOnly && c.Name == ".AspNetCore.Cookies");
```

### Step 5 — Check GitHub Actions CI Logs (GitHub MCP)
- Use GitHub MCP to fetch the failing pipeline run.
- Look for: Testcontainers startup failures, port conflicts, missing environment variables.
- Common issue: Testcontainers require Docker socket access in CI — verify runner has `docker` available.

### Debugging Priority Queue
```
Priority 1: Cross-tenant data leakage         → Check EF Global Query Filter + TenantId injection
Priority 2: Auth/auth failures                → Check Keycloak realm, BFF OIDC config, cookie policy
Priority 3: Missing TenantId in context       → Check TenantContextMiddleware host-header parsing
Priority 4: Slow queries / timeouts           → PostgreSQL MCP EXPLAIN ANALYZE + index verification
Priority 5: Background worker failures        → Check IHostedService registration, queue saturation
Priority 6: CI pipeline failures              → GitHub MCP pipeline logs + Testcontainers Docker check
```

---

## 📐 HOW THE `writing-plans` SKILL MAPS ARCHITECTURE IN THIS PROJECT

### IRON LAW #3: EVERY PLAN MUST MAP TO THE MODULAR MONOLITH BOUNDARIES.
Plans MUST NOT propose changes that violate domain assembly isolation.

### Plan Structure Template (mandatory format for all implementation plans)

```markdown
## Feature: [Name] — [Jira Ticket ID]

### Business Value (Required Before Technical Design)
> What tenant tier does this serve? What business outcome does it enable?
> [Must be answered BEFORE any technical sections are written]

### Domain Boundary
> Which domain assembly owns this feature?
> [ ] Catalog  [ ] Cart  [ ] Inventory  [ ] Analytics  [ ] Platform (cross-cutting)

### Tenant Tier Gate
> [ ] Basic Tier  [ ] Premium Tier  [ ] Both
> How is the tier gate enforced? (Policy? Feature Flag? Middleware?)

### Data Model Changes
> New tables / columns / indexes. MUST include TenantId if tenant-scoped.
> MUST include EF Core migration name.

### API Contract
> Endpoint: [METHOD] /api/[domain]/[resource]
> Auth: [Required policy name]
> Rate limit partition: TenantId (always)

### Test Plan (must reference IDD loop)
> Integration Test: [Description — must include cross-tenant isolation assertion]
> Unit Test: [Pure logic only — describe what is being isolated]
> E2E Test: [Playwright scenario — describe the user flow]

### Keycloak / Auth Impact
> Does this require a new permission? New role? New policy?
> If yes — describe the Keycloak Admin REST API call required.

### ADR Required?
> [ ] Yes — [describe the decision and link to Confluence ADR page]
> [ ] No — [brief justification]
```

### Domain Responsibility Map (reference this when assigning ownership)

| Domain | Owns | Does NOT Own |
|---|---|---|
| `Catalog` | Products, Categories, Pricing, Seller Details | Cart state, inventory counts |
| `Cart` | Cart sessions, line items, checkout initiation | Payment processing, product data |
| `Inventory` | Stock counts, reservation, bulk imports | Cart, pricing, analytics |
| `Analytics` | Report generation, event aggregation | Any write operations to other domains |
| `Platform` | TenantContextMiddleware, Keycloak provisioning, Rate limiting, Observability | Any domain business logic |

---

## 🔑 KEYCLOAK REALM PROVISIONING — REFERENCE SCRIPT

When any plan involves onboarding a new tenant, the background worker MUST:

```csharp
// 1. Create the realm
POST /admin/realms
{ "realm": "tenant-{tenantSlug}", "enabled": true }

// 2. Create the BFF client with PKCE
POST /admin/realms/tenant-{tenantSlug}/clients
{ "clientId": "bff-client", "publicClient": false,
  "standardFlowEnabled": true, "pkceCodeChallengeMethod": "S256" }

// 3. Create roles (map to subscription tier permissions)
POST /admin/realms/tenant-{tenantSlug}/roles
{ "name": "basic-user" }   // or "premium-user"

// 4. Assign permissions to roles via client scopes
// (permissions are Keycloak client scope protocol mappers)
```

---

## 📦 ASPIRE APPHOST — LOCAL DEVELOPMENT REFERENCE

The `.NET Aspire AppHost` MUST register all services:
```csharp
// AppHost/Program.cs — canonical service wiring
var postgres = builder.AddPostgres("postgres").AddDatabase("saasdb");
var keycloak = builder.AddKeycloak("keycloak", port: 8080);
var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(postgres)
    .WithReference(keycloak);
var bff = builder.AddProject<Projects.Bff>("bff")
    .WithReference(catalogApi)
    .WithReference(keycloak);
```

---

## 🏷️ NAMING CONVENTIONS (PROJECT-WIDE)

| Item | Convention | Example |
|---|---|---|
| Solution | `[ProductName].sln` | `SaaSCommerce.sln` |
| Domain project | `[Domain].Api` | `Catalog.Api` |
| Test project | `[Domain].Tests` | `Catalog.Tests` |
| EF Migration | `[YYYY]_[Description]` | `2026_AddTenantIdToProducts` |
| Jira branch | `feature/[ID]-[slug]` | `feature/SAAS-001-tenant-middleware` |
| Keycloak realm | `tenant-[slug]` | `tenant-acme-corp` |
| ACA container | `[env]-[domain]-api` | `prod-catalog-api` |
