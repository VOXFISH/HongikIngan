# D05 — Target Selection and Evaluation Timing — 2026-11-05 16:40:26+09:00

- Recorded: 2026-11-05 16:40:26+09:00 (Asia/Seoul). This is the recording time, not an inferred message time.
- Status: Proposed / decision pending. Confirmed constraints below retain their existing authority.
- Source comments: [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- References: [Game System Design](../Game%20System%20Design.md), [Magical Girl Attributes](../Magical%20Girl%20Attributes.md).

## Confirmed constraints

Target selection uses a separate strategy function; role is never a combat filter.

## Recommendation

TargetSelector delegates to strategies. Evaluate conditions and read runtime values near execution; snapshot ordering separately from targets and values. Validate target/effect compatibility.

## Decision required

Decide condition timing, shared versus per-effect target selection, no-target behavior, multi-target order, random selection and invalidated targets.

## Planned impact

TargetSelector, ActionRule adapter, IBattleCommand. These are proposed responsibilities; no scripts are created by this record.

## Acceptance evidence to collect later

An earlier keyword grant can affect a later condition under the chosen timing; a position swap cannot target the team-damage accumulator.

## Decision record

- Owner: Unassigned.
- Decision: Pending; no acceptance inferred from encouragement or documentation requests.
- Decision time: Not yet recorded.
- Implementation: Not started.

[Decision index](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [Recommendation log](Recommendation-Log-2026-11-05_16-40-26_KST.md)
