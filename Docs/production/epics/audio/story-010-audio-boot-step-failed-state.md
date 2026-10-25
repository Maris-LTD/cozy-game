# Story 010: `audio.service` Boot Step & Failed State

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A15 Ranh giới, A16 Bước khởi động, States and Transitions "Uninitialized", "Failed", Edge Cases "Khởi tạo và nền tảng")
**Requirement**: `AC-41, AC-32, AC-31` (khởi tạo `ServiceInit` theo ADR-0004 Ordering Note và ADR-0002)

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001, ADR-0002)
**ADR Decision Summary**: Audio khởi tạo ở pha `ServiceInit` qua `IBootStep`; Audio phụ thuộc Bootstrap một chiều; Bootstrap bỏ qua bước không cốt lõi bị lỗi và vẫn phát `BootCompleted`.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: dịch vụ nền cần khởi tạo phải cài `IBootStep`; khởi tạo chỉ qua `BootstrapController` (Composition Root).
- Forbidden: Bootstrap không tham chiếu Audio; không tham chiếu Plinko/Gacha/Shelf/Feedback; không static singleton.
- Guardrails: không ném exception ra khỏi `Run()`.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có; mẫu theo `PoolingBootModule`/`PoolingServiceBootStep` trong `Foundation/Pooling/`.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`AudioServiceBootStep.cs`, `AudioBootModule.cs`, mở rộng `AudioRuntimeState.cs` với Uninitialized/Failed)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioBootStepTests.cs`, `AudioBoundaryTests.cs`, `FakeBootStepRegistry.cs`
- Data: `AudioConfig`, kho `SoundCue`

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-41 [EditMode]` **GIVEN** sổ đăng ký `IBootStep` giả của Bootstrap, **WHEN** Audio đăng ký, **THEN** có đúng 1 bước của Audio với `Id` = `audio.service`, `Phase` = `ServiceInit`, `RunsAfter` rỗng, `IsCritical` = sai; trước `Run()` mọi `Play` → `Dropped(NotReady)`; sau `Run()` với `C` hợp lệ thì `IsDone` = đúng, `IsFailed` = sai và số nguồn phát = `C.totalVoices`; với `AudioConfig` thiếu thì `IsFailed` = đúng, trạng thái Failed, không exception lọt ra ngoài `Run()`; assembly của Bootstrap không tham chiếu assembly của Audio.
- [ ] `AC-32 [EditMode]` **GIVEN** thiếu `AudioConfig`, **WHEN** khởi tạo rồi gọi `Play`, `SetUserMuted(true)`, **THEN** trạng thái Failed, `Play` → `Dropped(NotReady)` không exception, `IsUserMuted` = đúng và giá trị được lưu.
- [ ] `AC-31 [EditMode]` **GIVEN** assembly definition của Audio, **WHEN** đọc danh sách tham chiếu, **THEN** không có assembly nào của Plinko, Gacha, Shelf, Feedback & Juice (Audio không nghe sự kiện gameplay, D1).

## Implementation Notes

- `AudioServiceBootStep : IBootStep`: `Id` = `"audio.service"`, `Phase` = `BootPhase.ServiceInit`, `RunsAfter` rỗng, `IsCritical` = false. `Run()` gọi một lần: đọc `AudioConfig` và kho `SoundCue`, đọc thiết lập người chơi (A12, `IAudioSettingsStore` Story 008), tạo sẵn `totalVoices` voice qua backend (A4), rồi `IsDone`; hệ rời Uninitialized sang Active (hoặc Locked, Story 011). Thiếu config, không tạo được voice, ngoại lệ, hoặc quá hạn bước của Bootstrap (`BootConfig.stepTimeoutSeconds`, Bootstrap tự xử lý) → `IsFailed`, Audio vào Failed; `Run()` bắt mọi exception, tuyệt đối không ném ra ngoài, và báo qua `IAudioDiagnosticsSink`.
- Uninitialized: mọi `Play` → `NotReady`; ghi nhớ `FirstInteraction` nếu đến (xử lý ở Story 011). Failed: không thoát trong phiên, mọi `Play` → `NotReady`; `SetUserMuted`, `IsUserMuted`, lưu/đọc thiết lập vẫn hoạt động để lần sau đúng (cần đọc thiết lập kể cả khi `AudioConfig` thiếu: dùng mặc định cứng chỉ cho giá trị `false`/lưu trữ, không đọc `AudioConfig`).
- `AudioBootModule` theo mẫu `PoolingBootModule`: `RegisterSteps(Func<IBootStep, bool> register)`; đăng ký đúng một bước. Audio đăng nghe `FirstInteraction`/`Paused`/`Resumed`/`Interactive` ngay lúc đăng ký (A16.1): module phơi handler công khai (`HandleFirstInteraction`, `HandlePaused`, `HandleResumed`, `HandleInteractive`) để composition root nối; nối thật ở Story 012.
- Ranh giới (A15, AC-31): code Audio sống trong `CozyGachapon.Foundation` (namespace `CozyGachapon.Audio`). Test đọc `CozyGachapon.Foundation.asmdef` (không tham chiếu assembly Plinko/Gacha/Shelf/Feedback) và kiểm bằng reflection namespace `CozyGachapon.Audio` không tham chiếu kiểu thuộc gameplay, namespace Bootstrap không tham chiếu kiểu Audio (mẫu `PoolNamespaceBoundaryTests`). Hiện chưa có assembly Plinko/Gacha/Shelf/Feedback nên điều kiện thỏa tầm thường; test giữ làm bảo vệ về sau.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, không static singleton, tuning trong ScriptableObject.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 011]: Locked, `FirstInteraction`, ghi nhớ `FirstInteraction` trước `Run()` (AC-42), `Interactive`.
- [Story 012]: đăng ký module thật trong `BootstrapController`, backend `AudioSource`, nối event.
- Hợp đồng `IBootStep` và Bootstrap: epic Bootstrap đã Complete, không sửa ở đây.

## QA Test Cases

- **AC-41**: đăng ký và `Run()`
  - Given: sổ đăng ký `IBootStep` giả; `AudioConfig` hợp lệ, rồi thiếu
  - When: `RegisterSteps`; `Play` trước `Run()`; `Run()`
  - Then: đúng 1 bước (`audio.service`, `ServiceInit`, `RunsAfter` rỗng, không cốt lõi); `Play` → `NotReady` trước `Run()`; sau `Run()` hợp lệ `IsDone`, số nguồn = `totalVoices`; config thiếu → `IsFailed`, Failed, không exception; Bootstrap không tham chiếu Audio
  - Edge cases: `Run()` gọi hai lần không tạo thêm voice
- **AC-32**: Failed vẫn lưu thiết lập
  - Given: thiếu `AudioConfig`
  - When: khởi tạo; `Play`; `SetUserMuted(true)`
  - Then: Failed; `Dropped(NotReady)` không exception; `IsUserMuted` = true và đã lưu vào kho giả
  - Edge cases: khởi tạo lại trên cùng kho (phiên mới) đọc đúng `true`
- **AC-31**: ranh giới assembly
  - Given: `CozyGachapon.Foundation.asmdef` và các kiểu `CozyGachapon.Audio`
  - When: đọc danh sách tham chiếu và quét tham chiếu kiểu
  - Then: không có Plinko/Gacha/Shelf/Feedback & Juice
  - Edge cases: namespace Bootstrap không tham chiếu kiểu Audio

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioBootStepTests.cs`, `AudioBoundaryTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 008
- Unlocks: Story 011, Story 012
