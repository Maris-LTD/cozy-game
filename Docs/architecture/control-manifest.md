# Control Manifest

> **Engine**: Unity 6000.3.16f1 (URP 2D, input: new)
> **Last Updated**: 2026-10-21
> **Manifest Version**: 2026-10-21
> **ADRs Covered**: ADR-0001, ADR-0002, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007, ADR-0008, ADR-0009
> **ADRs Excluded**: None
> **Status**: Active - regenerate with game-manifest update when ADRs change
> **Review Note**: TD-MANIFEST skipped (lean)

Quick reference for programmers, extracted from Accepted ADRs, the harness house rules, studio budgets, project conventions and unity-reference. The referenced ADR explains each rule.

---

## Foundation Layer Rules
*Applies to: bootstrap and composition root, scene flow, save/load, event bus, pooling, config loading*

### Required Patterns
- **Layered asmdef isolation**: Code must reside in five layered asmdefs with strict downward dependency flow (`Presentation` → `Feature` → `Core` → `Foundation` → `Contracts`) - source: ADR-0001
- **Shared contracts boundary**: All public interfaces, event structs, DTOs, and enums must be placed in `CozyGachapon.Contracts` - source: ADR-0001
- **Deterministic composition root**: Initialization must be coordinated solely by `BootstrapController` via sequential 6-phase lifecycle (`CoreInit`, `ServiceInit`, `DataLoad`, `GameplayInit`, `Prewarm`, `Ready`) - source: ADR-0002
- **IBootStep contract**: Any foundational service requiring initialization must implement `IBootStep` - source: ADR-0002
- **Prewarmed object pooling**: Transient visual objects (coins, floating text, fx) must register in `GameplayInit` and prewarm to 100% quota during `Prewarm` via `ObjectPoolService` - source: ADR-0003
- **Lease token safety**: Borrowed pool instances must be checked out and released using `PoolLease` structs with sequence validation - source: ADR-0003
- **Central audio service**: Audio playback must route through `IAudioService.PlayCue()` using `SoundCue` ScriptableObject assets - source: ADR-0004
- **16-voice priority pool**: Audio voices must be managed within a fixed 16-voice pool with priority-based stealing - source: ADR-0004
- **WebGL audio unlock**: Audio context must be initialized muted/suspended and unlocked on the `FirstInteraction` user event - source: ADR-0004

### Forbidden Approaches
- **Never reference higher layers**: Lower layers must never reference higher layers (e.g., `Foundation` must never reference `Core`, `Features`, or `Presentation`) - source: ADR-0001
- **Never place logic in Contracts**: `CozyGachapon.Contracts` must contain zero implementation logic or stateful services - source: ADR-0001
- **Never use static singletons**: Never use `FindObjectOfType`, `GameObject.Find`, or static instance singletons for service location; dependencies must be injected via Composition Root - source: ADR-0002
- **Never call Instantiate/Destroy at runtime**: `GameObject.Instantiate` and `GameObject.Destroy` are strictly prohibited outside the `Prewarm` boot phase - source: ADR-0003
- **Never instantiate AudioSource in prefabs**: Subsystems must never create local `AudioSource` components in prefabs - source: ADR-0004

### Performance Guardrails
- **Startup curtain**: Complete boot and prewarm sequence within the 2.0s first-frame-to-interactive budget - source: ADR-0002
- **Object pooling GC**: 0 B GC per frame during borrow and release operations - source: ADR-0003
- **Audio voice budget**: Exactly 16 pooled `AudioSource` instances, <= 0.5 ms per frame CPU processing - source: ADR-0004

---

## Core Layer Rules
*Applies to: core loop, player systems, physics and collision*

### Required Patterns
- **Double-precision currencies**: Wallet currency balances must be stored and computed using `double` (IEEE 754 float64) values - source: ADR-0005
- **Layered income pipeline**: Income multipliers must be registered into explicit pipeline layers (`FlatBase`, `AdditivePercent`, `SynergyMultiplier`, `BoostMultiplier`) with per-layer caps - source: ADR-0005
- **Dynamic base coin value**: Plinko coin drop values must scale dynamically as `baseCoinValue = (currentGachaCost / baseGachaCost)^alpha` via `IBaseCoinValueSource` - source: ADR-0005
- **Non-allocating number formatting**: Number display formatting (K, M, B, T) must use pre-allocated buffers and cached suffix arrays - source: ADR-0005
- **World Space 2D physics**: Plinko board simulation must run under Unity `Physics2D` (Box2D) in a dedicated World Space camera separated from Canvas UI - source: ADR-0006
- **Concurrent coin ceiling**: Active Plinko coins must never exceed the hard cap of 48 concurrent coins - source: ADR-0006
- **Unity 6 linear velocity**: Physics velocity in Unity 6 must be accessed and set using `Rigidbody2D.linearVelocity` (never deprecated `velocity`) - source: ADR-0006 / unity-reference
- **Stuck coin watchdog**: Coins remaining stuck on pegs longer than 6.0s must be automatically recycled and credited via `CoinStuck` - source: ADR-0006
- **Self-validating data assets**: Balance tuning ScriptableObjects must implement `IValidatableAsset` - source: ADR-0009

