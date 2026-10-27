# Story 006: Balance Model Targets and Power Budget Validator

> **Epic**: [Pet Catalog](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/EPIC.md)  
> **Status**: Complete (Verified 1055/1055 tests)
> **Type**: Logic
> **Granularity**: Balanced

---

## 1. Mục tiêu
Hiện thực `PetBalanceTargets` cấu hình budget sức mạnh lý thuyết và `PetBalanceValidator` để xác thực toàn bộ chỉ số của danh sách Pet trong Catalog nằm trong biên độ dung sai cho phép (tolerance +/- 15%), đảm bảo không có pet nào bị over-budget hoặc under-budget theo công thức cân bằng mục 4 GDD.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-20**: Điểm sức mạnh cơ sở: `base_power = 10`.
- [x] **AC-21**: Trọng số vai trò `role_weight`: `GoldBooster = 1.0`, `Multiplier = 1.2`, `Spawner = 1.1`, `SynergyEnabler = 1.3`.
- [x] **AC-22**: Ngân sách sức mạnh mục tiêu `power_target = base_power * rarity_coef * role_weight`.
- [x] **AC-23**: `PetBalanceValidator` kiểm tra power thực tế của từng pet nằm trong khoảng sai số `power_target * (1 +/- 0.15)`.
- [x] **AC-33, AC-34, AC-35, AC-36, AC-37, AC-38**: Đảm bảo toàn bộ 7 MVP pets đạt chuẩn ngân sách và công thức power budget.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `PetBalanceValidatorTests.cs`: Kiểm tra tính toán ngân sách power mục tiêu, xác thực sai số chấp nhận được, phát hiện pet vượt ngưỡng +/- 15%, xác thực 7 MVP pet pass 100%.
