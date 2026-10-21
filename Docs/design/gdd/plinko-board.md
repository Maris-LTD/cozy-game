# Plinko Board

> **Status**: Approved
> **Author**: Maris-LTD + Claude (game-designer consulted for Formulas)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 1 — Kinetic ASMR (hỗ trợ Pillar 3 — Meaningful Constraints qua kênh thu nhập Plinko)
> **Resolves review**: BLOCKING #1, #2, #4 và phần Plinko của #3 (`Docs/design/gdd/reviews/first-gdd-review-2026-10-21.md`)
> **Consulted**: game-designer (Formulas), art-director (Visual/Audio, Game Feel), qa-lead (Acceptance Criteria)
> [CD-GDD-ALIGN] skipped (lean)
> **Revision 2026-10-21** (sau `game-design-review`, Needs Revision): thưởng nhắm đo trên EV hiệu dụng (gồm Bùng Xu) → vùng thả 0.14 + lệch ±1.0; nửa đinh gắn tường ở tầng chẵn + ràng buộc khe tối thiểu; Economy Core phụ thuộc Plinko (chỉ `baseCoinValue` đi vào); giữ chỗ cho chạm tay khi có hàng đợi loạt; sửa câu chữ R4.1, AC-36, AC-38, AC-39.
> **Revision 2 — 2026-10-21** (khớp các GDD Foundation vừa viết): `baseCoinValue` là input runtime: Economy Core cài đặt `IBaseCoinValueSource` theo công thức `base_coin_value` (Economy D4), Plinko đọc lúc giải quyết xu, `PlinkoConfig.baseCoinValue` = 1 chỉ là dự phòng; tạm dừng chỉ khi tab ẩn / app xuống nền qua `Paused`/`Resumed` của Bootstrap (Boot D3), Bootstrap sở hữu input router và `ActiveTab`/`TabChanged` (Boot D1); bàn ẩn → bus Plinko −12 dB, chỉ tiếng vào hũ + jackpot (Audio D5), Plinko không gọi Audio (Audio D1); pool `plinko.coin` khai báo `maxActiveCoins`, headroom thuộc Object Pooling (Pool D4); thêm vế khe chéo vào `min_passage_gap`.
> **Revision 3 — 2026-10-21** (đợt 2, theo `Docs/production/session-logs/gdd-consistency-2026-10-21-run1.json` C1[2], C1[3], C1[4], C1[7], C1[8], C1[9], C1[10], C1[11], C1[13], C1[16], C1[17], C1[19]; quyết định trong `Docs/production/session-logs/decision-log.md`): tạm dừng chỉ theo `Paused`/`Resumed` của Bootstrap, mất focus mà vẫn hiển thị thì chạy tiếp (Boot D3) — R11.2, bảng trạng thái bàn, Edge Case, AC-29; Plinko không gọi Audio, mọi tiếng đi qua Feedback & Juice (Audio D1, D5); `baseCoinValue` là input runtime do Economy Core cấp qua `IBaseCoinValueSource`, `PlinkoConfig.baseCoinValue` = 1 chỉ là dự phòng (Economy D4, R10); thêm R12 — danh sách sự kiện công bố kèm payload (`PegHit`, `CoinSpawned`, `JackpotHit`, `CoinResolved`, `BoardBusy`, `CoinStuck`) và API (`RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister`, `acceptManualTaps`); độ lớn hiệu ứng thú thuộc Pet Catalog (Kẹo Bảy Màu ×1.7); bỏ `pegPitchJitter`, `autoSpawnVolumeOffsetDb` khỏi `PlinkoFeedbackConfig` (thuộc SoundCue của `audio.md`); xu về pool ngay khi Resolved, không có trạng thái Fizzling; đóng các Open Question đã được Economy R5.1/R5.4/R7.4/R10 trả lời. Giữ phương án B của thưởng nhắm (vùng 0.14 + lệch ±1.0).
> **Revision 4 — 2026-10-21** (fix round 2 theo `Docs/design/gdd/reviews/plinko-board-review-2026-10-21.md` Round 2, mục chặn PB2-B1, PB2-B2, PB2-B3): mô hình Galton/EV tham chiếu `pegs_per_row`, `bucket_width_weights`, `bucket_multipliers` thay số trần; sửa output range của `auto_drop_interval` (không trần trên), `effective_bounciness` (hệ số nảy hữu hạn < 1 được kẹp lên 1, R5.3, R12.5) và `galton_spread` (sigma 1.44–2.72 s); `coin_rate_ceiling` thành trần mô hình gồm auto-drop và độ nảy, ghi rõ Overflow vượt sức chứa, "≈ 20" chỉ áp ở độ nảy gốc không auto-drop, thêm AC-13b/AC-13c và hai knob `T.maxRawPayoutPerSecondPerBaseCoinWithAuto`, `…AtCap`. Giữ phương án B; Economy D4, Boot D1/D3, Audio D1/D5, Pool D4 không đổi.
> **Revision 5 — 2026-10-21** (fix round 3 theo `Docs/design/gdd/reviews/plinko-board-review-2026-10-21.md` Round 3 PB3-B1 và `Docs/production/session-logs/gdd-consistency-2026-10-21-run2.json` C2[9], C2[10], C2[11], C2[13], C2[17]; quyết định user sau design run 2 trong `Docs/production/session-logs/decision-log.md`): mọi tham số test trong AC thành knob `PlinkoBalanceTargets` hoặc ghi "constant (lý do)"/dữ liệu fixture, AC-36 dùng `BootTestTargets.perfReferenceDevice`, AC-40/41 có sai số, Game Feel trỏ AC-31/AC-35 (PB3-B1); đóng NEEDS-USER trần thu nhập theo (a) — trần **mềm**, trần test tách theo bốn cấu hình (thêm `T.maxRawPayoutPerSecondPerBaseCoinAutoAndCap`, AC-13d), Overflow là nguồn vượt trần; đóng hiệu ứng tan theo B (pool `feedback.coinFizzle`, `pools[plinko.coin].headroom` = 0); thêm R11.3 — khi `Paused`, hàng đợi loạt được trả ngay toàn bộ với `reason = Overflow` (AC-47), rewarded ad mở không tạm dừng (Bootstrap B6.10); thêm R13 — bước khởi động `plinko.board` (AC-48); dự phòng `baseCoinValue` = 1 chỉ cho test, Edge Case giá trị 0 theo range ≥ 1; ngân sách Kẹo Bảy Màu theo `pet-catalog.md` (+32% mục tiêu, +40% ước tính); ghi chủ sở hữu tương lai của `PlinkoFeedbackConfig`, thêm `fizzleMaxOnScreen`.
> **Revision 6 — 2026-10-21** (fix round 1 theo `Docs/design/gdd/reviews/plinko-board-review-2026-10-21.md` Round 1, mục chặn PB-B1; Bootstrap B6.5, B6.9, Q8): mô phỏng chỉ tiến khi `IsSimulationRunning` đúng, bắt đầu ở `Interactive`, sau `Resumed` đọc lại `IsSimulationRunning` vì vòng đời có thể về `Booting.Curtain` (R11.2, R11.4 mới); hành vi trước `Interactive` (đăng ký nguồn được nhận, `RequestBurst` xếp hàng, `WorldPress` bỏ im lặng, `Paused` ở `Booting.Curtain` vẫn xả hàng đợi theo R11.3); trạng thái bàn mới "Chờ mở cửa", sửa lối ra của Paused; R13.2 kết thúc ở "Chờ mở cửa"; thêm `Interactive` + `IsSimulationRunning` vào Interactions, Dependencies, Cross-References; AC-29 thêm ca `Resumed` về `Booting.Curtain`, AC-48 thêm ca `IsSimulationRunning` sai. Quyết định user giữ nguyên (trần mềm (a), hiệu ứng tan B, xả hàng đợi khi `Paused`, rewarded ad không tạm dừng).
> **Revision 7 — 2026-10-21** (đồng bộ run3 `gdd-consistency-2026-10-21-run3.json` C3[0], C3[8], C3[12]): bounce uplift tham chiếu `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` (output `raw_rate_ceiling`, Cross-References, AC-13c); thêm kẹp bước mô phỏng `PlinkoConfig.maxSimStep` (R11.4.1, AC-49); bỏ ghi chú cross-doc đã lỗi thời. C3[2]: `fizzleMaxOnScreen` đã có (= 4), không cần sửa. Assumed: `maxSimStep` = 0.25 s, knob riêng bằng `EconomyConfig.maxFrameDelta` (cùng khoảng 0.1–0.5, hai hệ không lệch nhau).

## Summary