### Forbidden Approaches
- **Never store currency in 32-bit int/float**: Game currency must never use `int` or `float` which overflow or lose precision in incremental progression - source: ADR-0005
- **Never bypass Wallet**: Subsystems must never mutate currency balances directly without calling `IWallet.Credit()` or `IWallet.TrySpend()` - source: ADR-0005
- **Never parent physics under Canvas**: Never place moving `Rigidbody2D` or `Collider2D` components under any Canvas hierarchy - source: ADR-0006
- **Never call GetComponent in physics callbacks**: Peg hit and collision callbacks must never invoke `GetComponent` in update/collision loops - source: ADR-0006
- **Never mutate data during validation**: `IValidatableAsset.Validate()` must strictly inspect and never modify serialized asset values - source: ADR-0009

### Performance Guardrails
- **Plinko physics step**: <= 2.0 ms per fixed step (50 Hz) - source: ADR-0006
- **Plinko draw calls**: 2-4 draw calls total for the Plinko board using batched 2D sprite atlas - source: ADR-0006
- **Income pipeline**: < 1 microsecond per CPS calculation - source: ADR-0005

---

## Feature Layer Rules
*Applies to: secondary systems, AI, meta progression*

### Required Patterns
- **Two-tier CDF drop sampling**: Gacha drop pool resolution must use two-tier cumulative distribution function (tier first, then pet) - source: ADR-0008
- **Injectable RNG**: Gacha machine resolution must depend strictly on `IRandomProvider` interface to support deterministic EditMode testing - source: ADR-0008
- **10,000 basis points weight sum**: Gacha rarity tables must sum to exactly 10,000 basis points in config assets - source: ADR-0008, ADR-0009
- **Atomic drop dispatch**: Gacha pull results must be packaged in `GachaDropResult` structs and dispatched atomically to `InventoryService` - source: ADR-0008
- **Editor validation coverage**: Every rarity tier with weight > 0 must contain at least one valid pet in the catalog - source: ADR-0009

### Forbidden Approaches
- **Never use UnityEngine.Random in Gacha**: Never call `UnityEngine.Random` directly in Gacha logic; always use injected `IRandomProvider` - source: ADR-0008
- **Never permit empty rarity tiers**: Never save a drop table with non-zero weight for an empty pet rarity tier - source: ADR-0008, ADR-0009
- **Never mutate inventory directly**: Subsystems must never alter inventory state without triggering formal domain mutation events - source: ADR-0001, ADR-0008

### Performance Guardrails
- **Gacha resolution GC**: 0 B GC per pull, < 1 microsecond execution time - source: ADR-0008

---

## Presentation Layer Rules
*Applies to: uGUI / UI Toolkit, rendering, audio, VFX, shaders, animation*

### Required Patterns
- **Centralized juice translation**: Audiovisual effects (DOTween, floating text, sound cues, haptics) must be orchestrated by `FeedbackJuiceManager` - source: ADR-0007
- **Pooled floating text**: Floating numbers must be borrowed from `IPoolService.Get("feedback.floatingText")` with `RecycleOldest` policy - source: ADR-0007
- **Non-allocating UI text**: HUD text updates must use `TMP_Text.SetText()` or event-based throttled formatting - source: ADR-0007 / unity-reference
- **Interactive reveal skip**: Gacha reveal sequences must support touch input to immediately skip to final presentation - source: ADR-0007
- **ReduceMotion accessibility**: Global `ReduceMotion` flag must be checked before triggering screenshake or rapid screen flashes - source: ADR-0007
- **Dual canvas separation**: UI must maintain separate static and dynamic canvases with `CanvasScaler` set to 1080x1920 reference resolution - source: ADR-0006, ADR-0007

