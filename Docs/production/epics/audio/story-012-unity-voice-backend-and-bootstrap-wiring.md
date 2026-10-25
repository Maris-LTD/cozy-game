# Story 012: Unity Voice Backend & Bootstrap Wiring

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: 2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A4 Voice, A13 Đồng hồ, A16 Bước khởi động, Interactions "Bootstrap & Scene Flow", Acceptance Criteria preamble "backend voice giả")
**Requirement**: `AC-36` (hiện thực backend thật cho `TR-audio-002`; nối `TR-audio-003`/`TR-audio-004` vào Bootstrap)

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001, ADR-0002)
**ADR Decision Summary**: 16 `AudioSource` cấp sẵn lúc `ServiceInit` do `AudioService` quản; `BootstrapController` là Composition Root duy nhất, truyền sự kiện vòng đời cho Audio.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: khởi tạo qua `BootstrapController`; đúng 16 `AudioSource` tạo lúc `ServiceInit`; adapter MonoBehaviour ở `CozyGachapon.Runtime`.
- Forbidden: không `AudioSource` trong prefab của hệ khác; không `FindObjectOfType`/`GameObject.Find`/static singleton; không tạo/hủy `AudioSource` sau `ServiceInit`.
- Guardrails: ≤ 0.5 ms CPU mỗi khung; 0 B GC mỗi khung.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: `AudioSource.Pause/UnPause` giữ vị trí; `panStereo` và `Pause` trên WebGL chỉ xác minh ở AC-25 (hoãn). Mặc định `SupportsPan` = true, chưa có xác minh cho WebGL.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`UnityVoiceBackend.cs`, `UnityAudioClock.cs`, `AudioDiagnosticsRecorder.cs`); `Assets/CozyGachapon/Scripts/Bootstrap/` (`AudioTickDriver.cs` hoặc `AudioWiring.cs` MonoBehaviour; sửa `BootstrapController.cs` tối thiểu, tách nếu vượt 250 dòng)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/UnityVoiceBackendTests.cs` (tạo backend trong EditMode, kiểm số `AudioSource` và thuộc tính); Play mode qua `unity-harness smoke`
- Data: `AudioConfig.asset`, kho cue (Story 002); `BootstrapController` gán `[SerializeField]` cho config và kho cue
- Scene / prefab: `Assets/CozyGachapon/Scenes/` MainGame — thêm container `AudioVoices` (GameObject rỗng), gán config vào `BootstrapController`; không `AudioSource` nằm sẵn trong scene/prefab

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-36 [Play mode]` **GIVEN** backend thật trong Play mode, **WHEN** `Play` trả Played, **THEN** nguồn phát tương ứng báo đang phát trong cùng khung (trễ ≤ `T.requestToStartMaxFrames` khung).

## Implementation Notes

- `UnityVoiceBackend : IVoiceBackend`: tạo đúng `totalVoices` `AudioSource` (một lần, `AddComponent` trên container do composition root truyền vào, `playOnAwake = false`, `spatialBlend = 0`) lúc `Run()` của `audio.service` (pha `ServiceInit`); không tạo/hủy sau đó. `Start` đặt clip, `volume`, `pitch` (> 0), `panStereo` (nếu `SupportsPan`), `loop` rồi `Play()`; `Pause`/`Resume` dùng `Pause()`/`UnPause()`; `IsPlaying` và `GetPosition` (`time`) đọc không cấp phát.
- `UnityAudioClock : IAudioClock`: `Time.realtimeSinceStartupAsDouble` (không phụ thuộc time scale, A13). `requiresUnlock` = `Application.platform == RuntimePlatform.WebGLPlayer`, do composition root tính và truyền vào service.
- `AudioDiagnosticsRecorder : IAudioDiagnosticsSink`: ghi nhận mã lỗi/cảnh báo, không `Debug.Log*` (mẫu `PoolDiagnosticsRecorder`).
- Nối vào `BootstrapController` (Composition Root): đăng ký `AudioBootModule.RegisterSteps(Graph.RegisterStep)` trong lúc scene nạp trước khung đầu (như `RegisterPoolingSteps`); nối `Graph.Lifecycle.OnPaused/OnResumed` → `HandlePaused/HandleResumed`, `Graph.FirstInteraction.OnFirstInteraction` → `HandleFirstInteraction`, `Graph.Curtain.OnInteractive` → `HandleInteractive` ngay lúc đăng ký (Audio phải ghi nhớ `FirstInteraction` đến sớm); huỷ đăng ký khi `OnDestroy`. Một MonoBehaviour nhỏ gọi `AudioService.Tick()` mỗi khung (thu hồi voice hết clip, cập nhật gain/fade); không `GetComponent` trong `Update`.
- Phạm vi nối: chỉ visibility `Paused/Resumed` của Bootstrap. `SetMuteReason(Ad, …)` do Platform Services gọi (Alpha), `SetUserMuted` do HUD, `SetBusBackground` do Feedback & Juice: giữ API công khai trong `IAudioService`, không nối ở đây. Dịch vụ `IAudioService` phơi qua composition root cho hệ khách sau này (không static singleton).
- Cấu hình thiếu (`AudioConfig` chưa gán) → bước `audio.service` thất bại, game chạy không tiếng (Story 010): không phá boot.
- AC-36: kiểm tra trong Play mode (smoke hoặc test Play mode tối thiểu): sau mở khóa, gọi `Play("Plinko.CoinSpawn")` bằng clip placeholder và khẳng định `AudioSource.isPlaying` của voice được cấp trong cùng khung. Ghi `playtest.md` với kết quả và console không lỗi. Kiểm tra số `AudioSource` = `totalVoices` bằng EditMode.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, không static singleton, 0 cấp phát khi `Play`.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- AC-25 [Play mode] (WebGL Chrome/Safari), AC-33/AC-34/AC-35 [Perf] — thiết bị/build WebGL; xem EPIC "Deferred acceptance criteria".
- Gọi `Play`/`SetBusBackground` từ Plinko/Feedback, `SetMuteReason(Ad)` từ Platform Services, nút loa HUD, thanh trượt Settings: các epic đó.
- Clip âm thanh thật (CC0/thương mại): đầu vào ngoài; dùng clip placeholder (Story 002).
- Xác minh `panStereo`/`Pause` trên WebGL thật.

## QA Test Cases

- **AC-36**: backend thật phát cùng khung
  - Setup: Play mode, MainGame, bước `audio.service` Done, đã `HandleFirstInteraction` (hoặc nền tảng không cần mở khóa)
  - Verify: gọi `Play("Plinko.CoinSpawn")` và đọc `isPlaying` của voice được cấp trong cùng khung; đếm `AudioSource` dưới container
  - Pass condition: `Played` và `isPlaying` = true trong cùng khung (trễ ≤ `requestToStartMaxFrames` = 0); số `AudioSource` = `totalVoices`; console không lỗi âm thanh

## Test Evidence

**Story Type**: Integration
**Required evidence**: một EditMode/PlayMode test, hoặc playtest note `Docs/production/qa/evidence/audio/story-012-unity-voice-backend-and-bootstrap-wiring/playtest.md`; `unity-harness smoke` pass

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 006, Story 010, Story 011
- Unlocks: None
