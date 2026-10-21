# Audio

> **Status**: Approved
> **Author**: Maris-LTD + Claude (game-designer)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 1 — Kinetic ASMR (hỗ trợ Pillar 2 — Gacha Thrill: khoảnh khắc thưởng không bị tiếng nền lấn)
> **Decisions**: Audio D1–D6 + Boot D3 + quyết định D2 về voice PegHit (6 `PegHitScored` + 2 `PegHitUnscored` trong tổng 16) + Bootstrap B (rewarded ad không phát `Paused`, Audio tắt tiếng qua `SetMuteReason(Ad)`) (`Docs/production/session-logs/decision-log.md`, 2026-10-21)
> **Consulted**: không (workflow cấm subagent; Formulas và Acceptance Criteria do game-designer tự viết; chưa có agent audio — ý đồ âm thanh chỉ là mô tả)
> [CD-GDD-ALIGN] skipped (lean)
> **Revision 1 — 2026-10-21** (đợt 2, theo `Docs/production/session-logs/gdd-consistency-2026-10-21-run1.json` C1[9], C1[11], C1[15], C1[17]): Audio tự đăng ký bước `audio.service` (pha `ServiceInit`, không cốt lõi) vào hợp đồng pha `IBootStep` của Bootstrap — Audio phụ thuộc Bootstrap, không ngược lại (A16); dùng đúng `Paused {reason}` / `Resumed {reason, awaySeconds}`, `FirstInteraction`, `BootCompleted`, `TabChanged {previous, current}` của `bootstrap-and-scene-flow.md`; cue Plinko khớp sự kiện R12 của `plinko-board.md` (`PegHit.rowIndex` → `pitchPosition`, `CoinSpawned.origin` / `burstIndex`, `CoinStuck`, `BoardBusy`); `pitchJitterRatio` của `Plinko.PegHitScored` và chênh dB `CoinSpawnAuto`/`CoinSpawn` chỉ có chủ là SoundCue; Cross-References cập nhật file đã có. Audio D1–D6 giữ nguyên.
> **Revision 2 — 2026-10-21** (fix round 1 theo `Docs/design/gdd/reviews/audio-review-2026-10-21.md`, ba mục chặn): AU1-B1 — `Plinko.PegHitUnscored.minInterval` 0.08 → 0.10 (steal_age 0.20) và `Plinko.CoinSpawn` → `DropNew` kèm trần độ dài clip `T.manualSpawnClipMaxSeconds` (≤ `tap_cooldown`), validator, AC-5, AC-6 cập nhật; AU1-B2 — `Plinko.JackpotBurstSpawn` có `pitchedSamples` riêng (3 mẫu chime 4/12/19), Visual/Audio ghi số mẫu cần thu; AU1-B3 — ví dụ `steal_victim` và AC-12 ghi thời điểm yêu cầu và dựng trạng thái tường minh, khớp A3.8/A3.9. Audio D1–D6 giữ nguyên.
> **Revision 3 — 2026-10-21** (fix round 2 theo `Docs/design/gdd/reviews/audio-review-2026-10-21.md`, mục chặn AU2-B1): `cue_throughput` Ràng buộc 2 so **thời gian phát thực** `max(clip.length) / (1 − Q.pitchJitterRatio)` thay cho `clip.length`, và thêm ràng buộc knob `T.manualSpawnClipMaxSeconds ≤ tap_cooldown − T.spawnClipMarginSeconds` (knob mới 0.03 s, biên khung/bộ đệm và chênh đồng hồ mô phỏng–thực); `T.manualSpawnClipMaxSeconds` 0.15 → 0.12; AC-5, AC-6, Edge Cases, Tuning Knobs, Visual/Audio cập nhật. Audio D1–D6 giữ nguyên.
> **Revision 5 — 2026-10-21** (fix round theo `gdd-consistency-2026-10-21-run3.json` C3[4], C3[17] và quyết định asset): mốc nhạc VS = `Interactive` thay `BootCompleted` (A11.2, A16.1, Interactions, Dependencies, AC-26, Assumed); cue hũ ánh xạ theo `bucketIndex` / nhãn hũ, không theo `multiplier`; đóng AU-R20 (CC0 cho MVP, mua gói cho VS).
> **Revision 4 — 2026-10-21** (fix round 3 theo `Docs/design/gdd/reviews/audio-review-2026-10-21.md` Round 3 và quyết định user trong `decision-log.md`): AU3-R2 — D2 = 6 voice `PegHitScored` + 2 voice `PegHitUnscored` trong tổng 16 là quyết định của user, không còn là Assumed (A4, Open Questions; knob 6/2 giữ nguyên); khớp `plinko-board.md` R11.3 — xu xả hàng đợi loạt khi `Paused` dùng lại `reason = Overflow` (không có reason mới), không có cue nào phát; khớp Bootstrap B (B6.10) — `SetMuteReason(Ad)` vẫn tạm ngưng tiếng trong khi mô phỏng chạy tiếp, voice SFX dừng hẳn thay vì giữ vị trí (A9, States, Interactions, Dependencies, Edge Cases, AC-43).

## Summary

Audio là hạ tầng âm thanh của tiệm trứng: nó giữ kho SoundCue (clip, dB, cao độ, ưu tiên, trần phát), 16 voice có luật cướp theo ưu tiên, cây bus, nhạc nền từ Vertical Slice, và việc tắt tiếng khi tab ẩn, khi quảng cáo chạy hay khi người chơi bấm nút loa. Với người chơi, nó biến hàng trăm cú chạm đinh mỗi giây thành một "cơn mưa marimba" ngũ cung êm tai mà tiếng jackpot luôn nổi lên được — trung tâm của Pillar 1. Audio không nghe sự kiện gameplay; Feedback & Juice gọi `Play(cueId)` và Audio quyết định tiếng nào được phát.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `Bootstrap & Scene Flow (soft: bước audio.service ở pha ServiceInit; Paused/Resumed, FirstInteraction, Interactive)`; được dùng bởi Feedback & Juice, HUD & Tab Navigation, Settings & Accessibility (Alpha), Rewarded Ads / Platform Services (Alpha)

## Overview

Audio là hạ tầng âm thanh của game: nó sở hữu cây bus âm lượng, kho **SoundCue** (mỗi cue là một bộ clip kèm âm lượng dB, dải cao độ, mức ưu tiên, trần số bản phát cùng lúc và khoảng tối thiểu giữa hai lần phát), một nhóm voice cấp sẵn có trần, luật cướp voice theo ưu tiên, nhạc nền (từ Vertical Slice), tắt tiếng theo lý do (app xuống nền / tab ẩn, quảng cáo, người chơi) và việc mở khóa âm thanh WebGL sau thao tác đầu tiên. Audio **không** nghe sự kiện gameplay: Feedback & Juice dịch sự kiện Plinko (`CoinSpawned`, `PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy` — `plinko-board.md` R12), sự kiện Gacha… thành lệnh `Play(CueId, …)`; Audio chỉ quyết định cue đó có được phát không, phát ở voice nào, to bao nhiêu và ở cao độ nào (D1).

Người chơi không tương tác trực tiếp với hệ này ngoài nút tắt tiếng ở header (MVP) và hai thanh trượt Nhạc/SFX trong Settings (Alpha). Nhưng mọi tiếng xu gỗ, nốt marimba và chuông jackpot mà Pillar 1 hứa hẹn đều đi qua đây: khi bàn Plinko bão hòa sinh tới ~134 lần chạm đinh mỗi giây, chính luật giới hạn voice của Audio quyết định người chơi nghe một "cơn mưa marimba" êm hay một mớ tiếng vụn kiểu casino. Thiếu Audio, mỗi hệ sẽ tự phát tiếng, không ai giữ trần voice trên WebGL, và tiếng jackpot có thể bị tiếng chạm đinh nuốt mất.

## Detailed Design

### Core Rules

Mọi tên trong `backtick` là knob ở `AudioConfig`, ở một asset `SoundCue` hoặc ở `AudioBalanceTargets` (xem Tuning Knobs); không có con số nào nằm trong logic.

**A1. Cây bus**
```
Master
├── Music
├── Ambience          (dự trữ, v1.0 không dùng)
└── Sfx
    ├── Gameplay      (cue gameplay khác: Kệ…)
    │   ├── Plinko
    │   └── Gacha
    └── Ui
```
1. Mỗi bus có một mức dB mặc định (`AudioConfig.busDefaultDb[bus]`). Riêng Music được suy ra: `Music = Sfx + musicOffsetFromSfxDb` (mặc định −12 dB so với SFX, D3).
2. Mỗi SoundCue thuộc đúng một bus (lá hoặc nhánh). Âm lượng cuối của một voice là tổng dB dọc đường từ cue lên Master rồi đổi sang tuyến tính một lần (công thức `voice_gain`), áp thẳng vào âm lượng nguồn phát. Thiết kế không đòi AudioMixer (chỉ được hỗ trợ một phần trên WebGL); có dùng mixer hay không → ADR.
3. Mọi nguồn phát là 2D (spatialBlend = 0, hằng số thiết kế: game 2D màn dọc, nghe chủ yếu qua loa điện thoại).

**A2. SoundCue** (D1) — mỗi cue là một asset gồm: `cueId` (chuỗi bất biến dạng `<Nhóm>.<Tên>`, ví dụ `Plinko.PegHitScored`), `bus`, `clips[]`, `volumeDb`, `priority` (0–100, lớn = quan trọng hơn), `maxInstances`, `minInterval` (s), `instanceCapPolicy` (`StealOldest` | `DropNew`), `pitchMode` (`None` | `Random` | `Scale`), `pitchJitterRatio`, `keepWhenBusBackground` (bool), `avoidRepeat` (bool), `panFromPosition` (bool), `loop` (bool). Feedback & Juice chỉ giữ bảng sự kiện → `cueId`; không hệ nào ngoài Audio giữ dB, cao độ hay ưu tiên của tiếng.

**A3. Nhận một yêu cầu phát** — `Play(cueId, params)` với `params = { pitchPosition ∈ [0,1] (tùy chọn), panPosition ∈ [−1,1] (tùy chọn) }`. Các bước kiểm tra theo đúng thứ tự; bước đầu tiên không thỏa thì yêu cầu bị **bỏ** với một lý do duy nhất, không xếp hàng, không phát lại sau:
1. Audio chưa khởi tạo xong hoặc khởi tạo lỗi → `NotReady`.
2. `cueId` không có trong kho → `UnknownCue`.
3. Đang khóa WebGL (A10) → `Locked`.
4. Đang tạm ngưng vì lý do Background hoặc Ad (A9) → `Suspended`.
5. Người chơi đang tắt tiếng và cue không phải nhạc (A9) → `UserMuted`.
6. Bus của cue đang im (thanh trượt người chơi = 0, Alpha) → `BusSilent`.
7. Bus của cue đang ở chế độ nền và cue có `keepWhenBusBackground = false` (A8) → `BackgroundFiltered`.
8. Chưa qua `minInterval` kể từ lần **được phát** gần nhất của cue này → `RateLimited`. Yêu cầu bị bỏ không dời mốc này.
9. Số bản đang phát của cue = `maxInstances`: với `StealOldest` dừng bản cũ nhất của chính cue đó và dùng lại voice của nó; với `DropNew` → `InstanceCap`.
10. Cấp voice (A4, A5); không có voice → `NoVoice`.
Kết quả trả về `Played` hoặc `Dropped(reason)`; mỗi kết quả tăng đúng một bộ đếm chẩn đoán (A14).

**A4. Voice** — Audio tạo sẵn `totalVoices` nguồn phát lúc khởi tạo và không bao giờ tạo thêm hay hủy trong phiên (giữ 0 B GC, Plinko AC-38). `musicVoices` voice được dành riêng cho bus Music, `ambienceVoices` cho Ambience; số còn lại là voice SFX (`sfx_voices`). Một yêu cầu SFX lấy voice SFX rảnh bất kỳ; nếu không còn voice rảnh thì sang A5. Tổng voice 16 tính cả voice nhạc (D2; Assumed, xem Open Questions). Trong 15 voice SFX, chạm đinh dùng tối đa 6 voice `Plinko.PegHitScored` + 2 voice `Plinko.PegHitUnscored` (quyết định user về D2, decision-log 2026-10-21; trần là `maxInstances` của hai cue). Voice của Audio tự quản, không dùng Object Pooling (Pool D2).

**A5. Cướp voice theo ưu tiên** — khi hết voice SFX rảnh, ứng viên bị cướp là các voice SFX đang phát cue có `priority` **nhỏ hơn hẳn** cue mới. Chọn ứng viên có `priority` thấp nhất; bằng nhau thì chọn voice bắt đầu sớm nhất. Không có ứng viên → bỏ yêu cầu (`NoVoice`). Cue cùng mức ưu tiên không cướp nhau (tránh xoay vòng vô ích; cùng một cue đã có A3.9). Voice bị cướp dừng ngay, không fade: nạn nhân luôn là tiếng ít giá trị nhất và cũ nhất, tức đã tắt dần nhiều nhất. Voice nhạc không bao giờ bị cướp.

Thứ tự ưu tiên (lớn → nhỏ): `JackpotHit` > mở trứng / chuông phẩm cấp > chime xu Bùng Xu > xu vào hũ > UI xác nhận > `PegHit` được tính > spawn xu > xu kẹt (fizzle) > bàn bận > `PegHit` không tính. `PegHit` không tính đứng thấp nhất và bị bỏ trước (D2; khác thứ tự trong brief, nơi "bàn bận" đứng cuối — quyết định của user thắng).

**A6. Cao độ** — mọi cao độ luôn > 0 (WebGL chỉ nhận pitch dương trên nguồn phát).
1. `None`: pitch = 1.
2. `Random`: pitch = 1 + U(−`pitchJitterRatio`, +`pitchJitterRatio`).
3. `Scale` (marimba theo tầng đinh, D4): `pitchPosition` được đổi thành một nốt trong thang ngũ cung trưởng 2 quãng tám (`scaleNoteCount` nốt, gốc `scaleRootMidi`, khoảng `scaleIntervals`); mỗi cue `Scale` có bộ mẫu gốc riêng (trầm/trung/cao) với nốt gốc riêng (`pitchedSamples[i].nativeSemitone`) — v1.0 có hai cue `Scale`: `Plinko.PegHitScored` (3 mẫu marimba) và `Plinko.JackpotBurstSpawn` (3 mẫu chime), cả hai nốt gốc 4/12/19 (Tuning Knobs); mỗi nốt dùng mẫu gần nhất, đổi pitch không quá `maxSampleShiftSemitones` (±4 nửa cung), rồi nhân jitter (1 ± `pitchJitterRatio`; với `Plinko.PegHitScored` là `SoundCue[Plinko.PegHitScored].pitchJitterRatio` (0.03) — chủ duy nhất theo D1, `plinko-board.md` đã bỏ `pegPitchJitter` và chỉ tham chiếu field này). Công thức `note_index`, `sample_pitch`.
4. Chọn clip: `Scale` lấy mẫu theo nốt; `None`/`Random` chọn ngẫu nhiên một clip, và nếu `avoidRepeat` và cue có ≥ 2 clip thì không lặp lại clip vừa phát của cue đó.
5. Thang âm cố định một tông cho mọi cue có cao độ và cho nhạc VS: ngũ cung không có quãng nghịch nên nốt chồng ngẫu nhiên vẫn hòa âm — điều kiện để "mưa marimba" nghe êm.

**A7. Pan** — cue có `panFromPosition` nhận pan = `maxPan` × `panPosition` (kẹp [−1,1]); cue khác pan = 0. Feedback & Juice tính `panPosition` từ x của sự kiện (ví dụ x xu / nửa bề rộng bàn). Hỗ trợ pan trên WebGL cần xác minh (→ ADR); nếu không hỗ trợ, pan = 0 trên WebGL mà không đổi luật nào khác.

**A8. Bus ở chế độ nền** (D5) — `SetBusBackground(bus, on)`. Khi bật cho bus Plinko (bàn không hiển thị: Tab 2/3):
1. Bus Plinko được cộng `plinkoBackgroundOffsetDb` (−12 dB), chuyển mượt trong `busTransitionSeconds`; voice đang phát tiếp tục với âm lượng mới, không bị cắt.
2. Yêu cầu mới cho cue có `keepWhenBusBackground = false` bị bỏ (`BackgroundFiltered`). Trong v1.0 chỉ hai nhóm cue giữ lại: xu vào hũ (`Plinko.BucketX1/X2/X5`, đã được Feedback gộp theo cửa sổ số bay) và `Plinko.JackpotHit`. Mọi `PegHit`, spawn, chime Bùng Xu, fizzle, bàn bận đều bị bỏ.
3. Không dùng lọc thông thấp hay hiệu ứng mixer (an toàn WebGL, D5).
4. Tắt chế độ nền → offset về 0 trong `busTransitionSeconds`, cue lại được nhận bình thường.
5. Chỉ bus Plinko có offset nền ở v1.0. Feedback & Juice gọi `SetBusBackground(Plinko, TabChanged.current ≠ Board)` khi nhận `TabChanged {previous, current}` của Bootstrap (`TabChanged` theo Bootstrap B9.2; bảng Interactions của Bootstrap liệt kê đúng phản ứng này cho Feedback; Feedback GDD chốt — xem Open Questions); Audio không biết khái niệm tab. Khởi động luôn vào tab `Board` (Bootstrap `SceneFlowConfig.defaultTab`) nên bus Plinko bắt đầu ở Normal mà không cần sự kiện nào.

