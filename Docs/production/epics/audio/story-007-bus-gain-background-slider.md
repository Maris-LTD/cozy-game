# Story 007: Bus Gain, Background Bus & User Volume

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A1 Cây bus, A8 Bus ở chế độ nền, Formulas `voice_gain`, `slider_to_db`, `fade_factor`, Edge Cases "Tab, tạm ngưng, câm")
**Requirement**: `TR-audio-001`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001)
**ADR Decision Summary**: Gain tính phần mềm bằng tổng dB dọc cây bus (cue + bus + master), đổi sang tuyến tính một lần; không AudioMixer, không lọc.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: tính gain phân cấp theo bus trong `AudioService`; cue cấu hình bằng `SoundCue`.
- Forbidden: không AudioMixer/hiệu ứng mixer (ADR-0004); không static singleton.
- Guardrails: cập nhật gain voice đang phát mỗi `Tick` không cấp phát, ≤ 0.5 ms.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không dùng AudioMixer vì hỗ trợ WebGL một phần; `AudioSource.volume` được backend đặt từ gain đã tính.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`VoiceGainCalculator.cs`, `BusStateTable.cs`, `SliderMath.cs`, `BusBackgroundController.cs`, `VoiceGainUpdater.cs`); `Contracts/Audio/IAudioService.cs` thêm `SetBusBackground`, `SetUserVolume`
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/VoiceGainTests.cs`, `BusBackgroundTests.cs`, `SliderMathTests.cs`
- Data: `AudioConfig.busDefaultDb`, `plinkoBackgroundOffsetDb`, `busTransitionSeconds`, `userSliderMinDb`, `defaultUserSlider`

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-3 [EditMode]` **GIVEN** backend giả, `C` mặc định, **WHEN** phát `Plinko.BucketX5` khi bus Plinko Normal, rồi khi bus Plinko đã ở Background quá `C.busTransitionSeconds`, và phát `Music.ShopLoop`, **THEN** volume của voice lần lượt = db_to_linear(`Q[BucketX5].volumeDb`) (0.398), db_to_linear(`Q[BucketX5].volumeDb` + `C.plinkoBackgroundOffsetDb`) (0.100), db_to_linear(`Q[ShopLoop].volumeDb` + `C.busDefaultDb[Sfx]` + `C.musicOffsetFromSfxDb`) (0.251); một cue cấu hình +6 dB cho volume = 1 và validator cảnh báo.
- [ ] `AC-18 [EditMode]` **GIVEN** `SetBusBackground(Plinko, true)`, **WHEN** yêu cầu từng cue bus Plinko và một cue bus Gacha, Ui, **THEN** mọi cue có `keepWhenBusBackground = false` (PegHitScored, PegHitUnscored, CoinSpawn, CoinSpawnAuto, JackpotBurstSpawn, CoinFizzle, BoardBusy) → `Dropped(BackgroundFiltered)`; BucketX1/X2/X5 và JackpotHit được phát với âm lượng thấp hơn `C.plinkoBackgroundOffsetDb` so với khi Normal; cue Gacha/Ui không đổi âm lượng.
- [ ] `AC-19 [EditMode]` **GIVEN** một voice `PegHitScored` đang phát, **WHEN** gọi `SetBusBackground(Plinko, true)`, rồi `false` sau `C.busTransitionSeconds`, **THEN** voice không bị dừng, âm lượng giảm đúng `fade_factor` tới −12 dB sau `C.busTransitionSeconds`, rồi trở lại mức cũ sau thêm `C.busTransitionSeconds`; gọi `true` hai lần liên tiếp không đổi gì.
- [ ] `AC-28 [EditMode]` **GIVEN** thanh trượt (Alpha), **WHEN** s = 1, 0.75, 0.5, 0, NaN, 1.5, **THEN** user_db = 0, −10, −20, im (cue SFX trên bus → `Dropped(BusSilent)`, nhạc vẫn chạy câm), `slider_to_db(C.defaultUserSlider)`, 0.

## Implementation Notes

