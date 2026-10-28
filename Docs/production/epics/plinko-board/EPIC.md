# Epic: Plinko Board

> **Layer**: Core
> **GDD**: Docs/design/gdd/plinko-board.md
> **Code area**: Assets/CozyGachapon/Scripts/Core/Plinko/ (assembly `CozyGachapon.Core`; adapter MonoBehaviour vật lý 2D đặt trong `CozyGachapon.Runtime`)
> **Status**: Ready
> **Stories**: 13 — see table

## Overview

Plinko Board là nguồn xu chủ động của game: bàn gỗ đứng dọc ở Tab 1, người chơi chạm để thả xu, xu nảy qua các tầng đinh và rơi vào một trong năm hũ `[x5][x2][x1][x2][x5]`. Người chơi chỉ chọn vị trí ngang trong vùng thả ở giữa và mỗi xu lệch ngẫu nhiên, nên jackpot là may mắn chứ không phải ngắm. Bàn còn nhận xu từ thả tự động, mưa xu và Bùng Xu. Với các hệ khác, Plinko là nguồn dữ liệu có trần: chỉ báo giá trị thô (xu gốc × hệ số hũ) và sự kiện `CoinSpawned` / `PegHit` / `JackpotHit` / `CoinResolved` / `CoinStuck` / `BoardBusy`; Economy Core áp bùa lợi. Mọi giới hạn chống lạm phát và quá tải WebGL (cooldown chạm, trần xu đồng thời, trần nảy, despawn xu kẹt) thuộc hệ này. Bàn chạy hoàn toàn trong world-space 2D với camera riêng; API `RequestBurst` / `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister`, cờ `acceptManualTaps`, interface `IBaseCoinValueSource`.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0006: World Space Plinko Physics | Box2D world-space với camera riêng, bước vật lý cố định, trần xu và độ nảy < 1.0, watchdog xu kẹt | LOW |
| ADR-0003: Zero-Allocation Object Pooling | Pool `plinko.coin`, lease an toàn, không cấp phát khi chạy | LOW |
| ADR-0002: Bootstrap Composition Root | `WorldPress`, `TabChanged`, `Paused`/`Resumed`, `Interactive` + `IsSimulationRunning` | LOW |
| ADR-0001: Layered Assembly Architecture | `IPlinkoBoard` và event struct ở Contracts | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-plinko-001 | 2D Box2D physics simulation rendered in dedicated World Space camera | ADR-0006 ✅ |
| TR-plinko-002 | Spawning rate limiter and hard concurrent active coin cap | ADR-0006 ✅ |
| TR-plinko-003 | Five bucket multipliers with bounded bounciness factor < 1.0 | ADR-0006 ✅ |
| TR-plinko-004 | Event publication for PegHit, JackpotHit, CoinResolved, and CoinStuck | ADR-0006 ✅ |
| TR-plinko-005 | Despawn watchdog timer recovering stuck coins back to pool | ADR-0006 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria (AC-1…AC-49) from `Docs/design/gdd/plinko-board.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/plinko-board/` (Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | [Hình học bàn & kiểm định thông số](story-001-board-geometry-and-config-validation.md) | Logic | Complete | ADR-0006 |
| 002 | [Thiết lập vật lý Box2D & trần độ nảy](story-002-physics-simulation-and-bounciness-cap.md) | Logic | Complete | ADR-0006 |
| 003 | [Vùng thả & bộ kiểm soát nhận chạm](story-003-drop-zone-and-tap-admission-limiter.md) | Logic | Ready | ADR-0006 |
| 004 | [Quản lý sức chứa & hàng đợi loạt](story-004-concurrency-and-burst-queue-management.md) | Logic | Ready | ADR-0006 |
| 005 | [Va chạm đinh & tính điểm có debounce](story-005-peg-collision-and-scored-debounce.md) | Logic | Ready | ADR-0006 |
| 006 | [Watchdog phát hiện xu kẹt & despawn](story-006-stuck-watchdog-and-despawn-timer.md) | Logic | Ready | ADR-0006 |
| 007 | [Cảm biến hũ, trả thưởng & tích hợp Economy](story-007-bucket-payout-and-economy-integration.md) | Integration | Ready | ADR-0006, ADR-0005 |
| 008 | [Jackpot & chuỗi Bùng Xu không lặp](story-008-jackpot-and-chain-free-burst-cascades.md) | Logic | Ready | ADR-0006 |
| 009 | [Dịch vụ đăng ký nguồn ngoài & Auto-Drop](story-009-external-modifiers-and-auto-drop-service.md) | Logic | Ready | ADR-0006 |
| 010 | [Bộ sự kiện R12 & quản lý pool zero-allocation](story-010-event-publication-and-zero-allocation-pooling.md) | Integration | Ready | ADR-0006, ADR-0003 |
| 011 | [Vòng đời mô phỏng & tích hợp Bootstrap](story-011-simulation-lifecycle-and-bootstrap-integration.md) | Integration | Ready | ADR-0006, ADR-0002 |
| 012 | [Kiểm chứng thống kê & mô hình cân bằng](story-012-balance-models-and-statistical-verification.md) | Logic | Ready | ADR-0006 |
| 013 | [Viewport camera, định tuyến chạm & hiển thị](story-013-presentation-touch-router-and-ui-alignment.md) | Visual/Feel | Ready | ADR-0006 |

## Next Step

Run `game-readiness Docs/production/epics/plinko-board/story-001-board-geometry-and-config-validation.md` or `game-dev Docs/production/epics/plinko-board/story-001-board-geometry-and-config-validation.md` to begin implementation.
