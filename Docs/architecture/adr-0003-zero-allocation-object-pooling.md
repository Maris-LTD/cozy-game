# ADR-0003: Zero-Allocation Object Pooling Service

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Technical Director
> **Review Note**: TD-ADR skipped (lean)

## Summary
The game uses a centralized, prewarmed object pooling infrastructure in `CozyGachapon.Foundation` that enforces zero runtime GC allocation (0 B GC/frame) for all transient visual entities (Plinko coins, floating damage text, particle emitters). Pools support explicit lease tokens and two exhaustion policies: `Fixed` for gameplay-critical entities and `RecycleOldest` for decorative juice.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | `UnityEngine.CoreModule` |
| **Domain** | Core / Memory |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md`, `references/current-best-practices.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | 0 B GC per frame under continuous coin drop and floating text spawn |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0002 |
| **Enables** | ADR-0006, ADR-0007 |
| **Blocks** | Plinko simulation and Feedback & Juice implementation stories |
| **Ordering Note** | Must register pools during GameplayInit and prewarm during Prewarm phase |

## Context

### Problem Statement
In mobile WebGL and low-end mobile devices, frequent `Instantiate()` and `Destroy()` calls trigger severe garbage collection pauses and frame drops, directly violating the target 60 FPS budget and Pillar 1 ("Kinetic ASMR").

### Constraints
- Must guarantee 0 B GC allocation per frame during continuous gameplay.
- Plinko coins must never exceed defined concurrency limits (max 48 coins).
- Decorative numbers (floating text) must prioritize the newest values without crashing if the pool saturates during bursts.

### Requirements
- Central service implementing `IPoolService` exposed via `CozyGachapon.Contracts`.
- Client modules register their pools during `GameplayInit` and specify capacities and exhaustion policies.
- Full prewarm under the startup curtain in `Prewarm` boot phase.
- Lease tracking to prevent double-release or stale release bugs.

## Decision
We implement `ObjectPoolService` in `CozyGachapon.Foundation`:
1. **Registration & Prewarm Lifecycle**:
   - In `GameplayInit`, systems call `RegisterPool(poolId, prefab, capacity, policy)`.
   - In `Prewarm`, `BootstrapController` executes `pooling.prewarm` which instantiates all registered pools to 100% capacity and deactivates them.
   - Once the game is `Ready`, runtime instantiation and destruction are strictly forbidden.
2. **Exhaustion Policies**:
   - `Fixed`: Used for gameplay-critical objects (e.g. `plinko.coin`). If pool is empty, returns an invalid lease and logs an error in the Editor. The caller must drop the action.
   - `RecycleOldest`: Used for visual/feedback effects (e.g. `feedback.floatingText`, `feedback.coinFizzle`). If pool is empty, reclaims the oldest active lease, invokes its reset callback, and assigns it to the new request.
3. **Lease Guards**:
   - `PoolLease` struct encapsulates the pooled instance, pool ID, and an incrementing sequence number. Releasing an already-released or invalidated lease is safely ignored and logged.

### Architecture Diagram

```
[ Client System (e.g. Plinko) ]
          │
          │ 1. Get("plinko.coin")
          ▼
[ ObjectPoolService ] ─── Policy Check ───► [ Free Queue ]
          │                                        │
          │ 2. Return PoolLease                    ▼
          ▼                               [ Active Instances ]
[ Client activates GameObject ]                    │
          │                                        │
          │ 3. Release(lease)                      │
          ▼                                        ▼
[ ObjectPoolService ] ◄──────────────────── Return to Free Queue
```

### Key Interfaces
```csharp
namespace CozyGachapon.Contracts
{
    public enum PoolExhaustionPolicy
    {
        Fixed,
        RecycleOldest
    }

    public readonly struct PoolLease
    {
        public readonly int InstanceId;
        public readonly string PoolId;
        public readonly uint Sequence;
        public readonly GameObject Instance;
        public bool IsValid => Instance != null;
    }

    public interface IPoolService
    {
        void RegisterPool(string poolId, GameObject prefab, int capacity, PoolExhaustionPolicy policy);
        PoolLease Get(string poolId);
        void Release(in PoolLease lease);
    }
}
```

### Implementation Guidelines
- Programmers **must never** call `GameObject.Instantiate` or `GameObject.Destroy` during runtime outside the `Prewarm` boot phase.
- Systems borrowing instances **must** return them via `Release(lease)` when lifecycle ends.
- The pool service **must** keep borrowed instances organized under dedicated container GameObjects in the scene hierarchy.

## Alternatives Considered

### Alternative 1: Unity's Built-in `UnityEngine.Pool.ObjectPool<T>`
- **Pros**: Built-in to Unity 2021+.
- **Cons**: Allocates collection nodes unless carefully wrapped; does not provide out-of-the-box `RecycleOldest` semantics; lacks central sequence-based lease tracking across multiple subsystems.
- **Rejection Reason**: Custom lightweight implementation provides stricter lease guarantees, diagnostic metrics, and deterministic multi-pool prewarm.

### Alternative 2: Subsystem-Specific Ad-hoc Lists
- **Pros**: No shared service.
- **Cons**: Duplicated pooling logic across Plinko, UI, and Audio; impossible to coordinate global prewarm under the startup curtain.
- **Rejection Reason**: High maintenance cost and risk of memory leaks.

## Consequences

### Positive
- Guarantees 0 B GC per frame for transient game objects.
- Protects 60 FPS target on WebGL and mobile.
- Clean separation between gameplay-critical objects (`Fixed`) and visual juice (`RecycleOldest`).

### Negative
- Initial memory footprint is committed upfront during the startup sequence.

## Risks
- **Object state pollution**: Reused objects retaining dirty state from prior uses.
  - *Mitigation*: The service invokes an `IPoolable.OnSpawn()` / `OnDespawn()` contract on the component to enforce state reset.

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| object-pooling.md | TR-pooling-001 | Zero-allocation prewarmed pool infrastructure |
| object-pooling.md | TR-pooling-002 | Fixed and RecycleOldest exhaustion policies |
| object-pooling.md | TR-pooling-003 | Lease token safety with sequence counters |
| object-pooling.md | TR-pooling-004 | Diagnostic active and peak tracking |

## Performance Implications
- **CPU**: O(1) borrow and release operations using internal arrays/stacks.
- **Memory**: Pre-allocated contiguous buffers; 0 bytes GC per frame.

## Validation Criteria
- Unity Profiler shows 0 B GC allocated during 60 seconds of continuous coin dropping.
- Burst spawning beyond capacity triggers `RecycleOldest` for floating text without throwing exceptions.

## Related
- `Docs/architecture/adr-0001-layered-assembly-architecture.md`
- `Docs/architecture/adr-0002-bootstrap-composition-root.md`
