# Feedback & Juice

> **Status**: In Review
> **Author**: user + gdd-pipeline (game-designer)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 1 — Kinetic ASMR (đánh bóng cảm giác xu rơi / âm thanh); Pillar 2 — Gacha Thrill (khoảnh khắc bóc trứng có kiểm soát)

## Summary

Feedback & Juice là lớp trình bày thuần của game: nó nghe sự kiện của Plinko, Economy, Bootstrap và Shelf (và nhận lời gọi `RequestReveal` của Gacha qua API của chính nó) rồi đổi thành số bay, punch/tween, đinh sáng, hiệu ứng tan của xu kẹt, sân khấu bóc trứng theo phẩm cấp, haptic nhẹ, và gọi `Audio.Play(cueId, …)` qua bảng sự kiện → cue do chính nó sở hữu. Hệ không giữ trạng thái kinh tế, không đổi kết quả mô phỏng, và là chủ duy nhất của `FeedbackConfig` (nhận lại mọi knob trình bày từ `PlinkoFeedbackConfig`).

> **Quick reference** — Layer: `Presentation` · Priority: `MVP` · Key deps: `Plinko Board, Economy Core, Audio, Object Pooling, Bootstrap & Scene Flow (hard)`; `Shelf & Synergy (soft)`, `HUD & Tab Navigation (soft, provisional)`, `Pet Catalog (soft)`; Gacha phụ thuộc Feedback một chiều (Feedback không phụ thuộc Gacha)

## Overview

Pillar 1 nói "đánh bóng cảm giác hơn thêm tính năng", Pillar 2 nói "giữ nhịp hồi hộp nhưng có nút tua": hệ này là nơi hai câu đó thành số. Nó làm ba việc:

1. **Dịch sự kiện thành cảm giác**: mỗi sự kiện gameplay có tối đa một dòng trong `FeedbackCueTable` (cue, cao độ, pan, haptic, bậc ưu tiên) và một hiệu ứng hình ảnh; không có dòng nghĩa là im lặng.
2. **Giữ tiền luôn đọc được**: ngân sách hiệu ứng 3 bậc (FB D6) — tiền/jackpot > phẩm cấp > đinh/âm lặt vặt; khi quá tải thì bỏ bậc thấp trước, số bay dùng `RecycleOldest`.
3. **Sở hữu khoảnh khắc bóc trứng** (FB D3): `RevealProfile` theo phẩm cấp là dữ liệu của hệ này; Gacha chỉ gọi `RequestReveal` và nghe `RevealSettleStarted` / `RevealFinished`.

Tông luôn cozy: không chữ đỏ khi từ chối chi tiêu, không rung màn hình, không kéo dài hồi hộp để dụ quay tiếp, hình ảnh phẩm cấp chỉ lộ sau khi kết quả đã định.

## Detailed Design

### Core Rules

**R1. Ranh giới.** Feedback chỉ nghe sự kiện và chỉ gọi: `Audio.Play`, `Audio.SetBusBackground`, `Pooling` (Get/Release), haptic của nền tảng. Feedback không ghi ví, không đổi mô phỏng Plinko, không giữ kết quả Gacha. Mọi tween dùng thời gian không phụ thuộc `Time.timeScale` nhưng dừng khi `Paused` (R12). Cơ chế phát/nghe sự kiện và thư viện tween → ADR (cần ADR "feedback-tween-and-budget").

**R2. Chủ dữ liệu (FB D1, D4).** `FeedbackConfig` (một ScriptableObject) giữ mọi knob của hệ này (Tuning Knobs). `FeedbackCueTable` (ScriptableObject bảng dòng) là bảng sự kiện → cue (R4). `RevealProfileTable` (ScriptableObject, 4 dòng theo phẩm cấp) là bảng bóc trứng (R9). Giá trị đã khóa ở `PlinkoFeedbackConfig` giữ nguyên, chỉ đổi chủ và tên asset (Open Questions, mục Sync).

**R3. Mỗi sự kiện, một dòng, một lần.** Mỗi sự kiện nghe được phát sinh tối đa một cue và một nhóm hiệu ứng; không sự kiện nào phát lại khi `Paused` kết thúc. Sự kiện đến khi hệ ở `Suspended`/`Failed` bị bỏ im lặng.

**R4. Bảng sự kiện → cue** (`FeedbackCueTable`). Mỗi dòng: `eventType`, `condition` (một trường payload + giá trị), `cueId`, `pitchMode` (`None | PegRow | BurstIndex`), `panMode` (`None | WorldX`), `hapticId`, `tier` (1–3), `mergeKey` (`None | Bucket`). Dòng MVP:

| Sự kiện (điều kiện) | `cueId` (Audio) | pitchMode | panMode | tier | mergeKey |
|---|---|---|---|---|---|
| `CoinSpawned` (`origin` = Manual) | `Plinko.CoinSpawn` | None | WorldX | 3 | None |
| `CoinSpawned` (`origin` = Auto) | `Plinko.CoinSpawnAuto` | None | WorldX | 3 | None |
| `CoinSpawned` (`origin` = JackpotBurst) | `Plinko.JackpotBurstSpawn` | BurstIndex | WorldX | 3 | None |
| `CoinSpawned` (`origin` = Rain) | — (MVP; VS: `Plinko.RainLoop`) | — | — | — | — |
| `PegHit` (`scored` = đúng) | `Plinko.PegHitScored` | PegRow | WorldX | 3 | None |
| `PegHit` (`scored` = sai) | `Plinko.PegHitUnscored` | None | WorldX | 3 | None |
| `JackpotHit` | `Plinko.JackpotHit` | None | WorldX | 1 | None |
| `CoinResolved` (`reason` = Bucket) | `Plinko.BucketX1` / `X2` / `X5` theo nhãn hũ (R5) | None | WorldX | 1 | Bucket |
| `CoinResolved` (`reason` = Stuck / Overflow) | — | — | — | — | — |
| `CoinStuck` | `Plinko.CoinFizzle` | None | WorldX | 3 | None |
| `BoardBusy` | `Plinko.BoardBusy` | None | WorldX | 3 | None |
| `SpendRejected` | `Ui.Rejected` (provisional, Audio chưa có cue này; Feedback là chủ duy nhất của cue) | None | None | 2 | None |
| `RevealStage Charge` | `Gacha.Crank` | None | None | 2 | None |
| `RevealStage Burst` | `Gacha.EggPop` rồi `Gacha.RarityReveal.<rarity>` (cue lấy từ `RevealProfile`) | None | None | 2 | None |
| `ComboTierChanged` (`newCount` > `previousCount`) | `Shelf.ComboTierUp` (provisional, Audio chưa có cue này) | None | None | 2 | None |
| `ShelfSlotUnlocked` | `Shelf.SlotUnlock` (provisional, Audio chưa có cue này; VS) | None | None | 2 | None |

`ShelfChanged` không có dòng trong bảng (Feedback không trình diễn đặt/gỡ thú; Shelf tự lo). Cue `Ui.Tap` / `Ui.Confirm` không thuộc bảng này: HUD gọi trực tiếp (HUD H3, H7); `Ui.SoundOn` do Audio tự phát sau khi bật lại tiếng (Audio). Feedback không phát lại các cue này (tránh phát đôi).

Validator (Edit time): mọi `cueId` phải tồn tại trong kho `SoundCue` của Audio; mọi `eventType` phải thuộc danh sách sự kiện đã công bố; hai dòng không được trùng (`eventType`, `condition`). Cue Gacha, `Ui.Rejected`, `Shelf.ComboTierUp` và `Shelf.SlotUnlock` là *provisional* theo bảng cue của Audio.

**R5. Nhãn hũ → cue.** `FeedbackConfig.bucketCueTier[i]` ∈ {X1, X2, X5} ánh xạ `bucketIndex` → cue theo **nhãn**, không theo `multiplier` (Audio, Plinko): đổi `PlinkoConfig.bucketMultipliers` không đổi ánh xạ. Mặc định theo bố cục `[x5][x2][x1][x2][x5]`. Validator: độ dài mảng = số hũ của `PlinkoConfig`; mọi hũ `bucket_is_jackpot` phải là X5.

**R6. Số bay tiền xu (hũ).** Nghe `IncomeCredited` có `bucketIndex` ≠ none (không nghe `CoinResolved` để lấy số). Quy tắc gộp: lần ghi đầu của một `bucketIndex` mở cửa sổ `floatingTextMergeWindow`; mọi khoản cùng hũ trong cửa sổ cộng vào cùng một số bay (cập nhật tại chỗ, đồng hồ sống của số được đặt lại); hết cửa sổ, khoản kế mở số mới. Số hiển thị là `creditedAmount` (sau modifier) định dạng theo Economy R13 — Feedback không tự định dạng. Màu: ink; mật ong nếu có ít nhất một khoản trong cửa sổ có `creditedAmount > baseAmount` (boost). Pool `feedback.floatingText`: `client_declared_max` = `floatingTextMaxOnScreen`, headroom 2, `RecycleOldest`; số bị tái chế biến mất ngay (hook `Recycled`, không mờ dần). Punch hũ và cue hũ chạy ở `CoinResolved` (`reason` = Bucket). Cue hũ có **cửa sổ riêng** theo `bucketIndex`, độ dài `floatingTextMergeWindow`, do chính `CoinResolved` mở (không phụ thuộc `IncomeCredited`, thứ tự cùng khung, hay tab): chỉ phát cue của lần đầu mỗi hũ trong cửa sổ. `IncomeCredited` có `coinId` ≠ none nhưng `bucketIndex` = none (xu Overflow/Stuck được trả tiền): không số bay, không cue; HUD count-up là kênh duy nhất.

