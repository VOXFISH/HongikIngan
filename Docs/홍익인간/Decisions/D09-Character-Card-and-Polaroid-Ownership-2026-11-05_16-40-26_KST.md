# D09 — Character Card and Polaroid Ownership — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

The user wants keyword/action/Polaroid information on the character card. Polaroids can be obtained for characters not recruited.

## Recommendation

Add a future Polaroid-definition reference to the card definition; keep the owned collection in run/inventory state, independent of recruited instances. Skill rules may reference keyword definitions.

## Decision required

Define the missing card-to-Polaroid connection and collection identity/count semantics. No new Polaroid values or acquisition mechanics are approved here.

## Planned impact

Future character-card adapter, battle setup contribution, run/inventory integration. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

A Polaroid for an unowned character contributes without requiring a recruited character instance; totals are not duplicated on every character.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
