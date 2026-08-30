# Architecture Decision Records

Each ADR captures one significant, hard-to-reverse decision so that agents do not
re-litigate it. Format: **Status · Context · Decision · Alternatives · Consequences**.

| ADR | Title | Status |
| --- | --- | --- |
| [ADR-001](ADR-001-react-native-product-shell.md) | React Native as the product shell | Accepted |
| [ADR-002](ADR-002-unity-3d-simulation-runtime.md) | Unity 6 as the 3D simulation runtime | Accepted |
| [ADR-003](ADR-003-deterministic-engine-owns-clinical-truth.md) | A deterministic C# engine owns clinical truth | Accepted |
| [ADR-004](ADR-004-data-driven-versioned-case-format.md) | Data-driven, versioned case format | Accepted |
| [ADR-005](ADR-005-modular-monolith-backend.md) | Modular monolith backend (NestJS) | Accepted |
| [ADR-006](ADR-006-rn-unity-versioned-bridge-contract.md) | Versioned, typed RN↔Unity bridge contract | Accepted |
| [ADR-007](ADR-007-ai-provider-abstraction-and-safety-boundary.md) | AI provider abstraction + safety boundary | Accepted |

Decisions ADR-008..012 from the blueprint (voice later, golden replay as release
gate, 2 production cases before feature expansion, JSONB case/attempt fields, beta
via TestFlight/Play) are recorded in the blueprint and the backlog and will be
promoted to standalone ADRs as they are implemented.
