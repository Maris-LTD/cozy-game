# Story 001: Pet Contracts, Enums, Rarity Config and Power Formulas

> **Epic**: [Pet Catalog](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/EPIC.md)  
> **Status**: Complete (Verified 1023/1023 tests)
> **Type**: Logic
> **Granularity**: Balanced

---

## 1. Mục tiêu
Định nghĩa hệ thống enum cốt lõi (`PetRarity`, `PetGroup`, `PetRole`, `PetReleaseTier`, `PetEffectType`, `PetEffectScope`), struct `PetEffect`, ScriptableObject `PetRarityConfig` và logic tính điểm sức mạnh / hệ số phẩm cấp (`PetPowerFormula`) theo đúng GDD `pet-catalog.md`.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-4**: Enum `PetRarity` gồm `None = 0`, `Common = 1` (Thường), `Rare = 2` (Hiếm), `Epic = 3` (Sử Thi), `Legendary = 4` (Huyền Thoại).
- [x] **AC-5**: `PetRarityConfig` ScriptableObject lưu hệ số phẩm cấp với `Common = 1.0` (chuẩn hoá cố định), và 3 knob: `Rare = 2.5`, `Epic = 6.0`, `Legendary = 15.0`.
- [x] **AC-30**: Enum `PetGroup` (`None = 0`, `Woodland = 1`, `Aquatic = 2`, `Savanna = 3`, `Sweets = 4`, `Sky = 5`, `Mythic = 6`), `PetRole` (`None = 0`, `GoldBooster = 1`, `Multiplier = 2`, `Spawner = 3`, `SynergyEnabler = 4`), `PetReleaseTier` (`None = 0`, `Mvp = 1`, `PostMvp = 2`).
- [x] **AC-31**: Enum `PetEffectType` (`None = 0`, `FlatGoldPerDrop = 1`, `PercentGoldBoost = 2`, `ScoreMultiplier = 3`, `SpawnRateBoost = 4`, `SynergyCountBonus = 5`), `PetEffectScope` (`None = 0`, `Self = 1`, `Group = 2`, `Board = 3`, `Both = 4`).
- [x] **AC-32**: `PetPowerFormula` tính toán:
  - `power_score = base_power * rarity_coef[rarity] * role_weight[role]`
  - `base_power = 10` mặc định.
  - Trả về kết quả chính xác theo công thức mục 4 GDD.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong assembly `CozyGachapon.Tests.EditMode`:
  - `PetRarityConfigTests.cs`: Kiểm tra giá trị enum, hệ số phẩm cấp mặc định, bất biến `Common = 1.0`.
  - `PetPowerFormulaTests.cs`: Kiểm tra tính toán power score cho từng role/rarity.
