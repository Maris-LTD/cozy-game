# Story 004: Orphan Retention, Starting Pets and Save Snapshot

> **Epic**: [Inventory](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/EPIC.md)  
> **Status**: Complete (Verified 1080/1080 tests)
> **Type**: Logic
> **Granularity**: Balanced

---

## 1. Mục tiêu
Hiện thực xử lý mục orphan (ID không có trong Catalog lúc nạp được giữ ẩn nhưng bảo toàn khi xuất dữ liệu save), áp dụng `startingPets` khi bắt đầu game mới (không có dữ liệu save), và chuẩn hoá dữ liệu nạp/xuất.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-12**: Nạp dữ liệu chứa ID orphan (`pet.d`) thì `IsOwned("pet.d") = false`, `GetCopies("pet.d") = 0`, không tính vào `OwnedSpeciesCount` hay `TotalCopies`, không phát sự kiện.
- [x] **AC-13**: Xuất dữ liệu save chứa cả loài hiển thị lẫn orphan sắp theo `id` ordinal; khi nạp lại với Catalog đã bổ sung `pet.d` thì `pet.d` hiển thị lại với số bản cũ.
- [x] **AC-14**: Gọi `AddPet` với ID orphan trả `Rejected(UnknownId)` và không làm hỏng dữ liệu orphan đã giữ.
- [x] **AC-15**: Khi nạp dữ liệu có mục trùng ID thì gộp cộng (kẹp `INT_MAX`); mục có `copies <= 0` hoặc `id` rỗng/`null` bị loại bỏ an toàn.
- [x] **AC-16**: Khi không có dữ liệu save (`savedEntries == null`), áp dụng `startingPets` im lặng (0 sự kiện); khi có dữ liệu save (kể cả mảng rỗng), bỏ qua hoàn toàn `startingPets`.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `InventoryOrphanAndSaveTests.cs`: Kiểm tra AC-12, AC-13, AC-14, AC-15, AC-16.
