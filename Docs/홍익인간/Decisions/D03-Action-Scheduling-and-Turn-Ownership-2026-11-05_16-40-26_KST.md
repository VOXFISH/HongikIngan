# D03 — Action Scheduling and Turn Ownership — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

Actions begin from the left. Movement can occur. Post-movement action order remains undecided.

## Recommendation

TurnActionState owns normal-action consumption; expose hasResolvedSlotAction as a derived lookup. Initially propose capturing participant IDs after placement, with one normal opportunity each. Reactions are separate.

## Decision required

Choose captured order versus dynamic slot order; define moved/removed actors, skipped actions, and whether extra participants join an already-built schedule. Captured order is not approved gameplay.

## Planned impact

ActionScheduler, TurnActionState, BattleParticipantState. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

Swapping acted and unacted participants neither accidentally duplicates nor loses normal action opportunities under the selected policy.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
