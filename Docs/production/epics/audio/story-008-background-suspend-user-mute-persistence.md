# Story 008: Background Suspend, User Mute & Settings Persistence

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A9 Tắt tiếng theo lý do, A12 Lưu thiết lập, States and Transitions, Formulas `fade_factor`)
**Requirement**: `TR-audio-004`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001, ADR-0002)
**ADR Decision Summary**: Tắt tiếng theo ngăn xếp lý do (`User`, `Ad`, `Background`); chỉ bật lại âm thanh khi mọi lý do đã được gỡ. Bootstrap phát `Paused`/`Resumed` khi tab ẩn hoặc app xuống nền.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: lý do tắt tiếng là tập độc lập; tạm ngưng dừng voice tại chỗ; Audio phụ thuộc Bootstrap một chiều (nhận event `Paused`/`Resumed` từ Contracts).
- Forbidden: Audio không đọc `IsAdOpen`, không tham chiếu Bootstrap; không static singleton.
- Guardrails: 0 B GC khi đổi lý do/fade.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: tạm dừng dùng `Pause`/`Resume` từng voice của backend; `AudioListener.pause` toàn cục không dùng (ADR chưa chốt, GDD yêu cầu dừng tại chỗ từng voice). Kho thiết lập cục bộ là `PlayerPrefs` (IndexedDB trên WebGL), xem Implementation Notes và câu hỏi mở.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`MuteReasonSet.cs`, `AudioSuspendController.cs`, `UserMuteController.cs`, `IAudioSettingsStore.cs`, `PlayerPrefsAudioSettingsStore.cs`, `AudioSettings` DTO nếu cần); `Contracts/Audio/IAudioService.cs` thêm `SetUserMuted`, `IsUserMuted`, `event MuteChanged`, `SetMuteReason`; `IAudioSettingsStore` đặt ở Contracts nếu Runtime cần dùng
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioSuspendTests.cs`, `UserMuteTests.cs`, `AudioSettingsPersistenceTests.cs`, `FakeAudioSettingsStore.cs`
- Data: `AudioConfig.resumeFadeInSeconds`, `userMuteFadeSeconds`, `defaultUserMuted`, `defaultUserSlider`

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-21 [EditMode]` **GIVEN** 3 voice SFX và voice nhạc đang phát, **WHEN** Bootstrap giả phát `Paused {reason = Hidden}`, chờ 60 s đồng hồ thực, rồi phát `Resumed {reason = Hidden, awaySeconds = 60}` (lặp lại với `reason = Background`), **THEN** trong lúc tạm ngưng mọi voice ở Paused giữ nguyên vị trí phát và mọi `Play` → `Dropped(Suspended)`; sau `Resumed` các voice tiếp tục từ vị trí cũ và âm lượng tăng theo `fade_factor` trong `C.resumeFadeInSeconds`.
- [ ] `AC-23 [EditMode]` **GIVEN** trạng thái Active với voice nhạc đang phát, **WHEN** `SetUserMuted(true)` rồi `SetUserMuted(false)`, **THEN** gain Master về 0 sau `C.userMuteFadeSeconds`; trong lúc câm `Play` SFX → `Dropped(UserMuted)` còn vị trí phát của nhạc tiếp tục tăng; sau khi bật lại gain về mức cũ sau `C.userMuteFadeSeconds` rồi `Ui.SoundOn` được phát đúng 1 lần; `MuteChanged` phát đúng 1 lần cho mỗi lần đổi.
- [ ] `AC-27 [EditMode]` **GIVEN** kho thiết lập giả rỗng, **WHEN** khởi tạo, **THEN** `IsUserMuted` = `C.defaultUserMuted` và hai thanh trượt = `C.defaultUserSlider`; **WHEN** `SetUserMuted(true)` rồi khởi tạo một Audio mới trên cùng kho, **THEN** `IsUserMuted` = đúng ngay sau khởi tạo, trước khi có `Play` nào được phát.

## Implementation Notes