Plinko Board là bàn gỗ ở Tab 1 nơi người chơi chạm để thả xu: xu nảy qua các tầng đinh rồi rơi vào năm hũ `[x5][x2][x1][x2][x5]`, hai hũ jackpot ở mép bung ra loạt "Bùng Xu" thưởng. Đây là nguồn xu chủ động và là khoảnh khắc cảm giác cốt lõi của game (Pillar 1), đồng thời là kênh thu nhập để cân với CPS idle (Pillar 3). Mọi trần chống lạm phát và chống quá tải WebGL — vùng thả giới hạn kèm lệch ngẫu nhiên, cooldown chạm, trần xu trên bàn, trần độ nảy < 1, trần chạm đinh được tính, despawn xu kẹt — nằm trong hệ này; Plinko chỉ báo giá trị thô, Economy Core cộng tiền.

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Object Pooling, Bootstrap & Scene Flow (input, tab, tạm dừng)`; Audio gián tiếp qua Feedback & Juice; Economy Core tiêu thụ `CoinResolved` và chỉ cấp `baseCoinValue`

## Overview

Plinko Board là nguồn xu chủ động của game: một bàn gỗ đứng dọc ở Tab 1, người chơi chạm để thả xu, xu nảy qua các tầng đinh và rơi vào một trong năm hũ đáy `[x5][x2][x1][x2][x5]`; hũ quyết định hệ số nhân của đồng xu đó. Người chơi chỉ chọn được vị trí ngang trong một **vùng thả** ở giữa bàn và mỗi xu bị lệch ngang ngẫu nhiên, nên hai hũ jackpot ở mép là kết quả của may mắn khi xu nảy ra ngoài, không phải của việc nhắm. Ngoài chạm tay, bàn còn nhận xu từ ba nguồn khác: thả tự động (khi một thú hoặc boost bật), cơn mưa xu (Boosts, Ads) và **Bùng Xu** — loạt xu thưởng bắn ra khi một xu rơi trúng hũ jackpot.

Hệ thống mang hai vai: với người chơi, đây là khoảnh khắc 30 giây cốt lõi của Pillar 1 (tiếng xu gỗ lách cách, số bay, hũ sáng lên); với các hệ khác, Plinko là một **nguồn dữ liệu có trần**: nó chỉ báo giá trị thô (xu gốc × hệ số hũ) và các sự kiện va chạm, còn Economy Core áp bùa lợi và cộng vào ví (hợp đồng ở `economy-core.md` R5, R10). Mọi giới hạn chống lạm phát và chống quá tải WebGL — cooldown chạm, trần số xu trên bàn, trần độ nảy, trần số lần chạm đinh được tính, despawn xu kẹt — nằm trong hệ này. Thiếu Plinko, game mất kênh thu nhập chủ động mà Pillar 3 cần để cân với CPS idle, và mất cảm giác "chạm là có xu" của phút đầu tiên.

Bàn chạy hoàn toàn trong world-space 2D (Rigidbody2D, collider, PhysicsMaterial2D) với camera riêng; Canvas UI chỉ đóng khung vùng hiển thị và không chứa vật thể vật lý nào.

## Detailed Design

### Core Rules

Mọi tên trong `backtick` là knob ở `PlinkoConfig` (xem Tuning Knobs); không có con số nào nằm trong logic.

**R1. Bàn và hình học**
1. Bàn là một khung chữ nhật world-space gồm: tường trái/phải, `peg_rows` tầng đinh so le theo lưới tam giác (khoảng cách tầng = `row_spacing_ratio` × `peg_spacing`), một **vạch thả** nằm trên tầng đinh đầu tiên, và một dãy 5 hũ đáy ngăn bởi vách thấp. Tọa độ (gốc x = tâm bàn, n = `pegs_per_row`, s = `peg_spacing`, tầng r = 0 là tầng trên cùng):
   - Tường tại x = ±n·s/2 (bề rộng trong = n·s).
   - Tầng chẵn (r = 0, 2, 4…): vị trí lưới x_k = (k − (n−1)/2)·s, k = 0…n−1. **Hai vị trí ngoài cùng (±(n−1)/2·s) không đặt đinh tự do** mà thay bằng **nửa đinh gắn tường**: nửa hình tròn bán kính `wall_bump_radius` có tâm nằm trên mặt tường, cùng độ cao tầng đó. Lý do: khe giữa đinh ±(n−1)/2·s và tường chỉ còn s/2 − `peg_radius`, hẹp hơn đường kính xu → xu kẹt nêm ngay làn dẫn tới hũ x5.
   - Tầng lẻ (r = 1, 3, 5…): x_k = (k − (n−2)/2)·s, k = 0…n−2 (n−1 đinh, đinh ngoài cùng tại ±(n−2)/2·s).
   - Với mặc định (n = 10): tầng chẵn có đinh tự do tại ±0.5 s … ±3.5 s và nửa đinh trên tường; tầng lẻ có đinh tại 0, ±1 s … ±4 s.
   - Mọi khe xu phải lọt (giữa hai đinh cùng tầng, giữa hai đinh ở hai tầng kề nhau theo đường chéo, giữa đinh và tường, giữa đinh và nửa đinh) phải ≥ 2·`coin_radius` + `min_gap_margin` (công thức `min_passage_gap`); validator chặn cấu hình vi phạm.
2. Hệ số hũ theo thứ tự trái → phải là `bucket_multipliers` = `[x5][x2][x1][x2][x5]`; độ rộng mỗi hũ theo `bucket_width_weights` (tổng chuẩn hóa về bề rộng trong của bàn). Hai hũ ngoài cùng là **hũ jackpot** (`bucket_is_jackpot`).
3. Mỗi hũ có một vùng cảm biến ở miệng hũ; xu chạm cảm biến là đã "rơi vào hũ" — không cần chờ xu nằm yên.
4. Xu không va chạm với nhau (ma trận layer); xu chỉ va chạm đinh, tường, vách hũ. Lý do: phân phối hũ không phụ thuộc mật độ xu (để kinh tế đo được) và giảm tải vật lý trên WebGL.

**R2. Vùng thả và lệch ngang**
1. Vùng thả là dải ngang ở giữa vạch thả, rộng `drop_zone_width_ratio` × bề rộng trong của bàn.
2. Chạm tay: tọa độ x của điểm chạm được kẹp vào vùng thả; tọa độ y của điểm chạm bị bỏ qua (xu luôn sinh ở vạch thả).
3. Mọi xu (mọi nguồn) nhận thêm một độ lệch ngang ngẫu nhiên đều trong ±`drop_jitter` (đơn vị: khoảng cách đinh), rồi bị kẹp vào bên trong tường.
4. Nguồn không có điểm chạm (tự động, mưa xu, Bùng Xu) chọn x ngẫu nhiên đều trong vùng thả.

**R3. Nguồn xu** — mỗi xu mang một `origin`:
| Origin | Kích hoạt | Ghi chú |
|---|---|---|
| `Manual` | Người chơi chạm trong khung bàn | Chịu cooldown chạm (R4) |
| `Auto` | Bộ hẹn giờ thả tự động, chỉ chạy khi có ít nhất một nguồn bật auto-drop | Mặc định TẮT |
| `Rain` | Lệnh "thả loạt N xu" từ hệ khác (Boosts: Mưa Đồng Xu; Rewarded Ads) | Vào hàng đợi loạt |
| `JackpotBurst` | Bùng Xu (R7) | Không thể kích hoạt Bùng Xu tiếp |

**R4. Nhận xu lên bàn (admission)**
1. Một cú chạm `Manual` được nhận khi **hai** điều kiện đúng, xét sau khi hàng đợi loạt đã sinh xu trong cùng bước mô phỏng (R4.3): đã qua `tap_cooldown` kể từ cú chạm được nhận gần nhất; số xu đang hoạt động < `max_active_coins`. Không thỏa → cú chạm bị bỏ, không xếp hàng, phát `BoardBusy` (phản hồi "bàn bận" nhẹ, UI Requirements). Cú chạm bị bỏ không dời mốc cooldown.
2. Xu `Rain` và `JackpotBurst` vào **hàng đợi loạt** (FIFO) và được sinh lần lượt, cách nhau `burst_spawn_interval`, mỗi khi số xu hoạt động < `max_active_coins` − `manual_reserved_slots` (các chỗ cuối cùng luôn để dành cho chạm tay). Hàng đợi có trần `burst_queue_max`; xu vượt trần được **trả ngay** (R9.3), không bao giờ mất. Khi bàn nhận `Paused`, toàn bộ hàng đợi cũng được trả ngay (R11.3).
3. Thứ tự trong **mỗi bước mô phỏng**: hàng đợi loạt sinh xu trước (nếu đã qua `burst_spawn_interval` và số xu hoạt động < `max_active_coins` − `manual_reserved_slots`); sau đó `Auto` được nhận nếu số xu hoạt động < `max_active_coins` − `manual_reserved_slots`; `Manual` được nhận nếu số xu hoạt động < `max_active_coins`. Kết quả: trong lúc Mưa Đồng Xu (sinh nhanh hơn bàn kịp giải quyết) người chơi vẫn chạm được, không gặp "bàn bận".
4. Xu `Auto` không chịu `tap_cooldown`; nó chịu chu kỳ auto-drop (Formulas) và trần `max_active_coins` − `manual_reserved_slots`. Một nhịp auto-drop gặp trần thì bị bỏ (không dồn, không phát `BoardBusy`).
5. Mỗi xu được nhận lên bàn mượn một đối tượng từ pool `plinko.coin` (Object Pooling); xu `Queued` không mượn gì. Nếu pool trả rỗng (chính sách Fixed cạn — lỗi lập trình, Object Pooling R7.1), xu coi như **không được nhận**: `Manual` → bỏ, phát `BoardBusy`, không dời mốc cooldown; `Auto` → bỏ nhịp; xu loạt → ở lại đầu hàng đợi, thử lại ở lần sinh kế tiếp. Không xu nào bị mất và không phát `CoinResolved`.
6. Chạm tay đến Plinko dưới dạng `WorldPress {worldPos, screenPos}` từ input router của Bootstrap, đã lọc UI, đã qua cổng chặn input toàn cục (Bootstrap B7, B9.5). Plinko **không** có cổng chặn riêng và không tự lọc UI/modal; nó chỉ có cờ `acceptManualTaps` (mặc định bật) để Tutorial bật/tắt riêng việc nhận xu `Manual`. Khi cờ tắt, mọi `WorldPress` bị bỏ im lặng: không sinh xu, không phát `BoardBusy`, không dời mốc cooldown. Cờ không ảnh hưởng `Auto`, `Rain`, `JackpotBurst`. Muốn chặn toàn bộ chạm world thì Tutorial dùng token cổng chặn của Bootstrap (B7), không dùng cờ này.

**R5. Vật lý xu**
1. Xu là Rigidbody2D tròn, trọng lực `coin_gravity_scale`, sinh với vận tốc ban đầu bằng 0.
2. Độ nảy hiệu lực của va chạm xu–đinh = độ nảy gốc × hệ số nảy từ các nguồn, **kẹp tại `bounciness_cap` < 1.0** (Formulas). Độ nảy của va chạm chỉ lấy theo đinh (xu có độ nảy 0 và cách kết hợp độ nảy phải không làm tăng giá trị đó — chế độ combine của PhysicsMaterial2D → ADR). Không nguồn nào có thể làm va chạm sinh thêm năng lượng: năng lượng cơ học của một xu sau va chạm không vượt trước va chạm quá `energyGainTolerance` (dung sai hiệu chỉnh vị trí của solver).
3. Modifier không hợp lệ (âm, NaN, vô cực) được coi là trung tính: hệ số nảy = 1, tốc độ auto-drop = 0. Hệ số nảy hữu hạn trong [0, 1) được kẹp lên 1: không nguồn nào làm đinh nảy kém hơn độ nảy gốc (khớp `BouncinessModifier` ≥ 1 của `pet-catalog.md` R6), nên `bounciness_modifier` ≥ 1 và độ nảy hiệu lực không bao giờ dưới `base_peg_bounciness`.
4. Tốc độ xu bị kẹp tại `max_coin_speed` mỗi bước vật lý.

**R6. Chạm đinh**
1. Mỗi va chạm xu–đinh (kể cả nửa đinh gắn tường) phát sự kiện `PegHit` (payload ở R12; Feedback & Juice dùng cho âm thanh/VFX, Shelf & Synergy cho thú).
2. Một `PegHit` là **được tính** (`scored = true`) chỉ khi xu chưa đạt `max_scored_peg_hits` lần được tính **và** xu chưa từng chạm đinh này hoặc đã qua `peg_hit_debounce` kể từ lần xu này chạm chính đinh này (debounce theo cặp xu–đinh). Chỉ `PegHit` được tính mới có giá trị kinh tế (ví dụ thú Thỏ Nhào Lộn).
3. Âm thanh chạm đinh bị giới hạn bởi Audio (số voice); Plinko không chịu trách nhiệm trộn âm.

**R7. Jackpot và Bùng Xu**
1. Xu có origin khác `JackpotBurst` rơi vào hũ jackpot → phát `JackpotHit { coinId, origin, bucketIndex, worldPosition }` (R12) và xếp `jackpot_burst_count` xu `JackpotBurst` vào hàng đợi loạt.
2. Xu `JackpotBurst` vẫn được nhân hệ số hũ bình thường (kể cả x5) và vẫn phát `JackpotHit` (với `origin = JackpotBurst`) khi trúng hũ jackpot, nhưng **không** tạo Bùng Xu mới (chặn dây chuyền). Trường `origin` cho phép hệ nghe lọc xu Bùng Xu (ví dụ `includeBurstCoins` của Pet Catalog).

**R8. Xu kẹt và hết hạn**
1. Xu có tốc độ < `stuck_speed_threshold` liên tục trong `stuck_time` giây, hoặc tồn tại quá `coin_max_lifetime` giây, bị coi là **kẹt**: despawn và trả theo `stuck_payout_multiplier` (R9).
2. Xu kẹt không phát `JackpotHit` và không tạo Bùng Xu.

**R9. Trả thưởng**
1. Mỗi xu được giải quyết **đúng một lần**: khi chạm cảm biến hũ, khi bị coi là kẹt, hoặc khi bị trả ngay. Xu đã có đối tượng pool trả đối tượng đó về pool `plinko.coin` **ngay trong bước mô phỏng giải quyết nó** (Resolved → Pooled); **không có trạng thái Fizzling**: hiệu ứng tan của xu kẹt và punch hũ do Feedback & Juice vẽ bằng đối tượng của chính nó (hiệu ứng tan: pool trang trí riêng `feedback.coinFizzle`, RecycleOldest, Feedback & Juice đăng ký), từ vị trí trong `CoinStuck` / `bucketIndex`. Vì vậy số đối tượng xu Plinko mượn cùng lúc ≤ `max_active_coins` và `pools[plinko.coin].headroom` = 0 — phương án B, quyết định user (`decision-log.md`), khớp `object-pooling.md` Revision 4. Xu vào hũ không rơi tiếp trong hũ: nó biến mất ở cảm biến miệng hũ (R1.3).
2. Plinko phát `CoinResolved { coinId, origin, bucketIndex | none, multiplier, rawValue, scoredPegHits, reason }` với `reason ∈ {Bucket, Stuck, Overflow}` (`bucketIndex` = none khi `reason` ≠ `Bucket`) và `rawValue = base_coin_value × multiplier` (Formulas). `base_coin_value` là giá trị Plinko đọc từ `IBaseCoinValueSource.BaseCoinValue` **tại thời điểm giải quyết xu**; Economy Core cài đặt và đăng ký nguồn đó theo công thức `base_coin_value` của nó (Economy D4, `economy-core.md` R10). Không có nguồn nào được đăng ký (test, prototype, chạy không có Economy Core) → dùng dự phòng `PlinkoConfig.baseCoinValue` (= 1). Plinko không có setter hay input đẩy cho giá trị này và không khai `RunsAfter` bước `economy.core`. Plinko **không** cộng vào ví: Economy Core nhận sự kiện, áp modifier kênh Plinko cho **mọi** origin và reason, rồi cộng tiền (`economy-core.md` R5.1).
3. "Trả ngay" (tràn hàng đợi loạt, R4.2; hoặc xả hàng đợi loạt khi `Paused`, R11.3): xu được giải quyết với `instant_payout_multiplier` mà không cần mô phỏng, phát `CoinResolved` với `reason = Overflow`. `Overflow` nghĩa là "xu loạt được trả ngay không qua mô phỏng", không chỉ "tràn".

**R10. Thả tự động**
1. Auto-drop chạy khi tổng tốc độ auto-drop từ các nguồn (thú, boost) đã đăng ký qua `RegisterAutoDropRate` (R12) > 0; chu kỳ theo Formulas, có sàn `auto_drop_min_interval`.
2. Khi không còn nguồn nào, bộ hẹn giờ dừng ngay; xu đã thả vẫn rơi tiếp.

**R11. Vòng đời theo màn hình**
1. Bàn tiếp tục mô phỏng khi người chơi chuyển sang Tab 2/Tab 3 (xu, auto-drop, hàng đợi vẫn chạy; chỉ tắt render). Lý do: tiền không "đứng hình" khi đang sắp kệ.
2. Bàn chỉ tiến mô phỏng khi `IsSimulationRunning` của Bootstrap đúng (R11.4, Bootstrap B6.5). Khi đã chạy, bàn dừng mô phỏng **chỉ** khi nhận `Paused {reason}` của Bootstrap (trang trình duyệt bị ẩn hoặc app xuống nền, Boot D3, Bootstrap B6). `Resumed {reason, awaySeconds}` **không** có nghĩa là "chạy lại": sau `Resumed` Plinko đọc `IsSimulationRunning`; đúng (vòng đời về `Running`) → chạy tiếp; sai (vòng đời về `Booting.Curtain`, Bootstrap B6.9) → chờ `Interactive` (R11.4). Trong lúc Paused bàn không tiến bước mô phỏng; đồng hồ kẹt, tuổi xu, cooldown và nhịp auto-drop chỉ tăng theo bước mô phỏng nên không chạy; xu đang rơi đứng yên và tiếp tục nguyên trạng khi mô phỏng chạy lại (hàng đợi loạt thì đã được trả ở R11.3). **Cửa sổ chỉ mất focus mà vẫn hiển thị** (bấm ra ngoài canvas WebGL, mở thanh địa chỉ) không có `Paused` → mô phỏng tiếp tục, khớp Economy Core (Idle vẫn cộng) và Audio (vẫn phát). **Rewarded ad đang mở** cũng không có `Paused` (cờ "ad đang mở", Bootstrap B6.10, quyết định user B) → mô phỏng chạy tiếp ở mọi nền tảng, kể cả hàng đợi loạt. Plinko không tự nghe sự kiện focus, visibility hay quảng cáo của nền tảng. Thu nhập lúc vắng mặt thuộc Offline Earnings, không thuộc Plinko; Plinko bỏ qua `awaySeconds`.
3. **Xả hàng đợi loạt khi Paused** (quyết định user, `decision-log.md`): ngay khi nhận `Paused {reason}`, trong cùng lời xử lý và trước khi dừng mô phỏng, Plinko giải quyết **mọi** xu `Queued` (`Rain`, `JackpotBurst`) theo thứ tự FIFO bằng trả ngay (R9.3): `rawValue` = `base_coin_value` × `instant_payout_multiplier`, phát `CoinResolved(reason = Overflow, bucketIndex = none, scoredPegHits = 0)` cho từng xu, không phát `CoinSpawned`, không mượn đối tượng pool; hàng đợi rỗng sau đó. Lý do: thưởng đã trúng (Bùng Xu, Mưa Đồng Xu) không bao giờ mất khi trang/app bị đóng từ trạng thái ẩn (anti-pillar "không mất mát"). Ẩn tab để xả hàng đợi không có lợi: `instant_payout_multiplier` ≤ EV_mean (`coin_payout`), không cao hơn giá trị mô phỏng của xu đó. Xu đang Falling **không** được trả: chúng đứng yên chờ `Resumed`. `RequestBurst` đến trong lúc Paused cũng được trả ngay theo cùng luật (hàng đợi luôn rỗng khi Paused). Tối đa `max_active_coins` xu đang rơi có thể mất nếu phiên kết thúc khi đang Paused (MVP, chưa có Save — Edge Cases).
4. **Mô phỏng chỉ chạy khi `IsSimulationRunning`** (Bootstrap B6.5, B6.9; `IsSimulationRunning` đúng ⇔ vòng đời Bootstrap = `Running`):
   1. Mọi thứ tiến theo thời gian của bàn — bước vật lý (`PhysicsScene2D.Simulate`), bộ hẹn giờ auto-drop (R10), sinh xu từ hàng đợi loạt và nhịp `burst_spawn_interval` (R4.2), đồng hồ kẹt và tuổi xu (R8), mốc cooldown chạm (R4.1) — **chỉ** tiến khi `IsSimulationRunning` đúng. Mỗi khung, trước khi tiến bước nào, Plinko đọc `IsSimulationRunning`; sai → khung đó không có bước mô phỏng nào. Không có bước "bù" cho thời gian đã chờ. **Kẹp bước khi khung dài:** mỗi khung, thời gian mô phỏng tiến = `min(dt, max_sim_step)` (knob `PlinkoConfig.maxSimStep`); phần dt vượt bị bỏ, không bù (khớp Economy `maxFrameDelta`, Bootstrap B6.10.1: sau `Resumed`/khung giật, khung đầu không nhảy vọt).
   2. Mô phỏng bắt đầu ở `Interactive` (khung đầu tiên `IsSimulationRunning` đúng). Bộ hẹn giờ auto-drop bắt đầu đếm từ 0 lúc đó: xu `Auto` đầu tiên sinh sau một chu kỳ `auto_drop_interval` đầy đủ tính bằng thời gian mô phỏng.
   3. Sau `Resumed`, Plinko đọc `IsSimulationRunning` thay vì coi `Resumed` là chạy lại (R11.2): sai (vòng đời về `Booting.Curtain`) → bàn về "Chờ mở cửa" và chờ `Interactive`.
   4. **Trước `Interactive`** (trạng thái bàn "Chờ mở cửa", sau `Run()` của R13 hoặc sau `Resumed` về `Booting.Curtain`): `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister` vẫn được nhận và cập nhật bảng nguồn (R12.5) nhưng không có nhịp auto-drop nào; `RequestBurst` xếp xu vào hàng đợi loạt như bình thường (tràn `burst_queue_max` vẫn trả ngay theo R4.2) và các xu đó chỉ sinh sau `Interactive`; mọi `WorldPress` bị bỏ im lặng (không sinh xu, không phát `BoardBusy`, không dời mốc cooldown — thực tế router của Bootstrap không giao chạm world dưới rèm); cờ `acceptManualTaps` đặt được. Không có xu nào đang rơi ở trạng thái này (`Interactive` chỉ phát một lần mỗi lần nạp scene và vòng đời không quay từ `Running` về `Booting.Curtain`).
   5. `Paused` nhận khi đang ở `Booting.Curtain` vẫn xả toàn bộ hàng đợi loạt theo R11.3 (trả ngay, `reason = Overflow`), và `RequestBurst` đến trong lúc Paused vẫn được trả ngay — luật xả không phụ thuộc mô phỏng đã từng chạy hay chưa.
   6. Ở `BootFailed` và `Quitting`, `IsSimulationRunning` sai nên không có bước mô phỏng nào; Plinko không tự nghe các sự kiện đó.

**R12. Sự kiện và API công bố** (hợp đồng do Plinko sở hữu; cơ chế phát sự kiện → ADR)
1. `coinId` là số nguyên tăng dần, duy nhất trong phiên, cấp khi xu được nhận lên bàn (R4) hoặc khi được xếp vào hàng đợi loạt; xu từ hàng đợi giữ `coinId` cấp lúc xếp hàng, nên xu `Overflow` cũng có `coinId`. `worldPosition` là tọa độ world 2D của bàn (camera Plinko).
2. Sự kiện công bố — Plinko chỉ phát, không gọi hệ nghe nào:

| Sự kiện | Payload | Phát khi | Hệ nghe |
|---|---|---|---|
| `CoinSpawned` | `{ coinId, origin, burstIndex \| none, worldPosition }` | Xu chuyển sang Falling ở vạch thả (mọi origin). `burstIndex` = thứ tự của xu trong loạt Bùng Xu sinh ra nó (0 … `jackpot_burst_count` − 1) khi `origin = JackpotBurst`, none với origin khác. `worldPosition` = điểm sinh (`drop_x`, vạch thả) | Feedback & Juice (pop-in; cue `Plinko.CoinSpawn` / `CoinSpawnAuto` / `JackpotBurstSpawn`) |
| `PegHit` | `{ coinId, pegId, rowIndex, scored, worldPosition }` | Mỗi va chạm xu–đinh hoặc xu–nửa đinh gắn tường (R6). `pegId` = id ổn định của đinh trong bàn đang dựng; `rowIndex` = tầng r của đinh (0 = tầng trên cùng … `peg_rows` − 1); `scored` theo `peg_hit_scored`; `worldPosition` = điểm tiếp xúc | Feedback & Juice (đinh sáng; cao độ theo `rowIndex`), Shelf & Synergy (`PegHitBonus`, chỉ khi `scored`) |
| `JackpotHit` | `{ coinId, origin, bucketIndex, worldPosition }` | Xu (mọi origin) chạm cảm biến hũ jackpot (R7); phát trước `CoinResolved` của cùng xu, cùng bước. `worldPosition` = điểm xu chạm cảm biến | Feedback & Juice ("BONUS!", cue `Plinko.JackpotHit`), Shelf & Synergy (`JackpotBonus`, lọc theo `origin`) |
| `CoinResolved` | `{ coinId, origin, bucketIndex \| none, multiplier, rawValue, scoredPegHits, reason }`, `reason ∈ {Bucket, Stuck, Overflow}` (`Overflow` = trả ngay không qua mô phỏng: tràn hàng đợi R4.2 hoặc xả hàng đợi khi `Paused` R11.3) | Đúng một lần mỗi xu (R9). `bucketIndex` = none khi `reason` ≠ `Bucket`; `multiplier` = hệ số đã dùng (hệ số hũ / `stuck_payout_multiplier` / `instant_payout_multiplier`); `scoredPegHits` = số `PegHit` được tính của xu (0 với `Overflow`) | Economy Core (`economy-core.md` R5.1), Shelf & Synergy, Tutorial & Onboarding, Feedback & Juice (punch hũ theo `bucketIndex`) |
| `CoinStuck` | `{ coinId, origin, worldPosition }` | Xu vào Stuck (R8), ngay trước `CoinResolved(reason = Stuck)` cùng bước. `worldPosition` = vị trí xu lúc despawn. Chỉ để trình bày, không có giá trị kinh tế | Feedback & Juice (hiệu ứng tan, cue `Plinko.CoinFizzle`) |
| `BoardBusy` | `{ worldPosition }` | Một `WorldPress` bị bỏ vì cooldown, trần xu hoặc pool rỗng (R4.1, R4.5). Không phát cho nhịp `Auto`, xu loạt, hay khi `acceptManualTaps` tắt. `worldPosition` = điểm chạm đã kẹp vào vùng thả, trên vạch thả, trước jitter | Feedback & Juice (khấc vùng thả lắc, cue `Plinko.BoardBusy`) |

   Thứ tự của một xu: `CoinSpawned` → `PegHit`… → (`JackpotHit` hoặc `CoinStuck`) → `CoinResolved`. Xu `Overflow` chỉ có `CoinResolved`.
3. Plinko **không gọi Audio**: Feedback & Juice ánh xạ sự kiện → `cueId` và gọi `Play(cueId, …)` của Audio; Audio không nghe sự kiện Plinko (Audio D1, A15). Plinko cũng không gọi Economy Core, Shelf hay Feedback.
4. API nhận vào:

| API | Hệ gọi | Hành vi |
|---|---|---|
| `RequestBurst(count, origin = Rain)` | Boosts, Rewarded Ads | Xếp `count` xu `Rain` vào hàng đợi loạt (R4.2); `count` ≤ 0 bị bỏ qua |
| `RegisterAutoDropRate(sourceId, rate)` | Shelf & Synergy (thú `AutoDropRate`), Boosts | Đặt hoặc thay đóng góp tốc độ auto-drop của nguồn `sourceId` |
| `RegisterBouncinessFactor(sourceId, factor)` | Shelf & Synergy (thú `BouncinessModifier`), Boosts | Đặt hoặc thay hệ số nảy của nguồn `sourceId` |
| `Unregister(sourceId)` | Như hai dòng trên | Gỡ mọi đóng góp của nguồn (tốc độ và hệ số nảy); `sourceId` chưa đăng ký → bỏ qua |
| `acceptManualTaps` (cờ) | Tutorial & Onboarding | R4.6 |
| `IBaseCoinValueSource { double BaseCoinValue { get; } }` (interface do Plinko sở hữu) | Economy Core cài đặt và đăng ký | R9.2 |

5. **Plinko sở hữu luật cộng dồn**: đăng ký lại cùng `sourceId` thay giá trị cũ (không cộng đôi); mỗi đóng góp được làm sạch theo R5.3 (không hợp lệ → tốc độ 0, hệ số 1; hệ số nảy hữu hạn < 1 → 1); Plinko tự tính `auto_rate_multiplier` = **tổng** các tốc độ và `bounciness_modifier` = **tích** các hệ số, rồi kẹp bằng `auto_drop_min_interval` và `bounciness_cap` (Formulas). Hệ gọi không tự cộng/nhân trước khi gửi. **Độ lớn** của từng đóng góp thuộc hệ gọi: thú — `PetDefinition.effects[i].magnitude` của Pet Catalog (Shelf & Synergy thi hành); boost — Boosts. Bảng nguồn được cấp phát sẵn, không cấp phát khi chạy (AC-38).

**R13. Bước khởi động** (hợp đồng `IBootStep` của Bootstrap B2; cùng mẫu với `economy.core`, `pet.catalog`, `pooling.service`)
1. Plinko tự đăng ký **một** bước khi scene nạp: `Id` = `plinko.board`, `Phase` = `GameplayInit`, `RunsAfter` rỗng, `IsCritical` = đúng. Bootstrap chỉ biết interface, không tham chiếu Plinko. `RunsAfter` rỗng vì Plinko không đọc gì của hệ cùng pha lúc khởi động: `baseCoinValue` được kéo lúc giải quyết xu và có dự phòng (R9.2), nên không khai `economy.core`; Bootstrap xếp bước theo thứ tự chữ của `Id` (B3.2). Pool đã nhận đăng ký vì `pooling.service` (pha `ServiceInit`) xong trước và `pooling.prewarm` (pha `Prewarm`) chạy sau.
2. `Run()` (Bootstrap gọi đúng một lần): đăng ký pool `plinko.coin` với `client_declared_max` = `max_active_coins`, `forbid_recycle` = true (Object Pooling R3); dựng bàn từ `PlinkoConfig` (R1); đăng ký viewport world `PlinkoViewport` + camera Plinko vào router và gốc hình ảnh của tab `Board` (Bootstrap B9). Mọi việc xong trong cùng khung → `IsDone` = đúng; bàn ở trạng thái **Chờ mở cửa** (R11.4), chưa có xu, hàng đợi rỗng, chưa tiến bước mô phỏng nào; bàn chỉ sang Idle khi `Interactive` phát (`IsSimulationRunning` đúng).
3. Đăng ký pool bị từ chối (Object Pooling R3.2) → `IsFailed` = đúng, Plinko không dựng bàn và không đăng ký viewport; vì bước là cốt lõi, Bootstrap vào BootFailed (bảng "Thử lại", Bootstrap B4). Ngoại lệ trong `Run()` được Bootstrap coi như `IsFailed`.

**Người chơi không thể**: thả xu ngoài vùng thả, nhắm chính xác một hũ, giữ hơn `max_active_coins` xu trên bàn, tạo Bùng Xu dây chuyền, làm xu nảy mạnh hơn lúc rơi.

### States and Transitions

Trạng thái của một đồng xu:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Pooled | Khởi tạo pool; hoặc xu vừa được giải quyết | Được nhận lên bàn (R4) | Ẩn, không mô phỏng |
| Queued | Yêu cầu `Rain`/`JackpotBurst` chưa có chỗ | Có chỗ và đã qua `burst_spawn_interval` → Falling; tràn `burst_queue_max` hoặc bàn nhận `Paused` (R11.3) → Resolved (trả ngay, `reason = Overflow`) | Chỉ là một mục trong hàng đợi, chưa lấy đối tượng từ pool |
| Falling | Sinh ở vạch thả | Chạm cảm biến hũ → Resolved; thỏa điều kiện kẹt → Stuck | Mô phỏng vật lý, phát `PegHit`, đếm chạm được tính |
| Stuck | R8 | Ngay lập tức → Resolved | Phát `CoinStuck` (Feedback & Juice vẽ hiệu ứng "tan" bằng đối tượng của chính nó), không phát `JackpotHit` |
| Resolved | Hũ / kẹt / trả ngay | Đã phát `CoinResolved` → Pooled, **cùng bước mô phỏng** (trả đối tượng pool ngay; không có trạng thái Fizzling, R9.1) | Phát sự kiện đúng một lần |

Trạng thái của bàn:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Chờ mở cửa | `Run()` xong (R13.2); hoặc nhận `Resumed` mà `IsSimulationRunning` sai (vòng đời về `Booting.Curtain`, Bootstrap B6.9) | `Interactive` (`IsSimulationRunning` đúng) → Idle nếu hàng đợi rỗng, Active nếu không; nhận `Paused` → Paused | Không tiến bước mô phỏng nào (R11.4): không vật lý, không nhịp auto-drop, không sinh xu từ hàng đợi, đồng hồ kẹt/tuổi xu/cooldown đứng yên; nhận đăng ký nguồn (R12.4); `RequestBurst` xếp hàng; bỏ im lặng mọi `WorldPress` (không `BoardBusy`) |
| Idle | Không có xu hoạt động, hàng đợi rỗng, `IsSimulationRunning` đúng | Có xu được nhận | Chờ chạm; auto-drop có thể chạy |
| Active | ≥ 1 xu hoạt động hoặc hàng đợi không rỗng, và còn chỗ | Hết xu → Idle; đạt `max_active_coins` → Saturated | Nhận chạm theo R4 |
| Saturated | Số xu hoạt động = `max_active_coins` | Một xu được giải quyết → Active | Bỏ chạm `Manual` (phát `BoardBusy`) và nhịp `Auto`; hàng đợi chờ. Khi số xu hoạt động ≥ `max_active_coins` − `manual_reserved_slots` nhưng < `max_active_coins`, bàn vẫn ở Active: chỉ hàng đợi và `Auto` phải chờ |
| Paused | Nhận `Paused {reason}` của Bootstrap (tab trình duyệt ẩn / app xuống nền, Boot D3) khi đang ở Chờ mở cửa (vòng đời `Booting.Curtain`), Idle, Active hoặc Saturated | Nhận `Resumed {reason, awaySeconds}` → đọc `IsSimulationRunning` (R11.4): đúng → Idle / Active / Saturated theo số xu hoạt động và hàng đợi; sai → Chờ mở cửa | Lúc vào: trả ngay toàn bộ hàng đợi loạt (`CoinResolved(reason = Overflow)`, R11.3). Trong trạng thái: không mô phỏng, giữ nguyên xu đang rơi, mốc cooldown và nhịp auto-drop; hàng đợi luôn rỗng (`RequestBurst` đến lúc này được trả ngay); bỏ mọi `WorldPress` (thực tế không có). Chỉ mất focus mà vẫn hiển thị, hoặc rewarded ad đang mở, thì **không** vào Paused (R11.2) |

### Interactions with Other Systems

| Hệ | Plinko nhận vào | Plinko phát ra | Chủ interface |
|---|---|---|---|
| Economy Core (`economy-core.md`) | `baseCoinValue` runtime: Plinko kéo `IBaseCoinValueSource.BaseCoinValue` lúc giải quyết xu; Economy Core cài đặt nguồn theo `base_coin_value` (D4, R10); không có nguồn → dự phòng `PlinkoConfig.baseCoinValue` (= 1) | `CoinResolved { coinId, origin, bucketIndex \| none, multiplier, rawValue, scoredPegHits, reason ∈ {Bucket, Stuck, Overflow} }` | Plinko sở hữu sự kiện và interface `IBaseCoinValueSource`; Economy Core phụ thuộc Plinko, sở hữu cài đặt, công thức `base_coin_value`, modifier và ví |
| Shelf & Synergy | `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister` (R12.4) với độ lớn lấy từ Pet Catalog | `PegHit` (`scored`), `JackpotHit` (`origin`), `CoinResolved` để thú phản ứng (Thỏ, Mèo Chiêu Tài) | Plinko sở hữu API modifier, luật cộng dồn và các trần |
| Boosts | `RequestBurst(count, Rain)`; tốc độ auto-drop / hệ số nảy tạm thời qua cùng API đăng ký nguồn | — | Plinko |
| Rewarded Ads (Alpha) | `RequestBurst(count, Rain)` | — | Plinko |
| Object Pooling | Đối tượng xu (pool `plinko.coin`) | Trả đối tượng ngay khi Resolved | Object Pooling |
| Audio | — (gián tiếp qua Feedback & Juice) | — Plinko không gọi Audio, Audio không nghe Plinko (Audio D1) | Audio sở hữu SoundCue, giới hạn voice; Feedback sở hữu bảng sự kiện → `cueId` |
| Feedback & Juice | — | `CoinSpawned`, `PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy` (payload R12, kèm `worldPosition`) | Feedback & Juice (trình bày, gọi Audio, số bay theo `IncomeCredited` của Economy) |
| Tutorial & Onboarding | Cờ `acceptManualTaps` (chỉ bật/tắt nhận xu `Manual`, R4.6); chặn toàn bộ chạm world thì dùng cổng chặn của Bootstrap | `CoinResolved` đầu tiên | Plinko (cờ); Bootstrap (cổng chặn toàn cục); Tutorial (kịch bản) |
| Bootstrap & Scene Flow | `WorldPress {worldPos, screenPos}` đã lọc UI và cổng chặn; `ActiveTab` / `TabChanged`; `Paused` / `Resumed`; `Interactive` + truy vấn `IsSimulationRunning` (mô phỏng, auto-drop, hàng đợi loạt chỉ tiến khi đúng; đọc lại sau `Resumed` — R11.2, R11.4, Bootstrap B6.5, B6.9); `FrameChanged` | Bước `plinko.board` (`GameplayInit`, cốt lõi, `RunsAfter` rỗng — R13): đăng ký pool `plinko.coin`, `PlinkoViewport` + camera vào router, gốc hình ảnh tab `Board` | Bootstrap (router, cổng chặn, tab, vòng đời, hợp đồng `IBootStep`) → ADR định tuyến input |

→ Thành ADR: cơ chế phát sự kiện (C# event / message bus); cách chuyển điểm chạm màn hình sang world-space qua camera Plinko; chế độ mô phỏng Physics2D cho test — **phải xác minh trước khi viết test sim** (AC-17, AC-18, AC-26, AC-30, AC-31): `SceneManager.CreateScene` với `LocalPhysicsMode.Physics2D` có thể chỉ dùng được trong Play mode; nếu đúng, các test này chuyển sang Play mode hoặc dùng cách mô phỏng khác do ADR chọn; cấu hình `Physics2D.reuseCollisionCallbacks` và cấu trúc dữ liệu cấp phát sẵn cho debounce (AC-38).

## Formulas

Đơn vị dài **s** = `peg_spacing` (khoảng cách ngang giữa hai đinh). Bàn rộng trong `pegs_per_row` s (tường tại ±`pegs_per_row`/2 s); biên hũ theo `bucket_width_weights` (công thức `bucket_probability_and_ev`). Với mặc định (`pegsPerRow` = 10, trọng số đều): bàn rộng 10 s, mỗi hũ rộng 2 s: x5 [−5,−3], x2 [−3,−1], x1 [−1,1], x2 [1,3], x5 [3,5]. Giá trị mặc định đến từ đề xuất của game-designer (2026-10-21), sửa sau review: thưởng nhắm đo trên **EV hiệu dụng** (gồm Bùng Xu) nên vùng thả thu về 0.14 và lệch ngang tăng lên ±1.0 s (user chọn phương án B); xu gốc = 1. Mô hình Galton chưa tính nửa đinh gắn tường (làn ngoài cùng tầng chẵn rộng hơn làn trong) — mô phỏng ở AC-30 là thước đo cuối. F7–F9 là **mô hình thiết kế** (Galton lý tưởng) để chọn knob; giá trị thật được chốt bằng mô phỏng ở Acceptance Criteria.

### drop_x

`drop_x = clamp( clamp(tap_x, zone_min, zone_max) + U(−drop_jitter, +drop_jitter) × peg_spacing, wall_left + coin_radius, wall_right − coin_radius )`
với `zone_min/max = board_center_x ∓ drop_zone_width_ratio × board_inner_width / 2`; nguồn không có điểm chạm thay `tap_x` bằng `U(zone_min, zone_max)`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| tap_x | float (world) | bất kỳ | calculated | x world của điểm chạm (đã lọc UI) |
| drop_zone_width_ratio | float | 0.10–0.20 | SO field `PlinkoConfig.dropZoneWidthRatio` | độ rộng vùng thả / bề rộng trong |
| drop_jitter | float (s) | 0.5–1.25 | SO field `PlinkoConfig.dropJitter` | biên độ lệch ngang đều |
| peg_spacing | float (world u) | 0.45–0.65 | SO field `PlinkoConfig.pegSpacing` | khoảng cách ngang giữa đinh |
| board_inner_width | float (world u) | = `(pegs_per_row) × peg_spacing` | calculated | bề rộng giữa hai tường |
| coin_radius | float (world u) | 0.12–0.20 | SO field `PlinkoConfig.coinRadius` | bán kính xu |

**Output range:** trong [−5 s + r, 5 s − r]; với mặc định (vùng ±0.7 s, lệch ±1.0 s), điểm sinh nằm trong ±1.7 s quanh tâm.
**Example:** chạm ở x = −4 s → kẹp về −0.7 s → jitter +0.6 s → `drop_x` = −0.1 s.

### min_passage_gap

`min_passage_gap = min( peg_spacing − 2·peg_radius, peg_spacing − peg_radius, 1.5·peg_spacing − peg_radius − wall_bump_radius, sqrt( (peg_spacing/2)² + (row_spacing_ratio·peg_spacing)² ) − 2·peg_radius )`
Bốn vế lần lượt là: khe giữa hai đinh cạnh nhau cùng tầng; khe giữa đinh ngoài cùng tầng lẻ (±(n−2)/2·s) và tường; khe giữa đinh tự do ngoài cùng tầng chẵn (±(n−3)/2·s) và nửa đinh gắn tường; khe chéo giữa hai đinh ở hai tầng kề nhau (lệch ngang s/2, lệch dọc `row_spacing_ratio`·s). Hệ số ½ của vế chéo là hằng hình học của lưới so le (tầng lẻ dịch nửa bước đinh, R1.1), không phải knob. Ràng buộc: `min_passage_gap ≥ 2·coin_radius + min_gap_margin`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| peg_spacing | float (u) | 0.45–0.65 | SO field `PlinkoConfig.pegSpacing` | khoảng cách ngang giữa đinh |
| row_spacing_ratio | float | 0.76–1.0 | SO field `PlinkoConfig.rowSpacingRatio` | khoảng cách tầng chia cho `peg_spacing` |
| peg_radius | float (u) | 0.04–0.10 | SO field `PlinkoConfig.pegRadius` | bán kính đinh |
| wall_bump_radius | float (u) | 0.04–0.10 | SO field `PlinkoConfig.wallBumpRadius` | bán kính nửa đinh gắn tường |
| coin_radius | float (u) | 0.12–0.19 | SO field `PlinkoConfig.coinRadius` | bán kính xu |
| min_gap_margin | float (u) | 0.03–0.12 | SO field `PlinkoConfig.minGapMargin` | biên an toàn trên đường kính xu |

**Output range:** với mặc định: khe đinh–đinh 0.43 u, đinh–tường (tầng lẻ) 0.49 u, đinh–nửa đinh (tầng chẵn) 0.705 u, khe chéo sqrt(0.275² + (0.866 × 0.55)²) − 0.12 ≈ 0.43 u (lưới tam giác đều nên bằng khe đinh–đinh) → `min_passage_gap` = 0.43 u ≥ 0.32 + 0.06 = 0.38 u → hợp lệ (dư 0.05 u). Khe chéo giảm khi `rowSpacingRatio` giảm; với các knob khác ở mặc định, `rowSpacingRatio` tối thiểu hợp lệ ≈ 0.76 (khe chéo = 0.38 u). Trước khi sửa, khe đinh–tường tầng chẵn là 0.5 × 0.55 − 0.06 = 0.215 u < 0.32 u.
**Example:** tăng `coinRadius` lên 0.20 → yêu cầu 0.46 u > 0.43 u → validator báo lỗi. Giảm `rowSpacingRatio` xuống 0.75 (giữ mặc định còn lại) → khe chéo = sqrt(0.275² + 0.4125²) − 0.12 ≈ 0.376 u < 0.38 u → validator báo lỗi dù ba vế còn lại vẫn hợp lệ.

### effective_bounciness

`effective_bounciness = min(base_peg_bounciness × bounciness_modifier, bounciness_cap)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| base_peg_bounciness | float | 0.3–0.6 | SO field `PlinkoConfig.basePegBounciness` | độ nảy gốc của đinh (vật liệu xu có độ nảy 0) |
| bounciness_modifier | float | ≥ 1 (thường 1.0–3.0) | calculated: tích các hệ số đăng ký qua `RegisterBouncinessFactor` sau khi làm sạch (R5.3, R12.5); độ lớn từng nguồn thuộc Pet Catalog / Boosts | đóng góp âm/NaN/vô cực → 1; hữu hạn trong [0, 1) → 1 (R5.3); không có nguồn → 1 |
| bounciness_cap | float | 0.7–0.9, luôn < 1.0 | SO field `PlinkoConfig.bouncinessCap` | trần chống sinh năng lượng |

