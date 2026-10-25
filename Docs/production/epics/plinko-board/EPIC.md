# Epic: Plinko Board

> **Layer**: Core
> **GDD**: Docs/design/gdd/plinko-board.md
> **Code area**: Assets/CozyGachapon/Scripts/Core/Plinko/ (assembly `CozyGachapon.Core` — asmdef chưa tồn tại; adapter MonoBehaviour vật lý 2D đặt trong `CozyGachapon.Runtime`)
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks plinko-board`

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
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`. At qaLevel minimal tests are advisory.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/plinko-board/` (Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.
- Inherited from Bootstrap & Scene Flow: AC-20 (tạm dừng 60 s với 10 xu rơi), AC-23 (`WorldPress` bị chặn bởi nút UI, khớp Plinko AC-34), AC-24 (trễ router ≤ `routerMaxAddedFrames`, xu xuất hiện), AC-28 (renderer/camera bàn tắt khi sang Shelf nhưng `CoinResolved` vẫn tăng; cần thêm HUD cho phần `ActiveTab` mặc định), AC-39 (nhấn ở viền letterbox; cần nút UI từ HUD).
- Inherited from Object Pooling: AC-26, AC-29, AC-33 (pool `plinko.coin` trên bàn thật); AC-28 (prefab MVP thật, cùng Feedback & Juice).
- Inherited from Audio: AC-33 (0 cấp phát 600 khung trên bàn thật), AC-34 (bàn bão hòa trên thiết bị tham chiếu), AC-25 (WebGL Chrome/Safari cùng bàn thật).
- Phụ thuộc đã chốt nhưng chưa có số: ngân sách Physics2D/draw call, `k_diff` thực tế (prototype Plinko, AC-30), `maxActiveCoins` = 24 (Open Questions của GDD).
- GDD lưu ý: header `Status` là `In Review` trong khi systems-index ghi Approved.

## Next Step

Run `game-tasks plinko-board` to break this epic into implementable stories (balanced granularity: kỳ vọng 5-10 stories). PR-EPIC bỏ qua (lean).