**A9. Tắt tiếng theo lý do** — một tập lý do độc lập, mỗi lý do bật/tắt riêng:
| Lý do | Bật khi | Tắt khi | Hành vi |
|---|---|---|---|
| `Background` | Bootstrap phát `Paused {reason}` với `reason = Hidden` (tab trình duyệt ẩn) hoặc `Background` (app xuống nền) — **không** phải khi cửa sổ chỉ mất focus mà vẫn hiển thị (Boot D3, Bootstrap B6.2) | Bootstrap phát `Resumed {reason, awaySeconds}` (Audio bỏ qua `awaySeconds`) | Tạm ngưng (Suspended) |
| `Ad` (Alpha) | Platform Services gọi `SetMuteReason(Ad, true)` khi rewarded ad bắt đầu (cùng lúc `SetAdOpen(true)`, Bootstrap B6.10) | Platform Services gọi `SetMuteReason(Ad, false)` khi quảng cáo đóng | Tạm ngưng (Suspended); mô phỏng **vẫn chạy** (Bootstrap B: không có `Paused`) |
| `User` | Người chơi bấm nút tắt tiếng | Bấm lại | Câm có fade |

1. **Tạm ngưng** (còn ít nhất một lý do `Background`/`Ad`): mọi voice, kể cả nhạc, dừng tại chỗ (giữ vị trí phát); yêu cầu mới bị bỏ (`Suspended`). Khi lý do tạm ngưng cuối cùng tắt, mọi voice tiếp tục từ vị trí cũ và âm lượng tăng từ 0 lên mức đích trong `resumeFadeInSeconds`. Với `Background`, mô phỏng Plinko cũng dừng cùng lúc (Plinko R11) nên tiếng tiếp tục khớp với bàn.
   - **Riêng `Ad`** (quyết định user Bootstrap B, B6.10): Bootstrap không phát `Paused`, nên mô phỏng Plinko, Idle và Economy chạy tiếp và Feedback & Juice vẫn gọi `Play` — mọi yêu cầu bị bỏ (`Suspended`), không xếp hàng, không phát bù khi quảng cáo đóng (kể cả `JackpotHit`: xu vẫn được cộng, chỉ mất tiếng). Khi `Ad` bật, mọi voice SFX (đang Playing hoặc Paused) dừng hẳn và về Free thay vì giữ vị trí, vì bàn đã chạy tiếp nên đuôi tiếng cũ sẽ lệch bàn khi quay lại; voice nhạc vẫn dừng tại chỗ và tiếp tục với fade-in như trên. Audio không đọc `IsAdOpen` và không nghe sự kiện quảng cáo của nền tảng: lý do `Ad` chỉ đổi qua `SetMuteReason(Ad, bool)`.
2. **Câm bởi người chơi**: âm lượng Master giảm về 0 trong `userMuteFadeSeconds`; nhạc vẫn chạy câm (vị trí tiếp tục trôi như radio); yêu cầu SFX mới bị bỏ (`UserMuted`) để không tốn voice. Bật lại tiếng → Master về mức cũ trong `userMuteFadeSeconds`, rồi phát `Ui.SoundOn` một lần để xác nhận.
3. Hai cơ chế chồng nhau được: ví dụ quảng cáo kết thúc trong khi tab vẫn ẩn → vẫn tạm ngưng; trở lại tab khi người chơi đang câm → vẫn câm.

**A10. Mở khóa âm thanh WebGL** — trên WebGL (`requiresUnlock` = đúng, hằng số theo nền tảng do chính sách autoplay của trình duyệt) Audio khởi tạo ở trạng thái Locked; nó mở khóa khi Bootstrap phát `FirstInteraction`. Bootstrap có thể phát `FirstInteraction` ngay trong `Booting.Steps`, kể cả trước khi bước `audio.service` chạy (Bootstrap B12.3, bảng trạng thái `Booting.Steps`), nên Audio đăng nghe sự kiện này ngay lúc đăng ký bước (A16) và ghi nhớ nếu nó đến khi còn Uninitialized: khởi tạo xong thì vào thẳng Active (hoặc UserMuted / Suspended theo A9) thay vì Locked. Trước đó mọi yêu cầu bị bỏ (`Locked`), không xếp hàng: không cue quan trọng nào được thiết kế để phát trước cú chạm đầu. Cú chạm đầu vào Plinko vẫn thả xu bình thường; tiếng "tok" của chính cú đó có thể mất. Trên Android/iOS không có khóa.

**A11. Nhạc nền** (D3)
1. MVP: không có nhạc. Bus Music và voice nhạc đã tồn tại (voice để rảnh) để VS không phải đổi cấu trúc.
2. Vertical Slice: đúng một loop nhạc acoustic/lo-fi nhẹ trên bus Music (`Music.ShopLoop`), lặp liền mạch, viết ở cùng tông `scaleRootMidi`. Nhạc bắt đầu khi Audio đã mở khóa (A10) và Bootstrap đã phát `Interactive` (B5.3: rèm kéo xong, game tương tác được; mốc bắt đầu nhạc), tăng dần trong `musicFadeInSeconds`. Mặc định nghe nhỏ hơn SFX `musicOffsetFromSfxDb` (−12 dB).
3. Không có nhạc thích ứng, không đổi bài, không crossfade trong v1.0.

**A12. Lưu thiết lập của người chơi** (D6) — `userMuted` (MVP) và hai giá trị thanh trượt Nhạc/SFX (Alpha) được ghi vào kho thiết lập cục bộ của thiết bị ngay khi đổi, và được đọc khi Audio khởi tạo, **trước** khi bất kỳ cue nào có thể phát. Mặc định lần đầu: không câm, hai thanh trượt = `defaultUserSlider`. Kho thiết lập tách khỏi save tiến độ (Assumed); API lưu cụ thể → ADR.

**A13. Đồng hồ** — `minInterval`, fade và tuổi voice đo bằng đồng hồ thực không phụ thuộc time scale (tiêm được cho test). Không có cue nào được nhận trong lúc tạm ngưng, nên thời gian tạm ngưng không ảnh hưởng `minInterval`.

**A14. Chẩn đoán** — Audio đếm: số yêu cầu, số `Played`, số `Dropped` theo từng lý do, số lần cướp (A5) và số lần cướp cùng cue (A3.9), voice đang phát và đỉnh voice. Test đọc các bộ đếm này thay cho log.

**A15. Ranh giới** (D1) — Audio chỉ phụ thuộc Bootstrap & Scene Flow (hợp đồng pha `IBootStep` và sự kiện vòng đời, cùng tầng Foundation). Audio không tham chiếu Plinko, Gacha, Shelf hay bất kỳ hệ gameplay nào; mọi tiếng gameplay đến qua `Play(cueId, …)` do Feedback & Juice gọi.

**A16. Bước khởi động** (C1[15]) — Bootstrap không tham chiếu Audio; Audio tự đăng ký vào hợp đồng pha của Bootstrap (Bootstrap B2, B3), nên mũi tên phụ thuộc chỉ đi một chiều Audio → Bootstrap.
1. Trong lúc scene nạp (trước khung đầu, Bootstrap B2.1), Audio đăng ký đúng một `IBootStep`: `Id` = `audio.service`, `Phase` = `ServiceInit`, `RunsAfter` rỗng, `IsCritical` = sai. Cùng lúc đó Audio đăng nghe `Paused`, `Resumed`, `FirstInteraction`, `Interactive`.
2. `Run()` (Bootstrap gọi đúng một lần): đọc `AudioConfig` và kho SoundCue, đọc thiết lập người chơi (A12), tạo sẵn `totalVoices` voice (A4). Xong → `IsDone`, hệ rời Uninitialized sang Locked / Active (States and Transitions). Thiếu config, không tạo được voice, `Run()` ném ngoại lệ hoặc quá hạn bước của Bootstrap (`BootConfig.stepTimeoutSeconds`) → `IsFailed` / bước lỗi, Audio vào Failed; Bootstrap bỏ qua bước không cốt lõi và vẫn phát `BootCompleted` (Bootstrap B4.2).
3. Audio không có bước ở pha nào khác và không chờ dữ liệu hay save: thiết lập âm thanh nằm ở kho cục bộ riêng (A12), không ở pha `SaveLoad`.

**Hệ này không thể**: phát quá `totalVoices` tiếng cùng lúc; phát một cue nhiều hơn `maxInstances` bản hay dày hơn 1/`minInterval` lần mỗi giây; cho tiếng thấp ưu tiên cướp tiếng cao ưu tiên; phát tiếng trước cú chạm đầu trên WebGL; phát tiếng khi tab ẩn / app ở nền / quảng cáo đang chạy; tạo cao độ âm hay bằng 0.

### States and Transitions

Trạng thái của hệ Audio:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Uninitialized | Scene nạp; Audio đăng ký bước `audio.service` (A16) | `Run()` của `audio.service` xong → Locked (WebGL, chưa có `FirstInteraction`) hoặc Active (mobile, hoặc `FirstInteraction` đã đến trước, A10); bước lỗi → Failed | Mọi `Play` → `NotReady`; ghi nhớ `FirstInteraction` nếu nó đến (`Paused`/`Resumed` không phát trong `Booting.Steps`, Bootstrap B6.4) |
| Failed | Bước `audio.service` lỗi: thiếu `AudioConfig`, không tạo được voice, ngoại lệ hoặc quá hạn (A16.2) | Không thoát trong phiên (nạp lại scene sau "Thử lại" của Bootstrap là phiên mới) | Mọi `Play` → `NotReady`; API tắt tiếng vẫn ghi/đọc thiết lập; game chạy tiếp không tiếng (Bootstrap: phụ thuộc mềm) |
| Locked | Khởi tạo xong trên WebGL | `FirstInteraction` → Active / UserMuted (theo `userMuted` đã lưu) | Mọi `Play` → `Locked` |
| Active | Mở khóa, hết lý do tạm ngưng, không câm | Bật `User` → UserMuted; bật `Background`/`Ad` → Suspended | Nhận yêu cầu theo A3 |
| UserMuted | Bật lý do `User` | Tắt `User` → Active; bật `Background`/`Ad` → Suspended | Master fade về 0; nhạc chạy câm; SFX mới → `UserMuted` |
| Suspended | Có ít nhất một lý do `Background`/`Ad` | Hết lý do tạm ngưng → UserMuted (nếu `User` bật) hoặc Active, kèm fade-in `resumeFadeInSeconds` | Mọi voice dừng tại chỗ (khi `Ad` bật: voice SFX dừng hẳn về Free, chỉ nhạc giữ vị trí — A9.1); mọi `Play` → `Suspended` |

Lý do `Background`/`Ad` có thể bật trong lúc Locked: khi đó hệ vẫn ở Locked, và sau `FirstInteraction` sẽ vào Suspended nếu lý do còn bật.

Trạng thái của một voice:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Free | Khởi tạo; clip (không loop) phát hết; bị dừng / bị cướp; voice SFX khi lý do `Ad` bật (A9.1) | Được cấp cho một yêu cầu → Playing | Im, chờ cấp |
| Playing | Được cấp (A3.9 / A4 / A5) | Hết clip → Free; bị cướp → Free (ngay lập tức, rồi Playing cue mới); lý do `Ad` bật → Free (voice SFX) hoặc Paused (voice nhạc); hệ vào Suspended vì `Background` → Paused | Phát với âm lượng `voice_gain`, cao độ, pan đã tính lúc nhận |
| Paused | Hệ vào Suspended (voice nhạc, hoặc mọi voice khi lý do là `Background`) | Hệ rời Suspended → Playing (fade-in); lý do `Ad` bật khi voice SFX đang Paused → Free | Giữ vị trí phát |

Trạng thái của một bus: `Normal` ↔ `Background` (A8; chuyển trong `busTransitionSeconds`, gọi lại cùng trạng thái không làm gì).

### Interactions with Other Systems

| Hệ | Audio nhận vào | Audio phát ra | Chủ interface |
|---|---|---|---|
| Bootstrap & Scene Flow (`bootstrap-and-scene-flow.md`) | `Run()` của bước `audio.service` (pha `ServiceInit`, `IsCritical` = sai, A16); `Paused {reason}` / `Resumed {reason, awaySeconds}` (tab ẩn, app nền), `FirstInteraction`, `Interactive` (mốc bắt đầu nhạc VS) | `IsDone` / `IsFailed` của bước (Ready / Failed) | Bootstrap sở hữu hợp đồng pha và sự kiện vòng đời; Audio sở hữu bước của mình và phản ứng |
| Feedback & Juice *(chưa có GDD)* | `Play(cueId, {pitchPosition, panPosition})`; `SetBusBackground(Plinko, TabChanged.current ≠ Board)` khi `TabChanged` | `PlayResult` (Played / Dropped + lý do) | Audio sở hữu SoundCue và API; Feedback sở hữu bảng sự kiện → `cueId` (đề xuất cho Plinko ở bảng dưới), cách tính `pitchPosition` và `panPosition`, việc gộp tiếng vào hũ theo `floatingTextMergeWindow` |
| HUD & Tab Navigation *(chưa có GDD)* | `SetUserMuted(bool)` từ nút loa ở header | `IsUserMuted`, sự kiện `MuteChanged` | Audio |
| Settings & Accessibility (Alpha) | `SetUserVolume(Music \| Sfx, 0–1)` | Giá trị thanh trượt hiện tại | Audio |
| Platform Services / Rewarded Ads (Alpha) | `SetMuteReason(Ad, bool)` quanh mỗi rewarded ad (cùng lúc `SetAdOpen` của Bootstrap B6.10; mô phỏng chạy tiếp, chỉ tiếng tạm ngưng) | — | Audio (API, hành vi tạm ngưng); Platform Services (gọi đúng lúc); Bootstrap (cờ "ad đang mở" — Audio không đọc) |
| Plinko Board | — (gián tiếp qua Feedback & Juice) | — | Plinko R6.3 giao giới hạn voice cho Audio; Plinko R12.3 không gọi Audio; Audio không nghe sự kiện R12 (`CoinSpawned`, `PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy`) trực tiếp |
| Gacha Machine, Shelf & Synergy | — (gián tiếp qua Feedback & Juice) | — | Cue ID của Gacha là provisional tới khi có GDD Gacha |
| Object Pooling | — | — | Không phụ thuộc (Pool D2: Audio tự quản voice) |

**Sự kiện Plinko → cue** (C1[9]; *đề xuất* cho bảng của Feedback & Juice, là chủ bảng này theo D1 — Audio chỉ cam kết `cueId` và ý nghĩa của `pitchPosition`/`panPosition`). Payload theo `plinko-board.md` R12.2; `panPosition` của mọi dòng = `worldPosition.x` / (nửa bề rộng bàn = `PlinkoConfig.pegsPerRow` × `PlinkoConfig.pegSpacing` / 2), Audio kẹp vào [−1, 1] (A7).

| Sự kiện Plinko (payload dùng) | `cueId` | `pitchPosition` | Ghi chú |
|---|---|---|---|
| `CoinSpawned` (`origin` = Manual) | `Plinko.CoinSpawn` | — | |
| `CoinSpawned` (`origin` = Auto) | `Plinko.CoinSpawnAuto` | — | Nhỏ hơn `CoinSpawn` theo hai `SoundCue.volumeDb` (Tuning Knobs) |
| `CoinSpawned` (`origin` = JackpotBurst, `burstIndex`) | `Plinko.JackpotBurstSpawn` | `burstIndex` / (`jackpot_burst_count` − 1) | Chime đi lên theo thứ tự xu trong loạt |
| `CoinSpawned` (`origin` = Rain) | — (MVP); VS: `Plinko.RainLoop` chạy khi hàng đợi còn xu `Rain` | — | Assumed (Open Questions) |
| `PegHit` (`scored` = đúng, `rowIndex`) | `Plinko.PegHitScored` | 1 − `rowIndex` / (`PlinkoConfig.pegRows` − 1) | Tầng trên (r = 0) nốt cao nhất, nốt đi xuống khi xu rơi |
| `PegHit` (`scored` = sai) | `Plinko.PegHitUnscored` | — | |
| `JackpotHit` (mọi `origin`) | `Plinko.JackpotHit` | — | Phát trước `CoinResolved` của cùng xu (R12.2) |
| `CoinResolved` (`reason` = Bucket, `bucketIndex`) | `Plinko.BucketX1` / `X2` / `X5` theo `bucketIndex` | — | Tra bảng `bucketIndex` → cue của Feedback, theo nhãn hũ (hũ jackpot `bucket_is_jackpot` → `X5`; hũ kề tâm → `X2`; hũ giữa → `X1`), **không** theo giá trị `multiplier`: đổi `PlinkoConfig.bucketMultipliers` không làm đổi ánh xạ (tên `X1/X2/X5` là bậc thưởng, không phải hệ số). Feedback gộp cùng hũ theo `floatingTextMergeWindow` |
| `CoinResolved` (`reason` = Stuck / Overflow) | — | — | Tiếng tan đi theo `CoinStuck`; Overflow (trả ngay: tràn hàng đợi R4.2, xả hàng đợi loạt khi `Paused` R11.3, hoặc `RequestBurst` đến lúc Paused) không có tiếng (Assumed) — với ca `Paused` thì Audio cũng đang/sắp Suspended (`Background`), nên dù thứ tự hai hệ nghe `Paused` thế nào cũng không cue nào phát |
| `CoinStuck` | `Plinko.CoinFizzle` | — | |
| `BoardBusy` | `Plinko.BoardBusy` | — | Feedback chặn thêm theo `PlinkoFeedbackConfig.busyFeedbackMinInterval` |

