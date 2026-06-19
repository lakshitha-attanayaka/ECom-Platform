# 🏛️ GLOBAL PROJECT DIRECTIVE — WHITE-LABEL MULTI-TENANT SAAS E-COMMERCE PLATFORM

> **ATTENTION AI AGENT:** This document is your supreme operational constitution for this project.
> Every decision you make must be traced back to a rule or principle defined here.
> Deviation is not permitted without explicit written approval from the Human Architect.

---

## ⚖️ PRIME DIRECTIVE — HUMAN SOVEREIGNTY (NON-NEGOTIABLE)

```
ARTICLE I:   You are an executor of intent, not an autonomous decision-maker.
ARTICLE II:  You MUST NOT make architectural, security, or data-model decisions unilaterally.
ARTICLE III: Every plan requires a human checkpoint. Every implementation requires an approved plan.
ARTICLE IV:  When in doubt, STOP and ASK. Ambiguity is not a license to proceed.
ARTICLE V:   You MUST surface all assumptions in your plan before any code is written.
```

**Operational Flow (strictly enforced):**
```
DIAGNOSE → PLAN → HUMAN APPROVES → IMPLEMENT → VERIFY → HUMAN SIGNS OFF
```
Never compress or skip phases. Never merge PLAN and IMPLEMENT into a single step.

---

## 🧠 SKILL DISCOVERY FALLBACK (MANDATORY)

Before executing any specialized workflow, you MUST verify your active skill context.

> **IF** native Superpowers skills (e.g., `writing-plans`, `test-driven-development`,
> `systematic-debugging`, `agile-pipeline`, `project-context`) are **not visually detected**
> in your current context window, you MUST use your available skill-loading mechanisms
> (e.g., `view_file` on the relevant `SKILL.md`, or `list_skills`/`use_skill` MCP tools)
> to load them **before proceeding**.

**Required Skills for this Project:**
| Skill                    | Trigger Condition                            |
|--------------------------|----------------------------------------------|
| `project-context`        | Always — load at session start               |
| `agile-pipeline`         | Before any feature work begins               |
| `writing-plans`          | Before any implementation                    |
| `systematic-debugging`   | On any bug, test failure, or unexpected behavior |
| `test-driven-development`| Before writing any new production code       |

---

## 🏗️ PROJECT IDENTITY

| Property            | Value                                                               |
|---------------------|---------------------------------------------------------------------|
| **Project Type**    | Greenfield — White-Label Multi-Tenant SaaS                          |
| **Business Model**  | Subscription-based, tier-gated features                             |
| **Core Domain**     | E-Commerce Storefront Platform                                      |
| **Tenant Model**    | Logical Isolation (Shared DB, TenantId column) → Physical Silos (future) |
| **Provisioning**    | Zero-Touch Logical Onboarding via background workers                |

**Tier Feature Matrix (Sacred — Do NOT implement features in the wrong tier):**

| Feature                        | Basic Tier | Premium Tier |
|-------------------------------|------------|--------------|
| Product Catalog (Read-Only)    | ✅          | ✅            |
| Seller Contact Details         | ✅          | ✅            |
| Limited UI Theming             | ✅          | ✅            |
| Shopping Cart                  | ❌          | ✅            |
| Payment Gateway Integration    | ❌          | ✅            |
| Stock Management               | ❌          | ✅            |
| Business Analytics             | ❌          | ✅            |

---

## ⚙️ TECH STACK — IMMUTABLE CONSTRAINTS

### Frontend
- **Framework:** Blazor Web App (Unified Rendering Model)
- **Pattern:** Backend-For-Frontend (BFF) — The BFF is the **only** component that holds auth tokens.
  The browser MUST NEVER receive raw JWTs.
- **SSR Mandate:** All public-facing pages (product catalog, seller details) MUST be Server-Side Rendered for SEO compliance.
- **Interactive Islands:** Shopping cart, dashboards, and tenant admin UI use Blazor Server or WASM interactive render modes.
- **UI Testing:** bUnit for component-level assertion. No browser required for unit-level component tests.

### Backend
- **Framework:** ASP.NET Core (Minimal APIs preferred; Controllers for complex resource groups)
- **Orchestration:** .NET Aspire — AppHost is the single source of truth for all service definitions,
  ports, and environment wiring during local development.
- **Architecture:** Modular Monolith — Domains: `Catalog`, `Cart`, `Inventory`, `Analytics`.
  Each domain is a distinct C# project/assembly. Domains MUST NOT reference each other directly;
  use domain events or shared kernel interfaces only.
- **Backgrounding:** Heavy operations (CSV imports, report generation, tenant onboarding provisioning)
  MUST be offloaded to background workers (`IHostedService` or queue-backed processor).
  Primary API containers MUST remain stateless and responsive.
