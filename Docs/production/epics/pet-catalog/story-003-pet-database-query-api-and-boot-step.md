# Story 003: Pet Database Query API and Catalog Boot Step

> **Epic**: [Pet Catalog](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/EPIC.md)  
> **Status**: Complete (Verified 1039/1039 tests)
> **Type**: Integration
> **Granularity**: Balanced

---

## 1. Mục tiêu
Hiện thực `IPetDatabase` / `PetDatabase` cung cấp API tra cứu O(1) theo ID, lọc theo `rarity`, `group`, `role`, `releaseTier` không sinh rác (zero-allocation khi query hoặc dùng cache arrays/lists). Hiện thực `PetCatalogBootStep` đăng ký vào Bootstrap framework tại `BootPhase.DataLoad` với `IsCritical = true`.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-18**: `PetDatabase.Get(string id)` trả về `PetDefinition` với độ phức tạp O(1). Nếu không tìm thấy, throw `KeyNotFoundException` hoặc trả false tuỳ phương thức `TryGet`.
- [x] **AC-19**: `PetDatabase.TryGet(string id, out PetDefinition pet)` trả về true/false an toàn.
- [x] **AC-25**: Các hàm truy vấn danh sách: `GetByGroup(PetGroup group)`, `GetByRarity(PetRarity rarity)`, `GetByRole(PetRole role)`, `GetByTier(PetReleaseTier tier)` trả về tập hợp đã được index trước, không cấp phát heap trong quá trình truy vấn lặp lại.
- [x] **AC-26**: `PetDatabase` nạp dữ liệu từ `PetCatalogConfig` hoặc danh sách `PetDefinition`.
- [x] **AC-27**: `PetCatalogBootStep` kế thừa `IBootStep`, chạy ở phase `BootPhase.DataLoad`, `Order = 20`, `IsCritical = true`.
- [x] **AC-28**: Khởi tạo database hoàn tất và đăng ký vào Service locator / container trong `PetWiring`.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `PetDatabaseTests.cs`: Kiểm tra Get O(1), TryGet, filtering theo group/rarity/role/tier, kiểm tra zero-alloc queries.
  - `PetBootStepTests.cs`: Kiểm tra thực thi boot step thành công, gán database hợp lệ.
