# D02 — Formation Ownership and Saved Locations — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

The user requires editable formation and remembered locations after swapping. slotIndex in the attributes document is a candidate field.

## Recommendation

FormationState alone owns SlotId-to-participant assignments. Keep stable IDs separate from logical order and screen position. Derive slotIndex. Swap atomically; preserve mappings between turns and through visual layout changes.

## Decision required

Confirm basic/max slots, empty-slot rules, extra-member selection and lifetime, occupied-slot removal, and whether persistence must survive restarting the application. Reject occupied-slot removal until relocation is defined.

## Planned impact

FormationState, SlotView, CharacterCardView. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

Swapping updates both occupants once; invalid drops preserve state; capture/restore validates all IDs before applying changes.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
