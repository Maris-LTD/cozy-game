# Story 004: Priority Voice Stealing & Instance Caps

> **Epic**: Audio
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/audio.md` (A3.9, A3.10, A4, A5 Cướp voice theo ưu tiên, Formulas `steal_victim`, Edge Cases "Dồn dập và voice")
**Requirement**: `TR-audio-002`

**ADR Governing Implementation**: ADR-0004: Priority Voice Management & Audio Subsystem (kèm ADR-0001)
**ADR Decision Summary**: Khi hết voice, cướp voice đang phát cue ưu tiên thấp nhất (và cũ nhất); `instanceCap` hoặc bỏ yêu cầu mới (`DropNew`) hoặc cướp bản cũ nhất của chính cue (`StealOldest`).
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: nhóm 16 voice có cướp theo ưu tiên; phát qua `IAudioService`.
- Forbidden: không tạo/hủy `AudioSource` trong phiên; không static singleton.
- Guardrails: ≤ 16 voice đang phát ở mọi thời điểm; 0 B GC.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: logic thuần trên backend giả; đỉnh dựa trên voice đang phát của backend.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Audio/` (`VoiceStealer.cs`, `InstanceCapResolver.cs`, mở rộng `VoicePool.cs`, `PlayAdmission.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Audio/VoiceStealingTests.cs`, `AudioStormDriver.cs` (helper dùng chung với Story 005)
- Data: asset Story 002

## Acceptance Criteria

*From Docs/design/gdd/audio.md, scoped to this story:*

- [ ] `AC-9 [EditMode]` **GIVEN** `Q[PegHitScored].maxInstances` (6) bản đang phát, **WHEN** đến yêu cầu thứ 7 hợp lệ về `minInterval`, **THEN** bản bắt đầu sớm nhất dừng, bản mới phát trên voice đó, số bản = 6, bộ đếm "cướp cùng cue" tăng 1; **AND GIVEN** `Q[CoinSpawnAuto].maxInstances` bản đang phát, **WHEN** có yêu cầu mới, **THEN** `Dropped(InstanceCap)` và bản cũ không bị dừng.
- [ ] `AC-11 [EditMode]` **GIVEN** Audio đã khởi tạo, **WHEN** đếm nguồn phát, chạy kịch bản bão hòa `T.stormTestSeconds` giây, rồi đếm lại, **THEN** số nguồn = `C.totalVoices` ở cả hai lần (không tạo thêm, không hủy), và có đúng `C.musicVoices` voice chỉ nhận cue bus Music.
- [ ] `AC-12 [EditMode]` **GIVEN** đồng hồ tiêm, backend giả giữ mọi voice ở Playing (clip chưa hết), Audio Active, bus Plinko Normal, và trạng thái A = ví dụ `steal_victim`: 15 voice SFX bận gồm 6 `PegHitScored`, 2 `PegHitUnscored` (bắt đầu 2.10 s và 2.20 s), 1 `CoinSpawn`, 3 `BucketX2`, 3 voice khác ưu tiên ≥ 50, cùng voice nhạc đang phát; **WHEN** lần lượt yêu cầu `Plinko.PegHitUnscored` tại t = 2.25 s, `Plinko.JackpotHit` tại t = 2.30 s, rồi `Plinko.PegHitUnscored` tại t = 2.35 s (≥ 2.20 + `Q[PegHitUnscored].minInterval` (0.10)), **THEN** lần lượt: `Dropped(RateLimited)` (2.25 − 2.20 < `Q[PegHitUnscored].minInterval`, A3.8 xét trước A3.10; mốc vẫn 2.20); Played với nạn nhân là voice `PegHitUnscored` bắt đầu 2.10 s; `Dropped(NoVoice)` (số bản 1 < `maxInstances`, không voice nào ưu tiên < 10).
  **AND GIVEN** trạng thái B: 15 voice SFX bận bởi cue ưu tiên ≥ 80 **không** gồm `BucketX2` — 3 `BucketX5` (80), 3 `BucketX1` (80), 3 `JackpotBurstSpawn` (85), 2 `JackpotHit` (100), 4 cue Gacha ưu tiên 90 (`Gacha.Crank`, `Gacha.EggPop`, `Gacha.RarityReveal.common`, `Gacha.RarityReveal.rare`, mỗi cue 1 bản) — số bản `BucketX2` = 0 < `Q[BucketX2].maxInstances` và đã qua `Q[BucketX2].minInterval` kể từ lần phát gần nhất, **WHEN** yêu cầu `Plinko.BucketX2`, **THEN** `Dropped(NoVoice)` (không cướp ngang hàng với `BucketX5`/`BucketX1` ưu tiên 80) và không voice nào bị dừng.
  **AND GIVEN** lại trạng thái A (3 bản `BucketX2` đang phát = `Q[BucketX2].maxInstances`, `StealOldest`), **WHEN** yêu cầu `Plinko.BucketX2` sau `Q[BucketX2].minInterval`, **THEN** Played bằng cách tự cướp bản `BucketX2` cũ nhất (A3.9), bộ đếm "cướp cùng cue" tăng 1. Ở mọi vế, voice nhạc không bao giờ là nạn nhân.

## Implementation Notes

- A3.9: số bản đang phát của cue = `maxInstances` → `StealOldest` dừng bản cũ nhất của chính cue và dùng lại voice đó (đếm "cướp cùng cue"); `DropNew` → `Dropped(InstanceCap)`, bản cũ không bị dừng. A3.10: cấp voice SFX rảnh; hết voice → A5.
- A5 `steal_victim`: nạn nhân = voice SFX đang phát có `priority` **nhỏ hơn hẳn** `Q.priority`, so thứ tự từ điển `(priority, startTime)` — ưu tiên thấp nhất rồi bắt đầu sớm nhất; tập rỗng → `NoVoice`. Cue cùng mức ưu tiên không cướp nhau. Voice cướp dừng ngay, không fade. Voice nhạc không bao giờ là nạn nhân.
- Phân vùng voice (A4): `musicVoices` voice đầu dành riêng cho bus Music, `ambienceVoices` cho Ambience, còn lại `sfx_voices`; yêu cầu SFX chỉ chọn trong voice SFX. Tổng voice không đổi trong phiên (AC-11 đếm số nguồn của backend giả trước/sau).
- Hai trạng thái AC-12 dựng tường minh trong test bằng cách đặt đồng hồ tiêm và gửi yêu cầu theo đúng thời điểm (clip dài nên không hết); không dựng bằng cách sửa nội bộ.
- `AudioStormDriver` (helper test) sinh kịch bản bão hòa của AC-13 (134 `PegHitScored`/s xen 10% `PegHitUnscored`, 6.67 `CoinSpawn`/s, 1 `JackpotHit` mỗi 2 s trong `T.stormTestSeconds`) trên đồng hồ tiêm; Story 005 dùng lại. AC-11 chỉ cần chạy kịch bản này và đếm nguồn.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ (duyệt voice bằng vòng `for` trên mảng cấp sẵn), file ≤ 250 dòng, SRP, không static singleton, 0 cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 005]: khẳng định kết quả kịch bản bão hòa (số `PegHitScored` ≤ 251, 100% `JackpotHit`, tuổi nạn nhân) và tổng hợp chẩn đoán (AC-13, AC-30).
- [Story 003]: các bước A3.1–A3.8 và cấp voice rảnh.
- [Story 006]: chọn clip/pitch của bản phát.

