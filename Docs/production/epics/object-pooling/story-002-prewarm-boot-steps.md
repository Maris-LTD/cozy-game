# Story 002: Prewarm & Boot Steps

> **Epic**: Object Pooling
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/object-pooling.md` (R4 Prewarm, States and Transitions "Trạng thái của dịch vụ" và "Free", Formulas `prewarm_frames`, Tuning Knobs `prewarmPerFrame`, `prewarmMaxSeconds`, Interactions "Bootstrap & Scene Flow")
**Requirement**: `TR-pooling-001`

**ADR Governing Implementation**: ADR-0003: Zero-Allocation Object Pooling (kèm ADR-0001, ADR-0002)
**ADR Decision Summary**: Ở pha `Prewarm`, `pooling.prewarm` tạo mọi pool đủ 100% dung lượng rồi tắt đối tượng; sau Ready cấm `Instantiate`/`Destroy`. Bootstrap chỉ biết `IBootStep`.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: hệ cần khởi tạo dùng `IBootStep`; pool prewarm 100% ở `Prewarm`; đối tượng pool nằm dưới container GameObject trong scene.
- Forbidden: không `Instantiate`/`Destroy` ngoài pha `Prewarm`; không static singleton; không tham chiếu tầng cao hơn.
- Guardrails: boot + prewarm nằm trong 2.0 s first-frame-to-interactive; Pooling dùng ≤ `prewarmMaxSeconds`.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: `Object.Instantiate` chỉ được gọi trong bước prewarm; tắt đối tượng bằng `SetActive(false)` hoặc tắt renderer + `Rigidbody2D.simulated` (ADR D1 để ngỏ, chọn cách thỏa AC-9 và ghi vào Completion Notes).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Pooling/`: `PoolServiceState.cs`, `PoolPrewarmer.cs`, `PoolingServiceBootStep.cs`, `PoolingPrewarmBootStep.cs`, `PoolContainer.cs`; đăng ký vào `BootstrapGraph` (`Assets/CozyGachapon/Scripts/Foundation/Bootstrap/BootstrapGraph.cs`); contract dùng `IBootStep`, `BootPhase` ở `Assets/CozyGachapon/Scripts/Contracts/Boot/`
- Test: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolPrewarmTests.cs`, `PoolBootStepTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `PoolingConfig` (`prewarmPerFrame`, `prewarmMaxSeconds`); `targetFps` = 60 đọc từ hằng cấu hình studio, không thuộc Pooling

## Acceptance Criteria

*From Docs/design/gdd/object-pooling.md, scoped to this story:*

- [x] `AC-6 [EditMode]` **GIVEN** các pool đã đăng ký với tổng dung lượng `N` và `P.prewarmPerFrame` = `k`, **WHEN** chạy từng bước prewarm (mỗi bước = một khung), **THEN** mỗi bước tạo ≤ `k` đối tượng, số bước = ceil(`N` / `k`), và sau bước cuối dịch vụ Ready với `created` = `capacity` ở mọi pool; kiểm với (`N`, `k`) = (40, 10) → 4 bước, (41, 10) → 5 bước, (40, 40) → 1 bước, (40, 4) → 10 bước.
- [x] `AC-7 [EditMode]` **GIVEN** `P.prewarmPerFrame` = 0, **WHEN** chạy validator và prewarm, **THEN** validator báo lỗi, prewarm vẫn kết thúc sau `N` bước (dùng 1) và không ném exception.
- [x] `AC-8 [EditMode]` **GIVEN** cấu hình có ceil(`N` / `P.prewarmPerFrame`) / `targetFps` > `P.prewarmMaxSeconds` (ví dụ `N` = 128, `k` = 4 → 0.53 s > 0.25 s), **WHEN** chạy validator, **THEN** có cảnh báo; với cấu hình MVP thật của project (`N` = `registered_capacity_total` = 36 + `F`, `k` = `P.prewarmPerFrame`; ví dụ `F` = 4 → (40, 10) → 0.067 s) thì không có cảnh báo.
- [x] `AC-9 [EditMode]` **GIVEN** prewarm xong, **WHEN** đọc mọi đối tượng ở trạng thái Free, **THEN** không đối tượng nào có renderer đang hiển thị hay Rigidbody2D đang mô phỏng.
- [x] `AC-30 [EditMode]` **GIVEN** registry bước khởi động giả của Bootstrap và dịch vụ Pooling vừa được nạp, **WHEN** đọc các bước Pooling đã đăng ký rồi chạy khởi động với các bước `GameplayInit` giả mỗi bước đăng ký 1 pool, **THEN** Pooling đăng ký đúng hai bước — `pooling.service` (pha `ServiceInit`, `IsCritical` = true) và `pooling.prewarm` (pha `Prewarm`, `IsCritical` = true); mọi đăng ký pool được chấp nhận trước `Run()` của `pooling.prewarm`; `pooling.prewarm` báo `IsDone` = false cho tới khi dịch vụ Ready và = true ngay khi Ready; Pooling không gọi API nào của Bootstrap ngoài hợp đồng `IBootStep` (khớp Bootstrap AC-7).

