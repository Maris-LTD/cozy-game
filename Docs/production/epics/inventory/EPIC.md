# Epic: Inventory

> **Layer**: Core
> **GDD**: Docs/design/gdd/inventory.md
> **Code area**: Assets/CozyGachapon/Scripts/Core/Inventory/
> **Status**: Complete
> **Stories**: 5/5 stories completed (Story 001 - Story 005)

## Overview

Inventory là mô hình dữ liệu nhỏ có đúng một cổng ghi (`AddPet(id, amount = 1)` trả `Added { isNew, copies }` hoặc `Rejected`). Nó trả lời "đã có loài này chưa", "có bao nhiêu bản" và "danh sách loài đã sở hữu theo thứ tự ổn định", và phát một sự kiện duy nhất `PetAcquired { id, isNew, copies }` cho Gacha, HUD và Feedback. Inventory không sở hữu dữ liệu thú, tỷ lệ gacha, công thức Sao, vị trí kệ hay định dạng save. Epic gồm kho trong bộ nhớ, hàng đợi sự kiện có trần (`maxQueuedEvents` = 64), cách giữ ID lạ, truy vấn chỉ đọc cho Shelf, và bước boot `inventory.core`. Màn Kho vẽ bởi Shelf & Synergy.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0001: Layered Assembly Architecture | Contracts cho interface/DTO/sự kiện, cài đặt ở assembly Core | LOW |
| ADR-0002: Bootstrap Composition Root | Bước `inventory.core` ở pha `GameplayInit` | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-inventory-001 | In-memory store tracking owned pet IDs, duplicate counts, and Star rank | ADR-0001, ADR-0002 ⚠️ partial |
| TR-inventory-002 | Mutation events for pet acquisition and duplicate increments | ADR-0001 ⚠️ partial |
| TR-inventory-003 | Read-only query interfaces for shelf loadout selection | ADR-0001 ⚠️ partial |

## Stories

| Story | Title | Type | Status |
|-------|-------|------|--------|
| [story-001](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/story-001-inventory-contracts-config-and-math.md) | Inventory Contracts, Config and Badge Math | Logic | Complete |
| [story-002](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/story-002-core-inventory-store-addpet-and-event-queue.md) | Core Inventory Store, AddPet and FIFO Event Queue | Logic | Complete |
| [story-003](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/story-003-inventory-queries-sorting-and-progress.md) | Inventory Queries, Display Sorting and Collection Progress | Logic | Complete |
| [story-004](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/story-004-orphan-retention-starting-pets-and-save-snapshot.md) | Orphan Retention, Starting Pets and Save Snapshot | Logic | Complete |
| [story-005](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/inventory/story-005-inventory-boot-step-and-config-validator.md) | Inventory Boot Step, Wiring and Config Validator | Integration | Complete |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-29) from `Docs/design/gdd/inventory.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`.
- `unity-harness all` passes.