**Output range:** [`base_peg_bounciness`, `bounciness_cap`]: mặc định 0.5 (không nguồn nào hoặc tích hệ số = 1) đến 0.85 (trần); trên toàn safe range 0.3–0.9 (vì `base_peg_bounciness` ≤ 0.6 < 0.7 ≤ `bounciness_cap`); không bao giờ dưới độ nảy gốc và không bao giờ ≥ 1.0.
**Example:** Kẹo Bảy Màu (×1.7, `pet-catalog.md` R8) → min(0.5 × 1.7, 0.85) = **0.85**. Một nguồn đăng ký 0.5 → kẹp lên 1 → min(0.5 × 1, 0.85) = **0.5**.

### coin_payout

`raw_payout = base_coin_value × bucket_multipliers[bucket_index]`
`stuck_payout = base_coin_value × stuck_payout_multiplier`
`instant_payout = base_coin_value × instant_payout_multiplier`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| base_coin_value | double | ≥ 1 | input runtime: Economy Core cài đặt `IBaseCoinValueSource` theo công thức `base_coin_value` của `economy-core.md`, Plinko đọc lúc giải quyết xu (kéo, R9.2); dự phòng `PlinkoConfig.baseCoinValue` (= 1, chỉ cho test) | giá trị một xu trước hũ |
| bucket_multipliers | float[5] | mỗi phần tử 1–10 | SO field `PlinkoConfig.bucketMultipliers` | `[5, 2, 1, 2, 5]` |
| stuck_payout_multiplier | float | 0.5–1.0 | SO field `PlinkoConfig.stuckPayoutMultiplier` | trả cho xu kẹt; phải < EV trung bình để làm kẹt xu không có lợi |
| instant_payout_multiplier | float | 1.0–2.0, ≤ EV_mean | SO field `PlinkoConfig.instantPayoutMultiplier` | trả cho xu trả ngay (`reason = Overflow`): tràn hàng đợi hoặc xả hàng đợi khi `Paused` (R9.3, R11.3) |

**Output range:** 1–5 × `base_coin_value` xu thô mỗi xu với mặc định; Economy Core nhân tiếp.
**Example:** xu rơi hũ x5 với `base_coin_value` = 1 → `raw_payout` = 5. Xu kẹt → 1. Xu tràn hàng đợi → 1.9.

### peg_hit_scored

`scored = (coin.scored_hits < max_scored_peg_hits) AND (coin.never_hit(peg_id) OR t − coin.last_hit_time(peg_id) ≥ peg_hit_debounce)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| max_scored_peg_hits | int | 12–30 | SO field `PlinkoConfig.maxScoredPegHits` | trần số lần chạm đinh được tính / xu |
| peg_hit_debounce | float (s) | 0.05–0.25 | SO field `PlinkoConfig.pegHitDebounce` | khoảng chống lặp cho cặp (xu, đinh) |
| t | float (s) | ≥ 0 | calculated | thời gian mô phỏng (không phải giờ thực) |

**Output range:** mỗi xu có tối đa `max_scored_peg_hits` lần `scored = true`; một xu bình thường chạm 12–16 đinh nên trần chỉ cắt các xu bất thường.
**Example:** xu đã có 20 lần được tính chạm thêm đinh mới → `scored = false`, vẫn phát âm thanh.

### tap_admitted

`tap_admitted = (t − last_admitted_tap_time ≥ tap_cooldown) AND (active_coins_after_queue_spawn < max_active_coins)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| tap_cooldown | float (s) | 0.10–0.25 | SO field `PlinkoConfig.tapCooldown` | khoảng tối thiểu giữa hai cú chạm được nhận |
| max_active_coins | int | 12–40 | SO field `PlinkoConfig.maxActiveCoins` | trần xu đồng thời (ngân sách WebGL) |
| active_coins_after_queue_spawn | int | 0–max | calculated | xu đang Falling sau khi hàng đợi loạt đã sinh trong bước mô phỏng này (R4.3) |

**Output range:** tối đa 1 / `tap_cooldown` = 6.67 cú chạm được nhận mỗi giây.
**Example:** cú chạm sau cú trước 0.1 s → bị bỏ; sau 0.2 s và bàn có 10/24 xu → được nhận.

### auto_drop_interval

`auto_drop_interval = max(auto_drop_base_interval / auto_rate_multiplier, auto_drop_min_interval)` (chỉ khi `auto_rate_multiplier` > 0)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| auto_drop_base_interval | float (s) | 1–4 | SO field `PlinkoConfig.autoDropBaseInterval` | chu kỳ khi có đúng 1 đơn vị tốc độ |
| auto_rate_multiplier | float | ≥ 0 (thường 0–10) | calculated: tổng các tốc độ đăng ký qua `RegisterAutoDropRate` (R12.5, Plinko sở hữu luật cộng dồn); đóng góp âm/NaN/vô cực → 0 (R5.3) | 0 = tắt |
| auto_drop_min_interval | float (s) | 0.3–1.0 | SO field `PlinkoConfig.autoDropMinInterval` | sàn chu kỳ |

**Output range:** ≥ `auto_drop_min_interval` (0.4 s); = `auto_drop_base_interval` (2.0 s) khi tổng tốc độ = 1; **không có trần trên**: `auto_drop_base_interval` / `auto_rate_multiplier` tăng không giới hạn khi tổng tốc độ → 0 (ví dụ Hamster một mình, `AutoDropRate` 0.27 → 7.41 s); dừng khi `auto_rate_multiplier` = 0. Nhịp auto-drop tối đa 1 / `auto_drop_min_interval` = 2.5 xu tự động/giây ≈ 37% trần chạm tay, giữ kênh chủ động đáng giá (Pillar 3).
**Example:** hai nguồn cộng tốc độ 2 → max(2.0 / 2, 0.4) = 1.0 s. Hamster một mình (0.27) → max(2.0 / 0.27, 0.4) ≈ **7.41 s**. Một nguồn 0.5 (AC-43) → **4.0 s**.

### galton_spread (mô hình thiết kế)

`sigma = sqrt(peg_rows × k_diff² / 4 + (drop_jitter)² / 3)` (đơn vị s)
`sigma_zone = sqrt(sigma² + W² / 12)` với `W = drop_zone_width_ratio × pegs_per_row` (đơn vị s; bề rộng trong của bàn = `pegs_per_row` s)

Các hệ số ¼ (phương sai một bước nảy ±½ s), ⅓ (phương sai của phân phối đều U(−a, a) = a²/3, áp cho lệch ±`drop_jitter`) và 1/12 (phân phối đều trên bề rộng W = W²/12) là hằng thống kê của mô hình Galton, không phải knob.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| peg_rows | int | 8–14 | SO field `PlinkoConfig.pegRows` | số tầng đinh |
| k_diff | float | 1.0–1.4 | calculated (đo bằng mô phỏng) | độ khuếch tán của vật lý thật so với Galton lý tưởng |
| drop_jitter | float (s) | 0.5–1.25 | SO field `PlinkoConfig.dropJitter` | biên độ lệch ngang đều |
| drop_zone_width_ratio | float | 0.10–0.20 | SO field `PlinkoConfig.dropZoneWidthRatio` | độ rộng vùng thả / bề rộng trong |
| pegs_per_row | int | 8–12 | SO field `PlinkoConfig.pegsPerRow` | số vị trí đinh tầng chẵn = bề rộng trong tính bằng s (R1.1) |
| W | float (s) | 0.8–2.4 | calculated | bề rộng vùng thả |

**Output range:** sigma 1.44–2.72 s trong khoảng an toàn (min: 8 tầng, `k_diff` 1.0, jitter 0.5 → sqrt(2 + 0.083) = 1.44; max: 14 tầng, `k_diff` 1.4, jitter 1.25 → sqrt(6.86 + 0.521) = 2.72); sigma_zone 1.46–2.80 s.
**Example:** 12 tầng, jitter 1.0, k_diff 1 → sigma = sqrt(3 + 0.333) = **1.826 s**; W = 0.14 × 10 = 1.4 s → sigma_zone = sqrt(3.333 + 0.163) = **1.870 s**.

### bucket_probability_and_ev (mô hình thiết kế)

`e_i = −n/2 + n × (Σ_{j<i} w_j) / (Σ_j w_j)`, i = 0 … B (đơn vị s; n = `pegs_per_row`, w = `bucket_width_weights`, B = số hũ = số phần tử của `bucket_multipliers`; e_0 = −n/2 là tường trái, e_B = n/2 là tường phải)
`p_i(x0) = Φ((e_{i+1} − x0)/sigma) − Φ((e_i − x0)/sigma)`, i = 0 … B−1, trong đó e_0 được thay bằng −∞ và e_B bằng +∞ (khối xác suất vượt tường dồn vào hũ ngoài cùng)
`p_jackpot(x0) = Σ_{i : bucket_is_jackpot[i]} p_i(x0)`
`EV(x0) = Σ_i bucket_multipliers[i] × p_i(x0)`
`EV_effective(x0) = EV(x0) + p_jackpot(x0) × jackpot_burst_count × EV_mean` (xu Bùng Xu sinh ngẫu nhiên đều trong vùng thả nên mỗi xu thưởng có giá trị kỳ vọng EV_mean)
`EV_effective_mean = EV_mean + p_jackpot_mean × jackpot_burst_count × EV_mean`
`aim_ratio = max over x0 of EV_effective(x0) / min over x0 of EV_effective(x0)` (đo trên EV hiệu dụng, không phải EV trực tiếp: bỏ Bùng Xu khỏi phép đo thì không thấy lợi thế gõ mép vùng thả)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| x0 | float (s) | [−W/2, W/2] | calculated | điểm thả trước jitter |
| sigma | float (s) | 1.44–2.72 | calculated (`galton_spread`) | độ lệch chuẩn vị trí xu ở đáy khi thả tại x0 |
| pegs_per_row | int | 8–12 | SO field `PlinkoConfig.pegsPerRow` | bề rộng trong của bàn tính bằng s (R1.1) |
| bucket_width_weights | float[5] | mỗi ô 0.8–1.2 | SO field `PlinkoConfig.bucketWidthWeights` | trọng số độ rộng hũ, chuẩn hóa về bề rộng trong (R1.2) |
| bucket_multipliers | float[5] | mỗi ô 1–10, đối xứng | SO field `PlinkoConfig.bucketMultipliers` | hệ số hũ trái → phải (R1.2) |
| bucket_is_jackpot | bool[5] | đúng hai hũ ngoài cùng | calculated (R1.2) | hũ jackpot |
| e_i | float (s) | [−n/2, n/2] | calculated | biên trái của hũ i (e_B = biên phải hũ cuối) |
| p_i | float | 0–1, Σ_i p_i = 1 | calculated | xác suất xu thả tại x0 rơi vào hũ i |
| Φ | function | — | constant (hàm toán học, không chỉnh được) | hàm phân phối chuẩn tích lũy |
| jackpot_burst_count | int | 3–8 | SO field `PlinkoConfig.jackpotBurstCount` | số xu của Bùng Xu |
| EV_mean, p_jackpot_mean | float | — | calculated | trung bình trên vùng thả (dùng sigma_zone, x0 = 0) |

**Output range (mục tiêu, verify bằng mô phỏng):** p_jackpot trung bình 10–13%, tại mọi x0 ≤ 18% và tại tâm ≥ 6%; EV_mean 1.85–2.05; `aim_ratio` (EV hiệu dụng max/min) ≤ 1.20; ở `bounciness_cap`: p_jackpot trung bình ≤ 20%, EV_mean ≤ 2.3. Các knob tương ứng ở Tuning Knobs (`PlinkoBalanceTargets.jackpotProb*`, `.ev*`, `.capJackpotProbMeanMax`, `.capEvMeanMax`).
**Example (mặc định sau sửa):** n = 10, trọng số đều → e = [−5, −3, −1, 1, 3, 5] s, hũ jackpot là hũ 0 và 4. Thả ở tâm → p_jackpot = p_0 + p_4 ≈ 10.0%, p_2 (hũ x1) ≈ 41.6%, EV ≈ 1.885, EV_effective ≈ 1.885 + 0.100 × 5 × 1.919 ≈ **2.85**; thả ở mép vùng (x0 = 0.7 s) → p_jackpot ≈ 12.5%, EV ≈ 1.986, EV_effective ≈ **3.19**; `aim_ratio` ≈ 3.19 / 2.85 ≈ **1.12** ("thưởng nhắm nhẹ"). Trung bình vùng: p_jackpot ≈ 10.9%, EV_mean ≈ 1.919, EV_effective_mean ≈ **2.96** (Bùng Xu ≈ 35% thu nhập Plinko). Bản trước sửa (vùng 0.24, lệch ±0.5): EV_effective 2.68 → 3.69, `aim_ratio` ≈ 1.38.