## Implementation Notes

- Namespace `CozyGachapon.Pooling` (hoặc con) cho mọi kiểu Pooling (GDD AC-32).
- Vòng đời dịch vụ: `Accepting` → `Prewarming` → `Ready` (enum `PoolServiceState`). `Run()` của `pooling.service` tạo dịch vụ mới ở Accepting với sổ đăng ký rỗng; `Run()` của `pooling.prewarm` đóng đăng ký, chuyển Prewarming và dựng danh sách việc prewarm theo thứ tự đăng ký.
- Hai bước `IBootStep` đều `IsCritical` = true: `PoolingServiceBootStep` (`Id` = `pooling.service`, `Phase` = `ServiceInit`, `IsDone` đúng sau `Run()`) và `PoolingPrewarmBootStep` (`Id` = `pooling.prewarm`, `Phase` = `Prewarm`; `IsDone` = dịch vụ Ready; `IsFailed` khi prewarm lỗi). Đăng ký cả hai vào `BootstrapGraph`; Pooling không gọi API Bootstrap nào ngoài `IBootStep` (khớp Bootstrap AC-7). `RunsAfter` theo hợp đồng hiện có.
- Prewarm theo khung: một hàm `Step()` tạo tối đa `k` = `max(1, prewarmPerFrame)` đối tượng (`k` < 1 → dùng 1, không chia 0, không exception), gọi mỗi khung bởi bộ đẩy của bước `pooling.prewarm` (bộ đẩy do composition root cấp, thuần C#, test gọi `Step()` trực tiếp). Số bước = ceil(`N` / `k`). Không thu thập `Time` hay timer (Pooling không phụ thuộc thời gian).
- Mỗi đối tượng tạo xong được đưa về Free ngay (AC-9): không hiển thị, không mô phỏng; đặt dưới container GameObject riêng theo pool (ADR guideline). `created` tăng theo từng đối tượng.
- Validator (mở rộng `PoolingConfigValidator` của Story 001): `prewarmPerFrame` < 1 → lỗi; ceil(`N` / `k`) / `targetFps` > `prewarmMaxSeconds` → cảnh báo; không ghi log, trả danh sách kết quả cho nơi gọi. Cấu hình MVP thật chỉ kiểm khi các pool MVP có đủ prefab; trước đó test dùng cấu hình dựng tay có số khớp (36 + `F`).
- Đây là đường duy nhất được `Instantiate` của dịch vụ; các story sau không thêm `Instantiate`/`Destroy`.
- Dự án: không comment, không `Debug.Log*` (lỗi/cảnh báo đi qua `IPoolDiagnosticsSink` của Story 001), không LINQ, file ≤ 250 dòng.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 001]: Đăng ký pool và công thức dung lượng.
- [Story 003]: Get / Release (Get khi dịch vụ chưa Ready, AC-15, thuộc Story 003).
- [Story 006]: Hủy dịch vụ khi nạp lại scene (AC-31).
- AC-10 [Perf]: đo prewarm trên WebGL thiết bị tham chiếu — hoãn tới đợt perf sau.
- AC-33 [Play mode]: kịch bản "Thử lại" trên scene thật — cần Plinko Board, hoãn.