- `voice_gain`: `total_db = Q.volumeDb + Σ_{b ∈ path(Q.bus)} (bus_db(b) + user_db(b) + background_db(b))`; `volume = min(db_to_linear(total_db), 1) × fade_factor × user_mute_gain`. `bus_db(Music) = busDefaultDb[Sfx] + musicOffsetFromSfxDb`, bus khác = `busDefaultDb[b]`; `path` từ `AudioBusTable` (Story 001). `fade_factor` (resume) và `user_mute_gain` mặc định 1 ở story này; Story 008 nối giá trị thật.
- Âm lượng tính một lần lúc phát và cập nhật mỗi `Tick` cho voice đang phát khi bus nền / thanh trượt đang chuyển (để AC-19 thấy âm lượng giảm dần mà voice không dừng). Không cấp phát.
- `volumeDb` > 0 hoặc tổng dB > 0: volume kẹp ở 1, validator cảnh báo (không chặn): thêm mã cảnh báo vào validator Story 002 và test kiểm cả hai.
- Bus nền (A8): `SetBusBackground(bus, on)` chỉ hợp lệ cho Plinko ở v1.0 (bus khác bị bỏ qua). `background_db(Plinko) = plinkoBackgroundOffsetDb × fade_factor` (fade `busTransitionSeconds`, dùng `AudioFade` Story 001); gọi lại cùng trạng thái không làm gì; đảo chiều từ mức hiện tại. Bật nền: yêu cầu cue `keepWhenBusBackground = false` bị `BackgroundFiltered` (bước A3.7); tắt nền: cue nhận lại bình thường. Không lọc thông thấp. Audio không biết khái niệm tab: Feedback & Juice gọi `SetBusBackground(Plinko, TabChanged.current ≠ Board)`; khởi đầu Normal.
- `slider_to_db` (Alpha): `s ≤ 0` → bus im (`BusSilent`, A3.6), ngược lại `userSliderMinDb × (1 − s)`; `s` NaN → `defaultUserSlider`; `s > 1` → 1. `SetUserVolume(Music | Sfx, 0–1)` đổi `user_db` của hai bus. Lưu thanh trượt xuống kho thiết lập: Story 008. Nhạc vẫn phát câm khi slider Nhạc = 0 (không bị bỏ bằng `BusSilent` khi đang phát, chỉ yêu cầu mới).
- AC-3: `Music.ShopLoop` là cue mốc VS, không có trong bảng mặc định (Story 002): test tạo cue fixture 0 dB bus Music.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, tuning trong ScriptableObject, không static singleton, 0 cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 008]: `user_mute_gain`, fade tạm ngưng, lưu thanh trượt.
- [Story 011]: bắt đầu phát nhạc `Music.ShopLoop` thật.
- AC-20 [Play mode] (Feedback & Juice nối `TabChanged` trong scene thật) — thuộc Feedback & Juice; xem EPIC "Deferred acceptance criteria".
- AC-38 [Screenshot] (màn Settings) — thuộc HUD/Settings UI.

## QA Test Cases

- **AC-3**: gain theo bus
  - Given: backend giả, cấu hình mặc định, cue fixture `Music.ShopLoop` 0 dB
  - When: phát `BucketX5` (Normal), phát `BucketX5` khi đã Background quá 0.2 s, phát nhạc
  - Then: volume 0.398, 0.100, 0.251; cue +6 dB cho volume = 1 và validator cảnh báo
  - Edge cases: `silenceFloorDb` làm volume 0
- **AC-18**: lọc theo bus nền
  - Given: `SetBusBackground(Plinko, true)` đã chuyển xong
  - When: yêu cầu từng cue Plinko, một cue Gacha, một cue Ui
  - Then: bảy cue `keep = false` → `Dropped(BackgroundFiltered)`; `BucketX1/X2/X5`, `JackpotHit` Played thấp hơn 12 dB; Gacha/Ui nguyên mức
  - Edge cases: `SetBusBackground` cho bus ngoài Plinko không có tác dụng
- **AC-19**: chuyển bus mượt
  - Given: một voice `PegHitScored` đang phát
  - When: `true`, đến t + T `false`, đến t + 2T; gọi `true` hai lần
  - Then: voice không dừng; volume về −12 dB sau T rồi về mức cũ sau T nữa; lần gọi lặp không đổi gì
  - Edge cases: đảo chiều giữa chừng không nhảy âm lượng
- **AC-28**: thanh trượt
  - Given: `userSliderMinDb` = −40
  - When: s = 1, 0.75, 0.5, 0, NaN, 1.5
  - Then: 0, −10, −20 dB, im, `slider_to_db(defaultUserSlider)`, 0 dB; s = 0 → cue SFX `BusSilent`, nhạc đang phát vẫn chạy câm
  - Edge cases: s âm coi như 0

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/VoiceGainTests.cs`, `BusBackgroundTests.cs`, `SliderMathTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 002, Story 003
- Unlocks: Story 008