**R7. Thu nhập không gắn xu — `PegHitBonus` (FB D2).** `IncomeCredited` có `coinId` = none và `bucketIndex` = none (từ `SubmitIncome`, ví dụ `PegHitBonus`) được **gộp thành một số bay** theo `sourceKey` trong `floatingTextMergeWindow`, dùng cùng pool/trần với R6, và chỉ hiện ở nơi người chơi nhìn thấy nguồn:
- Tab Board đang mở: một số duy nhất gộp mọi `sourceKey` ở **đầu bàn** (`bonusBoardAnchor`, neo bởi Plinko viewport), màu ink/mật ong theo R6.
- Tab Shelf đang mở: một số mỗi `sourceKey` ở vị trí ô thú (`sourceKey = shelf.slot<i>…`, Shelf S3; vị trí qua `IShelfSlotAnchor`) kèm `slotPunchScale` cho ô đó.
- Tab khác (Shop, v.v.): không số bay; HUD count-up đã đủ.
Không hiện số cho từng cú chạm đinh; đinh sáng của chính cú chạm vẫn chạy (R8).

**R8. Hiệu ứng bàn.** Khi `ActiveTab` = Board:
- `CoinSpawned`: pop-in `spawnPopDuration` (0→1, squash); `Manual` thêm một nháy khấc vùng thả; `Auto` pop nhỏ không nháy khấc; `JackpotBurst` dùng biến thể sprite viền mật ong.
- `PegHit` đúng: đinh sáng kem `pegFlashDuration` và punch `pegPunchScale`; `PegHit` sai: không hiệu ứng hình.
- `CoinResolved` vào hũ: punch hũ `bucketPunchScale` trong `bucketPunchDuration`.
- `JackpotHit`: hũ sáng mật ong, giữ `jackpotHoldScale` trong `jackpotHoldDuration`, nhãn "BONUS!" ngắn trên hũ; vòng sáng hạt (VS) tối đa `jackpotParticleMax`.
- `BoardBusy`: khấc vùng thả mờ đi và lắc ngang `busyShakeDistancePx`; không màu coral, không chữ; tối đa một lần mỗi `busyFeedbackMinInterval` (cả hình lẫn cue).
- Vùng thả thường trực: hai khấc gỗ nối đường chấm mờ `dropZoneLineAlpha`. VS: gợi ý Idle sau `idleHintDelay`, xu ma `ghostCoinAlpha`.
Khi `ActiveTab` ≠ Board: bỏ mọi hiệu ứng hình của bàn (không ai thấy); cue chỉ còn `BucketX*` và `JackpotHit` do Audio lọc (Audio A8), Feedback vẫn gọi mọi dòng nhưng không tốn hiệu ứng hình.

**R9. Xu kẹt — hiệu ứng tan (`feedback.coinFizzle`).** Nhận `CoinStuck` → `Get` một đối tượng từ pool trang trí `feedback.coinFizzle` (`client_declared_max` = `fizzleMaxOnScreen`, headroom 0, `RecycleOldest`). Hiệu ứng: bản sao xu mờ co lại tại `worldPosition` trong `fizzleDuration`, bung `fizzleDotCount` chấm kem, số nhỏ màu ink độ mờ `fizzleTextAlpha`; xong → `Release`. Xu thật đã về pool `plinko.coin` ở Resolved nên hai pool độc lập. Khi pool cạn, hiệu ứng tan cũ nhất bị tái chế; hook `Recycled` cắt nó ngay (không mờ dần). Tier 3.

**R10. Chi tiêu bị từ chối.** `SpendRejected` → Feedback chỉ phát cue `Ui.Rejected` (chủ duy nhất của cue; không đỏ, không coral, không chữ). Nháy chip Xu thuộc HUD (`HudConfig.spendFlashSeconds`); nút bị từ chối tự lắc theo cấu hình của chủ nút (HUD/Gacha/Shop). Feedback không giữ hình ảnh hay knob nào cho việc này, không tốn ngân sách hiệu ứng.

**R11. Bóc trứng — `RevealProfile` (FB D3).**
- **Hợp đồng (phương án A, decision-log 2026-10-21): Feedback là chủ duy nhất; Gacha phụ thuộc Feedback một chiều.** Gacha gọi `RequestReveal(petId, rarity, canSkip)` **sau** khi `GachaRolled` đã phát (kết quả đã ghi, hình ảnh không ảnh hưởng gì). Feedback trả `RevealHandle` và phát hai sự kiện: `RevealSettleStarted { handle }` đúng một lần khi vào Settling (kể cả sau tua; Gacha bật thẻ tại đây), rồi `RevealFinished { handle, petId, rarity, skipped }` đúng một lần. Ở `Failed`/bận: `RevealFinished(skipped = true)` phát đồng bộ bên trong `RequestReveal`, không có `RevealSettleStarted`. Gacha giữ luồng (modal chặn chạm bàn, khóa tab, hiện thẻ thú, nút tua từ lượt 2 — Gacha D5); Feedback không chặn input, không nghe sự kiện Gacha nào và không phụ thuộc Gacha.
- Feedback sở hữu **sân khấu**: quả trứng, ánh sáng phẩm cấp, hạt, cue, haptic, thời gian. Gacha/HUD sở hữu **nội dung thẻ** (tên thú, "MỚI", số bản, bộ đếm pity, nút tua).
- Chuỗi: `Charge` (chung mọi phẩm cấp, `revealChargeSeconds`, cue `Gacha.Crank`) → `Burst` (`RevealProfile.burstSeconds`, cue `Gacha.EggPop` rồi `Gacha.RarityReveal.<rarity>`, ánh sáng `glowColorId`, `particleCount` hạt) → `Settle` (`RevealProfile.settleSeconds`, thẻ hiện) → `Finished`.
- **Chống dark pattern**: pha `Charge` giống hệt nhau cho mọi phẩm cấp (không lộ trước kết quả, không "gần trúng"); không hiện màu/ánh sáng phẩm cấp nào trước `Burst`; không có phẩm cấp nào ở `Charge` hiện màu cao hơn kết quả; tổng thời lượng mỗi dòng ≤ `revealMaxSeconds`.
- Tua (`SkipReveal()` khi `canSkip`): nhảy thẳng tới `Settle` với mọi hiệu ứng còn lại rút gọn về `revealSkipSettleSeconds`; cue `RarityReveal.<rarity>` vẫn phát một lần nếu chưa phát; `RevealFinished.skipped` = true. **Tua không cắt** cue `Gacha.Crank`/`Gacha.EggPop` đang phát (để hết tự nhiên, cue ngắn; không cắt tiếng đột ngột) — Assumed (Open Questions).

**R12. Vòng đời, tab, tạm dừng.**
- `TabChanged {previous, current}` → `Audio.SetBusBackground(Plinko, current ≠ Board)`; đồng thời hủy các hiệu ứng bàn đang chạy khi rời Board (R8). Khởi động ở tab Board nên không cần gọi lúc đầu.
- `Paused` → mọi tween/hạt đứng yên, cửa sổ gộp đang mở được **xả ngay** (số bay hiện tổng hiện có rồi dừng); `Resumed` → tiếp tục. Reveal đang chạy đứng yên rồi tiếp tục (Gacha vẫn đang chờ).
- Bước khởi động `feedback.init` (pha `GameplayInit`, sau `pooling.service` và `audio.service`, **không cốt lõi**): đăng ký hai pool, nạp ba asset, dựng pool. Lỗi → `Failed`.

**R13. Ngân sách 3 bậc (FB D6).** Mỗi hiệu ứng hình đang chạy (peg flash, punch hũ, số bay, fizzle, sân khấu reveal, hạt) tính 1 "hiệu ứng hoạt động". Bậc: **1** tiền/jackpot (số bay tiền, punch hũ, `JackpotHit`), **2** sân khấu reveal, **3** đinh/âm lặt vặt (peg flash, spawn pop, fizzle, busy). Luật nhận theo `effect_admit` (Formulas): bậc 1 và 2 **không bao giờ bị từ chối**; chỉ bậc 3 bị bỏ im lặng khi `active ≥ B − effectReserveTier3`. Khi `active` = B, bậc 1/2 lấy chỗ bằng cách cắt hiệu ứng **bậc 3 cũ nhất**, nếu không còn thì số bay bậc 1 cũ nhất; sân khấu reveal không bao giờ bị cắt nên `RevealFinished` luôn phát (reveal thiếu hạt vẫn chạy, R11). Hạt dùng thêm ngân sách riêng `particleBudgetTotal` (`particle_grant`). Số bay còn bị trần pool riêng (`RecycleOldest`).

**R14. Haptic nhẹ + giảm chuyển động (FB D5).**
- Haptic (Android/iOS): `hapticTapMs` khi `CoinSpawned` `origin` = Manual nhận được; `hapticJackpotMs` khi `JackpotHit`; `RevealProfile.hapticMs` ở `Burst`. WebGL: không làm gì. Cách nhau tối thiểu `hapticMinInterval`; tắt hoàn toàn khi `ReduceMotion`.
- `FeedbackSettings.ReduceMotion` (bool toàn cục, mặc định `false`) đặt hệ số `motion_scale` k = `reduceMotionIntensity` khi bật, 1 khi tắt; áp lên punch scale, quãng đường tween/lắc, độ cao số bay, số hạt (Formulas). Thời lượng và âm thanh không đổi. Settings & Accessibility (Alpha) nối UI và lưu; MVP không lưu giữa các phiên (Assumed).