### coin_rate_ceiling

`coin_rate_ceiling = min(1 / tap_cooldown + auto_drop_rate, max_active_coins / median_fall_time)` (xu sơ cấp — `Manual` + `Auto` — được nhận mỗi giây)
`raw_rate_ceiling = coin_rate_ceiling × EV_effective_mean(effective_bounciness) × base_coin_value` (xu thô mỗi giây, trước modifier của Economy Core)
với `auto_drop_rate = 1 / auto_drop_interval` khi auto-drop chạy, = 0 khi dừng (R10); do đó `auto_drop_rate` ≤ 1 / `auto_drop_min_interval`.

**Trần mềm, Overflow vượt sức chứa bàn** (quyết định user (a), `decision-log.md`): xu `JackpotBurst` (và `Rain`) không có chỗ thì nằm trong hàng đợi loạt, tràn `burst_queue_max` (hoặc bị xả khi `Paused`, R11.3) thì được **trả ngay** `instant_payout_multiplier` (≈ EV_mean, R9.3) mà không chiếm chỗ trên bàn. **Overflow là nguồn vượt trần**: `max_active_coins` chỉ chặn xu sơ cấp; thu nhập Bùng Xu đi qua `reason = Overflow` dù bàn đầy, nên thu nhập thô ≈ số xu sơ cấp được nhận mỗi giây × EV hiệu dụng, vượt phần bàn kịp mô phỏng. `raw_rate_ceiling` là **trần mềm theo cấu hình** (cận trên mô hình, phụ thuộc auto-drop và độ nảy hiệu lực), **không phải trần cứng**: Plinko không kẹp thu nhập theo nó, Economy Core cũng không (`economy-core.md` R5.5) mà cân bằng theo bốn cấu hình (`EconomyBalanceTargets.plinkoRawRateCeilingAssumed`: độ nảy gốc không auto-drop / auto-drop ở sàn / độ nảy ở trần / cả hai). Các trần cứng của Plinko chỉ là cooldown chạm, `max_active_coins`, `auto_drop_min_interval` và `bounciness_cap`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| tap_cooldown | float (s) | 0.10–0.25 | SO field `PlinkoConfig.tapCooldown` | khoảng tối thiểu giữa hai cú chạm được nhận |
| auto_drop_rate | float (xu/s) | 0 – 1 / `auto_drop_min_interval` | calculated (`auto_drop_interval`) | nhịp auto-drop hiện tại; 0 khi không có nguồn |
| auto_drop_min_interval | float (s) | 0.3–1.0 | SO field `PlinkoConfig.autoDropMinInterval` | sàn chu kỳ auto-drop |
| max_active_coins | int | 12–40 | SO field `PlinkoConfig.maxActiveCoins` | trần xu đồng thời |
| median_fall_time | float (s) | 1.8–3.5 | calculated (đo); mục tiêu `PlinkoBalanceTargets.targetMedianFallTime` | thời gian rơi trung vị từ vạch thả tới hũ |
| effective_bounciness | float | [`base_peg_bounciness`, `bounciness_cap`] | calculated (`effective_bounciness`) | độ nảy hiệu lực của đinh |
| EV_effective_mean(b) | float | ≈ 2.96 ở độ nảy gốc; ≤ `PlinkoBalanceTargets.capEvMeanMax` × (1 + `PlinkoBalanceTargets.capJackpotProbMeanMax` × `jackpot_burst_count`) ở `bounciness_cap` | calculated: mô hình `bucket_probability_and_ev` ở độ nảy gốc; ở độ nảy khác đo bằng sim (AC-18, AC-30) | hệ số trả kỳ vọng mỗi xu sơ cấp, gồm Bùng Xu |
| jackpot_burst_count | int | 3–8 | SO field `PlinkoConfig.jackpotBurstCount` | số xu của Bùng Xu |
| instant_payout_multiplier | float | 1.0–2.0 | SO field `PlinkoConfig.instantPayoutMultiplier` | hệ số trả ngay cho xu tràn hàng đợi |
| base_coin_value | double | ≥ 1 | input runtime (R9.2) | giá trị một xu trước hũ |

**Output range:** `coin_rate_ceiling` 6.67 xu sơ cấp/giây khi không có auto-drop (24 / 2.5 = 9.6 không phải giới hạn), tối đa min(6.67 + 2.5, 9.6) ≈ 9.17 khi auto-drop ở sàn. `raw_rate_ceiling` (theo `base_coin_value`): **≈ 20 ở độ nảy gốc, không auto-drop** (giá trị đã duyệt, chỉ áp dụng ở điều kiện này); ≈ 27 khi auto-drop ở sàn; khi độ nảy hiệu lực = `bounciness_cap` (Kẹo Bảy Màu): ≈ 26 theo mục tiêu ngân sách +32% kênh Plinko (`plinko_target_uplift` = 0.324 của `pet-catalog.md`), ≈ 27.6 theo ước tính hiện tại +40% (`EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` = 0.40, `budget_ratio` 1.235), ≤ 30.7 ở biên mục tiêu (`T.capEvMeanMax`, `T.capJackpotProbMeanMax`) — AC-13c đo giá trị thật và cập nhật knob giả định của Economy Core; ≤ ≈ 42 khi cả auto-drop ở sàn và trần nảy. Về hàng đợi: khi cộng xu Bùng Xu (≈ 6.67 × (1 + 0.109 × 5) ≈ 10.3 xu/s) nhu cầu vượt 9.6 → hàng đợi loạt hấp thụ phần dư và phần tràn được trả ngay; hàng đợi chỉ dùng tới `max_active_coins` − `manual_reserved_slots` chỗ (22 / 2.5 ≈ 8.8 xu/s). Mưa Đồng Xu sinh 1 / `burst_spawn_interval` = 12.5 xu/s, nhanh hơn sức chứa → hàng đợi chạm trần của nó nhưng chạm tay vẫn còn `manual_reserved_slots` chỗ. Economy Core cân bằng theo `raw_rate_ceiling` của cấu hình đang xét, không theo một trần cố định.
**Example:** (a) spam liên tục, độ nảy gốc, không auto-drop: min(6.67, 9.6) × 2.96 × `base_coin_value` ≈ **19.7 ≈ 20 xu thô/giây**; test dùng `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoin` × `base_coin_value` (24, có biên cho biến động jackpot). (b) thêm auto-drop ở sàn (0.4 s): min(6.67 + 2.5, 9.6) × 2.96 ≈ **27.1**; test dùng `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoinWithAuto` (32). (c) không auto-drop, độ nảy = `bounciness_cap` (Kẹo Bảy Màu): 6.67 × 2.3 × (1 + 0.20 × 5) ≈ **30.7** ở biên mục tiêu; test dùng `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoinAtCap` (36). (d) auto-drop ở sàn và độ nảy = `bounciness_cap`: min(9.17, 9.6) × 2.3 × (1 + 0.20 × 5) ≈ **42.2**; test dùng `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoinAutoAndCap` (50). (b), (c), (d) vượt 24 là **đúng thiết kế** (trần mềm, phần vượt đến từ Overflow), không phải lỗi; mỗi cấu hình có trần test riêng (AC-13, AC-13b, AC-13c, AC-13d), và "≈ 20" chỉ là giá trị của cấu hình (a).

### anti_tunnel_constraint

`max_coin_speed × fixed_delta_time < peg_radius + coin_radius`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| max_coin_speed | float (u/s) | 4–10 | SO field `PlinkoConfig.maxCoinSpeed` | trần tốc độ xu |
| fixed_delta_time | float (s) | 0.02 | constant (Project Settings → Time) | bước vật lý cố định |
| peg_radius | float (u) | 0.04–0.10 | SO field `PlinkoConfig.pegRadius` | bán kính đinh |

**Output range:** ràng buộc phải đúng; kiểm tra cấu hình trong Editor cảnh báo nếu vi phạm.
**Example:** 6 × 0.02 = 0.12 < 0.06 + 0.16 = 0.22 → hợp lệ.

## Edge Cases

- **If người chơi chạm liên tục nhanh hơn `tap_cooldown` (autoclicker, nhiều ngón)**: chỉ cú chạm đầu tiên trong mỗi cửa sổ cooldown được nhận; các cú khác bị bỏ, không xếp hàng. Đa chạm trong cùng khung hình được xử lý như một cú chạm (lấy điểm chạm đầu tiên).
- **If bàn đạt `max_active_coins`**: chạm `Manual` bị bỏ với phản hồi "bàn bận" (không trừ gì, không dời cooldown); nhịp `Auto` bị bỏ không phản hồi; xu `Rain`/`JackpotBurst` chờ trong hàng đợi.
- **If Mưa Đồng Xu đang chạy (hàng đợi lấp tới `max_active_coins` − `manual_reserved_slots`) và người chơi chạm**: cú chạm được nhận vào chỗ dành riêng (nếu qua cooldown); chỉ khi người chơi đã lấp hết `manual_reserved_slots` chỗ đó mới gặp "bàn bận".
- **If xu đi sát tường ở tầng chẵn**: gặp nửa đinh gắn tường và bị đẩy vào trong; không có khe nào hẹp hơn đường kính xu nên xu không kẹt nêm ở làn dẫn tới hũ x5.
- **If hàng đợi loạt vượt `burst_queue_max`** (ví dụ nhiều Mưa Đồng Xu chồng nhau): phần vượt được trả ngay theo `instant_payout_multiplier`, phát `CoinResolved` với `reason = Overflow`. Người chơi không mất xu.
- **If chạm nằm ngoài khung bàn hoặc trúng một phần tử UI** (nút vặn trứng, header, toast): không thả xu.
- **If chạm nằm trong khung bàn nhưng ngoài vùng thả**: x bị kẹp vào mép vùng thả gần nhất, xu vẫn được thả.
- **If độ lệch ngang đẩy điểm sinh ra ngoài tường**: điểm sinh bị kẹp vào bên trong tường, cách tường ít nhất một bán kính xu.
- **If các nguồn cộng dồn đẩy độ nảy vượt `bounciness_cap`** (Kẹo Bảy Màu cộng nguồn khác): độ nảy hiệu lực = `bounciness_cap`; phần vượt bị bỏ. Đây là chặn BLOCKING #2 của review.
- **If một nguồn đăng ký hệ số nảy hữu hạn < 1** (0, 0.5): hệ số đó được kẹp lên 1 (R5.3, R12.5); độ nảy hiệu lực không xuống dưới `base_peg_bounciness`, xu không bao giờ "chết" trên đinh vì độ nảy 0.
- **If một xu chạm đinh vượt `max_scored_peg_hits`**: các lần chạm sau vẫn phát `PegHit` (âm thanh/VFX) với `scored = false`, không có giá trị kinh tế.
- **If xu nằm yên/rung trên một đinh hoặc trên đỉnh vách hũ**: chạm lặp cùng đinh trong `peg_hit_debounce` không được tính; nếu tốc độ < `stuck_speed_threshold` liên tục `stuck_time` giây → xu kẹt, trả theo `stuck_payout_multiplier`.
- **If xu kẹt giữa hai đinh mà vẫn rung (tốc độ không dưới ngưỡng)**: `coin_max_lifetime` là chốt cuối; quá hạn → xu kẹt.
- **If một xu chạm cảm biến của hai hũ cùng bước vật lý** (rơi đúng mép vách): chỉ cảm biến được ghi nhận trước được tính; xu đã Resolved bỏ qua mọi va chạm sau. Mỗi xu chỉ trả một lần.
- **If xu `JackpotBurst` trúng hũ jackpot**: nhận x5 và phát `JackpotHit` với `origin = JackpotBurst`, nhưng không tạo Bùng Xu mới; thú có `includeBurstCoins = false` (Mèo Chiêu Tài) không nhận thưởng — Pet Catalog R6/R8.
- **If nhiều jackpot xảy ra gần nhau**: mỗi jackpot (từ origin hợp lệ) xếp `jackpot_burst_count` xu vào hàng đợi; trần hàng đợi và trả ngay xử lý phần dư.
- **If nguồn auto-drop bị gỡ (thú rời kệ, boost hết hạn) khi đang chờ nhịp**: bộ hẹn giờ dừng ngay; nhịp kế tiếp không xảy ra.
- **If tốc độ auto-drop từ các nguồn cộng lại rất lớn**: chu kỳ không nhỏ hơn `auto_drop_min_interval`.
- **If một nguồn gọi `RegisterAutoDropRate` / `RegisterBouncinessFactor` hai lần với cùng `sourceId`** (thú được đặt lại lên kệ, boost gia hạn): giá trị sau thay giá trị trước, không cộng/nhân đôi; `Unregister` một `sourceId` chưa đăng ký bị bỏ qua.
- **If Tutorial tắt `acceptManualTaps`**: chạm vào bàn không sinh xu, không phát `BoardBusy`, không dời cooldown; auto-drop, Mưa Đồng Xu và Bùng Xu vẫn chạy (R4.6).
- **If người chơi chuyển sang Tab 2/3**: mô phỏng tiếp tục (R11); `CoinResolved` vẫn cộng tiền; tiếng vào hũ và jackpot phát giảm −12 dB, mọi tiếng khác bị bỏ (Audio D5, A8); số bay do Feedback & Juice quyết định.
- **If tab trình duyệt bị ẩn hoặc app xuống nền** (Bootstrap phát `Paused`): mọi xu trong hàng đợi loạt được trả ngay theo `instant_payout_multiplier` (`CoinResolved(reason = Overflow)`, R11.3); mô phỏng dừng, xu đang rơi đứng yên; sau `Resumed` mà `IsSimulationRunning` đúng, xu đang rơi tiếp tục, không có xu nào bị coi là kẹt do thời gian tạm dừng (bộ đếm kẹt và tuổi xu dùng thời gian mô phỏng, không dùng giờ thực).
- **If bàn còn dưới rèm** (sau `Run()`, trước `Interactive`; hoặc `Resumed` đưa vòng đời về `Booting.Curtain`, Bootstrap B6.9): bàn ở "Chờ mở cửa", không tiến bước mô phỏng nào; một thú auto-drop đã lên kệ (đăng ký nguồn) không thả xu nào dưới rèm; `RequestBurst` nằm trong hàng đợi và chỉ sinh sau `Interactive`; chạm bị bỏ im lặng; nếu trang bị ẩn lúc này, `Paused` vẫn xả hàng đợi theo R11.3 (R11.4).
- **If Mưa Đồng Xu hoặc Bùng Xu đang xếp hàng khi tab bị ẩn**: người chơi nhận đủ phần thưởng ngay lúc `Paused` (mỗi xu × `instant_payout_multiplier`), không xem được xu rơi; khi quay lại bàn chỉ còn các xu đang rơi lúc ẩn.
- **If rewarded ad đang mở** (Alpha; cờ "ad đang mở" bật, Bootstrap B6.10): không có `Paused` → mô phỏng chạy tiếp, hàng đợi loạt không bị xả; trên mobile engine có thể không chạy khung khi quảng cáo che, bước mô phỏng bị kẹp như mọi khung dài.
- **If cửa sổ chỉ mất focus mà vẫn hiển thị** (bấm ra ngoài canvas WebGL trên trang Poki/CrazyGames): không có `Paused` → mô phỏng tiếp tục, xu vẫn rơi và cộng tiền (Boot D3).
- **If phiên kết thúc khi còn xu trên bàn hoặc trong hàng đợi** (đóng tab, thoát app): hàng đợi loạt đã được trả ngay ở `Paused` (R11.3) khi trang/app bị ẩn trước khi đóng — trường hợp thường gặp trên web và mobile, nên thưởng đã trúng không mất. Chỉ tối đa `max_active_coins` xu **đang rơi** chưa giải quyết bị mất **trong MVP** (chưa có Save) — chấp nhận có chủ đích, lượng nhỏ (C2[17]). Nếu nền tảng đóng mà không qua `Paused` (WebGL có thể không báo, Bootstrap B6.7), hàng đợi cũng mất. Từ Vertical Slice, Save/Load quyết định xu đang rơi có được trả ngay hay không — Open Question.
- **If khung hình rơi rất thấp (thiết bị yếu)**: vật lý chạy theo bước cố định; xu nhanh không xuyên qua đinh (phát hiện va chạm liên tục cho xu) — ràng buộc kỹ thuật → ADR.
- **If một hệ số hũ bị đặt bằng 0 trong cấu hình**: hợp lệ về kỹ thuật (xu vào hũ đó trả 0); kiểm tra cấu hình ở Editor cảnh báo (ngoài safe range 1–10), không chặn chạy.
- **If dự phòng `PlinkoConfig.baseCoinValue` bị đặt < 1** (0, âm, NaN): **không** hợp lệ — `base_coin_value` có range ≥ 1 (`coin_payout`, getter của Economy R10.2); validator báo lỗi cấu hình. Knob dự phòng chỉ nhận 1 (Tuning Knobs).

## Dependencies

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Economy Core | **Economy Core phụ thuộc Plinko** (tiêu thụ `CoinResolved`, cài đặt `IBaseCoinValueSource`); Plinko không tham chiếu Economy Core | Hard (với Economy Core); Soft (với Plinko: chạy được với dự phòng `PlinkoConfig.baseCoinValue`, không khai `RunsAfter` `economy.core`) | Out: `CoinResolved { coinId, origin, bucketIndex \| none, multiplier, rawValue, scoredPegHits, reason ∈ {Bucket, Stuck, Overflow} }` (`Overflow` gồm cả xu xả hàng đợi khi `Paused`, R11.3 — Economy Core phải xử lý `CoinResolved` cả khi đã nhận `Paused`); Economy Core áp modifier kênh Plinko cho mọi origin/reason và cộng ví (`economy-core.md` R5.1). In: `baseCoinValue` runtime, Plinko kéo qua `IBaseCoinValueSource` lúc giải quyết xu (`economy-core.md` R10, Economy D4). Owner: Plinko (sự kiện, interface, dự phòng); Economy Core (cài đặt, công thức `base_coin_value`, modifier, ví). |
| Object Pooling | Plinko phụ thuộc | Hard | In: đăng ký `plinko.coin` (`client_declared_max` = `max_active_coins`, `forbid_recycle` = true, trong bước `GameplayInit`); Get khi xu được nhận (R4.5), Release ngay khi xu Resolved (R9.1, không có Fizzling). Owner: Object Pooling (dung lượng, headroom, chính sách Fixed). |
| Audio | Gián tiếp qua Feedback & Juice — Plinko không gọi Audio | Soft (bàn chạy được khi câm) | Plinko không có interface với Audio. Feedback & Juice ánh xạ sự kiện R12 → `cueId` (Audio D1); Audio sở hữu SoundCue, giới hạn voice (R6.3), chế độ nền bus Plinko (Audio D5). |
| Bootstrap & Scene Flow | Plinko phụ thuộc | Hard | In: `WorldPress {worldPos, screenPos}` đã lọc UI và cổng chặn input; `ActiveTab` / `TabChanged` (R11.1); `Paused` / `Resumed` (R11.2, Boot D3); `Interactive` + `IsSimulationRunning` (mô phỏng và auto-drop chỉ tiến khi `Running`, bắt đầu ở `Interactive`, đọc lại sau `Resumed` — R11.4, Bootstrap B6.5, B6.9); `FrameChanged` (khớp camera Plinko). Out: bước `plinko.board` (`GameplayInit`, cốt lõi, `RunsAfter` rỗng, R13), đăng ký `PlinkoViewport` + camera vào router, gốc hình ảnh tab `Board`. Owner: Bootstrap (router, cổng chặn, tab, vòng đời); Plinko (`acceptManualTaps`, cooldown, trần xu). |
| Shelf & Synergy | Shelf phụ thuộc Plinko | Hard (với Shelf) | In: `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister` (R12.4; Plinko cộng/nhân rồi kẹp). Out: `PegHit` (`scored`), `JackpotHit` (`origin`), `CoinResolved`. Owner: Plinko (API, luật cộng dồn, trần); Pet Catalog (độ lớn hiệu ứng thú). Shelf GDD phải liệt kê Plinko là phụ thuộc. |
| Boosts | Boosts phụ thuộc Plinko | Hard (với Boosts) | In: `RequestBurst(count, origin = Rain)`; tốc độ auto-drop / hệ số nảy tạm thời qua `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister`. Owner: Plinko. |
| Rewarded Ads | Ads phụ thuộc Plinko | Soft | In: `RequestBurst(count, origin = Rain)`. Owner: Plinko. |
| Feedback & Juice | Feedback phụ thuộc Plinko | Hard (với Feedback) | Out: `CoinSpawned`, `PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy` (payload R12). Owner: Plinko (sự kiện); Feedback (trình bày, pool số bay `feedback.floatingText` và pool hiệu ứng tan `feedback.coinFizzle` của nó; knob trình bày hiện ở asset tạm `PlinkoFeedbackConfig`, Tuning Knobs). |
| Tutorial & Onboarding | Tutorial phụ thuộc Plinko | Soft | In: cờ `acceptManualTaps` (R4.6). Out: `CoinResolved` đầu tiên. Owner: Plinko (cờ); cổng chặn chạm world toàn cục thuộc Bootstrap (B7). |
| Save/Load (VS) | Plinko không phụ thuộc | — | Plinko không có trạng thái cần lưu trong MVP; hàng đợi loạt đã được trả ở `Paused` (R11.3); xu đang rơi khi đóng phiên → Open Question. |

