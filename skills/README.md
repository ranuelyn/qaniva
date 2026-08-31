# Skills

Reusable, tool-agnostic working instructions — the persistent "how we do X here" so
an agent doesn't relearn it every task. Each `SKILL.md` follows the same shape:

**Purpose · When to use · Inputs · Non-negotiable rules · Workflow · Validation ·
Done criteria · Common failure modes**

| Skill | Covers |
| --- | --- |
| [`qaniva-architecture`](qaniva-architecture/SKILL.md) | boundaries, ADRs, where things go |
| [`react-native-mobile`](react-native-mobile/SKILL.md) | the RN product shell |
| [`unity-mobile`](unity-mobile/SKILL.md) | Unity 6 / URP simulation runtime |
| [`unity-rn-bridge`](unity-rn-bridge/SKILL.md) | the versioned RN↔Unity contract |
| [`deterministic-clinical-engine`](deterministic-clinical-engine/SKILL.md) | the pure C# engine |
| [`case-authoring`](case-authoring/SKILL.md) | evidence-first case lifecycle: research → blueprint → review gate → `case.json` |
| [`case-clinical-review`](case-clinical-review/SKILL.md) | the clinician review gate (section verdicts, evidence verification, versioned approval) |
| [`clinical-safety`](clinical-safety/SKILL.md) | the AI boundary + clinical-truth rules |
| [`testing-and-golden-replay`](testing-and-golden-replay/SKILL.md) | test layers + golden files |
| [`coding-standards`](coding-standards/SKILL.md) | lint/format/TS/C# conventions |
| [`git-and-release`](git-and-release/SKILL.md) | commits, PRs, CI, releases |

Keep skills short and specific. Update the relevant skill in the same PR as a
change that affects it.
