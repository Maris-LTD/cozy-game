# Story 001: Audio Contracts, Config Assets, dB & Fade Math

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A1 Cây bus, A2 SoundCue, A13 Đồng hồ, Formulas `db_to_linear`, `fade_factor`, Tuning Knobs `AudioConfig` / `SoundCue`)
**Requirement**: `TR-audio-001`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001)
**ADR Decision Summary**: Âm lượng tính phần mềm theo cây bus phân cấp (không AudioMixer), cue là ScriptableObject `SoundCue`; interface và DTO ở `CozyGachapon.Contracts`, logic ở `CozyGachapon.Foundation`.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: mọi interface/enum/DTO ở `CozyGachapon.Contracts`; cue cấu hình bằng `SoundCue` ScriptableObject; asmdef phân tầng.
- Forbidden: không logic trong Contracts; không static singleton; không tham chiếu tầng cao hơn.
- Guardrails: 0 B GC mỗi khung; tra cue không cấp phát.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không dùng API sau cutoff; chỉ ScriptableObject và `UnityEngine.AudioClip` làm tham chiếu dữ liệu.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Audio/` (`AudioBus.cs`, `MuteReason.cs`, `DropReason.cs`, `PlayResult.cs`, `AudioPlayParams.cs`); `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`AudioConfig.cs`, `SoundCue.cs`, `SoundCueRepository.cs`, `AudioMath.cs`, `AudioFade.cs`, `AudioBusTable.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioMathTests.cs`, `AudioFadeTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `Assets/CozyGachapon/Data/Audio/` (asset mặc định tạo ở Story 002)

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-2 [EditMode]` **GIVEN** `db_to_linear`, **WHEN** đưa vào 0, −6, −12, `C.silenceFloorDb`, `C.silenceFloorDb` − 20, **THEN** nhận 1, 0.5012, 0.2512, 0, 0.
- [ ] `AC-4 [EditMode]` **GIVEN** `fade_factor` với T = 0 và T = `C.busTransitionSeconds`, **WHEN** lấy mẫu tại t0, t0 + T/4, t0 + T, t0 + 2T, **THEN** T = 0 cho 1 ở mọi mẫu (không chia cho 0); T > 0 cho 0, 0.25, 1, 1; đảo chiều fade ở t0 + T/4 bắt đầu từ 0.25, không nhảy.

## Implementation Notes

- Contracts (`CozyGachapon.Contracts`, chỉ enum/struct, không logic): `AudioBus` theo cây GDD A1 (`Master, Music, Ambience, Sfx, Gameplay, Plinko, Gacha, Ui`; ADR-0004 liệt kê thiếu `Ambience`, GDD thắng), `[Flags] MuteReason` (`None, User, Ad, Background`), `DropReason` (`NotReady, UnknownCue, Locked, Suspended, UserMuted, BusSilent, BackgroundFiltered, RateLimited, InstanceCap, NoVoice` — đúng thứ tự A3), `PlayResult` (readonly struct: `Played`, `Reason`), `AudioPlayParams` (readonly struct: `PitchPosition`, `PanPosition`; NaN = không truyền). `IAudioService` tạo ở Story 003, các story sau thêm thành viên.
- `SoundCue` (ScriptableObject): đúng các field A2: `cueId`, `bus`, `clips[]`, `volumeDb`, `priority`, `maxInstances`, `minInterval`, `instanceCapPolicy` (`StealOldest|DropNew`), `pitchMode` (`None|Random|Scale`), `pitchJitterRatio`, `keepWhenBusBackground`, `avoidRepeat`, `panFromPosition`, `loop`, `pitchedSamples[]` (`clip`, `nativeSemitone`). Field private + `[SerializeField]`, expose qua property chỉ đọc.
- `AudioConfig` (ScriptableObject): toàn bộ knob mục "Voice và bus", "Thang âm", "Thanh trượt người chơi" trong Tuning Knobs (giá trị mặc định theo bảng), gồm `silenceFloorDb`, `busDefaultDb[bus]`, `musicOffsetFromSfxDb`, các `*Seconds` fade, `maxPan`, `totalVoices`, `musicVoices`, `ambienceVoices`. Không con số nào trong logic.
- `SoundCueRepository`: dựng từ danh sách `SoundCue`, tra `cueId` → chỉ số bằng `Dictionary` dựng sẵn (không cấp phát khi tra); `cueId` trùng hoặc cue lỗi dữ liệu được đánh dấu không dùng được (→ `UnknownCue` khi chạy, Story 003). Validator đầy đủ ở Story 002.
- `AudioMath.DbToLinear(db, silenceFloorDb)`: `0` nếu `db ≤ silenceFloorDb`, ngược lại `10^(db/20)`; kết quả kẹp ở `voice_gain` (Story 007), không kẹp ở hàm này. Sai số so sánh 1e-4.
- `AudioFade` (struct, không cấp phát): `StartFade(from, target, duration, now)`, `Evaluate(now)`; fade bắt đầu từ giá trị hiện tại nên đảo chiều không nhảy; `T ≤ 0` cho giá trị đích tức thì, không chia cho 0. Dùng `double` giây từ đồng hồ tiêm (A13); đồng hồ `IAudioClock` tạo ở Story 003, ở story này truyền `double now` thẳng vào hàm.
- `AudioBusTable`: bảng cha của từng bus (Plinko → Gameplay → Sfx → Master; Music → Master; Ambience → Master; Ui → Sfx) dùng cho `voice_gain` ở Story 007 và AC-1 ở Story 002.
- Quy tắc dự án: namespace `CozyGachapon.Audio` (Foundation) / `CozyGachapon.Contracts`; không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, tuning trong ScriptableObject, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 002]: asset mặc định (`AudioConfig`, bảng cue, clip placeholder), validator, AC-1, AC-5, AC-6.
- [Story 003]: `IAudioService`, nhận yêu cầu `Play`, `IAudioClock`, voice backend.
- [Story 007]: `voice_gain`, `slider_to_db`, bus nền.

## QA Test Cases

- **AC-2**: `db_to_linear`
  - Given: `silenceFloorDb` = −80
  - When: `DbToLinear(0 / −6 / −12 / −80 / −100)`
  - Then: 1, 0.5012, 0.2512, 0, 0 (sai số 1e-4)
  - Edge cases: dB > 0 trả > 1 (không kẹp ở hàm này); `silenceFloorDb` đổi trong safe range −100…−60 vẫn cho 0 ở đúng ngưỡng
- **AC-4**: `fade_factor`
  - Given: đồng hồ là tham số `now`, T = 0 rồi T = `busTransitionSeconds`
  - When: lấy mẫu tại t0, t0 + T/4, t0 + T, t0 + 2T
  - Then: T = 0 → 1 ở mọi mẫu; T > 0 → 0, 0.25, 1, 1; đảo chiều tại t0 + T/4 bắt đầu từ 0.25
  - Edge cases: T âm coi như 0; fade đảo chiều nhiều lần liên tiếp không nhảy giá trị

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioMathTests.cs`, `AudioFadeTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: None (Bootstrap epic đã Complete)
- Unlocks: Story 002, Story 003
