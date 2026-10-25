# Story 003: Play Admission & Voice Pool Core

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A3 Nhận một yêu cầu phát, A4 Voice, A13 Đồng hồ, A14 Chẩn đoán, Formulas `cue_admitted`)
**Requirement**: `TR-audio-002`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001)
**ADR Decision Summary**: `AudioService` ở Foundation giữ nhóm 16 voice cấp sẵn; mỗi yêu cầu qua kiểm `minInterval`, `instanceCap`, rồi cấp voice; không tạo/hủy voice trong phiên.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: phát âm qua `IAudioService`; nhóm voice cố định 16; tách backend để kiểm thử.
- Forbidden: không `AudioSource` trong prefab; không static singleton; không tham chiếu tầng cao hơn.
- Guardrails: đúng 16 `AudioSource`, ≤ 0.5 ms CPU mỗi khung, 0 B GC khi `Play`.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: logic thuần, backend voice là interface; `AudioClip` chỉ là dữ liệu tham chiếu (EditMode tạo bằng `AudioClip.Create`).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Audio/` (`IAudioService.cs`, `IAudioClock.cs`, `IAudioDiagnosticsSink.cs`, `AudioDiagnosticCode.cs`, `AudioStats.cs`); `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`AudioService.cs`, `IVoiceBackend.cs`, `VoicePool.cs`, `VoiceSlot.cs`, `CueRuntimeTable.cs`, `PlayAdmission.cs`, `AudioRuntimeState.cs`, `AudioCounters.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/PlayAdmissionTests.cs`, `FakeVoiceBackend.cs`, `FakeAudioClock.cs`, `CountingAudioDiagnosticsSink.cs`
- Data: dùng asset Story 002

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-7 [EditMode]` **GIVEN** Audio chưa khởi tạo, **WHEN** gọi `Play(Plinko.CoinSpawn)` và `Play("Khong.Ton.Tai")` sau khi khởi tạo, **THEN** lần lượt `Dropped(NotReady)` và `Dropped(UnknownCue)`; không voice nào phát; không có exception.
- [ ] `AC-8 [EditMode]` **GIVEN** đồng hồ tiêm, **WHEN** yêu cầu `Plinko.PegHitScored` tại t = 0, t = `Q.minInterval` − ε, t = `Q.minInterval`, **THEN** kết quả lần lượt Played / `RateLimited` / Played, và yêu cầu bị bỏ không dời mốc.
- [ ] `AC-10 [EditMode]` **GIVEN** mỗi bước A3 được làm sai riêng lẻ (Locked, Suspended, UserMuted, BusSilent, BackgroundFiltered, RateLimited, InstanceCap, NoVoice), **WHEN** gửi yêu cầu, **THEN** lý do trả về đúng bước đó; khi nhiều bước cùng sai, lý do là bước có số thứ tự nhỏ nhất.
- [ ] `AC-29 [EditMode]` **GIVEN** time scale = 0 rồi = 2, **WHEN** lặp AC-8, **THEN** kết quả giống hệt (minInterval đo bằng đồng hồ thực).

## Implementation Notes

- `IAudioService` (Contracts) tạo ở story này với `PlayResult Play(string cueId, in AudioPlayParams p)` và `PlayResult Play(int cueIndex, in AudioPlayParams p)` kèm `bool TryResolveCue(string cueId, out int cueIndex)` (cueId tra sẵn thành chỉ số, GDD "Thành ADR"). Tham số dạng struct, không cấp phát. ADR-0004 phác `PlayCue(string, float, float)`; GDD (hợp đồng hành vi, `PlayResult`, `pitchPosition`/`panPosition`) thắng, ghi lệch vào Completion Notes. Các story sau thêm thành viên (`SetBusBackground`, `SetUserMuted`, `SetMuteReason`, ...).
- `IAudioClock` (Contracts): `double Now` (đồng hồ thực, không phụ thuộc `Time.timeScale`, A13); test dùng đồng hồ tiêm, test AC-29 đặt `Time.timeScale` 0 và 2 rồi khôi phục.
- `IVoiceBackend` (Foundation, không phải Contracts): số voice, `Start(voice, clip, volume, pitch, pan, loop)`, `Stop`, `Pause`, `Resume`, `SetVolume`, `IsPlaying`, `GetPosition`, `SpatialBlend` (luôn 0). `FakeVoiceBackend` trong test ghi lại clip, volume, pitch, pan, trạng thái từng voice; backend `AudioSource` thật ở Story 012.
- Trình tự `PlayAdmission` đúng thứ tự A3: NotReady → UnknownCue → Locked → Suspended → UserMuted (trừ cue bus Music) → BusSilent → BackgroundFiltered → RateLimited → InstanceCap/StealOldest → NoVoice. Bước đầu tiên sai quyết định lý do. Bước Locked/Suspended/UserMuted/BusSilent/BackgroundFiltered đọc cờ từ `AudioRuntimeState` (story này chỉ chứa cờ; các story 007–011 ghi cờ). AC-10 đặt cờ trực tiếp để làm sai từng bước.
- `RateLimited` không dời mốc `last_played`; mốc chỉ cập nhật khi `Played`. Cue `StealOldest` ở trần bản và `NoVoice` do hết voice: story này chỉ cấp voice rảnh, hết voice → `NoVoice`; luật cướp ở Story 004.
- `AudioService.Initialize(...)` nhận config, kho cue, backend, đồng hồ, sink; story này chỉ đưa hệ về Active (không WebGL khóa). Failed/Locked/Suspended ở Story 008–011. `Tick()` thu hồi voice có clip phát hết về Free.
- Voice: nhóm `totalVoices` cấp ở `Initialize`, `musicVoices` dành riêng cho bus Music (Story 004 kiểm tra phân vùng), không tạo thêm trong phiên.
- Chẩn đoán A14: `IAudioDiagnosticsSink` (Contracts) báo lỗi/cảnh báo (ví dụ `UnknownCue` trong Editor); đếm yêu cầu, `Played`, `Dropped` theo lý do qua `AudioStats` đọc không cấp phát. Bộ đếm cướp/đỉnh voice hoàn thiện ở Story 005. Không `Debug.Log*`.
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng (tách `PlayAdmission`, `VoicePool`, `CueRuntimeTable`), SRP, không static singleton, `Play` không cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 004]: cướp voice theo ưu tiên, `StealOldest`/`DropNew` ở trần bản, phân vùng voice nhạc (AC-9, AC-11, AC-12).
- [Story 005]: kịch bản bão hòa, đếm cướp và đỉnh voice (AC-13, AC-30).
- [Story 006]: pitch, chọn clip, pan.
- [Story 007]–[Story 011]: bus nền, tắt tiếng, tạm ngưng, Locked, boot step (chỉ dựng cờ trạng thái ở đây).
- [Story 012]: backend `AudioSource` thật, đồng hồ thật.

## QA Test Cases

- **AC-7**: NotReady / UnknownCue
  - Given: service chưa `Initialize`, rồi đã `Initialize`
  - When: `Play("Plinko.CoinSpawn")`, rồi `Play("Khong.Ton.Tai")`
  - Then: `Dropped(NotReady)` rồi `Dropped(UnknownCue)`; backend không có voice nào phát; không exception
  - Edge cases: cue lỗi dữ liệu (Story 001 đánh dấu) cũng `UnknownCue`; sink nhận cảnh báo
- **AC-8**: `minInterval`
  - Given: đồng hồ tiêm, `PegHitScored` (0.04 s)
  - When: yêu cầu tại 0, 0.04 − ε, 0.04
  - Then: Played / `RateLimited` / Played; mốc không dời bởi yêu cầu bị bỏ
  - Edge cases: hai yêu cầu cùng khung: yêu cầu trước Played, yêu cầu sau `RateLimited` (nếu `minInterval` > 0)
- **AC-10**: thứ tự lý do
  - Given: từng cờ trạng thái làm sai đúng một bước
  - When: gửi yêu cầu
  - Then: lý do đúng bước đó; nhiều bước cùng sai → bước số nhỏ nhất
  - Edge cases: `UserMuted` không áp cho cue bus Music
- **AC-29**: đồng hồ thực
  - Given: `Time.timeScale` = 0, rồi 2
  - When: lặp kịch bản AC-8
  - Then: kết quả giống hệt
  - Edge cases: khôi phục `Time.timeScale` sau test (teardown)

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/PlayAdmissionTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001, Story 002
- Unlocks: Story 004, Story 006, Story 007, Story 008
