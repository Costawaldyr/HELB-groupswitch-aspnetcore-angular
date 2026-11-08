# GroupSwitch Engineering Guidelines

## Project context

GroupSwitch is an academic application for managing students,
study groups, assignments, group-change requests, and swap proposals.

Read the project specification before implementing a feature.
Locate it in the repository, including the parent directory when needed.

Verify the actual code, project references, framework versions,
frontend framework, and database provider before making changes.

Preserve the existing solution structure:

- 0 docs
- 1 frontend
- 2 backend
- GroupSwitch.sln

Do not rename directories or restructure the solution without approval.

Distinguish requirements from implemented and verified behavior.
Report contradictions between the specification and implementation.

## Language

- Write new code identifiers in English.
- Write new code comments and technical documentation in English.
- Explain decisions to the user in French unless requested otherwise.
- Preserve external data, proper names, and existing public contracts.
- Do not translate existing API fields without handling compatibility.

## Naming

- Use PascalCase for types, methods, and properties.
- Prefix interface names with I.
- Use camelCase for parameters and local variables.
- Follow the existing private-field naming convention consistently.
- Use descriptive domain names.
- Name booleans as questions or states.
- Use the Async suffix for asynchronous Task or ValueTask methods.

Prefer names such as:

- currentAssignment
- requestedGroup
- pendingChangeRequests
- availableCapacity
- affectedStudentIds
- hasScheduleConflict
- canApproveRequest

Prefer precise method names:

- FindSwapCycles
- ValidateGroupCapacity
- GetStudentAssignmentsAsync
- ImportScheduleAsync
- ApproveChangeRequestAsync

Avoid vague names such as data, obj, tmp, result2, manager,
or helper when a more meaningful name exists.

Short lambda parameter names are acceptable when immediately clear.

## Readability and teaching style

- Prefer straightforward C# that the student can explain and modify.
- Follow existing formatting and the course's programming approach.
- Use constructor injection and explicit dependencies.
- Prefer readable control flow over clever expressions.
- Use expression-bodied members only when they improve readability.
- Use var when the type is obvious.
- Give intermediate calculations meaningful names.
- Prefer guard clauses over deeply nested conditions.
- Keep methods focused and classes cohesive.
- Do not impose arbitrary method or class line-count limits.

Use names and structure to explain what the code does.

Do not add comments that simply repeat the code.
Keep comments for:

- non-obvious design reasons;
- external protocol constraints;
- algorithm invariants;
- necessary public API documentation.

Remove obsolete comments and commented-out code within the approved scope.

Course examples illustrate concepts.
Do not reproduce insecure shortcuts, incorrect routes,
inconsistent naming, or missing validation from demonstration code.

## Architecture and responsibilities

### Api

Responsible for:

- HTTP endpoints;
- request and response DTOs;
- transport-level validation;
- authentication and authorization integration;
- dependency injection configuration;
- mapping application outcomes to HTTP responses.

Keep controllers thin.
Do not implement allocation algorithms or EF Core queries in controllers.

### Business

Responsible for:

- application use cases;
- assignment rules;
- capacity validation;
- timetable compatibility;
- change-request processing;
- swap-cycle search;
- business-operation coordination.

Keep business interfaces in Business where supported by
the existing architecture.

### Data

Responsible for:

- EF Core configuration;
- database queries;
- persistence;
- repository implementations;
- database constraints and migrations.

Repositories must not decide whether a change request is acceptable.

### External integrations

Keep Hyperplanning access and AI-provider calls behind dedicated contracts.
Separate retrieval, parsing, normalization, and business validation.

Check project references before moving types.
Avoid circular dependencies.

### Frontend

Responsible for:

- presentation;
- form state;
- HTTP calls;
- displaying validation errors;
- authorized real-time updates.

The backend remains authoritative for permissions and business rules.

## Clean code and design patterns

- Apply single responsibility to classes and methods.
- Prefer composition over inheritance.
- Reuse existing services and contracts.
- Introduce interfaces for useful boundaries or substitution points.
- Do not create an interface automatically for every class.
- Avoid premature generalization.

Use patterns only for demonstrated needs:

- Repository for existing persistence boundaries.
- Strategy for genuinely interchangeable policies.
- Adapter for external providers.
- Dependency injection for external dependencies.

Explain the benefit and tradeoffs of a new pattern.

Do not introduce CQRS, MediatR, generic repositories, event buses,
microservices, or extra layers merely to appear professional.

Prefer incremental improvements over wholesale rewrites.

## Nullability and valid state

- Avoid unnecessary null values.
- Return empty collections for successful queries with no elements.
- Represent genuine optional values with nullable types.
- Handle not-found results explicitly.
- Validate required inputs and configuration at boundaries.
- Initialize required state appropriately.

Do not hide missing values using:

- arbitrary empty strings;
- zero identifiers;
- dummy entities;
- unjustified null-forgiving operators;
- fabricated defaults.

Do not disable nullable warnings to hide defects.

Follow the existing convention for expected failures.
Do not introduce a new Result framework without a concrete need.

Use exceptions for exceptional failures.
Never swallow an exception and report success.

