# Epic: Inventory

> **Layer**: Core
> **GDD**: Docs/design/gdd/inventory.md
> **Code area**: Assets/CozyGachapon/Scripts/Features/Inventory/ (theo architecture.md `InventoryService` thuộc assembly `CozyGachapon.Features` — asmdef chưa tồn tại; systems-index xếp hệ này ở tầng Core)
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks inventory`

## Overview

Inventory là mô hình dữ liệu nhỏ có đúng một cổng ghi (`AddPet(id, amount = 1)` trả `Added { isNew, copies }` hoặc `Rejected`). Nó trả lời "đã có loài này chưa", "có bao nhiêu bản" và "danh sách loài đã sở hữu theo thứ tự ổn định", và phát một sự kiện duy nhất `PetAcquired { id, isNew, copies }` cho Gacha, HUD và Feedback. Inventory không sở hữu dữ liệu thú, tỷ lệ gacha, công thức Sao, vị trí kệ hay định dạng save. Epic gồm kho trong bộ nhớ, hàng đợi sự kiện có trần (`maxQueuedEvents` = 64), cách giữ ID lạ, truy vấn chỉ đọc cho Shelf, và bước boot `inventory.core`. Màn Kho vẽ bởi Shelf & Synergy.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0001: Layered Assembly Architecture | Contracts cho interface/DTO/sự kiện, cài đặt ở assembly Features | LOW |
| ADR-0002: Bootstrap Composition Root | Bước `inventory.core` ở pha `GameplayInit` | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-inventory-001 | In-memory store tracking owned pet IDs, duplicate counts, and Star rank | ADR-0001, ADR-0002 ⚠️ partial |
| TR-inventory-002 | Mutation events for pet acquisition and duplicate increments | ADR-0001 ⚠️ partial |
| TR-inventory-003 | Read-only query interfaces for shelf loadout selection | ADR-0001 ⚠️ partial |

Thông tin (không chặn): cả 3 yêu cầu chỉ được ADR-0001/0002 phủ một phần và không có ADR riêng; mức standard chỉ đòi ADR Foundation. Lưu ý TR-inventory-001 nhắc "Star rank" nhưng GDD giao Sao cho Star Progression (VS) suy từ `GetCopies`, không lưu ở Inventory.

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-29) from `Docs/design/gdd/inventory.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`. At qaLevel minimal tests are advisory.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/inventory/` (Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.
- GDD lưu ý: header `Status` là `In Review` trong khi systems-index ghi Approved; Save/Load là soft (VS) nên khôi phục trạng thái và quy ước "không có khóa Inventory" áp dụng `startingPets` chưa nằm trong epic MVP.
- Lệch tầng: systems-index xếp Inventory ở Core, architecture.md đặt `InventoryService` ở assembly Features.

## Next Step

Run `game-tasks inventory` to break this epic into implementable stories (balanced granularity: kỳ vọng 5-10 stories; hệ nhỏ có thể rơi ở cận dưới). PR-EPIC bỏ qua (lean).
