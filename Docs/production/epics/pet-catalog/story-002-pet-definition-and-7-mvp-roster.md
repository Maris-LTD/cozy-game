# Story 002: PetDefinition ScriptableObject and 7 MVP Roster Assets

> **Epic**: [Pet Catalog](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/EPIC.md)  
> **Status**: Complete (Verified 1028/1028 tests)
> **Type**: Config/Data
> **Granularity**: Balanced

---

## 1. Mục tiêu
Hiện thực ScriptableObject `PetDefinition` và `PetCatalogConfig`, đồng thời tạo đầy đủ 7 asset ScriptableObject cho 7 Pet MVP theo đúng bảng Roster và tiêu chuẩn của GDD `pet-catalog.md`.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-3**: `PetDefinition` chứa đầy đủ: `id`, `displayNameKey`, `rarity`, `group`, `role`, `releaseTier`, danh sách `effects` (`PetEffect[]`), `iconAddress`, `prefabAddress`.
- [x] **AC-6**: Tạo đủ 7 asset pet MVP trong `Assets/CozyGachapon/Data/Pets/`:
  1. `pet.woody_duck` (Woodland, Common, GoldBooster)
  2. `pet.bouncy_bunny` (Woodland, Common, Multiplier)
  3. `pet.cozy_fox` (Woodland, Rare, SynergyEnabler)
  4. `pet.rainbow_candy` (Sweets, Common, GoldBooster)
  5. `pet.dreamy_frog` (Aquatic, Rare, Spawner)
  6. `pet.lucky_cat` (Savanna, Epic, Multiplier)
  7. `pet.chubby_king_cat` (Savanna, Legendary, SynergyEnabler)
- [x] **AC-16**: Phân bố phẩm cấp MVP: 3 Thường, 2 Hiếm, 1 Sử Thi, 1 Huyền Thoại.
- [x] **AC-17**: `PetCatalogConfig` ScriptableObject gom danh sách tất cả `PetDefinition` đăng ký trong catalog.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `PetDefinitionTests.cs`: Kiểm tra tính toàn vẹn của class `PetDefinition`.
  - `MvpRosterTests.cs`: Load hoặc kiểm tra 7 asset MVP có đầy đủ ID, không null, enum hợp lệ (> 0).
