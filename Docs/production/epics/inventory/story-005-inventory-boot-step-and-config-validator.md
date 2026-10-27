# Story 005: Inventory Boot Step, Wiring and Config Validator

> **Epic**: [Inventory](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/EPIC.md)  
> **Status**: Complete (Verified 1080/1080 tests)
> **Type**: Integration
> **Granularity**: Balanced

---

## 1. Mục tiêu
Hiện thực `InventoryBootStep` (`Id = "inventory.core"`, `Phase = BootPhase.GameplayInit`, `RunsAfter = ["economy.core"]`, `IsCritical = true`), `InventoryWiring`, tích hợp vào `BootstrapController`, và `InventoryConfigValidator` kiểm tra hợp lệ `InventoryConfig`.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-17**: `InventoryConfigValidator` phát hiện lỗi khi `startingPets` có ID lạ, `copies < 1`, hoặc ID trùng; phát hiện lỗi khi `copiesDisplayMax` ngoài `[9, 999]` hoặc `maxQueuedEvents` ngoài `[8, 1024]`.
- [x] **AC-18**: `InventoryConfigValidator` phát cảnh báo khi `startingPets` chứa thú `deprecated` hoặc có nhiều hơn 1 mục.
- [x] **AC-23**: `InventoryBootStep` có `Id = "inventory.core"`, `Phase = GameplayInit`, `RunsAfter = ["economy.core"]`, `IsCritical = true`; báo `IsFailed = true` nếu `IPetDatabase` chưa sẵn sàng.
- [x] **AC-24, AC-25**: Trạng thái Inventory độc lập với `Paused`/`Resumed`/`TabChanged`/`ShopEconomyReset`, và khởi tạo lại sạch từ `startingPets` ở mỗi phiên MVP.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `InventoryConfigValidatorTests.cs`: Kiểm tra AC-17, AC-18, AC-21.
  - `InventoryBootStepTests.cs`: Kiểm tra AC-23, AC-24, AC-25.
