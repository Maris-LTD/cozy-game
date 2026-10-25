# Story 018: `economy.core` Boot Step, Composition Root Wiring & Zero-Alloc Frame

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`, nối dây ở `CozyGachapon.Runtime`)
> **Type**: Integration
> **Estimate**: 2 days
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R10.4, R13.3, R16 Khởi động qua hợp đồng pha, States "Uninitialized", Acceptance Criteria AC-43, AC-46)
**Requirement**: `TR-economy-001`, `TR-economy-002`, `TR-economy-005`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root (kèm ADR-0005, ADR-0001)
**ADR Decision Summary**: `BootstrapController` là composition root duy nhất; service tự đăng ký `IBootStep` ở pha `GameplayInit` (`Wallet`, `IncomePipeline` thuộc pha này); bước cốt lõi lỗi thì vào BootFailed, không crash im lặng.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: dịch vụ cần khởi tạo cài `IBootStep`; phụ thuộc truyền qua composition root; interface ở Contracts.
- Forbidden: không `FindObjectOfType`/`GameObject.Find`/static singleton; Foundation/Contracts không tham chiếu Core; Plinko không khai `RunsAfter` `economy.core`.
- Guardrails: 0 B GC mỗi khung; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: `Update` của `BootstrapController` gọi `EconomyWiring.Tick(Time.unscaledDeltaTime hoặc Time.deltaTime)`, chọn một và ghi Completion Notes (dt phải là delta khung mô phỏng; `maxFrameDelta` kẹp phần còn lại).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Economy/` (`EconomyBootStep.cs`, `EconomyBootModule.cs`); `Assets/CozyGachapon/Scripts/Contracts/Boot/` (`IServiceRegistry.cs`); `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/ServiceRegistry.cs`; `Assets/CozyGachapon/Scripts/Bootstrap/EconomyWiring.cs`, `SimulationStateAdapter.cs`; sửa `Assets/CozyGachapon/Scripts/Bootstrap/BootstrapController.cs` (thêm `RegisterEconomySteps`, gọi `Tick`); `Assets/CozyGachapon/Scripts/CozyGachapon.Runtime.asmdef` thêm `CozyGachapon.Core`
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/EconomyBootStepTests.cs`, `EconomyFrameAllocationTests.cs`, `FakeBootRegistry.cs`
- Data: `Assets/CozyGachapon/Data/Economy/EconomyConfig.asset` gán vào `BootstrapController` qua `[SerializeField]`

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-46 [EditMode]` **GIVEN** registry bước khởi động giả của Bootstrap, **WHEN** scene nạp và Economy Core đăng ký, **THEN** có đúng một bước `Id` = `economy.core`, `Phase` = `GameplayInit`, `RunsAfter` rỗng, `IsCritical` = đúng; trước `Run()` Economy Core ở Uninitialized, `Add`/`TrySpend` trả `false` và registry dịch vụ giả không có `IBaseCoinValueSource`; sau `Run()` bước báo `IsDone` trong cùng lời gọi, Economy Core ở **Ready** (không phải Running), `Add` và `TrySpend` (có ngữ cảnh hành động) hợp lệ, registry có đúng một `IBaseCoinValueSource` là Economy Core, và với một nguồn Idle có `idle_cps` > 0 chạy 60 bước khi Bootstrap giả giữ `IsSimulationRunning` sai thì số dư không đổi — Idle chỉ bắt đầu cộng sau khi Bootstrap giả phát `Interactive`; với `EconomyConfig` thiếu `currency.coin`, bước báo `IsFailed` và số dư không đổi.
- [ ] `AC-43 [Perf]` **GIVEN** trạng thái ổn định, Idle cộng mỗi khung, 24 `CoinResolved`/s, số dư và hai tốc độ được định dạng mỗi khung, **WHEN** ghi 600 khung bằng `ProfilerRecorder` "GC Allocated In Frame", **THEN** 0 B mỗi khung do Economy Core và bộ định dạng.

## Implementation Notes