→ Thành ADR: cơ chế API (`Play` không cấp phát: tham số dạng struct, `cueId` tra sẵn thành chỉ số); dùng AudioMixer hay âm lượng tuyến tính trên nguồn phát; cách tạm dừng toàn bộ trên WebGL (AudioListener.pause hay dừng từng nguồn) và xác minh trên Unity 6000.3; `panStereo` trên WebGL; load type / nén clip (SFX ngắn giải nén khi tải, nhạc nén/stream); phiên âm thanh iOS (tôn trọng công tắc im lặng, không ngắt nhạc app khác); kho thiết lập cục bộ (PlayerPrefs / IndexedDB trên WebGL).

## Formulas

Ký hiệu: `C.` = `AudioConfig`, `Q[x].` = asset `SoundCue` có `cueId` x, `T.` = `AudioBalanceTargets`. Giá trị mặc định do game-designer tự đề xuất (workflow không cho gọi specialist; chưa có agent audio) — **chưa được review cân bằng**, chốt bằng playtest (AC-34, AC-35). Hệ số 20 trong công thức dB và 12 nửa cung/quãng tám là hằng số toán học/âm nhạc, không phải knob.

### db_to_linear

`db_to_linear(dB) = 0 nếu dB ≤ silence_floor_db, ngược lại 10^(dB / 20)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| dB | float | −∞ đến 0 trong cấu hình hợp lệ | calculated | mức âm lượng theo dB |
| silence_floor_db | float | −100 đến −60 | SO field `AudioConfig.silenceFloorDb` | dưới mức này coi là im hẳn |
| 20 | float | 20 | constant | định nghĩa dB biên độ |

**Output range:** 0 đến 1 khi dB ≤ 0; dB > 0 cho kết quả > 1 nhưng bị kẹp ở `voice_gain` và validator cảnh báo.
**Example:** −6 dB → **0.501**; −12 dB → **0.251**; −80 dB → **0**.

### voice_gain

`total_db = Q.volumeDb + Σ_{b ∈ path(Q.bus)} ( bus_db(b) + user_db(b) + background_db(b) )`
`volume = min(db_to_linear(total_db), 1) × fade_factor × user_mute_gain`
với `bus_db(Music) = C.busDefaultDb[Sfx] + C.musicOffsetFromSfxDb`, các bus khác `bus_db(b) = C.busDefaultDb[b]`; `path` là chuỗi bus từ bus của cue lên Master (ví dụ Plinko → Gameplay → Sfx → Master).

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| Q.volumeDb | float (dB) | −40 đến 0 | SO field `SoundCue.volumeDb` | âm lượng riêng của cue |
| busDefaultDb[b] | float (dB) | −30 đến 0 | SO field `AudioConfig.busDefaultDb` | mức mặc định của bus |
| musicOffsetFromSfxDb | float (dB) | −18 đến −6 | SO field `AudioConfig.musicOffsetFromSfxDb` | nhạc so với SFX (D3) |
| user_db(b) | float (dB) | `C.userSliderMinDb` đến 0 | calculated (`slider_to_db`; chỉ Music, Sfx; MVP = 0) | thanh trượt người chơi |
| background_db(b) | float (dB) | `C.plinkoBackgroundOffsetDb` đến 0 | calculated = `C.plinkoBackgroundOffsetDb` × `fade_factor` khi bus Plinko ở chế độ nền, 0 với bus khác | giảm âm khi bàn không hiển thị (D5) |
| fade_factor | float | 0–1 | calculated (`fade_factor`) | fade-in sau tạm ngưng |
| user_mute_gain | float | 0–1 | calculated (`fade_factor` của lý do `User`) | 0 khi câm hẳn |

**Output range:** 0 đến 1 (kẹp). Với cấu hình mặc định, cue to nhất (`Gacha.RarityReveal`, −4 dB) cho 0.631.
**Example:** `Plinko.BucketX5` (−8 dB) khi bàn hiển thị: −8 + 0 → **0.398**; khi bàn ở Tab 2 (đã chuyển xong): −8 − 12 = −20 dB → **0.100**. `Music.ShopLoop` (0 dB, VS): 0 + (0 − 12) + 0 = −12 dB → **0.251**; thêm thanh trượt Nhạc = 0.5 (Alpha, −20 dB) → −32 dB → **0.025**.

### slider_to_db (Alpha)

`user_db = im (bus silent) nếu s ≤ 0, ngược lại C.userSliderMinDb × (1 − s)`; s = NaN → `C.defaultUserSlider`; s > 1 → 1.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| s | float | 0–1 | calculated (giá trị thanh trượt đã lưu, A12) | vị trí thanh trượt Nhạc hoặc SFX |
| userSliderMinDb | float (dB) | −60 đến −30 | SO field `AudioConfig.userSliderMinDb` | mức ở vị trí thấp nhất khác 0 |
| defaultUserSlider | float | 0.5–1.0 | SO field `AudioConfig.defaultUserSlider` | giá trị lần đầu |

**Output range:** `userSliderMinDb` (s → 0⁺) đến 0 dB (s = 1); s = 0 làm bus im và cue trên bus đó bị bỏ với `BusSilent`. Tuyến tính theo dB nên mỗi nấc trượt nghe đều tay.
**Example:** s = 0.75 → −40 × 0.25 = **−10 dB**; s = 0.5 → **−20 dB**; s = 0 → **im**.

### note_index

`p = clamp(pitchPosition, 0, 1)` (NaN hoặc không truyền → `C.fallbackPitchPosition`)
`n = floor(p × (C.scaleNoteCount − 1) + 0.5)`
`semitone(n) = 12 × floor(n / k) + C.scaleIntervals[n mod k]`, với k = số phần tử của `scaleIntervals`
`midi(n) = C.scaleRootMidi + semitone(n)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| pitchPosition | float | 0–1 | calculated (Feedback & Juice truyền; đề xuất `PegHit`: 1 − `PegHit.rowIndex` / (`PlinkoConfig.pegRows` − 1); `JackpotBurstSpawn`: `CoinSpawned.burstIndex` / (`jackpot_burst_count` − 1)) | 0 = nốt thấp nhất, 1 = cao nhất |
| fallbackPitchPosition | float | 0–1 | SO field `AudioConfig.fallbackPitchPosition` | dùng khi thiếu/NaN |
| scaleNoteCount | int | 5–15 | SO field `AudioConfig.scaleNoteCount` | số nốt trong thang (10 = 2 quãng tám ngũ cung) |
| scaleIntervals | int[] | mỗi phần tử 0–11, tăng dần | SO field `AudioConfig.scaleIntervals` | `[0, 2, 4, 7, 9]` ngũ cung trưởng |
| scaleRootMidi | int | 55–67 | SO field `AudioConfig.scaleRootMidi` | nốt gốc (60 = C4) |
| 12 | int | 12 | constant | nửa cung mỗi quãng tám |

**Output range:** n = 0…`scaleNoteCount` − 1; với mặc định semitone ∈ {0, 2, 4, 7, 9, 12, 14, 16, 19, 21}, tức C4–A5.
**Example:** `PegHit.rowIndex` = 3, `pegRows` = 12 → pitchPosition = 1 − 3/11 = 0.727 → n = floor(0.727 × 9 + 0.5) = floor(7.045) = **7** → semitone = 12 × 1 + 4 = **16** (E5).

### sample_pitch

`i* = argmin_i | semitone(n) − Q.pitchedSamples[i].nativeSemitone |` (bằng nhau → mẫu thấp hơn)
`shift = semitone(n) − Q.pitchedSamples[i*].nativeSemitone`
`pitch = 2^(shift / 12) × (1 + U(−Q.pitchJitterRatio, +Q.pitchJitterRatio))`
Ràng buộc validator: với mọi n, |shift| ≤ `C.maxSampleShiftSemitones`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| pitchedSamples[i].nativeSemitone | int | 0 đến semitone lớn nhất của thang | SO field `SoundCue.pitchedSamples` | nốt gốc của mẫu i: trầm 4, trung 12, cao 19 (cả `PegHitScored` lẫn `JackpotBurstSpawn`, nên bảng dưới đúng cho cả hai cue) |
| maxSampleShiftSemitones | int | 2–6 | SO field `AudioConfig.maxSampleShiftSemitones` | đổi pitch tối đa trước khi âm sắc "chipmunk" (D4: ±4) |
| pitchJitterRatio | float | 0–0.06 | SO field `SoundCue.pitchJitterRatio` | lệch ngẫu nhiên; `Plinko.PegHitScored` = 0.03 (chủ duy nhất; `plinko-board.md` chỉ tham chiếu field này) |
| U(a, b) | float | [a, b] | calculated | ngẫu nhiên đều |

Bảng chọn mẫu với mặc định:

| n | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
|---|---|---|---|---|---|---|---|---|---|---|
| semitone | 0 | 2 | 4 | 7 | 9 | 12 | 14 | 16 | 19 | 21 |
| mẫu | trầm | trầm | trầm | trầm | trung | trung | trung | cao | cao | cao |
| shift | −4 | −2 | 0 | +3 | −3 | 0 | +2 | −3 | 0 | +2 |

**Output range:** pitch ∈ [2^(−4/12) × 0.97, 2^(+4/12) × 1.03] = **[0.770, 1.298]** ở biên knob; với bảng trên thực tế [0.770, 1.225]. Luôn > 0.
**Example:** n = 7 → mẫu cao (19), shift = −3 → 2^(−0.25) = 0.8409; U = +0.01 → pitch = 0.8409 × 1.01 = **0.849**. `Plinko.JackpotBurstSpawn` với `jackpot_burst_count` = 5: `burstIndex` 0…4 → pitchPosition 0, 0.25, 0.5, 0.75, 1 → n = 0, 2, 5, 7, 9 → semitone 0, 4, 12, 16, 21 → mẫu chime trầm/trầm/trung/cao/cao, shift −4, 0, 0, −3, +2 → mọi |shift| ≤ `C.maxSampleShiftSemitones` (4); validator kiểm mọi n = 0…`scaleNoteCount` − 1 nên mọi `jackpot_burst_count` trong safe range đều hợp lệ.

### cue_admitted

`admitted = ready ∧ known ∧ ¬locked ∧ ¬suspended ∧ (¬user_muted ∨ Q.bus = Music) ∧ ¬bus_silent ∧ ¬(bus_background ∧ ¬Q.keepWhenBusBackground) ∧ (never_played ∨ t − last_played ≥ Q.minInterval) ∧ (instances < Q.maxInstances ∨ Q.instanceCapPolicy = StealOldest) ∧ voice_available`
Các vế được xét theo thứ tự A3; vế sai đầu tiên là lý do bỏ.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| t | double (s) | ≥ 0 | calculated (đồng hồ thực, A13) | thời điểm nhận yêu cầu |
| last_played | double (s) | ≥ 0 | calculated | lần **được phát** gần nhất của cue |
| Q.minInterval | float (s) | 0–0.5 | SO field `SoundCue.minInterval` | khoảng tối thiểu giữa hai lần phát |
| instances | int | 0–`maxInstances` | calculated | số bản cue đang phát |
| Q.maxInstances | int | 1–8 | SO field `SoundCue.maxInstances` | trần bản phát cùng lúc |
| voice_available | bool | — | calculated (voice rảnh, hoặc `steal_victim` tồn tại, hoặc bản cũ nhất của cùng cue khi `StealOldest`) | có chỗ phát |

**Output range:** bool, kèm một lý do bỏ khi sai.
**Example:** `Plinko.PegHitScored` được phát lúc t = 1.000 s; yêu cầu lúc 1.030 s → 0.030 < 0.04 → **bỏ (`RateLimited`)**, mốc vẫn là 1.000; yêu cầu lúc 1.040 s → được nhận.

### steal_victim

`victim = argmin_{v ∈ voice SFX đang phát, priority(v) < Q.priority} ( priority(v), startTime(v) )` (so theo thứ tự từ điển); tập rỗng → `NoVoice`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| Q.priority | int | 0–100 | SO field `SoundCue.priority` | ưu tiên cue mới |
| priority(v) | int | 0–100 | SO field `SoundCue.priority` của cue đang phát trên v | ưu tiên nạn nhân |
| startTime(v) | double (s) | ≥ 0 | calculated | lúc voice bắt đầu phát |

**Output range:** một voice hoặc không có.
**Example:** 15 voice SFX đều bận và mọi clip còn đang phát: 6 `PegHitScored` (60), 2 `PegHitUnscored` (10, bắt đầu 2.10 s và 2.20 s; lần được phát gần nhất 2.20 s), 1 `CoinSpawn` (50), 3 `BucketX2` (80), 3 voice khác ≥ 50. `JackpotHit` (100) đến lúc t = 2.30 s → nạn nhân là `PegHitUnscored` bắt đầu **2.10 s**. Một `PegHitUnscored` mới (10) đến lúc t = 2.35 s: đã qua `minInterval` (2.35 − 2.20 = 0.15 ≥ `Q[PegHitUnscored].minInterval` 0.10, A3.8), số bản 1 < `maxInstances` 2 (A3.9 không áp), không voice rảnh và không ai có ưu tiên thấp hơn hẳn 10 → **bỏ (`NoVoice`)**. Nếu nó đến trước 2.20 + 0.10 = 2.30 s thì lý do là **`RateLimited`** (A3.8 xét trước A3.10). Lưu ý: một `BucketX2` mới trong trạng thái này **không** bị `NoVoice` — 3 bản `BucketX2` đang phát = `maxInstances`, `StealOldest` → tự cướp bản cũ nhất (A3.9) và được phát; ca `NoVoice` của cue ưu tiên 80 cần trạng thái không có bản nào của chính cue đó (AC-12).

### cue_throughput

`admitted_rate = min(demand_rate, 1 / Q.minInterval)`
`needed_concurrency = admitted_rate × clip_length`
`steal_age = Q.maxInstances × Q.minInterval` (tuổi của bản cũ nhất khi bị cướp, áp dụng khi `needed_concurrency > Q.maxInstances` và `StealOldest`)
Ràng buộc 1 (validator): với mọi cue `StealOldest` có `priority` ≤ `C.stormPriorityCeiling`, `steal_age ≥ T.minStealAgeSeconds`.
Ràng buộc 2 (validator), áp cho `Plinko.CoinSpawn` và `Plinko.CoinSpawnAuto` (cùng clip "tok", Visual/Audio; cả hai `pitchMode = Random`):
`spawn_play_seconds(Q) = max_{clip ∈ Q.clips}(clip.length) / (1 − Q.pitchJitterRatio)` (thời gian kêu thực ở pitch thấp nhất của A6.2; clip kêu `length / pitch` giây)
2a. `spawn_play_seconds(Q) ≤ T.manualSpawnClipMaxSeconds`
2b. `T.manualSpawnClipMaxSeconds ≤ tap_cooldown − T.spawnClipMarginSeconds`
Hai cue này là `DropNew`, 1 bản (không cướp, nên Ràng buộc 1 không áp). 2a + 2b bảo đảm "tok" của cú chạm trước đã tắt trước cú chạm tay kế tiếp ít nhất `T.spawnClipMarginSeconds` — biên cho trễ bắt đầu theo khung / bộ đệm âm thanh (WebGL lượng tử hóa thời điểm bắt đầu theo bộ đệm Web Audio) và cho chênh giữa đồng hồ mô phỏng của Plinko (`tap_cooldown` đếm theo bước mô phỏng, Plinko R4.1) với đồng hồ thực của Audio (A13) — nên không cú chạm tay nào mất tiếng vì `InstanceCap`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| demand_rate | float (/s) | 0 đến `T.stormPegHitRate` | calculated; mục tiêu test `T.stormPegHitRate` | yêu cầu mỗi giây; PegHit lúc bão hòa ≈ `max_active_coins` (24) × ~14 đinh / `targetMedianFallTime` (2.5 s) ≈ 134/s; spam tay ≈ 6.67 × 14 ≈ 93/s (registry Plinko) |
| clip_length | float (s) | 0.1–`T.pegClipLengthMaxSeconds` | calculated (độ dài clip thật) | độ ngân của nốt |
| minStealAgeSeconds | float (s) | 0.1–0.3 | SO field `AudioBalanceTargets.minStealAgeSeconds` | nốt bị cắt phải đã tắt dần đủ lâu |
| clip.length | float (s) | > 0 | calculated (độ dài clip thật trong `Q.clips`) | độ dài clip "tok" ở pitch 1 |
| Q.pitchJitterRatio | float | 0–0.06 | SO field `SoundCue.pitchJitterRatio` | lệch cao độ của cue spawn (0.03); pitch thấp nhất = 1 − jitter |
| spawn_play_seconds | float (s) | > 0 | calculated | thời gian kêu thực dài nhất của cue spawn |
| manualSpawnClipMaxSeconds | float (s) | 0.05 đến `tap_cooldown` − `T.spawnClipMarginSeconds` (0.12) | SO field `AudioBalanceTargets.manualSpawnClipMaxSeconds` | thời gian kêu thực tối đa của "tok" spawn |
| spawnClipMarginSeconds | float (s) | 0.017–0.05 (≈ 1–3 khung ở 60 FPS) | SO field `AudioBalanceTargets.spawnClipMarginSeconds` | biên giữa lúc "tok" tắt và cú chạm tay kế tiếp |
| tap_cooldown | float (s) | 0.15 | registry `tap_cooldown` (Plinko sở hữu; validator Editor đọc để kiểm Ràng buộc 2b, runtime Audio không tham chiếu Plinko — A15) | khoảng tối thiểu giữa hai xu tay |

