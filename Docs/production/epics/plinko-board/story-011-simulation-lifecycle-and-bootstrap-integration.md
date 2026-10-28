# Story 011: Vòng đời mô phỏng & tích hợp Bootstrap (Simulation Lifecycle & Bootstrap Integration)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Integration
> **Type**: Integration
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-001`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation, ADR-0002: Bootstrap Composition Root
**ADR Decision Summary**: Triển khai IBootStep plinko.board trong pha GameplayInit; mô phỏng chỉ chạy khi IsSimulationRunning đúng; khi nhận Paused, lập tức xả toàn bộ hàng đợi loạt với lý do Overflow để không mất thưởng của người chơi; kẹp bước mô phỏng bằng maxSimStep khi gặp khung dài.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Triển khai hợp đồng `IBootStep` với `Id = "plinko.board"`, `Phase = BootPhase.GameplayInit`, `IsCritical = true`; kiểm tra `IsSimulationRunning` trước mỗi bước mô phỏng.
- Forbidden: Không tự ý mô phỏng dưới rèm (trước `Interactive`); không bù bước vật lý cho thời gian đã dừng.
- Guardrails: Khung hình giật (hitch frame) bị kẹp tại `maxSimStep` để ngăn xuyên vật lý; xả hàng đợi khi `Paused` theo thứ tự FIFO trong cùng lời xử lý.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoLifecycleTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-28 [Play mode]**: GIVEN `T.lifecycleTestCoins` xu đang rơi, WHEN chuyển sang Tab 2 trong `T.tabSwitchTestSeconds`, THEN số `CoinResolved` vẫn tăng, renderer của bàn tắt, mô phỏng vẫn chạy.
- [ ] **AC-29 [EditMode]**: GIVEN `T.lifecycleTestCoins` xu đang rơi, hàng đợi loạt không rỗng, nguồn vòng đời giả của Bootstrap và đồng hồ inject, WHEN nguồn giả phát `Paused {Hidden}`, đồng hồ thực tiến `T.pauseTestSeconds`, rồi phát `Resumed {Hidden, awaySeconds = T.pauseTestSeconds}` với `IsSimulationRunning = true`, THEN ngay sau `Paused` hàng đợi loạt rỗng (xả theo R11.3); trong lúc Paused số bước mô phỏng = 0; sau `Resumed` không xu nào thành Stuck hay hết tuổi, vị trí xu đang rơi và mốc cooldown giữ nguyên. AND WHEN chỉ mất focus hoặc cờ ad đang mở bật, THEN mô phỏng tiến đủ số bước, `CoinResolved` tăng và hàng đợi không bị xả. AND GIVEN nguồn giả ở `Booting.Curtain` (`IsSimulationRunning = false`), bàn ở Chờ mở cửa sau `Run()`, một nguồn auto-drop và hàng đợi có xu Rain, WHEN phát `Paused` rồi `Resumed` về `Booting.Curtain`, rồi phát `Interactive`, THEN hàng đợi xả khi Paused; từ Resumed tới Interactive số bước = 0; sau Interactive bước mô phỏng tiến và xu Auto sinh đúng chu kỳ.
- [ ] **AC-47 [EditMode]**: GIVEN nguồn vòng đời giả, listener Economy giả, pool `plinko.coin` giả đếm Get, `T.lifecycleTestCoins` xu Falling và hàng đợi loạt đầy `C.burstQueueMax` xu, WHEN nguồn giả phát `Paused {Background}`, sau đó gọi `RequestBurst(C.jackpotBurstCount, Rain)`, rồi phát `Resumed` và sim tới khi bàn Idle, THEN trong cùng lời xử lý `Paused` có đúng `C.burstQueueMax` `CoinResolved` theo thứ tự FIFO của `coinId`, mỗi cái có `reason = Overflow`, `bucketIndex = none`, `scoredPegHits = 0`, `origin` đúng nguồn, `rawValue = C.baseCoinValue * C.instantPayoutMultiplier`; 0 `CoinSpawned` và 0 Get; hàng đợi rỗng; 0 `CoinResolved` cho xu Falling trong lúc Paused; `RequestBurst` trong lúc Paused phát ngay đúng `C.jackpotBurstCount` `CoinResolved(reason = Overflow)`.
- [ ] **AC-48 [EditMode]**: GIVEN registry bước khởi động giả, dịch vụ pool giả và router giả, WHEN scene nạp, Plinko đăng ký bước rồi Bootstrap gọi `Run()`, THEN có đúng 1 bước `Id = "plinko.board"`, `Phase = GameplayInit`, `RunsAfter` rỗng, `IsCritical = true`; `Run()` hoàn thành trong 1 khung (`IsDone = true`); pool nhận đăng ký `plinko.coin` với `client_declared_max = C.maxActiveCoins`, `forbid_recycle = true`; router giả có `PlinkoViewport` + camera Plinko; bàn ở trạng thái Chờ mở cửa. Nếu pool từ chối đăng ký, `IsFailed = true`.
- [ ] **AC-49 [EditMode]**: GIVEN bàn `Running` với `T.lifecycleTestCoins` xu đang rơi và hàng đợi loạt, WHEN một khung có `dt = T.hitchFrameSeconds (> C.maxSimStep)` rồi `dt = 1/60 s`, THEN thời gian mô phỏng tiến đúng `C.maxSimStep` ở khung đầu và 1/60 s ở khung sau, và không xu nào bị bỏ qua cảm biến hay đinh (không xuyên).

## Implementation Notes

- Cài đặt `PlinkoBootStep : IBootStep`:
  - `Id => "plinko.board"`, `Phase => BootPhase.GameplayInit`, `IsCritical => true`, `RunsAfter => Array.Empty<string>()`.
  - Trong `Run()`: đăng ký pool `plinko.coin` (`client_declared_max = maxActiveCoins`, `forbid_recycle = true`); thiết lập `PlinkoViewport` và camera World Space; chuyển bàn sang trạng thái "Chờ mở cửa".
- Quản lý đồng hồ mô phỏng `PlinkoSimulationClock`:
  - Đọc `IBootstrapController.IsSimulationRunning` trước mỗi khung hình. Nếu false, không tiến bước nào.
  - Kẹp thời gian mỗi khung `deltaSimTime = Mathf.Min(deltaTime, maxSimStep)`.
- Xử lý sự kiện `Paused`:
  - Lập tức duyệt toàn bộ hàng đợi loạt FIFO, phát `CoinResolved` với `reason = CoinResolveReason.Overflow`, `rawValue = baseCoinValue * instantPayoutMultiplier`.
  - Đặt hàng đợi về rỗng.

## Out of Scope

- [Story 012]: Kiểm chứng thống kê và mô hình cân bằng.
- [Story 013]: Viewport camera, định tuyến chạm và giao diện.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Integration
**Required evidence**: An EditMode test `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoLifecycleTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 010 must be Complete
- Unlocks: Story 012

