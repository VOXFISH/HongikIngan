# D08 — Character Identity and Upgrade Integration — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

Duplicate recruitment is forbidden by characterId. Grades share prices. Roles are display-only. No character HP, defense, crit or speed exists. Upgrades replace a current variable; shared definitions remain unchanged.

## Recommendation

Validate characterId uniqueness and keep runtime instance identity. Read current skillVariables during actions. A later upgrade service validates then commits payment, replacement and count together outside combat.

## Decision required

Decide re-recruitment after release, upgrade replacement formula and actual prices. Example prices/formulas are temporary. My earlier two-owned-copies example is superseded.

## Planned impact

BattleSetup, ContentValidator, future owned-character adapter and upgrade service. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

After an upgrade, actions use the replacement value. Failed upgrades change neither currency, variables nor count; roles never change combat results.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