**Output range:** admitted_rate ≤ 1 / `minInterval` (PegHit: **25 nốt/s**).
**Example:** bão hòa: min(134, 25) = 25 nốt/s (18.7% số `PegHit` được nghe); needed_concurrency = 25 × 0.5 = 12.5 > 6 → cue bão hòa; steal_age = 6 × 0.04 = **0.24 s** ≥ 0.20 → nốt bị cắt luôn đã ngân ≥ 240 ms. Bảng cue mặc định chỉ có hai cue `StealOldest` ưu tiên ≤ 60: `PegHitScored` 0.24 s ✓ và `PegHitUnscored` 2 × 0.10 = **0.20 s** ≥ 0.20 ✓. `CoinSpawn` / `CoinSpawnAuto` là `DropNew` → không xét steal_age. Ràng buộc 2b: 0.12 ≤ 0.15 − 0.03 = **0.12** ✓ (vừa khít). Ràng buộc 2a với jitter 0.03: clip "tok" 0.11 s → 0.11 / 0.97 = **0.113 s** ≤ 0.12 ✓; clip 0.12 s → 0.12 / 0.97 = **0.124 s** > 0.12 → validator lỗi (hợp lệ nếu chỉ so `length`, vượt khi chia pitch thấp nhất); clip dài nhất được nhận = 0.12 × 0.97 = **0.116 s**.

### voice_budget

`sfx_voices = C.totalVoices − C.musicVoices − C.ambienceVoices`
`storm_instances = Σ_{c : Q[c].priority ≤ C.stormPriorityCeiling} Q[c].maxInstances`
Ràng buộc validator: `storm_instances ≤ sfx_voices − C.minFreeVoicesForRewards`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| totalVoices | int | 8–24 | SO field `AudioConfig.totalVoices` | tổng voice (D2: 16) |
| musicVoices | int | 0–2 | SO field `AudioConfig.musicVoices` | voice dành riêng cho nhạc |
| ambienceVoices | int | 0–1 | SO field `AudioConfig.ambienceVoices` | voice dành riêng cho ambience |
| stormPriorityCeiling | int | 40–70 | SO field `AudioConfig.stormPriorityCeiling` | cue có ưu tiên ≤ mức này là tiếng "nền" có thể dồn dập |
| minFreeVoicesForRewards | int | 2–6 | SO field `AudioConfig.minFreeVoicesForRewards` | số voice luôn còn rảnh cho tiếng thưởng mà không phải cướp |

**Output range:** ràng buộc đúng/sai.
**Example:** sfx_voices = 16 − 1 − 0 = **15**; storm_instances = PegHitScored 6 + CoinSpawn 1 + CoinSpawnAuto 1 + CoinFizzle 1 + BoardBusy 1 + PegHitUnscored 2 = **12** ≤ 15 − 3 = 12 → hợp lệ (vừa khít: thêm một bản cue nền phải tăng `totalVoices` hoặc giảm cue khác).

### storm_peak_gain

`storm_peak_gain = Σ_{c : Q[c].priority ≤ C.stormPriorityCeiling} Q[c].maxInstances × db_to_linear(Q[c].volumeDb + path_db(c))`
Ràng buộc validator: `storm_peak_gain ≤ T.stormPeakGainMax`. Bi quan có chủ đích: giả định mọi bản đều ở đỉnh cùng lúc (bỏ qua độ tắt dần).

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| path_db(c) | float (dB) | −30 đến 0 | calculated (tổng `bus_db` dọc đường, bàn hiển thị, thanh trượt mặc định) | |
| stormPeakGainMax | float | 1.5–3.0 | SO field `AudioBalanceTargets.stormPeakGainMax` | trần tổng biên độ tiếng nền, chống ồn kiểu casino và vỡ tiếng |

**Output range:** 0 đến vài đơn vị; mặc định 1.76.
**Example:** 6 × 0.1995 (−14) + 1 × 0.1995 (−14) + 1 × 0.100 (−20) + 1 × 0.0794 (−22) + 1 × 0.0794 (−22) + 2 × 0.0501 (−26) = 1.197 + 0.200 + 0.100 + 0.079 + 0.079 + 0.100 = **1.76** ≤ 2.0 → hợp lệ.

### fade_factor

`fade_in(t) = 1 nếu T ≤ 0, ngược lại clamp((t − t0) / T, 0, 1)`; `fade_out(t) = 1 − fade_in(t)`. Fade bắt đầu từ giá trị hiện tại (đảo chiều giữa chừng không nhảy).

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| T | float (s) | 0–4 | SO field `AudioConfig.resumeFadeInSeconds` / `.userMuteFadeSeconds` / `.busTransitionSeconds` / `.musicFadeInSeconds` | thời lượng fade |
| t0 | double (s) | ≥ 0 | calculated | lúc bắt đầu fade |

**Output range:** 0–1; T = 0 cho chuyển tức thì, không chia cho 0.
**Example:** `busTransitionSeconds` = 0.2, 0.05 s sau khi sang Tab 2 → f = 0.25 → background_db = −12 × 0.25 = **−3 dB**.

## Edge Cases

**Khởi tạo và nền tảng**
- **If `Play` được gọi trước khi Audio khởi tạo xong** (thứ tự khởi động sai): bỏ với `NotReady`, không ném lỗi, không xếp hàng. Bước `audio.service` chạy ở pha `ServiceInit`, trước mọi bước `GameplayInit` (Bootstrap B3), nên trường hợp này chỉ xảy ra khi thứ tự khởi động bị sai.
- **If `FirstInteraction` đến trước khi bước `audio.service` chạy** (người chơi chạm ngay khung đầu trên WebGL): Audio đã đăng nghe từ lúc đăng ký bước nên ghi nhớ nó; khởi tạo xong thì không vào Locked mà vào thẳng Active (A10). Không có cue nào bị phát bù.
- **If bước `audio.service` quá hạn `BootConfig.stepTimeoutSeconds`**: Bootstrap coi là bước lỗi, bỏ qua (không cốt lõi); Audio vào Failed như khi thiếu config, game chạy tiếp không tiếng.
- **If thiếu `AudioConfig` hoặc không tạo được voice**: Audio vào Failed; mọi `Play` → `NotReady`; game vẫn vào và chơi được không tiếng (Audio là phụ thuộc mềm của Bootstrap). Nút loa vẫn đổi và lưu trạng thái để lần sau đúng.
- **If WebGL chưa có thao tác đầu tiên**: mọi cue bị bỏ (`Locked`). Tiếng "tok" của cú chạm đầu có thể mất; xu vẫn rơi. Nhạc VS chỉ bắt đầu sau khi mở khóa, có fade-in.
- **If `Background` hoặc `Ad` bật trong lúc còn Locked**: hệ vẫn Locked; sau `FirstInteraction` vào Suspended nếu lý do còn bật, không phát tiếng nào.
- **If máy iOS đang bật công tắc im lặng**: game im (tôn trọng công tắc), không ngắt nhạc của app khác; trạng thái nút loa trong game không đổi. Cấu hình phiên âm thanh → ADR.
- **If nền tảng không hỗ trợ pan** (cần xác minh trên WebGL): pan = 0, mọi luật khác giữ nguyên.

**Dữ liệu cue**
- **If `cueId` không có trong kho**: bỏ với `UnknownCue`, đếm vào chẩn đoán, cảnh báo trong Editor; không ném lỗi trong build.
- **If cue có 0 clip, `maxInstances` < 1, `minInterval` < 0, `priority` ngoài 0–100, `cueId` trùng, hoặc cue `Scale` có nốt cần đổi pitch quá `maxSampleShiftSemitones`**: validator Editor báo lỗi; khi chạy, cue lỗi bị coi như không có (`UnknownCue`).
- **If `volumeDb` > 0 hoặc tổng dB của một voice > 0**: âm lượng bị kẹp ở 1; validator cảnh báo (không chặn).
- **If cấu hình vi phạm `voice_budget` hoặc `storm_peak_gain`, cue `StealOldest` nền vi phạm `steal_age`, clip "tok" spawn có thời gian kêu thực (`clip.length` / (1 − `Q.pitchJitterRatio`)) dài hơn `T.manualSpawnClipMaxSeconds`, hoặc `T.manualSpawnClipMaxSeconds` > `tap_cooldown` − `T.spawnClipMarginSeconds`** (`cue_throughput` Ràng buộc 1, 2a, 2b): validator báo lỗi; game vẫn chạy (luật cướp voice vẫn giữ tiếng thưởng nghe được) nhưng cấu hình không được merge.
- **If cue có ≥ 2 clip và `avoidRepeat`**: không phát lại đúng clip vừa phát; cue chỉ 1 clip thì bỏ qua cờ này.

**Tham số của yêu cầu**
- **If `pitchPosition` thiếu, NaN hay vô cực**: dùng `fallbackPitchPosition`; ngoài [0, 1] thì kẹp. Cue không phải `Scale` bỏ qua tham số này.
- **If `panPosition` NaN hay vô cực**: pan = 0; ngoài [−1, 1] thì kẹp.

**Dồn dập và voice**
- **If bàn Plinko bão hòa (~134 `PegHit`/s)**: chỉ tối đa 25 nốt/s được nhận (`minInterval` 40 ms), tối đa 6 bản cùng lúc; bản thứ 7 cướp bản cũ nhất của chính cue (đã ngân ≥ 0.24 s). `PegHit` không tính chỉ được tối đa 2 bản và bị cướp đầu tiên.
- **If hai yêu cầu cùng cue đến trong cùng một khung hình**: yêu cầu đến trước được phát, yêu cầu sau bị `RateLimited` (nếu `minInterval` > 0).
- **If mọi voice SFX đều bận bởi cue có ưu tiên ≥ cue mới**: cue mới bị bỏ (`NoVoice`), không cướp ngang hàng.
- **If `JackpotHit` đến khi 15 voice SFX đều bận**: luôn có nạn nhân (ưu tiên 100 là cao nhất), nên jackpot luôn được phát trừ khi chính nó vượt `maxInstances`/`minInterval` (hai jackpot cách nhau < 0.1 s → chỉ nghe một; hai tiếng sẽ hòa vào nhau dù sao).
- **If nhiều jackpot liên tiếp sinh nhiều Bùng Xu**: chime Bùng Xu bị giới hạn 3 bản, `minInterval` 0.06 s, nhanh hơn nhịp sinh xu loạt 0.08 s của Plinko nên mỗi xu thưởng vẫn có chime trừ khi bàn đang Tab 2/3.
- **If voice bị cướp đang ở giữa nốt**: dừng ngay, không fade (A5). Nguy cơ tiếng "click" thấp vì nạn nhân là tiếng nhỏ nhất và cũ nhất; nếu playtest nghe thấy click → Open Questions.

**Tab, tạm ngưng, câm**
- **If người chơi sang Tab 2/3 khi đang có nốt marimba ngân**: nốt không bị cắt, chỉ nhỏ dần −12 dB trong `busTransitionSeconds`; `PegHit` mới bị bỏ; tiếng vào hũ (đã gộp) và jackpot vẫn nghe, nhỏ hơn (D5). Không dùng lọc thông thấp.
- **If người chơi đổi tab liên tục nhanh hơn `busTransitionSeconds`**: fade đảo chiều từ mức hiện tại, không nhảy âm lượng; gọi lại cùng trạng thái không làm gì.
- **If cửa sổ trình duyệt chỉ mất focus nhưng vẫn hiển thị** (bấm ra ngoài canvas): không có `Paused`, âm thanh chạy tiếp (Boot D3).
- **If tab trình duyệt bị ẩn / app xuống nền**: mọi voice dừng tại chỗ, kể cả nhạc; quay lại thì tiếp tục từ vị trí cũ và tăng dần trong `resumeFadeInSeconds`. Tiếng "dở dang" không phát lại từ đầu.
- **If quảng cáo kết thúc trong khi tab vẫn ẩn**: Bootstrap phát `Paused` ngay khung `SetAdOpen(false)` (B6.10) → lý do `Background` bật; vẫn tạm ngưng tới khi có `Resumed`. Nếu `SetMuteReason(Ad, false)` đến trước `Paused` trong cùng khung, Audio có thể Active tối đa một khung với fade-in bắt đầu từ 0 — không nghe thấy; Platform Services nên gọi `SetAdOpen(false)` trước `SetMuteReason(Ad, false)` (Assumed).
- **If rewarded ad đang mở** (Alpha; Bootstrap B): không có `Paused`, mô phỏng chạy tiếp và Feedback vẫn gọi `Play` → mọi yêu cầu `Dropped(Suspended)`; voice SFX đã dừng hẳn lúc `Ad` bật, nhạc giữ vị trí. Quảng cáo đóng → nhạc fade-in từ vị trí cũ, tiếng bàn bắt đầu lại từ yêu cầu kế tiếp; không phát bù tiếng của khoảng quảng cáo (jackpot trong lúc đó chỉ mất tiếng, xu vẫn cộng).
- **If trở lại tab trong khi người chơi đang câm**: vẫn câm; nhạc tiếp tục chạy câm.
- **If người chơi bấm nút loa liên tục**: mỗi lần bấm đổi trạng thái và lưu ngay; fade đảo chiều từ mức hiện tại; `Ui.SoundOn` bị `minInterval` của nó chặn spam. Không có lợi ích gameplay nào để khai thác.
- **If thanh trượt Nhạc hoặc SFX = 0 (Alpha)**: bus đó im; cue SFX mới trên bus đó bị bỏ (`BusSilent`) để không tốn voice; nhạc vẫn chạy câm giữ vị trí.
- **If kho thiết lập cục bộ bị xóa** (xóa dữ liệu trình duyệt): về mặc định (không câm, thanh trượt `defaultUserSlider`); không ảnh hưởng tiến độ chơi.
- **If time scale ≠ 1** (dù Plinko cấm hit-stop toàn cục): `minInterval` và fade vẫn đo theo đồng hồ thực, không đổi.

**Nhạc (VS)**
- **If clip nhạc lỗi hoặc chưa tải xong**: không có nhạc, SFX không bị ảnh hưởng, voice nhạc để rảnh.
- **If bão hòa voice SFX trong lúc nhạc chạy**: voice nhạc là voice riêng, không bao giờ bị cướp.

## Dependencies

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Bootstrap & Scene Flow | Audio phụ thuộc Bootstrap (hợp đồng pha); Bootstrap không tham chiếu Audio | Soft (Audio chạy được khi không có sự kiện vòng đời: luôn Active trên mobile, Locked mãi trên WebGL; Bootstrap coi bước Audio là không cốt lõi) | Out (Audio đăng ký): bước `audio.service` ở pha `ServiceInit`, `IsCritical` = sai, kết quả `IsDone` / `IsFailed` (Ready / Failed) — A16. In: `Paused {reason}` / `Resumed {reason, awaySeconds}` (tab ẩn, app nền — Boot D3), `FirstInteraction`, `Interactive` (mốc nhạc VS, B5.3). Owner: Bootstrap (hợp đồng pha, sự kiện); Audio (nội dung bước, phản ứng). Khớp hàng Audio của `bootstrap-and-scene-flow.md`. |
| Feedback & Juice | Feedback phụ thuộc Audio | Hard (với Feedback) | In: `Play(cueId, {pitchPosition, panPosition})`, `SetBusBackground(Plinko, bool)`. Out: `PlayResult`. Owner: Audio (SoundCue, API, voice); Feedback (bảng sự kiện → `cueId`, `pitchPosition`, `panPosition`, gộp tiếng vào hũ, gọi chế độ nền khi `TabChanged`). |
| HUD & Tab Navigation | HUD phụ thuộc Audio | Soft (HUD vẽ được khi Audio lỗi) | In: `SetUserMuted(bool)`. Out: `IsUserMuted`, `MuteChanged`. Owner: Audio. Chưa có trong systems index. |
| Settings & Accessibility (Alpha) | Settings phụ thuộc Audio | Hard (với Settings) | In: `SetUserVolume(Music \| Sfx, s)`. Out: giá trị hiện tại. Owner: Audio (giá trị, lưu cục bộ); Settings (màn hình). |
| Rewarded Ads / Platform Services (Alpha) | Ads / Platform phụ thuộc Audio | Soft | In: `SetMuteReason(Ad, bool)` khi quảng cáo bắt đầu/kết thúc (cùng lúc `SetAdOpen` của Bootstrap B6.10; Bootstrap không phát `Paused` nên mô phỏng chạy tiếp, Audio chỉ tạm ngưng tiếng — quyết định user B). Owner: Audio. Chưa có trong systems index. |
| Plinko Board | Gián tiếp (qua Feedback & Juice) | Soft (Plinko chạy được khi câm) | Plinko không gọi Audio và Audio không nghe Plinko (D1). Plinko R6.3 vẫn đúng: giới hạn voice của `PegHit` thuộc Audio. Feedback & Juice dịch sự kiện R12 sang cue (bảng "Sự kiện Plinko → cue"). Hàng "Audio" của `plinko-board.md` (Revision 3) đã ghi gián tiếp qua Feedback & Juice và bàn ẩn → −12 dB (D1, D5) — khớp. |
| Gacha Machine, Shelf & Synergy | Gián tiếp (qua Feedback & Juice) | — | Tiếng của các hệ này là SoundCue trong kho Audio; Feedback map sự kiện của chúng. |
| Object Pooling | Không phụ thuộc | — | Pool D2: Audio tự quản voice. |
| Save/Load (VS) | Không phụ thuộc | — | Thiết lập âm thanh lưu ở kho thiết lập cục bộ riêng, không nằm trong save tiến độ (Assumed). |

