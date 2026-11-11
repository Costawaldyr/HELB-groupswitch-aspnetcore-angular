---
name: groupswitch-import
description: Diagnose or implement GroupSwitch Hyperplanning scraping, parsing, schedule normalization and persistence. Use for missing sessions, duplicate imports, cancellations, date errors and partial failures.
---

# Hyperplanning Import Workflow

1. Read applicable AGENTS.md instructions and relevant requirements.

2. Trace the actual import pipeline:
   - source retrieval;
   - group selection;
   - period selection;
   - parsing;
   - date calculation;
   - normalization;
   - persistence;
   - import reporting.

3. Establish stable identifiers and expected source coverage.
   Distinguish captured source data from synthetic fixtures.

4. Reproduce the issue with the smallest relevant fixture.
   Never invent Hyperplanning properties, endpoints, or protocol values.

5. Define repeated-import behavior before modifying persistence.
   Distinguish new sessions, updates, duplicates, and cancellations.

6. Do not treat an omitted session in an incomplete response
   as a confirmed deletion.

7. Check:
   - group mapping;
   - academic dates;
   - weeks and recurrence;
   - timetable timezone;
   - teachers and rooms;
   - cancellations;
   - duplicate sessions;
   - partial failures.

8. Apply a targeted correction using existing contracts.
   Keep transaction boundaries consistent with the import policy.

9. Test the changed behavior using local fixtures.
   Include repeated imports and malformed responses when relevant.

10. Run live scraping checks only when requested.
    Report fixture tests and real-source verification separately.

11. Explain:
    - the cause;
    - changes made;
    - checks executed;
    - actual source coverage;
    - remaining limitations.