**Hai chiều**: Economy Core (`economy-core.md`), Object Pooling (`object-pooling.md`), Bootstrap & Scene Flow (`bootstrap-and-scene-flow.md`) đã liệt kê Plinko Board với interface khớp bảng trên (kể cả `Interactive` + `IsSimulationRunning`); Audio (`audio.md`) liệt kê Plinko là gián tiếp qua Feedback & Juice — khớp. Pet Catalog (`pet-catalog.md`) không phụ thuộc lúc chạy, chỉ khớp ngữ nghĩa tham số (`includeBurstCoins` dựa vào trường `origin` của `JackpotHit`). Chưa có GDD nào trong số Shelf & Synergy, Boosts, Rewarded Ads, Feedback & Juice, Tutorial & Onboarding — khi viết, mỗi GDD đó phải liệt kê Plinko Board với cùng interface ở trên.

## Tuning Knobs

Hai asset: `PlinkoConfig` (runtime) và `PlinkoBalanceTargets` (chỉ dùng cho test mô phỏng / cân bằng, không ảnh hưởng gameplay). Không có số nào trong code; kiểm tra cấu hình ở Editor cảnh báo khi giá trị ra ngoài safe range hoặc vi phạm `anti_tunnel_constraint`. Knob do hệ khác sở hữu không lặp lại ở đây: độ lớn hiệu ứng thú (`PegHitBonus`, `JackpotBonus`, `BouncinessModifier`, `AutoDropRate`…) thuộc `PetDefinition` của Pet Catalog, Shelf & Synergy thi hành; đường tăng `base_coin_value` (`EconomyConfig.coinValueExponent`) thuộc Economy Core; lệch cao độ chạm đinh và âm lượng spawn thuộc `SoundCue` của Audio. Luật cộng dồn tốc độ auto-drop và hệ số nảy là luật của Plinko (R12.5), không phải knob.

**Hình học**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Số tầng đinh | `PlinkoConfig.pegRows` | int | 12 | 8–14 | Xu rơi lâu, jackpot hiếm hơn mục tiêu | Phân phối hẹp, jackpot gần như không trúng; ít "tiếng nảy" | dropJitter, targetMedianFallTime, p_jackpot |
| Số đinh tầng chẵn | `PlinkoConfig.pegsPerRow` | int | 10 | 8–12 | Đinh nhỏ/dày, khó đọc trên màn dọc | Bàn hẹp, hũ hẹp | pegSpacing, board_inner_width |
| Khoảng cách đinh | `PlinkoConfig.pegSpacing` | float (u) | 0.55 | 0.45–0.65 | Bàn tràn khỏi khung Tab 1 | Xu kẹt giữa đinh | coinRadius, pegRadius |
| Tỉ lệ khoảng cách tầng | `PlinkoConfig.rowSpacingRatio` | float | 0.866 | 0.76–1.0 | Bàn cao, rơi lâu | Khe chéo giữa hai tầng hẹp hơn đường kính xu (vi phạm `min_passage_gap`), xu kẹt nêm | pegRows, pegSpacing, pegRadius, coinRadius, minGapMargin |
| Bán kính đinh | `PlinkoConfig.pegRadius` | float (u) | 0.06 | 0.04–0.10 | Khe hẹp, xu kẹt | Khó thấy, dễ xuyên | coinRadius, maxCoinSpeed |
| Bán kính xu | `PlinkoConfig.coinRadius` | float (u) | 0.16 | 0.12–0.19 | Xu không lọt khe (vi phạm `min_passage_gap`) | Xu khó nhìn trên mobile | pegSpacing, pegRadius, wallBumpRadius, minGapMargin, rowSpacingRatio |
| Bán kính nửa đinh gắn tường | `PlinkoConfig.wallBumpRadius` | float (u) | 0.06 | 0.04–0.10 | Khe đinh–nửa đinh hẹp, xu kẹt | Xu trượt dọc tường, p_jackpot tăng | pegRadius, coinRadius |
| Biên khe tối thiểu | `PlinkoConfig.minGapMargin` | float (u) | 0.06 | 0.03–0.12 | Ràng buộc hình học quá chặt, khó chỉnh bàn | Xu sượt khe, dễ kẹt vì sai số solver | coinRadius, pegSpacing |
| Hệ số hũ | `PlinkoConfig.bucketMultipliers` | float[5] | [5, 2, 1, 2, 5] | mỗi ô 1–10, đối xứng | Lạm phát kênh Plinko | Plinko vô nghĩa so với CPS | EV, Economy Core |
| Trọng số độ rộng hũ | `PlinkoConfig.bucketWidthWeights` | float[5] | [1, 1, 1, 1, 1] | mỗi ô 0.8–1.2 | Hũ jackpot rộng → p_jackpot vượt mục tiêu | Hũ jackpot hẹp → trông "gian" | p_jackpot |
| Độ rộng vùng thả | `PlinkoConfig.dropZoneWidthRatio` | float | 0.14 | 0.10–0.20 | Nhắm quá có lợi (`aim_ratio` trên EV hiệu dụng > `evRatioMax`), chỉ gõ mép | Chạm không còn ý nghĩa | dropJitter, jackpotBurstCount, evRatioMax |
| Lệch ngang | `PlinkoConfig.dropJitter` | float (s) | 1.0 | 0.5–1.25 | Mất cảm giác điều khiển | Kết quả quá dễ đoán; một mình không giảm được `aim_ratio` | sigma, dropZoneWidthRatio |