- **Rate Limiting:** ASP.NET Core Rate Limiting middleware MUST be partitioned by `TenantId`.
  Cross-tenant resource exhaustion is a Sev-1 architectural violation.
- **Observability:** Every log, trace, and metric MUST be enriched with `TenantId`.
  OpenTelemetry is the standard. Aspire Dashboard is the local observability sink.

### Identity
- **Provider:** Keycloak (self-hosted, containerized via .NET Aspire)
- **Protocol:** OAuth2 / OIDC (MANDATORY — no exceptions)
- **Model:** One Keycloak Realm per Tenant. Realm provisioning via Keycloak Admin REST API.
- **RBAC:** Permission-based. Permissions → Roles → Users.
  Keycloak claims MUST be mapped to .NET authorization policies.
- **BFF Auth Flow:** Cookie-based sessions. PKCE flow for browser-to-BFF. BFF proxies to backend APIs
  using server-side tokens.

### Database
- **Engine:** PostgreSQL (Azure Database for PostgreSQL — Flexible Server in production)
- **ORM:** Entity Framework Core
- **Isolation:** `TenantId` column on ALL data-bearing tables + EF Core Global Query Filters (MANDATORY)
- **Index Mandate:** Every tenant-scoped table MUST have a composite index: `(TenantId, ...)` as leading column.
- **Query Safety:** EF Core Command Timeout globally enforced (max 10 seconds). No unbounded queries.
- **Future-Proofing:** Tenant middleware routing logic MUST support connection string switching
  (Silo model) without rewriting domain logic.

### Hosting
- **Platform:** Azure Container Apps (ACA)
- **CI/CD:** GitHub Actions
- **IaC:** Azure Developer CLI (`azd`) + .NET Aspire manifest
- **Assets:** Azure Blob Storage + CDN for tenant themes and product images
- **Subdomain Routing:** ACA wildcard domain (`*.platform.com`) → Tenant Context Middleware

---

## 🔒 SECURITY MANDATES (ZERO TOLERANCE — ANY BREACH IS A SEV-1 INCIDENT)

### S-1: Tenant Data Isolation (CRITICAL)
- EF Core Global Query Filters MUST be applied to **every** `DbSet` containing tenant data.
- `IgnoreQueryFilters()` requires code comment justification AND review gate before merge.
- Every integration test suite MUST include at least one cross-tenant data leakage assertion.

### S-2: Authentication & Authorization
- OAuth2 / OIDC via Keycloak is the ONLY permitted authentication mechanism.
- No `[AllowAnonymous]` on any endpoint returning tenant-scoped data. Ever.
- All authorization is policy-based: Permissions → Roles → Policies → Endpoints.

### S-3: BFF Token Security
- JWTs MUST NOT be stored in browser `localStorage` or `sessionStorage`.
- The BFF holds all tokens server-side. Browser holds only an HttpOnly, Secure, SameSite=Strict cookie.

### S-4: API Hardening
- Rate limiting MUST be partitioned by `TenantId` (not just IP).
- All API inputs MUST be validated via `FluentValidation` before reaching domain logic.
- No raw SQL strings in production code paths. EF Core parameterized queries only.

---

## 🌿 GIT ISOLATION MANDATE

> **BEFORE writing a single line of implementation code**, you MUST establish an isolated
> working environment. This is non-negotiable.

**Workflow:**
1. Feature work: `feature/<jira-ticket-id>-<short-description>`
2. Bug fixes: `fix/<jira-ticket-id>-<short-description>`
3. `main` is protected — No direct pushes. PRs require passing CI + human review.
4. Use **git worktrees** when parallelizing domain work to avoid context-switching.

```
main (protected)
  └── develop
        ├── feature/SAAS-001-tenant-context-middleware
        ├── feature/SAAS-002-keycloak-realm-provisioning
        └── fix/SAAS-042-global-query-filter-bypass
```

---

## 🚫 ABSOLUTE PROHIBITIONS

| #    | Prohibition                                                                            |
|------|----------------------------------------------------------------------------------------|
| P-1  | NEVER write production code without a corresponding failing test                       |
| P-2  | NEVER mock `DbContext` or `IQueryable` in integration tests — use Testcontainers       |
| P-3  | NEVER store tokens in browser storage (localStorage / sessionStorage)                  |
| P-4  | NEVER use `IgnoreQueryFilters()` without explicit justification and review gate        |
| P-5  | NEVER implement Premium-tier features in a Basic-tier code path                        |
| P-6  | NEVER make an architectural decision without a corresponding ADR in Confluence         |
| P-7  | NEVER merge to `main` without passing CI (xUnit + Playwright)                          |
| P-8  | NEVER execute IaC pipelines for tenant onboarding — provisioning is always logical    |
| P-9  | NEVER cross domain assembly boundaries with direct project references                  |
| P-10 | NEVER allow a background worker to block the primary API thread                        |
