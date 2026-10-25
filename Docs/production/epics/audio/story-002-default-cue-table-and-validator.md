# Story 002: Default Cue Table, Placeholder Clips & Config Validator

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A1, A2, Formulas `voice_budget`, `storm_peak_gain`, `cue_throughput`, Edge Cases "Dữ liệu cue", Tuning Knobs "Bảng cue v1.0", "Mục tiêu kiểm thử")
**Requirement**: `TR-audio-001` (AC-1, AC-5, AC-6)

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001)
**ADR Decision Summary**: Cue là `SoundCue` ScriptableObject dữ liệu hóa (dB, ưu tiên, trần bản, khoảng tối thiểu); cấu hình sai bị chặn bằng validator trước khi merge.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: cue cấu hình bằng `SoundCue` ScriptableObject; tuning ở ScriptableObject; 16 voice cố định.
- Forbidden: không số tuning trong logic; không static singleton; không tham chiếu tầng cao hơn.
- Guardrails: validator là logic thuần Foundation (không `UnityEditor`), chạy được trong EditMode.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: `AudioClip.length` dùng cho ràng buộc clip "tok"; clip placeholder sinh bằng script (WAV im lặng hoặc sóng sin biên độ thấp) để độ dài đúng ràng buộc.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`AudioBalanceTargets.cs`, `AudioConfigValidator.cs`, `AudioCueDataValidator.cs`, `AudioBudgetValidator.cs`, `AudioSpawnClipValidator.cs`, `AudioConfigIssue.cs`, `AudioConfigIssueCode.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/DefaultCueTableTests.cs`, `AudioConfigValidatorTests.cs`
- Data: `Assets/CozyGachapon/Data/Audio/AudioConfig.asset`, `AudioBalanceTargets.asset`, `Cues/*.asset`, `Clips/*.wav` (placeholder)

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-1 [EditMode]` **GIVEN** `C` mặc định, **WHEN** tra đường bus của mọi cue trong kho và mức dB hiệu dụng của từng bus, **THEN** mỗi cue có đúng một đường kết thúc ở Master, và dB(Music) − dB(Sfx) = `C.musicOffsetFromSfxDb` (−12).
- [ ] `AC-5 [EditMode]` **GIVEN** kho cue mặc định, **WHEN** chạy validator, **THEN** 0 lỗi, `storm_instances` (12) ≤ `C.totalVoices` − `C.musicVoices` − `C.ambienceVoices` − `C.minFreeVoicesForRewards` (12), `storm_peak_gain` (1.76) ≤ `T.stormPeakGainMax`, mọi cue `StealOldest` có `priority` ≤ `C.stormPriorityCeiling` thỏa `maxInstances` × `minInterval` ≥ `T.minStealAgeSeconds` (`PegHitScored` 0.24, `PegHitUnscored` 0.20 ≥ 0.20), `Plinko.CoinSpawn` và `Plinko.CoinSpawnAuto` là `DropNew` với max(`clip.length`) / (1 − `Q.pitchJitterRatio`) ≤ `T.manualSpawnClipMaxSeconds` (0.12) và `T.manualSpawnClipMaxSeconds` ≤ registry `tap_cooldown` − `T.spawnClipMarginSeconds` (0.15 − 0.03 = 0.12), và mọi cue `pitchMode = Scale` (`PegHitScored`, `JackpotBurstSpawn`) có `pitchedSamples` không rỗng với |shift| ≤ `C.maxSampleShiftSemitones` cho mọi n = 0…`C.scaleNoteCount` − 1.
- [ ] `AC-6 [EditMode]` **GIVEN** lần lượt từng cấu hình sai: cue 0 clip; `maxInstances` = 0; `minInterval` < 0; `priority` = 101; hai cue trùng `cueId`; `pitchedSamples` chỉ có mẫu trung (12); `Q[JackpotBurstSpawn].pitchedSamples` rỗng; tăng `Q[PegHitUnscored].maxInstances` thêm 1; `Q[PegHitScored].volumeDb` = 0; `Q[PegHitUnscored].minInterval` = 0.08; clip của `Plinko.CoinSpawn` dài `T.manualSpawnClipMaxSeconds` + 0.05 s; clip của `Plinko.CoinSpawnAuto` dài đúng `T.manualSpawnClipMaxSeconds` (0.12 s — hợp lệ nếu chỉ so `length`) với `Q[CoinSpawnAuto].pitchJitterRatio` (0.03), tức kêu thực 0.12 / 0.97 = 0.124 s; `T.manualSpawnClipMaxSeconds` > registry `tap_cooldown` − `T.spawnClipMarginSeconds` (ví dụ = `tap_cooldown`); `T.spawnClipMarginSeconds` ngoài safe range 0.017–0.05, **WHEN** chạy validator, **THEN** mỗi cấu hình sinh đúng lỗi tương ứng (lỗi đổi pitch nêu cue và nốt vi phạm; cue `Scale` thiếu mẫu nêu tên cue; lỗi ngân sách nêu `storm_instances` 13 > 12; lỗi biên độ nêu giá trị > `T.stormPeakGainMax`; lỗi steal_age nêu `PegHitUnscored` 0.16 < `T.minStealAgeSeconds`; lỗi clip spawn nêu tên cue, tên clip, `length` và thời gian kêu thực `length` / (1 − `pitchJitterRatio`) — ca `CoinSpawnAuto` 0.12 s phải bị bắt với 0.124 > 0.12; lỗi knob nêu `manualSpawnClipMaxSeconds` > `tap_cooldown` − `spawnClipMarginSeconds`; lỗi safe range nêu `spawnClipMarginSeconds`).

## Implementation Notes

- Bảng cue mặc định: đúng các dòng mốc MVP của "Bảng cue v1.0" (giá trị dB, ưu tiên, `maxInstances`, `minInterval`, capPolicy, pitchMode/jitter, keep khi bus nền, bus). KHÔNG tạo `Plinko.RainLoop` và `Music.ShopLoop` (mốc VS; RainLoop làm `storm_instances` = 13 > 12). Cue Gacha/UI là provisional (GDD): `Gacha.RarityReveal.<phẩm cấp>` tạo hai cue `common` và `rare` theo ví dụ AC-12 cho tới khi có GDD Gacha.
- Clip placeholder: sinh bằng script Editor/`Unity_RunCommand` thành WAV ngắn, ghi LICENSE/nguồn "placeholder generated" trong thư mục clip (GDD AU-R20: mỗi clip có nguồn, clip không rõ nguồn không vào build). Độ dài: clip "tok" của `CoinSpawn`/`CoinSpawnAuto` ≤ 0.116 s (ví dụ 0.10 s); 3 mẫu marimba và 3 mẫu chime ≤ `T.pegClipLengthMaxSeconds` (0.5 s); `pitchedSamples.nativeSemitone` = 4 / 12 / 19 cho cả `PegHitScored` lẫn `JackpotBurstSpawn`. Clip thật thay sau (ngoài phạm vi epic này).
- `AudioBalanceTargets` (ScriptableObject): toàn bộ knob mục "Mục tiêu kiểm thử". Không ảnh hưởng gameplay runtime.
- Validator trả danh sách `AudioConfigIssue` (mã lỗi, `cueId`, giá trị liên quan), không `Debug.Log`, không exception. Tách ba lớp: dữ liệu cue (0 clip, `maxInstances` < 1, `minInterval` < 0, `priority` ngoài 0–100, `cueId` trùng, cue `Scale` thiếu mẫu hoặc |shift| > `maxSampleShiftSemitones` cho mọi n), ngân sách (`voice_budget`, `storm_peak_gain`, `steal_age` Ràng buộc 1), clip spawn (Ràng buộc 2a/2b, safe range `spawnClipMarginSeconds`). `volumeDb` > 0 là cảnh báo, không chặn.
- `tap_cooldown` thuộc Plinko, Audio runtime không tham chiếu Plinko (A15): validator nhận `tapCooldownSeconds` là tham số (test truyền 0.15). Nguồn thật cho tham số này chờ `PlinkoConfig` (xem Out of Scope và mục câu hỏi mở của EPIC/báo cáo).
- AC-1 dùng `AudioBusTable` (Story 001) để dựng đường bus; dB hiệu dụng Music = `busDefaultDb[Sfx]` + `musicOffsetFromSfxDb`.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng (tách validator theo lớp), SRP, tuning trong ScriptableObject, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 007]: `voice_gain` và kiểm tra âm lượng thực của voice (AC-3).
- AC-35 [Perf] (tổng dung lượng nén clip) — cần build report WebGL và clip thật; xem EPIC "Deferred acceptance criteria".
- Nối `tapCooldownSeconds` với `PlinkoConfig` thật — chờ Plinko Board epic.

## QA Test Cases

- **AC-1**: đường bus
  - Given: `AudioConfig` mặc định và kho cue mặc định
  - When: dựng đường bus của từng cue và mức dB hiệu dụng từng bus
  - Then: mỗi cue một đường kết thúc ở Master; dB(Music) − dB(Sfx) = −12
  - Edge cases: cue không có bus hợp lệ coi là lỗi
- **AC-5**: validator trên kho mặc định
  - Given: asset mặc định, `tapCooldownSeconds` = 0.15
  - When: chạy validator
  - Then: 0 lỗi; `storm_instances` = 12 ≤ 12; `storm_peak_gain` ≈ 1.76 ≤ 2.0; steal_age 0.24 và 0.20; clip spawn 0.113 s ≤ 0.12; mọi cue `Scale` đủ mẫu, |shift| ≤ 4 cho n = 0…9
  - Edge cases: giá trị biên 12 ≤ 12 và 0.12 ≤ 0.12 phải hợp lệ (so `≤`)
- **AC-6**: các cấu hình sai
  - Given: bản sao cấu hình mặc định, mỗi lần sửa đúng một điểm theo danh sách AC
  - When: chạy validator
  - Then: mỗi điểm sinh đúng một lỗi mang đúng nội dung mô tả trong AC (cue, nốt, giá trị so sánh)
  - Edge cases: ca `CoinSpawnAuto` 0.12 s phải bị bắt (0.124 > 0.12); `volumeDb` = 0 của `PegHitScored` làm `storm_peak_gain` vượt trần

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/DefaultCueTableTests.cs`, `AudioConfigValidatorTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001
- Unlocks: Story 003, Story 007