### States and Transitions

Hệ thống:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Uninitialized | Trước `feedback.init` | `Run()` xong → Active; lỗi → Failed | Bỏ mọi sự kiện |
| Active | `Run()` thành công; hoặc `Resumed` | `Paused` → Suspended | Nhận mọi sự kiện, chạy hiệu ứng, gọi Audio |
| Suspended | `Paused` | `Resumed` → Active | Hiệu ứng và reveal đứng yên; bỏ sự kiện mới; không gọi Audio |
| Failed | `Run()` lỗi / pool không đăng ký được | Không thoát trong phiên | Không hiệu ứng/cue; `RequestReveal` trả `RevealFinished(skipped = true)` ngay trong cùng lời gọi; số bay không hiện |

Một lần bóc trứng (`RevealHandle`):

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Charging | `RequestReveal` được nhận | Hết `revealChargeSeconds` → Bursting; `SkipReveal` → Settling | Trứng rung nhẹ, cue `Gacha.Crank`; không màu phẩm cấp |
| Bursting | Hết Charge | Hết `burstSeconds` → Settling; `SkipReveal` → Settling | Cue `EggPop` + `RarityReveal`, ánh sáng, hạt, haptic |
| Settling | Hết Burst, hoặc tua | Hết `settleSeconds` (hoặc `revealSkipSettleSeconds` khi tua) → Finished | Phát `RevealSettleStarted { handle }` khi vào; Gacha bật thẻ khi nhận sự kiện này; ánh sáng lắng |
| Finished | Hết Settle | — | Phát `RevealFinished` đúng một lần, trả hạt/đối tượng |

### Interactions with Other Systems

| Hệ | Feedback nhận | Feedback gọi/phát | Ghi chú |
|---|---|---|---|
| Plinko Board | `CoinSpawned`, `PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy` (R12 của Plinko) | — | Feedback nghe, không gọi Plinko |
| Economy Core | `IncomeCredited { channel, coinId, bucketIndex, origin, reason, sourceKey, baseAmount, creditedAmount }`, `SpendRejected { currencyId, amount, reason }` | — | Số bay lấy từ `creditedAmount` |
| Audio | `PlayResult` | `Play(cueId, {pitchPosition, panPosition})`, `SetBusBackground(Plinko, bool)` | Audio sở hữu SoundCue; Feedback sở hữu bảng R4 |
| Object Pooling | Phiếu mượn, hook `Recycled` | Đăng ký/Get/Release `feedback.floatingText`, `feedback.coinFizzle` | `client_declared_max` đọc từ `FeedbackConfig` |
| Bootstrap | `TabChanged`, `Paused/Resumed`, bước khởi động | `IBootStep feedback.init` | `TabChanged` còn provisional |
| Gacha Machine | `RequestReveal(petId, rarity, canSkip)` → `RevealHandle`, `SkipReveal()` (lời gọi vào API của Feedback) | `RevealSettleStarted { handle }`, `RevealFinished { handle, petId, rarity, skipped }` | Gacha phụ thuộc Feedback một chiều; Feedback không nghe sự kiện Gacha |
| Shelf & Synergy | `ShelfChanged`, `ComboTierChanged { group, previousCount, newCount, bonus }`, `ShelfSlotUnlocked { newSlotCount }`, `IShelfSlotAnchor.Get(sourceKey)` | Punch ô thú; cue combo/mở ô (R4) | Feedback chỉ dùng `ComboTierChanged`, `ShelfSlotUnlocked` và anchor |
| HUD & Tab Navigation | Neo số Xu (nếu cần) | — | HUD sở hữu count-up, nháy chip Xu khi `SpendRejected`, cue `Ui.Tap/Confirm`; Audio sở hữu `Ui.SoundOn`; Feedback chỉ phát `Ui.Rejected` |
| Pet Catalog | `rarity` của thú (soft) | — | Màu phẩm cấp theo art bible |

## Formulas

### merged_float_total

`merged_float_total(key, t) = Σ creditedAmount_i` với mọi khoản i cùng `key` có `t0 ≤ t_i < t0 + floatingTextMergeWindow`, `t0` = thời điểm khoản đầu của cửa sổ.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| key | enum/string | `bucketIndex` hoặc `sourceKey` | event `IncomeCredited` | khóa gộp |
| creditedAmount_i | double | ≥ 0 | event `IncomeCredited` | khoản thứ i, sau modifier |
| t0 | float (s) | ≥ 0 | calculated | khoản đầu của cửa sổ |
| floatingTextMergeWindow | float (s) | 0.1–0.5 | SO field `FeedbackConfig.floatingTextMergeWindow` | độ dài cửa sổ |

**Output range:** ≥ 0; bằng Σ `creditedAmount` của cửa sổ, không bao giờ nhỏ hơn khoản đầu. Số số bay mở ra mỗi khóa ≤ 1 / `floatingTextMergeWindow` mỗi giây.
**Example:** W = 0.25 s; hũ 2 nhận 2.0 ở t = 0.00, 2.0 ở 0.10, 5.0 ở 0.30 → số 1 = 4.0 (hai khoản đầu), khoản ở 0.30 mở số 2 = 5.0.

### floating_text_pose

`y(t) = floatingTextRisePx × k × clamp(t / floatingTextLifetime, 0, 1)` · `alpha(t) = 1 − clamp((t − f × L) / ((1 − f) × L), 0, 1)` với `f` = `floatingTextFadeStart`, `L` = `floatingTextLifetime`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| t | float (s) | 0–L | calculated | tuổi số bay (đặt lại khi gộp thêm khoản) |
| floatingTextRisePx | float (px) | 24–96 | SO field `FeedbackConfig.floatingTextRisePx` | độ cao bay lên |
| floatingTextLifetime | float (s) | 0.6–1.4 | SO field `FeedbackConfig.floatingTextLifetime` | thời gian sống |
| floatingTextFadeStart | float | 0.3–0.8 | SO field `FeedbackConfig.floatingTextFadeStart` | phần thời gian bắt đầu mờ |
| k | float | 0–1 | calculated (`motion_scale`) | hệ số giảm chuyển động |

**Output range:** y ∈ [0, `floatingTextRisePx`]; alpha ∈ [0, 1]; ở `t ≥ L` số được `Release`.
**Example:** Rise 48, L 0.9, f 0.6, k = 1, t = 0.72 → y = 48 × 0.8 = 38.4 px; alpha = 1 − (0.72 − 0.54)/0.36 = 0.5. Với k = `reduceMotionIntensity` 0.25 → y = 9.6 px.

### motion_scale

`k = ReduceMotion ? reduceMotionIntensity : 1` · `scale_eff = 1 + (S − 1) × k` · `px_eff = px × k` · `count_eff = floor(count × k + 0.5)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| ReduceMotion | bool | — | `FeedbackSettings.ReduceMotion` | cờ toàn cục, mặc định false |
| reduceMotionIntensity | float | 0–0.5 | SO field `FeedbackConfig.reduceMotionIntensity` | hệ số khi bật |
| S | float | ≥ 1 | SO field (`pegPunchScale`, `bucketPunchScale`, `jackpotHoldScale`, `slotPunchScale`) | scale punch cấu hình |
| px | float | ≥ 0 | SO field (`busyShakeDistancePx`, `floatingTextRisePx`) | quãng đường cấu hình |
| count | int | ≥ 0 | SO field (`jackpotParticleMax`, `RevealProfile.particleCount`) | số hạt cấu hình |

**Output range:** `scale_eff` ∈ [1, S]; `px_eff` ∈ [0, px]; `count_eff` ∈ [0, count]. k = 1 là phép đồng nhất; k = 0 cho ra scale 1, quãng đường 0, 0 hạt (chỉ còn đổi độ sáng/độ mờ).
**Example:** `bucketPunchScale` 1.12, k = 0.25 → 1 + 0.12 × 0.25 = 1.03; `busyShakeDistancePx` 4 → 1 px; 28 hạt → floor(28 × 0.25 + 0.5) = floor(7.5) = 7.

### effect_admit

`admit(tier) = (tier ≤ 2) or (active < B − effectReserveTier3)` · nếu `tier ≤ 2` và `active = B` thì cắt một hiệu ứng (bậc 3 cũ nhất, nếu không có thì số bay bậc 1 cũ nhất) trước khi nhận · với `B` = `effectBudgetTotal`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| active | int | 0–B | calculated | số hiệu ứng hình đang chạy |
| tier | int | 1–3 | SO field `FeedbackCueTable` dòng/`RevealProfile` | bậc ưu tiên |
| effectBudgetTotal (B) | int | 12–40 | SO field `FeedbackConfig.effectBudgetTotal` | tổng ngân sách |
| effectReserveTier3 | int | 1 đến B − 1 | SO field `FeedbackConfig.effectReserveTier3` | chỗ dành riêng cho bậc 1–2 trước khi bậc 3 bị từ chối |

**Output range:** `active ≤ B` luôn đúng. Bậc 1–2 luôn nhận (cắt bậc 3 cũ nhất, hoặc số bay bậc 1 cũ nhất, khi `active` = B; sân khấu reveal không bao giờ là nạn nhân). Bậc 3 nhận khi `active < B − effectReserveTier3`.
**Example:** B = 24, reserve3 = 10: active = 13 → bậc 3 nhận (13 < 14); active = 14 → bậc 3 bị bỏ, bậc 2 vẫn nhận; active = 24, đến một số bay tiền hoặc reveal → cắt một hiệu ứng bậc 3 cũ nhất rồi nhận.

### particle_grant

`granted = clamp(floor(requested × k + 0.5), 0, particle_free)` · `particle_free = particleBudgetTotal − Σ hạt đang chạy`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| requested | int | 0–`particleBudgetTotal` | SO field `jackpotParticleMax` hoặc `RevealProfile.particleCount` | số hạt yêu cầu |
| k | float | 0–1 | calculated (`motion_scale`) | hệ số giảm chuyển động |
| particleBudgetTotal | int | 20–80 | SO field `FeedbackConfig.particleBudgetTotal` | trần hạt cùng lúc (WebGL overdraw) |

**Output range:** 0 đến `particle_free`; không bao giờ làm tổng hạt vượt `particleBudgetTotal`. Hạt bậc 1 (jackpot) được cấp trước hạt bậc 2 (reveal) khi cùng khung.
**Example:** budget 40, đang có 12 hạt jackpot, reveal Legendary yêu cầu 28, k = 1 → free = 28 → granted = 28 (tổng 40). Nếu jackpot khác thêm 12 hạt trước → free = 16 → reveal nhận 16.

### cue_params

`pitchPosition(PegHit) = 1 − rowIndex / (pegRows − 1)` · `pitchPosition(JackpotBurstSpawn) = burstIndex / (jackpot_burst_count − 1)` · `panPosition = clamp(worldPosition.x / (pegsPerRow × pegSpacing / 2), −1, 1)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| rowIndex | int | 0 – `pegRows` − 1 | event `PegHit` | tầng đinh |
| pegRows | int | 12 (registry `peg_rows`) | SO field `PlinkoConfig.pegRows` | số tầng; nếu ≤ 1 thì không truyền pitch (Audio dùng `fallbackPitchPosition`) |
| burstIndex | int | 0 – `jackpot_burst_count` − 1 | event `CoinSpawned` | thứ tự trong loạt |
| jackpot_burst_count | int | ≥ 2 (registry) | registry | số xu một loạt; nếu ≤ 1 thì không truyền pitch |
| worldPosition.x | float | ± nửa bề rộng bàn | event payload | tọa độ x của sự kiện |
| pegsPerRow, pegSpacing | int, float | theo `PlinkoConfig` | SO field `PlinkoConfig.*` | bề rộng bàn |