### Forbidden Approaches
- **Never import presentation packages into Core**: Core gameplay and economy code must never reference `DOTween`, `TextMeshProUGUI`, or `ParticleSystem` - source: ADR-0007
- **Never violate ReduceMotion**: Camera shake must never play when `ReduceMotion` is enabled by the player - source: ADR-0007
- **Never concatenate strings in UI update**: String concatenations (`coins + " Xu"`) in `Update()` are strictly forbidden - source: ADR-0005, ADR-0007

### Performance Guardrails
- **Floating text GC**: 0 B GC per spawned number - source: ADR-0007
- **UI draw calls**: <= 15 draw calls with properly atlased sprites - source: ADR-0006, ADR-0007

---

## Global Rules (All Layers)

### House Rules
| Rule | Value | Source |
|---|---|---|
| Max file length | 250 lines | `.unity-harness/config.json rules.maxLines` |
| No new comments | on | `.unity-harness/config.json rules.noComments` |
| No new Debug.Log* | on | `.unity-harness/config.json rules.noDebugLog` |
| CompareTag instead of .tag == | on | `.unity-harness/config.json rules.compareTag` |

### Code Structure & Architecture
- **Single Responsibility Principle (SRP)**: Each class or component must perform exactly one well-defined task - source: AGENTS.md / studio protocol
- **Logic in plain C#**: Core and Feature domain logic must live in pure C# classes testable in EditMode; MonoBehaviours stay thin - source: studio protocol
- **Data-driven via ScriptableObjects**: Tuning values must be stored in ScriptableObject assets, never as literals in code - source: studio protocol
- **Injected dependencies**: Dependencies must be passed via constructors or initialization methods from Composition Root; no global singletons - source: studio protocol / ADR-0002
- **Encapsulation**: Fields must be `private` or `protected` with `[SerializeField]` for Inspector exposure; use properties (`get; private set;`) for external access - source: AGENTS.md
- **Cache components**: Never call `GetComponent` in `Update()` or `FixedUpdate()`; cache all references in `Awake()` or `Start()` - source: AGENTS.md

### Naming Conventions
| Element | Convention | Example | Source |
|---|---|---|---|
| Classes / Interfaces / Structs | PascalCase / `I` prefix for interfaces | `WalletService`, `IBootStep` | AGENTS.md / observed |
| Private serialized fields | camelCase with `[SerializeField]` | `[SerializeField] private int maxCoins;` | AGENTS.md |
| Private non-serialized fields | camelCase or `_` prefix | `private double _currentCoins;` | AGENTS.md |
| Public properties | PascalCase | `public double Coins { get; private set; }` | AGENTS.md |
| Events | PascalCase past tense or Action | `event Action<double> CoinsChanged;` | AGENTS.md |
| Constants / Enums | PascalCase | `ModifierLayer.AdditivePercent` | AGENTS.md |

### Performance Budgets
| Target | Value | Source |
|---|---|---|
| Frame rate | 60 fps (16.6 ms frame) | `studio config targetFps` |
| Draw calls | <= 100 draw calls | `studio config drawCallBudget` |
| Memory ceiling | <= 1024 MB | `studio config memoryBudgetMb` |
| Runtime GC Allocations | 0 B GC per frame during continuous gameplay | ADR-0003, AGENTS.md |

### Approved Packages
- `com.unity.render-pipelines.universal` (17.3.0) - approved for 2D URP rendering pipeline - source: ADR-0006
- `com.unity.ugui` (2.0.0) - approved for HUD and UI navigation - source: ADR-0001, ADR-0007
- `com.unity.inputsystem` (1.19.0) - approved for player pointer input handling - source: ADR-0002
- `com.unity.test-framework` (1.6.0) - approved for EditMode test suites - source: ADR-0001

### Forbidden APIs (Unity 6000.3.16f1)
- `Rigidbody2D.velocity` - deprecated in Unity 6 - use `Rigidbody2D.linearVelocity` - source: unity-reference / ADR-0006
- `Object.FindObjectOfType` / `GameObject.Find` - deprecated architectural pattern - use Composition Root injection - source: ADR-0002
- `UnityEngine.Random` in Gacha logic - unseedable global static - use `IRandomProvider` - source: ADR-0008
- Legacy Input Manager (`Input.GetMouseButton`, `Input.touchCount`) - deprecated - use `com.unity.inputsystem` - source: `studio config inputSystem`
- `Instantiate` / `Destroy` at runtime - causes GC hitches - use `IPoolService` - source: ADR-0003
