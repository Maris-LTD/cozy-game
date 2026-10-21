# ADR-0001: Layered Assembly Architecture & Module Boundaries

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Unity Specialist (validated)
> **Review Note**: TD-ADR skipped (lean)

## Summary
The Unity codebase is organized into five layered Assembly Definition Files (asmdefs) with strict downward dependency flow, anchored by a shared `Contracts` assembly. This structure enforces clean architectural separation, prevents cyclic coupling, and accelerates incremental compilation in Unity 6000.3.16f1.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | `com.unity.ugui` 2.0.0, `com.unity.inputsystem` 1.19.0, `com.unity.test-framework` 1.6.0 |
| **Domain** | Scripting / Core |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md`, `references/breaking-changes.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | EditMode compilation and assembly references resolution |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | None |
| **Enables** | ADR-0002, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007 |
| **Blocks** | All feature implementation stories |
| **Ordering Note** | Must be accepted and implemented before writing domain logic |

## Context

### Problem Statement
Without explicit assembly definition boundaries, Unity compiles all C# code into a single monolithic assembly (`Assembly-CSharp.dll`), allowing arbitrary inter-system calls, circular dependencies (e.g., between Economy Core and Plinko Board events), and slow iteration times due to full recompilation on every edit.

### Constraints
- Must run in Unity 6000.3.16f1 with URP 2D and C# 9.
- Must support fast, standalone EditMode unit testing without instantiating full game scenes.
- Must support multi-platform build targets: WebGL, Android, iOS.
- Must eliminate circular dependency risks flagged in the Systems Design phase.

### Requirements
- Clear unidirectional dependency hierarchy: Presentation → Feature → Core → Foundation → Contracts.
- Pure C# domain logic decoupled from heavy Unity MonoBehaviours.
- Shared contracts (interfaces, DTOs, events) accessible across all layers without pulling in concrete implementations.

## Decision
We create five runtime assembly definitions and one test assembly under `Assets/CozyGachapon/`:

1. **`CozyGachapon.Contracts`**:
   - Contains all public interfaces (`IWallet`, `IIncomePipeline`, `IBootStep`, `IPoolService`, `IAudioService`, `IPlinkoBoard`), event structs, DTOs, and enums.
   - References: Minimal Unity engine modules (`UnityEngine.CoreModule` for `GameObject` reference). Contains zero business logic.
   - Root namespace: `CozyGachapon.Contracts`.

2. **`CozyGachapon.Foundation`**:
   - Contains low-level foundational infrastructure: `BootstrapController` (Composition Root), `ObjectPoolService`, `AudioService`.
   - References: `CozyGachapon.Contracts`, `UnityEngine.AudioModule`, `UnityEngine.CoreModule`.
   - Root namespace: `CozyGachapon.Foundation`.

3. **`CozyGachapon.Core`**:
   - Contains core mathematical, economic, and simulation systems: `Wallet`, `IncomePipeline`, `PetDatabase`, `PlinkoSimulation`.
   - References: `CozyGachapon.Contracts`, `CozyGachapon.Foundation`, `UnityEngine.Physics2DModule`.
   - Root namespace: `CozyGachapon.Core`.

4. **`CozyGachapon.Features`**:
   - Contains secondary and meta gameplay systems: `InventoryService`, `ShelfService`, `GachaMachineService`.
   - References: `CozyGachapon.Contracts`, `CozyGachapon.Core`, `CozyGachapon.Foundation`.
   - Root namespace: `CozyGachapon.Features`.

5. **`CozyGachapon.Presentation`**:
   - Contains UI view controllers, HUD binders, tab navigation, floating text views, and juice animation adapters.
   - References: `CozyGachapon.Contracts`, `CozyGachapon.Features`, `CozyGachapon.Core`, `CozyGachapon.Foundation`, `UnityEngine.UI`, `Unity.InputSystem`.
   - Root namespace: `CozyGachapon.Presentation`.

6. **`CozyGachapon.Tests.EditMode`**:
   - Contains automated NUnit unit tests.
   - References: All runtime assemblies above, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`.
   - Root namespace: `CozyGachapon.Tests.EditMode`.

### Architecture Diagram

```
[ CozyGachapon.Presentation ]
       │              │
       │              ▼
       │      [ CozyGachapon.Features ]
       │              │
       ▼              ▼