**Output range:** pitch ∈ [0, 1]; pan ∈ [−1, 1]. Công thức do Audio chốt ý nghĩa; Feedback chỉ truyền.
**Example:** `rowIndex` 3, `pegRows` 12 → pitch 1 − 3/11 = 0.727; x = +2.0 với nửa bề rộng 5.0 → pan 0.4; x = −7.0 → −1 (kẹp).

### busy_throttle

`allow_busy = (t − last_busy) ≥ busyFeedbackMinInterval` (chưa từng phát: `allow_busy` = đúng)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| t, last_busy | float (s) | ≥ 0 | calculated | giờ thực không dừng khi `Paused` — dùng giờ tween của Feedback |
| busyFeedbackMinInterval | float (s) | 0.3–1.0 | SO field `FeedbackConfig.busyFeedbackMinInterval` | khoảng tối thiểu |

**Output range:** tối đa một phản hồi bàn bận (hình + cue) mỗi `busyFeedbackMinInterval`; sự kiện bị chặn không đổi `last_busy`.
**Example:** interval 0.5; `BoardBusy` ở t = 1.0 → phát; ở 1.3 → bỏ; ở 1.6 → phát (1.6 − 1.0 ≥ 0.5).

### haptic_allow

`haptic_allow = platformHasHaptic ∧ ¬ReduceMotion ∧ (t − last_haptic ≥ hapticMinInterval)` · thời lượng = `hapticTapMs` | `hapticJackpotMs` | `RevealProfile.hapticMs`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| platformHasHaptic | bool | — | calculated | Android/iOS = đúng, WebGL = sai |
| hapticMinInterval | float (s) | 0.03–0.3 | SO field `FeedbackConfig.hapticMinInterval` | khoảng tối thiểu giữa hai lần rung |
| hapticTapMs, hapticJackpotMs | int (ms) | 0–20, 0–50 | SO field `FeedbackConfig.*` | độ dài rung |

**Output range:** 0 hoặc một xung 0–50 ms; 0 ms nghĩa là không rung.
**Example:** tap Manual ở t = 2.00 (rung 10 ms), tap kế ở 2.05 với `hapticMinInterval` 0.08 → bỏ rung (tiếng/hình vẫn chạy).

### reveal_timeline

`T_total(r) = revealChargeSeconds + burstSeconds(r) + settleSeconds(r)` · `T_skip_remaining = revealSkipSettleSeconds` · ràng buộc `T_total(r) ≤ revealMaxSeconds` ∀ r

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| revealChargeSeconds | float (s) | 0.4–1.2 | SO field `FeedbackConfig.revealChargeSeconds` | pha Charge chung |
| burstSeconds(r) | float (s) | 0.2–2.0 | SO field `RevealProfileTable[r].burstSeconds` | pha Burst theo phẩm cấp r |
| settleSeconds(r) | float (s) | 0.2–1.2 | SO field `RevealProfileTable[r].settleSeconds` | pha Settle |
| revealSkipSettleSeconds | float (s) | 0.1–0.4 | SO field `FeedbackConfig.revealSkipSettleSeconds` | thời gian còn lại sau khi tua |
| revealMaxSeconds | float (s) | 3.0–5.0 | SO field `FeedbackConfig.revealMaxSeconds` | trần cứng (chống kéo dài hồi hộp) |

**Output range:** tăng theo phẩm cấp nhưng ≤ `revealMaxSeconds`; sau tua luôn ≤ `revealSkipSettleSeconds`.
**Example:** Charge 0.8 + Legendary (1.6 + 0.8) = 3.2 s ≤ 4.0; Common 0.8 + 0.4 + 0.4 = 1.6 s; tua ở giữa Charge → còn 0.25 s.

## Edge Cases

- **If `IncomeCredited` đến khi `ActiveTab` ≠ Board và nguồn là hũ**: không số bay (R8); cửa sổ số bay không mở; HUD count-up là kênh duy nhất. Cửa sổ cue hũ vẫn mở từ `CoinResolved` (R6).
- **If `IncomeCredited` có `coinId` ≠ none và `bucketIndex` = none** (Overflow/Stuck): không số bay, không cue (R6).
- **If `bucketIndex` nằm ngoài `bucketCueTier`**: bỏ cue, vẫn punch hũ nếu tồn tại; validator ở Edit time chặn độ dài sai.
- **If hàng chục xu vào hũ trong một khung (Mưa Đồng Xu)**: một số bay mỗi hũ mỗi cửa sổ (R6), cue hũ chỉ phát lần đầu mỗi hũ mỗi cửa sổ; số bay mở ≤ `floatingTextMaxOnScreen` + headroom, cũ nhất bị tái chế.
- **If cùng lúc `IncomeCredited` của hũ và `PegHitBonus` làm đầy pool số bay**: `RecycleOldest` cắt số cũ nhất bất kể loại; tiền không mất (HUD đã cập nhật).
- **If `PegHitBonus` đến ở trần ~140 sự kiện/giây mỗi thú**: gộp theo `sourceKey` nên tối đa một số mỗi cửa sổ; không tạo số theo từng cú chạm.
- **If `IncomeCredited` có `creditedAmount` = 0**: hiện "+0" theo định dạng Economy, màu ink (khớp Economy: hũ hệ số 0 trong cấu hình thử); không bỏ.
- **If `creditedAmount` không hữu hạn hoặc âm** (lỗi nguồn): bỏ số bay, không ném lỗi.
- **If `CoinStuck` đến khi pool `feedback.coinFizzle` cạn**: tái chế hiệu ứng cũ nhất, hook `Recycled` cắt ngay; pool xu `plinko.coin` không bị ảnh hưởng.
- **If `Paused` đến khi cửa sổ gộp đang mở**: xả (R12), số hiện tổng hiện có rồi đứng yên; `Resumed` tiếp tục tuổi sống.
- **If `Resumed` mà tab ≠ Board**: không phát lại hiệu ứng bàn đã bị hủy.
- **If `TabChanged` đến nhiều lần liên tiếp**: mỗi lần gọi `SetBusBackground` với giá trị mới; không gọi lại khi giá trị không đổi.
- **If `RequestReveal` đến khi một reveal khác đang chạy**: từ chối reveal mới, trả `RevealFinished(skipped = true)` ngay cho lời gọi mới và ghi lỗi; Gacha D6 chỉ vặn từng quả nên đây là lỗi lập trình.
- **If `RequestReveal` với `rarity` = None hoặc không có dòng `RevealProfile`**: dùng dòng `common`, ghi lỗi; không treo luồng Gacha.
- **If `SkipReveal` được gọi khi `canSkip` = false hoặc ở trạng thái Settling/Finished**: bỏ qua, không đổi trạng thái.
- **If `Paused` giữa reveal**: reveal đứng yên; `Resumed` tiếp tục; `RevealFinished` chỉ phát một lần sau khi tiếp tục.
- **If hệ ở `Failed`**: `RequestReveal` kết thúc ngay (`skipped` = true) để Gacha không bị khóa; không hiệu ứng, không cue.
- **If `Audio.Play` trả `Dropped` (lý do bất kỳ)**: không thử lại, không đổi hình ảnh.
- **If `ReduceMotion` đổi giữa chừng**: hiệu ứng đang chạy giữ k lúc bắt đầu; hiệu ứng mới dùng k mới.
- **If WebGL không có haptic hoặc pan**: haptic là no-op; `panPosition` vẫn tính, Audio tự đặt pan = 0 (Audio A7).
- **If `particle_free` = 0**: hiệu ứng tiếp tục không hạt; không coi là lỗi.
- **If `rarity` mới (Mythic, v.v.) được thêm vào Pet Catalog nhưng thiếu dòng `RevealProfile`**: validator Edit time báo thiếu; runtime dùng dòng `common` và ghi lỗi (như ca `rarity` = None).

