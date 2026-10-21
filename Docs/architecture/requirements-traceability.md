# Requirements Traceability
Last updated: 2026-10-21   Unity: 6000.3.16f1   Mode: full

## Coverage summary
- Total Requirements: 44
- ✅ Covered: 34 (77.3%)
- 🟡 Proposed: 0 (0.0%)
- ⚠️ Partial: 10 (22.7%)
- ❌ Gaps: 0 (0.0%)
- ❓ Not assessed: 0 (0.0%)

## Full Matrix

| Requirement ID | GDD | System | Requirement | ADR Coverage | Status |
|---|---|---|---|---|---|
| TR-economy-001 | economy-core.md | Economy Core | Multi-currency wallet tracking double-precision Coin amounts | ADR-0005, ADR-0001 | ✅ Covered |
| TR-economy-002 | economy-core.md | Economy Core | Layered Income Pipeline with additive base and multiplicative tier caps | ADR-0005, ADR-0001 | ✅ Covered |
| TR-economy-003 | economy-core.md | Economy Core | Dynamic formula family for base coin value scaling with gacha cost | ADR-0005 | ✅ Covered |
| TR-economy-004 | economy-core.md | Economy Core | Exponential gacha pull cost curve 100 * 1.05^n | ADR-0005 | ✅ Covered |
| TR-economy-005 | economy-core.md | Economy Core | Compact string formatting for big numbers (K, M, B, T) backed by double | ADR-0005 | ✅ Covered |
| TR-pet-001 | pet-catalog.md | Pet Catalog | Data-driven ScriptableObject catalog with immutable IDs, groups, roles, rarities | ADR-0001, ADR-0002 | ⚠️ Partial |
| TR-pet-002 | pet-catalog.md | Pet Catalog | Multiplier calculation based on rarity factor and base idle CPS unit | ADR-0005 | ⚠️ Partial |
| TR-pet-003 | pet-catalog.md | Pet Catalog | Editor validation for pet parameter bounds and group assignments | ADR-0009 | ✅ Covered |
| TR-boot-001 | bootstrap-and-scene-flow.md | Bootstrap | Phased startup sequence using IBootStep interface in single-scene | ADR-0002, ADR-0001 | ✅ Covered |
| TR-boot-002 | bootstrap-and-scene-flow.md | Bootstrap | Simulation pause lifecycle on application hidden/background and continue on focus | ADR-0002 | ✅ Covered |
| TR-boot-003 | bootstrap-and-scene-flow.md | Bootstrap | Content view tab router with central input blocking barrier | ADR-0002 | ✅ Covered |
| TR-boot-004 | bootstrap-and-scene-flow.md | Bootstrap | Fixed 9:16 to 9:21 vertical aspect viewport layout with letterbox bounds | ADR-0002, ADR-0006 | ✅ Covered |
| TR-boot-005 | bootstrap-and-scene-flow.md | Bootstrap | Startup curtain coordination covering prewarm operations until ready | ADR-0002, ADR-0003 | ✅ Covered |
| TR-pooling-001 | object-pooling.md | Object Pooling | Zero-allocation object pool service with prewarming for transient visual objects | ADR-0003 | ✅ Covered |
| TR-pooling-002 | object-pooling.md | Object Pooling | Per-pool exhaustion policies supporting Fixed and RecycleOldest behaviours | ADR-0003 | ✅ Covered |
| TR-pooling-003 | object-pooling.md | Object Pooling | Lease-based borrow and release guards preventing double or stale releases | ADR-0003 | ✅ Covered |
| TR-pooling-004 | object-pooling.md | Object Pooling | Diagnostic tracking for active instances, peaks, and leak detection | ADR-0003 | ✅ Covered |
| TR-audio-001 | audio.md | Audio | Central SoundCue repository with hierarchical bus gain routing | ADR-0004 | ✅ Covered |
| TR-audio-002 | audio.md | Audio | Fixed 16-voice pool with priority stealing and pitch randomization | ADR-0004 | ✅ Covered |
| TR-audio-003 | audio.md | Audio | WebGL audio context unlock on first user pointer interaction | ADR-0004 | ✅ Covered |
| TR-audio-004 | audio.md | Audio | Mute reason stack tracking for ad presentation and user preference | ADR-0004 | ✅ Covered |
| TR-plinko-001 | plinko-board.md | Plinko Board | 2D Box2D physics simulation rendered in dedicated World Space camera | ADR-0006, ADR-0001 | ✅ Covered |
| TR-plinko-002 | plinko-board.md | Plinko Board | Spawning rate limiter and hard concurrent active coin cap | ADR-0006 | ✅ Covered |
| TR-plinko-003 | plinko-board.md | Plinko Board | Five bucket multipliers with bounded bounciness factor < 1.0 | ADR-0006 | ✅ Covered |
| TR-plinko-004 | plinko-board.md | Plinko Board | Event publication for PegHit, JackpotHit, CoinResolved, and CoinStuck | ADR-0006, ADR-0001 | ✅ Covered |
| TR-plinko-005 | plinko-board.md | Plinko Board | Despawn watchdog timer recovering stuck coins back to pool | ADR-0006 | ✅ Covered |
| TR-inventory-001 | inventory.md | Inventory | In-memory store tracking owned pet IDs, duplicate counts, and Star rank | ADR-0001, ADR-0002 | ⚠️ Partial |
| TR-inventory-002 | inventory.md | Inventory | Mutation events for pet acquisition and duplicate increments | ADR-0001 | ⚠️ Partial |
| TR-inventory-003 | inventory.md | Inventory | Read-only query interfaces for shelf loadout selection | ADR-0001 | ⚠️ Partial |
| TR-gacha-001 | gacha-machine.md | Gacha Machine | Single-pull gacha machine with dynamic coin cost querying Economy Core | ADR-0005, ADR-0001 | ⚠️ Partial |
| TR-gacha-002 | gacha-machine.md | Gacha Machine | Drop pool resolver with public rarity weights producing starter pets | ADR-0008 | ✅ Covered |
| TR-gacha-003 | gacha-machine.md | Gacha Machine | Atomic integration dispatching new acquisitions to Inventory | ADR-0001 | ⚠️ Partial |
| TR-gacha-004 | gacha-machine.md | Gacha Machine | Reveal orchestration lifecycle with skip and fast-forward handling | ADR-0007 | ✅ Covered |
| TR-shelf-001 | shelf-and-synergy.md | Shelf & Synergy | 3 to 5 shelf slot loadout state reading from Inventory | ADR-0001 | ⚠️ Partial |
| TR-shelf-002 | shelf-and-synergy.md | Shelf & Synergy | Synergy evaluation for group matching and adjacent slot bonuses | ADR-0005 | ⚠️ Partial |
| TR-shelf-003 | shelf-and-synergy.md | Shelf & Synergy | Dual-channel income modifier calculation for Idle CPS and Plinko boosts | ADR-0005 | ✅ Covered |
| TR-shelf-004 | shelf-and-synergy.md | Shelf & Synergy | Dynamic modifier registration and deregistration into Income Pipeline | ADR-0005 | ✅ Covered |
| TR-hud-001 | hud-and-tab-navigation.md | HUD | Top header displaying formatted coin balance, CPS rate, and active boost | ADR-0005, ADR-0002 | ✅ Covered |
| TR-hud-002 | hud-and-tab-navigation.md | HUD | Bottom navigation bar triggering tab transitions through Bootstrap router | ADR-0002 | ✅ Covered |
| TR-hud-003 | hud-and-tab-navigation.md | HUD | uGUI Canvas Scaler configured for 1080x1920 reference resolution | ADR-0002, ADR-0006 | ✅ Covered |
| TR-feedback-001 | feedback-and-juice.md | Feedback & Juice | Event-to-feedback translator coordinating audio cues, floating text, and tweens | ADR-0007 | ✅ Covered |
| TR-feedback-002 | feedback-and-juice.md | Feedback & Juice | Zero-allocation floating text display borrowed from Object Pooling | ADR-0007, ADR-0003 | ✅ Covered |
| TR-feedback-003 | feedback-and-juice.md | Feedback & Juice | Gacha reveal animation sequence with rarity glow and skip integration | ADR-0007 | ✅ Covered |
| TR-feedback-004 | feedback-and-juice.md | Feedback & Juice | Global ReduceMotion flag disabling intense screenshake and flashing | ADR-0007 | ✅ Covered |

## Known Gaps
- None (All 44 technical requirements covered or addressed).

## History
| Date | Coverage % | Full chain % | Notes |
|---|---|---|---|
| 2026-10-21 | 77.3% (34/44) | — | ADR-0008 and ADR-0009 added. 0 Gaps remaining. Verdict: PASS. |
| 2026-10-21 | 72.7% (32/44) | — | Initial baseline architecture review (7 ADRs Accepted). Verdict: CONCERNS. |
