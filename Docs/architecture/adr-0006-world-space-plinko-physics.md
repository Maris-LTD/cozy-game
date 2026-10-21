# ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Technical Director
> **Review Note**: TD-ADR skipped (lean)

## Summary
The Plinko board is simulated via Unity's native 2D Box2D physics in a dedicated World Space orthographic camera, completely separated from the uGUI Screen Space Canvas. This prevents Canvas vertex dirtying/rebuilds on every physics step, preserves stable physics collisions across aspect ratios, and guarantees <=100 draw calls and 60 FPS on mobile and WebGL.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | `UnityEngine.Physics2DModule`, `com.unity.render-pipelines.universal` 17.3.0 |
| **Domain** | Physics / Rendering |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md`, `references/breaking-changes.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | 60 FPS and < 100 draw calls during 48-coin cascade |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0003 |
| **Enables** | ADR-0007 |
| **Blocks** | Plinko board gameplay implementation |
| **Ordering Note** | Requires Object Pooling for coin instances |

## Context

### Problem Statement
Placing moving physics rigidbodies (coins) inside a Unity uGUI Canvas forces Unity to recalculate the entire Canvas vertex mesh every frame for every moving element, causing severe CPU spikes on mobile WebGL and dropping framerates below 30 FPS.

### Constraints
- Must maintain a stable 60 FPS on mobile browsers (WebGL) and low-end mobile devices.
- Total draw calls must remain <= 100 across the entire screen.
- Physics behavior must remain deterministic and unaffected by varying display resolutions and aspect ratios (9:16 to 9:21).

### Requirements
- Plinko coins and pegs must run under Unity `Physics2D` (Box2D) in World Space.
- The UI (HUD header, bottom navigation bar) resides in an overlay Canvas (`Screen Space - Overlay` or `Camera`).
- Bounciness and velocity must be bounded to prevent infinite energy accumulation.
- Stuck coins must be automatically recycled after a timeout.

## Decision
We decouple Plinko simulation into a dedicated World Space domain in `CozyGachapon.Core`:
1. **Camera & Coordinate System**:
   - `WorldCamera`: Orthographic camera focused on the Plinko board (bounds: X [-2.5, 2.5], Y [-4.5, 4.5]).
   - `UICanvas`: Screen Space - Overlay Canvas with `CanvasScaler` set to 1080x1920 (Match Height).
   - The Plinko viewport is letterboxed/pillarboxed within the safe area between the Top Header and Bottom Nav.
2. **Physics Configuration & Rate Limiting**:
   - Maximum active concurrent coins: 48 (enforced by `plinko.coin` pool capacity).
   - Coin drop rate limited by tap cooldown (e.g. 0.15s) and auto-drop intervals.
   - Bounciness is capped (< 0.95) using a shared 2D Physics Material to ensure coins always settle into bottom buckets.
3. **Watchdog Despawn Timer**:
   - Each active coin tracks time-since-spawn. If a coin remains stuck on pegs longer than `coinDespawnTimeoutSeconds` (e.g. 6.0s), the watchdog fires `CoinStuck`, credits base value, and releases the coin back to the pool.
4. **Event Dispatching**:
   - The simulation exposes pure C# events (`PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`) consumed by `FeedbackJuiceManager` in Presentation.

### Architecture Diagram

```
[ Screen Space Overlay Canvas ]
├── Top Header (Coins / CPS / Boost)  <── Canvas Layer (UI)
└── Bottom Nav (Tabs 1, 2, 3)

                 ▲
                 │ (Visual Layering)
                 ▼

[ World Space Camera ] (Plinko Board)
├── Spawn Zone (Drop Limiter)
├── Peg Grid (CircleCollider2D - Static)
├── Moving Coins (Rigidbody2D + CircleCollider2D - Pooled)
└── 5 Multiplier Buckets (BoxCollider2D - Triggers)
```

### Key Interfaces
```csharp
namespace CozyGachapon.Contracts
{
    public readonly struct PegHitInfo
    {
        public readonly int RowIndex;
        public readonly Vector2 ContactPoint;
        public readonly bool IsScored;
    }

    public readonly struct CoinResolvedInfo
    {
        public readonly int BucketIndex;
        public readonly double Multiplier;
        public readonly double PayoutAmount;
    }

    public interface IPlinkoBoard
    {
        bool CanDropCoin { get; }
        bool TryDropCoin(float normalizedDropX);
        event Action<PegHitInfo> PegHit;
        event Action<CoinResolvedInfo> CoinResolved;
        event Action<int> JackpotHit;
        event Action CoinStuck;
    }
}
```

### Implementation Guidelines
- Programmers **must never** parent moving `Rigidbody2D` or `Collider2D` components under a Canvas hierarchy.
- Coins **must** obtain their physics instance via `IPoolService.Get("plinko.coin")`.
- Contact detection with pegs **must** avoid `GetComponent` in callbacks, using cached layer masks and pre-allocated contact buffers.

## Alternatives Considered

### Alternative 1: UI-based Physics (Physics inside Canvas with Graphic components)
- **Pros**: Matches RectTransform coordinates easily.
- **Cons**: Causes continuous Canvas Rebuilds, massive GC spikes, and inconsistent physics scaling across resolutions.
- **Rejection Reason**: Completely unviable for mobile WebGL performance budgets.

### Alternative 2: Custom Fake Physics Simulation (Pure math kinematic lerp)
- **Pros**: Completely deterministic.
- **Cons**: Lacks organic "Kinetic ASMR" feel and unexpected chaotic bounces that make Plinko satisfying.
- **Rejection Reason**: Violates Pillar 1 ("Kinetic ASMR").

## Consequences

### Positive
- Zero canvas rebuild overhead from moving physics objects.
- Smooth 60 FPS physics on mobile browsers.
- Draw calls stay well below the <= 100 budget (coins and pegs batched using a single 2D sprite atlas).

### Negative
- Screen-to-world coordinate conversion required when translating touch inputs from the UI overlay to the drop zone.

## Risks
- **Screen aspect ratio variations**:
  - *Mitigation*: The camera orthographic size is pinned, and borders are decorated with wood framing to accommodate aspect ratios from 9:16 to 9:21.

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| plinko-board.md | TR-plinko-001 | World Space 2D physics separation |
| plinko-board.md | TR-plinko-002 | Coin drop rate limiter and active concurrency cap |
| plinko-board.md | TR-plinko-003 | Five bucket multiplier triggers with safe bounciness |
| plinko-board.md | TR-plinko-004 | Event channel publication for collisions and payouts |
| plinko-board.md | TR-plinko-005 | Watchdog timer recovering stuck coins |

## Performance Implications
- **CPU**: Fixed physics step execution (50 Hz / 20 ms), <= 2 ms physics simulation time.
- **Draw calls**: 2-4 draw calls for the entire Plinko board with URP 2D sprite batching.

## Validation Criteria
- Dropping 48 coins simultaneously produces 0 Canvas rebuilds and maintains 60 FPS.
- Stuck coins reliably despawn within 6.0 seconds.

## Related
- `Docs/architecture/adr-0001-layered-assembly-architecture.md`
- `Docs/architecture/adr-0003-zero-allocation-object-pooling.md`
