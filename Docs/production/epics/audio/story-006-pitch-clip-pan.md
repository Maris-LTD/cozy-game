# Story 006: Pentatonic Pitch, Clip Selection & Pan

> **Epic**: Audio
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A6 Cao độ, A7 Pan, Formulas `note_index`, `sample_pitch`, Edge Cases "Tham số của yêu cầu")
**Requirement**: `TR-audio-002`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001)
**ADR Decision Summary**: Cue chạm đinh dùng thang ngũ cung với đổi pitch ±4 nửa cung từ bộ mẫu gốc để dòng xu nghe như marimba, không hỗn loạn.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: pitch/pan áp lên voice của pool qua `IAudioService`; tuning ở `SoundCue`/`AudioConfig`.
- Forbidden: không `AudioSource` trong prefab; không static singleton (`System.Random` tiêm qua interface, không `UnityEngine.Random` tĩnh trong logic).
- Guardrails: 0 B GC khi `Play`; pitch luôn > 0.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: WebGL chỉ nhận pitch dương trên nguồn phát; `panStereo` trên WebGL chưa xác minh (GDD A7): backend có cờ `SupportsPan`, nếu sai thì pan = 0 mà không đổi luật khác (xác minh thực ở AC-25, hoãn).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`IAudioRandom.cs`, `NoteIndexCalculator.cs`, `SamplePitchSelector.cs`, `CueClipSelector.cs`, `PanCalculator.cs`, `PitchResolver.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/PitchAndNoteTests.cs`, `ClipSelectionTests.cs`, `PanTests.cs`, `FixedAudioRandom.cs`
- Data: `AudioConfig.scaleRootMidi`, `scaleIntervals`, `scaleNoteCount`, `maxSampleShiftSemitones`, `fallbackPitchPosition`, `maxPan`; `SoundCue.pitchedSamples`

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [x] `AC-14 [EditMode]` **GIVEN** `note_index` với `C` mặc định, **WHEN** pitchPosition = 0, 1, 0.727, −0.5, 1.7, NaN, **THEN** n = 0, 9, 7, 0, 9, floor(`C.fallbackPitchPosition` × 9 + 0.5) (5); semitone(n) của n = 0…9 đúng dãy {0, 2, 4, 7, 9, 12, 14, 16, 19, 21}.
- [x] `AC-15 [EditMode]` **GIVEN** `Plinko.PegHitScored` và RNG giả trả 0, **WHEN** phát với n = 0…9, **THEN** mẫu được chọn và pitch khớp bảng `sample_pitch` (pitch = 2^(shift/12)), |shift| ≤ `C.maxSampleShiftSemitones`; **AND WHEN** phát 10 000 lần với RNG thật, **THEN** mọi pitch ∈ [2^(−`C.maxSampleShiftSemitones`/12) × (1 − `Q.pitchJitterRatio`), 2^(`C.maxSampleShiftSemitones`/12) × (1 + `Q.pitchJitterRatio`)] và > 0. Lặp cả hai vế với `Plinko.JackpotBurstSpawn` (mẫu chime 4/12/19, `Q.pitchJitterRatio` của chính cue): cùng bảng chọn mẫu và cùng giới hạn; với `burstIndex` 0…`jackpot_burst_count` − 1 (5 xu) thì n = 0, 2, 5, 7, 9.
- [x] `AC-16 [EditMode]` **GIVEN** cue `None`, cue `Random` có 3 clip và `avoidRepeat`, **WHEN** phát 1000 lần mỗi cue, **THEN** cue `None` luôn pitch = 1; cue `Random` có pitch ∈ [1 − `Q.pitchJitterRatio`, 1 + `Q.pitchJitterRatio`] và không có hai lần liên tiếp cùng clip.
- [x] `AC-17 [EditMode]` **GIVEN** cue có `panFromPosition`, **WHEN** panPosition = −1, 0, 1, 5, NaN, **THEN** pan = −`C.maxPan`, 0, `C.maxPan`, `C.maxPan`, 0; cue không có cờ luôn pan = 0; mọi voice có spatialBlend = 0.

## Implementation Notes