## Dependencies

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Plinko Board | Feedback phụ thuộc Plinko | Hard | In: `CoinSpawned`, `PegHit`, `JackpotHit`, `CoinResolved`, `CoinStuck`, `BoardBusy` (Plinko R12). Out: không. Owner: Plinko (sự kiện); Feedback (trình bày, `FeedbackConfig`, pool). Plinko giữ `PlinkoConfig.pegRows/pegsPerRow/pegSpacing` mà Feedback đọc cho pitch/pan. |
| Economy Core | Feedback phụ thuộc Economy | Hard | In: `IncomeCredited`, `SpendRejected`. Owner: Economy. |
| Audio | Feedback phụ thuộc Audio | Hard (qua `Play`; im lặng vẫn chạy được) | Out: `Play(cueId, {pitchPosition, panPosition})`, `SetBusBackground(Plinko, bool)`. In: `PlayResult`. Owner: Audio (SoundCue, API); Feedback (bảng R4, pitch/pan, gộp tiếng hũ). |
| Object Pooling | Feedback phụ thuộc Pooling | Hard | Out: đăng ký/Get/Release `feedback.floatingText` (`client_declared_max` = `FeedbackConfig.floatingTextMaxOnScreen`), `feedback.coinFizzle` (`client_declared_max` = `FeedbackConfig.fizzleMaxOnScreen`). In: phiếu, hook `Recycled`. Owner: Pooling (cơ chế); Feedback (knob). |
| Bootstrap & Scene Flow | Feedback phụ thuộc Bootstrap | Hard | In: `TabChanged`, `Paused/Resumed`, `IBootStep`. Out: bước `feedback.init` (không cốt lõi). Owner: Bootstrap. `TabChanged` provisional. |
| Gacha Machine | Gacha phụ thuộc Feedback (một chiều; Feedback không phụ thuộc Gacha) | Hard (với Gacha, luồng bóc trứng) | Gacha gọi vào Feedback: `RequestReveal(petId, rarity, canSkip)` → `RevealHandle`, `SkipReveal()`. Feedback phát: `RevealSettleStarted { handle }`, `RevealFinished { handle, petId, rarity, skipped }`. Owner: Feedback (sân khấu, `RevealProfile`, hai sự kiện); Gacha (kết quả, thẻ, giữ luồng, nút tua). |
| Shelf & Synergy | Feedback phụ thuộc Shelf | Soft | In: `ComboTierChanged`, `ShelfSlotUnlocked`, `IShelfSlotAnchor.Get(sourceKey)` (nhận mọi khóa `shelf.slot<i>…`). Out: punch ô, cue R4. Owner: Shelf (sự kiện, vị trí ô, khóa nguồn); Feedback (trình bày). |
| HUD & Tab Navigation | Feedback đọc HUD | Soft; provisional | In: neo chip Xu. Owner: HUD (count-up, nháy chip, lắc nút, cue `Ui.Tap/Confirm`); Audio (`Ui.SoundOn`); Feedback (cue `Ui.Rejected`). |
| Pet Catalog | Feedback đọc (soft) | Soft | In: `rarity` của thú để chọn `RevealProfile`; màu theo art bible. Owner: Pet Catalog. |
| Settings & Accessibility *(Alpha)* | Settings phụ thuộc Feedback | Soft | Out: `FeedbackSettings.ReduceMotion` (Settings đặt/lưu). Owner: Feedback (cờ, k); Settings (UI, lưu). |
| Tutorial & Onboarding *(VS)* | Tutorial phụ thuộc Feedback | Soft | Out: ghi đè `idleHintDelay`, `dropZoneLineAlpha`. Owner: Feedback (giá trị mặc định). |

## Tuning Knobs

Mọi knob là trường của `FeedbackConfig` (ScriptableObject duy nhất) trừ khi ghi khác. Giá trị đã duyệt chuyển từ `PlinkoFeedbackConfig`, giữ nguyên.

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Pop-in xu | `FeedbackConfig.spawnPopDuration` | float (s) | 0.08 | 0.05–0.15 | Xu xuất hiện chậm | Không thấy pop | `tapToSpawnMaxMs` (Plinko) |
| Thời lượng sáng đinh | `FeedbackConfig.pegFlashDuration` | float (s) | 0.08 | 0.05–0.15 | Bàn nhấp nháy loạn | Không thấy chạm | `effectBudgetTotal` |
| Punch scale đinh | `FeedbackConfig.pegPunchScale` | float | 1.1 | 1.0–1.2 | Đinh trông lỏng | Không cảm giác va | `reduceMotionIntensity` |
| Punch scale hũ | `FeedbackConfig.bucketPunchScale` | float | 1.12 | 1.05–1.2 | Hũ nhảy loạn | Không thấy hũ nhận xu | `bucketPunchDuration` |
| Thời lượng punch hũ | `FeedbackConfig.bucketPunchDuration` | float (s) | 0.15 | 0.1–0.25 | Punch chồng nhau | Giật | `bucketPunchScale` |
| Cửa sổ gộp số bay | `FeedbackConfig.floatingTextMergeWindow` | float (s) | 0.25 | 0.1–0.5 | Số cộng dồn trễ | Quá nhiều số, tốn draw | `floatingTextMaxOnScreen` |
| Số bay tối đa trên màn | `FeedbackConfig.floatingTextMaxOnScreen` | int | 10 | 6–16 | Rối, tốn draw call | Mất thông tin | `PoolingConfig.pools[feedback.floatingText]` |
| Độ cao bay của số | `FeedbackConfig.floatingTextRisePx` | float (px) | 48 | 24–96 | Số trôi khỏi hũ | Số dính vào hũ | `floatingTextLifetime` |
| Thời gian sống của số | `FeedbackConfig.floatingTextLifetime` | float (s) | 0.9 | 0.6–1.4 | Số chồng nhau, tốn pool | Số biến mất trước khi đọc kịp | `floatingTextMaxOnScreen` |
| Điểm bắt đầu mờ của số | `FeedbackConfig.floatingTextFadeStart` | float | 0.6 | 0.3–0.8 | Số nháy mất | Số mờ quá sớm | `floatingTextLifetime` |
| Scale "nín thở" jackpot | `FeedbackConfig.jackpotHoldScale` | float | 1.15 | 1.1–1.25 | Lố, kiểu casino | Không thấy khoảnh khắc | `jackpotHoldDuration` |
| Thời lượng "nín thở" | `FeedbackConfig.jackpotHoldDuration` | float (s) | 0.12 | 0.08–0.25 | Cảm giác khựng | Không thấy | `burstSpawnInterval` (Plinko) |
| Hạt tối đa vòng sáng jackpot (VS) | `FeedbackConfig.jackpotParticleMax` | int | 12 | 6–20 | Overdraw additive WebGL | Mờ nhạt | `particleBudgetTotal` |
| Biên độ lắc bàn bận | `FeedbackConfig.busyShakeDistancePx` | float (px) | 4 | 2–8 | Trông như lỗi | Không thấy | `busyFeedbackMinInterval` |
| Khoảng tối thiểu phản hồi bàn bận | `FeedbackConfig.busyFeedbackMinInterval` | float (s) | 0.5 | 0.3–1.0 | Bỏ lỡ phản hồi | Spam âm khi autoclick | `tap_cooldown` (Plinko) |
| Trễ gợi ý Idle (VS) | `FeedbackConfig.idleHintDelay` | float (s) | 5 | 3–10 | Người mới không biết chạm | Gây phiền | Tutorial |
| Độ mờ đường vùng thả | `FeedbackConfig.dropZoneLineAlpha` | float | 0.3 | 0.15–0.5 | Rối mắt | Không thấy vùng thả | Tutorial |
| Độ mờ xu ma (VS) | `FeedbackConfig.ghostCoinAlpha` | float | 0.4 | 0.25–0.6 | Nhầm xu thật | Không thấy | — |
| Số chấm khi xu kẹt | `FeedbackConfig.fizzleDotCount` | int | 3 | 2–6 | Kẹt trông như sự kiện lớn | Không thấy xu biến mất | `fizzleMaxOnScreen` |
| Độ mờ số xu kẹt | `FeedbackConfig.fizzleTextAlpha` | float | 0.6 | 0.4–0.8 | Nhầm thưởng thật | Không đọc được | — |
| Thời lượng tan | `FeedbackConfig.fizzleDuration` | float (s) | 0.5 | 0.3–0.8 | Chiếm pool lâu | Không thấy | `fizzleMaxOnScreen` |
| Số hiệu ứng tan tối đa | `FeedbackConfig.fizzleMaxOnScreen` | int | 4 | 2–8 | Tốn pool, nhiều xu kẹt trông như sự kiện lớn | Hiệu ứng cũ bị tái chế sớm | `PoolingConfig.pools[feedback.coinFizzle]`, `stuckRateMax` |
| Ánh xạ hũ → cue | `FeedbackConfig.bucketCueTier[i]` | enum[] (X1/X2/X5) | X5, X2, X1, X2, X5 | độ dài = số hũ; hũ jackpot = X5 | — | — | `PlinkoConfig.bucketMultipliers` |
| Scale punch ô thú | `FeedbackConfig.slotPunchScale` | float | 1.08 | 1.0–1.15 | Ô nhảy loạn | Không thấy nguồn thu | `bucketPunchDuration` |
| Tổng ngân sách hiệu ứng | `FeedbackConfig.effectBudgetTotal` | int | 24 | 12–40 | Tụt FPS, quá 100 draw call | Hiệu ứng bậc 2–3 hay bị bỏ | `feedbackDrawCallBudget` |
| Chỗ dành riêng bậc 3 | `FeedbackConfig.effectReserveTier3` | int | 10 | 1 đến B − 1 | Đinh/âm lặt bị bỏ quá sớm | Bậc 3 chiếm chỗ bậc 1–2 | `effectBudgetTotal` |
| Ngân sách hạt | `FeedbackConfig.particleBudgetTotal` | int | 40 | 20–80 | Overdraw WebGL mobile | Reveal Legendary nhạt | `jackpotParticleMax`, `RevealProfile.particleCount` |
| Ngân sách draw call | `FeedbackConfig.feedbackDrawCallBudget` | int | 20 | 10–30 | Cạnh tranh `boardDrawCallBudget` (Plinko), vượt 100 | Hiệu ứng bị ép bỏ | `effectBudgetTotal` |
| Rung khi chạm được nhận | `FeedbackConfig.hapticTapMs` | int (ms) | 10 | 0–20 | Khó chịu khi spam | Không cảm nhận | `hapticMinInterval` |
| Rung khi jackpot | `FeedbackConfig.hapticJackpotMs` | int (ms) | 30 | 0–50 | Khó chịu | Không cảm nhận | `hapticMinInterval` |
| Khoảng tối thiểu giữa hai lần rung | `FeedbackConfig.hapticMinInterval` | float (s) | 0.08 | 0.03–0.3 | Mất rung hợp lệ | Rung liên tục | `hapticTapMs` |
| Hệ số khi giảm chuyển động | `FeedbackConfig.reduceMotionIntensity` | float | 0.25 | 0–0.5 | Giảm chưa đủ | Mất phản hồi, hết "juice" | Settings (Alpha) |
| Pha Charge chung | `FeedbackConfig.revealChargeSeconds` | float (s) | 0.8 | 0.4–1.2 | Kéo dài hồi hộp | Mất nhịp | `revealMaxSeconds` |
| Thời gian còn lại sau tua | `FeedbackConfig.revealSkipSettleSeconds` | float (s) | 0.25 | 0.1–0.4 | Tua không nhanh hơn | Giật | Gacha (nút tua) |
| Trần thời lượng reveal | `FeedbackConfig.revealMaxSeconds` | float (s) | 4.0 | 3.0–5.0 | Dark pattern, người chơi bực | Cắt Legendary | `RevealProfileTable` |
| Burst theo phẩm cấp (Thường / Hiếm / Sử Thi / Huyền Thoại) | `RevealProfileTable[r].burstSeconds` | float (s) | 0.4 / 0.7 / 1.1 / 1.6 | 0.2–2.0, không giảm theo phẩm cấp | Quá lâu | Không thấy khác biệt | `revealMaxSeconds` |
| Settle theo phẩm cấp | `RevealProfileTable[r].settleSeconds` | float (s) | 0.4 / 0.5 / 0.6 / 0.8 | 0.2–1.2 | Chờ lâu | Thẻ hiện giật | `revealMaxSeconds` |
| Hạt theo phẩm cấp | `RevealProfileTable[r].particleCount` | int | 0 / 8 / 16 / 28 | 0–40 | Overdraw | Mờ nhạt | `particleBudgetTotal` |
| Haptic theo phẩm cấp | `RevealProfileTable[r].hapticMs` | int (ms) | 0 / 0 / 25 / 40 | 0–60 | Khó chịu | Không cảm nhận | `hapticMinInterval` |
| Cue và màu theo phẩm cấp | `RevealProfileTable[r].cueId / glowColorId` | id | `Gacha.RarityReveal.<rarity>` / màu art bible | cue tồn tại trong Audio | — | — | Audio, Pet Catalog |

