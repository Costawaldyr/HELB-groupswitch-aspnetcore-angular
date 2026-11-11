---
name: groupswitch-test
description: Design, implement and run focused GroupSwitch tests for changed behavior, regressions, business invariants, persistence and authorization. Use when adding tests or investigating test failures.
---

# Testing Workflow

1. Read applicable AGENTS.md instructions.

2. Inspect:
   - the requested behavior;
   - implementation and public contracts;
   - existing test projects;
   - test framework and dependencies;
   - relevant specification requirements.

3. Define expected outcomes independently of implementation details.
   Do not reproduce the same algorithm inside the test.

4. Choose the appropriate test level:
   - unit test for deterministic business logic;
   - relational integration test for persistence and transactions;
   - API integration test for HTTP and authorization;
   - explicit live check for external services.

5. Build the smallest meaningful test matrix:
   - successful scenario;
   - relevant boundary;
   - invalid input;
   - failure or regression scenario.

6. Use clear test names:
   MethodName_Condition_ExpectedBehavior.

7. Structure tests using Arrange, Act, Assert.
   Use descriptive variable names and minimal fixtures.

8. Control time, randomness, and external responses.
   Avoid sleeps, live AI calls, and shared database dependencies.

9. Use the existing dependencies where possible.
   Do not add a mocking library without a concrete need.

10. Run the focused tests first.
    Run broader checks when the change or failure justifies them.

11. For a regression test, demonstrate failure before the fix
    when feasible without disturbing unrelated work.

12. Do not modify production behavior merely to satisfy a test.
    Report defects to the implementation owner.

13. Report:
    - tests added or changed;
    - commands executed;
    - results;
    - skipped or blocked checks;
    - remaining coverage gaps.

Never describe an unexecuted test as passing.