**Vật lý**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Độ nảy gốc đinh | `PlinkoConfig.basePegBounciness` | float | 0.5 | 0.3–0.6 | Xu nảy loạn, khuếch tán vượt mô hình | Xu "dính", ít âm thanh | bouncinessCap, k_diff |
| Trần độ nảy | `PlinkoConfig.bouncinessCap` | float | 0.85 | 0.7–0.9 (luôn < 1.0) | ≥ 1.0 sinh năng lượng → xu vô hạn (BLOCKING #2) | Kẹo Bảy Màu không có cảm giác | Shelf (Kẹo Bảy Màu), capJackpotProbMeanMax |
| Trọng lực xu | `PlinkoConfig.coinGravityScale` | float | 0.6 | 0.3–1.5 | Rơi nhanh, mất cảm giác ASMR | Rơi lâu, giảm thông lượng | targetMedianFallTime |
| Trần tốc độ xu | `PlinkoConfig.maxCoinSpeed` | float (u/s) | 6 | 4–10 | Xuyên đinh (vi phạm anti_tunnel) | Xu trôi như bong bóng | fixed_delta_time, pegRadius, coinRadius |
| Trần bước mô phỏng mỗi khung | `PlinkoConfig.maxSimStep` | float (s) | 0.25 | 0.1–0.5 | Nhảy vọt sau giật hình, tốn CPU bù bước vật lý | Bàn chậm hơn thời gian thực ở thiết bị < 4 fps | fixed_delta_time, `EconomyConfig.maxFrameDelta` (giữ bằng nhau), maxCoinSpeed |

**Nhịp, trần và chống lạm dụng**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Cooldown chạm | `PlinkoConfig.tapCooldown` | float (s) | 0.15 | 0.10–0.25 | Cảm giác bàn "ì", chạm bị nuốt | Autoclicker kiếm quá nhanh (BLOCKING #4) | coin_rate_ceiling |
| Trần xu đồng thời | `PlinkoConfig.maxActiveCoins` | int | 24 | 12–40 | Tụt FPS WebGL/mobile yếu | Bàn hay "bận" khi Bùng Xu/Mưa Xu | Object Pooling, burstQueueMax |
| Nhịp sinh xu loạt | `PlinkoConfig.burstSpawnInterval` | float (s) | 0.08 | 0.04–0.15 | Mưa xu lê thê | Xu chồng nhau ở vạch thả | maxActiveCoins |
| Trần hàng đợi loạt | `PlinkoConfig.burstQueueMax` | int | 60 | 30–120 | Hàng đợi kéo dài hàng chục giây | Tràn sớm, nhiều xu bị trả ngay | Boosts (Mưa Đồng Xu), jackpotBurstCount |
| Chỗ dành cho chạm tay | `PlinkoConfig.manualReservedSlots` | int | 2 | 1–4 (< maxActiveCoins) | Mưa xu / auto-drop kéo dài, bàn trống | Chạm tay bị "bàn bận" trong lúc nhận thưởng | maxActiveCoins, burstSpawnInterval |
| Số xu Bùng Xu | `PlinkoConfig.jackpotBurstCount` | int | 5 | 3–8 | Bùng Xu chiếm phần lớn thu nhập, lạm phát | Jackpot không có cảm giác thưởng | EV_effective |
| Chu kỳ auto-drop gốc | `PlinkoConfig.autoDropBaseInterval` | float (s) | 2.0 | 1–4 | Nguồn auto-drop yếu | Kênh idle lấn kênh chủ động | auto_rate_multiplier |
| Sàn chu kỳ auto-drop | `PlinkoConfig.autoDropMinInterval` | float (s) | 0.4 | 0.3–1.0 | Cộng dồn nguồn không có ý nghĩa | Auto ≈ chạm tay, Pillar 3 hỏng | tapCooldown |
| Trần chạm đinh được tính | `PlinkoConfig.maxScoredPegHits` | int | 20 | 12–30 | Xu bất thường cho Thỏ quá nhiều | Cắt cả xu bình thường | Shelf (Thỏ Nhào Lộn) |
| Chống lặp chạm đinh | `PlinkoConfig.pegHitDebounce` | float (s) | 0.10 | 0.05–0.25 | Bỏ sót chạm thật | Xu rung trên đinh spam sự kiện | maxScoredPegHits |
| Ngưỡng tốc độ kẹt | `PlinkoConfig.stuckSpeedThreshold` | float (u/s) | 0.15 | 0.05–0.30 | Xu chậm bình thường bị xóa oan | Xu kẹt không bị phát hiện | stuckTime |
| Thời gian kẹt | `PlinkoConfig.stuckTime` | float (s) | 1.0 | 0.5–2.0 | Xu đứng im lâu trên màn | Xóa oan khi xu dừng chốc lát trên đinh | stuckSpeedThreshold |
| Tuổi thọ tối đa xu | `PlinkoConfig.coinMaxLifetime` | float (s) | 8 | 6–15 | Xu rung giữa đinh chiếm chỗ lâu | Xóa oan xu rơi chậm (phải ≥ `fallTimeP95Max` + `lifetimeMarginSeconds`) | fallTimeP95Max |
| Hệ số trả xu kẹt | `PlinkoConfig.stuckPayoutMultiplier` | float | 1.0 | 0.5–1.0 (< EV_mean) | Cố ý làm kẹt xu có lợi | Người chơi thấy "mất xu" | EV_mean |
| Hệ số trả ngay | `PlinkoConfig.instantPayoutMultiplier` | float | 1.9 | 1.0–2.0 (≤ EV_mean) | Chồng Mưa Xu để tràn hàng đợi có lợi | Tràn hàng đợi bị thiệt | burstQueueMax, EV_mean |
| Xu gốc dự phòng (không có Economy Core) | `PlinkoConfig.baseCoinValue` | double | 1 | = 1 — constant (chỉ cho test/prototype khi không có `IBaseCoinValueSource`; bằng `base_coin_value(0)` của Economy D4 nên test không phụ thuộc Economy; khớp `economy-core.md` "dự phòng = 1, chỉ cho test"). Validator báo lỗi khi < 1 hoặc không hữu hạn (Edge Cases); không kiểm giá trị runtime của nguồn | — | — | Economy Core (`base_coin_value`, R10) |

**Mục tiêu cân bằng (chỉ dùng cho test mô phỏng)**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Thời gian rơi trung vị | `PlinkoBalanceTargets.targetMedianFallTime` | float (s) | 2.5 | 1.8–3.5 | — | — | coinGravityScale, pegRows |
| Dung sai thời gian rơi | `PlinkoBalanceTargets.fallTimeTolerance` | float (s) | 0.4 | 0.2–0.6 | Test quá lỏng | Test chập chờn | — |
| Trần p95 thời gian rơi | `PlinkoBalanceTargets.fallTimeP95Max` | float (s) | 5.0 | ≤ coinMaxLifetime − lifetimeMarginSeconds | — | — | coinMaxLifetime |
| p_jackpot trung bình (min / max) | `PlinkoBalanceTargets.jackpotProbMeanMin` / `.jackpotProbMeanMax` | float | 0.10 / 0.13 | 0.05–0.20 | — | — | geometry |
| p_jackpot tối đa tại một điểm | `PlinkoBalanceTargets.jackpotProbMaxAtAnyX` | float | 0.18 | 0.12–0.25 | — | — | dropZoneWidthRatio |
| p_jackpot tối thiểu tại tâm | `PlinkoBalanceTargets.jackpotProbMinAtCenter` | float | 0.06 | 0.03–0.10 | — | — | pegRows, dropJitter |
| EV trung bình (min / max) | `PlinkoBalanceTargets.evMeanMin` / `.evMeanMax` | float | 1.85 / 2.05 | 1.5–2.5 | — | — | bucketMultipliers |
| Tỉ lệ EV **hiệu dụng** max/min theo điểm thả (`aim_ratio`, gồm Bùng Xu) | `PlinkoBalanceTargets.evRatioMax` | float | 1.20 | 1.05–1.30 | — | — | dropZoneWidthRatio, dropJitter, jackpotBurstCount |
| p_jackpot trung bình ở trần nảy | `PlinkoBalanceTargets.capJackpotProbMeanMax` | float | 0.20 | 0.15–0.25 | — | — | bouncinessCap |
| EV trung bình ở trần nảy | `PlinkoBalanceTargets.capEvMeanMax` | float | 2.3 | 2.0–2.6 | — | — | bouncinessCap |
| Tỉ lệ xu kẹt tối đa | `PlinkoBalanceTargets.stuckRateMax` | float | 0.01 | 0.005–0.03 | — | — | stuck*, geometry |
| Số xu mỗi điểm thả trong mô phỏng | `PlinkoBalanceTargets.simCoinsPerDropX` | int | 2000 | 1000–10000 | Test chạy lâu | Sai số thống kê lớn | — |
| Số điểm thả mẫu trên vùng thả | `PlinkoBalanceTargets.simDropXSamples` | int | 7 | 5–15 | Test chạy lâu | Bỏ sót điểm xấu nhất | — |
| Biên an toàn tuổi xu | `PlinkoBalanceTargets.lifetimeMarginSeconds` | float (s) | 1.0 | 0.5–3.0 | Tuổi xu phải dài, xu rung chiếm chỗ lâu | Xu rơi chậm bị xóa oan | coinMaxLifetime, fallTimeP95Max |
| Dung sai tăng năng lượng mỗi va chạm | `PlinkoBalanceTargets.energyGainTolerance` | float (tỉ lệ) | 0.02 | 0.005–0.05 | Không bắt được lỗi sinh năng lượng | Test trượt do hiệu chỉnh vị trí của solver | bouncinessCap |
| Trần test thu nhập thô kênh Plinko / giây — cấu hình gốc (độ nảy gốc, không auto-drop, tính theo `baseCoinValue`; trần mềm, chỉ dùng cho AC-13) | `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoin` | float | 24 | 20–30 (> `raw_rate_ceiling` mô hình ≈ 19.7) | Không bắt được lạm phát | Test trượt do biến động jackpot | tapCooldown, jackpotBurstCount, bucketMultipliers |
| Trần thu nhập thô / giây khi auto-drop ở sàn (độ nảy gốc, theo `baseCoinValue`) | `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoinWithAuto` | float | 32 | 28–38 (> `raw_rate_ceiling` mô hình ≈ 27.1) | Không bắt được lạm phát do auto-drop | Test trượt do biến động jackpot | autoDropMinInterval, tapCooldown, jackpotBurstCount, maxRawPayoutPerSecondPerBaseCoin |
| Trần thu nhập thô / giây ở trần nảy (không auto-drop, theo `baseCoinValue`) | `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoinAtCap` | float | 36 | 31–42 (> `raw_rate_ceiling` ở biên mục tiêu ≈ 30.7) | Không bắt được lạm phát do tăng nảy | Test trượt do biến động jackpot | bouncinessCap, capEvMeanMax, capJackpotProbMeanMax, jackpotBurstCount |
| Trần test thu nhập thô / giây khi auto-drop ở sàn **và** ở trần nảy (theo `baseCoinValue`) | `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoinAutoAndCap` | float | 50 | 44–56 (> `raw_rate_ceiling` mô hình ≈ 42.2) | Không bắt được lạm phát khi cộng dồn auto-drop và tăng nảy | Test trượt do biến động jackpot | autoDropMinInterval, bouncinessCap, capEvMeanMax, capJackpotProbMeanMax, maxRawPayoutPerSecondPerBaseCoinWithAuto, maxRawPayoutPerSecondPerBaseCoinAtCap |
| Dung sai phân bố đều của điểm thả | `PlinkoBalanceTargets.dropUniformityTolerance` | float | 0.015 | 0.005–0.03 | Không bắt được lệch RNG | Test chập chờn | simCoinsPerDropX |
| Độ tương phản tối thiểu của nhãn hũ | `PlinkoBalanceTargets.labelContrastMin` | float | 4.5 | ≥ 4.5 (WCAG AA) | — | Nhãn khó đọc trên mobile | màu hũ (art bible) |
| Ngân sách thời gian Physics2D (p95) | `PlinkoBalanceTargets.physicsFrameBudgetMs` | float (ms) | 4.0 (*provisional* — technical-director giao) | 2–6 | Lấn ngân sách khung 16.7 ms | — | maxActiveCoins |
| Trễ chạm → xu xuất hiện | `PlinkoBalanceTargets.tapToSpawnMaxMs` | int (ms) | 50 | 33–83 | Chạm cảm thấy lì | — | tapCooldown |
| Trễ xu vào hũ → phản hồi | `PlinkoBalanceTargets.resolveToFeedbackMaxMs` | int (ms) | 34 | 17–50 | Số bay lệch khỏi khoảnh khắc | — | Feedback & Juice |
| Ngân sách draw call của bàn | `PlinkoBalanceTargets.boardDrawCallBudget` | int | 15 | 8–25 (trong tổng 100 của game) | Lấn ngân sách UI/Gacha | — | maxActiveCoins, atlas |
| Số xu / mẫu của lô sim lớn | `PlinkoBalanceTargets.simCoinCountLarge` | int | 10000 | 5000–50000 | Test chạy lâu | Sai số thống kê lớn, bỏ sót ca hiếm (xu rơi đúng mép vách) | AC-5, AC-6, AC-26 |
| Số xu của lô sim nhỏ | `PlinkoBalanceTargets.simCoinCountSmall` | int | 1000 | 500–5000 | Test chậm | Bỏ sót ca hiếm (kẹt, va mép) | AC-16, AC-32, AC-46 |
| Số ô kiểm phân bố đều của vùng thả | `PlinkoBalanceTargets.dropUniformityBins` | int | 10 | 5–20 | Mỗi ô ít mẫu, test chập chờn | Không thấy lệch cục bộ của RNG | dropUniformityTolerance, simCoinCountLarge |
| Tần suất autoclicker thử | `PlinkoBalanceTargets.autoclickerRatesPerSecond` | int[] (chạm/s) | [30, 60, 240] | mỗi phần tử 20–480, mọi phần tử > 1 / `C.tapCooldown`, ít nhất một phần tử > `targetFps` (nhiều chạm trong cùng khung) | Test chậm | Không thử được nhiều chạm/khung | tapCooldown, AC-12, AC-13…AC-13d |
| Thời lượng sim autoclicker | `PlinkoBalanceTargets.autoclickerSimSeconds` | float (s) | 10 | 5–60 | Test chậm | Ít cửa sổ cooldown, bỏ sót lệch tích lũy | AC-12 |
| Thời lượng sim thông lượng | `PlinkoBalanceTargets.throughputSimSeconds` | float (s) | 60 | 30–300 | Test `Slow` kéo dài | Biến động jackpot làm test chập chờn | AC-13…AC-13d, maxRawPayoutPerSecondPerBaseCoin* |
| Thời lượng tạm dừng trong test | `PlinkoBalanceTargets.pauseTestSeconds` | float (s) | 60 | 10–600, > `C.coinMaxLifetime` và > `C.autoDropBaseInterval` | — (đồng hồ inject nên rẻ) | Không phân biệt được lỗi đếm giờ thực với tuổi xu; không chứng minh được "0 nhịp auto-drop dưới rèm" | AC-29, AC-48, coinMaxLifetime, autoDropBaseInterval |
| Thời lượng ở tab khác trong test | `PlinkoBalanceTargets.tabSwitchTestSeconds` | float (s) | 5 | 3–10, ≥ `targetMedianFallTime` | Test Play mode chậm | Không xu nào kịp vào hũ, test không chứng minh mô phỏng chạy | AC-28, targetMedianFallTime |
| Số xu đang rơi trong test vòng đời | `PlinkoBalanceTargets.lifecycleTestCoins` | int | 10 | 5 – (`C.maxActiveCoins` − `C.manualReservedSlots`) | Chạm trần xu, lẫn với "bàn bận" | Ít xu, bỏ sót | AC-28, AC-29, AC-47 |
| Dt khung giật trong test kẹp bước | `PlinkoBalanceTargets.hitchFrameSeconds` | float (s) | 1.0 | > `C.maxSimStep` – 2.0 | Không chạm kẹp, test vô nghĩa | Quá lớn vô ích | AC-49 |
| Số ngón chạm cùng khung trong test | `PlinkoBalanceTargets.multiTouchTestFingers` | int | 3 | 2–5 | — | — | AC-34 |
| Dung sai hình học | `PlinkoBalanceTargets.geometryTolerance` | float (u) | 1e-4 | 1e-5–1e-3 | Không bắt được lệch dựng bàn | Sai số float làm test trượt | AC-1, AC-2 |
| Dung sai trần tốc độ | `PlinkoBalanceTargets.speedTolerance` | float (u/s) | 1e-3 | 1e-4–1e-2 | Bỏ sót vượt `maxCoinSpeed` | Sai số solver làm test trượt | AC-16, maxCoinSpeed |
| Dung sai điểm tiếp xúc | `PlinkoBalanceTargets.contactTolerance` | float (u) | 1e-3 | 1e-4–1e-2 | Bỏ sót `worldPosition` sai | Sai số solver làm test trượt | AC-42, pegRadius, coinRadius |
| Thời lượng đo [Perf] | `PlinkoBalanceTargets.perfSampleSeconds` | float (s) | 60 | 30–300 | Phiên đo dài | Không phủ được jackpot/Mưa Xu | AC-36, AC-37 |
| Độ phân giải chụp tham chiếu | `PlinkoBalanceTargets.screenshotReferenceResolution` | Vector2Int (px) | 1080×1920 | dọc 9:16, cạnh ngắn 720–1440 | Ảnh lớn, chụp chậm | Chữ nhãn vỡ, đo tương phản sai | AC-39, AC-40, AC-41 |
| Sai số vị trí trên ảnh chụp | `PlinkoBalanceTargets.screenshotPositionTolerancePx` | int (px, ở độ phân giải tham chiếu) | 4 | 2–8 | Lệch vị trí khấc/số bay lọt test | Làm tròn pixel, khử răng cưa gây trượt giả | AC-40, AC-41 |

Knob test do hệ khác sở hữu, tham chiếu chứ không lặp: thiết bị đo [Perf] `BootTestTargets.perfReferenceDevice` (tạm, tới khi ADR hiệu năng thay — AC-36, AC-37); số khung đo GC `PoolingTestTargets.perfSampleFrames` (600, cùng kịch bản với Object Pooling AC-25 — AC-38); `targetFps` của studio config (60).

**Phản hồi** (`PlinkoFeedbackConfig` — **asset tạm** do Plinko giữ cho tới khi có GDD Feedback & Juice; Plinko không đọc knob nào trong đó để mô phỏng)

Chủ sở hữu theo hành vi (C2[13]): mọi knob dưới đây do **Feedback & Juice** thi hành (R12.3: Feedback vẽ pop-in, đinh sáng, punch hũ, "nín thở" jackpot, hạt, bàn bận, số bay, hiệu ứng tan, xu ma, rung) và chuyển sang asset của Feedback & Juice khi GDD đó được viết — **trừ** `dropZoneLineAlpha`, thuộc Plinko (hai khấc và đường vùng thả là phần tĩnh của bàn, presenter tối thiểu của story Plinko). `idleHintDelay` có thể thuộc Tutorial & Onboarding (VS). Hệ khác đang tham chiếu tạm: Object Pooling lấy `client_declared_max` của `feedback.floatingText` từ `floatingTextMaxOnScreen` và của `feedback.coinFizzle` từ `fizzleMaxOnScreen`; Audio tham chiếu `busyFeedbackMinInterval` và `floatingTextMergeWindow`. Khi chuyển, chỉ tên asset đổi, giá trị và safe range giữ nguyên.

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Thời lượng pop-in xu | `PlinkoFeedbackConfig.spawnPopDuration` | float (s) | 0.08 | 0.05–0.15 | Xu xuất hiện chậm | Không thấy pop | tapToSpawnMaxMs |
| Thời lượng sáng đinh | `PlinkoFeedbackConfig.pegFlashDuration` | float (s) | 0.08 | 0.05–0.15 | Bàn nhấp nháy loạn | Không thấy chạm | maxActiveCoins |
| Punch scale đinh | `PlinkoFeedbackConfig.pegPunchScale` | float | 1.1 | 1.0–1.2 | Đinh trông lỏng | Không có cảm giác va | — |
| Punch scale hũ | `PlinkoFeedbackConfig.bucketPunchScale` | float | 1.12 | 1.05–1.2 | Hũ nhảy loạn khi nhiều xu | Không thấy hũ nhận xu | bucketPunchDuration |
| Thời lượng punch hũ | `PlinkoFeedbackConfig.bucketPunchDuration` | float (s) | 0.15 | 0.1–0.25 | Punch chồng nhau | Giật | — |
| Cửa sổ gộp số bay | `PlinkoFeedbackConfig.floatingTextMergeWindow` | float (s) | 0.25 | 0.1–0.5 | Số cộng dồn trễ | Quá nhiều số, tốn draw | floatingTextMaxOnScreen |
| Số bay tối đa trên màn | `PlinkoFeedbackConfig.floatingTextMaxOnScreen` | int | 10 | 6–16 | Rối, tốn draw call | Mất thông tin | Object Pooling |
| Scale "nín thở" jackpot | `PlinkoFeedbackConfig.jackpotHoldScale` | float | 1.15 | 1.1–1.25 | Lố, kiểu casino | Không thấy khoảnh khắc | — |
| Thời lượng "nín thở" jackpot | `PlinkoFeedbackConfig.jackpotHoldDuration` | float (s) | 0.12 | 0.08–0.25 | Cảm giác khựng | Không thấy | burstSpawnInterval |
| Hạt tối đa vòng sáng jackpot | `PlinkoFeedbackConfig.jackpotParticleMax` | int | 12 | 6–20 | Overdraw additive trên WebGL mobile | Mờ nhạt | boardDrawCallBudget |
| Biên độ lắc bàn bận | `PlinkoFeedbackConfig.busyShakeDistancePx` | float (px) | 4 | 2–8 | Trông như lỗi | Không thấy | — |
| Khoảng tối thiểu phản hồi bàn bận | `PlinkoFeedbackConfig.busyFeedbackMinInterval` | float (s) | 0.5 | 0.3–1.0 | Bỏ lỡ phản hồi | Spam âm thanh khi autoclick | tapCooldown |
| Trễ gợi ý khi Idle (VS) | `PlinkoFeedbackConfig.idleHintDelay` | float (s) | 5 | 3–10 | Người mới không biết chạm | Gây phiền | Tutorial |
| Rung khi chạm được nhận (VS) | `PlinkoFeedbackConfig.hapticTapMs` | int (ms) | 10 | 0–20 | Rung khó chịu khi spam | Không cảm nhận | tapCooldown |
| Độ mờ đường vùng thả | `PlinkoFeedbackConfig.dropZoneLineAlpha` | float | 0.3 | 0.15–0.5 | Rối mắt | Không thấy vùng thả | — |
| Độ mờ xu ma (VS) | `PlinkoFeedbackConfig.ghostCoinAlpha` | float | 0.4 | 0.25–0.6 | Nhầm với xu thật | Không thấy | — |
| Số chấm khi xu kẹt | `PlinkoFeedbackConfig.fizzleDotCount` | int | 3 | 2–6 | Kẹt trông như sự kiện lớn | Không thấy xu biến mất | — |
| Độ mờ số của xu kẹt | `PlinkoFeedbackConfig.fizzleTextAlpha` | float | 0.6 | 0.4–0.8 | Nhầm với thưởng thật | Không đọc được | — |
| Số hiệu ứng tan tối đa cùng lúc | `PlinkoFeedbackConfig.fizzleMaxOnScreen` | int | 4 | 2–8 | Tốn đối tượng pool, nhiều xu kẹt trông như sự kiện lớn | Hiệu ứng tan cũ bị tái chế sớm (RecycleOldest) khi nhiều xu kẹt cùng lúc | `client_declared_max` của pool `feedback.coinFizzle` (Object Pooling, headroom 0), stuckRateMax, fizzleDotCount |

## Visual/Audio Requirements

Nguồn: art-director (2026-10-21) + lựa chọn của user. Màu hũ theo **thang ấm**: x1 gỗ nhạt/kem, x2 matcha `#7EA172`, x5 mật ong `#F59E0B` — đọc được bằng độ sáng và cỡ số tăng dần, không chỉ bằng sắc màu. Các board Penpot có hũ x2 màu xanh và hũ x5 ở giữa phải cập nhật theo bố cục `[x5][x2][x1][x2][x5]`. Âm thanh: marimba / gỗ ASMR (chưa có agent audio — mô tả ý đồ). Plinko không phát tiếng: Feedback & Juice ánh xạ sự kiện R12 sang cue `Plinko.*` của `audio.md` (bảng cue), mọi dB, cao độ và giới hạn voice thuộc `SoundCue`. Mọi thời lượng/số lượng hình ảnh dưới đây là knob ở `PlinkoFeedbackConfig` (Tuning Knobs).

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| Spawn `Manual` | Xu pop-in scale 0→1 có squash (`spawnPopDuration`); khấc vùng thả nháy một lần | "Tok" gỗ khô ngắn | MVP |
| Spawn `Auto` | Pop nhỏ hơn, không nháy khấc | Cùng "tok": cue `Plinko.CoinSpawnAuto` nhỏ hơn `CoinSpawn` 6 dB (`audio.md` bảng cue) | MVP |
| Spawn `Rain` | Một dải mây emoji ☁️ mỏng trên vạch thả khi hàng đợi còn xu `Rain` | Gậy mưa chạy nền | VS |
| Spawn `JackpotBurst` | Xu có viền mật ong (biến thể sprite trong atlas) | Chime đi lên theo từng xu | MVP |
| `PegHit` được tính | Đinh sáng màu kem trong `pegFlashDuration`, punch scale `pegPunchScale` | Nốt marimba ngũ cung, cao độ theo tầng đinh (`PegHit.rowIndex`), lệch cao độ theo `SoundCue[Plinko.PegHitScored].pitchJitterRatio` (`audio.md`) | MVP |
| `PegHit` không tính | Không có | Tiếng gỗ trầm rất nhỏ (Audio giới hạn voice) | MVP |
| `CoinResolved` vào hũ | Hũ punch-scale 1→`bucketPunchScale`→1 trong `bucketPunchDuration`; số bay "+n" = `IncomeCredited.creditedAmount` (Economy R5.4, ghép theo `coinId`/`bucketIndex`) màu ink (x5: màu mật ong); **gộp số cùng hũ trong `floatingTextMergeWindow`**, tối đa `floatingTextMaxOnScreen` số | x1 "clunk" gỗ, x2 hai nốt marimba, x5 arpeggio ba nốt | MVP |
| `JackpotHit` + Bùng Xu | Hũ sáng mật ong, giữ scale `jackpotHoldScale` trong `jackpotHoldDuration` rồi thả; nhãn **"BONUS!"** ngắn trên hũ; xu thưởng rơi từ vạch thả | Arpeggio kalimba/glockenspiel rồi tiếng mưa xu nhẹ | MVP |
| Vòng sáng lan khi jackpot | Một vòng tròn mềm, tối đa `jackpotParticleMax` hạt từ ParticleSystem dùng chung | — | VS |
| Xu kẹt (fizzle, theo `CoinStuck`) | Xu thật đã về pool (R9.1); Feedback & Juice vẽ bản sao xu mờ + co lại tại `CoinStuck.worldPosition` bằng đối tượng của pool trang trí `feedback.coinFizzle` (tối đa `fizzleMaxOnScreen` cùng lúc), bung `fizzleDotCount` chấm kem, số nhỏ màu ink độ mờ `fizzleTextAlpha` | "Phụt" hơi rất nhỏ | MVP |
| Bàn bận (chạm bị bỏ) | Khấc vùng thả mờ đi và lắc ngang `busyShakeDistancePx`; **không dùng màu coral, không có chữ** | Gỗ bị bịt rất khẽ, tối đa 1 lần / `busyFeedbackMinInterval` | MVP |
| Vùng thả (thường trực) | Hai khấc gỗ `#A77443` ở hai đầu vùng thả trên vạch thả, nối bằng đường chấm mảnh độ mờ `dropZoneLineAlpha`; không khung, không tô nền | — | MVP |
| Bàn Idle lâu | Hai khấc "thở" nhẹ sau `idleHintDelay` để gợi ý chạm | — | VS (cùng Tutorial) |
| Xu ma theo ngón tay / chuột | Xu độ mờ `ghostCoinAlpha` theo x của điểm chạm/hover, ẩn khi có xu đang rơi | — | VS |

**Ràng buộc hiệu năng hình ảnh**: xu, đinh, hũ, chữ số dùng chung một Sprite Atlas (mục tiêu 1–3 draw call cho phần tĩnh + động); đổi màu đinh bằng màu vertex của SpriteRenderer (giữ batching); mỗi loại particle là một ParticleSystem dùng chung với `Emit()`; toàn bàn ≤ `boardDrawCallBudget` draw call khi có `maxActiveCoins` xu. Phần shader/particle → unity-shader-specialist; số bay pooled → ui-programmer.

## Game Feel

- **Feel reference**: tiếng "thunk" khi trái cây đáp trong Suika Game; âm xúc giác khi đặt đồ trong Unpacking. **Anti-reference**: Plinko kiểu casino/crypto (neon, còi hú); fever quá tay của Peggle.
- **Không hit-stop toàn cục** (`Time.timeScale`): làm đứng mọi xu và lệch bộ đếm kẹt. Thay bằng "nín thở" cục bộ của hũ jackpot. **Không rung màn hình** (giữ chữ và số đọc được).
- **Haptics (VS, tùy chọn)**: rung `hapticTapMs` khi chạm được nhận; mẫu ngắn-ngắn khi jackpot; không rung ở `PegHit` hay bàn bận. WebGL chỉ có trên Android (iOS Safari không hỗ trợ).

Input responsiveness:

| Action | Max input-to-response (ms) | Frames at target fps | Notes |
|---|---|---|---|
| Chạm được nhận → xu xuất hiện ở vạch thả | `tapToSpawnMaxMs` (50) | 3 | Sinh trong khung hình kế tiếp sau khi input được xử lý |
| Chạm bị bỏ → phản hồi "bàn bận" | `tapToSpawnMaxMs` (50) | 3 | Cùng ngân sách |
| Xu chạm cảm biến hũ → số bay + punch hũ | `resolveToFeedbackMaxMs` (34) | 2 | |

Animation feel targets:

| Animation | Startup | Active | Recovery frames | Feel goal |
|---|---|---|---|---|
| Xu pop-in | 0 | `spawnPopDuration` (80 ms ≈ 5 f) | 0 | Xu "bật" ra từ khe, nhẹ |
| Đinh sáng khi chạm | 0 | `pegFlashDuration` (80 ms ≈ 5 f) | 0 | Lách cách, không chói |
| Hũ punch | 0 | `bucketPunchDuration` (150 ms ≈ 9 f) | 0 | Hũ "nuốt" xu |
| Hũ jackpot nín thở | 0 | `jackpotHoldDuration` (120 ms ≈ 7 f) | `bucketPunchDuration` | Khoảnh khắc "ồ!" trước khi xu thưởng rơi |

Impact moments:

| Impact | Duration (ms) | Effect | Tunable? |
|---|---|---|---|
| Hũ jackpot nín thở (cục bộ) | `jackpotHoldDuration` (120) | Scale `jackpotHoldScale`, sáng mật ong | Yes |
| Haptic chạm (VS) | `hapticTapMs` (10) | Rung nhẹ | Yes |
| Hit-stop toàn cục / rung màn hình | 0 | Không dùng | No (quyết định thiết kế) |

**Weight profile**: xu nhẹ và "trôi" (`coinGravityScale` thấp), va chạm gỗ đanh nhưng không gắt; tốc độ cảm nhận chậm hơn Plinko casino để ASMR có thời gian. Feel acceptance: AC-31 (thời gian rơi trung vị) và AC-35 (độ trễ chạm, phản hồi bàn bận) ở Acceptance Criteria.

## UI Requirements

Plinko là world-space; Canvas chỉ đóng khung `PlinkoViewport` ở Tab 1. Các thông tin dưới đây là world-space trừ khi ghi khác.

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Nhãn hệ số hũ `x5 / x2 / x1 / x2 / x5` | Trên mỗi hũ, đáy bàn | Tĩnh | Luôn hiện |
| Vùng thả (hai khấc + đường chấm) | Vạch thả, đỉnh bàn | Tĩnh; nháy khi chạm | Luôn hiện |
| Số bay "+n" | Trên hũ vừa nhận xu | Mỗi `IncomeCredited` của Economy Core cho xu đó (đã gộp theo `bucketIndex`) | Chỉ khi Tab 1 đang hiển thị |
| Nhãn "BONUS!" | Trên hũ jackpot | Mỗi `JackpotHit` từ origin hợp lệ | Chỉ khi Tab 1 đang hiển thị |
| Phản hồi bàn bận | Khấc vùng thả | Tối đa 1 lần / `busyFeedbackMinInterval` | Chạm bị bỏ |
| Tổng xu / CPS | Header (Canvas) | Do HUD & Tab Navigation quyết định | — (không thuộc Plinko) |

UX flag: this system has UI. A UX spec per screen is recommended before epics; UX authoring is not in this plugin yet, so describe each screen in this section or a short spec next to the GDD. Penpot board `Main_Plinko` / `Main_Plinko_Fever` là đặc tả màn hình hiện có và cần cập nhật bố cục hũ + màu thang ấm.

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Pillars, Quyết định 5, MVP Definition | `Docs/design/gdd/game-concept.md` | Pillar 1, Pillar 3 (hai kênh thu nhập), bố cục 3 tab | Ràng buộc thiết kế |
| Thu nhập kênh Plinko | `Docs/design/gdd/economy-core.md` | R5.1 (`CoinResolved`, mọi origin/reason qua kênh Plinko), R5.4 (`IncomeCredited`, số bay = `creditedAmount`), R7.4 (auto-drop không quy đổi offline), R10 + D4 (`base_coin_value`, `IBaseCoinValueSource`) | Economy Core tiêu thụ `CoinResolved` và cài đặt nguồn `baseCoinValue` của Plinko |
| Độ lớn hiệu ứng thú | `Docs/design/gdd/pet-catalog.md` | R6 (loại hiệu ứng, `includeBurstCoins`), R8 (Kẹo Bảy Màu `BouncinessModifier` 1.7, Mèo Chiêu Tài `JackpotBonus`, Hamster `AutoDropRate`), `plinko_target_uplift` (Kẹo 0.324), `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` (0.40) | Pet Catalog sở hữu độ lớn và ngân sách; Plinko sở hữu sự kiện, API và trần; AC-13c đo lại giả định |
| Hiệu ứng thú lên Plinko | `Docs/design/gdd/shelf-and-synergy.md` *(chưa có)* | Kẹo Bảy Màu (độ nảy), Thỏ (chạm đinh được tính), Mèo Chiêu Tài (jackpot), tốc độ auto-drop | Shelf tiêu thụ sự kiện + đăng ký nguồn qua API R12.4 |
| Mưa Đồng Xu, Trà Sốt Dẻo | `Docs/design/gdd/boosts.md` *(chưa có)* | `RequestBurst(Rain)`, auto-drop tạm thời qua `RegisterAutoDropRate` | Boosts gọi API Plinko |
| Mưa xu qua quảng cáo | `Docs/design/gdd/rewarded-ads.md` *(chưa có)* | `RequestBurst(Rain)` | Ads gọi API Plinko |
| Pool xu | `Docs/design/gdd/object-pooling.md` | Pool `plinko.coin` (`client_declared_max` = `maxActiveCoins`, Fixed, `forbid_recycle`), R7.1 (Fixed cạn), `pools[plinko.coin].headroom` = 0 và pool hiệu ứng tan `feedback.coinFizzle` (phương án B, quyết định user, Revision 4) | Phụ thuộc kỹ thuật |
| Âm thanh | `Docs/design/gdd/audio.md` | D1, A15 (Audio không nghe Plinko), D5/A8 (bus Plinko −12 dB khi bàn ẩn), bảng cue `Plinko.*`, `SoundCue[Plinko.PegHitScored].pitchJitterRatio` | Gián tiếp qua Feedback & Juice |
| Phản hồi | `Docs/design/gdd/feedback-and-juice.md` *(chưa có)* | `PlinkoFeedbackConfig` có thể chuyển sở hữu sang đây; ánh xạ sự kiện R12 → cue; pool số bay và pool hiệu ứng tan | Feedback tiêu thụ sự kiện |
| Hướng dẫn | `Docs/design/gdd/tutorial-and-onboarding.md` *(chưa có)* | Cờ `acceptManualTaps`, `CoinResolved` đầu tiên | Tutorial điều khiển |
| Input, tab, vòng đời | `Docs/design/gdd/bootstrap-and-scene-flow.md` | B2/B3 (hợp đồng `IBootStep`, bước `plinko.board`), B6 (`Paused`/`Resumed`, Boot D3; B6.5 `Interactive` + `IsSimulationRunning`; B6.9 trạng thái trả về sau `Resumed`; B6.10 cờ "ad đang mở"), B7 (cổng chặn input, ranh giới `acceptManualTaps`), B9 (`ActiveTab`/`TabChanged`, router `WorldPress`), `BootTestTargets.perfReferenceDevice` | Phụ thuộc kỹ thuật → ADR |
| Lỗi chặn đã xử lý | `Docs/design/gdd/reviews/first-gdd-review-2026-10-21.md` | BLOCKING #1, #2, #4 + phần Plinko của #3 | Truy vết |
| Quyết định user | `Docs/production/session-logs/decision-log.md` | Trần mềm (a); hiệu ứng tan B; xả hàng đợi loạt khi `Paused`; rewarded ad không tạm dừng (Bootstrap B) | Ràng buộc thiết kế |
| Review Round 3 | `Docs/design/gdd/reviews/plinko-board-review-2026-10-21.md` | PB3-B1 (tham số test thành knob) | Truy vết |

## Acceptance Criteria

Nguồn: qa-lead (2026-10-21). Ký hiệu `C.` = `PlinkoConfig`, `T.` = `PlinkoBalanceTargets`, `F.` = `PlinkoFeedbackConfig`. "Sim" = scene vật lý cục bộ trong EditMode (`LocalPhysicsMode.Physics2D`), bước `PhysicsScene2D.Simulate(fixedDeltaTime)` gọi tay, RNG seed cố định, đồng hồ được inject (không dùng giờ thực); vì xu không va nhau (R1.4) có thể thả cả lô xu một lượt. Test sim nặng (gắn `[Slow]`) chạy trong category NUnit `Slow`: bắt buộc trước khi đóng story Plinko và trên CI đêm; chỉ assert cân bằng trên Editor (thứ tự giải của Box2D có thể khác giữa nền tảng). Các con số trong ngoặc là giá trị hiện tại của knob, không phải literal của test. Mọi tham số test (số xu, số giây, tần suất chạm, dung sai, số khung, độ phân giải) là knob `T.` hoặc knob test của hệ khác (`BootTestTargets.perfReferenceDevice`, `PoolingTestTargets.perfSampleFrames`). Còn lại là **dữ liệu fixture — constant** (giá trị chọn để chạm biên một luật, đổi nó không phải tuning): giá trị RNG giả của AC-5; các lớp modifier/tốc độ của AC-14, AC-15, AC-27, AC-43 (và tốc độ auto-drop 1 của nguồn "a" ở AC-29, AC-48); cấu hình cố ý sai của AC-19; giá trị nguồn giả của AC-45 (2.68 và 18.68 = `base_coin_value` ở n = 10 và n = 100 với α 0.6 của Economy D4); số 1 trong "+ 1" của AC-12 (cú chạm ở t = 0); ε của AC-8 (một tích tắc của đồng hồ inject). Test của Plinko không đăng ký `IBaseCoinValueSource` (trừ AC-45), nên `C.baseCoinValue` trong các AC là giá trị `base_coin_value` Plinko đọc được lúc giải quyết xu (R9.2).

**Hình học (R1)**
- `AC-1 [EditMode]` **GIVEN** `C` mặc định, **WHEN** dựng bàn, **THEN** có `C.pegRows` tầng; tường tại ±`C.pegsPerRow`·`C.pegSpacing`/2; tầng chẵn có `C.pegsPerRow` − 2 đinh tự do tại x = (k − (`C.pegsPerRow`−1)/2)·`C.pegSpacing`, k = 1…`C.pegsPerRow`−2, cộng 2 nửa đinh bán kính `C.wallBumpRadius` có tâm trên mặt tường; tầng lẻ có `C.pegsPerRow` − 1 đinh tại x = (k − (`C.pegsPerRow`−2)/2)·`C.pegSpacing`; khoảng tầng = `C.rowSpacingRatio` × `C.pegSpacing` (sai số ≤ `T.geometryTolerance`).
- `AC-2 [EditMode]` **GIVEN** `C.bucketMultipliers`, `C.bucketWidthWeights`, **WHEN** dựng hũ, **THEN** số hũ = số phần tử của `C.bucketMultipliers` (5) theo đúng thứ tự, tổng bề rộng = board_inner_width (sai số ≤ `T.geometryTolerance`), đúng hai hũ ngoài cùng có `bucket_is_jackpot`.
- `AC-3 [EditMode]` **GIVEN** xu Falling đi vào cảm biến hũ i, **WHEN** trigger xảy ra, **THEN** xu chuyển Resolved với `bucketIndex = i` ngay trong bước đó, không chờ xu nằm yên.
- `AC-4 [EditMode]` **GIVEN** ma trận layer của project, **WHEN** đọc `Physics2D.GetIgnoreLayerCollision`, **THEN** Coin–Coin bị bỏ qua; Coin–Peg, Coin–Wall, Coin–Divider va chạm.

**Vùng thả và nguồn xu (R2, R3, drop_x)**
- `AC-5 [EditMode]` **GIVEN** RNG giả trả −1 / 0 / +1, **WHEN** tính `drop_x` với `tap_x` ngoài vùng thả, **THEN** x = mép vùng gần nhất + {−1, 0, +1} × `C.dropJitter` × `C.pegSpacing`; với `T.simCoinCountLarge` mẫu RNG thật, mọi x ∈ [wall_left + `C.coinRadius`, wall_right − `C.coinRadius`].
- `AC-6 [EditMode]` **GIVEN** chạm có y bất kỳ, hoặc nguồn Auto/Rain/JackpotBurst, **WHEN** sinh `T.simCoinCountLarge` xu, **THEN** y = vạch thả và x trước jitter ∈ [zone_min, zone_max]; với nguồn không có điểm chạm, chia vùng thả thành `T.dropUniformityBins` ô bằng nhau, mỗi ô nhận 1 / `T.dropUniformityBins` ± `T.dropUniformityTolerance` (1.5%) số xu.
- `AC-7 [EditMode]` **GIVEN** bốn đường sinh xu, **WHEN** mỗi xu được giải quyết, **THEN** `CoinResolved.origin` đúng nguồn (Manual / Auto / Rain / JackpotBurst).

**Nhận xu, chống autoclicker (R4, tap_admitted) — BLOCKING #4 review**
- `AC-8 [EditMode]` **GIVEN** đồng hồ inject, **WHEN** chạm ở t = 0, t = `C.tapCooldown` − ε, t = `C.tapCooldown`, **THEN** kết quả lần lượt nhận / bỏ / nhận, và cú bị bỏ không dời mốc cooldown.
- `AC-9 [EditMode]` **GIVEN** số xu hoạt động = `C.maxActiveCoins`, **WHEN** có cú chạm đã qua cooldown và một nhịp Auto, **THEN** cả hai bị bỏ; chỉ cú chạm phát `BoardBusy`; không có gì được xếp hàng; nhịp Auto không dồn sang nhịp sau.
- `AC-10 [EditMode]` **GIVEN** hàng đợi loạt không rỗng, đã qua `C.burstSpawnInterval`, **WHEN** sim đủ lâu, **THEN** số xu hoạt động do hàng đợi và `Auto` sinh ra không bao giờ làm tổng vượt `C.maxActiveCoins` − `C.manualReservedSlots`; khi tổng = `C.maxActiveCoins` − `C.manualReservedSlots`, hàng đợi và nhịp Auto chờ.
- `AC-10b [EditMode]` **GIVEN** `RequestBurst(C.maxActiveCoins + 1, Rain)` đang chạy (hàng đợi đã lấp tới `C.maxActiveCoins` − `C.manualReservedSlots`), **WHEN** người chơi chạm `C.manualReservedSlots` lần cách nhau ≥ `C.tapCooldown`, **THEN** cả `C.manualReservedSlots` cú chạm được nhận và không phát `BoardBusy`; cú chạm kế tiếp khi bàn đủ `C.maxActiveCoins` mới phát `BoardBusy`.
- `AC-11 [EditMode]` **GIVEN** `RequestBurst(N)` với N > `C.burstQueueMax`, **WHEN** sim tới khi hàng đợi rỗng, **THEN** hai lần sinh liên tiếp cách nhau ≥ `C.burstSpawnInterval`, hàng đợi luôn ≤ `C.burstQueueMax`, phần dư nhận ngay `CoinResolved(reason = Overflow)`, tổng số `CoinResolved` = N.
- `AC-12 [EditMode]` **GIVEN** autoclicker ở từng tần suất trong `T.autoclickerRatesPerSecond` ([30, 60, 240] chạm/giây; có nhiều chạm trong cùng khung), **WHEN** sim `T.autoclickerSimSeconds` (10 s), **THEN** số xu Manual được nhận ≤ floor(`T.autoclickerSimSeconds` / `C.tapCooldown`) + 1 và số xu hoạt động ≤ `C.maxActiveCoins` ở mọi bước.
- `AC-13 [EditMode, Slow]` (cấu hình gốc; trần mềm — mỗi cấu hình một trần test) **GIVEN** spam ở tần suất lớn nhất của `T.autoclickerRatesPerSecond` (> 1 / `C.tapCooldown`) trong `T.throughputSimSeconds` sim ở độ nảy gốc, không có nguồn auto-drop, có jackpot thật, **WHEN** tính thông lượng, **THEN** số Manual nhận mỗi giây ≤ 1 / `C.tapCooldown` và tổng `rawValue` / giây (gồm mọi reason, kể cả Overflow — nguồn vượt sức chứa bàn) ≤ `T.maxRawPayoutPerSecondPerBaseCoin` × `C.baseCoinValue`.
- `AC-13b [EditMode, Slow]` **GIVEN** cùng kịch bản spam và thời lượng sim như AC-13, độ nảy gốc, thêm `RegisterAutoDropRate("auto", r)` với r ≥ `C.autoDropBaseInterval` / `C.autoDropMinInterval` (chu kỳ ở sàn), **WHEN** tính thông lượng, **THEN** số Auto nhận mỗi giây ≤ 1 / `C.autoDropMinInterval`, số Manual nhận mỗi giây ≤ 1 / `C.tapCooldown`, và tổng `rawValue` / giây (gồm Overflow) ≤ `T.maxRawPayoutPerSecondPerBaseCoinWithAuto` × `C.baseCoinValue`.
- `AC-13c [EditMode, Slow]` **GIVEN** cùng kịch bản spam và thời lượng sim như AC-13, không có nguồn auto-drop, `RegisterBouncinessFactor("cap", f)` với f ≥ `C.bouncinessCap` / `C.basePegBounciness` (độ nảy hiệu lực = `C.bouncinessCap`), **WHEN** tính thông lượng, **THEN** tổng `rawValue` / giây (gồm Overflow) ≤ `T.maxRawPayoutPerSecondPerBaseCoinAtCap` × `C.baseCoinValue`, và test in tỉ lệ tổng `rawValue` / giây so với AC-13 (giá trị đo của `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed`).
- `AC-13d [EditMode, Slow]` **GIVEN** cùng kịch bản spam và thời lượng sim như AC-13, `RegisterAutoDropRate("auto", r)` như AC-13b (chu kỳ ở sàn) và `RegisterBouncinessFactor("cap", f)` như AC-13c (độ nảy hiệu lực = `C.bouncinessCap`), **WHEN** tính thông lượng, **THEN** số Auto nhận mỗi giây ≤ 1 / `C.autoDropMinInterval`, số Manual nhận mỗi giây ≤ 1 / `C.tapCooldown`, và tổng `rawValue` / giây (gồm Overflow) ≤ `T.maxRawPayoutPerSecondPerBaseCoinAutoAndCap` × `C.baseCoinValue`.

**Vật lý và trần nảy (R5, effective_bounciness, anti_tunnel) — BLOCKING #2 review**
- `AC-14 [EditMode]` **GIVEN** modifier ∈ {0.5, 1, 2, 3, 100}, **WHEN** tính `effective_bounciness`, **THEN** kết quả = min(`C.basePegBounciness` × max(m, 1), `C.bouncinessCap`) (m = 0.5 cho đúng `C.basePegBounciness`) và PhysicsMaterial2D của đinh mang đúng giá trị đó; validator báo lỗi khi `C.bouncinessCap` ≥ 1.
- `AC-15 [EditMode]` **GIVEN** modifier nảy là âm, NaN, vô cực, 0 hoặc 0.5, hoặc tốc độ auto là âm, NaN hoặc vô cực, **WHEN** áp vào bàn, **THEN** `effective_bounciness` = min(`C.basePegBounciness`, `C.bouncinessCap`) và timer auto-drop dừng.
- `AC-16 [EditMode]` **GIVEN** `T.simCoinCountSmall` xu sim ở `C.bouncinessCap`, **WHEN** mỗi xu sinh ra và rơi, **THEN** gravityScale = `C.coinGravityScale`, vận tốc ban đầu = 0, và |v| ≤ `C.maxCoinSpeed` + `T.speedTolerance` sau mọi bước.
- `AC-17 [EditMode, Slow]` **GIVEN** độ nảy hiệu lực = `C.bouncinessCap`, **WHEN** sim `T.simDropXSamples` × `T.simCoinsPerDropX` xu, **THEN** qua mọi va chạm xu–đinh năng lượng cơ học ½v² + g·gravityScale·h không tăng quá `T.energyGainTolerance`; 100% xu Resolved trước `C.coinMaxLifetime`; mọi xu có `scoredPegHits` ≤ `C.maxScoredPegHits`.
- `AC-18 [EditMode, Slow]` **GIVEN** cùng sim ở trần nảy, **WHEN** tổng hợp, **THEN** p_jackpot trung bình ≤ `T.capJackpotProbMeanMax`, EV_mean ≤ `T.capEvMeanMax`, và `rawValue` mọi xu ≤ max(`C.bucketMultipliers`) × `C.baseCoinValue`.
- `AC-19 [EditMode]` **GIVEN** cấu hình hiện tại và một cấu hình cố ý sai, **WHEN** chạy validator, **THEN** cấu hình hiện tại thỏa `C.maxCoinSpeed` × fixedDeltaTime < `C.pegRadius` + `C.coinRadius` và `min_passage_gap` (đinh–đinh, đinh–tường tầng lẻ, đinh–nửa đinh tầng chẵn, khe chéo giữa hai tầng kề nhau) ≥ 2·`C.coinRadius` + `C.minGapMargin`; mỗi cấu hình sai sau sinh lỗi validator: (a) `C.coinRadius` = 0.20; (b) bỏ nửa đinh để đinh ngoài cùng tầng chẵn đứng cách tường ½`C.pegSpacing`; (c) `C.rowSpacingRatio` = 0.75, các knob khác mặc định (khe chéo ≈ 0.376 u < 0.38 u, ba vế còn lại hợp lệ) — lỗi validator phải nêu vế khe chéo; sim ở tốc độ tối đa không có xu nào ra khỏi khung bàn.
- `AC-19b [EditMode, Slow]` **GIVEN** `T.simCoinsPerDropX` xu thả sát tường (x sinh = wall ± `C.coinRadius`, dùng trực tiếp không qua vùng thả), **WHEN** sim xong, **THEN** tỉ lệ xu kẹt ≤ `T.stuckRateMax` và không xu nào bị kẹt nêm giữa đinh tầng chẵn và tường.

**Chạm đinh (R6, peg_hit_scored)**
- `AC-20 [EditMode]` **GIVEN** logic `peg_hit_scored`, **WHEN** xét: đã đạt `C.maxScoredPegHits` / lặp cùng đinh trong < `C.pegHitDebounce` / lặp cùng đinh sau ≥ `C.pegHitDebounce` / đinh chưa chạm, **THEN** `scored` = false / false / true / true, và `PegHit` vẫn phát trong cả bốn trường hợp.
- `AC-21 [Play mode]` **GIVEN** một xu rơi, **WHEN** xu va đinh rồi va tường, **THEN** mỗi va chạm xu–đinh phát đúng một `PegHit` kèm vị trí world; va tường không phát `PegHit`.

**Jackpot và Bùng Xu (R7)**
- `AC-22 [EditMode]` **GIVEN** xu origin Manual, Auto hoặc Rain, **WHEN** vào hũ jackpot, **THEN** phát đúng 1 `JackpotHit` và hàng đợi loạt tăng đúng `C.jackpotBurstCount`.
- `AC-23 [EditMode]` **GIVEN** stub "mọi hũ đều là jackpot", **WHEN** thả 1 xu Manual và sim tới khi bàn Idle, **THEN** tổng số xu JackpotBurst = `C.jackpotBurstCount`; mỗi xu JackpotBurst có `rawValue` = `C.baseCoinValue` × hệ số hũ và phát `JackpotHit` nhưng làm hàng đợi tăng 0 (không dây chuyền).

**Kẹt, trả thưởng (R8, R9, coin_payout)**
- `AC-24 [EditMode]` **GIVEN** xu có |v| < `C.stuckSpeedThreshold` liên tục `C.stuckTime` (thời gian sim), hoặc xu rung trên ngưỡng đến tuổi `C.coinMaxLifetime`, **WHEN** tới mốc, **THEN** xu thành Stuck, `rawValue` = `C.baseCoinValue` × `C.stuckPayoutMultiplier`, không phát `JackpotHit`, không có Bùng Xu.
- `AC-25 [EditMode]` **GIVEN** từng hũ i, một ca kẹt và một ca tràn hàng đợi, **WHEN** xu được giải quyết, **THEN** `rawValue` = `C.baseCoinValue` × `C.bucketMultipliers[i]` (hũ x5 → 5) / × `C.stuckPayoutMultiplier` / × `C.instantPayoutMultiplier` (1.9).
- `AC-26 [EditMode, Slow]` **GIVEN** sim `T.simCoinCountLarge` xu có xu rơi đúng mép vách, **WHEN** đếm sự kiện, **THEN** số `CoinResolved` = số xu được nhận + số Overflow, không id nào bị giải quyết hai lần.

**Auto-drop và vòng đời (R10, R11, auto_drop_interval)**
- `AC-27 [EditMode]` **GIVEN** `auto_rate_multiplier` = 0 / 1 / 2 / 100, **WHEN** tính chu kỳ, **THEN** lần lượt timer dừng / `C.autoDropBaseInterval` / `C.autoDropBaseInterval` ÷ 2 / `C.autoDropMinInterval`; gỡ nguồn cuối khi đang chờ nhịp → không sinh thêm xu Auto, xu đang rơi vẫn được giải quyết.
- `AC-28 [Play mode]` **GIVEN** `T.lifecycleTestCoins` xu đang rơi, **WHEN** chuyển sang Tab 2 trong `T.tabSwitchTestSeconds`, **THEN** số `CoinResolved` vẫn tăng, renderer của bàn tắt, mô phỏng vẫn chạy.
- `AC-29 [EditMode]` **GIVEN** `T.lifecycleTestCoins` xu đang rơi, hàng đợi loạt không rỗng, nguồn vòng đời giả của Bootstrap và đồng hồ inject, **WHEN** nguồn giả phát `Paused {Hidden}`, đồng hồ thực tiến `T.pauseTestSeconds`, rồi phát `Resumed {Hidden, awaySeconds = T.pauseTestSeconds}` với `IsSimulationRunning` = đúng (vòng đời về `Running`), **THEN** ngay sau `Paused` hàng đợi loạt rỗng (xả theo R11.3, chi tiết ở AC-47); trong lúc Paused số bước mô phỏng tiến = 0; sau `Resumed` không xu nào thành Stuck hay hết tuổi, vị trí xu đang rơi và mốc cooldown giữ nguyên như lúc `Paused`. **AND WHEN** chỉ mô phỏng mất focus `T.pauseTestSeconds` (không phát `Paused`), hoặc cờ "ad đang mở" của nguồn giả bật trong `T.pauseTestSeconds` (Bootstrap B6.10, không phát `Paused`), **THEN** ở cả hai ca mô phỏng tiến đủ số bước của `T.pauseTestSeconds`, số `CoinResolved` tăng và hàng đợi loạt không bị xả (không có `CoinResolved(reason = Overflow)` nào ngoài tràn `C.burstQueueMax`). **AND GIVEN** nguồn vòng đời giả ở `Booting.Curtain` (`IsSimulationRunning` = sai), bàn ở Chờ mở cửa sau `Run()`, một nguồn `RegisterAutoDropRate("a", 1)` và hàng đợi có `C.jackpotBurstCount` xu `Rain`, **WHEN** nguồn giả phát `Paused {Hidden}`, rồi `Resumed {Hidden, awaySeconds = T.pauseTestSeconds}` với `IsSimulationRunning` vẫn sai (trả về `Booting.Curtain`, Bootstrap B6.9), đồng hồ inject tiến `T.pauseTestSeconds`, rồi nguồn giả phát `Interactive` (`IsSimulationRunning` = đúng), **THEN** ngay sau `Paused` có đúng `C.jackpotBurstCount` `CoinResolved(reason = Overflow)` và hàng đợi rỗng; từ `Resumed` tới `Interactive` số bước mô phỏng = 0, 0 `CoinSpawned`, bàn ở Chờ mở cửa; sau `Interactive` bước mô phỏng tiến và xu `Auto` đầu tiên sinh sau max(`C.autoDropBaseInterval` / 1, `C.autoDropMinInterval`) thời gian mô phỏng (sai số một bước vật lý — constant: `Time.fixedDeltaTime`, ProjectSettings/TimeManager, vì nhịp chỉ được xét ở biên bước).
- `AC-47 [EditMode]` (R11.3) **GIVEN** nguồn vòng đời giả, listener Economy giả, pool `plinko.coin` giả đếm Get, `T.lifecycleTestCoins` xu Falling và hàng đợi loạt đầy `C.burstQueueMax` xu gồm cả `Rain` và `JackpotBurst`, **WHEN** nguồn giả phát `Paused {Background}`, sau đó (vẫn Paused) gọi `RequestBurst(C.jackpotBurstCount, Rain)`, rồi phát `Resumed` và sim tới khi bàn Idle, **THEN** trong cùng lời xử lý `Paused` có đúng `C.burstQueueMax` `CoinResolved` theo thứ tự FIFO của `coinId`, mỗi cái có `reason = Overflow`, `bucketIndex` = none, `scoredPegHits` = 0, `origin` đúng nguồn, `rawValue` = `C.baseCoinValue` × `C.instantPayoutMultiplier`; 0 `CoinSpawned` và 0 Get cho các xu đó; hàng đợi rỗng; 0 `CoinResolved` cho xu Falling trong lúc Paused; `RequestBurst` trong lúc Paused phát ngay đúng `C.jackpotBurstCount` `CoinResolved(reason = Overflow)`; sau `Resumed`, tổng số `CoinResolved` = `T.lifecycleTestCoins` + `C.burstQueueMax` + `C.jackpotBurstCount` và không `coinId` nào bị giải quyết hai lần.

**Phân phối hũ và thời gian rơi (galton_spread, bucket_probability_and_ev) — BLOCKING #1 review**
- `AC-30 [EditMode, Slow]` **GIVEN** `T.simDropXSamples` điểm x0 rải đều trên vùng thả (gồm hai mép và tâm), mỗi điểm `T.simCoinsPerDropX` xu, seed cố định, độ nảy gốc, **WHEN** sim xong, **THEN** test in bảng p_jackpot, EV trực tiếp và **EV hiệu dụng** theo từng x0 (min/max/mean), trong đó EV hiệu dụng(x0) = trung bình trên các xu thả tại x0 của (hệ số hũ của xu đó + tổng hệ số hũ của mọi xu `JackpotBurst` mà xu đó kích hoạt, Bùng Xu bật và sinh như runtime) và assert: mean p_jackpot ∈ [`T.jackpotProbMeanMin`, `T.jackpotProbMeanMax`]; max p_jackpot theo x0 ≤ `T.jackpotProbMaxAtAnyX`; p_jackpot tại tâm ≥ `T.jackpotProbMinAtCenter`; EV_mean (trực tiếp) ∈ [`T.evMeanMin`, `T.evMeanMax`]; max / min EV hiệu dụng theo x0 (`aim_ratio`) ≤ `T.evRatioMax`; tỉ lệ kẹt ≤ `T.stuckRateMax`.
- `AC-31 [EditMode, Slow]` **GIVEN** cùng sim, **WHEN** đo thời gian từ sinh tới Resolved, **THEN** trung vị ∈ `T.targetMedianFallTime` ± `T.fallTimeTolerance`, p95 ≤ `T.fallTimeP95Max`, và validator xác nhận `T.fallTimeP95Max` ≤ `C.coinMaxLifetime` − `T.lifetimeMarginSeconds`.

**Tương tác với hệ khác**
- `AC-32 [EditMode]` **GIVEN** listener Economy giả, **WHEN** sim `T.simCoinCountSmall` xu, **THEN** mỗi xu phát `CoinResolved` đúng một lần với đủ trường coinId, origin, bucketIndex, multiplier, rawValue, scoredPegHits, reason (reason ∈ {Bucket, Stuck, Overflow}); Σ rawValue khớp tổng tính lại từ các trường; Plinko không ghi vào ví.
- `AC-33 [EditMode]` **GIVEN** Boosts giả gọi `RequestBurst(count, Rain)`, **WHEN** xử lý xong, **THEN** số xu sinh + số Overflow = count, mọi xu mang origin Rain; `count` ≤ 0 bị bỏ qua, không sự kiện nào phát.
- `AC-42 [EditMode]` **GIVEN** listener ghi mọi sự kiện, stub "hũ 0 là jackpot", sim có xu Manual, Auto, Rain, xu kích hoạt Bùng Xu, một xu bị ép kẹt, một `RequestBurst` vượt `C.burstQueueMax` và một cú chạm khi bàn đủ `C.maxActiveCoins`, **WHEN** sim tới khi bàn Idle, **THEN**: mỗi `CoinSpawned` có `coinId` chưa từng xuất hiện, `origin` đúng nguồn, `burstIndex` ∈ {0 … `C.jackpotBurstCount` − 1} và không trùng trong một loạt khi origin = JackpotBurst, = none với origin khác, `worldPosition.y` = vạch thả; mỗi `PegHit` có `coinId` của một xu đang Falling, `pegId` thuộc bàn đang dựng, `rowIndex` = tầng của đinh đó (0 … `C.pegRows` − 1), `scored` khớp `peg_hit_scored`, `worldPosition` cách tâm đinh ≤ `C.pegRadius` + `C.coinRadius` + `T.contactTolerance`; mỗi `JackpotHit` có `bucketIndex` của hũ jackpot, `origin` của xu (kể cả JackpotBurst) và phát trước `CoinResolved` cùng `coinId`; mỗi xu kẹt phát đúng một `CoinStuck` cùng `coinId` trước `CoinResolved(reason = Stuck, bucketIndex = none)`; xu Overflow có `CoinResolved(reason = Overflow, bucketIndex = none, scoredPegHits = 0)` và không có `CoinSpawned`; cú chạm bị bỏ phát đúng một `BoardBusy` với `worldPosition` = điểm chạm đã kẹp vào vùng thả trên vạch thả.
- `AC-43 [EditMode]` **GIVEN** bàn không có nguồn nào, **WHEN** lần lượt `RegisterAutoDropRate("a", 1)`, `RegisterAutoDropRate("b", 1.5)`, `RegisterAutoDropRate("a", 0.5)`, `RegisterBouncinessFactor("c", 1.7)`, `RegisterBouncinessFactor("d", 1.2)`, `RegisterBouncinessFactor("e", NaN)`, `Unregister("b")`, `Unregister("khong-co")`, **THEN** sau ba lệnh đầu `auto_rate_multiplier` lần lượt = 1, 2.5, 2.0, và sau `Unregister("b")` = 0.5; `bounciness_modifier` cuối = 1.7 × 1.2 × 1 = 2.04 và `effective_bounciness` = min(`C.basePegBounciness` × 2.04, `C.bouncinessCap`); chu kỳ auto-drop = max(`C.autoDropBaseInterval` / 0.5, `C.autoDropMinInterval`); `Unregister` nguồn chưa đăng ký không đổi gì và không có exception.
- `AC-44 [EditMode]` **GIVEN** `acceptManualTaps` = false, hàng đợi Rain không rỗng và một nguồn auto-drop, **WHEN** gửi `WorldPress` hợp lệ đã qua cooldown rồi bật lại cờ và gửi tiếp sau `C.tapCooldown`, **THEN** lần đầu 0 xu Manual, 0 `BoardBusy`, mốc cooldown không đổi, xu Rain và Auto vẫn sinh; lần sau 1 xu Manual được nhận.
- `AC-45 [EditMode]` **GIVEN** (a) không đăng ký `IBaseCoinValueSource`; (b) một nguồn giả trả 2.68 rồi đổi sang 18.68 khi xu đang rơi, **WHEN** một xu vào hũ x2, **THEN** (a) `rawValue` = `C.bucketMultipliers[1]` × `C.baseCoinValue`; (b) `rawValue` = `C.bucketMultipliers[1]` × 18.68 (giá trị đọc lúc giải quyết, không phải lúc sinh xu); Plinko không có API ghi `baseCoinValue` và validator không cảnh báo với giá trị runtime 18.68 của nguồn (khác `C.baseCoinValue`; giá trị runtime không có trần). 2.68 và 18.68 là dữ liệu fixture (phần mở đầu AC).
- `AC-48 [EditMode]` (R13) **GIVEN** registry bước khởi động giả của Bootstrap, dịch vụ pool giả và router giả, **WHEN** scene nạp, Plinko đăng ký bước rồi Bootstrap giả gọi `Run()`, **THEN** có đúng một bước `Id` = `plinko.board`, `Phase` = `GameplayInit`, `RunsAfter` rỗng, `IsCritical` = đúng; `Run()` được gọi đúng 1 lần và `IsDone` = đúng ngay trong khung đó; pool giả nhận đúng 1 đăng ký `plinko.coin` với `client_declared_max` = `C.maxActiveCoins`, `forbid_recycle` = true; router giả có `PlinkoViewport` + camera Plinko; gốc hình ảnh tab `Board` đã đăng ký; bàn ở Chờ mở cửa, 0 xu, hàng đợi rỗng. **AND WHEN** sau `Run()` nguồn vòng đời giả giữ `IsSimulationRunning` = sai (`Booting.Steps` rồi `Booting.Curtain`), test gọi `RegisterAutoDropRate("a", 1)` và `RequestBurst(C.jackpotBurstCount, Rain)`, gửi một `WorldPress` hợp lệ, và đồng hồ inject tiến `T.pauseTestSeconds`, **THEN** 0 lần gọi `PhysicsScene2D.Simulate` (0 bước vật lý), 0 `CoinSpawned` (0 xu `Auto`, 0 xu `Rain`, 0 xu `Manual`), 0 `BoardBusy`, 0 `CoinResolved`, 0 Get trên pool giả, hàng đợi giữ đúng `C.jackpotBurstCount` xu và `auto_rate_multiplier` = 1. **AND WHEN** nguồn giả phát `Interactive` (`IsSimulationRunning` = đúng) và sim tiếp, **THEN** bước vật lý bắt đầu tiến, xu `Rain` trong hàng đợi sinh theo R4.2, và xu `Auto` đầu tiên sinh sau max(`C.autoDropBaseInterval` / 1, `C.autoDropMinInterval`) thời gian mô phỏng kể từ `Interactive` (sai số một bước vật lý, constant `Time.fixedDeltaTime` như AC-29). **AND WHEN** pool giả từ chối đăng ký `plinko.coin`, **THEN** `IsFailed` = đúng, `IsDone` = sai, không có viewport nào được đăng ký vào router giả.
- `AC-49 [EditMode]` (R11.4.1) **GIVEN** bàn `Running` với `T.lifecycleTestCoins` xu đang rơi và hàng đợi loạt, **WHEN** một khung có dt = `T.hitchFrameSeconds` (> `C.maxSimStep`) rồi dt = 1/60 s (constant, khung tham chiếu 60 fps), **THEN** thời gian mô phỏng tiến đúng `C.maxSimStep` ở khung đầu và 1/60 s (constant, khung tham chiếu 60 fps) ở khung sau (sai số 1e-6 s, constant: độ chính xác float), và không xu nào bị bỏ qua cảm biến hay đinh (không xuyên).
- `AC-46 [EditMode]` **GIVEN** pool `plinko.coin` giả đếm Get/Release, **WHEN** sim `T.simCoinCountSmall` xu gồm xu vào hũ và xu bị ép kẹt, **THEN** mỗi xu Release đúng một lần trong cùng bước mô phỏng phát `CoinResolved` của nó, số đối tượng đang mượn ≤ `C.maxActiveCoins` ở mọi bước, và xu Queued/Overflow không bao giờ Get.
- `AC-34 [Play mode]` **GIVEN** `InputTestFixture`, **WHEN** lần lượt: chạm trúng nút vặn trứng / header / toast; chạm trong khung bàn nhưng ngoài vùng thả; `T.multiTouchTestFingers` ngón cùng khung hình, **THEN** lần lượt 0 xu; 1 xu tại mép vùng thả; 1 xu.

**Cảm giác và hiệu năng**
- `AC-35 [Play mode]` **GIVEN** bàn Idle, **WHEN** một cú chạm hợp lệ được gửi qua `InputTestFixture`, **THEN** xu xuất hiện ở vạch thả trong ≤ `T.tapToSpawnMaxMs`; một cú chạm bị bỏ phát phản hồi bàn bận trong ≤ `T.tapToSpawnMaxMs`, và phản hồi bàn bận lặp lại không dày hơn `F.busyFeedbackMinInterval`.
- `AC-36 [Perf]` **GIVEN** bản WebGL chạy trên máy theo hồ sơ `BootTestTargets.perfReferenceDevice` (hồ sơ tạm của Bootstrap, cho tới khi ADR hiệu năng của technical-director thay bằng thiết bị tầm trung cụ thể), số xu hoạt động = `C.maxActiveCoins` cộng `RequestBurst(C.burstQueueMax)`, **WHEN** đo `T.perfSampleSeconds`, **THEN** frame time p95 ≤ 1000 / `targetFps` của studio config (16.7 ms) và thời gian Physics2D p95 ≤ `T.physicsFrameBudgetMs`.
- `AC-37 [Perf]` **GIVEN** cùng kịch bản và máy đo như AC-36, **WHEN** soi Frame Debugger ở khung có số xu hoạt động = `C.maxActiveCoins`, **THEN** số draw call của camera Plinko ≤ `T.boardDrawCallBudget`.
- `AC-38 [Perf]` **GIVEN** pool đã warm, trạng thái ổn định, **WHEN** ghi `PoolingTestTargets.perfSampleFrames` (600) khung bằng `ProfilerRecorder` "GC Allocated In Frame", **THEN** 0 B mỗi khung, kể cả lúc phát `PegHit`, `CoinResolved` và số bay (điều kiện: `Physics2D.reuseCollisionCallbacks` bật; bảng debounce theo cặp xu–đinh và bộ đệm sự kiện được cấp phát sẵn theo `C.maxActiveCoins`, không cấp phát khi chạy).

**Hiển thị**
- `AC-39 [Screenshot]` **GIVEN** Tab 1 dọc ở độ phân giải tham chiếu `T.screenshotReferenceResolution` (1080×1920), **WHEN** chụp, **THEN** nhãn `x5 x2 x1 x2 x5` sinh từ `C.bucketMultipliers` hiện với màu thang ấm (x1 gỗ nhạt, x2 matcha, x5 mật ong) và độ tương phản giữa chữ nhãn và nền hũ ≥ `T.labelContrastMin` (4.5:1, đo trên ảnh chụp); đổi config thì nhãn đổi theo.
- `AC-40 [Screenshot]` **GIVEN** Tab 1 ở `T.screenshotReferenceResolution`, **WHEN** chụp, **THEN** thấy hai khấc gỗ đánh dấu vùng thả, tâm mỗi khấc (chiếu từ world sang pixel bằng camera Plinko) cách board_center_x ± `C.dropZoneWidthRatio` × board_inner_width / 2 không quá `T.screenshotPositionTolerancePx`.
- `AC-41 [Screenshot]` **GIVEN** Tab 1 ở `T.screenshotReferenceResolution`, một xu vào hũ x5 và một xu vào hũ x1, **WHEN** chụp ở khung có số bay, **THEN** tâm ngang mỗi số bay cách tâm ngang của đúng hũ nhận xu không quá `T.screenshotPositionTolerancePx`, hũ jackpot sáng mật ong và hiện nhãn "BONUS!" nằm trong bề rộng của hũ đó.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| ~~`baseCoinValue` có co giãn theo tiến độ (CPS, giá quay, prestige) không, và theo công thức nào?~~ **Đã đóng** — Economy D4 + `economy-core.md` R10: `base_coin_value` = (giá quay / giá gốc)^α do Economy Core cấp qua `IBaseCoinValueSource`; `PlinkoConfig.baseCoinValue` = 1 chỉ là dự phòng (R9.2). | Economy Core | 2026-10-21 (đã đóng) |
| ~~Số bay hiển thị giá trị thô (`rawValue`) hay giá trị sau modifier?~~ **Đã đóng** — `economy-core.md` R5.4: số bay = `IncomeCredited.creditedAmount` (sau modifier), kèm `coinId`, `bucketIndex` để Feedback đặt và gộp theo hũ. | Economy Core | 2026-10-21 (đã đóng) |
| Từ Vertical Slice, xu **đang rơi** khi phiên kết thúc trong lúc Paused (≤ `maxActiveCoins`): trả ngay theo `instantPayoutMultiplier` lúc `Paused` (cùng `reason = Overflow` như R11.3) hay lưu và trả lúc mở lại? (MVP: mất, chấp nhận có chủ đích; hàng đợi loạt đã được trả ở `Paused` theo quyết định user.) Mặc định đề xuất cho Save/Load: trả ngay để khớp anti-pillar "không mất mát" (C2[17]). | Save/Load GDD | 2026-11-11 |
| `k_diff` thực tế của Physics2D là bao nhiêu? Nếu AC-30 trượt: giảm `pegRows` (p_jackpot quá cao) hoặc tăng `dropJitter` (quá thấp). | Prototype Plinko (gameplay-programmer) | 2026-11-04 |
| Ngân sách Physics2D (`physicsFrameBudgetMs`) và draw call của bàn (`boardDrawCallBudget`) trên WebGL tầm trung; `maxActiveCoins` = 24 có giữ được không? | technical-director (ADR hiệu năng) | 2026-11-04 |
| ADR (cần làm trước story Plinko): chế độ kết hợp độ nảy/ma sát của PhysicsMaterial2D trong Unity 6000.3 để độ nảy chỉ lấy theo đinh; tên API vận tốc 2D (`linearVelocity`); chế độ mô phỏng Physics2D cho test — xác minh `SceneManager.CreateScene` + `LocalPhysicsMode.Physics2D` có chạy được trong EditMode không (có thể chỉ Play mode) trước khi viết AC-17, AC-18, AC-19b, AC-26, AC-30, AC-31; `reuseCollisionCallbacks` và cấp phát sẵn cho AC-38. | technical-director → `game-adr` | 2026-11-04 |
| ~~Thu nhập auto-drop có được quy đổi khi offline không?~~ **Đã đóng** — `economy-core.md` R7.4: không quy đổi auto-drop sang thu nhập offline (Offline Earnings xác nhận lại khi viết GDD). | Economy Core | 2026-10-21 (đã đóng) |
| ~~Modifier kênh Plinko (ví dụ Trà Sốt Dẻo, Cáo Khăn Len `NeighborBoost` scope Plinko) có áp lên xu Auto / Rain / JackpotBurst như xu Manual không?~~ **Đã đóng** — `economy-core.md` R5.1: mọi origin và mọi reason đi qua kênh Plinko với `rawValue` làm gốc. | Economy Core | 2026-10-21 (đã đóng) |
| ~~**NEEDS-USER** — Trần thu nhập kênh Plinko có phải **cứng** không?~~ **Đã đóng** — user chọn (a) trần **mềm** (`decision-log.md`, 2026-10-21): `raw_rate_ceiling` là trần mềm theo cấu hình, Overflow là nguồn vượt trần, Economy Core cân bằng theo cấu hình (`economy-core.md` R5.5); trần test tách bốn cấu hình (`T.maxRawPayoutPerSecondPerBaseCoin`, `…WithAuto`, `…AtCap`, `…AutoAndCap`; AC-13 … AC-13d). Nội dung cũ: hiện `raw_rate_ceiling` là trần mô hình: ≈ 20 × `base_coin_value`/s chỉ ở độ nảy gốc không auto-drop; auto-drop ở sàn ≈ 27, Kẹo Bảy Màu (MVP) ≈ 26–31, cả hai ≈ 42 — vượt `T.maxRawPayoutPerSecondPerBaseCoin` (24) vì xu Bùng Xu tràn hàng đợi được trả ngay (Overflow vượt sức chứa). Lựa chọn: (a) giữ trần mềm như bản này, Economy Core cân bằng theo cấu hình (khuyến nghị tạm, không đổi gameplay); (b) Overflow trả thấp hơn `instant_payout_multiplier` hiện tại; (c) trần số xu Bùng Xu đang chờ, phần vượt bỏ hoặc trả thấp; (d) giảm nhịp xu sơ cấp khi có auto-drop (auto chiếm chỗ của chạm tay). Ảnh hưởng `economy-core.md` (Edge Case "Autoclicker trên Plinko … Economy Core không cần chặn thêm", output range dựa trên ≈ 20) và ngân sách Kẹo Bảy Màu của `pet-catalog.md`. | User + Economy Core | 2026-10-21 (đã đóng) |
| Cập nhật Penpot `Main_Plinko`, `Main_Plinko_Fever`: bố cục hũ `[x5][x2][x1][x2][x5]`, màu thang ấm, khấc vùng thả (hẹp hơn: 0.14 bề rộng bàn), nửa đinh gắn tường ở tầng chẵn, nhãn "BONUS!". | User + art-director (Penpot) | 2026-10-28 |
| ~~Hiệu ứng tan của xu kẹt dùng đối tượng nào, xu về pool lúc nào?~~ **Đã đóng** — user chọn **B** (`decision-log.md`, 2026-10-21): xu trả đối tượng về pool ngay khi Resolved, **không có trạng thái Fizzling** (R9.1); hiệu ứng tan do Feedback & Juice vẽ bằng pool trang trí riêng `feedback.coinFizzle` (RecycleOldest, `client_declared_max` = `PlinkoFeedbackConfig.fizzleMaxOnScreen`); `pools[plinko.coin].headroom` = 0 — khớp `object-pooling.md` Revision 4. | User + Object Pooling | 2026-10-21 (đã đóng) |
| Assumed: thêm sự kiện trình bày `CoinStuck { coinId, origin, worldPosition }` (ngoài năm sự kiện của fix list) vì `CoinResolved` không mang vị trí mà hiệu ứng tan cần sau khi xu đã về pool; giữ nguyên payload `CoinResolved` khớp Economy R5.1. | game-designer | 2026-10-21 |
| Assumed: nửa đinh gắn tường là đinh đối với `PegHit` (có `pegId`, `rowIndex` của tầng đó, có thể `scored`); va mặt tường phẳng không phát `PegHit` (AC-21). Lý do: nó thay chỗ một đinh trên lưới và vẫn tạo tiếng nảy. | game-designer | 2026-10-21 |
| Assumed: `coinId` cấp lúc nhận lên bàn hoặc lúc xếp hàng đợi loạt (xu Overflow cũng có id); `BoardBusy.worldPosition` = điểm chạm đã kẹp vào vùng thả trên vạch thả; khi `acceptManualTaps` tắt thì chạm bị bỏ im lặng (Tutorial tự lo gợi ý, tránh phản hồi "bận" gây nhầm). | game-designer | 2026-10-21 |
| Assumed: đăng ký lại cùng `sourceId` thay giá trị cũ; `Unregister` gỡ cả tốc độ lẫn hệ số nảy của nguồn (một thú/boost chỉ có một `sourceId`, tránh cộng đôi khi thú đổi ô). | game-designer | 2026-10-21 |
| Assumed (Revision 5): xu xả hàng đợi khi `Paused` dùng `reason = Overflow` (định nghĩa lại là "xu loạt trả ngay không qua mô phỏng"), không thêm reason mới — Economy xử lý mọi reason như nhau (R5.1), giữ enum `{Bucket, Stuck, Overflow}` của registry và Economy AC-15 không đổi; chỉ câu mô tả `Overflow` và trạng thái Paused của `economy-core.md` cần khớp (Economy phải nhận `CoinResolved` cả khi đã Paused, vì thứ tự hai hệ nghe `Paused` không xác định). | game-designer | 2026-10-21 |
| Assumed (Revision 5): `RequestBurst` đến trong lúc Paused được trả ngay (hàng đợi luôn rỗng khi Paused), xả theo FIFO trong cùng lời xử lý `Paused`, trước khi dừng mô phỏng — đơn giản nhất, không để thưởng nằm chờ qua trạng thái có thể bị đóng. | game-designer | 2026-10-21 |
| Assumed (Revision 6): Plinko đọc `IsSimulationRunning` mỗi khung trước khi tiến bước (một nguồn sự thật, không tự suy trạng thái từ chuỗi sự kiện), `Interactive` / `Resumed` chỉ là mốc chuyển; bộ hẹn giờ auto-drop đếm từ 0 lúc `Interactive`, không bù thời gian dưới rèm; `RequestBurst` trước `Interactive` xếp hàng (không trả ngay) vì thưởng chưa có nguy cơ mất trừ khi `Paused`, mà `Paused` đã xả theo R11.3. | game-designer | 2026-10-21 |
| Assumed (Revision 5): bước khởi động `plinko.board`, `IsCritical` = đúng (bàn là nguồn xu cốt lõi, Bootstrap Edge Case "Plinko lỗi → BootFailed"), `RunsAfter` rỗng, xong trong một khung — cùng mẫu `economy.core`. | game-designer | 2026-10-21 |
| Assumed (Revision 5): giá trị knob test mới lấy đúng literal cũ của AC (10000, 1000, [30, 60, 240], 10 s, 60 s, 5 s, 10 xu, 3 ngón, 1e-4 u, 1e-3) để hành vi test không đổi; thêm `dropUniformityBins` = 10 (cũ: "mỗi phần mười"), `screenshotPositionTolerancePx` = 4 px (khoảng một nửa độ dày khấc ở 1080×1920), `maxRawPayoutPerSecondPerBaseCoinAutoAndCap` = 50 (≈ 1.18 × mô hình 42.2, cùng biên với hai knob kia), `fizzleMaxOnScreen` = 4 (giá trị ví dụ của `object-pooling.md`; `stuckRateMax` 1% làm nhiều xu kẹt cùng lúc hiếm). | game-designer | 2026-10-21 |
