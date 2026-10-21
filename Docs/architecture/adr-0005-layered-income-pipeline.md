# ADR-0005: Layered Income Pipeline & Double Currency Format

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Technical Director
> **Review Note**: TD-ADR skipped (lean)

## Summary
The game uses a pure C# double-precision economic calculation engine in `CozyGachapon.Core` with a multi-currency `Wallet` and a layered `IncomePipeline` (Base → Additive → Multiplicative Tiers with Caps). Dynamic prices scale exponentially with gacha pull count (`100 * 1.05^n`), and big numbers are formatted into compact strings (`K`, `M`, `B`, `T`) without per-frame string allocations.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | None (Pure C# Standard 2.1) |
| **Domain** | Economy / Scripting |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Floating-point precision validation up to 1e30 and non-allocating string builder |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001 |
| **Enables** | ADR-0006, ADR-0007 |
| **Blocks** | Shelf synergy, Shop, and Gacha economy stories |
| **Ordering Note** | Core economic logic substrate |

## Context

### Problem Statement
Incremental games suffer from rapid numerical inflation where integers overflow 32-bit and 64-bit bounds. Furthermore, mixing arbitrary multipliers across different subsystems leads to runaway exponential loops or makes individual buffs imperceptible.

### Constraints
- Must support currencies scaling from 0 to over 1e20 without losing gameplay-critical precision.
- UI formatting for large numbers must not generate garbage strings in `Update()`.
- Gacha costs must scale consistently without allowing exploit loopholes (e.g., stripping shelf pets before purchasing).

### Requirements
- Centralized `Wallet` service managing currency balances in `double` precision.
- Layered `IncomePipeline`: `TotalCPS = (BaseCPS + SumFlat) * (1 + SumPercent) * Prod(TierMultipliers)`.
- Base coin drop value dynamically calculated as `baseCoinValue = (currentGachaCost / baseGachaCost)^alpha` (alpha = 0.5–0.7).
- Efficient string formatter producing standard suffixes (`K`, `M`, `B`, `T`, `q`, `Q`, `s`, `S`).

## Decision
We implement `Wallet` and `IncomePipeline` in `CozyGachapon.Core`:
1. **Double Precision Currency**:
   - `Wallet` tracks currencies using `double` (IEEE 754 float64), natively supporting values up to `~1.79e308` with 15–17 decimal digits of precision, completely avoiding external BigInteger libraries in v1.0.
   - Balances are clamped to zero (`CanAfford` and `TrySpend` return false on insufficient funds).
2. **Layered Income Pipeline**:
   - Systems (e.g. `ShelfService`, temporary boosts) register `IncomeModifier` structs into specific layers:
     - Layer 0: Flat Additive modifiers.
     - Layer 1: Percent Additive modifiers (summed).
     - Layer 2+: Named Multiplicative tiers (e.g., Group Synergy tier, Boost tier, Global tier), each with configured safety caps.
3. **Plinko Scaling Anchor**:
   - `PlinkoSimulation` queries `IBaseCoinValueSource` from `EconomyCore` to determine coin value upon drop, keeping active Plinko earnings meaningful even as gacha costs escalate.
4. **String Formatter**:
   - Pre-allocated `StringBuilder` with cached string arrays for suffixes formats numbers for HUD display with zero allocation.

### Architecture Diagram

```
[ Shelf / Boosts / Events ]
             │
             │ RegisterModifier(layer, value)
             ▼
   [ IncomePipeline ] ─── (Base + Flat) * (1 + Sum%) * Prod(Tiers)
             │
             │ TotalCPS
             ▼
   [ Wallet (Coins) ] ◄── Credit(CoinResolved) ── [ Plinko Board ]
             │
             ▼
[ NumberFormatter ] ──> "1.25 M / s" ──> [ HUD View ]
```

### Key Interfaces
```csharp
namespace CozyGachapon.Contracts
{
    public enum ModifierLayer
    {
        FlatBase = 0,
        AdditivePercent = 1,
        SynergyMultiplier = 2,
        BoostMultiplier = 3
    }

    public readonly struct IncomeModifier
    {
        public readonly string Id;
        public readonly ModifierLayer Layer;
        public readonly double Value;
    }

    public interface IWallet
    {
        double Coins { get; }
        event Action<double> CoinsChanged;
        void Credit(double amount);
        bool TrySpend(double amount);
        bool CanAfford(double amount);
    }

    public interface IIncomePipeline
    {
        double CalculateCurrentCps();
        void RegisterModifier(IncomeModifier modifier);
        void UnregisterModifier(string modifierId);
        event Action ModifiersChanged;
    }

    public interface IBaseCoinValueSource
    {
        double GetCurrentBaseCoinValue();
    }
}
```

### Implementation Guidelines
- Gameplay systems **must never** calculate currency values directly or deduct balances from private fields.
- Multipliers **must** specify an explicit `ModifierLayer` to prevent mathematical order-of-operation exploits.
- Number formatting **must** be cached and refreshed only upon value change or throttled display updates (e.g. 10 Hz).

## Alternatives Considered

### Alternative 1: Arbitrary Precision BigInteger / BigDecimal Library
- **Pros**: Can represent infinite numbers.
- **Cons**: Substantial memory allocations per arithmetic operation; slows down mobile performance.
- **Rejection Reason**: Native `double` reaches 1e308, vastly exceeding the target ceiling for Cozy Gachapon Room v1.0.

### Alternative 2: Flat Ad-hoc Multipliers
- **Pros**: Simple to write.
- **Cons**: Exponential runaway effects that ruin game balance within hours of play.
- **Rejection Reason**: Violates Pillar 3 ("Meaningful Constraints").

## Consequences

### Positive
- Strict mathematical control over inflation and economic balance.
- Pure C# logic with zero Unity dependencies, enabling rapid EditMode test suites.
- Plinko earnings remain relevant across all stages of progression.

### Negative
- Floating-point subtraction can suffer from precision truncation if comparing values of vastly differing magnitudes (e.g. adding 1 to 1e18), mitigated by incremental soft tiers.

## Risks
- **Modifier ID collisions**:
  - *Mitigation*: The pipeline throws an exception if duplicate modifier IDs are registered.

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| economy-core.md | TR-economy-001 | Double-precision wallet with non-negative checks |
| economy-core.md | TR-economy-002 | Layered pipeline calculation engine |
| economy-core.md | TR-economy-003 | Dynamic base coin value scaling formula |
| economy-core.md | TR-economy-004 | Exponential cost scaling curve 100 * 1.05^n |
| economy-core.md | TR-economy-005 | Compact non-allocating string formatter |

## Performance Implications
- **CPU**: Sub-microsecond calculation time for income pipeline evaluation.
- **Memory**: 0 bytes GC per frame when using pre-allocated number formatter.

## Validation Criteria
- EditMode tests verify pipeline calculation across all 4 layers with min/max values.
- Precision checks confirm stability up to 1e100.

## Related
- `Docs/architecture/adr-0001-layered-assembly-architecture.md`
- `Docs/architecture/architecture.md`
