# Story 013: Lifecycle State Machine & Idle Accrual

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R6.2, R6.3, R10.4 phần xử lý `CoinResolved` ở Ready/Paused, R16.3–R16.4, States and Transitions, Formulas `idle_accrual`, Edge Cases "Thời gian")
**Requirement**: `TR-economy-002`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root (kèm ADR-0005)
**ADR Decision Summary**: mô phỏng dừng khi tab ẩn hoặc app xuống nền (`Paused`/`Resumed`); các hệ nhận sự kiện vòng đời từ Composition Root; Idle là một kênh của pipeline ADR-0005.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Economy nghe vòng đời qua interface ở Contracts, phụ thuộc Bootstrap một chiều; số `double`.
- Forbidden: không `FindObjectOfType`/static singleton để đọc trạng thái; Core không tham chiếu `BootstrapController`.
- Guardrails: `Tick` 0 B GC; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: `Tick(double dt)` nhận delta từ composition root (không đọc `Time.deltaTime` trong Core); không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Boot/ISimulationState.cs` (`bool IsSimulationRunning`; Open Questions của EPIC: Contracts hiện chỉ có `IBootStatusReader`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`EconomyLifecycle.cs`, `EconomyRunState.cs`, `IdleAccrual.cs`, mở rộng `EconomyCore.cs`: `Tick`, `HandleInteractive`, `HandlePaused`, `HandleResumed`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/IdleAccrualTests.cs`, `EconomyLifecycleTests.cs`, `PausedResolveTests.cs`, `FakeSimulationState.cs`
- Data: `EconomyConfig.maxFrameDelta` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-21 [EditMode]` **GIVEN** `idle_cps` = 58, **WHEN** 60 bước dt = 1/60 s; một bước dt = 1.0 s; một bước dt âm hoặc NaN, **THEN** số dư tăng lần lượt 58, 58 × `C.maxFrameDelta` (14.5), 0.
- [ ] `AC-22 [EditMode]` **GIVEN** `idle_cps` > 0, bước dt = 1/60 s và Bootstrap giả điều khiển được `IsSimulationRunning`, **WHEN** (a) ngay sau `Run()` (`IsSimulationRunning` sai, vòng đời giả ở `Booting.Curtain`) chạy 600 bước, rồi phát `Interactive` (`IsSimulationRunning` đúng) và chạy 60 bước; (b) từ Running phát `Paused {Hidden}`, chạy 600 bước, rồi phát `Resumed {Hidden, 10}` với `IsSimulationRunning` đúng và chạy 60 bước; (c) trước `Interactive`, phát `Paused {Hidden}` (vòng đời giả ở `Booting.Curtain`), chạy 600 bước, phát `Resumed {Hidden, 10}` với `IsSimulationRunning` sai, chạy 600 bước, rồi phát `Interactive` và chạy 60 bước; (d) ở Running, Bootstrap giả đặt `IsSimulationRunning` sai mà không phát sự kiện nào, chạy 60 bước, **THEN** (a) 600 bước đầu: Economy Core ở Ready, số dư không đổi, không có `WalletChanged`; sau `Interactive`: Running, số dư tăng `idle_cps` × 1 s (sai số `T.valueRelTolerance`); (b) không có `WalletChanged` và số dư không đổi trong lúc tạm dừng; sau `Resumed`: Running, cộng Idle chạy lại ở bước kế tiếp, tổng tăng `idle_cps` × 1 s, không cộng gì cho `awaySeconds`; (c) sau `Resumed` Economy Core ở Ready (không phải Running), số dư không đổi trong cả 1,200 bước trước `Interactive`, và chỉ tăng (`idle_cps` × 1 s) trong 60 bước sau `Interactive`; (d) số dư không đổi (dt = 0, R6.2).
- [ ] `AC-49 [EditMode]` **GIVEN** Economy Core ở (a) Ready (sau `Run()`, chưa `Interactive`) hoặc (b) Paused (sau `Paused {Hidden}`), modifier đã biết, **WHEN** Plinko giả phát `CoinResolved` với `reason` = Overflow, `bucketIndex` = none, `rawValue` r, **THEN** cộng đúng `plinko_credit(r)`, phát đúng một `IncomeCredited` (`bucketIndex` = none, `reason` = Overflow), trạng thái giữ nguyên Ready / Paused và không có khoản Idle nào được cộng.

## Implementation Notes

- Trạng thái đúng bảng GDD: `Uninitialized → Ready` (sau `Initialize`/`Run`), `Ready → Running` khi nhận `Interactive` hoặc khi vào Ready mà `IsSimulationRunning` đã đúng (R16.4), `Running/Ready → Paused` khi nhận `Paused {reason}`, `Paused → Running` hoặc `Ready` khi nhận `Resumed` theo giá trị **đọc được** của `IsSimulationRunning` (không coi `Resumed` là "chạy lại"); `Interactive` khi đã Running không làm gì. `ResettingShop` là bước ngắn trong `ResetForNewShop` (Story 005), không cần trạng thái bền.
- `ISimulationState.IsSimulationRunning` đúng ⇔ vòng đời Bootstrap = `Running` (B6.5). Story này đặt interface ở Contracts và dùng trong Core; việc `AppLifecycleWatchdog`/`StartupCurtainController` cài đặt hoặc một adapter ở Runtime làm ở Story 018. Test dùng `FakeSimulationState` điều khiển cờ và phát sự kiện giả (`InteractiveEvent`, `PausedEvent`, `ResumedEvent` đã có ở Contracts/Events). Không dùng `awaySeconds` (thuộc Offline Earnings).
- `Tick(double dt)` (idle_accrual): cộng `idle_cps × min(dt, maxFrameDelta)` **chỉ khi** trạng thái Running **và** `IsSimulationRunning` đúng; mọi khung khác dt = 0. Đọc `IsSimulationRunning` mỗi khung là chốt chặn cuối kể cả khi một sự kiện vòng đời bị lỡ (AC-22 (d)). dt âm, NaN hoặc vô cực → 0 (Edge Case). Mỗi `Tick` có cộng phát đúng một `WalletChanged` (gộp tối đa một mỗi khung, R15); không cộng hoặc cộng 0 thì không phát sự kiện. Delta khung đầu sau khi vào Running vẫn bị kẹp bởi `maxFrameDelta`.
- `CoinResolved` và `SubmitIncome` được xử lý ở Ready, Running và Paused như nhau (R10.4, States): story này chỉ kiểm không có điều kiện trạng thái nào chặn handler của Story 011 và không có khoản Idle nào được cộng ở Ready/Paused. Ở Uninitialized, mọi API trả `false` và không xử lý `CoinResolved` (States, Uninitialized).
- Idle chỉ chạy khi Running; chuyển Tab 1/2/3 không dừng Idle (không có đầu vào Tab trong Economy). Thu nhập lúc tạm dừng thuộc Offline Earnings.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng (tách `EconomyLifecycle`, `IdleAccrual`), SRP, không static singleton, `Tick` không cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 014]: bù thời gian xem ad (`AdClosed`) và `ad_simulated_seconds`.
- [Story 015]: tốc độ công bố (`PlinkoRateAvg`, `IncomeRatesChanged`), đồng hồ `running_time`.
- [Story 018]: cài đặt `ISimulationState` bằng Bootstrap thật, gọi `Tick` từ composition root, bước boot.
- AC-23 (Play mode): xem Deferred acceptance criteria trong EPIC.

## QA Test Cases

- **AC-21**: kẹp delta khung
  - Given: `idle_cps` = 58, Running, `IsSimulationRunning` đúng
  - When: 60 bước dt = 1/60; một bước dt = 1.0; một bước dt âm; một bước dt NaN
  - Then: tăng 58; 58 × `maxFrameDelta` (14.5); 0; 0
  - Edge cases: dt +∞ → 0; `idle_cps` = 0 thì không phát `WalletChanged`
- **AC-22**: bốn kịch bản vòng đời
  - Given: Economy sau `Initialize`, nguồn Idle có `idle_cps` > 0, `FakeSimulationState` điều khiển được
  - When: chạy lần lượt (a), (b), (c), (d) đúng như AC
  - Then: (a) Ready, số dư không đổi 600 bước, sau `Interactive` tăng `idle_cps` × 1 s; (b) dừng không cộng, sau `Resumed` Running, tăng `idle_cps` × 1 s, không tính `awaySeconds`; (c) sau `Resumed` ở Ready, số dư không đổi 1,200 bước, sau `Interactive` tăng `idle_cps` × 1 s; (d) số dư không đổi
  - Edge cases: `Interactive` phát khi đã Running không đổi gì; vào Ready khi `IsSimulationRunning` đã đúng thì Running ngay
- **AC-49**: `CoinResolved(Overflow)` ở Ready/Paused
  - Given: Economy ở (a) Ready, (b) Paused; modifier đã biết
  - When: Plinko giả phát `CoinResolved` reason Overflow, `bucketIndex` none, rawValue r
  - Then: cộng đúng `plinko_credit(r)`, một `IncomeCredited` (`bucketIndex` none, reason Overflow), trạng thái giữ nguyên, không có khoản Idle nào
  - Edge cases: cùng kịch bản ở Uninitialized thì không xử lý

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/IdleAccrualTests.cs`, `EconomyLifecycleTests.cs`, `PausedResolveTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 004, Story 009, Story 011
- Unlocks: Story 010, Story 014, Story 015, Story 018
