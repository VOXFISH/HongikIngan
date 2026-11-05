# Recommendation Log — 2026-11-05 16:40:26+09:00

All entries below were recorded at 2026-11-05 16:40:26+09:00; this is not the original time of the referenced comments. This log records recommendations and corrections, not approvals or implementation completion.

| Entry | Source | Topic | Recommendation / change | Status |
| --- | --- | --- | --- | --- |
| R01 | C01, C02, C03 | [D01 Modular Battle Structure](D01-Modular-Battle-Structure-2026-11-05_16-40-26_KST.md) | Separate flow, runtime state, content adapters, and presentation. Keep character-specific branches out of BattleController. | Proposed; decision pending |
| R02 | C02, C03 | [D02 Formation Ownership and Saved Locations](D02-Formation-Ownership-and-Saved-Locations-2026-11-05_16-40-26_KST.md) | FormationState alone owns SlotId-to-participant assignments. Keep stable IDs separate from logical order and screen position. Derive slotIndex. Swap atomically; preserve mappings between turns and through visual layout changes. | Proposed; decision pending |
| R03 | C01, C02, C03 | [D03 Action Scheduling and Turn Ownership](D03-Action-Scheduling-and-Turn-Ownership-2026-11-05_16-40-26_KST.md) | TurnActionState owns normal-action consumption; expose hasResolvedSlotAction as a derived lookup. Initially propose capturing participant IDs after placement, with one normal opportunity each. Reactions are separate. | Proposed; decision pending |
| R04 | C01, C02, C03 | [D04 Trigger Ordering and Reaction Chains](D04-Trigger-Ordering-and-Reaction-Chains-2026-11-05_16-40-26_KST.md) | Use an explicit resolver queue, event identity and parent identity; snapshot subscriber order per event. Drain the current actor's reactions before advancing. Keep slot opportunities, actions, effects and reactions distinct. | Proposed; decision pending |
| R05 | C02, C03 | [D05 Target Selection and Evaluation Timing](D05-Target-Selection-and-Evaluation-Timing-2026-11-05_16-40-26_KST.md) | TargetSelector delegates to strategies. Evaluate conditions and read runtime values near execution; snapshot ordering separately from targets and values. Validate target/effect compatibility. | Proposed; decision pending |
| R06 | C01, C02, C03 | [D06 Float Damage and Finalization](D06-Float-Damage-and-Finalization-2026-11-05_16-40-26_KST.md) | Keep calculation and formatting separate. Distinguish addition, percentage-point addition and multiplication. Freeze final damage and apply once per resolution ID. | Proposed; decision pending |
| R07 | C01, C02, C03 | [D07 Keyword State and Turn Persistence](D07-Keyword-State-and-Turn-Persistence-2026-11-05_16-40-26_KST.md) | Route all grants through a keyword state operation; separate temporary battle state from persistent upgrades. Keep turn-reset policy explicit. | Proposed; decision pending |
| R08 | C01, C02, C03 | [D08 Character Identity and Upgrade Integration](D08-Character-Identity-and-Upgrade-Integration-2026-11-05_16-40-26_KST.md) | Validate characterId uniqueness and keep runtime instance identity. Read current skillVariables during actions. A later upgrade service validates then commits payment, replacement and count together outside combat. | Proposed; decision pending |
| R09 | C02, C03 | [D09 Character Card and Polaroid Ownership](D09-Character-Card-and-Polaroid-Ownership-2026-11-05_16-40-26_KST.md) | Add a future Polaroid-definition reference to the card definition; keep the owned collection in run/inventory state, independent of recruited instances. Skill rules may reference keyword definitions. | Proposed; decision pending |
| R10 | C01, C02, C03 | [D10 Input Presentation and Unity Lifecycle](D10-Input-Presentation-and-Unity-Lifecycle-2026-11-05_16-40-26_KST.md) | Lock dragging and attack input synchronously on confirmation. Keep UI read-only except command requests. Use a resolution ID and cancellable playback; animation completion asks the controller to apply damage. | Proposed; decision pending |
| R11 | C01, C02, C03 | [D11 Battle Phases and Result Boundaries](D11-Battle-Phases-and-Result-Boundaries-2026-11-05_16-40-26_KST.md) | Initialize → prepare → lock → extra slots/members → placement → actions/reactions → Polaroids → boss keywords → chant/beam → damage once → completed-turn count and outcome. Count synergies immediately before chanting. | Proposed; decision pending |
| R12 | C01, C02, C03, C04 | [D12 Implementation Stages and Validation](D12-Implementation-Stages-and-Validation-2026-11-05_16-40-26_KST.md) | After authorization: types/setup → formation → turn flow → resolver and strategies → numeric/target boundaries → editor UI → presentation/results → integration tests. Validate IDs/references before battle. Update stale undecided lists when decisions are approved. | Proposed; decision pending |

## Corrections to earlier recommendations

| Earlier comment | Updated evidence | Disposition |
| --- | --- | --- |
| C02 used two owned copies as an identity example. | Updated attributes prohibit duplicate characterId recruitment. | Example superseded; preserve runtime identity without permitting duplicates. |
| C01/C02 left numeric storage open. | Updated design requires float. | Storage settled; formulas and invalid/range behavior remain open. |
| C02 scheduler and character field could each track action consumption. | C03 identified duplicate state ownership. | Recommend one TurnActionState owner; not implemented. |
| C02 command generation could capture values/targets too early. | C03 identified sequential keyword interactions. | Recommend separating order snapshot from evaluation time; exact timing pending. |

## How to maintain this log

Append new dated entries when recommendations change. Record explicit acceptance/rejection and its source; do not silently change historical recommendations into confirmed rules. Update the corresponding decision record and authoritative design documents when a decision is actually made. Track implementation and validation separately.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md)
