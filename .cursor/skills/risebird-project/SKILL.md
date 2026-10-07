---
name: risebird-project
description: >-
  Risebird monorepo navigation — solution layers, doc index, and when to load
  scoped docs. Use for any Risebird task before exploring the full codebase.
---

# Risebird Project

## Quick map
| Project | Role |
|---------|------|
| Risebird.Web | MVC portal |
| Risebird.AzureTimerFunctions | Timers (Plivo, search crawl, purge) |
| Risebird.Business / Repository | Logic + Dapper SQL |
| Risebird.DB | SQL scripts |
| Risebird.CommonHelper | External APIs |

## Read next (one doc only)
| Task | File |
|------|------|
| Structure | `Docs/Architecture.md` |
| Plivo/Vapi | `Docs/VoiceAgents.md` |
| SQL | `Docs/Database.md` |
| Search indexes | `Docs/AzureSearch.md` |
| Deploy | `Docs/Deployment.md` |

Full index: `Docs/README.md`

## Critical schema
- `IRF.id` (int) vs `IRF.IRFId` (varchar) — do not mix in joins
- `@NowIST` for meetings; UTC for Plivo `CreatedAt`
