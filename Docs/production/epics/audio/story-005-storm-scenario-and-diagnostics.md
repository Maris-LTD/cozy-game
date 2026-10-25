# Story 005: Storm Scenario & Diagnostics Counters

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A14 Chẩn đoán, Formulas `cue_throughput`, Edge Cases "Dồn dập và voice", Tuning Knobs `T.stormPegHitRate`, `T.stormTestSeconds`)
**Requirement**: `TR-audio-002`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001)
**ADR Decision Summary**: Chuỗi 100+ yêu cầu chạm đinh mỗi giây không bao giờ vượt 16 voice; `JackpotHit` luôn phát và cướp voice ưu tiên thấp.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: 16 voice cố định với cướp theo ưu tiên; phát qua `IAudioService`.
- Forbidden: không tạo/hủy `AudioSource` trong phiên; không `Debug.Log*`.
- Guardrails: ≤ 16 voice ở mọi bước; 0 B GC; chẩn đoán đọc không cấp phát.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có; toàn bộ chạy trên backend giả và đồng hồ tiêm.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`AudioCounters.cs`, mở rộng `AudioStats.cs` ở Contracts)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioStormTests.cs` (dùng `AudioStormDriver` của Story 004)
- Data: asset Story 002, `AudioBalanceTargets.asset`

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-13 [EditMode]` **GIVEN** đồng hồ tiêm và `T.stormPegHitRate` (134) yêu cầu `PegHitScored`/s xen 10% `PegHitUnscored`, 6.67 `CoinSpawn`/s và 1 `JackpotHit` mỗi 2 s, **WHEN** chạy `T.stormTestSeconds` (10 s), **THEN** số `PegHitScored` được phát ≤ `T.stormTestSeconds` / `Q.minInterval` + 1 (251); voice đang phát ≤ `C.totalVoices` ở mọi bước; 100% `JackpotHit` được phát; mọi lần cướp cùng cue `PegHitScored` có tuổi nạn nhân ≥ `T.minStealAgeSeconds`.
- [ ] `AC-30 [EditMode]` **GIVEN** kịch bản AC-13, **WHEN** tổng hợp chẩn đoán, **THEN** số yêu cầu = Played + Σ Dropped theo lý do, mỗi yêu cầu được đếm đúng một lần, và đỉnh voice ≤ `C.totalVoices`.

## Implementation Notes

- Bộ đếm A14: số yêu cầu, `Played`, `Dropped` theo từng `DropReason`, số lần cướp (A5), số lần cướp cùng cue (A3.9), voice đang phát, đỉnh voice (`peakVoices`). Mỗi yêu cầu tăng đúng một bộ đếm kết quả. Đọc qua struct `AudioStats` (`TryGetStats`/`GetStats`), không cấp phát, test đọc bộ đếm thay cho log.
- Tuổi nạn nhân của lần cướp cùng cue = thời điểm yêu cầu − `startTime` của bản bị dừng; story này chỉ ghi nhận (giá trị nhỏ nhất của các lần cướp cùng cue, hoặc từng mẫu qua hook test) để khẳng định ≥ `T.minStealAgeSeconds`. Đảm bảo bởi dữ liệu (`maxInstances` × `minInterval` ≥ ngưỡng, Story 002), không thêm luật mới vào logic.
- Kịch bản bão hòa dùng `AudioStormDriver` (Story 004) với đồng hồ tiêm bước 1 ms hoặc theo thời điểm sự kiện; số yêu cầu `PegHitScored` ≈ 1340 trong 10 s nhưng chỉ ≤ 251 được phát (`minInterval` 0.04 s).
- Mọi giá trị 134, 10 s, 0.2 s lấy từ `AudioBalanceTargets`/`AudioConfig`, không literal trong test.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, không static singleton, 0 cấp phát khi đếm.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- AC-33 [Perf] (0 B mỗi khung trong scene thật bằng `ProfilerRecorder`) và AC-34 [Perf] — cần scene Plinko thật / thiết bị tham chiếu; xem EPIC "Deferred acceptance criteria".
- [Story 004]: luật cướp voice và `AudioStormDriver`.
- [Story 007]: ảnh hưởng của bus nền lên kịch bản.

## QA Test Cases

- **AC-13**: bão hòa
  - Given: đồng hồ tiêm, backend giả giữ clip đang phát, cấu hình mặc định
  - When: chạy kịch bản bão hòa 10 s
  - Then: `PegHitScored` Played ≤ 251; voice đang phát ≤ 16 mọi bước; mọi `JackpotHit` Played; mọi lần cướp cùng cue `PegHitScored` có tuổi nạn nhân ≥ 0.2 s
  - Edge cases: hai `JackpotHit` cách nhau < 0.1 s chỉ phát một (không có trong kịch bản mặc định, cách nhau 2 s)
- **AC-30**: chẩn đoán
  - Given: kết thúc kịch bản AC-13
  - When: đọc `AudioStats`
  - Then: yêu cầu = Played + Σ Dropped theo lý do; mỗi yêu cầu đếm một lần; `peakVoices` ≤ `totalVoices`
  - Edge cases: `GetStats` gọi nhiều lần không đổi số liệu và không cấp phát

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/AudioStormTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 004
- Unlocks: None
