# ADR-0009: ScriptableObject Data Validation Tooling in Editor

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Technical Director
> **Review Note**: TD-ADR skipped (lean)

## Summary
The game uses automated Editor validation tooling in `CozyGachapon.Editor` (implementing `IValidatableAsset` and integrated into pre-build hooks and CI) that scans all ScriptableObject data assets (`PetDefinition`, `GachaDropTableConfig`, `SoundCue`, `PlinkoConfig`). It verifies parameter bounds, non-empty groups, valid ID naming conventions, and references, failing fast in the Editor before bad data enters runtime builds.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | `UnityEditor.CoreModule` |
| **Domain** | Tooling / Data |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Validator menu item (`Tools/CozyGachapon/Validate All Assets`) and pre-build validation check |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001 |
| **Enables** | Clean data authoring for Pet Catalog, Gacha, Audio, and Plinko |
| **Blocks** | Content authoring and release build verification |
| **Ordering Note** | Runs inside CozyGachapon.Editor assembly |

## Context

### Problem Statement
In data-driven incremental games, balance tuning values (multipliers, base CPS, drop weights, cooldowns) are stored in dozens of ScriptableObject assets. Human error during data entry (e.g., negative CPS, duplicate pet IDs, empty groups, total gacha weight != 10000, missing audio clips) leads to subtle runtime bugs, divide-by-zero crashes, and broken economies.

### Constraints
- Editor code must be strictly isolated in `CozyGachapon.Editor` and stripped from standalone/WebGL runtime builds.
- Validation must run fast (< 500 ms for all project assets) to be executed on asset save, in menu items, and as a pre-build requirement.
- Must satisfy `TR-pet-003` from `pet-catalog.md`.

### Requirements
- Self-validating asset contract: `IValidatableAsset` with `ValidationResult Validate()`.
- Editor batch scanner: `AssetValidationRunner` providing a manual menu item and automated pre-build hook via `IPreprocessBuildWithReport`.
- Detailed diagnostic output pinpointing the invalid asset path, property name, and violated rule.

## Decision
We implement `IValidatableAsset` in `CozyGachapon.Contracts` and `AssetValidationRunner` in `CozyGachapon.Editor`:
1. **Asset Validation Contract**:
   - Data assets implement `IValidatableAsset`:
     ```csharp
     public interface IValidatableAsset
     {
         void Validate(ValidationContext context);
     }
     ```
2. **Rule Checks by System**:
   - **Pet Catalog (`PetDefinition`)**:
     - `Id` must start with `pet.` and contain only lowercase alphanumeric characters and periods.
     - `BaseCpsWeight` and `PlinkoBonusMultiplier` must be > 0.
     - `Group` must be assigned to an active recognized pet group (e.g. `Animals`, `Sweets`, `Royal`).
     - `Role` must be valid (`Idle`, `Plinko`, `Support`).
     - Display sprites must not be null.
   - **Gacha Table (`GachaDropTableConfig`)**:
     - Total rarity weight must sum exactly to 10,000 basis points.
     - Every rarity tier with weight > 0 must contain at least one valid pet in the active catalog.
   - **Plinko Config (`PlinkoConfig`)**:
     - `maxActiveCoins` <= 48.
     - `bouncinessFactor` < 0.95.
     - `tapCooldown` > 0.05s.
3. **Automated Integration**:
   - Unity Editor Menu: `Tools > Cozy Gachapon > Validate All Assets (Ctrl+Alt+V)`.
   - `BuildPlayerProcessor`: Implements `IPreprocessBuildWithReport` (callback order 0). If any asset validation error exists, `BuildFailedException` halts the build.

### Architecture Diagram

```
[ Unity Editor / Pre-Build ]
             │
             │ Tools > Validate All Assets OR Build Player Hook
             ▼
[ AssetValidationRunner ] (CozyGachapon.Editor)
             │
             ├── FindAssetsByType<ScriptableObject>()
             ├── Filter for IValidatableAsset
             │
             ▼
[ Asset.Validate(context) ]
 ├── PetDefinition ──► Validates ID, bounds, group, sprite
 ├── GachaTable    ──► Validates 10000 sum, non-empty tiers
 └── PlinkoConfig  ──► Validates max 48 coins, bounciness < 0.95
             │
             ▼
[ Validation Summary ] ─── Errors > 0 ───► Abort Build & Ping Console
```

### Key Interfaces
```csharp
namespace CozyGachapon.Contracts
{
    public interface IValidationContext
    {
        void AddError(string message, UnityEngine.Object contextObj = null);
        void AddWarning(string message, UnityEngine.Object contextObj = null);
        bool HasErrors { get; }
    }

    public interface IValidatableAsset
    {
        void Validate(IValidationContext context);
    }
}
```

### Implementation Guidelines
- Data assets **must** implement `IValidatableAsset` if they contain balance tuning parameters.
- Validation logic **must never** modify asset fields or mark them dirty; validation is strictly read-only inspection.
- Editor validation code **must** reside exclusively within `CozyGachapon.Editor.asmdef`.

## Alternatives Considered

### Alternative 1: Rely on Runtime `Debug.Assert` during Startup
- **Pros**: Easy to write.
- **Cons**: Catches invalid data only when that specific screen or character is loaded during play; breaks released builds if assertions are stripped.
- **Rejection Reason**: Violates the principle of shifting bugs left to design time.

### Alternative 2: Third-party Odin Validator
- **Pros**: Rich GUI attributes.
- **Cons**: Commercial paid asset, adds large external dependency.
- **Rejection Reason**: Simple pure C# `IValidatableAsset` with native Unity pre-build hooks fulfills 100% of project requirements with zero bloat.

## Consequences

### Positive
- Guarantees data integrity before any build is generated.
- Instant feedback for game designers when editing pet stats or drop weights.
- Fully automated in CI/CD pipeline via `IPreprocessBuildWithReport`.

### Negative
- Developers must write a small `Validate()` method when creating new balance ScriptableObjects.

## Risks
- **Overly strict validation blocking prototypes**:
  - *Mitigation*: Severity is separated into Warnings (informative) and Errors (build-blocking).

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| pet-catalog.md | TR-pet-003 | Automated Editor validator for pet parameters, bounds, and groups |
| gacha-machine.md | TR-gacha-002 | Pre-build verification of gacha drop table sum and tier coverage |

## Performance Implications
- **Editor**: < 200 ms to scan hundreds of assets.
- **Runtime**: Zero runtime memory or CPU impact; entirely compiled out of standalone player.

## Validation Criteria
- Menu item `Tools/CozyGachapon/Validate All Assets` runs cleanly and reports verified assets.
- Intentionally creating a pet with negative CPS or empty group flags an Editor error.

## Related
- `Docs/architecture/adr-0001-layered-assembly-architecture.md`
- `Docs/architecture/adr-0008-weighted-random-drop-selection.md`
