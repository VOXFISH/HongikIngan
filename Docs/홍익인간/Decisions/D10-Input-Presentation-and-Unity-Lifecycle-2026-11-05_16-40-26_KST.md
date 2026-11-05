# D10 — Input Presentation and Unity Lifecycle — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

UI assets are to be authored inside the Unity Editor. Relic throwing is cosmetic; attack-position movement and relic throwing are deferred.

## Recommendation

Lock dragging and attack input synchronously on confirmation. Keep UI read-only except command requests. Use a resolution ID and cancellable playback; animation completion asks the controller to apply damage.

## Decision required

Confirm input-lock policy, exact impact timing, interruption/re-entry behavior and whether cinematic movement fires gameplay events. Recommend visual-only movement remain separate from logical swaps.

## Planned impact

BattleSceneInstaller, BattleHud, SlotView, CharacterCardView, BattlePresentation. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

Awake validates references, Start initializes, OnDestroy disposes; listener binding works regardless of enable/bind order. Interrupted playback cannot leave a hidden permanent wait.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
