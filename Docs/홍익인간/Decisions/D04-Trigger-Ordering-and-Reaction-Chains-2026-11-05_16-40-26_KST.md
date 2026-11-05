# D04 — Trigger Ordering and Reaction Chains — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

Simultaneous ordering is relic acquisition order then character slot order. Other-ally reactions exclude self.

## Recommendation

Use an explicit resolver queue, event identity and parent identity; snapshot subscriber order per event. Drain the current actor's reactions before advancing. Keep slot opportunities, actions, effects and reactions distinct.

## Decision required

Define action-event timing, reaction-on-reaction eligibility, repeat limits and termination. Decide whether events produced within multi-effect actions resolve between effects or after the action.

## Planned impact

BattleEvent, BattleResolver, IReactionPolicy. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

A multi-effect action does not accidentally count as multiple ally actions. Diagnostic overflow faults visibly rather than silently discarding effects.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