[ CozyGachapon.Core ]  │
       │              │
       ▼              ▼
[ CozyGachapon.Foundation ]
       │
       ▼
[ CozyGachapon.Contracts ]  <── Referenced by all layers

[ CozyGachapon.Tests.EditMode ] ──> References all assemblies
```

### Key Interfaces
Defined in `CozyGachapon.Contracts`:
```csharp
namespace CozyGachapon.Contracts
{
    public interface IBootStep
    {
        string StepId { get; }
        BootPhase Phase { get; }
        bool IsCritical { get; }
        bool IsDone { get; }
        bool IsFailed { get; }
        void Run();
    }
}
```

### Implementation Guidelines
- Programmers **must** place all shared interfaces and event types in `CozyGachapon.Contracts`.
- Higher layer modules **must never** be referenced by lower layer modules (e.g., `Core` must never reference `Features` or `Presentation`).
- `Contracts` **must never** contain concrete implementation classes or stateful services.
- Domain rules **must** reside in pure C# classes testable in EditMode; `MonoBehaviour` classes are strictly reserved for Unity view/lifecycle adaptors.

## Alternatives Considered

### Alternative 1: Single Monolithic Runtime Assembly (`CozyGachapon.Runtime`)
- **Description**: Keep everything in one asmdef with logical folders.
- **Pros**: Fewer asmdef files to maintain, simpler initial setup.
- **Cons**: Cannot prevent circular dependencies at compile time; recompilation takes longer; architectural leakage between UI and economy rules is inevitable.
- **Rejection Reason**: Violates the project's strict architecture guidelines and allows architectural drift.

### Alternative 2: One Asmdef Per Individual System (11 asmdefs)
- **Description**: Create separate asmdefs for each of the 11 systems (e.g. `CozyGachapon.Economy`, `CozyGachapon.Plinko`, etc.).
- **Pros**: Maximum isolation.
- **Cons**: High management overhead, complex asmdef graph for a compact 2D incremental game.
- **Rejection Reason**: Excessive fragmentation for a project of this scale; 5-layer grouping offers optimal balance.

## Consequences

### Positive
- Strict compile-time enforcement of architecture layers.
- Sub-second incremental compilation times when iterating on UI and Presentation.
- Fast, clean EditMode testing of all Core and Feature rules.
- Upward event and contract references between Economy Core and Plinko Board are cleanly resolved via `Contracts`.

### Negative
- Developers must maintain asmdef reference lists when adding dependencies.
- Changes to `Contracts` trigger a recompile of all downstream assemblies (mitigated by freezing contracts early).

## Risks
- **Leaking engine dependencies into Contracts**: Adding heavy engine packages to `Contracts` defeats its purpose.
  - *Mitigation*: Restrict `Contracts.asmdef` references strictly to `UnityEngine.CoreModule` only.

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| bootstrap-and-scene-flow.md | TR-boot-001 | Defines Foundation assembly for boot sequence orchestration |
| economy-core.md | TR-economy-001, TR-economy-002 | Decouples economy contracts from visual presentation |
| plinko-board.md | TR-plinko-001, TR-plinko-004 | Isolates 2D physics rules from UI and economy sink |
| All GDDs | TR-* | Provides clean, unidirectional dependency substrate |

## Performance Implications
- **CPU**: Zero runtime CPU overhead; assembly boundaries are resolved at compile time.
- **Memory**: Zero additional memory or GC allocations.
- **Load Time / Compilation**: Significantly faster iterative compile times in the Unity Editor.

## Migration Plan
- Split existing `CozyGachapon.Runtime.asmdef` into the 5 target asmdefs.
- Update `CozyGachapon.Tests.EditMode.asmdef` to reference the new asmdefs.

## Validation Criteria
- Project compiles cleanly with no cyclic reference errors.
- EditMode tests in `CozyGachapon.Tests.EditMode` can instantiate Core and Feature classes directly.
- The dependency graph matches the architecture diagram.

## Related
- `Docs/architecture/architecture.md` (Master Architecture Document)
- `Docs/architecture/tr-registry.yaml` (Technical Requirements Baseline)
