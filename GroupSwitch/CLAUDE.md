# CLAUDE.md

@AGENTS.md

AGENTS.md is the source of truth for architecture, style, security and testing.
This file only adds rules for how Claude works with the student on this repository.

## Priority when rules conflict

1. The written specification (`projet_dotnet_IV_v1_1.md`).
2. `AGENTS.md`.
3. The teacher's course examples (style only; never copy their shortcuts).
4. Conventions from earlier chat messages.

## Communication

- Explain in simple French. The student is not a native speaker:
  explain each technical term the first time it appears.
- Code identifiers and technical documentation in English.
  User-facing messages (API errors shown in the UI, import journal) stay in French.
- When the student asks, show code and fixes directly in the chat. Never zip files.

## Way of working

- Small pieces of 15 to 25 minutes, one branch per piece
  (`feature/...`, `fix/...`, `refactor/...`), with tests before the commit.
- Conventional commits. Do not commit, push, deploy, delete data
  or apply a migration unless asked.
- State clearly what was executed and what was only read.
  Never claim that a build or a test passed without running it.
- End each piece with a one-question mini-quiz.

## Conventions already used in the code

- Private fields have no underscore. Constructor parameters are written `_name`
  and assigned to the field.
- Interfaces only at real boundaries (persistence, Hyperplanning, AI, services used
  by controllers) or when a test needs a substitute. No interface per class.
- Optional values are nullable types. No `!` in production code,
  no empty-string or zero sentinels.
- An import replaces only the (group, week) pairs that were actually scraped.
  Never delete sessions outside the scraped weeks.
- Tests are named `Method_Condition_Result`. Live Hyperplanning tests carry
  `[Trait("Category", "Online")]` and are excluded from CI.
- SQLite tests do not prove SQL Server behavior. Say so when a test depends on it.