Hằng số (không phải knob): 60 FPS mục tiêu, ≤ 100 draw call và ≤ 1024 MB là cam kết của dự án (`game-concept`), dùng làm ngưỡng kiểm thử, không phải giá trị của hệ này.

## Visual/Audio Requirements

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| `CoinSpawned` Manual / Auto / JackpotBurst | Pop-in `spawnPopDuration`; nháy khấc (Manual); sprite viền mật ong (Burst) | `Plinko.CoinSpawn` / `CoinSpawnAuto` / `JackpotBurstSpawn` (chime đi lên) | MVP, tier 3 |
| `PegHit` đúng / sai | Đinh sáng kem + punch / không | `PegHitScored` (nốt theo tầng) / `PegHitUnscored` | MVP, tier 3 |
| `CoinResolved` vào hũ + `IncomeCredited` | Punch hũ; số bay "+n" (ink, mật ong khi boost), gộp theo cửa sổ | `BucketX1/X2/X5` theo nhãn hũ | MVP, tier 1 |
| `JackpotHit` | Hũ sáng mật ong, nín thở, "BONUS!"; vòng hạt (VS) | `JackpotHit` | MVP, tier 1 |
| `PegHitBonus` gộp | Số bay ở đầu bàn (Board) hoặc ở ô thú + punch ô (Shelf) | — | MVP, tier 1 |
| `CoinStuck` | Bản sao xu mờ co lại, chấm kem, số nhỏ mờ | `CoinFizzle` | MVP, tier 3 |
| `BoardBusy` | Khấc lắc, không màu, không chữ | `BoardBusy` (≤ 1 / `busyFeedbackMinInterval`) | MVP, tier 3 |
| `SpendRejected` | Không do Feedback (HUD nháy chip, chủ nút lắc; không đỏ) | `Ui.Rejected` (provisional) | MVP |
| Reveal Charge / Burst / Settle | Trứng rung → ánh sáng phẩm cấp + hạt → thẻ hiện | `Gacha.Crank` → `EggPop` + `RarityReveal.<rarity>` | MVP, tier 2 |
| Haptic | — | — (Android/iOS) | MVP |

Màu ánh sáng phẩm cấp lấy theo palette art bible, không đặt riêng từng thú; màu hũ theo thang ấm (kem/matcha/mật ong) của Plinko. Không dùng đỏ hay coral trong mọi phản hồi từ chối.

## Game Feel

- **Feel reference**: nhịp "tick-tick-clunk" của điểm số trong Balatro (số gộp, đọc được mà không chói) và cảm giác pachinko êm của Peglin. **Anti-reference**: máy slot casino (đèn chớp, tiếng xu tràn, kéo dài hồi hộp để dụ quay tiếp).
- **Input responsiveness**:

| Action | Max input-to-response (ms) | Frames at 60 fps | Notes |
|---|---|---|---|
| Chạm bàn được nhận → pop-in + cue + haptic | 33 | 2 | Gắn với `tapToSpawnMaxMs` của Plinko; Feedback bắt đầu cùng khung `CoinSpawned` |
| Chạm bị từ chối (`BoardBusy`) → lắc | 50 | 3 | Có throttle |
| `SpendRejected` → cue `Ui.Rejected` | 33 | 2 | Cùng khung sự kiện; lắc/nháy thuộc HUD |

- **Animation feel targets**:

| Animation | Startup | Active | Recovery frames | Feel goal |
|---|---|---|---|---|
| Peg flash | 0 | `pegFlashDuration` (~5 khung) | ~2 | Một "tích" nhẹ |
| Bucket punch | 0 | `bucketPunchDuration` (~9 khung) | ~3 | "Clunk" ấm, không nảy loạn |
| Jackpot hold | 0 | `jackpotHoldDuration` (~7 khung) | ~4 | Nín thở rồi thả |
| Reveal Burst Legendary | 0 | 1.6 s | 0.8 s settle | Đỉnh cảm xúc, không lố |

- **Impact moments**: không hit-stop, không rung màn hình, không đổi `timeScale` (tông cozy, D5). Có: punch hũ 150 ms, nín thở jackpot 120 ms, haptic 10/30/25–40 ms (mobile). Tất cả tunable.
- **Weight and responsiveness profile**: mềm, gỗ, ấm; phản hồi tức thì nhưng không bao giờ che mất số tiền. Phản hồi mạnh dần theo giá trị (x1 < x2 < x5 < jackpot < Legendary).
- **Feel acceptance**: xem AC-24 đến AC-27.

