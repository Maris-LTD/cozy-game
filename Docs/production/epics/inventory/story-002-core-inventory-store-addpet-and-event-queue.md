# Story 002: Core Inventory Store, AddPet and FIFO Event Queue

> **Epic**: [Inventory](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/EPIC.md)  
> **Status**: Complete (Verified 1080/1080 tests)
> **Type**: Logic
> **Granularity**: Balanced

---

## 1. Mục tiêu
Hiện thực `InventoryService` quản lý ánh xạ sở hữu `id -> copies`, cổng ghi duy nhất `AddPet(id, amount = 1)` kiểm tra 4 điều kiện (Ready, amount >= 1, Catalog hợp lệ, hàng đợi sự kiện chưa đầy), và cơ chế hàng đợi sự kiện FIFO chống đệ quy (`maxQueuedEvents`).

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-1**: `AddPet` loài mới trả `Added { isNew = true, copies = 1 }`, `IsOwned = true`, `GetCopies = 1`, phát `PetAcquired` đúng 1 lần.
- [x] **AC-2**: `AddPet` loài đã có tăng `copies`, `isNew = false`, `OwnedSpeciesCount` không đổi, phát `PetAcquired` 1 lần.
- [x] **AC-3**: `AddPet` với `amount = 3` cộng dồn `copies` và chỉ phát `PetAcquired` đúng 1 lần.
- [x] **AC-4**: `AddPet` với ID không tồn tại trong Catalog, chuỗi rỗng hoặc `null` trả `Rejected(UnknownId)`, 0 sự kiện.
- [x] **AC-5**: `AddPet` với `amount <= 0` trả `Rejected(InvalidAmount)`, 0 sự kiện.
- [x] **AC-6**: Khi `Uninitialized` hoặc `Failed`, `AddPet` trả `Rejected(NotReady)`, truy vấn trả `false` / 0.
- [x] **AC-7**: Cộng vượt `int.MaxValue` kẹp đúng ở `int.MaxValue`, không tràn số âm.
- [x] **AC-22**: Khi listener gọi `AddPet` lồng nhau, sự kiện được xếp vào hàng đợi FIFO và phát tuần tự sau khi sự kiện hiện tại đến hết các listener.
- [x] **AC-29**: Khi hàng đợi vượt quá `maxQueuedEvents`, trả `Rejected(QueueFull)` mà không đổi trạng thái.

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `InventoryAddPetTests.cs`: Kiểm tra AC-1 đến AC-7.
  - `InventoryEventQueueTests.cs`: Kiểm tra AC-22 và AC-29.