## QA Test Cases

- **AC-6**: số bước prewarm
  - Given: dịch vụ Accepting với các pool tổng `N`, prefab nhẹ (SpriteRenderer + Rigidbody2D)
  - When: chạy `Step()` tới khi Ready, ghi số đối tượng tạo mỗi bước, với (`N`, `k`) = (40, 10), (41, 10), (40, 40), (40, 4)
  - Then: mỗi bước tạo ≤ `k`; số bước = 4, 5, 1, 10; Ready sau bước cuối; `created` = `capacity` ở mọi pool
  - Edge cases: bước cuối của (41, 10) chỉ tạo 1 đối tượng; thứ tự tạo theo thứ tự đăng ký
- **AC-7**: `prewarmPerFrame` = 0
  - Given: `PoolingConfig` test với `prewarmPerFrame` = 0
  - When: chạy validator rồi prewarm
  - Then: validator có một lỗi; prewarm kết thúc sau `N` bước (mỗi bước 1 đối tượng); không exception
  - Edge cases: giá trị âm cũng dùng 1
- **AC-8**: ngân sách prewarm
  - Given: (a) `N` = 128, `k` = 4, `prewarmMaxSeconds` = 0.25; (b) (`N`, `k`) = (40, 10), `targetFps` = 60
  - When: chạy validator
  - Then: (a) có cảnh báo (0.53 s > 0.25 s); (b) không cảnh báo (0.067 s)
  - Edge cases: đúng bằng ngưỡng không cảnh báo
- **AC-9**: Free ở trạng thái ẩn
  - Given: prewarm xong
  - When: duyệt mọi đối tượng Free của mọi pool
  - Then: không renderer nào `enabled` hiển thị (hoặc GameObject inactive) và không `Rigidbody2D.simulated` = true
  - Edge cases: prefab có nhiều renderer con
- **AC-30**: hai bước Boot
  - Given: registry bước giả của Bootstrap và các bước `GameplayInit` giả mỗi bước đăng ký 1 pool
  - When: nạp dịch vụ, đọc bước đã đăng ký, chạy khởi động
  - Then: đúng hai bước `pooling.service` (`ServiceInit`, critical) và `pooling.prewarm` (`Prewarm`, critical); đăng ký pool đều được chấp nhận trước `Run()` của `pooling.prewarm`; `IsDone` = false tới khi Ready rồi = true ngay khi Ready; không gọi API Bootstrap ngoài `IBootStep`
  - Edge cases: đăng ký sau `Run()` của `pooling.prewarm` bị từ chối (đã có ở AC-4)

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolPrewarmTests.cs`, `PoolBootStepTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `PoolPrewarmTests.cs`, `PoolPrewarmValidatorTests.cs`, `PoolFreeStateTests.cs`, `PoolingBootStepTests.cs` (EditMode 208/208)

## Dependencies

- Depends on: Story 001
- Unlocks: Story 003

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 5/5 passing (AC-6, AC-7, AC-8, AC-9, AC-30 covered by EditMode tests)
**Harness**: unity-harness all PASS (rules, compile, EditMode 208/208); smoke PASS (MainGame, 0 console errors)
**Run result**: OBSERVED - smoke boots MainGame through `pooling.service` / `pooling.prewarm` with 0 console errors; no pools registered yet in MVP scene, so prewarm completes in one tick
**Deviations**: ADVISORY - required test file named `PoolingBootStepTests.cs` instead of `PoolBootStepTests.cs`; validator tests split into `PoolPrewarmValidatorTests.cs`; target FPS for the prewarm budget read from `BootConfig.TargetFps` (60); scene gains a `Pools` container referenced by `BootstrapController`
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolPrewarmTests.cs`, `PoolPrewarmValidatorTests.cs`, `PoolFreeStateTests.cs`, `PoolingBootStepTests.cs` (+ `PoolingBootFakes.cs`), passing
**Visual sign-off**: n/a (Logic)
**Code Review**: inline review after verify; fixed hardcoded `StudioTargets.TargetFps` constant (moved to BootConfig) and stripped TMP serialization noise from the scene; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
