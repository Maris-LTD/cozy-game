# Story 009: Ad Mute Reason & Reason Stacking

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A9 hàng `Ad`, A9.1 "Riêng `Ad`", States and Transitions, Edge Cases "rewarded ad đang mở")
**Requirement**: `TR-audio-004`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001)
**ADR Decision Summary**: `SetMuteReason(MuteReason.Ad, bool)` là cách duy nhất Audio biết về quảng cáo; âm thanh chỉ trở lại khi mọi lý do đã gỡ.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: lý do tắt tiếng độc lập; Audio chỉ đổi lý do `Ad` qua `SetMuteReason`.
- Forbidden: Audio không đọc `IsAdOpen`, không nghe sự kiện quảng cáo của nền tảng; không static singleton.
- Guardrails: 0 B GC; không phát bù tiếng của khoảng quảng cáo.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có; voice SFX dừng hẳn (về Free) nên không phụ thuộc hành vi `AudioSource` khi tab ẩn.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`AdMuteHandler.cs`, mở rộng `AudioSuspendController.cs`, `VoicePool.cs` thêm `StopAllSfx`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/AdMuteReasonTests.cs`
- Data: `AudioConfig.resumeFadeInSeconds`, `AudioBalanceTargets.stormTestSeconds`

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-22 [EditMode]` **GIVEN** trạng thái Active, **WHEN** lần lượt: bật `Ad`, bật `Background`, tắt `Ad`, tắt `Background`; và riêng một ca: cửa sổ mất focus mà không có `Paused`, **THEN** trạng thái lần lượt Suspended, Suspended, Suspended, Active; ca mất focus giữ Active.
- [ ] `AC-43 [EditMode]` (Alpha; Bootstrap B) **GIVEN** Audio Active, 3 voice SFX và voice nhạc đang phát, Bootstrap giả **không** phát `Paused`, **WHEN** gọi `SetMuteReason(Ad, true)`, gửi `Play(Plinko.PegHitScored)` và `Play(Plinko.JackpotHit)`, cho đồng hồ tiêm trôi `T.stormTestSeconds` (dữ liệu fixture: 3 voice SFX, 2 yêu cầu), rồi gọi `SetMuteReason(Ad, false)`, **THEN** ngay khi `Ad` bật trạng thái là Suspended, 3 voice SFX ở Free (không giữ vị trí) và voice nhạc ở Paused giữ vị trí; hai `Play` → `Dropped(Suspended)`; sau khi tắt `Ad` trạng thái Active, voice nhạc tiếp tục từ vị trí cũ với âm lượng theo `fade_factor` trong `C.resumeFadeInSeconds`, số voice SFX đang phát = 0 (không phát bù) và `Play` kế tiếp → Played.

## Implementation Notes

- `SetMuteReason(MuteReason reason, bool isActive)` (`IAudioService`, Story 008 đã thêm) xử lý `Ad` đầy đủ: bật `Ad` → vào Suspended; mọi voice SFX (Playing hoặc Paused) `Stop` và về Free (không giữ vị trí vì mô phỏng chạy tiếp, đuôi tiếng cũ sẽ lệch bàn); voice nhạc `Pause` giữ vị trí. Mọi `Play` → `Dropped(Suspended)`, không xếp hàng, không phát bù khi đóng (kể cả `JackpotHit`).
- Tắt `Ad`: chỉ rời Suspended khi không còn `Background`/`Ad` nào; khi rời, voice nhạc `Resume` từ vị trí cũ với fade-in `resumeFadeInSeconds`; số voice SFX đang phát bằng 0; `Play` kế tiếp bình thường.
- Chồng lý do (A9.3): `Ad` + `Background` + `User` độc lập; quảng cáo kết thúc trong khi tab vẫn ẩn → vẫn Suspended; về tab khi đang câm → UserMuted. `SetMuteReason` với `User` ủy cho `SetUserMuted` của Story 008 (một đường duy nhất, `MuteChanged` không phát hai lần).
- Ca "mất focus mà không có `Paused`": service không có đầu vào focus nên không đổi trạng thái; test khẳng định bằng cách không gọi `HandlePaused`.
- Thứ tự gọi khi quảng cáo đóng trong lúc tab ẩn (Assumed, Platform Services): `SetAdOpen(false)` trước `SetMuteReason(Ad, false)`; sai thứ tự chỉ gây tối đa một khung Active ở fade 0, không cần xử lý đặc biệt.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, không static singleton, 0 cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 008]: tạm ngưng `Background`, câm `User`, lưu thiết lập, `MuteChanged`.
- Platform Services / Rewarded Ads gọi `SetMuteReason(Ad, bool)` đúng thời điểm (Alpha) — thuộc Platform Services; xem EPIC "Deferred acceptance criteria".

## QA Test Cases

- **AC-22**: chồng lý do
  - Given: Active
  - When: bật `Ad`, bật `Background`, tắt `Ad`, tắt `Background`; ca riêng: không có `Paused`
  - Then: Suspended, Suspended, Suspended, Active; ca mất focus giữ Active
  - Edge cases: bật `User` giữa chừng → sau khi tắt hết lý do tạm ngưng trạng thái là UserMuted
- **AC-43**: quảng cáo mở
  - Given: Active, 3 voice SFX + voice nhạc đang phát, không `Paused`
  - When: `SetMuteReason(Ad, true)`; `Play(PegHitScored)`, `Play(JackpotHit)`; trôi `stormTestSeconds`; `SetMuteReason(Ad, false)`
  - Then: Suspended, 3 voice SFX Free, voice nhạc Paused giữ vị trí; hai `Play` → `Suspended`; sau tắt `Ad`: Active, nhạc tiếp tục với fade-in, 0 voice SFX đang phát, `Play` kế tiếp Played
  - Edge cases: `Ad` bật khi đang Locked giữ Locked (Story 011 kiểm tra phối hợp)

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/AdMuteReasonTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 004, Story 008
- Unlocks: Story 011