## Constants and configuration

- Replace unexplained business numbers with meaningful constants,
  enums, or validated configuration.
- Use enums for finite domain states where compatible with contracts.
- Use framework constants when available.
- Centralize repeated role names and protocol values appropriately.
- Keep tunable values in configuration.

Examples:

- scraping timeouts;
- maximum search depth;
- academic period;
- JWT lifetime;
- external service endpoints.

Ordinary zero and one values do not automatically require constants.
Do not create a generic Constants class containing unrelated values.

## Course concepts

Use relevant course concepts where they improve implementation:

- delegates and lambdas;
- Func and Action;
- closures;
- LINQ;
- IEnumerable;
- yield return;
- dependency injection;
- Entity Framework Core.

Do not insert these concepts artificially.

Keep LINQ readable.
Prefer ordinary loops for complex procedural logic.

Be explicit about deferred execution.
Do not return an unexecuted EF query depending on a disposed context.

## Domain rules

- A student can have assignments in several study blocks.
- Changing one assignment must preserve unrelated assignments.
- A student cannot have multiple active group assignments for
  the same study block in the applicable academic scope.
- A requested group must belong to the same study block
  as the current assignment.
- Administrative virtual categories are separate from assignments.
- Virtual categories are administrator-managed and hidden from students.

When approving a request, revalidate:

- current assignment;
- ownership;
- request status;
- destination block;
- capacity;
- timetable compatibility.

Swap proposals must:

- concern groups in the same study block;
- support cycles involving at least three students;
- never reuse a request or assignment within a proposal;
- apply all changes or none;
- revalidate persisted state before approval.

Use transactions and an appropriate concurrency strategy.
A previous capacity check does not guarantee capacity at commit time.

Distinguish study blocks, activities, groups, and assignments.
Report ambiguous terminology before changing the schema.

## Timetables and scraping

- Calculate conflicts deterministically in C#.
- Use validated Hyperplanning data.
- Include relevant courses across the student's study blocks.
- Define interval boundaries and cancellation behavior.
- Treat missing or stale coverage as unknown, not compatible.
- Make academic dates and timetable timezone explicit.
- Do not blindly convert local course times to UTC.

Separate:

- source retrieval;
- parsing;
- normalization;
- business validation;
- persistence.

Never invent source fields, sessions, dates, or protocol values.

Document repeated-import, update, and cancellation behavior.
Do not delete sessions merely because a partial scrape omitted them.

## Persistence and asynchronous operations

- Use asynchronous EF Core APIs for database I/O.
- Propagate CancellationToken where relevant.
- Do not use Task.Run to disguise synchronous code.
- Never run concurrent operations on the same DbContext.
- Never inject a scoped DbContext into a singleton.
- Use deliberate, bounded database queries.
- Use projections and AsNoTracking where appropriate.
- Preserve database constraints and existing migrations.

Do not reset a database to make tests pass.

## Authentication and authorization

- Protect the API by default.
- Mark intentionally public endpoints explicitly.
- Derive identity and roles from validated authentication.
- Check resource ownership on the backend.
- Never trust frontend-supplied identity or role claims.

Validate JWT:

- signature;
- issuer;
- audience;
- expiration.

Use established password hashing or the existing Identity setup.
Do not implement custom password cryptography.

Never commit or log:

- passwords;
- JWTs;
- signing keys;
- API keys;
- unnecessary personal data.

Return useful errors without internal exception details.

## Application AI

- AI interprets free-text reasons into structured criteria.
- Validate fields, types, values, and contradictions.
- Treat user text and model output as untrusted data.
- Deterministic C# checks capacities, assignments, and timetables.
- AI must not approve requests or directly change assignments.
- Keep ordinary processing available when AI is unavailable.
- Call the provider from the backend.
- Minimize personal data sent to the provider.
- Do not add a chatbot.

Respect the agreed administrator-controlled AI workflow.
Report conflicts with the written specification.

## SignalR

- Authenticate connections.
- Authorize recipients and group membership on the server.
- Send only information recipients are allowed to receive.
- Publish events after the operation has committed.
- Never announce a rolled-back change as successful.
- Handle connection loss and authentication expiry.

## CSV imports and exports

- Document CSV formats and stable identifiers.
- Report invalid rows, duplicates, and unknown references.
- Make allocation constraints testable.
- Make randomness reproducible in tests.

Exports must:

- enforce backend authorization;
- use persisted assignments;
- respect the selected study block;
- support empty groups;
- preserve accented characters;
- escape CSV values correctly;
- neutralize formula-like spreadsheet values;
- produce readable PDFs with appropriate pagination.

## Testing and verification

- Test observable behavior and important invariants.
- Follow existing test frameworks and conventions.
- Add regression tests for confirmed bugs.
- Keep ordinary tests independent of live Hyperplanning and paid AI.
- Separate unit, database, API/security, and live external tests.
- Use relational providers for relational constraints and transactions.
- Do not assume EF Core InMemory proves relational behavior.
- Do not claim SQLite proves SQL Server-specific behavior.
- Control time and randomness when relevant.

Never:

- weaken assertions to hide failures;
- remove authorization to make tests pass;
- replace production validation with a test stub;
- report an unexecuted check as successful.

A test count is not a coverage percentage.
Source inspection is not proof that tests pass.

## Documentation

Write technical documentation in English.
Explain documentation work to the user in French.

Use 0 docs as the main documentation location.
Inspect existing organization before creating new documents.
Maintain a linked documentation index.
Avoid duplicate sources of truth.

Distinguish:

- specified requirements;
- implemented behavior;
- partially implemented behavior;
- proposed design;
- approved decisions;
- verified results;
- unverified assumptions.

Ground documentation in code, configuration, migrations,
requirements, tests, and actual execution evidence.

Cover the relevant project needs:

- requirements and implementation status;
- architecture and project responsibilities;
- significant design decisions and tradeoffs;
- UML diagrams;
- Merise MCD, MLD, and physical database model;
- database data dictionary and constraints;
- API contracts and security;
- Hyperplanning and AI integrations;
- SignalR events;
- installation and configuration;
- Student and Admin user guides;
- maintenance and troubleshooting;
- test strategy and known limitations.

Keep diagram source editable.
Prefer Mermaid when suitable.
Use another format only when justified.

Do not label an ER diagram as conventional Merise notation
without explaining the representation.

Distinguish database schema inferred from migrations
from a schema inspected in a running database.

Never invent screenshots, successful commands, test results,
deployments, backups, restores, or implemented features.

Keep examples free of secrets and unnecessary personal information.

## Working process and approval

For analysis-only requests:

1. Inspect the relevant sources.
2. Present findings and evidence.
3. Propose a correction or implementation plan.
4. Identify exact affected files and verification steps.
5. Stop and wait for explicit approval before changing files.

For approved implementation:

1. Inspect existing changes.
2. Confirm the approved scope.
3. Implement focused changes.
4. Run the appropriate checks.
5. Review the diff.
6. Report results and limitations.

Approval applies to the agreed scope.
Do not repeatedly request permission for work already approved.
Ask before a material expansion of that scope.

If the user requests absolutely no file changes, do not run
commands that generate artifacts without first obtaining approval.

From the solution directory, verify the actual solution supports:

- dotnet build GroupSwitch.sln
- dotnet test GroupSwitch.sln

Discover frontend commands from its package configuration.

Preserve unrelated changes.
Do not commit, push, deploy, delete data, or apply destructive
migrations without an explicit request.

Document significant AI assistance and verification where relevant
to the academic deliverables.

## Agent roles

### Main agent

Own coordination, implementation, and integration.
Consolidate findings and present proposals to the user.

### schedule_explorer

Investigate scheduling, imports, assignments, and relevant code paths.
Do not modify files.

### dotnet_reviewer

Review correctness, security, concurrency, and maintainability.
Do not modify files.

### test_engineer

Design, implement, and run tests within the approved delegated scope.
Report production defects to the implementation owner.
Do not modify production code without explicit authorization.

### senior_software_analyst

Assess requirements coverage, working workflows, delivery risks,
and development priorities using repository evidence.
Remain read-only during assessments.

### senior_software_architect

Assess responsibilities, dependencies, data boundaries,
testability, and folder organization.
Propose incremental improvements.
Remain read-only during assessments.

### documentation_engineer

Maintain architecture, modeling, database, API, installation,
user, and maintenance documentation.
During assessments, report documentation gaps without editing.
After approval, update only authorized documents.

## Agent communication and coordination

The main agent coordinates communication.

Use direct messages when runtime tools support them.
Otherwise, relay findings through the main agent.

Never claim communication, delegation, or execution occurred
unless it actually occurred.

Give each agent:

- a focused question;
- relevant context;
- a clear scope;
- expected evidence and output;
- applicable editing restrictions.

For coordinated project assessments:

1. Ask the analyst to assess requirements and priorities.
2. Give the architect the analyst's findings for independent review.
3. Return architectural feedback to the analyst when needed.
4. Resolve or explicitly report disagreements.
5. Give reconciled findings to the documentation agent.
6. Present one consolidated proposal to the user.

Each handoff must identify:

- verified findings;
- file or requirement references;
- proposed decisions;
- unresolved questions;
- requested feedback.

Do not resolve disagreements by majority vote.
Use requirements, code, tests, and explicit user decisions.

Avoid concurrent agents editing the same files.
Run dependent tasks in order.
Parallelize only genuinely independent work.

Keep the number of active agents within runtime limits.
Available roles do not all need to run simultaneously.

Do not invoke every agent for a small local change.
Review delegated findings before accepting them.

Agent instructions do not replace runtime permissions.

## Project assessment output

Before implementing assessment recommendations, present:

- an evidence-based overall assessment;
- implemented and unverified behavior;
- missing mandatory requirements;
- confirmed defects and risks;
- architecture and documentation gaps;
- prioritized next tasks;
- exact affected files for the first task;
- acceptance criteria;
- verification steps;
- unresolved questions.

Do not invent completion percentages or delivery estimates.
Do not reorganize code during an assessment.
Wait for approval of the proposed scope.