**Hai chiều**: Bootstrap & Scene Flow (`bootstrap-and-scene-flow.md`) liệt kê Audio với bước `ServiceInit` không cốt lõi và `Paused`/`Resumed`, `FirstInteraction`, `Interactive` (mốc nhạc VS) — khớp; Plinko Board (`plinko-board.md`) liệt kê Audio là gián tiếp qua Feedback & Juice — khớp; Object Pooling (`object-pooling.md`) ghi không phụ thuộc — khớp. Chưa có GDD nào trong số Feedback & Juice, HUD & Tab Navigation, Settings & Accessibility, Rewarded Ads, Platform Services — khi viết, mỗi GDD phải liệt kê Audio với cùng interface ở trên.

## Tuning Knobs

Ba loại asset: `AudioConfig` (runtime, một asset), `SoundCue` (runtime, một asset mỗi cue, D1) và `AudioBalanceTargets` (chỉ dùng cho test, validator và playtest; không ảnh hưởng gameplay). Không có số nào trong code; validator Editor kiểm tra safe range, `voice_budget`, `storm_peak_gain`, `steal_age`, thời gian kêu thực của clip "tok" spawn và biên của nó so với `tap_cooldown` (`cue_throughput` Ràng buộc 2a, 2b) và giới hạn đổi pitch của mọi cue `Scale`. Knob do hệ khác sở hữu không lặp ở đây: cửa sổ gộp tiếng vào hũ = `PlinkoFeedbackConfig.floatingTextMergeWindow` (Plinko/Feedback), khoảng phản hồi bàn bận = `PlinkoFeedbackConfig.busyFeedbackMinInterval` (Plinko), độ rung haptics `hapticTapMs` (Feedback). Hai giá trị chỉ có một chủ là SoundCue (D1, C1[11]): lệch cao độ chạm đinh = `SoundCue[Plinko.PegHitScored].pitchJitterRatio`; chênh âm lượng spawn tự động = `SoundCue[Plinko.CoinSpawnAuto].volumeDb` − `SoundCue[Plinko.CoinSpawn].volumeDb` (−6 dB, giá trị suy ra từ hai knob, không có knob offset riêng). `plinko-board.md` (Revision 3) đã bỏ `pegPitchJitter` và `autoSpawnVolumeOffsetDb` khỏi `PlinkoFeedbackConfig` và chỉ tham chiếu các field SoundCue này.

Hằng số (không phải knob): spatialBlend = 0 (game 2D, A1.3); 20 trong công thức dB và 12 nửa cung/quãng tám (định nghĩa toán học); `requiresUnlock` theo nền tảng (chính sách autoplay của trình duyệt); 0 B GC mỗi khung (luật dự án, Plinko AC-38).

**Voice và bus** (`AudioConfig`)

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Tổng voice | `AudioConfig.totalVoices` | int | 16 | 8–24 | Nhiều node Web Audio, tốn CPU mobile (D2) | Tiếng thưởng phải cướp liên tục; vi phạm `voice_budget` | musicVoices, maxInstances các cue nền |
| Voice nhạc | `AudioConfig.musicVoices` | int | 1 | 0–2 | Bớt voice SFX | VS không có chỗ phát nhạc | totalVoices |
| Voice ambience | `AudioConfig.ambienceVoices` | int | 0 | 0–1 | Bớt voice SFX cho bus chưa dùng | — | totalVoices |
| Voice rảnh cho tiếng thưởng | `AudioConfig.minFreeVoicesForRewards` | int | 3 | 2–6 | Cue nền bị bóp quá chặt | Jackpot/hũ hay phải cướp | stormPriorityCeiling |
| Ngưỡng cue "nền" | `AudioConfig.stormPriorityCeiling` | int | 60 | 40–70 | Tiếng vào hũ bị tính là nền | `PegHit` được tính lọt khỏi kiểm tra ngân sách | priority các cue |
| Sàn im lặng | `AudioConfig.silenceFloorDb` | float (dB) | −80 | −100 đến −60 | Tiếng rất nhỏ bị coi là im | — | db_to_linear |
| Mức bus mặc định | `AudioConfig.busDefaultDb[Master/Sfx/Gameplay/Plinko/Gacha/Ui]` | float (dB) | 0 / 0 / 0 / 0 / 0 / 0 | −12–0 mỗi bus | > 0 dễ vỡ tiếng | Cả nhóm quá nhỏ | volumeDb các cue |
| Mức bus Ambience (dự trữ) | `AudioConfig.busDefaultDb[Ambience]` | float (dB) | −18 | −30 đến −6 | Ambience lấn SFX | — | — |
| Nhạc so với SFX | `AudioConfig.musicOffsetFromSfxDb` | float (dB) | −12 | −18 đến −6 | Nhạc che tiếng xu (phá Pillar 1) | Nhạc không nghe thấy | busDefaultDb[Sfx] |
| Giảm bus Plinko khi bàn ẩn | `AudioConfig.plinkoBackgroundOffsetDb` | float (dB) | −12 | −18 đến −6 | Tab 2/3 ồn, khó tập trung sắp kệ | Mất cảm giác "tiền vẫn chạy" | keepWhenBusBackground |
| Thời gian chuyển bus nền | `AudioConfig.busTransitionSeconds` | float (s) | 0.2 | 0.05–0.5 | Đổi tab mà tiếng còn to lâu | Âm lượng nhảy giật | — |
| Fade câm/bật tiếng | `AudioConfig.userMuteFadeSeconds` | float (s) | 0.15 | 0–0.3 | Bấm loa thấy phản hồi trễ | Tiếng tắt cụt có click | — |
| Fade khi quay lại | `AudioConfig.resumeFadeInSeconds` | float (s) | 0.3 | 0–1.0 | Mất tiếng đầu tiên sau khi quay lại | Tiếng bật to đột ngột | — |
| Fade-in nhạc (VS) | `AudioConfig.musicFadeInSeconds` | float (s) | 2.0 | 0.5–4.0 | Nhạc vào quá chậm | Nhạc bật đột ngột sau cú chạm đầu | — |
| Pan tối đa | `AudioConfig.maxPan` | float | 0.3 | 0–0.6 | Tai nghe nghe lệch, mệt | Mất cảm giác vị trí xu | panFromPosition |
| Tâm cao độ mặc định | `AudioConfig.fallbackPitchPosition` | float | 0.5 | 0–1 | — | — | note_index |

**Thang âm** (`AudioConfig`, D4)

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Nốt gốc | `AudioConfig.scaleRootMidi` | int (MIDI) | 60 (C4) | 55–67 | Marimba chói | Marimba đục, mất "lách cách" | pitchedSamples, nhạc VS (cùng tông) |
| Khoảng thang | `AudioConfig.scaleIntervals` | int[] | [0, 2, 4, 7, 9] | chỉ thang ngũ cung | Thêm quãng nghịch → nốt chồng nghe chỏi | — | scaleNoteCount |
| Số nốt | `AudioConfig.scaleNoteCount` | int | 10 | 5–15 | Thang rộng, mẫu phải đổi pitch nhiều | Ít nốt, lặp đơn điệu | maxSampleShiftSemitones |
| Đổi pitch tối đa mỗi mẫu | `AudioConfig.maxSampleShiftSemitones` | int | 4 | 2–6 | Nốt "chipmunk"/ì (D4) | Cần nhiều mẫu hơn | pitchedSamples |

**Thanh trượt người chơi** (`AudioConfig`, Alpha; D6)

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Mức thấp nhất của thanh trượt | `AudioConfig.userSliderMinDb` | float (dB) | −40 | −60 đến −30 | Nấc đầu vẫn to | Phần lớn thanh trượt gần như im | slider_to_db |
| Thanh trượt mặc định | `AudioConfig.defaultUserSlider` | float | 1.0 | 0.5–1.0 | — | Lần đầu nghe nhỏ, người chơi tưởng lỗi | busDefaultDb |
| Câm mặc định | `AudioConfig.defaultUserMuted` | bool | false | — | — | — | A10 (WebGL đã im tới cú chạm đầu) |

**Trường của SoundCue** (khoảng an toàn chung cho mọi cue)

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Âm lượng cue | `SoundCue.volumeDb` | float (dB) | xem bảng cue | −40–0 | Vỡ tiếng, lấn cue khác | Không nghe thấy | storm_peak_gain |
| Ưu tiên | `SoundCue.priority` | int | xem bảng cue | 0–100 | Cue nền cướp tiếng thưởng | Tiếng quan trọng bị cướp | steal_victim, stormPriorityCeiling |
| Trần bản phát | `SoundCue.maxInstances` | int | xem bảng cue | 1–8 | Tốn voice, ồn | Nốt bị cắt sớm | voice_budget, steal_age |
| Khoảng tối thiểu | `SoundCue.minInterval` | float (s) | xem bảng cue | 0–0.5 | Mất nhịp, nghe "thưa" | Dồn dập, ồn kiểu casino | cue_throughput |
| Lệch cao độ | `SoundCue.pitchJitterRatio` | float | xem bảng cue | 0–0.06 | Lạc giọng | Lặp máy móc | sample_pitch |
| Nốt gốc các mẫu (bắt buộc với mọi cue `Scale`) | `SoundCue.pitchedSamples[i].nativeSemitone` | int | `Plinko.PegHitScored`: 4 / 12 / 19 (3 mẫu marimba); `Plinko.JackpotBurstSpawn`: 4 / 12 / 19 (3 mẫu chime) | sao cho mọi nốt |shift| ≤ `maxSampleShiftSemitones` | — | Cue `Scale` thiếu mẫu → validator lỗi | scaleIntervals, scaleNoteCount, sfxDownloadBudgetKb |

**Bảng cue v1.0** (giá trị hiện tại; cue Gacha/UI là *provisional* tới khi có GDD Gacha, HUD)

| CueId | Bus | volumeDb | priority | maxInstances | minInterval (s) | capPolicy | pitchMode / jitter | keep khi bus nền | Mốc |
|---|---|---|---|---|---|---|---|---|---|
| `Plinko.JackpotHit` | Plinko | −6 | 100 | 2 | 0.10 | DropNew | Random / 0.02 | có | MVP |
| `Gacha.RarityReveal.<phẩm cấp>` | Gacha | −4 | 90 | 1 | 0 | StealOldest | None | — | MVP |
| `Gacha.Crank` / `Gacha.EggPop` | Gacha | −8 / −6 | 90 | 1 | 0.05 | StealOldest | Random / 0.03 | — | MVP |
| `Plinko.JackpotBurstSpawn` | Plinko | −12 | 85 | 3 | 0.06 | StealOldest | Scale / 0.01 | không | MVP |
| `Plinko.BucketX5` / `X2` / `X1` | Plinko | −8 / −9 / −10 | 80 | 3 | 0.03 | StealOldest | Random / 0.02 | có | MVP |
| `Ui.Confirm` / `Ui.SoundOn` | Ui | −10 | 70 | 2 | 0.10 | DropNew | None | — | MVP |
| `Ui.Tap` | Ui | −14 | 70 | 2 | 0.05 | StealOldest | Random / 0.02 | — | MVP |
| `Plinko.PegHitScored` | Plinko | −14 | 60 | 6 | 0.04 | StealOldest | Scale / 0.03 | không | MVP |
| `Plinko.CoinSpawn` | Plinko | −14 | 50 | 1 | 0.05 | DropNew | Random / 0.03 | không | MVP |
| `Plinko.CoinSpawnAuto` | Plinko | −20 | 50 | 1 | 0.10 | DropNew | Random / 0.03 | không | MVP |
| `Plinko.CoinFizzle` | Plinko | −22 | 45 | 1 | 0.10 | DropNew | Random / 0.03 | không | MVP |
| `Plinko.BoardBusy` | Plinko | −22 | 40 | 1 | 0.10 | DropNew | None | không | MVP |
| `Plinko.PegHitUnscored` | Plinko | −26 | 10 | 2 | 0.10 | StealOldest | Random / 0.05 | không | MVP |
| `Plinko.RainLoop` (loop) | Plinko | −16 | 55 | 1 | 0 | DropNew | None | không | VS |
| `Music.ShopLoop` (loop) | Music | 0 | voice riêng | 1 | 0 | — | None | — | VS |

D2 được giữ: `PegHitScored` tối đa 6 bản và cách nhau ≥ 40 ms; `PegHitUnscored` có ưu tiên thấp nhất (2 bản, cách nhau ≥ 100 ms để bản bị cướp đã ngân ≥ `T.minStealAgeSeconds`). `Plinko.CoinSpawn` là `DropNew` 1 bản với thời gian kêu thực (clip / pitch thấp nhất) ≤ `T.manualSpawnClipMaxSeconds` ≤ `tap_cooldown` − `T.spawnClipMarginSeconds`: không bao giờ cắt "tok" đang kêu, và cú chạm tay kế tiếp (sau `tap_cooldown`) luôn thấy bản trước đã tắt từ ít nhất `T.spawnClipMarginSeconds` trước đó. Hiệu −6 dB giữa `CoinSpawnAuto` và `CoinSpawn` chỉ do hai `volumeDb` này quyết định; Plinko Visual/Audio ghi "nhỏ hơn 6 dB" là tham chiếu tới bảng này, không phải knob thứ hai. `Plinko.RainLoop` (VS) có ưu tiên 55 ≤ `stormPriorityCeiling` nên sẽ đưa `storm_instances` lên 13 > 12: khi thêm cue này ở VS phải giải lại `voice_budget` (giảm `PegHitUnscored` xuống 1 bản, hoặc tăng `totalVoices`); validator chặn tới lúc đó (Open Questions).

**Mục tiêu kiểm thử** (`AudioBalanceTargets`, chỉ dùng cho test / validator / playtest)

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Tần suất `PegHit` lúc bão hòa | `AudioBalanceTargets.stormPegHitRate` | float (/s) | 134 | 90–200 | Test nặng hơn thực tế | Không thử đúng mức bão hòa | Plinko `max_active_coins`, `targetMedianFallTime` |
| Độ dài clip marimba tối đa | `AudioBalanceTargets.pegClipLengthMaxSeconds` | float (s) | 0.5 | 0.3–0.8 | Cần nhiều bản hơn, cắt nhiều | Nốt cụt, mất ngân | cue_throughput |
| Tuổi tối thiểu khi bị cướp | `AudioBalanceTargets.minStealAgeSeconds` | float (s) | 0.2 | 0.1–0.3 | Ép `maxInstances` lớn | Nốt bị cắt khi còn to (click) | maxInstances, minInterval |
| Thời gian kêu thực tối đa của "tok" spawn | `AudioBalanceTargets.manualSpawnClipMaxSeconds` | float (s) | 0.12 | 0.05 đến registry `tap_cooldown` − `spawnClipMarginSeconds` (0.12) | > `tap_cooldown` − margin: cú chạm tay kế tiếp có thể gặp bản trước còn kêu → `InstanceCap`, mất tiếng phản hồi (Pillar 1); validator chặn (Ràng buộc 2b) | "Tok" cụt, mất chất gỗ | Q[CoinSpawn/CoinSpawnAuto] clip và `pitchJitterRatio`, `tap_cooldown`, spawnClipMarginSeconds |
| Biên "tok" tắt trước cú chạm kế tiếp | `AudioBalanceTargets.spawnClipMarginSeconds` | float (s) | 0.03 | 0.017–0.05 (≈ 1–3 khung ở 60 FPS) | Clip "tok" phải ngắn hơn, mất đuôi gỗ | Trễ khung / bộ đệm Web Audio / chênh đồng hồ mô phỏng–thực làm "tok" trước còn kêu → `InstanceCap` | manualSpawnClipMaxSeconds, `tap_cooldown` (Plinko safe 0.10–0.25: ở 0.10 thì trần "tok" còn 0.07 s) |
| Trần tổng biên độ tiếng nền | `AudioBalanceTargets.stormPeakGainMax` | float | 2.0 | 1.5–3.0 | Vỡ tiếng, ồn kiểu casino | Tiếng nền quá nhỏ | volumeDb cue nền |
| Thời lượng test bão hòa | `AudioBalanceTargets.stormTestSeconds` | float (s) | 10 | 5–60 | Test chậm | Không đủ mẫu | — |
| Trễ yêu cầu → voice phát | `AudioBalanceTargets.requestToStartMaxFrames` | int (khung) | 0 | 0–1 | Tiếng lệch khỏi khoảnh khắc (Plinko `resolveToFeedbackMaxMs` 34 ms) | — | — |
| Ngân sách CPU âm thanh (p95) | `AudioBalanceTargets.audioFrameBudgetMs` | float (ms) | 1.0 (*provisional* — technical-director) | 0.5–2.0 | Lấn ngân sách khung 16.7 ms | — | totalVoices |
| Dung lượng tải SFX (MVP) | `AudioBalanceTargets.sfxDownloadBudgetKb` | int (KB) | 600 (*provisional* — ADR build/web) | 300–1000 | Tải đầu chậm trên Poki | — | số clip (gồm 3 mẫu marimba + 3 mẫu chime của hai cue `Scale`), nén |
| Dung lượng tải nhạc (VS) | `AudioBalanceTargets.musicDownloadBudgetKb` | int (KB) | 2000 (*provisional*) | 1000–2500 | Tải đầu chậm | Nhạc nén quá, rè | nén nhạc |
| Điểm "êm tai" playtest tối thiểu | `AudioBalanceTargets.playtestCalmScoreMin` | float (thang 1–5) | 4.0 | 3.5–4.5 | — | Chấp nhận tiếng ồn | — |
| Tỉ lệ nhận ra jackpot trong bão hòa | `AudioBalanceTargets.playtestJackpotRecognitionMin` | float | 0.8 | 0.6–1.0 | — | Jackpot bị nuốt | priority JackpotHit |
| Số người playtest | `AudioBalanceTargets.playtestSampleSize` | int | 5 | 5–10 | — | Kết luận không tin được | — |
| Tỉ lệ nhận ra bàn vẫn chạy khi ở Tab 2 | `AudioBalanceTargets.playtestBackgroundAwarenessMin` | float | 0.6 | 0.4–0.8 | — | Chấp nhận D5 không tạo được cảm giác "tiền vẫn chạy" | plinkoBackgroundOffsetDb |
| Số người thấy tiếng nền làm phiền (tối đa) | `AudioBalanceTargets.playtestBackgroundAnnoyedMax` | int | 0 | 0–1 | Chấp nhận Tab 2/3 ồn | — | plinkoBackgroundOffsetDb |

