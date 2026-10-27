# Story 003: Inventory Queries, Display Sorting and Collection Progress

> **Epic**: [Inventory](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/EPIC.md)  
> **Status**: Complete (Verified 1080/1080 tests)
> **Type**: Logic
> **Granularity**: Balanced

---

## 1. Mục tiêu
Hiện thực các phương thức truy vấn zero-allocation trên `InventoryService`: `GetOwnedSpecies(buffer, group, rarity)`, `GetUnownedReleased(buffer)`, thứ tự sắp xếp hiển thị R6 (`rarityOrderDescending`, `group`, `id` ordinal), `CollectionProgress`, `TotalCopies`, `OwnedSpeciesCount`, `ReleasedSpeciesCount`.

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-9**: Sắp xếp danh sách đã sở hữu theo `rarityOrderDescending` (true: Huyền Thoại trước; false: Thường trước), không phụ thuộc thứ tự nhận thú.
- [x] **AC-10**: Cùng phẩm cấp và nhóm thì sắp xếp theo `id` ordinal tăng dần.
- [x] **AC-11**: Lọc theo `group` và `rarity` hoạt động chính xác; xóa buffer trước khi ghi; buffer `null` trả về 0 không lỗi.
- [x] **AC-19**: Tính `CollectionProgress` = `ReleasedOwned / ReleasedSpeciesCount` (loại trừ thú `deprecated`), trả 0 khi mẫu số = 0.
- [x] **AC-20**: `TotalCopies` trả về tổng `long` của các loài hiển thị (không tính orphan).
- [x] **AC-26**: Truy vấn lặp lại và cộng bản trùng đạt 0 B cấp phát GC.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `InventoryQueryTests.cs`: Kiểm tra AC-9, AC-10, AC-11, AC-19, AC-20, AC-26.
