# ADR-0008: Weighted Random Drop Selection Algorithm & RNG Architecture

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Game Designer
> **Review Note**: TD-ADR skipped (lean)

## Summary
The Gacha drop pool resolution system in `CozyGachapon.Features` uses a two-tier cumulative distribution function (CDF) weighted sampling algorithm backed by an injectable, seedable random provider interface (`IRandomProvider`). This guarantees mathematically exact public rarity probabilities (Common, Rare, Epic, Legendary) without runtime memory allocation and allows deterministic EditMode test validation.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | None (Pure C# Standard 2.1) |
| **Domain** | Gameplay / Math |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Statistical distribution test (100,000 pulls within 0.5% tolerance in EditMode) |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0005 |
| **Enables** | Gacha machine execution and duplicate resolution |
| **Blocks** | Gacha drop rate balance and reward stories |
| **Ordering Note** | Consumed by GachaMachineService and PetCatalog |

## Context

### Problem Statement
Gacha systems require strict adherence to advertised drop probabilities (Pillar 2: "Gacha Thrill with controlled dopamine"). Hardcoding `UnityEngine.Random.Range` inside monolithic scripts prevents unit testing with fixed seeds, risks off-by-one errors in cumulative weight arrays, and produces non-auditable outcomes.

### Constraints
- Drop rates must be 100% public and auditable: e.g., Common 60%, Rare 30%, Epic 9%, Legendary 1%.
- Must run with 0 B GC allocation per pull.
- The RNG engine must be mockable/seedable in EditMode tests to verify specific drops and duplicate-to-Star progression deterministically.

### Requirements
- Two-tier resolution algorithm:
  1. Primary roll: Select rarity tier from cumulative weight table.
  2. Secondary roll: Select specific pet uniformly within that rarity tier from the active catalog.
- Pure C# RNG interface: `IRandomProvider` with `SystemRandomProvider` (using `System.Random` with timestamp seed) and `DeterministicRandomProvider` (for test fixtures).

## Decision
We implement `GachaDropResolver` in `CozyGachapon.Features` and `IRandomProvider` in `CozyGachapon.Contracts`:
1. **Two-Tier Cumulative Distribution Function (CDF)**:
   - Config asset `GachaDropTableConfig` contains integer or float weights per rarity tier: `[Common: 6000, Rare: 3000, Epic: 900, Legendary: 100]` (Total: 10,000 basis points).
   - `GachaDropResolver` generates a random number $r \in [0, \text{TotalWeight})$ and uses a binary search or linear sweep over the pre-calculated prefix sum array to select the rarity tier.
   - A second random roll selects uniformly among the available pets in that rarity tier (`petsInRarity[randomInt % count]`).
2. **Injectable Random Provider**:
   - `IGachaMachine` receives `IRandomProvider` via its constructor. Runtime uses `SystemRandomProvider`; EditMode tests inject `DeterministicRandomProvider(predefinedRolls)`.
3. **Atomic Result Struct**:
   - Drop result is returned as `GachaDropResult` (containing `PetDefinition`, `Rarity`, `IsDuplicate`, `StarGain`) without heap allocations.

### Architecture Diagram

```
[ GachaMachineService.Pull() ]
               │
               ▼
     [ GachaDropResolver ]
               │
               ├── 1. IRandomProvider.NextDouble()
               ├── 2. Cumulative Weight Binary Search ──► Tier: Epic (9%)
               ├── 3. Secondary Roll ────────────────────► Pet: Cáo Khăn Len
               ▼
[ GachaDropResult ] ───► [ InventoryService.RegisterAcquisition() ]
```

### Key Interfaces
```csharp
namespace CozyGachapon.Contracts
{
    public interface IRandomProvider
    {
        double NextDouble(); // [0.0, 1.0)
        int NextInt(int minInclusive, int maxExclusive);
    }

    public readonly struct GachaDropResult
    {
        public readonly string PetId;
        public readonly PetRarity Rarity;
        public readonly bool IsDuplicate;
        public readonly int StarsGained;

        public GachaDropResult(string petId, PetRarity rarity, bool isDuplicate, int starsGained)
        {
            PetId = petId;
            Rarity = rarity;
            IsDuplicate = isDuplicate;
            StarsGained = starsGained;
        }
    }

    public interface IGachaDropResolver
    {
        GachaDropResult ResolveDrop(IRandomProvider rng);
    }
}
```

### Implementation Guidelines
- Programmers **must never** call `UnityEngine.Random` directly in Gacha logic; always use `IRandomProvider`.
- Rarity weights **must** be stored as integers (basis points out of 10,000) in ScriptableObject assets to eliminate floating-point rounding inaccuracies.
- Drop pool tables **must** be pre-sorted and validated on load to ensure non-zero weights and at least one pet per represented tier.

## Alternatives Considered

### Alternative 1: Single Flat Weight Table Across All Pets
- **Pros**: Only one random roll needed.
- **Cons**: Adding a new pet alters the probabilities of all other pets unless all weights are manually recalculated; violates the public rarity guarantee (e.g. Legendary stays 1% regardless of whether there are 1 or 5 Legendary pets).
- **Rejection Reason**: Two-tier tier-first sampling preserves strict rarity guarantees.

### Alternative 2: Unity `UnityEngine.Random.Range`
- **Pros**: Built-in global static function.
- **Cons**: Cannot be seeded independently per service, cannot be mocked in EditMode tests, prone to global state contamination.
- **Rejection Reason**: Prevents automated unit testing.

## Consequences

### Positive
- Strict mathematical adherence to published drop rates.
- 100% deterministic testing capability for drops, duplicates, and star progression.
- Zero runtime GC allocations during pull resolution.

### Negative
- Requires maintaining `IRandomProvider` interface abstraction.

## Risks
- **Empty rarity tier in catalog**: If a catalog has no pets in a rolled tier, resolution fails.
  - *Mitigation*: Editor validator (ADR-0009) verifies that every rarity with non-zero weight has at least one active pet.

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| gacha-machine.md | TR-gacha-002 | Two-tier weighted random drop pool resolution algorithm |
| gacha-machine.md | TR-gacha-003 | Atomic drop result packaging for inventory dispatch |

## Performance Implications
- **CPU**: < 1 microsecond per pull (prefix sum binary search + integer modulo).
- **Memory**: 0 B GC per pull.

## Validation Criteria
- Monte Carlo test in EditMode running 100,000 simulated pulls confirms Common (60% ± 0.5%), Rare (30% ± 0.5%), Epic (9% ± 0.3%), Legendary (1% ± 0.1%).
- Deterministic test verifies exact pet returned when mocked RNG returns predefined values.

## Related
- `Docs/architecture/adr-0001-layered-assembly-architecture.md`
- `Docs/architecture/adr-0005-layered-income-pipeline.md`
- `Docs/architecture/adr-0009-scriptableobject-editor-validation.md`
