# Story 005: Pet Catalog Editor and Data Integrity Validator

> **Epic**: [Pet Catalog](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/EPIC.md)  
> **Status**: Complete (Verified 1051/1051 tests)
> **Type**: Logic
> **Granularity**: Balanced

---

## 1. Mục tiêu
Xây dựng `PetCatalogValidator` (và tích hợp menu Editor / test validator) để kiểm tra tính toàn vẹn của dữ liệu catalog theo toàn bộ quy tắc nghiệp vụ R1-R12 trong GDD `pet-catalog.md`.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-1**: Định dạng ID bắt buộc `pet.{tên_pet}` (chữ thường, gạch dưới, không khoảng trắng hay ký tự đặc biệt).
- [x] **AC-2**: Không có ID trùng lặp trong toàn bộ catalog.
- [x] **AC-7**: Không có trường enum nào mang giá trị `None = 0` (`rarity`, `group`, `role`, `releaseTier`, `type`, `scope`).
- [x] **AC-8**: Mọi pet phải có ít nhất 1 hiệu ứng (`effects.Length >= 1`).
- [x] **AC-9**: Số lượng hiệu ứng tối đa tuân theo phẩm cấp (R8): Thường = 1, Hiếm = 2, Sử Thi = 2, Huyền Thoại = 3.
- [x] **AC-10**: Hiệu ứng chính đầu tiên (`effects[0]`) phải khớp với `PetRole` (R7):
  - `GoldBooster` -> `FlatGoldPerDrop` hoặc `PercentGoldBoost`
  - `Multiplier` -> `ScoreMultiplier`
  - `Spawner` -> `SpawnRateBoost`
  - `SynergyEnabler` -> `SynergyCountBonus`
- [x] **AC-24**: Kiểm tra phạm vi `PetEffectScope.Both` chỉ được phép xuất hiện ở phẩm cấp Sử Thi (`Epic`) hoặc Huyền Thoại (`Legendary`) (R6).

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `PetCatalogValidatorTests.cs`: Kiểm tra phát hiện lỗi khi vi phạm từng quy tắc AC-1, AC-2, AC-7, AC-8, AC-9, AC-10, AC-24 và xác nhận catalog hợp lệ vượt qua thành công.