## Visual/Audio Requirements

Chưa có agent audio: cột âm thanh là **ý đồ** cho người làm/chọn asset, không phải đặc tả âm học. Chất liệu chung: marimba, kalimba, glockenspiel, gỗ khô — tiếng "đồ chơi gỗ trên kệ tiệm tạp hóa" (Visual Identity Anchor của concept). Anti-reference: còi hú, coin-shower kiểu casino, tiếng điện tử 8-bit. Mọi cue có cao độ ở tông `scaleRootMidi`. Clip SFX ngắn, đầu tiếng sắc (attack < 10 ms) để không cảm thấy trễ; đuôi tắt tự nhiên trong ≤ `pegClipLengthMaxSeconds` với cue nền. Hình ảnh của các sự kiện gameplay thuộc Plinko / Feedback & Juice; Audio chỉ có phần hình ảnh của nút loa.

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| `Plinko.PegHitScored` | (Plinko: đinh sáng kem) | Một nốt marimba ngũ cung, ngân ngắn; 3 mẫu gốc trầm/trung/cao thu cùng một cây đàn, cùng lực gõ; nốt đi xuống khi xu rơi (cao độ theo `PegHit.rowIndex`) | MVP |
| `Plinko.PegHitUnscored` | — | "Tóc" gỗ trầm rất nhỏ, gần như tiếng nền | MVP |
| `Plinko.CoinSpawn` / `CoinSpawnAuto` | (Plinko: xu pop-in) | "Tok" gỗ khô ngắn; thời gian kêu thực cả đuôi ở pitch thấp nhất ≤ `T.manualSpawnClipMaxSeconds` (0.12 s, tức clip ≤ 0.116 s với `pitchJitterRatio` 0.03; tắt trước `tap_cooldown` ít nhất `T.spawnClipMarginSeconds`); bản Auto dùng cùng clip, nhỏ hơn 6 dB | MVP |
| `Plinko.BucketX1` / `X2` / `X5` | (Plinko: hũ punch, số bay) | x1 "clunk" gỗ; x2 hai nốt marimba đi lên; x5 arpeggio ba nốt — tăng dần độ "thưởng" theo hệ số | MVP |
| `Plinko.JackpotHit` | (Plinko: hũ sáng mật ong, "BONUS!") | Arpeggio kalimba/glockenspiel rồi tiếng mưa xu nhẹ; to nhất bàn nhưng tròn, không chói | MVP |
| `Plinko.JackpotBurstSpawn` | (Plinko: xu viền mật ong) | Chime nhỏ, cao độ đi lên theo thứ tự xu trong loạt (`pitchPosition` = `CoinSpawned.burstIndex` / (`jackpot_burst_count` − 1)); **3 mẫu chime gốc** trầm/trung/cao (nốt gốc 4 / 12 / 19 trên `scaleRootMidi`, tức E4 / C5 / G5) thu cùng một chuông, cùng lực gõ — cùng cấu trúc với 3 mẫu marimba, tính vào `T.sfxDownloadBudgetKb` | MVP |
| `Plinko.CoinFizzle` | (Plinko: xu tan, theo `CoinStuck`) | "Phụt" hơi rất nhỏ | MVP |
| `Plinko.BoardBusy` | (Plinko: khấc vùng thả lắc, theo `BoardBusy`) | Gỗ bị bịt rất khẽ | MVP |
| `Gacha.Crank` / `Gacha.EggPop` *(provisional)* | (Gacha) | "Cạch… Cạch…" bánh răng gỗ; "BỤP!" vỏ trứng nhựa bật | MVP |
| `Gacha.RarityReveal.<phẩm cấp>` *(provisional)* | (Gacha: ánh sáng phẩm cấp) | Chuông, độ dài và số nốt tăng theo phẩm cấp; Huyền Thoại có thêm lớp ngân dài | MVP |
| `Ui.Tap` / `Ui.Confirm` | (HUD) | Gõ gỗ mềm / hai nốt matcha xác nhận | MVP |
| Câm / bật tiếng | Icon loa ở header đổi giữa "loa có sóng" và "loa gạch chéo", màu chữ nâu `#3D312A` (không dùng màu nhấn — Visual principle 3); đổi icon ngay khi bấm | Bật lại: `Ui.SoundOn` (một nốt marimba nhẹ) sau khi fade xong; tắt: im | MVP |
| `Plinko.RainLoop` | (Plinko: dải mây ☁️) | Gậy mưa chạy nền khi còn xu `Rain` trong hàng đợi | VS |
| `Music.ShopLoop` | — | Một loop acoustic/lo-fi nhẹ (guitar mộc, marimba, lắc gỗ), cùng tông `scaleRootMidi`, nhịp chậm, không lời, lặp liền mạch; nghe dưới SFX 12 dB | VS |
| Bàn Plinko không hiển thị (Tab 2/3) | — | Tiếng vào hũ và jackpot "vọng từ quầy" (−12 dB); không còn tiếng chạm đinh | MVP |

## Game Feel

Không bắt buộc với hệ Category Audio (chỉ Gameplay/UI); ghi ngắn vì Audio quyết định cảm giác ASMR của Pillar 1.
- **Feel reference**: video ASMR đồ chơi gỗ / marimba; tiếng "thunk" khi trái cây đáp trong Suika Game (cùng reference với Plinko). **Anti-reference**: máy slot/pachinko casino (tiếng dồn dập không nhịp, còi), tiếng vụn khi mỗi va chạm đều được phát.
- **Mật độ có nhịp, không phải từng va chạm**: ở bão hòa người chơi nghe một "cơn mưa marimba" ≤ 25 nốt/s hòa âm (ngũ cung), không nghe từng xu riêng — đánh đổi đã chấp nhận ở D2.
- **Thưởng luôn nổi lên**: jackpot và tiếng vào hũ ưu tiên cao, luôn cướp được tiếng nền (A5).

Input responsiveness:

| Action | Max input-to-response (ms) | Frames at target fps | Notes |
|---|---|---|---|
| `Play` được nhận → voice bắt đầu phát | `requestToStartMaxFrames` (0 khung thêm) | 0 | Cùng khung với yêu cầu; độ trễ bộ đệm âm thanh của nền tảng không tính (→ ADR) |
| Bấm nút loa → icon đổi | Ngân sách phản hồi nút của HUD & Tab Navigation | — | Icon đổi theo `MuteChanged` phát cùng khung với `SetUserMuted`; âm thanh fade trong `userMuteFadeSeconds` |

Impact moments: không có (Audio không dùng hit-stop, rung hay camera).

## UI Requirements

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Nút loa (trạng thái câm) | Header, góc phải, cạnh Xu / CPS / Boost (vị trí cuối do HUD & Tab Navigation và Penpot chốt) | Khi `MuteChanged` | Luôn hiện từ MVP |
| Thanh trượt Nhạc, SFX | Màn Settings | Khi kéo (giá trị lưu khi thả) | Alpha |
| Trạng thái Locked trên WebGL | Không hiển thị riêng (cú chạm đầu vào bàn đã là gợi ý tự nhiên) | — | — |

