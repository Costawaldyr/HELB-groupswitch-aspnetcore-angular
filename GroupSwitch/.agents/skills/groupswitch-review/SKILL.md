---
name: groupswitch-review
description: Review GroupSwitch changes for business correctness, security, clear English naming, responsibility boundaries, null handling and meaningful tests. Use for code reviews and specification compliance checks.
---

# Review Workflow

1. Read applicable instructions and establish the review scope.

2. Inspect changed files and relevant callers.
   Do not modify files during a review-only request.

3. Prioritize:
   - authorization defects;
   - lost or unrelated assignments;
   - capacity overruns;
   - partial swaps;
   - incorrect timetable calculations;
   - invalid AI output handling;
   - incorrect export scope;
   - unauthorized SignalR events.

4. Check:
   - clear English names;
   - focused methods;
   - cohesive classes;
   - justified design patterns;
   - explicit absence handling;
   - meaningful constants and configuration;
   - appropriate test coverage.

5. Distinguish:
   - confirmed defects;
   - verification gaps;
   - optional improvements.

6. For each actionable finding, provide:
   - severity;
   - file and location;
   - triggering scenario;
   - impact;
   - correction direction.

7. State which tests were inspected and which were executed.

8. Return findings first.
   If no actionable defects are found, state the review limitations.
