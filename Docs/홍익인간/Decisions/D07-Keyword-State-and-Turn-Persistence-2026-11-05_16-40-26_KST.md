# D07 — Keyword State and Turn Persistence — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

Keyword definitions decide whether multiple stacks are allowed; nonstacking keywords cannot hold multiple stacks.

## Recommendation

Route all grants through a keyword state operation; separate temporary battle state from persistent upgrades. Keep turn-reset policy explicit.

## Decision required

Define stack addition/caps, reapplication ignore/refresh, source tracking, duration, party-wide condition semantics, damage resets, formation retention and boss recovery.

## Planned impact

BattleParticipantState, Keyword state operation, ITurnStatePolicy. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

Nonstacking grants stay single-stack; the chosen expiry/reset rules do not erase persistent upgrades or leak temporary state across battles.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
