# Epic: Gacha Machine

> **Layer**: Feature
> **GDD**: Docs/design/gdd/gacha-machine.md
> **Code area**: Assets/CozyGachapon/Scripts/Features/Gacha/ (assembly `CozyGachapon.Features` — asmdef chưa tồn tại; validator editor theo ADR-0009)
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks gacha-machine`

## Overview

Máy trứng nằm ở tab Bàn Chơi, dưới bàn Plinko. Một lượt quay là một thao tác đồng bộ: trả tiền qua `TryPaySpin`, quay phẩm cấp, chọn thú, gọi `Inventory.AddPet`, phát `GachaRolled`; chỉ sau đó máy mới trình bày qua `ModalHost` của HUD và nhờ Feedback & Juice chạy hoạt cảnh theo `RevealProfile`. Kết quả ghi trước hoạt cảnh nên tải lại trang không làm mất thú và không cho save-scum. Máy sở hữu `GachaConfig` (tỷ lệ phẩm cấp, trọng số trong phẩm cấp, ngưỡng bảo hiểm `pityThreshold`, luật lượt đầu, luật tua, bảng tỷ lệ công khai) và bước boot `gacha.machine` (`IsCritical`). Không sở hữu giá quay (Economy Core), dữ liệu thú, số bản, sân khấu bóc trứng hay khung popup. Bán lượt quay bằng tiền thật bị cấm (Anti-Pillar).

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0008: Weighted Random Drop Selection | Chọn phẩm cấp/thú hai tầng bằng prefix sum, RNG inject để test seed cố định, 0 B GC mỗi lượt | LOW |
| ADR-0009: ScriptableObject Editor Validation | Kiểm tổng bảng drop và phủ phẩm cấp trước build | LOW |
| ADR-0007: Event-Driven Feedback Pipeline | Gacha gọi `RequestReveal`/`SkipReveal`, nghe `RevealSettleStarted`/`RevealFinished` | LOW |
| ADR-0005: Layered Income Pipeline | Giá quay từ họ công thức của Economy Core | LOW |
| ADR-0001: Layered Assembly Architecture | Ranh giới Contracts/Features | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-gacha-001 | Single-pull gacha machine with dynamic coin cost querying Economy Core | ADR-0005, ADR-0001 ⚠️ partial |
| TR-gacha-002 | Drop pool resolver with public rarity weights producing starter pets | ADR-0008, ADR-0009 ✅ |
| TR-gacha-003 | Atomic integration dispatching new acquisitions to Inventory | ADR-0008 (gói kết quả), ADR-0001 ⚠️ partial |
| TR-gacha-004 | Reveal orchestration lifecycle with skip and fast-forward handling | ADR-0007 ✅ |

Thông tin: TR-gacha-001 và TR-gacha-003 mới được phủ một phần; không chặn ở mức standard.

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-33) from `Docs/design/gdd/gacha-machine.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`. At qaLevel minimal tests are advisory.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/gacha-machine/` (Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.
- Monte Carlo 100.000 lượt (ADR-0008) khớp tỷ lệ công bố trong dung sai; bảng tỷ lệ hiển thị khớp `GachaConfig`.
- GDD lưu ý: header `Status` là `In Review` trong khi systems-index ghi Approved; còn nhiều mục `Assumed` (pity 50, modal không đóng được, không hoàn xu khi `AddPet` bị từ chối); Gacha cần HUD (`ModalHost`, `PetCardView`) và Feedback (`RequestReveal`) để chạy trọn luồng, nên các AC play mode cần chờ hai epic Presentation hoặc dùng giả lập.
- Sai lệch nhỏ: ADR-0007 và GDD dùng phương án (A) cho sự kiện bóc trứng (Feedback sở hữu, không còn `GachaReveal*`); ADR-0008 ghi tỷ lệ 60/30/9/1 trong khi GDD ghi tỷ lệ thực 3.84 % cho Huyền Thoại có bảo hiểm — story phải lấy `GachaConfig` làm chuẩn, kiểm với ADR khi viết test.

## Next Step

Run `game-tasks gacha-machine` to break this epic into implementable stories (balanced granularity: kỳ vọng 5-10 stories). PR-EPIC bỏ qua (lean).
