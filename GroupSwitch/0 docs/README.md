## Import behavior

- One Hyperplanning response contains one week of one group.
- An import replaces, for each group, only the weeks that were actually scraped.
  Sessions of other weeks are never deleted, so a partial import cannot lose data.
- A scraped week with no sessions clears that week for the group.
- A session cancelled after the first import is flagged `IsCancelled` at the next import
  that scrapes its week.
- A failed import keeps the groups already saved. The group being read when the failure
  happens is not saved at all (its transaction is never started).
- Session ids are not stable across imports (sessions are deleted and re-created).
  Nothing may reference a session id until an upsert strategy exists.
- Dates are local academic time (Europe/Brussels), stored without conversion to UTC.
- Tests run on SQLite. They do not prove SQL Server behavior.
