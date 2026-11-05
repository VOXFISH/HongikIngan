# D11 — Battle Phases and Result Boundaries — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

Boss defeat returns to classroom. Surviving boss permits at most three turns; turn-three victory takes priority. Rewards and next-stage flow are outside current scope.

## Recommendation

Initialize → prepare → lock → extra slots/members → placement → actions/reactions → Polaroids → boss keywords → chant/beam → damage once → completed-turn count and outcome. Count synergies immediately before chanting.

## Decision required

Define placement trigger count/targets and entry/exit integration. Preserve chant ties: region, job synergy, special synergy, then Korean alphabetical order; display-only role is not job synergy.

## Planned impact

BattleController, BattleSetup, IBattleTarget, BattlePresentation. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

Exact-zero and negative HP win on turns 1–3; positive HP after turn 3 loses. Cosmetic randomness does not alter final damage.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
