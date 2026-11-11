# AI Agent Guide for GroupSwitch

This guide explains how to create a precise AI-agent request for this project and how to work in a way that matches the repository's structure, technical constraints, and course requirements.

## 1. Project conventions to respect

Before changing code, always respect `GroupSwitch/AGENTS.md`.

Key rules:

- Write all code identifiers in English.
- Write code comments and technical documentation in English.
- Explain decisions to the user in French unless the instruction says otherwise.
- Preserve unrelated changes.
- Do not commit, push, or apply database migrations.
- Keep the solution structure unchanged.
- Keep the work focused on the relevant layer: `Api`, `Business`, `Data`, `Tests`.

## 2. Required solution structure

The project should follow this structure:

```text
GroupSwitch/
├── .github/workflows/ci.yml
├── frontend/                      (Angular)
├── docs/                          (report, screenshots, README)
└── backend/
    ├── GroupSwitch.Api/           Controllers, SignalR Hubs, Program.cs
    ├── GroupSwitch.Business/       Business rules, scraping, AI interfaces and services
    ├── GroupSwitch.Data/           Entities, DbContext, repositories, migrations
    └── GroupSwitch.Tests/          Automated tests
```

The recommended .NET project references are:

```bash
dotnet add GroupSwitch.Business reference GroupSwitch.Data
dotnet add GroupSwitch.Api reference GroupSwitch.Business GroupSwitch.Data
dotnet add GroupSwitch.Tests reference GroupSwitch.Business GroupSwitch.Data
```

## 3. Branching model

Use one branch per feature.

```text
main                  (always stable, protected)
├── feature/skeleton
├── feature/scraping
├── feature/domain-model
├── feature/csv-import
├── feature/auth
├── feature/change-requests
├── feature/schedule-conflicts
├── feature/chain-exchange
├── feature/ai-analysis
├── feature/signalr
├── feature/export
├── feature/frontend-xxx
└── docs/report
```

Example:

```bash
git checkout main
git pull
git checkout -b feature/scraping
git add .
git commit -m "feat(scraping): parse course list"
git push -u origin feature/scraping
```

## 4. Migrations

When creating EF Core migrations:

```bash
cd GroupSwitch.Api
dotnet ef migrations add InitialScrapingData --project ..\GroupSwitch.Data
dotnet ef database update --project ..\GroupSwitch.Data
```

Do not apply migrations unless explicitly requested.

## 5. Functional modules expected in the project

| Module          | Role                                                                                  |
| --------------- | ------------------------------------------------------------------------------------- |
| Scraping        | One-click retrieval of all courses for a quadrimester and persistence in the database |
| Domain          | Students, study blocks, groups, assignments, virtual categories, CSV import           |
| Auth            | JWT, 2 roles (Student, Admin), protected routes                                       |
| Change requests | Student fills a form; admin receives and decides                                      |
| Exchanges       | Cycles of 3 students or more, applied in a transaction                                |
| AI              | Analysis of free-text request, enabled only for admin                                 |
| Export          | CSV and PDF generation for groups                                                     |

## 6. Important AI role clarification

This is important for the project and for your notes.

The professor specifies that the AI:

- reads the free-text request and turns it into structured criteria (summary, preferences, constraints, missing information);
- never calculates timetable overlaps; this is done by C# code using scraped data;
- is never accessible to the student;
- is only triggered by the admin via an action such as "Analyze with AI";
- should not block the rest of the system if it fails.

The data flow should be:

1. Student creates a change request with free text.
2. Admin sees the request (SignalR updates the list).
3. Admin clicks "Analyze with AI".
4. Backend sends only the text, without name or identifier, to the AI.
5. AI returns JSON.
6. Backend validates the JSON.
7. C# engine searches for solutions using the criteria, validates capacity and timetable constraints.
8. Admin accepts or rejects the request.

### AI provider choice

Use:

- Groq for production-like external AI access;
- Ollama as a fallback local LLM without API key;
- `FakeRequestAnalyzer` for tests and offline behavior.

Recommended pattern:

```text
IRequestAnalyzer
├── GroqRequestAnalyzer
├── OllamaRequestAnalyzer
├── FakeRequestAnalyzer
```

Use dependency injection so the rest of the application does not need to change when the AI implementation changes.

API keys must stay in User Secrets, never in Git.

## 7. Simplified domain model

The project can use the following model:

```text
Student (Id, LastName, FirstName, Email)
StudyBlock (Id, Name: B1, B2, B3...)
StudentGroup (Id, Name, Capacity, StudyBlockId)
Assignment (Id, StudentId, StudentGroupId)
VirtualCategory (many-to-many with students)
ChangeRequest (Id, AssignmentId, WantedGroupId, Reason, Status, AiAnalysisJson)
Course (scraped sessions with start/end DateTime)
AppUser (ASP.NET Identity, roles: Student or Admin)
```

Important rules:

- A student can belong to multiple study blocks.
- A student can have only one active assignment per block.
- A change request targets one specific assignment, not the whole student profile.
- Use decimal only when precision is meaningful; it is not required for this project.

## 8. Test plan by feature

