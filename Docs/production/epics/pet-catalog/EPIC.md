# Epic: Pet Catalog

> **Layer**: Foundation
> **GDD**: Docs/design/gdd/pet-catalog.md
> **Code area**: Assets/CozyGachapon/Scripts/Core/PetCatalog/ (dữ liệu ScriptableObject + `PetDatabase`; assembly `CozyGachapon.Core` theo architecture.md — validator editor đặt trong assembly Editor riêng theo ADR-0009)
> **Status**: Complete
> **Stories**: 6/6 stories completed (Story 001 - Story 006)

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

## Stories

| Story | Title | Type | Status |
|-------|-------|------|--------|
| [story-001](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/story-001-pet-contracts-enums-and-rarity-config.md) | Pet Contracts, Enums, Rarity Config and Power Formulas | Logic | Complete |
| [story-002](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/story-002-pet-definition-and-7-mvp-roster.md) | PetDefinition ScriptableObject and 7 MVP Roster Assets | Config/Data | Complete |
| [story-003](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/story-003-pet-database-query-api-and-boot-step.md) | Pet Database Query API and Catalog Boot Step | Integration | Complete |
| [story-004](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/story-004-pet-card-effect-description-formatter.md) | Pet Card Effect Description Formatter | Logic | Complete |
| [story-005](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/story-005-pet-catalog-editor-validator.md) | Pet Catalog Editor and Data Integrity Validator | Logic | Complete |
| [story-006](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/story-006-balance-model-targets-and-budget-validator.md) | Balance Model Targets and Power Budget Validator | Logic | Complete |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-40) from `Docs/design/gdd/pet-catalog.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`.
- `unity-harness all` passes.
- Validator ADR-0009 chạy sạch trên 7 thú MVP và không có lỗi nhóm/phẩm cấp.
