# Story 014: Ad Idle Compensation & Wallet Event Integrity

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R6.4, R14 Bản đồ faucet/sink, R15 Sự kiện công bố, Formulas `ad_idle_compensation`, Edge Cases "hàng chục xu một khung")
**Requirement**: `TR-economy-001`, `TR-economy-002`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format (kèm ADR-0002)
**ADR Decision Summary**: mọi thay đổi số dư đi qua ví với lý do rõ ràng; `AdClosed` là sự kiện vòng đời do Bootstrap phát, Economy tự quyết bù Idle.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: mọi thay đổi số dư qua `IWallet` với `reason`; số `double`; vòng đời nhận qua Contracts.
- Forbidden: không bù kênh Plinko; không cộng hai lần; không static singleton.
- Guardrails: 0 B GC trên đường `Tick`/`AdClosed`; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Economy/` (`AdIdleCompensator.cs`, `AdSimulatedTimeTracker.cs`, `WalletChangeReasonValidator.cs`, mở rộng `EconomyCore.cs`: `HandleAdClosed`; `ISimulationState` thêm `IsAdOpen` hoặc interface riêng, xem Implementation Notes)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/AdIdleCompensationTests.cs`, `WalletEventIntegrityTests.cs`, `RecordingWalletEvents.cs`
- Data: `EconomyConfig.adCompensationMaxSeconds` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-48 [EditMode]` **GIVEN** Running, `idle_cps` = 58, `C.adCompensationMaxSeconds` (90), **WHEN** `AdClosed` đến: (a) `{30}` sau khi cờ ad giả bật mà không có khung nào chạy; (b) `{30}` sau 1,800 bước dt = 1/60 s với cờ ad bật; (c) `{200}` như (a); (d) `{30}` khi `IsSimulationRunning` sai (Ready); (e) `{NaN}` và `{−5}`, **THEN** phần bù (ngoài 58 × giây đã cộng ở `idle_accrual`) lần lượt = 1,740 (một `WalletChanged` với `reason` = AdIdleCompensation), 0 (tổng tăng đúng 58 × 30, không cộng đôi), 5,220, 0, 0; `PlinkoRateAvg` và mọi modifier Plinko không đổi.
- [ ] `AC-39 [EditMode]` **GIVEN** phiên kịch bản 60 s (Idle, 200 xu Plinko, 3 lượt quay, 1 lần `SpendRejected`), **WHEN** ghi mọi `WalletChanged`, **THEN** mỗi sự kiện có delta = current − previous và `reason` thuộc bảng faucet/sink R14; Idle phát ≤ 1 `WalletChanged` mỗi khung; tổng delta trong mỗi khung = Σ `creditedAmount` + phần Idle − chi tiêu của khung đó.

## Implementation Notes

- `ad_idle_compensation = idle_cps × min(max(sanitize(ad_seconds) − ad_simulated_seconds, 0), adCompensationMaxSeconds)` (R6.4). Chỉ khi `AdClosed {adSeconds}` đến lúc `IsSimulationRunning` đúng; sai, hoặc bù = 0, hoặc `adSeconds` NaN/âm → bỏ qua (không ví, không sự kiện). Cộng bằng `Add(currency.coin, ..., AdIdleCompensation)` phát đúng một `WalletChanged`. Chỉ kênh Idle: không đụng `PlinkoRateAvg` và modifier Plinko.
- `AdSimulatedTimeTracker`: trong lúc cờ ad bật (`IsAdOpen` đúng), `Tick` của Story 013 vẫn cộng Idle như thường và Economy cộng dồn `ad_simulated_seconds` = Σ `min(dt, maxFrameDelta)` đã cộng; về 0 sau mỗi `AdClosed`. Phần delta bị `maxFrameDelta` cắt không được bù ở đường nào khác. Kịch bản (b): 1,800 bước dt = 1/60 (30 s) → mô phỏng đủ 30 s → bù 0, tổng tăng đúng 58 × 30. Kịch bản (a)/(c): không khung nào chạy → bù 58 × 30 = 1,740 và 58 × min(200, 90) = 5,220.
- `IsAdOpen`: Bootstrap có `AppLifecycleWatchdog.IsAdOpen` nhưng Contracts chưa có interface; thêm `bool IsAdOpen` vào `ISimulationState` (Story 013) hoặc interface hẹp cạnh nó (chọn một, ghi Completion Notes); cài đặt thật ở Story 018. `AdClosedEvent` đã có ở `Contracts/Events`.
- Offline Earnings (VS) cũng nghe `AdClosed`; GDD Offline không được bù lại cùng `adSeconds` (ngoài epic).
- AC-39: Idle gộp tối đa một `WalletChanged` mỗi khung (Story 013), mỗi xu Plinko có `WalletChanged` riêng cùng lời gọi (Story 011); test dựng kịch bản 60 s bằng Plinko giả, nguồn Idle, ba `TryPaySpin`, một lần thiếu tiền, ghi mọi sự kiện: `delta = current − previous`, `reason` thuộc tập R14 (kiểm bằng hàm `WalletChangeReasonValidator` trên danh sách `WalletChangeReason`, không chuỗi tự do), tổng delta từng khung = Σ `creditedAmount` + phần Idle − chi tiêu của khung đó.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, không static singleton, không cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 013]: `Tick`/`idle_accrual` và trạng thái (story này mở rộng, không viết lại).
- [Story 015]: `PlinkoRateAvg`, `IncomeRatesChanged` (AC-48 chỉ kiểm chúng không đổi).
- [Story 018]: kết nối `AdClosedEvent` và `IsAdOpen` thật từ `AppLifecycleWatchdog`.
- Rewarded Ads, Offline Earnings: bên phát/nghe `AdClosed` khác (ngoài epic).

## QA Test Cases

- **AC-48**: bù thời gian xem ad
  - Given: Running, `idle_cps` = 58, `adCompensationMaxSeconds` = 90, cờ ad giả
  - When: (a) bật cờ ad rồi `AdClosed {30}` không khung nào; (b) bật cờ, 1,800 bước dt = 1/60, `AdClosed {30}`; (c) như (a) với `{200}`; (d) Ready với `{30}`; (e) `{NaN}` và `{−5}`
  - Then: bù 1,740 (một `WalletChanged` reason AdIdleCompensation); 0 (tổng tăng 58 × 30); 5,220; 0; 0; `PlinkoRateAvg` và modifier Plinko không đổi
  - Edge cases: bước giật dt = 1.0 trong lúc ad: phần bị cắt không được bù; `ad_simulated_seconds` về 0 sau `AdClosed`
- **AC-39**: toàn vẹn sự kiện ví
  - Given: kịch bản 60 s gồm Idle, 200 xu Plinko giả, 3 lượt `TryPaySpin`, một lần thiếu tiền
  - When: ghi mọi `WalletChanged` và `IncomeCredited` theo khung
  - Then: delta = current − previous; mọi `reason` thuộc tập R14; Idle ≤ 1 `WalletChanged` mỗi khung; tổng delta mỗi khung = Σ `creditedAmount` + phần Idle − chi tiêu
  - Edge cases: khung có cả Idle, Plinko và chi tiêu; khung không có biến động không phát sự kiện

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/AdIdleCompensationTests.cs`, `WalletEventIntegrityTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 005, Story 011, Story 013
- Unlocks: Story 018