- `MuteReasonSet` (cờ `MuteReason` của Contracts): bật/tắt từng lý do độc lập; nhóm tạm ngưng = `Background | Ad`; nhóm câm = `User`. Trạng thái dẫn xuất: có `Background`/`Ad` → Suspended; ngược lại `User` → UserMuted; ngược lại Active (kèm Locked/Failed ở Story 010–011). Hai cơ chế chồng nhau được (A9.3). Story này triển khai `Background` và `User`; hành vi riêng của `Ad` ở Story 009 nhưng tập lý do đã chứa đủ ba.
- Tạm ngưng (A9.1): `HandlePaused(PausedEvent)` bật `Background` cho cả `PauseReason.Hidden` và `PauseReason.Background`; `HandleResumed(ResumedEvent)` tắt `Background` (bỏ qua `awaySeconds`). Service chỉ nhận hai event Contracts này, không biết Bootstrap. Mất focus mà vẫn hiển thị không có `Paused` nên không đổi gì. Khi bật: mọi voice (kể cả nhạc) `Pause` giữ vị trí; mọi `Play` → `Dropped(Suspended)`. Khi lý do tạm ngưng cuối tắt: mọi voice `Resume` từ vị trí cũ, `fade_factor` 0 → 1 trong `resumeFadeInSeconds` (`AudioFade`, Story 001), nối vào `voice_gain` của Story 007. `minInterval` không bị ảnh hưởng (không cue nào nhận khi tạm ngưng, A13).
- Câm bởi người chơi (A9.2): `SetUserMuted(bool)` fade Master (`user_mute_gain`) trong `userMuteFadeSeconds`; nhạc chạy câm (vị trí tiếp tục trôi); SFX mới `UserMuted` (bước A3.5, không áp cue bus Music). Bật lại tiếng: Master về mức cũ trong `userMuteFadeSeconds` rồi phát `Ui.SoundOn` đúng một lần (`minInterval` của cue chặn spam). `MuteChanged` (event C# trong `IAudioService`) phát đúng một lần mỗi lần đổi; gọi cùng giá trị không phát.
- A12 lưu thiết lập: `IAudioSettingsStore` đọc/ghi `userMuted` và hai thanh trượt Nhạc/SFX; ghi ngay khi đổi, đọc khi `Initialize` trước khi bất kỳ cue nào có thể phát. Mặc định lần đầu từ `defaultUserMuted`/`defaultUserSlider`. Cài đặt mặc định `PlayerPrefsAudioSettingsStore` (key hằng số theo namespace); kho tách khỏi save tiến độ (Assumed GDD). Test dùng `FakeAudioSettingsStore`. Kho bị xóa → về mặc định, không ảnh hưởng tiến độ.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP (tách suspend, user mute, store), không static singleton, 0 cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 009]: lý do `Ad`, voice SFX dừng hẳn về Free khi `Ad` bật, kịch bản chồng lý do (AC-22, AC-43).
- [Story 010]: trạng thái Failed (API tắt tiếng vẫn ghi/đọc thiết lập, AC-32).
- [Story 011]: Locked, mở khóa WebGL.
- [Story 012]: nối `PausedEvent`/`ResumedEvent` thật từ `BootstrapGraph`.
- AC-37 [Screenshot] (icon loa ở header) — thuộc HUD & Tab Navigation; xem EPIC "Deferred acceptance criteria".

## QA Test Cases

- **AC-21**: tạm ngưng nền
  - Given: 3 voice SFX và voice nhạc đang phát, đồng hồ tiêm
  - When: `HandlePaused(Hidden)`, +60 s, `HandleResumed(Hidden, 60)`; lặp với `Background`
  - Then: voice Paused giữ vị trí; `Play` → `Suspended`; sau `Resumed` tiếp tục từ vị trí cũ, volume tăng theo `fade_factor` trong 0.3 s
  - Edge cases: `Resumed` không kèm `Paused` không đổi gì
- **AC-23**: câm bởi người chơi
  - Given: Active, voice nhạc đang phát
  - When: `SetUserMuted(true)`, chờ fade, `Play` SFX; `SetUserMuted(false)`
  - Then: gain Master 0 sau 0.15 s; SFX → `UserMuted`; vị trí nhạc vẫn tăng; sau bật lại gain về mức cũ rồi `Ui.SoundOn` Played đúng 1 lần; `MuteChanged` đúng 1 lần mỗi lần đổi
  - Edge cases: bấm liên tục đảo chiều fade từ mức hiện tại; `Ui.SoundOn` không spam (bị `minInterval`)
- **AC-27**: lưu thiết lập
  - Given: kho giả rỗng
  - When: khởi tạo; `SetUserMuted(true)`; khởi tạo Audio mới trên cùng kho
  - Then: lần đầu `IsUserMuted` = `defaultUserMuted`, hai thanh trượt = `defaultUserSlider`; lần hai `IsUserMuted` = true ngay sau khởi tạo, trước mọi `Play`
  - Edge cases: giá trị thanh trượt lưu rồi đọc lại (Story 007 `SetUserVolume`)

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioSuspendTests.cs`, `UserMuteTests.cs`, `AudioSettingsPersistenceTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 003, Story 007
- Unlocks: Story 009, Story 010