- `note_index`: `p = clamp(pitchPosition, 0, 1)` (NaN/vô cực/không truyền → `fallbackPitchPosition`), `n = floor(p × (scaleNoteCount − 1) + 0.5)`, `semitone(n) = 12 × floor(n / k) + scaleIntervals[n mod k]` (k = độ dài `scaleIntervals`), `midi(n) = scaleRootMidi + semitone(n)`.
- `sample_pitch`: `i*` = mẫu có `nativeSemitone` gần `semitone(n)` nhất (bằng nhau → mẫu thấp hơn); `shift = semitone(n) − nativeSemitone`; `pitch = 2^(shift/12) × (1 + U(−jitter, +jitter))`; bảng chọn mẫu mặc định theo GDD. Đồng thời dùng cho `JackpotBurstSpawn` với `pitchedSamples` của chính cue. Pitch cuối luôn > 0; kẹp tối thiểu một epsilon dương nếu cấu hình sai.
- A6.1–A6.2: `None` → pitch = 1; `Random` → `1 + U(−jitter, +jitter)`. A6.4: clip `None`/`Random` chọn ngẫu nhiên, `avoidRepeat` và cue có ≥ 2 clip thì không lặp clip vừa phát của cue đó (ghi nhớ chỉ số clip gần nhất mỗi cue); cue 1 clip bỏ qua cờ.
- RNG qua `IAudioRandom` (test dùng RNG giả trả 0 và RNG thật có seed; jitter U(−r, +r)); không dùng static random trong logic. Cấp phát 0 khi gọi.
- A7: `pan = clamp(maxPan × panPosition, −1, 1)` khi cue có `panFromPosition`, NaN/vô cực → 0; cue không có cờ pan = 0. `IVoiceBackend` (Story 003) giữ `SpatialBlend = 0` ở mọi voice (hằng số thiết kế); nếu `SupportsPan` = false thì pan áp = 0.
- `Play` (Story 003) gọi `PitchResolver` sau khi qua admission và trước `backend.Start`; cue không phải `Scale` bỏ qua `pitchPosition`.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP (mỗi tính toán một lớp), không static singleton, 0 cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 007]: `voice_gain` và âm lượng của voice.
- [Story 012]: áp pitch/pan thật lên `AudioSource`; kiểm tra pan trên WebGL (AC-25 hoãn).
- Bảng sự kiện Plinko → `cueId`/`pitchPosition` (do Feedback & Juice sở hữu); `jackpot_burst_count` chỉ là dữ liệu của test AC-15.

## QA Test Cases

- **AC-14**: `note_index`
  - Given: `AudioConfig` mặc định
  - When: pitchPosition = 0, 1, 0.727, −0.5, 1.7, NaN
  - Then: n = 0, 9, 7, 0, 9, 5; `semitone(0…9)` = {0, 2, 4, 7, 9, 12, 14, 16, 19, 21}
  - Edge cases: vô cực dùng `fallbackPitchPosition`; không truyền = NaN
- **AC-15**: chọn mẫu và pitch
  - Given: `PegHitScored` và `JackpotBurstSpawn`, RNG giả trả 0, rồi RNG thật
  - When: n = 0…9 (và `burstIndex` 0…4 → n = 0, 2, 5, 7, 9); 10 000 lần phát
  - Then: bảng mẫu/shift khớp GDD, |shift| ≤ 4; mọi pitch trong khoảng cho phép và > 0
  - Edge cases: khoảng cách tới hai mẫu bằng nhau chọn mẫu thấp hơn
- **AC-16**: `None`/`Random`
  - Given: cue `None`; cue `Random` 3 clip `avoidRepeat`
  - When: phát 1000 lần mỗi cue
  - Then: `None` pitch = 1; `Random` trong 1 ± jitter, không hai lần liên tiếp cùng clip
  - Edge cases: cue `Random` 1 clip vẫn phát (bỏ qua `avoidRepeat`)
- **AC-17**: pan
  - Given: cue có `panFromPosition`; cue không có cờ
  - When: panPosition = −1, 0, 1, 5, NaN
  - Then: −0.3, 0, 0.3, 0.3, 0; cue không cờ luôn 0; mọi voice `SpatialBlend` = 0
  - Edge cases: backend `SupportsPan` = false → pan áp = 0

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/PitchAndNoteTests.cs`, `ClipSelectionTests.cs`, `PanTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] note index, sample pitch, clip selection and pan tests under `Assets/CozyGachapon/Tests/EditMode/Audio/` (EditMode 471/471)

## Dependencies

- Depends on: Story 003
- Unlocks: Story 012

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 4/4 passing (AC-14, AC-15, AC-16, AC-17 + QA edge cases)
**Harness**: unity-harness all PASS (rules, compile, EditMode 471/471); smoke n/a (no scene change)
**Run result**: N/A - pitch/clip/pan applied to the fake voice backend
**Deviations**: ADVISORY - pan = `maxPan` x clamp(`panPosition`, -1, 1) to satisfy AC-17 (panPosition 5 -> maxPan); the Implementation Notes formula clamp(maxPan x panPosition, -1, 1) would give 1; new `IAudioRandom` (seeded real + fixed fake), `PitchResolver`, `CueClipSelector`, `PanCalculator`, reusing Story 002's `NoteIndexCalculator`/`SamplePitchSelector`
**Test Evidence**: note index / sample pitch / clip selection / pan tests in `Assets/CozyGachapon/Tests/EditMode/Audio/` (+ `FixedAudioRandom.cs`), passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review, 1 minor fixed (avoidRepeat first play could never pick the last clip) with test `AvoidRepeat_FirstPlay_CanPickTheLastClip`; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
