# Story 011: WebGL Audio Unlock & Music Start

> **Epic**: Audio
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1.5 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A10 Mở khóa âm thanh WebGL, A11 Nhạc nền, States and Transitions "Locked", Edge Cases "Khởi tạo và nền tảng", "Nhạc (VS)")
**Requirement**: `TR-audio-003`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001, ADR-0002)
**ADR Decision Summary**: Trên WebGL Audio khởi tạo ở trạng thái khóa và chỉ mở khi nhận `FirstInteraction`; yêu cầu trước đó bị bỏ, không xếp hàng.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Audio context khởi tạo muted/suspended và mở bằng event `FirstInteraction`; nhạc dùng voice nhạc riêng.
- Forbidden: không phát tiếng trước cú chạm đầu trên WebGL; không cướp voice nhạc; không static singleton.
- Guardrails: 0 B GC; không phát bù cue trước khi mở khóa.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: `requiresUnlock` là hằng số theo nền tảng, tiêm vào service (`Application.platform == RuntimePlatform.WebGLPlayer` do composition root truyền, Story 012); hành vi thực của Web Audio trên Chrome/Safari chỉ xác minh ở AC-25 (hoãn).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`AudioUnlockController.cs`, `MusicStarter.cs`, `AudioPlatformPolicy.cs`, mở rộng `AudioRuntimeState.cs` với Locked)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioUnlockTests.cs`, `MusicStartTests.cs`
- Data: `AudioConfig.musicFadeInSeconds`, `musicVoices`; cue fixture `Music.ShopLoop` (không có trong bảng mặc định)

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [x] `AC-24 [EditMode]` **GIVEN** `requiresUnlock` = đúng, **WHEN** gửi `Play` trước rồi sau `FirstInteraction`, **THEN** trước → `Dropped(Locked)`, sau → Played; nếu `Background` bật trước `FirstInteraction` thì sau đó trạng thái là Suspended; với `requiresUnlock` = sai, Audio vào Active ngay khi khởi tạo.
- [x] `AC-42 [EditMode]` **GIVEN** `requiresUnlock` = đúng và Audio đã đăng ký bước nhưng chưa `Run()`, **WHEN** Bootstrap giả phát `FirstInteraction` rồi gọi `Run()`, **THEN** ngay sau khi `IsDone` trạng thái là Active (không phải Locked), `Play(Plinko.CoinSpawn)` → Played, và không có cue nào được phát bù cho khoảng trước khởi tạo.
- [x] `AC-26 [EditMode]` **GIVEN** cấu hình MVP không có clip nhạc, **WHEN** khởi tạo và chạy kịch bản bão hòa, **THEN** không lỗi, voice nhạc rảnh và không bị cấp cho SFX; **AND GIVEN** cấu hình VS có `Music.ShopLoop`, **WHEN** mở khóa trước rồi Bootstrap giả phát `Interactive` (và ở ca khác: `Interactive` trước rồi mới mở khóa), **THEN** nhạc chưa phát cho tới khi cả hai điều kiện đúng, rồi phát với loop = đúng, âm lượng tăng theo `fade_factor` trong `C.musicFadeInSeconds`, và không bị cướp trong suốt kịch bản bão hòa.

## Implementation Notes

- Locked (A10): `requiresUnlock` = đúng → sau `Run()` vào Locked; `HandleFirstInteraction(FirstInteractionEvent)` chuyển sang Active / UserMuted / Suspended theo lý do đang bật (`userMuted` đã lưu, `Background`, `Ad` có thể bật khi còn Locked: hệ vẫn Locked và sau mở khóa vào Suspended). Mọi `Play` khi Locked → `Dropped(Locked)` (bước A3.3), không xếp hàng. `requiresUnlock` = sai → Active ngay khi `Run()` xong.
- Ghi nhớ `FirstInteraction` đến khi còn Uninitialized: audio đăng nghe ngay lúc đăng ký bước (Story 010 module); `Run()` xong thì vào thẳng Active (hoặc UserMuted/Suspended) thay vì Locked; không phát bù bất kỳ cue nào của khoảng trước khởi tạo. Cùng cơ chế ghi nhớ cho `Interactive`.
- Nhạc (A11): MVP không có clip nhạc nên không phát gì, voice nhạc rảnh (không cấp cho SFX, Story 004). VS: khi cấu hình có `Music.ShopLoop`, nhạc bắt đầu khi **cả hai** điều kiện đúng: đã mở khóa (không Locked) và đã nhận `Interactive`; thứ tự hai điều kiện tùy ý. Phát trên voice nhạc với `loop = true`, âm lượng `fade_factor` tăng 0 → 1 trong `musicFadeInSeconds`, qua `voice_gain` (Story 007). Không bị cướp, không nhạc thích ứng. `HandleInteractive(InteractiveEvent)` là handler công khai (nối ở Story 012). Clip nhạc lỗi/chưa tải: không có nhạc, SFX không bị ảnh hưởng.
- Không bao giờ lùi từ Active về Locked trong phiên; `FirstInteraction` đến lần hai bị bỏ qua.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP (unlock tách khỏi nhạc), không static singleton, 0 cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 010]: bước `audio.service`, Failed.
- [Story 012]: nối `FirstInteractionEvent`/`InteractiveEvent` thật từ `BootstrapGraph`, `requiresUnlock` theo nền tảng thật.
- AC-25 [Play mode] (Chrome/Safari WebGL, chạm bàn Plinko thật) — cần bản build WebGL và Plinko Board; xem EPIC "Deferred acceptance criteria".
- Clip nhạc thật `Music.ShopLoop` và mua gói âm thanh (Vertical Slice).

## QA Test Cases

- **AC-24**: Locked / mở khóa
  - Given: `requiresUnlock` = true, rồi false
  - When: `Play` trước và sau `FirstInteraction`; ca riêng: bật `Background` rồi `FirstInteraction`
  - Then: `Locked` rồi Played; ca riêng Suspended; `requiresUnlock` = false → Active ngay sau `Run()`
  - Edge cases: `FirstInteraction` lần hai không đổi gì
- **AC-42**: `FirstInteraction` trước `Run()`
  - Given: `requiresUnlock` = true, bước đã đăng ký, chưa `Run()`
  - When: `HandleFirstInteraction`, rồi `Run()`
  - Then: sau `IsDone` trạng thái Active; `Play(CoinSpawn)` Played; không cue nào phát bù
  - Edge cases: `Background` bật trước `Run()` → sau `Run()` là Suspended
- **AC-26**: nhạc
  - Given: cấu hình MVP không clip nhạc; cấu hình VS có `Music.ShopLoop`
  - When: bão hòa (MVP); mở khóa rồi `Interactive`; ca khác `Interactive` rồi mở khóa; bão hòa (VS)
  - Then: MVP không lỗi, voice nhạc rảnh; VS nhạc chỉ phát khi đủ cả hai điều kiện, loop = true, âm lượng fade-in 2 s, không bị cướp
  - Edge cases: clip nhạc null → không nhạc, SFX bình thường

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioUnlockTests.cs`, `MusicStartTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `AudioUnlockTests.cs`, `MusicStartTests.cs` (EditMode 609/609)

## Dependencies

- Depends on: Story 009, Story 010
- Unlocks: Story 012

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 3/3 passing (AC-24, AC-42, AC-26 + QA edge cases)
**Harness**: unity-harness all PASS (rules, compile, EditMode 609/609); smoke n/a (wired in Story 012)
**Run result**: N/A - unlock and music start on the fake voice backend
**Deviations**: ADVISORY - `AudioUnlockController` + `AudioPlatformPolicy` (requiresUnlock injected) kept separate from `MusicStarter` + `MusicFadeIn`; music cue id `Music.ShopLoop` (VS) resolved at init, absent in MVP so the music voice stays free; `AudioBootModule.HandleInteractive` added; `AudioUnlockController.IsUnlocked` is read only by tests (minor, kept)
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioUnlockTests.cs`, `MusicStartTests.cs`, passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review, 2 minor: music fade is cancelled when the start is dropped (no refresh churn); test-only `IsUnlocked` kept; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
