# Domain model (clinical engine)

All types live in `clinical-core/Qaniva.Clinical.Core/`. Pure C#, no Unity.

## Definition-time (immutable, from `case.json`)

| Type | Purpose | Key fields |
| --- | --- | --- |
| `CaseDefinition` | the whole case | `id`, `version`, `metadata`, `presentationProfile`, `initialState`, `availableActions`, `transitionRules`, `scoringCriteria`, `terminalStates` |
| `ActionDefinition` | one thing the player can do | `id`, `type`, `timeCostSec`, `visibility`/`visibleWhen`, `preconditions[]`, `params[]`, `effects[]`, `criterionIds[]`, `repeatable` |
| `Effect` | a state mutation | `op` (`setFlag`/`clearFlag`/`set`/`adjust`/`disclose`/`setEnum`/`setRhythm`), `target`, `value`, `flag`, `factId` |
| `TransitionRule` | time/state-driven change | `when` (expression), `priority`, `once`, `delaySec`, `effects[]`, `presentationCue`, `terminalState?` |
| `ScoringCriterion` | one rubric item | `criticality`, `acceptedActions[]`, `timingWindow?`, `stateConstraints[]`, `points`, `category`, `harmful`, `rationale` |
| `TerminalState` | an ending | `id`, `when`, `outcome` (`complete`/`discharge`/`admit`/`death`/`aborted`) |
| `HiddenFact` | info the patient/exam/orders can reveal | `id`, `disclosure` (`on_ask`/`on_exam`/`on_order_result`), `text` |

## Runtime

| Type | Purpose | Notes |
| --- | --- | --- |
| `PatientState` | live state | vitals, `rhythm`, `airway`/`breathing`/`circulation`/`neuro`, `painScore`, `simTimeSec`, sorted `Flags`/`DisclosedFacts`/`FiredRuleIds`/`ActionCounts` — all sorted for deterministic hashing |
| `Simulation` | the engine | `Initialize()`, `GetAvailableActions()`, `ApplyAction(id, params)`, `AdvanceTime(sec)`, `Snapshot()`, `Score()`, `BuildDebriefFacts()` |
| `SimulationClock` | time | advanced by action `timeCostSec` and by `AdvanceTime`; never wall-clock |
| `DeterministicRng` | seeded randomness | SplitMix64; the only source of randomness |
| `ActionResult` | outcome of one step | `Accepted`, `RejectionReason`, `Event`, `Terminated`, `PresentationCues` |
| `AttemptEvent` / `AttemptTimeline` | the event log | `seq`, `simTimeSec`, `actionId`, `params`, `beforeHash`, `afterHash`, `triggeredRuleIds`, `scoreDelta`, `classification` |
| `SimulationSnapshot` | presentation-safe capture | vitals + enums + flags + `StateHash`; no engine internals, no behaviour |
| `ScoringEngine` / `AttemptScore` | rubric evaluation | per-criterion credit, timing multiplier, harmful penalties, category breakdown, missed list |
| `Replayer` / `AttemptScript` / `ReplayResult` | replay | run an ordered script; `ComputeReplayHash` over `caseId+version+seed+actionIds+finalStateHash` |

## The tick

`ApplyAction`:

1. reject if terminated / unknown / not visible / non-repeatable-and-used / precondition fails (state unchanged)
2. `beforeHash = StateHash(state)`
3. clock += `timeCostSec`; increment action count; apply action `effects`
4. `RunRulePass()` — rules ordered by `priority` desc then `id`; `once` rules skip if fired; `delaySec` arms then applies; cascade up to 16 iterations
5. `ScoreAction()` — credit/deny criteria, timing multiplier, harmful penalty, classify the event
6. `afterHash`; check `terminalStates`; append `AttemptEvent`

`AdvanceTime(sec)` runs steps 2, 4, 6 with no action (pure time passage) — this is
how a timed deterioration rule fires without player input.

## Hashing / determinism

`Hashing.CanonicalStateJson` writes fields in a fixed order with sorted
collections and fixed number formatting; `StateHash` is its SHA-256. Two states
with the same meaning always hash the same. Golden tests assert both `run A == run
B` and `run == committed golden`.
