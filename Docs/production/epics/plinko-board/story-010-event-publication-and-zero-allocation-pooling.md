# Story 010: Bộ sự kiện R12 & quản lý pool zero-allocation (Event Publication & Zero-Allocation Pooling)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Integration
> **Type**: Integration
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-004`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation, ADR-0003: Zero-Allocation Object Pooling
**ADR Decision Summary**: Plinko công bố 6 sự kiện chuẩn R12 không phụ thuộc trực tiếp vào các hệ thống nghe; đối tượng đồng xu được mượn từ pool plinko.coin khi sinh và hoàn trả ngay khi giải quyết xong (Resolved -> Pooled) đảm bảo 0 B GC per frame.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Đảm bảo 0 B GC per frame trong các khung hình mô phỏng; mượn và trả pool an toàn theo hợp đồng `IPoolService`.
- Forbidden: Tuyệt đối không có trạng thái Fizzling giữ xu lại pool (hiệu ứng tan do Feedback & Juice vẽ bằng pool riêng); không gọi `Instantiate` hoặc `Destroy` tại runtime.
- Guardrails: Số lượng đối tượng mượn từ pool không bao giờ vượt quá `maxActiveCoins`; các xu Queued hoặc Overflow không mượn đối tượng pool.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoEventAndPoolTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`
- Contracts: `Assets/CozyGachapon/Scripts/Contracts/`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-38 [Perf]**: GIVEN pool đã warm, trạng thái ổn định, WHEN ghi `PoolingTestTargets.perfSampleFrames` (600) khung bằng `ProfilerRecorder` "GC Allocated In Frame", THEN 0 B mỗi khung, kể cả lúc phát `PegHit`, `CoinResolved` và số bay (`Physics2D.reuseCollisionCallbacks` bật; bảng debounce theo cặp xu-đinh và bộ đệm sự kiện được cấp phát sẵn theo `C.maxActiveCoins`, không cấp phát khi chạy).
- [ ] **AC-42 [EditMode]**: GIVEN listener ghi mọi sự kiện, stub "hũ 0 là jackpot", sim có xu Manual, Auto, Rain, xu kích hoạt Bùng Xu, một xu bị ép kẹt, một `RequestBurst` vượt `C.burstQueueMax` và một cú chạm khi bàn đủ `C.maxActiveCoins`, WHEN sim tới khi bàn Idle, THEN: mỗi `CoinSpawned` có `coinId` duy nhất, `origin` đúng nguồn, `burstIndex` hợp lệ, `worldPosition.y` = vạch thả; mỗi `PegHit` có `coinId`, `pegId`, `rowIndex`, `scored`, `worldPosition` cách tâm đinh ≤ tổng bán kính + dung sai; mỗi `JackpotHit` có `bucketIndex`, `origin` và phát trước `CoinResolved` cùng `coinId`; mỗi xu kẹt phát đúng 1 `CoinStuck` trước `CoinResolved(reason = Stuck)`; xu Overflow có `CoinResolved(reason = Overflow, bucketIndex = none, scoredPegHits = 0)` và không có `CoinSpawned`; cú chạm bị bỏ phát đúng 1 `BoardBusy`.
- [ ] **AC-46 [EditMode]**: GIVEN pool `plinko.coin` giả đếm Get/Release, WHEN sim `T.simCoinCountSmall` xu gồm xu vào hũ và xu bị ép kẹt, THEN mỗi xu Release đúng một lần trong cùng bước mô phỏng phát `CoinResolved` của nó, số đối tượng đang mượn ≤ `C.maxActiveCoins` ở mọi bước, và xu Queued/Overflow không bao giờ Get.

## Implementation Notes

- Xây dựng lớp phát sự kiện `PlinkoEventDispatcher` cung cấp các action/event C# thuần với struct payload:
  - `CoinSpawned { string coinId, CoinOrigin origin, int burstIndex, Vector2 worldPosition }`
  - `PegHit { string coinId, string pegId, int rowIndex, bool scored, Vector2 worldPosition }`
  - `JackpotHit { string coinId, CoinOrigin origin, int bucketIndex, Vector2 worldPosition }`
  - `CoinResolved { CoinResolvedEvent payload }`
  - `CoinStuck { string coinId, CoinOrigin origin, Vector2 worldPosition }`
  - `BoardBusy { Vector2 worldPosition }`
- Tích hợp vòng đời pool `plinko.coin`:
  - Khi xu vào trạng thái `Falling`: gọi `IPoolService.Get("plinko.coin")`.
  - Khi xu vào trạng thái `Resolved`: gọi `IPoolService.Release("plinko.coin", lease)` trong cùng bước xử lý.
  - Xu ở `Queued` hoặc giải quyết ngay bằng `Overflow`: tuyệt đối không gọi `Get`.

## Out of Scope

- [Story 011]: Vòng đời mô phỏng và tích hợp Bootstrap.
- [Story 012]: Kiểm chứng thống kê và mô hình cân bằng.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Integration
**Required evidence**: An EditMode test `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoEventAndPoolTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 009 must be Complete
- Unlocks: Story 011