## QA Test Cases

- **AC-9**: trần bản
  - Given: 6 bản `PegHitScored` đang phát; 1 bản `CoinSpawnAuto` đang phát (`DropNew`)
  - When: yêu cầu thứ 7 đủ `minInterval`; yêu cầu `CoinSpawnAuto` mới
  - Then: bản sớm nhất dừng, bản mới dùng voice đó, số bản = 6, "cướp cùng cue" +1; `CoinSpawnAuto` → `Dropped(InstanceCap)` và bản cũ vẫn phát
  - Edge cases: yêu cầu bị `RateLimited` không cướp voice nào
- **AC-11**: số voice cố định
  - Given: service đã khởi tạo trên backend giả
  - When: đếm nguồn, chạy bão hòa 10 s, đếm lại
  - Then: số nguồn = `totalVoices` cả hai lần; đúng `musicVoices` voice chỉ nhận bus Music
  - Edge cases: cấu hình MVP không có clip nhạc, voice nhạc vẫn không cấp cho SFX
- **AC-12**: nạn nhân và NoVoice
  - Given: trạng thái A rồi B theo mô tả AC (đồng hồ tiêm)
  - When: ba yêu cầu theo thời điểm 2.25 / 2.30 / 2.35; `BucketX2` ở trạng thái B; `BucketX2` ở trạng thái A
  - Then: RateLimited / Played (nạn nhân bắt đầu 2.10 s) / NoVoice; `NoVoice` không dừng voice nào; tự cướp bản `BucketX2` cũ nhất
  - Edge cases: voice nhạc không bao giờ là nạn nhân ở cả ba vế

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Audio/VoiceStealingTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 003
- Unlocks: Story 005, Story 009