## UI Requirements

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Số bay "+n" (hũ) | Trên hũ vừa nhận xu | Một số mỗi hũ mỗi `floatingTextMergeWindow` | Tab Board, `IncomeCredited` có `bucketIndex` |
| Số bay thu nhập thú (gộp) | Đầu bàn (Board) / ô thú (Shelf) | Một số mỗi cửa sổ mỗi nguồn | `IncomeCredited` không gắn xu |
| Nhãn "BONUS!" | Trên hũ jackpot | Mỗi `JackpotHit` | Tab Board |
| Sân khấu bóc trứng | Giữa modal Gacha | Mỗi `RequestReveal` | Gacha đang mở |
| Khấc vùng thả lắc | Vạch thả | ≤ 1 / `busyFeedbackMinInterval` | Tab Board |

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Sự kiện R12, `PlinkoConfig.pegRows/pegsPerRow/pegSpacing`, ma trận hũ | `Docs/design/gdd/plinko-board.md` | R12, R9.1, `PlinkoFeedbackConfig` (nhận về) | Feedback tiêu thụ; nhận chủ knob |
| `IncomeCredited`, `SpendRejected`, định dạng số R13 | `Docs/design/gdd/economy-core.md` | R5.4, R13 | Dữ liệu số bay |
| Bảng cue, `Play`, `SetBusBackground`, `fallbackPitchPosition` | `Docs/design/gdd/audio.md` | A3, A7, A8, bảng cue v1.0, D1 | Feedback gọi, chủ bảng map |
| Pool số bay và hiệu ứng tan | `Docs/design/gdd/object-pooling.md` | `feedback.floatingText`, `feedback.coinFizzle`, hook `Recycled` | Feedback là hệ khách |
| `TabChanged`, `Paused/Resumed`, `IBootStep` | `Docs/design/gdd/bootstrap-and-scene-flow.md` | B9.2, vòng đời, pha `GameplayInit` | Feedback nghe |
| `rarity` | `Docs/design/gdd/pet-catalog.md` | R3 phẩm cấp | Chọn `RevealProfile` |
| Bóc trứng, pity, tua | `Docs/design/gdd/gacha-machine.md` | G8.3, D5, Dependencies hàng Feedback | Gacha gọi Feedback (một chiều) |
| Slot anchor, `sourceKey`, combo, mở ô | `Docs/design/gdd/shelf-and-synergy.md` | S3 khóa nguồn `shelf.slot<i>…`, `IShelfSlotAnchor`, `ComboTierChanged`, `ShelfSlotUnlocked` | Feedback đọc |

## Acceptance Criteria

- `AC-1 [EditMode]` **GIVEN** `FeedbackCueTable` MVP, **WHEN** validator chạy, **THEN** mọi `cueId` tồn tại trong kho `SoundCue` (trừ `Ui.Rejected` được đánh dấu provisional), không có hai dòng trùng (`eventType`, `condition`), và mọi `eventType` thuộc danh sách sự kiện đã công bố.
- `AC-2 [EditMode]` **GIVEN** bảng R4, **WHEN** giả lập từng sự kiện Plinko (`CoinSpawned` ×3 origin, `PegHit` ×2, `JackpotHit`, `CoinResolved` Bucket, `CoinStuck`, `BoardBusy`), **THEN** `Audio.Play` nhận đúng `cueId` của dòng tương ứng; `Rain`, `Stuck`/`Overflow` resolved không gọi `Play`.
- `AC-3 [EditMode]` **GIVEN** `bucketCueTier` = [X5, X2, X1, X2, X5], **WHEN** `PlinkoConfig.bucketMultipliers` đổi giá trị nhưng giữ nhãn, **THEN** ánh xạ hũ → cue không đổi; **WHEN** độ dài mảng ≠ số hũ hoặc hũ `bucket_is_jackpot` không phải X5, **THEN** validator báo lỗi.
- `AC-4 [EditMode]` **GIVEN** `pegRows` = 12, **WHEN** `PegHit` `rowIndex` 3, **THEN** `pitchPosition` = 0.727 (± 0.001); `rowIndex` 0 → 1.0; `rowIndex` 11 → 0.0; `pegRows` = 1 → không truyền pitch.
- `AC-5 [EditMode]` **GIVEN** `jackpot_burst_count` = 5, **WHEN** `CoinSpawned` JackpotBurst `burstIndex` 0…4, **THEN** pitch lần lượt 0, 0.25, 0.5, 0.75, 1.0; `jackpot_burst_count` = 1 → không truyền pitch.
- `AC-6 [EditMode]` **GIVEN** nửa bề rộng bàn 5.0, **WHEN** `worldPosition.x` = +2.0, −7.0 và NaN, **THEN** `panPosition` = 0.4, −1.0 và 0 (không truyền NaN).
- `AC-7 [EditMode]` **GIVEN** `floatingTextMergeWindow` = 0.25 s, **WHEN** hũ 2 nhận cặp `CoinResolved` + `IncomeCredited` (cùng khung, thứ tự bất kỳ) 2.0 ở t = 0, 2.0 ở t = 0.10, 5.0 ở t = 0.30 (kể cả khi `ActiveTab` ≠ Board với phần cue), **THEN** có đúng hai số bay: 4.0 rồi 5.0; cue `BucketX2` phát đúng hai lần (lần đầu mỗi cửa sổ).
- `AC-8 [EditMode]` **GIVEN** hai hũ khác nhau cùng nhận xu trong một cửa sổ, **WHEN** xử lý, **THEN** hai số bay độc lập (không gộp chéo hũ).
- `AC-9 [EditMode]` **GIVEN** `creditedAmount` > `baseAmount` trong ít nhất một khoản của cửa sổ, **WHEN** số bay được tạo, **THEN** số dùng màu mật ong; mọi khoản `creditedAmount` = `baseAmount` → màu ink.
- `AC-10 [EditMode]` **GIVEN** `IncomeCredited` với `coinId` = none và `bucketIndex` = none ở 140 sự kiện/giây trong 1 s, **WHEN** tab Board mở, **THEN** số bay mở ra ≤ ⌈1 / `floatingTextMergeWindow`⌉ + 1 và tổng các số = Σ `creditedAmount`.
- `AC-11 [EditMode]` **GIVEN** `ActiveTab` = Shelf, **WHEN** hai `sourceKey` (`shelf.slot1`, `shelf.slot2`) cùng nhận bonus, **THEN** hai số bay riêng tại hai ô; **GIVEN** tab Shop, **THEN** không số bay nào được tạo.
- `AC-12 [EditMode]` **GIVEN** `floatingTextMaxOnScreen` = 10 và headroom 2, **WHEN** 13 số bay được yêu cầu cùng lúc, **THEN** pool phục vụ 12 đối tượng và số cũ nhất bị tái chế đúng thứ tự mượn; số bị tái chế biến mất ngay trong hook `Recycled`.
- `AC-13 [EditMode]` **GIVEN** `fizzleMaxOnScreen` = 4, headroom 0, **WHEN** 5 `CoinStuck` đến cùng khung, **THEN** có tối đa 4 hiệu ứng tan; hiệu ứng cũ nhất bị cắt ngay; pool `plinko.coin` không đổi.
- `AC-14 [EditMode]` **GIVEN** `busyFeedbackMinInterval` = 0.5, **WHEN** `BoardBusy` ở t = 1.0, 1.3, 1.6, **THEN** phản hồi (hình + `Play`) chạy ở 1.0 và 1.6, bỏ ở 1.3.
- `AC-15 [EditMode]` **GIVEN** tab Board, **WHEN** `TabChanged` current = Shelf, **THEN** gọi `SetBusBackground(Plinko, true)` đúng một lần và hủy hiệu ứng bàn; **WHEN** current = Board, **THEN** `SetBusBackground(Plinko, false)`.
- `AC-16 [EditMode]` **GIVEN** B = 24, reserve3 = 10, **WHEN** active = 13, 14, 20 và 24 với lần lượt bậc 3, bậc 3, bậc 2, bậc 2, **THEN** nhận, từ chối, nhận, nhận bằng cách cắt hiệu ứng bậc 3 cũ nhất (không bao giờ cắt sân khấu reveal); `active` không vượt B và `RevealFinished` vẫn phát đúng một lần khi reveal chạy ở active = 24.
- `AC-32 [EditMode]` **GIVEN** `IncomeCredited` có `coinId` ≠ none và `bucketIndex` = none (Overflow/Stuck), **WHEN** xử lý ở mọi tab, **THEN** không số bay nào được tạo và `Audio.Play` không được gọi.
- `AC-17 [EditMode]` **GIVEN** budget hạt 40 và đang có 12 hạt jackpot, **WHEN** reveal Legendary yêu cầu 28, **THEN** granted = 28; **WHEN** đã có 24 hạt, **THEN** granted = 16; tổng không vượt 40.
- `AC-18 [EditMode]` **GIVEN** `reduceMotionIntensity` = 0.25 và `ReduceMotion` = true, **WHEN** tính `bucketPunchScale` 1.12, `busyShakeDistancePx` 4, 28 hạt, **THEN** 1.03, 1 px, 7 hạt; `ReduceMotion` = false → 1.12, 4 px, 28.
- `AC-19 [EditMode]` **GIVEN** `ReduceMotion` = true, **WHEN** `hapticTapMs`, `JackpotHit` và reveal Epic kích hoạt haptic, **THEN** không có lời gọi haptic nào; `ReduceMotion` = false trên Android → ba lời gọi đúng độ dài; WebGL → không lời gọi.
- `AC-20 [EditMode]` **GIVEN** `hapticMinInterval` = 0.08, **WHEN** hai tap Manual cách nhau 0.05 s, **THEN** chỉ tap đầu rung.
- `AC-21 [EditMode]` **GIVEN** `RevealProfileTable` mặc định, **WHEN** validator chạy, **THEN** mọi `revealChargeSeconds + burstSeconds + settleSeconds` ≤ `revealMaxSeconds` (Legendary = 3.2 ≤ 4.0), thời lượng không giảm theo phẩm cấp, và pha Charge là một giá trị chung (không có trường theo phẩm cấp).
- `AC-22 [EditMode]` **GIVEN** `RequestReveal(petId, legendary, canSkip = true)`, **WHEN** `SkipReveal()` giữa Charge, **THEN** trạng thái chuyển Settling, cue `RarityReveal.legendary` phát đúng một lần, `RevealSettleStarted { handle }` phát đúng một lần, `RevealFinished { handle, skipped = true }` phát sau ≤ `revealSkipSettleSeconds` và đúng một lần.
- `AC-23 [EditMode]` **GIVEN** `canSkip` = false hoặc reveal ở Settling, **WHEN** `SkipReveal()`, **THEN** không đổi trạng thái; **GIVEN** hệ ở Failed, **WHEN** `RequestReveal`, **THEN** `RevealFinished(skipped = true)` phát trong cùng lời gọi; **GIVEN** reveal đang chạy, **WHEN** `RequestReveal` thứ hai, **THEN** lời gọi mới kết thúc ngay và reveal đầu không đổi.
- `AC-24 [Play mode]` **GIVEN** `Paused` giữa reveal Epic ở t = 1.0 s, **WHEN** `Resumed` sau 10 s, **THEN** reveal tiếp tục từ t = 1.0 s (không nhảy phía trước) và `RevealFinished` phát một lần.
- `AC-25 [Play mode]` **GIVEN** cửa sổ gộp đang mở và `Paused`, **WHEN** `Paused` đến, **THEN** số bay hiện tổng hiện có và đứng yên; sau `Resumed` hết tuổi sống thì được `Release`.
- `AC-26 [Screenshot]` **GIVEN** reveal ở Charge của hai phẩm cấp khác nhau (Common, Legendary), **WHEN** chụp ảnh ở 0.4 s, **THEN** hai ảnh giống hệt nhau về màu và ánh sáng (không lộ phẩm cấp trước `Burst`); ở `Burst` Legendary thấy ánh sáng/hạt rõ hơn Common.
- `AC-27 [EditMode]` **GIVEN** `SpendRejected`, **WHEN** Feedback xử lý (kể cả khi `active` = B), **THEN** `Audio.Play("Ui.Rejected")` được gọi đúng một lần, không tạo hiệu ứng hình nào và không tốn ngân sách.
- `AC-28 [Screenshot]` **GIVEN** `JackpotHit`, **WHEN** chụp ở đỉnh nín thở, **THEN** hũ mật ong ở scale `jackpotHoldScale` (± 0.02), nhãn "BONUS!" nằm trên hũ, và số tiền của hũ vẫn đọc được.
- `AC-29 [Perf]` **GIVEN** cảnh bão hòa (24 xu rơi, 12+ `PegHit`/s mỗi thú ở trần, jackpot đang chạy, 4 hiệu ứng tan, 12 số bay), **WHEN** đo 60 s trên WebGL, **THEN** FPS ≥ 60, tổng draw call ≤ 100, phần của Feedback ≤ `feedbackDrawCallBudget`, `active` ≤ `effectBudgetTotal`, không cấp phát GC trong `Update` của Feedback.
- `AC-30 [Perf]` **GIVEN** reveal Legendary chồng lên bàn Plinko đang có 24 xu, **WHEN** đo trong cả `Burst`, **THEN** draw call ≤ 100, FPS ≥ 60, tổng hạt ≤ `particleBudgetTotal`, bộ nhớ ≤ 1024 MB.
- `AC-31 [Play mode]` **GIVEN** tap Manual được nhận, **WHEN** đo từ sự kiện `CoinSpawned` tới khung đầu tiên có pop-in và gọi `Play`, **THEN** ≤ 2 khung (33 ms) ở 60 FPS.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| Đã chốt (decision-log 2026-10-21, phương án A): Feedback là chủ duy nhất của sự kiện bóc trứng (`RevealSettleStarted`, `RevealFinished`); Gacha gọi `RequestReveal`, bỏ `GachaReveal*`; Gacha phụ thuộc Feedback một chiều (R11). Sync: đồng bộ systems-index (Feedback không còn Depends On Gacha). | — | Đóng |
| Assumed: `SkipReveal` không cắt cue `Gacha.Crank`/`Gacha.EggPop` đang phát, chỉ phát `RarityReveal.<rarity>` ngay (cue ngắn, cắt sẽ gây tiếng gãy). Đóng Open Question cue Gacha của Audio (Sync). | Audio GDD (Sync) | 2026-10-28 |
| Assumed: Feedback trình diễn `ComboTierChanged` (tăng bậc) và `ShelfSlotUnlocked` bằng cue provisional `Shelf.ComboTierUp` / `Shelf.SlotUnlock` (bậc 2); Audio phải thêm hai cue này; hiệu ứng hình chi tiết theo Visual/Audio của Shelf. Không trình diễn `ShelfChanged`. | Audio GDD (Sync) | 2026-11-04 |
| Assumed: cue từ chối chi tiêu là `Ui.Rejected` mới trên bus Ui (không dùng `Plinko.BoardBusy`) vì bus Plinko bị lọc nền khi người chơi ở tab Gacha/Shop. Audio phải thêm cue này (bus Ui, priority ≈ 40, `keepWhenBusBackground` không áp dụng). | Audio GDD (Sync) | 2026-11-04 |
| Assumed: số bay thu nhập thú ở đầu bàn gộp mọi `sourceKey` thành một số; ở tab Shelf mới tách theo ô (đọc rẻ hơn, ít draw call). | game-designer | 2026-11-04 |
| Assumed: `ReduceMotion` không lưu giữa các phiên ở MVP (Settings & Accessibility Alpha lưu và nối UI); mặc định tắt. | Settings GDD | Alpha |
| Assumed: không rung màn hình trong MVP; chỉ haptic nhẹ (FB D5). | game-designer | — |
| Assumed: giá trị mặc định mới (`effectBudgetTotal` 24, reserve 4/10, `particleBudgetTotal` 40, `feedbackDrawCallBudget` 20, `RevealProfile` thời lượng/hạt, `floatingText*` mới, `haptic*`) là đề xuất chưa profile; chốt sau [Perf] AC-29, AC-30 trên WebGL và thiết bị yếu. | game-designer + Perf | 2026-11-18 |
| Assumed: màu ánh sáng phẩm cấp (`glowColorId`) lấy từ art bible, không ghi mã màu ở đây; art-director xác nhận. | art-director | 2026-11-04 |
| Assumed: nội dung thẻ thú (tên, "MỚI", số bản, pity) thuộc Gacha/HUD; Feedback chỉ dựng sân khấu. | Gacha GDD | 2026-11-04 |
| Assumed: `IShelfSlotAnchor` và neo số Xu là giao diện provisional chờ HUD/Shelf GDD xác nhận. Chủ hình ảnh từ chối: HUD (nháy chip) + chủ nút (lắc); Feedback chỉ cue `Ui.Rejected`. | HUD/Shelf GDD | 2026-11-04 |
| Cần ADR: thư viện tween (DOTween hay tự viết), cơ chế sự kiện, đếm hiệu ứng hoạt động theo `effect_admit`, cách dùng ParticleSystem chung trên WebGL. | technical-director | 2026-11-11 |
| Pan trên WebGL chưa xác minh (Audio ADR); Feedback vẫn tính `panPosition`. | Audio ADR | 2026-11-11 |

