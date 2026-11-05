# D06 — Float Damage and Finalization — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

Damage uses float. Normal display truncates fractions and groups three digits; above ten digits scientific notation uses two decimal places. Final modifiers are Polaroids then boss keywords.

## Recommendation

Keep calculation and formatting separate. Distinguish addition, percentage-point addition and multiplication. Freeze final damage and apply once per resolution ID.

## Decision required

Define initial value, percentage-point base, arithmetic ordering, actual modifier values, invalid/range-overflow behavior and scientific-rounding boundaries. Do not replace float with another numeric type.

## Planned impact

DamageAccumulator, DamageFormatter, IDamagePolicy, BattleController. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

Displayed truncation never changes applied damage; repeated/stale completion callbacks do not apply damage twice.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
