# Epic: Pet Catalog

> **Layer**: Foundation
> **GDD**: Docs/design/gdd/pet-catalog.md
> **Code area**: Assets/CozyGachapon/Scripts/Core/PetCatalog/ (dữ liệu ScriptableObject + `PetDatabase`; assembly `CozyGachapon.Core` theo architecture.md — asmdef chưa tồn tại; validator editor đặt trong assembly Editor riêng theo ADR-0009)
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks pet-catalog`

## Overview

Pet Catalog là danh mục tĩnh, chỉ đọc, của mọi thú: mỗi thú là một ScriptableObject với ID bất biến `pet.<slug>`, phẩm cấp (Thường / Hiếm / Sử Thi / Huyền Thoại), đúng một nhóm (Động Vật / Đồ Ngọt / Hoàng Gia), một vai (Idle / Plinko / Hỗ trợ), trọng số và tối đa một hiệu ứng từ bộ từ vựng đóng. Catalog không chạy logic; Shelf, Economy, Gacha, Inventory, HUD và Feedback chỉ đọc. Pillar 3 được đặt bằng cấu trúc: sức mạnh đi từ bảng hệ số phẩm cấp, không có hiệu ứng buff toàn cục, nên các lỗi "Mèo Hoàng Gia x2 mọi thứ" và "Kẹo Bảy Màu nảy > 1.0" không thể viết ra bằng dữ liệu. Epic gồm schema dữ liệu, 7 thú MVP, bước boot `pet.catalog` (pha `DataLoad`) và validator editor.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0009: ScriptableObject Editor Validation | Validator editor kiểm biên tham số, nhóm, tổng pool; menu `Tools/CozyGachapon/Validate All Assets` | LOW |
| ADR-0001: Layered Assembly Architecture | `PetDatabase` ở Core, DTO/enum ở Contracts | LOW |
| ADR-0002: Bootstrap Composition Root | Bước `pet.catalog` pha `DataLoad` | LOW |
| ADR-0005: Layered Income Pipeline | Công thức độ lớn dựa trên `idleCpsUnit` và hệ số phẩm cấp | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-pet-001 | Data-driven ScriptableObject catalog with immutable IDs, groups, roles, rarities | ADR-0001, ADR-0002 ⚠️ partial (không ADR riêng cho schema dữ liệu) |
| TR-pet-002 | Multiplier calculation based on rarity factor and base idle CPS unit | ADR-0005 ⚠️ partial |
| TR-pet-003 | Editor validation for pet parameter bounds and group assignments | ADR-0009 ✅ |

Thông tin (không chặn): 2 trên 3 yêu cầu chỉ được ADR phủ một phần; mức tier standard chỉ đòi ADR Foundation nên không cần ADR mới.

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-40) from `Docs/design/gdd/pet-catalog.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`. At qaLevel minimal tests are advisory.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/pet-catalog/` (Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.
- Validator ADR-0009 chạy sạch trên 7 thú MVP và không có lỗi nhóm/phẩm cấp.
- GDD lưu ý: header `Status` là `In Review` trong khi systems-index ghi Approved; mọi giá trị số tạm cho tới mô phỏng bảng tính chung (Economy + Catalog + Shelf); còn các mục `Assumed` trong Open Questions (ví dụ `scopeBothMinTier`, Kẹo Bảy Màu `BouncinessModifier` 1.7).
- Lệch registry: `tr-registry.yaml` gán nhầm ID `TR-pet-003` cho 14 yêu cầu của Bootstrap, Pooling và Audio (hai mục trùng ID với yêu cầu validator thật); cần sửa registry ngoài phạm vi epic này.
- Lệch tầng: systems-index xếp Pet Catalog ở Foundation, architecture.md đặt `PetDatabase` ở Core.

## Next Step

Run `game-tasks pet-catalog` to break this epic into implementable stories (balanced granularity: kỳ vọng 5-10 stories). PR-EPIC bỏ qua (lean).
