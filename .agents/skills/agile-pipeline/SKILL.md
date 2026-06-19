---
name: agile-pipeline
description: >
  The Agile Business Analyst pipeline for the White-Label Multi-Tenant SaaS E-Commerce Platform.
  Governs the complete flow from raw business requirement → Jira Epic/Story → Figma design token
  extraction → Gherkin Acceptance Criteria with embedded security assertions → approved
  implementation plan. Enforces the IRON LAW that no technical design begins before business value
  is validated and human-approved. Integrates Jira MCP, Confluence MCP, and Figma MCP as
  mandatory data sources.
---

# AGILE BA PIPELINE SKILL — MULTI-TENANT SAAS E-COMMERCE PLATFORM

---

## ⛔ IRON LAW #1: BUSINESS VALUE BEFORE TECHNICAL DESIGN

**THIS IS THE SUPREME RULE OF THIS SKILL. IT CANNOT BE OVERRIDDEN.**

```
╔══════════════════════════════════════════════════════════════════╗
║  NO ENTITY MODEL, NO API CONTRACT, NO DATABASE SCHEMA,          ║
║  NO COMPONENT DESIGN, AND NO CODE MAY BE PRODUCED UNTIL:        ║
║                                                                  ║
║  1. The business value of the feature is explicitly stated.      ║
║  2. The tenant tier that benefits is identified.                 ║
║  3. The human architect has given written approval on the        ║
║     Acceptance Criteria.                                         ║
╚══════════════════════════════════════════════════════════════════╝
```

**Violation of this law is treated as a Sev-1 process failure. Stop. Revert. Restart.**

---

## ⛔ IRON LAW #2: NO ASSUMPTION-DRIVEN DESIGN

You MUST NOT infer requirements. If a Jira ticket is ambiguous, you MUST:
1. Post a comment on the Jira issue (via Jira MCP) flagging the ambiguity.
2. Pause the pipeline and surface the question to the human architect.
3. Resume ONLY after the ticket is updated with a clarification.

---

## ⛔ IRON LAW #3: SECURITY ACCEPTANCE CRITERIA ARE MANDATORY

**Every feature touching tenant data, authentication, or authorization MUST include
Gherkin scenarios that explicitly test the security mandates (S-1 through S-4).**
Features without security scenarios are BLOCKED from moving to the implementation phase.

---

## ⛔ IRON LAW #4: STRICT ROLE BOUNDARY — EPICS/USER STORIES vs TECHNICAL SUB-TASKS

**Epics and main User Stories MUST NOT contain technical implementation details (e.g., no SQL schemas, no code classes/methods, no framework-specific routing paths, no API endpoints, no database table/field names, and no test library/tool names).**

