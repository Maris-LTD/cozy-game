# ADR-0002: Bootstrap Composition Root & Phased Lifecycle via IBootStep

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Technical Director
> **Review Note**: TD-ADR skipped (lean)

## Summary
The game uses a single-scene architecture with an explicit pure C# composition root in `BootstrapController` that executes startup in six sequential phases (`CoreInit`, `ServiceInit`, `DataLoad`, `GameplayInit`, `Prewarm`, `Ready`) via `IBootStep`. This eliminates Script Execution Order ambiguity and hidden global singletons.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | `com.unity.ugui` 2.0.0, `com.unity.inputsystem` 1.19.0 |
| **Domain** | Core / Lifecycle |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md`, `references/breaking-changes.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Phased startup sequence execution and pause event dispatching |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001 |
| **Enables** | ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007 |
| **Blocks** | All service registration and boot lifecycle stories |
| **Ordering Note** | Foundational initialization engine |

## Context

### Problem Statement
Standard Unity games often suffer from "initialization order hell" due to scattered `Awake()`, `Start()`, and `RuntimeInitializeOnLoadMethod` calls across multiple MonoBehaviours. Furthermore, relying on static singletons couples classes tightly, making EditMode testing difficult and introducing state leakage across scene resets.

### Constraints
- Must complete initial startup under the "Tiệm mở cửa" curtain within the 2.0s first-frame-to-interactive budget.
- Must cleanly pause gameplay simulation when the app is backgrounded or browser tab hidden without losing pending state.
- Must manage viewport scaling (9:16 to 9:21) and central input routing across 3 non-scrolling tabs.

### Requirements
- Deterministic, inspectable boot pipeline where every service registers its initialization step.
- Services must be testable in isolation without running Unity scenes.
- Central input router capable of blocking player input during transitions, animations, or pause.

## Decision
We implement `BootstrapController` in `CozyGachapon.Foundation` as the sole Composition Root:
1. **Phased Boot Pipeline**:
   - `CoreInit`: Hardware/platform detection, viewport setup (9:16 - 9:21 letterbox).
   - `ServiceInit`: Instantiation of low-level services (`ObjectPoolService`, `AudioService`).
   - `DataLoad`: Catalog loading from ScriptableObjects (`PetDatabase`).
   - `GameplayInit`: Domain logic wiring (`Wallet`, `IncomePipeline`, `PlinkoSimulation`, `InventoryService`, `ShelfService`, `GachaMachineService`). Pool registration.
   - `Prewarm`: Object pooling prewarming up to quota under the startup curtain.
   - `Ready`: Fade out curtain, enable input router, start main game loop.
2. **Lifecycle & Pause Management**:
   - `OnApplicationPause` and `OnApplicationFocus` map to `Paused(reason)` and `Resumed(reason, awaySeconds)` events.
   - When tab hidden / app backgrounded: simulation stops. When focus lost only (e.g. clicking outside on desktop web): simulation continues.
3. **Tab & Input Router**:
   - Manages active tab state (`ActiveTab`, `TabChanged` event) among 3 tabs: Tab 1 (Plinko & Gacha), Tab 2 (Kệ & Kho), Tab 3 (Tiệm Tạp Hóa).
   - Central `InputRouter` exposes `IsInputBlocked` flag to freeze gameplay inputs during modal dialogs and transitions.

### Architecture Diagram

```
[ Unity Scene Start ]
         │
         ▼
[ BootstrapController ] ──> (Composition Root)
         │
         ├── Phase 1: CoreInit
         ├── Phase 2: ServiceInit  ──> [ AudioService, ObjectPoolService ]
         ├── Phase 3: DataLoad     ──> [ PetCatalog ]
         ├── Phase 4: GameplayInit ──> [ Wallet, Income, Plinko, Inventory, Shelf, Gacha ]
         ├── Phase 5: Prewarm      ──> [ ObjectPoolService.PrewarmAll() ]
         └── Phase 6: Ready        ──> [ Curtain FadeOut, Enable InputRouter ]
```

### Key Interfaces
```csharp
namespace CozyGachapon.Contracts
{
    public enum BootPhase
    {
        CoreInit,
        ServiceInit,
        DataLoad,
        GameplayInit,
        Prewarm,
        Ready
    }

    public interface IBootStep
    {
        string StepId { get; }
        BootPhase Phase { get; }
        bool IsCritical { get; }
        bool IsDone { get; }
        bool IsFailed { get; }
        void Run();
    }

    public interface IInputRouter
    {
        bool IsInputBlocked { get; set; }
        int ActiveTab { get; }
        void SwitchTab(int tabIndex);
        event Action<int, int> TabChanged;
    }
}
```

### Implementation Guidelines
- Services **must** implement `IBootStep` if they need lifecycle initialization.
- Classes **must never** use `FindObjectOfType`, `GameObject.Find`, or static instance singletons for service location; dependencies **must** be injected during `GameplayInit`.
- Critical boot steps (`IsCritical = true`) that fail **must** display the retry prompt rather than crashing silently.

## Alternatives Considered

### Alternative 1: VContainer / Zenject DI Container Package
- **Pros**: Automated constructor injection, lifetime scoping.
- **Cons**: Adds external package dependency, extra boilerplate and mental model complexity for a compact game.
- **Rejection Reason**: The explicit `IBootStep` pattern in pure C# provides full deterministic control with zero third-party dependencies.

### Alternative 2: Script Execution Order Configuration
- **Pros**: Native Unity feature.
- **Cons**: Fragile, difficult to track in git, does not solve dependency injection or EditMode testability.
- **Rejection Reason**: Known anti-pattern for maintainable game architectures.

## Consequences

### Positive
- Completely deterministic startup order visible in code.
- EditMode tests can construct any domain service directly without bootstrapping a scene.
- Clear hooks for prewarming pools and loading data under the startup curtain.

### Negative
- Manual wiring in `BootstrapController` requires explicit code updates when adding new global services.

## Risks
- **Long prewarm stalling main thread**: Large pool prewarming might hitch the frame.
  - *Mitigation*: `prewarmPerFrame` throttling allows distributing instantiation over several frames under the curtain.

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| bootstrap-and-scene-flow.md | TR-boot-001 | Six-phase IBootStep startup pipeline |
| bootstrap-and-scene-flow.md | TR-boot-002 | OnApplicationPause/Focus mapping to simulation lifecycle |
| bootstrap-and-scene-flow.md | TR-boot-003 | Input router and 3-tab navigation state |
| bootstrap-and-scene-flow.md | TR-boot-004 | Fixed vertical aspect ratio bounds |
| bootstrap-and-scene-flow.md | TR-boot-005 | Curtain coordination during Prewarm |

## Performance Implications
- **CPU**: Clean startup with zero per-frame composition overhead.
- **Memory**: Linear allocation during initial load, zero garbage generated after `Ready`.

## Validation Criteria
- Phased startup runs from Phase 1 to Phase 6 in deterministic order.
- Switching tabs fires `TabChanged` and updates `ActiveTab`.
- Setting `IsInputBlocked = true` prevents game inputs from propagating.

## Related
- `Docs/architecture/adr-0001-layered-assembly-architecture.md`
- `Docs/architecture/architecture.md`
