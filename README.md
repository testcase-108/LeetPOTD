# Roadmap

## Project Architecture Overview

The **LeetPOTD** application is structured as a full-stack, modular solution using a **Domain-Driven / N-Layered Backend Architecture** paired with a **Schema-Driven Dynamic Angular Frontend**.

<img width="870" height="787" alt="image" src="https://github.com/user-attachments/assets/fafd1a88-de5b-4ce0-852b-e88122ea8f0a" />

### Layered Separation
1. **Domain Layer (Model)**: Core entities (`PotdRun`, `IntegrationConfig`, `SubmissionLog`), Enums (`ExecutionStatus`), and value objects.
2. **Repository Layer**: Abstraction of data access (`IRepository<T>`, `IPotdRunRepository`) decouples database access from domain logic.
3. **Domain Service Layer**: Core business logic and external system integrations (`ILeetCodeService`, `IGeminiService`, `ILinkedInService`, `IPotdOrchestrator`).
4. **Application Service Layer (AppService)**: Application logic layer (`IPotdAppService`, `IIntegrationAppService`) orchestrating DTO mappings, transaction management, and exposing use cases to controllers and background workers.
5. **Middleware Layer**: Custom Auth Middleware validating incoming requests, enforcing session checks, and managing session state creation/expiration across protected routes.
6. **Background Worker**: `PotdWorker` executing the execution pipeline on a scheduled loop or via manual triggers using `IPotdOrchestrator`.

---

## Roadmap & Milestones

---

### Milestone 1: Architectural Scaffolding, N-Layer Infrastructure & Auth Middleware

#### Issue 1.1: Project Scaffolding & N-Layer Separation
* **Scope**: Setup ASP.NET Core Web API solution structure, domain models, and Angular project skeleton.
* **Implementation Details**:
  * Create standard projects: `LeetPOTD.Domain`, `LeetPOTD.Infrastructure`, `LeetPOTD.Service`, `LeetPOTD.AppService`, and `LeetPOTD.Api`.
  * Scaffold Angular application (`LeetPOTD.Web`) with core routing structure.
* **Acceptance Criteria**:
  * Project builds clean across all layers with dependency injection configured.
  * Angular application bootstraps and routes to blank placeholder components.

#### Issue 1.2: Session Management & Authentication Middleware
* **Scope**: Build custom HTTP Middleware for session verification, session creation, and endpoint protection.
* **Implementation Details**:
  * Implement `SessionAuthMiddleware` in ASP.NET Core.
  * Intercept requests to create new sessions upon authentication or verify existing `X-Session-Token` headers.
  * Store encrypted session data in SQLite/In-Memory store with expiration policy checks.
* **Acceptance Criteria**:
  * Unauthenticated requests to protected API routes return `401 Unauthorized`.
  * Session creation endpoint returns valid session tokens that pass verification on subsequent calls.

#### Issue 1.3: Repository Pattern & EF Core Persistence Setup
* **Scope**: Implement Generic Repository pattern and Database Context for SQLite.
* **Implementation Details**:
  * Define `IRepository<TEntity, TKey>` and concrete implementation `Repository<TEntity, TKey>` using Entity Framework Core.
  * Configure `LeetPotdDbContext` with mappings for `PotdRun`, `IntegrationConfig`, and `SubmissionLog`.
  * Apply EF Core initial migrations.
* **Acceptance Criteria**:
  * CRUD operations function through repository abstractions without referencing `DbContext` in higher layers.
  * SQLite database creates tables seamlessly on startup.

---

### Milestone 2: Schema-Driven Integration Engine & Dynamic UI

#### Issue 2.1: Integration Schema & Provider Abstraction
* **Scope**: Design backend abstractions for dynamic integration schemas (Gemini, LinkedIn, LeetCode, Telegram).
* **Implementation Details**:
  * Define `IIntegrationProvider` and `IIntegrationSchemaProvider` interfaces.
  * Implement providers returning JSON Schema descriptors defining required configuration fields (e.g., secrets, dropdowns, strings).
* **Acceptance Criteria**:
  * Calling `/api/integrations/schemas` returns form configuration metadata for each available provider.

#### Issue 2.2: Dynamic Form UI Component (Angular)
* **Scope**: Create reusable, schema-driven form component in Angular to render provider configurations dynamically.
* **Implementation Details**:
  * Build `DynamicFormComponent` parsing schema objects into reactive forms.
  * Support input types: `text`, `password/secret`, `select`, and `toggle`.
  * Handle client-side validation dynamically based on schema constraints.
* **Acceptance Criteria**:
  * Adding a new backend integration schema automatically renders its setup form in Angular without modifying UI code.

#### Issue 2.3: Integration AppService & Secure Credential Persistence
* **Scope**: Handle secure persistence and testing of third-party integration credentials.
* **Implementation Details**:
  * Implement `IntegrationAppService` handling `SaveConfigAsync()` and `TestConnectionAsync()`.
  * Encrypt sensitive keys/tokens (e.g., API keys, OAuth tokens) using Data Protection API before saving via `IIntegrationConfigRepository`.
  * Never return plaintext secret values back to the client UI.
* **Acceptance Criteria**:
  * Connection tests correctly report `Connected` or `Invalid/Expired` status.
  * Stored credentials remain encrypted in the database.