**Ghi chú cho Sync (indexNotes)** — chỗ phải đổi tham chiếu khi hệ này được duyệt:
- **Plinko `plinko-board.md`**: thay `PlinkoFeedbackConfig` → `FeedbackConfig` (Dependencies hàng Feedback, bảng knob "Phản hồi" mục Tuning Knobs, Visual/Audio mục "Mọi thời lượng…", Cross-References hàng Phản hồi); bảng knob phản hồi rút gọn thành con trỏ tới GDD này; `floating_text_max_on_screen`, `fizzle_max_on_screen`, `floating_text_merge_window`, `busy_feedback_min_interval` đổi `source` sang `feedback-and-juice.md`; AC-35, AC-38 đến AC-41 chuyển sang Feedback hoặc gắn nhãn tích hợp (xem `AC-7`, `AC-12`, `AC-13`, `AC-14`, `AC-29`).
- **Object Pooling `object-pooling.md`**: thay `PlinkoFeedbackConfig.floatingTextMaxOnScreen` / `fizzleMaxOnScreen` → `FeedbackConfig.*`; bỏ nhãn *provisional* của hai pool; `fizzleMaxOnScreen` có giá trị 4.
- **Audio `audio.md`**: dòng `BoardBusy` thay `PlinkoFeedbackConfig.busyFeedbackMinInterval` → `FeedbackConfig.busyFeedbackMinInterval`; thêm cue `Ui.Rejected`; hàng "Feedback & Juice" bỏ "(chưa có GDD)".
- **HUD `hud-and-tab-navigation.md`**: ghi `Ui.Rejected` thuộc Feedback (hàng `SpendRejected` ở bảng Visual/Audio hiện ghi "Economy/Audio"); HUD giữ nháy chip và `Ui.Tap/Confirm/SoundOn`; nút bị từ chối tự lắc bằng cấu hình của chủ nút.
- **Economy `economy-core.md`**: hàng Số bay trỏ tới GDD này.
- **entities.yaml**: thêm `feedback_effect_budget_total` (24), `reveal_max_seconds` (4.0) nếu cần dùng chéo hệ; `systems-index.md`: Feedback & Juice → In Review.