- **The Epic / User Story** defines the *WHAT* and the *WHY* using pure business-oriented language (e.g., describing customer experience, merchant boundaries, subscription gating, and tenant data isolation) along with behavior-oriented Gherkin Acceptance Criteria.
- **The Technical Sub-tasks** are separate child issues containing the *HOW* details (e.g., SQL schema, database indices, specific C# class design, library references, and exact file paths).
- Violation of this role boundary on any Epic or Story is treated as a Sev-1 process failure. Technical details must be strictly isolated to sub-tasks.

---

## 📋 THE BA PIPELINE — 7 MANDATORY STAGES

```
[Stage 1] FETCH CONTEXT FROM JIRA + CONFLUENCE
      ↓
[Stage 2] CLARIFY & VALIDATE BUSINESS VALUE
      ↓
[Stage 3] EXTRACT DESIGN TOKENS FROM FIGMA
      ↓
[Stage 4] WRITE GHERKIN ACCEPTANCE CRITERIA (with security scenarios)
      ↓
[Stage 5] HUMAN APPROVAL GATE ← ⛔ FULL STOP HERE
      ↓
[Stage 6] GENERATE JIRA SUBTASKS & LINK TO EPIC
      ↓
[Stage 7] HAND OFF TO `writing-plans` SKILL
```

---

## STAGE 1: FETCH CONTEXT FROM JIRA + CONFLUENCE

**Tool: Jira MCP (`getJiraIssue`, `searchJiraIssuesUsingJql`)**
**Tool: Confluence MCP (`searchConfluenceUsingCql`, `getConfluencePage`)**

### Action Protocol:
```
1. Retrieve the target Jira Epic or Story by ticket ID.
   → Use: getJiraIssue(issueKey: "SAAS-XXX")

2. Extract from the Jira issue:
   - Summary (the feature headline)
   - Description (the raw business requirement)
   - Labels (must include the domain: catalog / cart / inventory / analytics / platform)
   - Custom fields: Tenant Tier (Basic / Premium / Both), Domain Owner

3. Search Confluence for relevant ADRs and context:
   → Use: searchConfluenceUsingCql(cql: 'space="SAAS" AND title ~ "ADR" AND text ~ "[feature keyword]"')
   
4. Read the linked ADR page if found:
   → Use: getConfluencePage(pageId: "...")
   → Extract: Architectural constraints, prior decisions, known limitations.

5. Check for existing related Jira issues to prevent duplication:
   → Use: searchJiraIssuesUsingJql(jql: 'project = SAAS AND summary ~ "[feature keyword]" AND issuetype in (Story, Bug)')
```

### Output of Stage 1:
```markdown
## Fetched Context: [SAAS-XXX] — [Issue Summary]

- **Business Requirement:** [verbatim from Jira description]
- **Domain:** [Catalog | Cart | Inventory | Analytics | Platform]
- **Tenant Tier:** [Basic | Premium | Both]
- **Related ADRs:** [Confluence link or "None found"]
- **Duplicate Risk:** [Existing issues found or "None found"]
```

---

## STAGE 2: CLARIFY & VALIDATE BUSINESS VALUE

Before any design, answer these 4 questions explicitly:

```markdown
## Business Value Validation

**Q1: What tenant problem does this solve?**
> [Specific pain point or business need — no technical jargon]

**Q2: Which subscription tier benefits?**
> [ ] Basic  [ ] Premium  [ ] Both
> Rationale: [Why this tier and not the other?]

**Q3: What is the measurable success criterion?**
> [e.g., "Tenant admin can upload 500 products in < 30 seconds",
>  "Tenant A users cannot see Tenant B's inventory under any circumstance"]

**Q4: Are there dependencies on external systems (Keycloak, Payment Gateway, etc.)?**
> [List explicitly — these will become integration test targets]
```

**If any answer is "unknown" or "unclear" → STOP. Post to Jira. Await human clarification.**

---

## STAGE 3: EXTRACT DESIGN TOKENS FROM FIGMA

**Tool: Figma MCP (`get_figma_data`, `download_figma_images`)**

### Action Protocol:
```
1. Locate the Figma file/frame linked in the Jira ticket.
   → Ask the human architect for the Figma URL if not present in the ticket.

2. Fetch the Figma node data:
   → Use: get_figma_data(fileKey: "...", nodeIds: ["..."])

3. Extract design tokens:
   - Colors: Primary, Secondary, Background, Surface, Error, Success
     → Map to CSS custom properties: --color-primary, --color-surface, etc.
   - Typography: Font family, weights, sizes for headings/body/labels
   - Spacing: Component padding/margin rhythm
   - Border radius: Card, button, input field
   - Component variants: Default, Hover, Disabled, Loading states

4. For tenant-customizable elements, identify:
   - Which tokens are FIXED (platform branding — not overridable by tenant)
   - Which tokens are TENANT-CUSTOMIZABLE (colors, logo — stored in TenantSettings table)

5. Download component reference images if needed:
   → Use: download_figma_images(fileKey: "...", nodeIds: ["..."])
```

### Design Token Output Format:
```csharp
// Generated: TenantTheme.cs (Shared Kernel)
public record TenantTheme
{
    public string PrimaryColor { get; init; } = "#3B82F6";    // Figma: brand/primary
    public string SecondaryColor { get; init; } = "#10B981";  // Figma: brand/secondary
    public string LogoUrl { get; init; } = string.Empty;      // Tenant-uploaded asset
    public string FontFamily { get; init; } = "Inter, sans-serif"; // Figma: typography/base
    // Basic tier: only PrimaryColor + LogoUrl are customizable
    // Premium tier: all tokens are customizable
}
```

---

## STAGE 4: WRITE GHERKIN ACCEPTANCE CRITERIA

### IRON LAW #3 REMINDER: Security scenarios are MANDATORY. Non-negotiable.

### Gherkin Template — Feature with Tenant Data Access:

```gherkin
Feature: [Feature Name] — [Jira Ticket ID]
  As a [tenant tier] tenant
  I want to [business action]
  So that [business outcome]

  # ────────────────────────────────────────────────
  # HAPPY PATH SCENARIOS
  # ────────────────────────────────────────────────

  Scenario: [Tier]-tier tenant successfully [action]
    Given I am authenticated as a "[Basic|Premium]" tier user
    And I am operating under tenant subdomain "tenant-a.platform.com"
    When I [perform the core action]
    Then I should see [expected outcome]
    And the response should contain only data belonging to "tenant-a"
    And the HTTP response code should be 200

  # ────────────────────────────────────────────────
  # TIER GATE ENFORCEMENT SCENARIOS
  # ────────────────────────────────────────────────

  Scenario: Basic-tier tenant is blocked from Premium features
    Given I am authenticated as a "Basic" tier user
    And I am operating under tenant subdomain "tenant-a.platform.com"
    When I attempt to access "[Premium feature endpoint]"
    Then I should be denied access
    And the HTTP response code should be 403
    And the response body should contain "Feature not available on your subscription tier"

  # ────────────────────────────────────────────────
  # SECURITY SCENARIO S-1: TENANT DATA ISOLATION
  # ────────────────────────────────────────────────

  Scenario: Authenticated user cannot access another tenant's data (S-1 ISOLATION)
    Given tenant "tenant-a" has [N] [resources] in the system
    And tenant "tenant-b" has [M] [resources] in the system
    And I am authenticated as a valid user of "tenant-a"
    When I request all [resources] from the API
    Then the response should contain exactly [N] [resources]
    And none of the returned [resources] should belong to "tenant-b"
    And the EF Core query should include a "WHERE TenantId = 'tenant-a'" predicate

  # ────────────────────────────────────────────────
  # SECURITY SCENARIO S-2: AUTHENTICATION ENFORCEMENT
  # ────────────────────────────────────────────────

  Scenario: Unauthenticated request to tenant data endpoint is rejected (S-2 OIDC)
    Given I am not authenticated
    When I send a request to "[tenant-scoped API endpoint]"
    Then I should be redirected to the Keycloak login page for the correct realm
    And the HTTP response code should be 401 or 302

  Scenario: User authenticated in wrong tenant realm is rejected (S-2 REALM ISOLATION)
    Given I am authenticated via the "tenant-b" Keycloak realm
    When I attempt to access "tenant-a.platform.com/api/[resource]"
    Then I should be denied access
    And the HTTP response code should be 403

  # ────────────────────────────────────────────────
  # SECURITY SCENARIO S-3: BFF TOKEN SECURITY
  # ────────────────────────────────────────────────

  Scenario: Browser does not receive raw JWT after login (S-3 BFF TOKEN SECURITY)
    Given I navigate to "tenant-a.platform.com"
    When I complete the Keycloak OIDC login flow
    Then the browser should NOT have a JWT in localStorage
    And the browser should NOT have a JWT in sessionStorage
    And an HttpOnly session cookie should be present
    And the cookie SameSite attribute should be "Strict"

  # ────────────────────────────────────────────────
  # SECURITY SCENARIO S-4: RATE LIMITING
  # ────────────────────────────────────────────────

  Scenario: Rate limiting is enforced per-tenant, not globally (S-4 RATE LIMITING)
    Given "tenant-a" has exhausted their API rate limit
    When a user of "tenant-b" makes a valid API request
    Then "tenant-b"'s request should succeed with HTTP 200
    And "tenant-a"'s next request should be rejected with HTTP 429
    And the 429 response should NOT affect "tenant-b"'s quota

  # ────────────────────────────────────────────────
  # PERMISSION-BASED ACCESS CONTROL SCENARIOS
  # ────────────────────────────────────────────────

  Scenario: User with insufficient permissions cannot perform [action]
    Given I am authenticated as a user with role "[insufficient-role]"
    And the role "[insufficient-role]" does NOT have the "[required-permission]" permission
    When I attempt to "[protected action]"
    Then I should receive HTTP 403
    And the error message should reference missing permissions

  Scenario: User with correct permission can perform [action]
    Given I am authenticated as a user with role "[correct-role]"
    And the role "[correct-role]" HAS the "[required-permission]" permission
    When I attempt to "[protected action]"
    Then the action should succeed with HTTP 200
```

---

## STAGE 5: HUMAN APPROVAL GATE ⛔

**FULL STOP. DO NOT PROCEED PAST THIS POINT WITHOUT EXPLICIT HUMAN SIGN-OFF.**

Present the following for review:
```markdown
## 📋 APPROVAL REQUEST — [SAAS-XXX] [Feature Name]

### Business Value Statement
[From Stage 2 — Q1 through Q4]

### Tenant Tier Impact
[Basic | Premium | Both] — [Rationale]

### Acceptance Criteria Summary
[List all Gherkin scenario names — link to full Gherkin doc]

### Security Coverage Checklist
- [ ] S-1: Cross-tenant data isolation scenario included
- [ ] S-2: OIDC authentication enforcement scenario included
- [ ] S-2: Keycloak realm isolation scenario included
- [ ] S-3: BFF token security scenario included
- [ ] S-4: Per-tenant rate limiting scenario included
- [ ] RBAC: Permission-based access control scenarios included

### Design Token Extraction
[Summarize Figma-sourced tokens, flag any ambiguities]

### Open Questions (if any)
[Must be empty OR explicitly acknowledged by architect before approval]

---
> **ACTION REQUIRED:** Reply with APPROVED to proceed to Stage 6.
> Or reply with CHANGES NEEDED: [feedback] to revise.
```

---

## STAGE 6: GENERATE JIRA SUBTASKS & LINK TO EPIC

**Tool: Jira MCP (`createJiraIssue`, `createIssueLink`, `addCommentToJiraIssue`)**

After human approval, create the implementation subtask breakdown. Every generated ticket is classified as either:
- **Epic / User Story**: The high-level objective or requirement, formatted to define the business value, merchant constraints, and Gherkin Acceptance Criteria. Epics and Stories MUST NOT contain implementation specifics (no code, no database schemas, no framework routes, no package references).
- **Technical Sub-task (Subtask Type)**: Child issue of the Story (or Epic, where appropriate) that contains the technical implementation instructions (*HOW*), such as class signatures, SQL schemas, packages, files to modify, or verification steps.

```
For each User Story, create a child Subtask to house its implementation notes:

[Story]   SAAS-XXX: [User Story Summary - Business Language Only]
   └─ [Subtask]  SAAS-XXX-1: [TECH] Implementation Notes (including libraries, patterns, database schema)

Link all Stories to parent Epic via: createIssueLink(type: "is part of")
Add approved Gherkin AC as a comment on the parent ticket:
→ addCommentToJiraIssue(issueKey: "SAAS-XXX", body: [full Gherkin text])
```

---

## STAGE 7: HAND OFF TO `writing-plans` SKILL

Trigger the `writing-plans` skill with the following pre-populated context package:

```markdown
## Context Package for `writing-plans`

- **Jira Ticket:** SAAS-XXX — [Title]
- **Domain:** [Catalog | Cart | Inventory | Analytics | Platform]
- **Tenant Tier:** [Basic | Premium | Both]
- **Approved Gherkin AC:** [linked or embedded]
- **Figma Design Tokens:** [TenantTheme record or CSS variables]
- **Security Mandates to implement:** [S-1, S-2, S-3, S-4 — as applicable]
- **Integration Test Target:** [describe Testcontainers setup needed]
- **E2E Test Target:** [describe Playwright scenario]
- **ADR Required:** [Yes — describe / No — reason]
- **Feature Branch Name:** feature/SAAS-XXX-[slug]
```

The `writing-plans` skill takes this package and generates the full Implementation Plan
for human review before any code is written.

---

## 🔁 PIPELINE QUICK-REFERENCE CARD

```
INPUT:  Jira ticket ID or raw requirement text
          ↓
[S1]  getJiraIssue + searchConfluenceUsingCql → fetch business context
[S2]  Business value validation → 4 questions answered → ambiguity → STOP & comment on Jira
[S3]  get_figma_data → extract design tokens → map to TenantTheme
[S4]  Write Gherkin AC → MUST include S-1, S-2, S-3, S-4, RBAC scenarios
[S5]  ⛔ HUMAN APPROVAL GATE — await written APPROVED signal
[S6]  createJiraIssue subtasks → createIssueLink → addCommentToJiraIssue (Gherkin AC)
[S7]  Hand off context package to writing-plans skill
OUTPUT: Approved Gherkin AC + Jira subtask tree + Context package for implementation
```