---

### Milestone 3: POTD Orchestrator & Algorithmic Execution Pipeline

#### Issue 3.1: LeetCode Service & Solution Lookup
* **Scope**: Implement `LeetCodeService` to fetch daily POTD details and handle submission interactions.
* **Implementation Details**:
  * Create `ILeetCodeService` to fetch daily problem metadata via LeetCode GraphQL API.
  * Implement local Git repository scanner to find user-provided solution files matching the problem slug or date.
  * Submit code via authenticated LeetCode session tokens and poll execution status until completion.
* **Acceptance Criteria**:
  * Successfully fetches today's POTD details (Title, Slug, Difficulty, Problem Statement).
  * Locates corresponding local C++ solution files and accurately retrieves test submission results.

#### Issue 3.2: Gemini Fallback & Explanation Service
* **Scope**: Implement Gemini API integration for automated bug-fixing and solution explanation generation.
* **Implementation Details**:
  * Implement `IGeminiService` with structured JSON output prompts.
  * **Fallback Flow**: If user submission fails (Wrong Answer/Runtime Error), pass problem statement + code + error log to Gemini for correction (capped at 2 attempts).
  * **Explanation Flow**: Upon `Accepted` status, prompt Gemini to output a structured summary (Approach, Time/Space Complexity, Key Takeaways).
* **Acceptance Criteria**:
  * Gemini correctly generates fixed code payloads when provided error logs.
  * Returns formatted markdown explanations and complexity analyses post-acceptance.

#### Issue 3.3: POTD Orchestrator Service & AppService Pipeline
* **Scope**: Combine execution flow into `PotdOrchestratorService` exposed via `PotdAppService`.
* **Implementation Details**:
  * Create `PotdOrchestratorService` managing execution state machine:
    $$\text{Fetch POTD} \rightarrow \text{Find Local Solution} \rightarrow \text{Submit} \rightarrow (\text{If Failed } \rightarrow \text{Gemini Fix}) \rightarrow \text{Generate Post/Summary}$$
  * Record execution stages into `SubmissionLog` using `IPotdRunRepository`.
* **Acceptance Criteria**:
  * End-to-end execution completes successfully for both local-first solutions and Gemini fallback routes.
  * Detailed execution history is accurately recorded in SQLite.

---

### Milestone 4: Midnight Scheduler & Operational Dashboard

#### Issue 4.1: Asia/Kolkata Midnight Hosted Background Worker
* **Scope**: Implement background worker running at 00:00 IST to execute the POTD pipeline automatically.
* **Implementation Details**:
  * Build `PotdWorker` deriving from `BackgroundService`.
  * Calculate exact delay targeting midnight `Asia/Kolkata` (`TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")`) across application restarts.
  * Resolve `IPotdAppService` within a dependency injection scope on trigger.
* **Acceptance Criteria**:
  * Worker accurately calculates next execution delay targeting 00:00 IST.
  * Restarts do not shift the targeted midnight execution schedule.

#### Issue 4.2: Operational Dashboard UI & Manual Trigger
* **Scope**: Build Angular operational dashboard with current status display and manual execution capabilities.
* **Implementation Details**:
  * Construct UI displaying today's POTD status, integration connection indicators, and last run log.
  * Add "Solve POTD Now" button invoking `POST /api/potd/run` through `PotdAppService`.
  * Ensure scheduler and manual button call the exact same underlying orchestrator logic.
* **Acceptance Criteria**:
  * Clicking "Solve POTD Now" instantly triggers execution and streams real-time state updates to the UI.

---

### Milestone 5: Publishing, Notifications & Audit Logging

#### Issue 5.1: LinkedIn Draft Generation & One-Click Publishing
* **Scope**: Build LinkedIn post draft creation and manual publishing workflow.
* **Implementation Details**:
  * Implement `LinkedInService` utilizing LinkedIn Community Management API.
  * Store generated post text in `LinkedInPostDraft` entity.
  * Expose `PublishDraftAsync(id)` in `PotdAppService` triggered via UI preview modal.
* **Acceptance Criteria**:
  * Accepted solutions automatically draft formatted LinkedIn posts.
  * One-click publishing successfully posts to the user's LinkedIn profile.

#### Issue 5.2: Multi-Channel Notification Service
* **Scope**: Send instant alert notifications for success, failure, or token expirations.
* **Implementation Details**:
  * Implement `INotificationService` supporting Telegram/Discord webhooks.
  * Dispatch notifications upon pipeline completion (Status, Execution Time, Attempts, Post Draft Ready) or authentication failures.
* **Acceptance Criteria**:
  * Notifications are delivered instantly to configured channels with execution summary metrics.

#### Issue 5.3: Audit Trail & Token Expiration Recovery UI
* **Scope**: Create history log UI and automated token failure handling.
* **Implementation Details**:
  * Build Angular `HistoryComponent` displaying paginated execution logs from `IPotdRunRepository`.
  * If third-party session/token expires during execution, flag integration status as `Invalid` and prompt user via UI banner to re-authenticate using the dynamic form.
* **Acceptance Criteria**:
  * Past runs, solution outputs, and error details are searchable via historical logs.
  * Auth failure cleanly transitions connection status to `Requires Authentication` without crashing the background worker.
