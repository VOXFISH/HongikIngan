# D01 — Modular Battle Structure — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

Characters and content are added later; game creation is on hold.

## Recommendation

Separate flow, runtime state, content adapters, and presentation. Keep character-specific branches out of BattleController.

## Decision required

Choose the minimal adapter contracts and whether definitions use ScriptableObjects. Class names remain proposals.

## Planned impact

BattleController, BattleSetup, BattleContracts, BattleSceneInstaller. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

A synthetic behavior can be registered without editing the controller; no production content is required.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