- `EconomyBootStep : IBootStep`: `Id` = `economy.core`, `Phase` = `BootPhase.GameplayInit`, `RunsAfter` rỗng, `IsCritical` = true. `Run()` đọc `EconomyConfig`, gọi `EconomyCore.Initialize(config, ReleaseTier.Mvp, loaded)` (pha `SaveLoad` rỗng ở MVP nên `loaded` rỗng, game mới), tính giá quay và `baseCoinValue`, đăng ký `IBaseCoinValueSource` vào registry dịch vụ, rồi đặt `IsDone` trong cùng lời gọi; Economy vào **Ready**, không phải Running. Thiếu `EconomyConfig` hoặc `currencies` không có `currency.coin` → `IsFailed` (Bootstrap vào BootFailed, không xu nào được cộng, số dư không đổi). Nếu `IsSimulationRunning` đã đúng lúc vào Ready thì Running ngay (R16.4, Story 013).
- Registry dịch vụ: GDD/ADR chỉ nói "cơ chế đăng ký dịch vụ (→ ADR)" và hiện chưa có registry nào ở Contracts/Foundation (Open Questions của EPIC). Giả định tối thiểu: `IServiceRegistry { bool Register<T>(T service) where T : class; bool TryGet<T>(out T service) where T : class; }` ở Contracts (không logic), cài đặt `ServiceRegistry` ở Foundation, instance do `BootstrapController` giữ và truyền vào wiring (không static). Đăng ký `IBaseCoinValueSource` đúng một lần, giữ suốt phiên (kể cả Paused và ResettingShop). Plinko không khai `RunsAfter` `economy.core` (R16.2).
- `EconomyBootModule` (Core) mirror `AudioBootModule`: dựng `EconomyCore`, `EconomyBootStep`, hàm `RegisterSteps(Func<IBootStep, bool>)`, các `Handle*` cho sự kiện vòng đời. `EconomyWiring` (Runtime, mirror `AudioWiring`): `Register(BootstrapGraph graph)` gọi `RegisterStep`, nối `graph.Lifecycle.OnPaused`, `OnResumed`, `OnAdClosed`, `graph.Curtain.OnInteractive`; `Tick(dt)` gọi từ `BootstrapController.Update`. `SimulationStateAdapter : ISimulationState` đọc `graph.Lifecycle.IsSimulationRunning` và `IsAdOpen` (Foundation giữ nguyên, adapter nằm ở Runtime để không sửa Foundation Bootstrap).
- `CozyGachapon.Runtime.asmdef` thêm `CozyGachapon.Core` (Core không tham chiếu Runtime). Giữ đúng quy tắc tầng: Foundation/Contracts không tham chiếu Core.
- AC-43 [Perf]: GDD đo bằng `ProfilerRecorder` 600 khung trên kịch bản thật; ở EditMode dùng `GcAllocProbe` (`Assets/CozyGachapon/Tests/EditMode/Pooling/GcAllocProbe.cs`) cho một khung đại diện lặp 600 lần: `Tick(dt)` (Idle cộng mỗi khung), 24 `CoinResolved`/s (một sự kiện mỗi 2–3 khung), `TryFormatAmount` cho số dư và `TryFormatRate` cho hai tốc độ qua đường không cấp phát (Story 006), và đọc `IBaseCoinValueSource.BaseCoinValue`; kỳ vọng 0 allocation do Economy và formatter. Việc đo `ProfilerRecorder` trên scene thật là phần bổ sung nằm ở Deferred (AC-43 bản play mode cùng AC-44 trong EPIC).
- Test AC-46 dùng `FakeBootRegistry` (nhận `IBootStep` đã đăng ký, đếm bước `economy.core`), `IServiceRegistry` giả (đếm kiểu đã đăng ký), `FakeSimulationState` (Story 013) và một nguồn Idle có `idle_cps` > 0.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng (`BootstrapController` hiện đã dài: thêm vào qua `EconomyWiring`, không thêm logic trực tiếp), SRP, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 013]: cài đặt `ISimulationState` (interface), lifecycle Ready/Running/Paused (story này nối vào Bootstrap thật).
- [Story 006]: logic formatter (story này chỉ kiểm không cấp phát trong khung).
- Save/Load (VS): nạp `EconomyStateData` thật; ở MVP pha `SaveLoad` rỗng.
- Plinko Board, Gacha, Shelf, HUD (epic khác): bên dùng `IBaseCoinValueSource`, `TryPaySpin`, `IIncomePipeline`, `INumberFormatter` qua registry/composition root.
- AC-23, AC-34, AC-42, AC-44, AC-45: xem Deferred acceptance criteria trong EPIC.

## QA Test Cases

- **AC-46**: bước khởi động và Ready
  - Given: `FakeBootRegistry`, `IServiceRegistry` giả, `FakeSimulationState` (`IsSimulationRunning` sai), nguồn Idle có `idle_cps` > 0
  - When: dựng `EconomyBootModule`, đăng ký, kiểm trước `Run()`; gọi `Run()`; chạy 60 bước dt; phát `Interactive`; lặp với `EconomyConfig` thiếu `currency.coin`
  - Then: đúng một bước (`economy.core`, `GameplayInit`, `RunsAfter` rỗng, `IsCritical`); trước `Run()` Uninitialized, `Add`/`TrySpend` trả `false`, registry không có `IBaseCoinValueSource`; sau `Run()` `IsDone` ngay, trạng thái Ready, `Add` và `TrySpend` có ngữ cảnh hợp lệ, registry có đúng một `IBaseCoinValueSource` là Economy; 60 bước số dư không đổi, chỉ tăng sau `Interactive`; config thiếu `currency.coin` → `IsFailed`, số dư không đổi
  - Edge cases: `Run()` gọi hai lần không đăng ký hai lần; `EconomyConfig` null → `IsFailed`
- **AC-43**: khung không cấp phát
  - Given: trạng thái ổn định, Running, nguồn Idle, `GcAllocProbe`
  - When: lặp 600 khung gồm `Tick`, 24 `CoinResolved`/s, định dạng số dư và hai tốc độ, đọc `BaseCoinValue`
  - Then: 0 allocation do Economy và formatter trong mọi khung đo
  - Edge cases: khung có `IncomeRatesChanged` được phát vẫn 0 allocation; khởi động (khung đầu) được khởi động nóng trước khi đo, như `GcAllocProbe`

## Test Evidence

**Story Type**: Integration
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/EconomyBootStepTests.cs`, `EconomyFrameAllocationTests.cs`, passing in `unity-harness tests`; `unity-harness smoke` xác nhận boot không `BootFailed` khi `EconomyConfig` đã gán

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 006, Story 013, Story 014, Story 015, Story 017
- Unlocks: None (mở khóa epic Plinko Board, Gacha Machine, Shelf & Synergy, HUD, Feedback & Juice)
