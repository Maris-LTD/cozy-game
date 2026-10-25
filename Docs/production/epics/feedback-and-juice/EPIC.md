# Epic: Feedback & Juice

> **Layer**: Presentation
> **GDD**: Docs/design/gdd/feedback-and-juice.md
> **Code area**: Assets/CozyGachapon/Scripts/Presentation/Feedback/ (assembly `CozyGachapon.Presentation` — asmdef chưa tồn tại)
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks feedback-and-juice`

## Overview

Feedback & Juice dịch sự kiện gameplay thành cảm giác: mỗi sự kiện có tối đa một dòng trong `FeedbackCueTable` (cue, cao độ, pan, haptic, bậc ưu tiên) và một hiệu ứng hình ảnh; không có dòng nghĩa là im lặng. Hệ giữ tiền luôn đọc được bằng ngân sách hiệu ứng 3 bậc (tiền/jackpot > phẩm cấp > đinh/âm lặt vặt; số bay dùng `RecycleOldest`) và sở hữu khoảnh khắc bóc trứng với `RevealProfile` theo phẩm cấp (Gacha chỉ gọi `RequestReveal`/`SkipReveal`). Tông cozy: không chữ đỏ khi từ chối chi tiêu, không rung màn hình, hình ảnh phẩm cấp chỉ lộ sau khi kết quả đã định. Gồm cờ `ReduceMotion` toàn cục, số bay từ pool `feedback.floatingText` / `feedback.coinFizzle` và nối `TabChanged` sang `SetBusBackground` của Audio.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0007: Event-Driven Feedback Pipeline | Orchestrator dịch sự kiện sang cue/tween/số bay, vòng đời bóc trứng có tua, cờ `ReduceMotion` | LOW |
| ADR-0003: Zero-Allocation Object Pooling | Số bay và hiệu ứng mượn từ `IPoolService` | LOW |
| ADR-0004: Priority Voice Management | Cue qua `IAudioService`, bus nền | LOW |
| ADR-0001: Layered Assembly Architecture | Presentation chỉ tham chiếu Contracts | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-feedback-001 | Event-to-feedback translator coordinating audio cues, floating text, and tweens | ADR-0007 ✅ |
| TR-feedback-002 | Zero-allocation floating text display borrowed from Object Pooling | ADR-0007, ADR-0003 ✅ |
| TR-feedback-003 | Gacha reveal animation sequence with rarity glow and skip integration | ADR-0007 ✅ |
| TR-feedback-004 | Global ReduceMotion flag disabling intense screenshake and flashing | ADR-0007 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-32) from `Docs/design/gdd/feedback-and-juice.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`. At qaLevel minimal tests are advisory.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/feedback-and-juice/` (Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.
- Inherited from Audio: AC-20 (nối `TabChanged` sang `SetBusBackground`, bộ đếm Played của `PegHit` không tăng ở Tab 2 trong khi `Bucket*` vẫn tăng; cần bàn Plinko thật).
- Inherited from Object Pooling: AC-27, AC-34 (pool số bay/hiệu ứng thật); AC-28 (prefab MVP thật, cùng Plinko Board).
- Audio là epic đã Complete: các cue mới mà GDD giả định (`Shelf.ComboTierUp`, `Shelf.SlotUnlock`, `Ui.Rejected`) chưa có trong bảng cue, cần story thêm cue vào Audio config (đề xuất theo Open Questions của GDD).
- GDD lưu ý: header `Status` là `In Review` trong khi systems-index ghi Approved; `ReduceMotion` không lưu giữa phiên ở MVP (Settings & Accessibility, Alpha); màu ánh sáng phẩm cấp chờ art-director xác nhận; các giá trị ngân sách hiệu ứng mặc định là đề xuất.

## Next Step

Run `game-tasks feedback-and-juice` to break this epic into implementable stories (balanced granularity: kỳ vọng 5-10 stories). PR-EPIC bỏ qua (lean).