UX flag: this system has UI. A UX spec per screen is recommended before epics; UX authoring is not in this plugin yet, so describe each screen in this section or a short spec next to the GDD. Board header trên Penpot (`Main_Plinko`) hiện chưa có nút loa — cần thêm (Open Questions); thanh trượt thuộc spec màn Settings (Alpha).

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Pillar 1, Anti-Pillars, Visual Identity Anchor, MVP Definition, Audio needs | `Docs/design/gdd/game-concept.md` | Kinetic ASMR; tông gỗ/cozy; "Moderate — SFX ASMR là trọng tâm" | Ràng buộc thiết kế |
| Giới hạn voice chạm đinh, sự kiện → cue | `Docs/design/gdd/plinko-board.md` | R6.3 (Audio giới hạn voice), R11 (bàn chạy tiếp ở Tab 2/3 và khi rewarded ad mở, dừng khi `Paused`; R11.3 xả hàng đợi loạt bằng `CoinResolved(reason = Overflow)`), R12 (sự kiện `CoinSpawned {origin, burstIndex}`, `PegHit {rowIndex, scored}`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy`; Plinko không gọi Audio), Visual/Audio Requirements (tham chiếu `SoundCue[Plinko.PegHitScored].pitchJitterRatio` và bảng cue), `PlinkoConfig.pegRows` / `pegsPerRow` / `pegSpacing`, `floatingTextMergeWindow`, `busyFeedbackMinInterval`, `burstSpawnInterval` | Audio thực thi; Plinko đã khớp D1, D5 (Revision 3) |
| Tần suất bão hòa | `Docs/design/registry/entities.yaml` | `max_active_coins` = 24, `tap_cooldown` = 0.15, `max_scored_peg_hits` = 20, `coin_rate_ceiling`, `jackpot_burst_count` = 5 | Đầu vào ước lượng `stormPegHitRate`; không đổi giá trị |
| Vòng đời ứng dụng, hợp đồng pha | `Docs/design/gdd/bootstrap-and-scene-flow.md` | B2 (`IBootStep`), B3 (pha `ServiceInit`), B4.2 (bước không cốt lõi), B6 (`Paused {reason}`, `Resumed {reason, awaySeconds}`), B9.2 (`TabChanged {previous, current}`, tab `Board`), B12.3 (`FirstInteraction`), B5.3 (`Interactive`), `BootConfig.stepTimeoutSeconds` (Boot D1, D3) | Audio đăng ký bước `audio.service` và nghe sự kiện |
| Map sự kiện → cue | `Docs/design/gdd/feedback-and-juice.md` *(chưa có)* | Bảng sự kiện → `cueId`, `pitchPosition`, `panPosition`, gọi `SetBusBackground` | Feedback gọi API Audio |
| Nút loa | `Docs/design/gdd/hud-and-tab-navigation.md` *(chưa có)* | Icon header, `SetUserMuted` | HUD gọi API Audio |
| Thanh trượt | `Docs/design/gdd/settings-and-accessibility.md` *(chưa có)* | `SetUserVolume` | Settings gọi API Audio (Alpha) |
| Tắt tiếng khi quảng cáo | `Docs/design/gdd/rewarded-ads.md`, `Docs/design/gdd/platform-services.md` *(chưa có)*; `Docs/design/gdd/bootstrap-and-scene-flow.md` B6.10 | `SetMuteReason(Ad, bool)`; cờ "ad đang mở" (`SetAdOpen`, không `Paused` — quyết định user B) | Ads/Platform gọi API Audio (Alpha); mô phỏng chạy tiếp |
| Voice tự quản | `Docs/design/gdd/object-pooling.md` | Pool D2, R1.2 (voice âm thanh không thuộc Pooling), cảnh báo prefab có AudioSource | Không phụ thuộc |
| Cue Gacha | `Docs/design/gdd/gacha-machine.md` *(chưa có)* | Nhịp "Cạch… Cạch… BỤP!", phẩm cấp | Cue ID provisional |

## Acceptance Criteria

Viết bởi game-designer với vai trò qa-lead (workflow không cho gọi specialist). Ký hiệu `C.` = `AudioConfig`, `Q[x].` = `SoundCue` x, `T.` = `AudioBalanceTargets`. Test EditMode dùng **backend voice giả** (ghi lại clip, volume, pitch, pan, trạng thái phát của từng voice thay cho AudioSource thật) và **đồng hồ tiêm** — cách tách backend → ADR. Các con số trong ngoặc là giá trị hiện tại của knob, không phải literal của test. Sai số so sánh số thực: 1e-4.

**Bus và âm lượng (A1, db_to_linear, voice_gain, fade_factor)**
- `AC-1 [EditMode]` **GIVEN** `C` mặc định, **WHEN** tra đường bus của mọi cue trong kho và mức dB hiệu dụng của từng bus, **THEN** mỗi cue có đúng một đường kết thúc ở Master, và dB(Music) − dB(Sfx) = `C.musicOffsetFromSfxDb` (−12).
- `AC-2 [EditMode]` **GIVEN** `db_to_linear`, **WHEN** đưa vào 0, −6, −12, `C.silenceFloorDb`, `C.silenceFloorDb` − 20, **THEN** nhận 1, 0.5012, 0.2512, 0, 0.
- `AC-3 [EditMode]` **GIVEN** backend giả, `C` mặc định, **WHEN** phát `Plinko.BucketX5` khi bus Plinko Normal, rồi khi bus Plinko đã ở Background quá `C.busTransitionSeconds`, và phát `Music.ShopLoop`, **THEN** volume của voice lần lượt = db_to_linear(`Q[BucketX5].volumeDb`) (0.398), db_to_linear(`Q[BucketX5].volumeDb` + `C.plinkoBackgroundOffsetDb`) (0.100), db_to_linear(`Q[ShopLoop].volumeDb` + `C.busDefaultDb[Sfx]` + `C.musicOffsetFromSfxDb`) (0.251); một cue cấu hình +6 dB cho volume = 1 và validator cảnh báo.
- `AC-4 [EditMode]` **GIVEN** `fade_factor` với T = 0 và T = `C.busTransitionSeconds`, **WHEN** lấy mẫu tại t0, t0 + T/4, t0 + T, t0 + 2T, **THEN** T = 0 cho 1 ở mọi mẫu (không chia cho 0); T > 0 cho 0, 0.25, 1, 1; đảo chiều fade ở t0 + T/4 bắt đầu từ 0.25, không nhảy.

**SoundCue và validator (A2, voice_budget, storm_peak_gain, cue_throughput)**
- `AC-5 [EditMode]` **GIVEN** kho cue mặc định, **WHEN** chạy validator, **THEN** 0 lỗi, `storm_instances` (12) ≤ `C.totalVoices` − `C.musicVoices` − `C.ambienceVoices` − `C.minFreeVoicesForRewards` (12), `storm_peak_gain` (1.76) ≤ `T.stormPeakGainMax`, mọi cue `StealOldest` có `priority` ≤ `C.stormPriorityCeiling` thỏa `maxInstances` × `minInterval` ≥ `T.minStealAgeSeconds` (`PegHitScored` 0.24, `PegHitUnscored` 0.20 ≥ 0.20), `Plinko.CoinSpawn` và `Plinko.CoinSpawnAuto` là `DropNew` với max(`clip.length`) / (1 − `Q.pitchJitterRatio`) ≤ `T.manualSpawnClipMaxSeconds` (0.12) và `T.manualSpawnClipMaxSeconds` ≤ registry `tap_cooldown` − `T.spawnClipMarginSeconds` (0.15 − 0.03 = 0.12), và mọi cue `pitchMode = Scale` (`PegHitScored`, `JackpotBurstSpawn`) có `pitchedSamples` không rỗng với |shift| ≤ `C.maxSampleShiftSemitones` cho mọi n = 0…`C.scaleNoteCount` − 1.
- `AC-6 [EditMode]` **GIVEN** lần lượt từng cấu hình sai: cue 0 clip; `maxInstances` = 0; `minInterval` < 0; `priority` = 101; hai cue trùng `cueId`; `pitchedSamples` chỉ có mẫu trung (12); `Q[JackpotBurstSpawn].pitchedSamples` rỗng; tăng `Q[PegHitUnscored].maxInstances` thêm 1; `Q[PegHitScored].volumeDb` = 0; `Q[PegHitUnscored].minInterval` = 0.08; clip của `Plinko.CoinSpawn` dài `T.manualSpawnClipMaxSeconds` + 0.05 s; clip của `Plinko.CoinSpawnAuto` dài đúng `T.manualSpawnClipMaxSeconds` (0.12 s — hợp lệ nếu chỉ so `length`) với `Q[CoinSpawnAuto].pitchJitterRatio` (0.03), tức kêu thực 0.12 / 0.97 = 0.124 s; `T.manualSpawnClipMaxSeconds` > registry `tap_cooldown` − `T.spawnClipMarginSeconds` (ví dụ = `tap_cooldown`); `T.spawnClipMarginSeconds` ngoài safe range 0.017–0.05, **WHEN** chạy validator, **THEN** mỗi cấu hình sinh đúng lỗi tương ứng (lỗi đổi pitch nêu cue và nốt vi phạm; cue `Scale` thiếu mẫu nêu tên cue; lỗi ngân sách nêu `storm_instances` 13 > 12; lỗi biên độ nêu giá trị > `T.stormPeakGainMax`; lỗi steal_age nêu `PegHitUnscored` 0.16 < `T.minStealAgeSeconds`; lỗi clip spawn nêu tên cue, tên clip, `length` và thời gian kêu thực `length` / (1 − `pitchJitterRatio`) — ca `CoinSpawnAuto` 0.12 s phải bị bắt với 0.124 > 0.12; lỗi knob nêu `manualSpawnClipMaxSeconds` > `tap_cooldown` − `spawnClipMarginSeconds`; lỗi safe range nêu `spawnClipMarginSeconds`).

**Nhận yêu cầu (A3, cue_admitted)**
- `AC-7 [EditMode]` **GIVEN** Audio chưa khởi tạo, **WHEN** gọi `Play(Plinko.CoinSpawn)` và `Play("Khong.Ton.Tai")` sau khi khởi tạo, **THEN** lần lượt `Dropped(NotReady)` và `Dropped(UnknownCue)`; không voice nào phát; không có exception.
- `AC-8 [EditMode]` **GIVEN** đồng hồ tiêm, **WHEN** yêu cầu `Plinko.PegHitScored` tại t = 0, t = `Q.minInterval` − ε, t = `Q.minInterval`, **THEN** kết quả lần lượt Played / `RateLimited` / Played, và yêu cầu bị bỏ không dời mốc.
- `AC-9 [EditMode]` **GIVEN** `Q[PegHitScored].maxInstances` (6) bản đang phát, **WHEN** đến yêu cầu thứ 7 hợp lệ về `minInterval`, **THEN** bản bắt đầu sớm nhất dừng, bản mới phát trên voice đó, số bản = 6, bộ đếm "cướp cùng cue" tăng 1; **AND GIVEN** `Q[CoinSpawnAuto].maxInstances` bản đang phát, **WHEN** có yêu cầu mới, **THEN** `Dropped(InstanceCap)` và bản cũ không bị dừng.
- `AC-10 [EditMode]` **GIVEN** mỗi bước A3 được làm sai riêng lẻ (Locked, Suspended, UserMuted, BusSilent, BackgroundFiltered, RateLimited, InstanceCap, NoVoice), **WHEN** gửi yêu cầu, **THEN** lý do trả về đúng bước đó; khi nhiều bước cùng sai, lý do là bước có số thứ tự nhỏ nhất.

**Voice và cướp voice (A4, A5, steal_victim)**
- `AC-11 [EditMode]` **GIVEN** Audio đã khởi tạo, **WHEN** đếm nguồn phát, chạy kịch bản bão hòa `T.stormTestSeconds` giây, rồi đếm lại, **THEN** số nguồn = `C.totalVoices` ở cả hai lần (không tạo thêm, không hủy), và có đúng `C.musicVoices` voice chỉ nhận cue bus Music.
- `AC-12 [EditMode]` **GIVEN** đồng hồ tiêm, backend giả giữ mọi voice ở Playing (clip chưa hết), Audio Active, bus Plinko Normal, và trạng thái A = ví dụ `steal_victim`: 15 voice SFX bận gồm 6 `PegHitScored`, 2 `PegHitUnscored` (bắt đầu 2.10 s và 2.20 s), 1 `CoinSpawn`, 3 `BucketX2`, 3 voice khác ưu tiên ≥ 50, cùng voice nhạc đang phát; **WHEN** lần lượt yêu cầu `Plinko.PegHitUnscored` tại t = 2.25 s, `Plinko.JackpotHit` tại t = 2.30 s, rồi `Plinko.PegHitUnscored` tại t = 2.35 s (≥ 2.20 + `Q[PegHitUnscored].minInterval` (0.10)), **THEN** lần lượt: `Dropped(RateLimited)` (2.25 − 2.20 < `Q[PegHitUnscored].minInterval`, A3.8 xét trước A3.10; mốc vẫn 2.20); Played với nạn nhân là voice `PegHitUnscored` bắt đầu 2.10 s; `Dropped(NoVoice)` (số bản 1 < `maxInstances`, không voice nào ưu tiên < 10).
  **AND GIVEN** trạng thái B: 15 voice SFX bận bởi cue ưu tiên ≥ 80 **không** gồm `BucketX2` — 3 `BucketX5` (80), 3 `BucketX1` (80), 3 `JackpotBurstSpawn` (85), 2 `JackpotHit` (100), 4 cue Gacha ưu tiên 90 (`Gacha.Crank`, `Gacha.EggPop`, `Gacha.RarityReveal.common`, `Gacha.RarityReveal.rare`, mỗi cue 1 bản) — số bản `BucketX2` = 0 < `Q[BucketX2].maxInstances` và đã qua `Q[BucketX2].minInterval` kể từ lần phát gần nhất, **WHEN** yêu cầu `Plinko.BucketX2`, **THEN** `Dropped(NoVoice)` (không cướp ngang hàng với `BucketX5`/`BucketX1` ưu tiên 80) và không voice nào bị dừng.
  **AND GIVEN** lại trạng thái A (3 bản `BucketX2` đang phát = `Q[BucketX2].maxInstances`, `StealOldest`), **WHEN** yêu cầu `Plinko.BucketX2` sau `Q[BucketX2].minInterval`, **THEN** Played bằng cách tự cướp bản `BucketX2` cũ nhất (A3.9), bộ đếm "cướp cùng cue" tăng 1. Ở mọi vế, voice nhạc không bao giờ là nạn nhân.
- `AC-13 [EditMode]` **GIVEN** đồng hồ tiêm và `T.stormPegHitRate` (134) yêu cầu `PegHitScored`/s xen 10% `PegHitUnscored`, 6.67 `CoinSpawn`/s và 1 `JackpotHit` mỗi 2 s, **WHEN** chạy `T.stormTestSeconds` (10 s), **THEN** số `PegHitScored` được phát ≤ `T.stormTestSeconds` / `Q.minInterval` + 1 (251); voice đang phát ≤ `C.totalVoices` ở mọi bước; 100% `JackpotHit` được phát; mọi lần cướp cùng cue `PegHitScored` có tuổi nạn nhân ≥ `T.minStealAgeSeconds`.

**Cao độ, clip và pan (A6, A7, note_index, sample_pitch)**
- `AC-14 [EditMode]` **GIVEN** `note_index` với `C` mặc định, **WHEN** pitchPosition = 0, 1, 0.727, −0.5, 1.7, NaN, **THEN** n = 0, 9, 7, 0, 9, floor(`C.fallbackPitchPosition` × 9 + 0.5) (5); semitone(n) của n = 0…9 đúng dãy {0, 2, 4, 7, 9, 12, 14, 16, 19, 21}.
- `AC-15 [EditMode]` **GIVEN** `Plinko.PegHitScored` và RNG giả trả 0, **WHEN** phát với n = 0…9, **THEN** mẫu được chọn và pitch khớp bảng `sample_pitch` (pitch = 2^(shift/12)), |shift| ≤ `C.maxSampleShiftSemitones`; **AND WHEN** phát 10 000 lần với RNG thật, **THEN** mọi pitch ∈ [2^(−`C.maxSampleShiftSemitones`/12) × (1 − `Q.pitchJitterRatio`), 2^(`C.maxSampleShiftSemitones`/12) × (1 + `Q.pitchJitterRatio`)] và > 0. Lặp cả hai vế với `Plinko.JackpotBurstSpawn` (mẫu chime 4/12/19, `Q.pitchJitterRatio` của chính cue): cùng bảng chọn mẫu và cùng giới hạn; với `burstIndex` 0…`jackpot_burst_count` − 1 (5 xu) thì n = 0, 2, 5, 7, 9.
- `AC-16 [EditMode]` **GIVEN** cue `None`, cue `Random` có 3 clip và `avoidRepeat`, **WHEN** phát 1000 lần mỗi cue, **THEN** cue `None` luôn pitch = 1; cue `Random` có pitch ∈ [1 − `Q.pitchJitterRatio`, 1 + `Q.pitchJitterRatio`] và không có hai lần liên tiếp cùng clip.
- `AC-17 [EditMode]` **GIVEN** cue có `panFromPosition`, **WHEN** panPosition = −1, 0, 1, 5, NaN, **THEN** pan = −`C.maxPan`, 0, `C.maxPan`, `C.maxPan`, 0; cue không có cờ luôn pan = 0; mọi voice có spatialBlend = 0.

**Bus nền khi bàn ẩn (A8, D5)**
- `AC-18 [EditMode]` **GIVEN** `SetBusBackground(Plinko, true)`, **WHEN** yêu cầu từng cue bus Plinko và một cue bus Gacha, Ui, **THEN** mọi cue có `keepWhenBusBackground = false` (PegHitScored, PegHitUnscored, CoinSpawn, CoinSpawnAuto, JackpotBurstSpawn, CoinFizzle, BoardBusy) → `Dropped(BackgroundFiltered)`; BucketX1/X2/X5 và JackpotHit được phát với âm lượng thấp hơn `C.plinkoBackgroundOffsetDb` so với khi Normal; cue Gacha/Ui không đổi âm lượng.
- `AC-19 [EditMode]` **GIVEN** một voice `PegHitScored` đang phát, **WHEN** gọi `SetBusBackground(Plinko, true)`, rồi `false` sau `C.busTransitionSeconds`, **THEN** voice không bị dừng, âm lượng giảm đúng `fade_factor` tới −12 dB sau `C.busTransitionSeconds`, rồi trở lại mức cũ sau thêm `C.busTransitionSeconds`; gọi `true` hai lần liên tiếp không đổi gì.
- `AC-20 [Play mode]` **GIVEN** scene thật với auto-drop đang chạy và Feedback & Juice nối `TabChanged`, **WHEN** chuyển Tab 1 → Tab 2 trong 5 s rồi về Tab 1, **THEN** trong lúc ở Tab 2 bộ đếm Played của mọi cue `PegHit` không tăng, Played của `Bucket*` vẫn tăng; về Tab 1 thì `PegHit` lại được phát trong ≤ `C.busTransitionSeconds`.

**Tắt tiếng theo lý do (A9)**
- `AC-21 [EditMode]` **GIVEN** 3 voice SFX và voice nhạc đang phát, **WHEN** Bootstrap giả phát `Paused {reason = Hidden}`, chờ 60 s đồng hồ thực, rồi phát `Resumed {reason = Hidden, awaySeconds = 60}` (lặp lại với `reason = Background`), **THEN** trong lúc tạm ngưng mọi voice ở Paused giữ nguyên vị trí phát và mọi `Play` → `Dropped(Suspended)`; sau `Resumed` các voice tiếp tục từ vị trí cũ và âm lượng tăng theo `fade_factor` trong `C.resumeFadeInSeconds`.
- `AC-22 [EditMode]` **GIVEN** trạng thái Active, **WHEN** lần lượt: bật `Ad`, bật `Background`, tắt `Ad`, tắt `Background`; và riêng một ca: cửa sổ mất focus mà không có `Paused`, **THEN** trạng thái lần lượt Suspended, Suspended, Suspended, Active; ca mất focus giữ Active.
- `AC-23 [EditMode]` **GIVEN** trạng thái Active với voice nhạc đang phát, **WHEN** `SetUserMuted(true)` rồi `SetUserMuted(false)`, **THEN** gain Master về 0 sau `C.userMuteFadeSeconds`; trong lúc câm `Play` SFX → `Dropped(UserMuted)` còn vị trí phát của nhạc tiếp tục tăng; sau khi bật lại gain về mức cũ sau `C.userMuteFadeSeconds` rồi `Ui.SoundOn` được phát đúng 1 lần; `MuteChanged` phát đúng 1 lần cho mỗi lần đổi.
- `AC-43 [EditMode]` (Alpha; Bootstrap B) **GIVEN** Audio Active, 3 voice SFX và voice nhạc đang phát, Bootstrap giả **không** phát `Paused`, **WHEN** gọi `SetMuteReason(Ad, true)`, gửi `Play(Plinko.PegHitScored)` và `Play(Plinko.JackpotHit)`, cho đồng hồ tiêm trôi `T.stormTestSeconds` (dữ liệu fixture: 3 voice SFX, 2 yêu cầu), rồi gọi `SetMuteReason(Ad, false)`, **THEN** ngay khi `Ad` bật trạng thái là Suspended, 3 voice SFX ở Free (không giữ vị trí) và voice nhạc ở Paused giữ vị trí; hai `Play` → `Dropped(Suspended)`; sau khi tắt `Ad` trạng thái Active, voice nhạc tiếp tục từ vị trí cũ với âm lượng theo `fade_factor` trong `C.resumeFadeInSeconds`, số voice SFX đang phát = 0 (không phát bù) và `Play` kế tiếp → Played.

**Mở khóa WebGL (A10)**
- `AC-24 [EditMode]` **GIVEN** `requiresUnlock` = đúng, **WHEN** gửi `Play` trước rồi sau `FirstInteraction`, **THEN** trước → `Dropped(Locked)`, sau → Played; nếu `Background` bật trước `FirstInteraction` thì sau đó trạng thái là Suspended; với `requiresUnlock` = sai, Audio vào Active ngay khi khởi tạo.
- `AC-25 [Play mode]` **GIVEN** bản build WebGL chạy trên Chrome và Safari (trang thử của cổng game), **WHEN** chạm bàn Plinko lần đầu rồi chạm tiếp 10 lần, **THEN** bộ đếm chẩn đoán cho thấy trạng thái Active sau cú chạm đầu, Played của `Plinko.CoinSpawn` ≥ 9 và console không có lỗi âm thanh.

**Nhạc (A11)**
- `AC-26 [EditMode]` **GIVEN** cấu hình MVP không có clip nhạc, **WHEN** khởi tạo và chạy kịch bản bão hòa, **THEN** không lỗi, voice nhạc rảnh và không bị cấp cho SFX; **AND GIVEN** cấu hình VS có `Music.ShopLoop`, **WHEN** mở khóa trước rồi Bootstrap giả phát `Interactive` (và ở ca khác: `Interactive` trước rồi mới mở khóa), **THEN** nhạc chưa phát cho tới khi cả hai điều kiện đúng, rồi phát với loop = đúng, âm lượng tăng theo `fade_factor` trong `C.musicFadeInSeconds`, và không bị cướp trong suốt kịch bản bão hòa.

**Lưu thiết lập (A12)**
- `AC-27 [EditMode]` **GIVEN** kho thiết lập giả rỗng, **WHEN** khởi tạo, **THEN** `IsUserMuted` = `C.defaultUserMuted` và hai thanh trượt = `C.defaultUserSlider`; **WHEN** `SetUserMuted(true)` rồi khởi tạo một Audio mới trên cùng kho, **THEN** `IsUserMuted` = đúng ngay sau khởi tạo, trước khi có `Play` nào được phát.
- `AC-28 [EditMode]` **GIVEN** thanh trượt (Alpha), **WHEN** s = 1, 0.75, 0.5, 0, NaN, 1.5, **THEN** user_db = 0, −10, −20, im (cue SFX trên bus → `Dropped(BusSilent)`, nhạc vẫn chạy câm), `slider_to_db(C.defaultUserSlider)`, 0.

**Đồng hồ, chẩn đoán, ranh giới, lỗi (A13–A15)**
- `AC-29 [EditMode]` **GIVEN** time scale = 0 rồi = 2, **WHEN** lặp AC-8, **THEN** kết quả giống hệt (minInterval đo bằng đồng hồ thực).
- `AC-30 [EditMode]` **GIVEN** kịch bản AC-13, **WHEN** tổng hợp chẩn đoán, **THEN** số yêu cầu = Played + Σ Dropped theo lý do, mỗi yêu cầu được đếm đúng một lần, và đỉnh voice ≤ `C.totalVoices`.
- `AC-31 [EditMode]` **GIVEN** assembly definition của Audio, **WHEN** đọc danh sách tham chiếu, **THEN** không có assembly nào của Plinko, Gacha, Shelf, Feedback & Juice (Audio không nghe sự kiện gameplay, D1).
- `AC-32 [EditMode]` **GIVEN** thiếu `AudioConfig`, **WHEN** khởi tạo rồi gọi `Play`, `SetUserMuted(true)`, **THEN** trạng thái Failed, `Play` → `Dropped(NotReady)` không exception, `IsUserMuted` = đúng và giá trị được lưu.

**Bước khởi động (A16, C1[15])**
- `AC-41 [EditMode]` **GIVEN** sổ đăng ký `IBootStep` giả của Bootstrap, **WHEN** Audio đăng ký, **THEN** có đúng 1 bước của Audio với `Id` = `audio.service`, `Phase` = `ServiceInit`, `RunsAfter` rỗng, `IsCritical` = sai; trước `Run()` mọi `Play` → `Dropped(NotReady)`; sau `Run()` với `C` hợp lệ thì `IsDone` = đúng, `IsFailed` = sai và số nguồn phát = `C.totalVoices`; với `AudioConfig` thiếu thì `IsFailed` = đúng, trạng thái Failed, không exception lọt ra ngoài `Run()`; assembly của Bootstrap không tham chiếu assembly của Audio.
- `AC-42 [EditMode]` **GIVEN** `requiresUnlock` = đúng và Audio đã đăng ký bước nhưng chưa `Run()`, **WHEN** Bootstrap giả phát `FirstInteraction` rồi gọi `Run()`, **THEN** ngay sau khi `IsDone` trạng thái là Active (không phải Locked), `Play(Plinko.CoinSpawn)` → Played, và không có cue nào được phát bù cho khoảng trước khởi tạo.

**Hiệu năng**
- `AC-33 [Perf]` **GIVEN** pool voice đã tạo, kịch bản AC-13 chạy trong scene thật, **WHEN** ghi 600 khung bằng `ProfilerRecorder` "GC Allocated In Frame", **THEN** 0 B mỗi khung, kể cả lúc cướp voice, đổi chế độ nền và câm/bật tiếng (cùng điều kiện với Plinko AC-38).
- `AC-34 [Perf]` **GIVEN** bản WebGL trên thiết bị tham chiếu tầm trung do ADR hiệu năng chỉ định, bàn Plinko bão hòa (`max_active_coins` xu + `RequestBurst`), **WHEN** đo 60 s, **THEN** frame time p95 ≤ 1000 / `targetFps` (16.7 ms, chung kịch bản với Plinko AC-36) và thời gian xử lý âm thanh p95 ≤ `T.audioFrameBudgetMs`.
- `AC-35 [Perf]` **GIVEN** build report WebGL, **WHEN** cộng kích thước nén của mọi clip SFX MVP (và clip nhạc từ VS), **THEN** tổng SFX ≤ `T.sfxDownloadBudgetKb`, nhạc ≤ `T.musicDownloadBudgetKb`.
- `AC-36 [Play mode]` **GIVEN** backend thật trong Play mode, **WHEN** `Play` trả Played, **THEN** nguồn phát tương ứng báo đang phát trong cùng khung (trễ ≤ `T.requestToStartMaxFrames` khung).

**Hiển thị**
- `AC-37 [Screenshot]` **GIVEN** header ở độ phân giải tham chiếu 1080×1920, **WHEN** chụp lúc không câm và lúc câm, **THEN** thấy lần lượt icon "loa có sóng" và "loa gạch chéo" màu nâu `#3D312A` ở vị trí HUD quy định, trạng thái icon khớp `IsUserMuted`.
- `AC-38 [Screenshot]` (Alpha) **GIVEN** màn Settings, **WHEN** chụp sau khi kéo Nhạc = 0.5, SFX = 1, **THEN** hai thanh trượt hiện đúng vị trí đã lưu và nhãn "Nhạc", "Hiệu ứng".

**Trải nghiệm (playtest)**
- `AC-39 [Play mode]` **GIVEN** `T.playtestSampleSize` (5) người chơi mới, mỗi người spam chạm Tab 1 trong 30 s kèm ít nhất 2 jackpot, **WHEN** hỏi "tiếng nghe êm hay ồn" (thang 1–5) và "bạn có nghe thấy lúc trúng thưởng lớn không", **THEN** điểm trung bình ≥ `T.playtestCalmScoreMin` (4.0) và tỉ lệ nhận ra jackpot ≥ `T.playtestJackpotRecognitionMin` (0.8).
- `AC-40 [Play mode]` **GIVEN** cùng nhóm người chơi ở Tab 2 với auto-drop đang chạy, **WHEN** hỏi "bàn chơi có còn đang kiếm xu không", **THEN** tỉ lệ trả lời "có" ≥ `T.playtestBackgroundAwarenessMin` (0.6) và số người nói tiếng làm phiền việc sắp kệ ≤ `T.playtestBackgroundAnnoyedMax` (0) (D5).

## Open Questions

Không có câu hỏi NEEDS-USER mở: mọi lựa chọn lớn đã có quyết định của user (Audio D1–D6, Boot D3, quyết định D2 về voice PegHit và Bootstrap B trong decision-log 2026-10-21); câu hỏi diễn giải D2 mà review Round 3 nêu (AU3-R2) đã được user trả lời; các lựa chọn nhỏ được giả định như dưới đây.

| Question | Owner | Target date |
|---|---|---|
| Assumed: file `Docs/design/gdd/audio.md`, tiêu đề "Audio" (Plinko GDD đã tham chiếu đúng đường dẫn này). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: các mục được viết = bộ standard + Tuning Knobs + Visual/Audio + Cross-References + Open Questions; thêm Game Feel và UI Requirements ngắn (workflow yêu cầu các mục non-tier "khi áp dụng"; brief định bỏ Game Feel — giữ ngắn vì Audio quyết định cảm giác Pillar 1). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: Overview framing hạ tầng, một câu về trải nghiệm (Foundation, không có fantasy riêng). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: cây bus Master → Music, Ambience, Sfx; Sfx → Gameplay (Plinko, Gacha), Ui (đủ cho thanh trượt Alpha và giảm riêng bus Plinko, không thừa tầng). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: âm lượng lưu bằng dB, `linear = 10^(dB/20)`, ≤ `silenceFloorDb` thì im; âm lượng cuối = tổng dB các tầng đổi một lần, áp tuyến tính lên nguồn phát (không phụ thuộc AudioMixer trên WebGL). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: thứ tự ưu tiên của brief, nhưng `PegHit` không tính xuống dưới "bàn bận" để đúng D2; thêm chime Bùng Xu (85) và fizzle (45) vào thang. | game-designer | 2026-10-21 (đã chốt) |
| Assumed: tập lý do tắt tiếng {Background, Ad, User}; lý do "Focus" của brief đổi thành `Background` = tab ẩn / app nền (Boot D3: chỉ mất focus mà vẫn hiển thị thì không tắt tiếng). | game-designer | 2026-10-21 (đã chốt) |
| Assumed (Revision 4, Bootstrap B): khi lý do `Ad` bật, voice SFX dừng hẳn (về Free) còn nhạc giữ vị trí, vì mô phỏng chạy tiếp trong lúc quảng cáo nên đuôi tiếng SFX cũ sẽ lệch bàn khi quay lại (với `Background` mô phỏng cũng dừng nên mọi voice vẫn giữ vị trí); không phát bù tiếng của khoảng quảng cáo. | game-designer | 2026-10-21 (đã chốt) |
| Assumed (Revision 4): Platform Services gọi `SetAdOpen(false)` trước `SetMuteReason(Ad, false)` khi quảng cáo đóng, để `Paused` (nếu trang/app vẫn ẩn) đến trước khi Audio rời Suspended; sai thứ tự chỉ gây tối đa một khung Active ở âm lượng fade 0. | Platform Services GDD | 2026-11-11 |
| Assumed: không cue quan trọng nào phát trước thao tác đầu trên WebGL; yêu cầu trước đó bị bỏ, không xếp hàng; nhạc fade-in sau mở khóa. | game-designer | 2026-10-21 (đã chốt) |
| Assumed: tạm ngưng = dừng mọi voice tại chỗ (nhạc giữ vị trí), tiếp tục khi `Resumed`, có fade-in. | game-designer | 2026-10-21 (đã chốt) |
| Assumed: tiếng vào hũ được Feedback gộp theo `floatingTextMergeWindow` (Plinko sở hữu); Audio chỉ có `minInterval` an toàn 0.03 s cho cue hũ, không thêm knob gộp trùng. | game-designer | 2026-10-21 (đã chốt) |
| Assumed: haptics không thuộc Audio (ở Feedback & Juice, `hapticTapMs`). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: mọi nguồn 2D, pan nhẹ theo x (`maxPan` 0.3); nếu WebGL không hỗ trợ pan thì pan = 0. | game-designer | 2026-10-21 (đã chốt) |
| Assumed: thang ngũ cung trưởng một tông (`scaleRootMidi` = C4), dùng chung cho mọi cue có cao độ và nhạc VS. | game-designer | 2026-10-21 (đã chốt) |
| Assumed: tôn trọng công tắc im lặng iOS, không ngắt nhạc app khác (cấu hình phiên → ADR). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: tham số kỹ thuật (load type, nén, mixer hay tuyến tính, cách tạm dừng, kho thiết lập) → ADR, không chốt trong GDD. | game-designer | 2026-10-21 (đã chốt) |
| Assumed: "tổng 16 voice" (D2) gồm 1 voice nhạc dành riêng → 15 voice SFX; MVP để voice nhạc rảnh. | game-designer | 2026-10-21 (đã chốt) |
| ~~Assumed: trần D2 (6 bản, ≥ 40 ms) áp cho cue marimba `PegHitScored`; `PegHitUnscored` là cue riêng 2 bản, ưu tiên thấp nhất.~~ **Quyết định user** (decision-log 2026-10-21, AU3-R2): D2 = 6 voice `PegHitScored` (≥ 40 ms) + 2 voice `PegHitUnscored` (ưu tiên thấp nhất; `minInterval` 0.10 s theo Revision 2) trong tổng 16 voice; knob `Q[PegHitScored].maxInstances` = 6, `Q[PegHitUnscored].maxInstances` = 2 giữ nguyên. | User | 2026-10-21 (đã chốt) |
| Assumed (Revision 2, AU1-B1): sửa vi phạm `steal_age` bằng dữ liệu (`PegHitUnscored.minInterval` 0.10; `CoinSpawn` → `DropNew` + clip ≤ `T.manualSpawnClipMaxSeconds` ≤ `tap_cooldown`) thay vì nới ràng buộc theo `clip.length`; giữ validator đơn giản và luật "không cắt tiếng đang kêu" cho cú chạm tay (Pillar 1). | game-designer | 2026-10-21 (đã chốt) |
| Assumed (Revision 3, AU2-B1): Ràng buộc 2 dùng pitch thấp nhất 1 − `pitchJitterRatio` (đúng cho `pitchMode = Random` của hai cue spawn; nếu đổi sang `Scale` phải dùng pitch thấp nhất của `sample_pitch`); biên `T.spawnClipMarginSeconds` = 0.03 s (≈ 2 khung 60 FPS) gộp trễ khung, bộ đệm Web Audio và chênh đồng hồ mô phỏng–thực thành một knob; `T.manualSpawnClipMaxSeconds` 0.12 (clip "tok" ≤ 0.116 s). Ngưỡng biên thực trên WebGL xác minh cùng ADR âm thanh (AC-25). | game-designer | 2026-10-21 (đã chốt) |
| Assumed (Revision 2, AU1-B2): `Plinko.JackpotBurstSpawn` có 3 mẫu chime 4/12/19 (như marimba, D4) thay vì thêm field giới hạn dải nốt `SoundCue.scaleNoteRange`; thêm 3 clip vào `T.sfxDownloadBudgetKb` (AC-35 kiểm). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: Feedback & Juice gọi `SetBusBackground(Plinko, TabChanged.current ≠ Board)` khi nhận `TabChanged {previous, current}` của Bootstrap; Audio không biết khái niệm tab (giữ D1: Audio không biết gameplay/UI). `bootstrap-and-scene-flow.md` đã ghi đúng phản ứng này cho Feedback; còn chờ Feedback GDD xác nhận. | Feedback & Juice GDD | 2026-10-28 |
| Assumed: câm bởi người chơi → nhạc chạy câm giữ nhịp, SFX mới bị bỏ (tiết kiệm voice). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: thiết lập âm thanh lưu ở kho thiết lập cục bộ của thiết bị, không nằm trong save tiến độ và không đi theo xuất/nhập Base64. | game-designer + Save/Load GDD | 2026-11-11 |
| Assumed: API nhận `pitchPosition` 0–1 chung; Feedback quy đổi `PegHit.rowIndex` (đề xuất 1 − `rowIndex` / (`pegRows` − 1), nốt đi xuống khi xu rơi) và `CoinSpawned.burstIndex` (`burstIndex` / (`jackpot_burst_count` − 1)) sang `pitchPosition` — hai trường này nay có trong payload R12 của Plinko. | Feedback & Juice GDD | 2026-10-28 |
| Assumed: voice bị cướp dừng ngay không fade (nạn nhân luôn nhỏ nhất, cũ nhất). Nếu playtest nghe tiếng click → thêm fade 1 khung (knob mới). | Playtest MVP (game-designer) | 2026-11-04 |
| Mọi giá trị trong bảng cue (dB, ưu tiên, trần, khoảng) là đề xuất chưa review cân bằng (không có agent audio, không được gọi specialist); chốt bằng AC-39, AC-40 trên asset thật. | game-designer + playtest | 2026-11-04 |
| ~~Nguồn asset âm thanh (AU-R20).~~ **Đã quyết** (User, 2026-10-21): MVP dùng asset CC0 (Kenney, Freesound CC0); Vertical Slice mua gói SFX/marimba thương mại. Ràng buộc giấy phép: mỗi clip CC0 lưu kèm file LICENSE / ghi nguồn (URL, tác giả, giấy phép) trong thư mục asset của clip; gói mua lưu bằng chứng giấy phép; clip không rõ nguồn không vào build. 3 mẫu marimba gốc vẫn phải cùng một cây đàn (D4) dù lấy từ nguồn nào. | User | 2026-10-21 (đã chốt) |
| ADR âm thanh (cần trước story Audio): AudioMixer hay âm lượng tuyến tính trên WebGL Unity 6000.3 (mixer chỉ hỗ trợ một phần, đổi pitch qua mixer không chạy trên Web); tạm dừng toàn bộ trên WebGL; `panStereo` trên WebGL; load type/nén; phiên âm thanh iOS; kho thiết lập cục bộ (PlayerPrefs/IndexedDB); `Play` không cấp phát (AC-33); tách backend voice để test EditMode. | technical-director → `game-adr` | 2026-11-04 |
| Ngân sách `audioFrameBudgetMs`, `sfxDownloadBudgetKb`, `musicDownloadBudgetKb` trên WebGL tầm trung; 16 voice có giữ được frame time (đo cùng ADR hiệu năng với Plinko AC-36)? | technical-director (ADR hiệu năng / build web) | 2026-11-04 |
| ~~Plinko GDD cần sửa theo D1, D5: hàng "Audio" trong Dependencies, câu Edge Case bàn ẩn, `pegPitchJitter` / `autoSpawnVolumeOffsetDb` chuyển sở hữu về SoundCue.~~ **Đã đóng** — `plinko-board.md` Revision 3 (R12.3, hàng Audio, Visual/Audio, Tuning Knobs) đã sửa đúng như vậy. | Plinko Board revision (game-designer) | 2026-10-21 (đã đóng) |
| Cue Gacha (`Gacha.Crank`, `Gacha.EggPop`, `Gacha.RarityReveal.<phẩm cấp>`) là provisional: xác nhận danh sách, nhịp và ưu tiên khi viết GDD Gacha (nút tua có cắt tiếng không?). | Gacha Machine GDD | 2026-10-28 |
| VS: thêm `Plinko.RainLoop` làm `storm_instances` = 13 > 12 — giảm `PegHitUnscored` xuống 1 bản hay tăng `totalVoices` lên 17? | game-designer (khi mở lại cho VS) | 2026-11-11 |
| Thêm icon loa vào header trên Penpot (`Main_Plinko` và các board có header). | User + art-director (Penpot) | 2026-10-28 |
| Assumed: bước khởi động `Id` = `audio.service`, `RunsAfter` rỗng (tên Bootstrap đã dùng trong AC-8 của nó; Audio không cần hệ nào chạy trước trong `ServiceInit`). | game-designer | 2026-10-21 (đã chốt) |
| Assumed: Audio đăng nghe `FirstInteraction` ngay lúc đăng ký bước và ghi nhớ nếu nó đến trước `Run()`, rồi vào thẳng Active (Bootstrap cho phép `FirstInteraction` trong `Booting.Steps`; không ghi nhớ thì WebGL có thể khóa tiếng cả phiên). | game-designer | 2026-10-21 (đã chốt) |
| Assumed (C3[4]): nhạc VS bắt đầu khi đã mở khóa **và** đã có `Interactive` của Bootstrap (B5.3, rèm kéo xong, phát một lần mỗi lần nạp scene) thay cho `BootCompleted` — nhạc không bắt đầu dưới rèm khởi động, không tranh tài nguyên với `boot_to_interactive`; `Interactive` đến trước khi mở khóa thì ghi nhớ như `FirstInteraction` (A10). | game-designer | 2026-10-21 (đã chốt) |
| Assumed (đề xuất cho Feedback): xu `origin = Rain` không có cue spawn riêng ở MVP, VS dùng `Plinko.RainLoop`; `CoinResolved` với `reason` = Stuck dùng tiếng của `CoinStuck`, `reason` = Overflow không có tiếng (xu không lên bàn, tránh dồn tiếng khi hàng đợi tràn). | Feedback & Juice GDD | 2026-10-28 |
