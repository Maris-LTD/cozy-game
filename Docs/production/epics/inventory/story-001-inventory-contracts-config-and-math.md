# Story 001: Inventory Contracts, Config and Badge Math

> **Epic**: [Inventory](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/EPIC.md)  
> **Status**: Complete (Verified 1060/1060 tests)
> **Type**: Logic
> **Granularity**: Balanced

---

## 1. Mục tiêu
Định nghĩa các hợp đồng dữ liệu cốt lõi (`AddPetRejectReason`, `AddPetResult`, `PetAcquiredEvent`, `PetCopyEntry`, `IInventoryService`), ScriptableObject `InventoryConfig` với các knob cấu hình và hàm công thức `InventoryMath` (`add_copies`, `copies_badge_text`). Hỗ trợ cờ `IsDeprecated` trong `PetDefinition`.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-8**: Công thức `add_copies` kẹp an toàn ở `int.MaxValue` (2,147,483,647) mà không tràn số âm, và tính `isNew = (copiesBefore == 0)`.
- [x] **AC-21**: Công thức `copies_badge_text` trả về `"x" + copies` khi `copies <= copiesDisplayMax`, và `"x" + copiesDisplayMax + "+"` khi vượt ngưỡng.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `InventoryMathTests.cs`: Kiểm tra `add_copies` (bao gồm biên `int.MaxValue`) và `copies_badge_text`.