| Branch             | Tests to write                                                                       |
| ------------------ | ------------------------------------------------------------------------------------ |
| scraping           | JSON parser, date calculation, import without duplicates                             |
| domain-model       | group capacity, student across several blocks, one assignment per block              |
| csv-import         | invalid line reported without stopping, duplicate detection, unknown reference       |
| auth               | no token => 401, student on admin route => 403, expired or invalid JWT rejected      |
| change-requests    | same block only, one request changes only one assignment                             |
| schedule-conflicts | overlap detection, adjacent times are not conflicts                                  |
| chain-exchange     | cycle of 3 found, one request used once, transaction rollback on failure             |
| ai-analysis        | valid JSON accepted, malformed JSON rejected, AI failure does not break the workflow |
| signalr            | non-admin users do not receive admin events                                          |
| export             | CSV with accents and empty groups, PDF generation, unauthorized export refused       |

## 9. Six-day delivery plan

| Day | Goal                                                | Branch                                              |
| --- | --------------------------------------------------- | --------------------------------------------------- |
| 1   | Skeleton and CI setup                               | skeleton, scraping                                  |
| 2   | Finish scraping, data model, Angular setup          | scraping, domain-model, frontend-setup              |
| 3   | CSV import, JWT, roles, login page                  | csv-import, auth                                    |
| 4   | Change requests, time conflicts, chain exchange     | change-requests, schedule-conflicts, chain-exchange |
| 5   | AI analysis and SignalR                             | ai-analysis, signalr                                |
| 6   | CSV and PDF export, Angular polishing, final report | export, frontend-\*, docs/report                    |

## 10. Example of a precise AI request for this project

Use this format when creating a task for an AI coding agent:

```text
Use $groupswitch-change-request.

Goal:
Approve a student's change request without modifying their
assignments in other study blocks.

Workflow:
1. Ask schedule_explorer to trace the current assignment,
   approval, persistence, and timetable-validation flow.
2. Define the expected behavior from the specification.
3. Implement a focused correction.
4. Ask test_engineer to add and run the relevant tests.
   Delegate only the affected test files to this agent.
5. Ask dotnet_reviewer to review the final changes.
6. Resolve confirmed defects.
7. Rerun affected checks after corrections.

Respect AGENTS.md.
Preserve unrelated changes.
Do not commit, push, or apply database migrations.

Write code in English.
Explain the implementation and verification in French.
```

## 11. More detailed example for this repository

```text
Use $groupswitch-change-request.

Goal:
The student change request must be approved only for the targeted assignment and must not modify other study-block assignments.

Workflow:
1. Ask schedule_explorer to inspect the assignment approval flow and the rules that validate the target group, capacity, and timetable compatibility.
2. Read the functional specification to confirm the expected behavior.
3. Implement the minimum correction in the business logic and API layer if needed.
4. Ask test_engineer to add or update only the relevant unit and integration tests for the affected behavior.
5. Ask dotnet_reviewer to review the code and flag any issues.
6. Fix confirmed defects and keep the patch focused.
7. Rerun only the affected checks.

Constraints:
- Respect AGENTS.md.
- Preserve unrelated files and history.
- Do not commit, push, or apply database migrations.
- Keep code names in English.
- Explain the implementation and verification in French.
```

## 12. Good AI-task construction rules

When creating a request for an agent, prefer this structure:

- Clear objective: one business problem, one fix, one feature.
- Concrete workflow: inspect, define, implement, test, review, fix, verify.
- Explicit constraints: respect AGENTS.md, no unrelated edits, no commit, no migration.
- Clear coding language: English for code.
- Clear explanation language: French for reasoning and final explanation.

## 13. Good examples of agent requests by feature

### Feature: scraping

```text
Use $groupswitch-scraping.

Goal:
Import and store Hyperplanning course data for one quadrimester without creating duplicate sessions.

Workflow:
1. Inspect the current scraping service and parser flow.
2. Confirm the expected course model and date calculations.
3. Implement the fix in the scraping and persistence layer.
4. Ask test_engineer to add tests for the parser and duplicate handling.
5. Review the patch and rerun only the affected tests.

Respect AGENTS.md.
Do not commit or push.
Write code in English.
Explain the implementation and verification in French.
```

### Feature: AI analysis

```text
Use $groupswitch-ai-analysis.

Goal:
Analyze the free-text change request with AI while keeping the rest of the workflow functional if the AI is unavailable.

Workflow:
1. Inspect the current request-analysis flow and the dependency injection setup.
2. Define the contract for the AI response JSON.
3. Implement IRequestAnalyzer with Groq, Ollama, and FakeRequestAnalyzer.
4. Ask test_engineer to add tests for valid JSON, malformed JSON, and AI failure fallback.
5. Ask dotnet_reviewer to review the final patch.
6. Rerun the affected tests.

Respect AGENTS.md.
Preserve unrelated work.
Do not commit, push, or apply migrations.
Write code in English.
Explain the implementation and verification in French.
```

### Feature: change request approval

```text
Use $groupswitch-change-request.

Goal:
Approve a change request for one assignment only without altering other assignments of the same student.

Workflow:
1. Trace the approval flow and validation rules in the business layer.
2. Confirm the expected behavior from the specification.
3. Apply a minimal correction.
4. Ask test_engineer to add targeted tests.
5. Ask dotnet_reviewer to review the final patch.
6. Rerun the affected test suite.

Respect AGENTS.md.
Do not migrate the database.
Do not commit or push.
Write code in English.
Explain the implementation and verification in French.
```

## 14. Final reminder

The most important rule is simple:

- code in English;
- explanations in French;
- strict focus on the feature requested;
- respect of `AGENTS.md` and the repository structure;
- no commit, no push, no migration unless explicitly requested.

This keeps the project aligned with the course requirements and with the expected engineering workflow.
