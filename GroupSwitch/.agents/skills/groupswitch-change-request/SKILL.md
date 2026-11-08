---
name: groupswitch-change-request
description: Implement or diagnose GroupSwitch assignment changes, allocation, capacity checks, timetable compatibility and multi-student swap cycles. Use when processing change requests or approving exchange proposals.
---

# Change Request Workflow

1. Read applicable instructions and relevant specification sections.

2. Trace the endpoint, business service, repositories,
   assignment model, and authorization checks.

3. Establish:
   - authenticated actor;
   - student ownership;
   - academic scope;
   - current assignment;
   - requested group;
   - current request status.

4. Define acceptance cases:
   - destination belongs to the same study block;
   - capacity is respected;
   - relevant timetables are compatible;
   - unrelated assignments remain unchanged;
   - approval is authorized.

5. For swap cycles, verify:
   - support for at least three students;
   - same-block consistency;
   - no reused request or assignment;
   - bounded search behavior.

6. Plan atomic persistence and concurrency handling.
   Revalidate stale proposals before committing.

7. Keep AI interpretation separate from deterministic validation.
   Missing timetable coverage must remain explicitly unknown.

8. Implement the smallest cohesive change.

9. Test relevant cases:
   - successful change;
   - full destination group;
   - wrong study block;
   - stale request;
   - unauthorized ownership;
   - preserved unrelated assignments;
   - complete rollback after an exchange failure.

10. Publish authorized SignalR events only after successful commit.

11. Report before-and-after behavior and verification evidence.
