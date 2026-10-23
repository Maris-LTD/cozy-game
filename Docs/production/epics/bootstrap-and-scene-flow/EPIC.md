# Epic: Bootstrap & Scene Flow

> **Layer**: Foundation
> **GDD**: Docs/design/gdd/bootstrap-and-scene-flow.md
> **Code area**: Assets/CozyGachapon/Scripts/Foundation/Bootstrap/
> **Status**: Ready
> **Stories**: 9 — see table

## Overview

Bootstrap & Scene Flow là hệ hạ tầng điều phối toàn bộ vòng đời và luồng cảnh của game trong một scene duy nhất (`MainGame`). Hệ thống chịu trách nhiệm thiết lập Composition Root thuần C#, điều phối trình tự khởi động theo các pha (`ServiceInit` -> `DataLoad` -> `SaveLoad` -> `GameplayInit` -> `Prewarm` -> `Ready`) qua hợp đồng `IBootStep`, che toàn bộ quá trình tải bằng rèm "Tiệm mở cửa" và đảm bảo đưa người chơi tới tương tác đầu tiên trong vòng 2 giây. Sau khi khởi động, Bootstrap quản lý vòng đời ứng dụng (tạm dừng/tiếp tục mô phỏng khi ẩn tab WebGL hoặc chuyển nền ứng dụng di động), điều hướng chuyển đổi tab giao diện kèm cổng chặn input trung tâm, và duy trì viewport cố định tỉ lệ 9:16 đến 9:21 với viền trang trí.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0001: Layered Assembly Architecture | Phân tách module thành 5 asmdef runtime tuân thủ đồ thị phụ thuộc một chiều DAG | LOW |
| ADR-0002: Bootstrap Composition Root & Phased Lifecycle | Khởi tạo thuần C# với interface `IBootStep`, điều phối vòng đời tạm dừng và router tab | LOW |
| ADR-0003: Zero-Allocation Object Pooling | Đồng bộ pha khởi động `ServiceInit` và `Prewarm` trước khi mở rèm | LOW |
| ADR-0006: World Space Plinko Physics | Đồng bộ hóa viewport 9:16-9:21 và quản lý vòng đời tạm dừng mô phỏng | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-boot-001 | Phased startup sequence using IBootStep interface in single-scene architecture | ADR-0002, ADR-0001 ✅ |
| TR-boot-002 | Simulation pause lifecycle on application hidden/background and continue on focus loss | ADR-0002 ✅ |
| TR-boot-003 | Content view tab router with central input blocking barrier | ADR-0002 ✅ |
| TR-boot-004 | Fixed 9:16 to 9:21 vertical aspect viewport layout with letterbox bounds | ADR-0002, ADR-0006 ✅ |
| TR-boot-005 | Startup curtain coordination covering prewarm operations until ready | ADR-0002, ADR-0003 ✅ |

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | IBootStep Phased Startup Sequencer | Logic | Complete | ADR-0002 |
| 002 | Boot Failure Watchdog & Timeout Handler | Logic | Complete | ADR-0002 |
| 003 | Startup Curtain Controller & Interactive Milestone | Logic | Complete | ADR-0002 |
| 004 | Application Lifecycle & Visibility Watchdog | Logic | Complete | ADR-0002 |
| 005 | Rewarded Ad Lifecycle & AdClosed Integration | Logic | Complete | ADR-0002 |
| 006 | Central Input Barrier & Tab Router | Logic | Complete | ADR-0002 |
| 007 | World Press Input Routing & Modal Back Handler | Integration | Ready | ADR-0002 |
| 008 | Viewport Calculator & Aspect Ratio Letterboxing | Logic | Ready | ADR-0002, ADR-0006 |
| 009 | Startup Curtain View & Retry Panel | UI | Ready | ADR-0002 |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria from `Docs/design/gdd/bootstrap-and-scene-flow.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/bootstrap-and-scene-flow/` (mỗi màn hình được can thiệp; Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.

## Next Step

Run `game-readiness Docs/production/epics/bootstrap-and-scene-flow/story-007-world-press-router.md` to validate the next story, then `game-dev` to implement it.
