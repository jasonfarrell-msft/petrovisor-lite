# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, Clean Architecture layering, project scaffolding | Leia | Solution structure, layer boundaries, ADRs |
| Backend / API / EF Core / Auth | Han | ASP.NET Core controllers, EF Core entities/migrations, JWT/Identity |
| KPI & analytics logic | Obi-Wan | Decline rate, production loss detection, artificial lift flags |
| Frontend / React / dashboards | Luke | React components, charts, auth context/hook |
| DevOps / IaC / Docker | Lando | Dockerfiles, Bicep, CI scaffolding |
| Feature scoping / work item breakdown / "new feature" requests | Smith | Decompose a feature idea into sequenced, assignable work items grounded in upstream O&G + AI domain knowledge |
| Agile/Scrum methodology, Epic/Feature/Work Item/Sprint Task leveling & sizing, sprint-readiness review | Yoda | Define conventions for work-item hierarchy; review Smith's breakdowns for structural alignment before finalizing |
| Code review | Leia | Review PRs, check quality, suggest improvements |
| Testing | Han (backend/xUnit), Luke (frontend/Jest) | Write tests, find edge cases, verify fixes |
| Scope & priorities | Leia | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |
| RAI review | Rai | Content safety, bias checks, credential detection, ethical review |
| Verification / devil's advocate | Fact Checker | Claim verification, pre-mortem, design challenge |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Planning Workflow — Smith + Yoda

Whenever Smith produces an Epic write-up, feature breakdown, or sprint plan intended to become a GitHub issue/Project item:

1. Coordinator spawns Smith to draft the breakdown (as today).
2. **Before finalizing**, coordinator spawns Yoda (background is fine, but must complete before the `gh issue create`/Project step) to review Smith's draft against Agile/Scrum conventions.
3. If Yoda returns ✅ Aligned → proceed to create the issue/Project item.
4. If Yoda returns ⚠️ Aligned with notes or 🔴 Misaligned → route the specific feedback back to Smith for revision. Smith revises; re-review by Yoda is not required for minor ⚠️ notes already addressed, but is required after a 🔴 revision.
5. Present Yoda's verdict to the user alongside Smith's final breakdown so the methodology check is visible, not silent.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
