# ADR-0007: Event-Driven Feedback & Juice Translation Pipeline

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Technical Director
> **Review Note**: TD-ADR skipped (lean)

## Summary
The game uses a decoupled, event-driven presentation architecture in `CozyGachapon.Presentation` centered around `FeedbackJuiceManager`. It translates domain gameplay events (`PegHit`, `JackpotHit`, `IncomeCredited`, `GachaRevealRequested`) into audiovisual juice (DOTween animations, pooled floating text, marimba audio cues, and haptics) with support for an accessibility `ReduceMotion` mode.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | `com.unity.ugui` 2.0.0, DOTween |
| **Domain** | Presentation / UI |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Floating text pooling throughput and Gacha reveal skip orchestration |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0003, ADR-0004 |
| **Enables** | None (Leaf presentation layer) |
| **Blocks** | UI polish, visual effects, and juice implementation |
| **Ordering Note** | Listens to Core and Feature domain events |

## Context

### Problem Statement
Hardcoding animations, audio triggers, and floating text logic directly inside Core gameplay scripts (such as Plinko or Economy) pollutes domain rules with visual presentation concerns, breaks EditMode testing, and creates messy spaghetti code.

### Constraints
- Gameplay systems must remain 100% functional and testable without any visual presentation or audio components present.
- Visual animations and floating text must not create garbage collection spikes during high-frequency coin impacts.
- Must provide accessibility support for players sensitive to screen shaking or flashing lights.

### Requirements
- Single presentation orchestrator (`FeedbackJuiceManager`) subscribing to Core event channels.
- Data-driven event-to-cue mapping via ScriptableObject (`FeedbackProfile`).
- High-throughput floating text spawned from `IPoolService.Get("feedback.floatingText")`.
- Gacha reveal sequence machine supporting normal animation, fast-forward, and immediate skip.
- Global `ReduceMotion` setting that suppresses camera shake and flashing.

## Decision
We implement `FeedbackJuiceManager` in `CozyGachapon.Presentation`:
1. **Unidirectional Event Translation**:
   - `FeedbackJuiceManager` subscribes to events exposed by `IPlinkoBoard`, `IWallet`, `IShelfService`, and `IGachaMachine`.
   - Core and Feature systems never call into Presentation or import Presentation assemblies.
2. **Audio & Visual Dispatch**:
   - On `PegHit(info)`: Maps row/velocity to marimba cue via `IAudioService.PlayCue()` and plays subtle peg squash tween.
   - On `CoinResolved(info)`: Plays bucket reward cue, spawns floating number from pool at bucket position with DOTween punch scale and fade.
   - On `JackpotHit(multiplier)`: Triggers golden honey particle burst, camera micro-shake (if `ReduceMotion` is false), and high-priority fanfare cue.
3. **Gacha Reveal Sequence**:
   - Gacha machine invokes `RevealRequested(pet, rarity)` on `IGachaRevealView`.
   - The view orchestrates 4 distinct phases: Capsule Drop → Wobble & Crack → Rarity Glow Burst → Settle & Card Presentation.
   - Touching the screen during reveal skips ahead to the settle phase immediately.
4. **Accessibility (ReduceMotion)**:
   - When enabled, screenshake intensity is clamped to 0, flashing alphas are reduced to soft color fades, and tween durations are halved.

### Architecture Diagram

```
[ Domain Events ]
├── IPlinkoBoard.PegHit
├── IPlinkoBoard.CoinResolved
└── IGachaMachine.RevealRequested
             │
             │ (C# Action Events)
             ▼
[ FeedbackJuiceManager ]
             │
             ├── 1. Audio Request ──────► [ IAudioService.PlayCue() ]
             ├── 2. Floating Text ──────► [ IPoolService.Get("floatingText") ]
             ├── 3. Animation Tweens ───► [ DOTween Transforms / Canvases ]
             └── 4. Haptic Pulse ───────► [ Device Haptics (Mobile) ]
```

### Key Interfaces
```csharp
namespace CozyGachapon.Contracts
{
    public interface IFeedbackService
    {
        bool ReduceMotion { get; set; }
        void SpawnFloatingText(string text, Vector2 worldPos, Color color);
        void PlayCameraShake(float intensity, float duration);
    }

    public interface IGachaRevealHandler
    {
        void RequestReveal(PetData pet, Action onRevealComplete);
        void SkipReveal();
    }
}
```

### Implementation Guidelines
- Core systems **must never** reference `DOTween`, `TextMeshProUGUI`, or `ParticleSystem`.
- Floating text components **must** implement `IPoolable` to reset tweens and alphas before returning to the pool.
- All tweens **must** use `.SetUpdate(true)` when running during pause states or `.SetAutoKill(true)`.

## Alternatives Considered

### Alternative 1: Direct Component Invocations inside Gameplay Code
- **Pros**: Quick to write in small prototypes.
- **Cons**: Severe coupling, prevents running headless unit tests in EditMode.
- **Rejection Reason**: Violates the project's single-responsibility and clean architecture rules.

### Alternative 2: ScriptableObject Event Architecture
- **Pros**: Decoupled in the Unity Inspector.
- **Cons**: Difficult to trace execution paths in code; prone to broken references during refactoring.
- **Rejection Reason**: Direct C# event subscription via Composition Root provides compile-time safety and instant code navigation.

## Consequences

### Positive
- Gameplay logic remains completely pure, portable, and 100% testable in EditMode.
- Visual juice can be tuned, redesigned, or replaced without risking bugs in economy or physics calculations.
- Comprehensive accessibility support baked in from day one.

### Negative
- Requires maintaining event structs and subscription logic across layer boundaries.

## Risks
- **Tween leaks when objects despawn prematurely**:
  - *Mitigation*: Pooled objects kill all active DOTween tweens via `transform.DOKill()` during `OnDespawn()`.

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| feedback-and-juice.md | TR-feedback-001 | Event-to-feedback translation orchestrator |
| feedback-and-juice.md | TR-feedback-002 | Zero-allocation floating text pooling integration |
| feedback-and-juice.md | TR-feedback-003 | Gacha reveal lifecycle with skip support |
| feedback-and-juice.md | TR-feedback-004 | Global ReduceMotion accessibility flag |

## Performance Implications
- **CPU**: Negligible event dispatch overhead; DOTween handles updates in batched tween loops.
- **Memory**: Floating text recycled via `IPoolService`; zero GC alloc per hit.

## Validation Criteria
- Rapid coin cascades spawn floating text continuously without triggering GC allocation.
- Triggering Gacha reveal skip immediately finishes presentation without hanging the game loop.

## Related
- `Docs/architecture/adr-0001-layered-assembly-architecture.md`
- `Docs/architecture/adr-0003-zero-allocation-object-pooling.md`
- `Docs/architecture/adr-0004-priority-voice-management.md`
