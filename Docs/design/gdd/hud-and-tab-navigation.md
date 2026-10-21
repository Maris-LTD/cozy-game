# HUD & Tab Navigation

> **Status**: In Review
> **Author**: Workflow gdd-pipeline (batch 2) + quyết định HUD D1–D6
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 1 Kinetic ASMR (số Xu chảy, nav phản hồi nhẹ), Pillar 3 Meaningful Constraints (CPS header chỉ là idle, hai kênh thu nhập không bị gộp)
> **Decisions**: HUD D1–D6 (`Docs/production/session-logs/decision-log.md`, mục Đợt GDD 2), minorChoices của hệ này trong `gdd-brief-2026-10-21-batch2.json`

## Summary

HUD & Tab Navigation là lớp giao diện cố định bao quanh ba tab: **header** (số Xu, CPS idle, chip Boost, nút loa), **bottom nav** 3 nút (Bàn Chơi / Kệ / Tiệm) và **modal host** dùng chung cho mọi popup. HUD chỉ *vẽ* và *gọi*: trạng thái tab, khóa tab, ngăn xếp Back và cổng chặn input thuộc Bootstrap & Scene Flow; số Xu/CPS và định dạng số thuộc Economy Core; tiếng thuộc Audio.

> **Quick reference** — Layer: `Presentation (UI)` · Priority: `MVP` · Key deps: `Bootstrap & Scene Flow (hard), Economy Core (hard), Audio (soft, nút loa), Pet Catalog (soft, thẻ thú), Shelf & Synergy (chưa có hợp đồng chạy; xem Open Questions)`

## Overview

Màn hình chính luôn có ba vùng cố định, không cuộn dọc (concept §Bố cục giao diện): header trên, vùng nội dung giữa (ba tab), bottom nav dưới. HUD sở hữu hai dải cố định và một lớp phủ (modal), còn nội dung từng tab thuộc hệ của tab đó (Plinko + Gacha ở Bàn Chơi, Kệ ở tab 2, Tiệm ở tab 3).

Ranh giới sở hữu (HUD D3 + quyết định người dùng):

| Việc | Chủ | HUD làm gì |
|---|---|---|
| `ActiveTab`, `TabChanged`, `RequestTab`, khóa tab | Bootstrap | Gọi `RequestTab`, tô nút theo `ActiveTab`/`TabChanged`, phản hồi khi `Locked` |
| Ngăn xếp Back (`PushBackHandler`/`PopBackHandler`), cổng chặn input (`AcquireInputBlock`/`Release`) | Bootstrap | Mỗi modal đẩy một trình xử lý Back và giữ một token; HUD không tự cài cơ chế thứ hai |
| Ví Xu, `idle_cps`, `display_format` (R13) | Economy Core | Chỉ nghe sự kiện, gọi bộ định dạng, không tự định dạng |
| Tắt tiếng (`SetUserMuted`, `IsUserMuted`, `MuteChanged`) | Audio | Nút loa gọi API, vẽ icon theo `MuteChanged` |
| Chồng modal, khung popup chuẩn, lớp phủ (scrim) | **HUD** | Sở hữu và là API duy nhất mở popup cho mọi hệ |
| Nội dung popup (kết quả Gacha, thẻ thú, xác nhận…) | Hệ gọi | Cấp khung, nút chuẩn, hoạt cảnh mở/đóng |

Người chơi cảm nhận: số Xu đếm lên mượt, nav bấm là đổi tab ngay (≤ `tabSwitchMaxMs`), nút khóa chỉ rung nhẹ chứ không quát. Không có chấm báo, không popup thúc ép (HUD D4, anti-pillar "không dark pattern").

## Detailed Design

### Core Rules

**H1. Bố cục cố định (HUD D6).** Header và bottom nav có kích thước cố định theo `frame_scale`; mọi phần cao dư trên màn dài (tới 9:21) thuộc vùng nội dung (công thức `hud_layout`). HUD đặt theo `FrameChanged.safeInsetsPx`: header cộng vùng an toàn trên, nav cộng vùng an toàn dưới; tính lại nhiều nhất một lần mỗi `FrameChanged`.

**H2. Bottom nav (HUD D1, D5).**
1. Đúng 3 nút, thứ tự cố định: `Board` (🎰 Bàn Chơi), `Shelf` (🧸 Kệ), `Shop` (🛒 Tiệm). Không có tab thứ tư, không menu con. Prestige "Mở Tiệm Mới" và lối vào Hàng Xóm (v1.1) nằm trong nội dung tab Tiệm.
2. Nút kích hoạt khi **nhả** (release) trong vùng nút, không phải khi nhấn (khớp Bootstrap AC-26). Hai nút nhả cùng khung → chỉ nút đầu tiên được xử lý.
3. Kích hoạt gọi `RequestTab(tab)` đúng một lần và đọc kết quả:
   - `Changed` → phát `Ui.Tap`; việc tô nút do `TabChanged` điều khiển (không tô trước khi nhận sự kiện).
   - `NoChange` (nút của tab đang mở) → không âm, không hoạt cảnh.
   - `Locked` → phản hồi nhẹ H3, không popup, không đổi tab.
4. Nút của `ActiveTab` ở trạng thái Active (tô `navActiveColor`); nút khác Inactive. Hoạt cảnh tô chạy `navHighlightSeconds` **song song**, không chặn việc hiện tab (Bootstrap `tab_switch_ms`).
5. Mọi nút có vùng chạm ≥ `navMinTouchDp` ở cả hai chiều (`hud_layout`).
6. Không chấm báo/huy hiệu trên nav (HUD D4). Nếu sau này cần, phải qua quyết định mới (Open Questions).

**H3. Phản hồi `Locked`** (minorChoices). Nút bị bấm rung ngang theo công thức `locked_shake` trong `lockedShakeSeconds`, kèm `Ui.Tap` một lần. Không popup, không chữ, không màu cảnh báo. Bấm lại trong lúc đang rung → khởi động lại rung nhưng `Ui.Tap` vẫn bị `minInterval` của Audio chặn spam.

**H4. Header — số Xu (HUD D4).**
1. Nguồn: `WalletChanged` của Economy (tối đa một lần mỗi khung, `currencyId` = Xu); các `currencyId` khác bị bỏ qua ở MVP.
2. Tăng → `displayed_coins` đuổi theo `current` bằng `coin_count_up` (count-up mượt). Giảm (chi tiêu: vặn trứng, mua) → `displayed_coins` = `current` ngay trong khung, không đếm xuống (để người chơi thấy giá bị trừ tức thì).
3. Chuỗi hiển thị lấy từ `display_format` của Economy (R13.1) qua bộ đệm ký tự cấp sẵn; chỉ ghi lại text TMP khi chuỗi khác chuỗi đang hiện (không GC mỗi khung, R13.3).
4. `SpendRejected` → chip Xu nháy một lần trong `spendFlashSeconds` (không đỏ, không san hô; Economy §Visual). Nút vừa bấm tự lắc do chủ nút, không do HUD.

**H5. Header — CPS (HUD D2).** Chỉ hiện `IncomeRatesChanged.idleCps` (kênh Idle, gồm mọi tầng modifier) định dạng tốc độ R13.2 kèm "/s". HUD **không** hiện `plinkoRateAvg` và không cộng gộp hai kênh ở header (Pillar 3); chỗ hiện tốc độ Plinko (nếu có) thuộc tab Bàn Chơi, do Plinko/Feedback & Juice quyết định. CPS cập nhật theo nhịp sự kiện của Economy (`ratesPublishInterval`), không nội suy.

**H6. Header — chip Boost (HUD D1).** MVP: chip **ẩn** hoàn toàn (không chỗ trống nhấp nháy: các chip còn lại không dịch chuyển, vị trí chip giữ trong layout nhưng không vẽ). Từ VS (khi Boosts có GDD): chip hiện khi có ít nhất một boost đang chạy, đếm ngược mm:ss mỗi `boostChipUpdateSeconds`; nguồn dữ liệu boost là **provisional** (Economy chưa công bố API/sự kiện "boost đang chạy", xem Open Questions).

**H7. Nút loa (HUD D5).** Ở header, góc phải. Bấm → `SetUserMuted(!IsUserMuted)`; icon (loa có sóng / loa gạch chéo) đổi theo `MuteChanged`, tức trong cùng khung với lời gọi (Audio Visual). Màu icon nâu `#3D312A`, không dùng màu nhấn. Nếu Audio ở trạng thái lỗi (soft dependency), nút vẫn vẽ và bấm được; icon theo `IsUserMuted` cuối cùng đọc được. Phát `Ui.SoundOn` do Audio chịu trách nhiệm (HUD không tự phát khi bật lại).

**H8. Tab Tiệm ở MVP (HUD D1).** Nút Tiệm luôn hiện và bấm được. Vì Shop/Prestige/Boosts chưa tồn tại ở MVP, HUD sở hữu một **màn "Sắp mở"** làm gốc hình ảnh đăng ký cho tab `Shop` với Bootstrap (cozy, không chữ báo lỗi, không tab trống). Khi GDD Shop có nội dung thật, nó thay màn này bằng cách đăng ký gốc hình ảnh của mình.

**H9. Modal host (HUD D3).**
1. HUD cấp API duy nhất `ModalHost.Open(ModalRequest)` → `ModalHandle` hoặc `Rejected(reason)`; `ModalHost.Close(handle)`. `ModalRequest { modalId, contentView, dismissible, primaryActionId?, secondaryActionId?, onClosed }`. Hệ khác không tự vẽ scrim, không tự gọi `PushBackHandler`/`AcquireInputBlock` cho popup.
   - **Kết quả:** mỗi modal mở thành công phát đúng một `ModalClosed { handle, result, actionId? }` (gọi `onClosed` và phát sự kiện), vào khung bắt đầu đóng (trước hoạt cảnh đóng), **sau khi** token và Back handler của modal đó đã nhả (H9.2); vì vậy `Open` gọi từ `onClosed`/`ModalClosed` không bao giờ bị modal đang đóng chiếm chỗ. `result` ∈ `Primary` (`actionId` = `primaryActionId`), `Secondary` (`actionId` = `secondaryActionId`), `Dismissed` (nút X hoặc Back, `actionId` rỗng), `Closed` (hệ gọi tự `Close(handle)`, `actionId` rỗng). `Rejected` không phát `ModalClosed`.
   - **Vòng đời `contentView`:** do hệ gọi tạo trước `Open`; host chỉ gắn vào slot, và **tháo ra (detach), không hủy** khi đóng; hệ gọi tự hủy/tái dùng trong hoặc sau `onClosed`.
   - **Vòng đời `ModalHandle`:** hợp lệ từ `Open` thành công tới `ModalClosed`; sau đó mọi `Close(handle)` bị bỏ qua (Edge Cases). Handle không được tái dùng cho modal khác.
2. Khi mở, host lấy **một** token `AcquireInputBlock(owner = modalId)` và đẩy **một** trình xử lý Back; khi đóng, nhả đúng cặp đó (LIFO). Độ sâu = số modal ở Opening/Open; modal Closing **không tính** vào trần. Độ sâu ≤ `modalStackMax`; vượt trần → `Rejected(StackFull)`, không xếp hàng ngầm (người gọi quyết định thử lại). `Close(handle)` của modal không ở đỉnh đóng dây chuyền: các modal phía trên đóng trước (`result = Closed`, đúng LIFO), rồi tới modal đó.
3. Scrim phủ **toàn khung** (header, nội dung, nav) bằng `scrimAlpha`, chặn raycast UI: khi có modal, không đổi tab, không bấm nút loa (Assumed, Open Questions). Modal trên cùng nhận Back.
4. Back với modal trên cùng: `dismissible` → đóng modal đó (`result = Dismissed`); không `dismissible` (ví dụ mở trứng) → nuốt phím, modal giữ nguyên.
5. Khung popup chuẩn: tiêu đề, vùng nội dung (slot của hệ gọi), tối đa hai nút (chính/phụ → `result` Primary/Secondary, đóng modal) và nút X nếu `dismissible`. Nút chính phát `Ui.Confirm`; nút X/phụ phát `Ui.Tap`. Kích thước nút tuân `navMinTouchDp`.
6. Hoạt cảnh mở `modalOpenSeconds`, đóng `modalCloseSeconds` (công thức `modal_scrim_alpha`). Token và trình xử lý Back được nhả **ngay lúc đóng bắt đầu**; scrim vẫn chặn raycast UI và Back bị nuốt tới hết Closing (không rơi xuống Bootstrap).
7. Thẻ thú (thông tin một thú) là một nội dung chuẩn do HUD cấp (`PetCardView`), đọc tên, phẩm cấp, icon từ Pet Catalog (soft); hệ gọi (Gacha, Shelf) chỉ truyền `petId` (Assumed, Open Questions).

**H10. Khởi tạo.** HUD là bước `GameplayInit` trình bày, chạy sau các bước gameplay (Bootstrap). Nó dựng header/nav ở trạng thái theo `ActiveTab` hiện tại (mặc định `defaultTab` = `Board`, HUD chỉ đọc), đọc giá trị ví và `idleCps` hiện có làm giá trị đầu (không count-up từ 0 lúc khởi động) rồi mới được vẽ khi rèm mở. Không gọi `RequestTab` lúc khởi động.

### States and Transitions

Nút nav (mỗi nút):

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Inactive | Tab của nút ≠ `ActiveTab` | `TabChanged.current` = tab của nút → Active; nhấn xuống → Pressed | Màu `navInactiveColor` |
| Pressed | Nhấn xuống trong vùng nút | Nhả trong vùng → kích hoạt (H2.3) rồi về Inactive/Active; kéo ra ngoài rồi nhả → hủy, không gọi `RequestTab` | Scale nhẹ nhấn xuống |
| Active | Tab của nút = `ActiveTab` | `TabChanged` sang tab khác → Inactive | Màu `navActiveColor`, nhả vào chính nó → `NoChange` |
| LockedShake | Nhận `Locked` | Hết `lockedShakeSeconds` → trạng thái trước đó | Rung theo `locked_shake`; nhấn lại khởi động lại |

Header số Xu: `Idle` (displayed = target) ↔ `CountingUp` (displayed < target, vào khi `WalletChanged.delta > 0`, về Idle khi chạm ngưỡng chốt của `coin_count_up`); `Flash` chồng lên bất kỳ trạng thái khi nhận `SpendRejected`, tồn tại `spendFlashSeconds`.

Modal host:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Closed | Không modal nào | `Open` hợp lệ | Không scrim, không token, không trình xử lý Back |
| Opening | `Open` được nhận (số modal Opening/Open < `modalStackMax`; Closing không tính) | Hết `modalOpenSeconds` | Lấy token + đẩy Back handler ngay; scrim fade-in |
| Open | Hết Opening | `Close`, hoặc Back với `dismissible` | Nhận Back; modal sau chồng lên (Opening) |
| Closing | `Close`/Back | Hết `modalCloseSeconds` → Closed (hoặc Open nếu còn modal dưới) | Token và Back handler đã nhả; scrim fade-out |

### Interactions with Other Systems

| Hệ | HUD nhận | HUD gửi | Ghi chú |
|---|---|---|---|
| Bootstrap & Scene Flow | `ActiveTab`, `TabChanged {previous, current}`, kết quả `RequestTab` (`Changed`/`NoChange`/`Locked`), `FrameChanged {frameRectPx, frameScale, extraHeightRef, safeInsetsPx}` | `RequestTab(tab)`, `AcquireInputBlock`/`Release`, `PushBackHandler`/`PopBackHandler`, bước `GameplayInit` | Bootstrap giữ trạng thái và cơ chế; HUD không giữ bản sao có thể lệch của `ActiveTab` |
| Economy Core | `WalletChanged`, `SpendRejected`, `IncomeRatesChanged {idleCps, plinkoRateAvg}`, `display_format` | — | HUD không tính và không định dạng số tự mình |
| Audio | `MuteChanged`, `IsUserMuted` | `SetUserMuted(bool)`, `Play(Ui.Tap / Ui.Confirm)` | Soft: Audio lỗi thì HUD vẫn chạy |
| Pet Catalog | định nghĩa thú (tên, phẩm cấp, icon) | — | Chỉ cho `PetCardView` (H9.7) |
| Gacha, Shelf, Shop, Tutorial | — | `ModalHost.Open/Close`, `PetCardView`, khung tab | Các hệ tự quyết nội dung popup; Shelf vẽ cả nội dung Tab 2, HUD chỉ cấp khung tab; Tutorial giữ token/khóa tab của chính nó qua Bootstrap |
| Feedback & Juice | — | — | Chỉ đọc neo chip Xu; không gọi `ModalHost` |

## Formulas

### hud_layout

`min_touch_px = navMinTouchDp × dpi_eff / 160`; `nav_h_eff = max(navHeightRef × frame_scale, min_touch_px)`; `nav_total_px = nav_h_eff + safe_bottom_px`; `header_total_px = headerHeightRef × frame_scale + safe_top_px`; `nav_button_w = (frame_w_px − 2 × (navSideMarginRef × frame_scale + max(safe_left_px, safe_right_px))) / 3`; `content_h_px = frame_h_px − header_total_px − nav_total_px`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| navMinTouchDp | float | 44–64 | SO field `HudConfig.navMinTouchDp` | vùng chạm tối thiểu |
| dpi_eff | float | 120–640 | calculated | `Screen.dpi` nếu > 0, ngược lại `HudConfig.fallbackDpi` |
| 160 | float | 160 | constant | dp chuẩn Android (1 dp = dpi/160 px) |
| navHeightRef, headerHeightRef | float (px) | 130–220, 100–220 | SO field `HudConfig.navHeightRef`, `HudConfig.headerHeightRef` | cao tại `referenceHeight` 1920 |
| navSideMarginRef | float (px) | 0–48 | SO field `HudConfig.navSideMarginRef` | lề hai bên nav |
| frame_scale, frame_w_px, frame_h_px | float | 0.5–1.2, >0, >0 | `FrameChanged` | từ Bootstrap |
| safe_*_px | float (px) | 0 – 0.15 × `frame_h_px` | `FrameChanged.safeInsetsPx` | tai thỏ / thanh điều hướng |
| 3 | int | 3 | constant | đúng 3 tab (concept), không thêm tab |

**Output range:** `content_h_px` > 0 mọi khung hợp lệ; nếu `content_h_px` ≤ 0 (khung quá nhỏ) giữ bố cục hợp lệ trước đó và cảnh báo Editor. `nav_button_w` ≥ `min_touch_px` (kiểm tra bằng AC).
**Example:** 1080×2400, `frame_scale` 1.0, dpi 420, safe top 66, safe bottom 34, safe ngang 0: `min_touch_px` = 48 × 420/160 = 126; `nav_h_eff` = max(168, 126) = 168; `nav_total_px` = 202; `header_total_px` = 150 + 66 = 216; `nav_button_w` = (1080 − 2×24)/3 = 344; `content_h_px` = 2400 − 216 − 202 = **1982**. Desktop 607.5×1080 (scale 0.5625), dpi 0 → fallback 160: `min_touch_px` = 48; `nav_h_eff` = max(94.5, 48) = 94.5.

### coin_count_up

`k = 1 − exp(−dt / countUpTau)`; `displayed' = displayed + (target − displayed) × k`; nếu `target − displayed' ≤ max(countUpMinGap, target × countUpSnapRatio)` thì `displayed' = target`. Khi `WalletChanged.delta < 0` thì `displayed' = target`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| dt | float (s) | 0 – `EconomyConfig.maxFrameDelta` | calculated | thời gian khung (kẹp bằng `maxFrameDelta` của Economy để khung giật không nhảy số) |
| countUpTau | float (s) | 0.05–0.4 | SO field `HudConfig.countUpTau` | hằng thời gian đuổi |
| countUpSnapRatio | float | 0.0001–0.01 | SO field `HudConfig.countUpSnapRatio` | ngưỡng chốt theo tỉ lệ |
| countUpMinGap | double (xu) | 0.5–1 | SO field `HudConfig.countUpMinGap` | ngưỡng chốt tối thiểu (số dư nhỏ) |
| target | double | 0 – `walletSafeCap` | `WalletChanged.current` | số dư thật |
| displayed | double | 0 – target | calculated | số đang vẽ |

**Output range:** `displayed` luôn ≤ `target` khi tăng, không bao giờ vượt target hay âm; đạt target sau tối đa `countUpTau × ln(1 / countUpSnapRatio)` giây (≈ 1.04 s với mặc định; lớn hơn chỉ khi target < `countUpMinGap / countUpSnapRatio` thì ngắn hơn). Giá trị cuối mọi lần hiển thị bằng `display_format(target)`.
**Example:** `countUpTau` 0.15, dt = 1/60 s → k = 1 − e^(−0.1111) = 0.1051. displayed 0, target 100: khung 1 → 10.51; khung 2 → 10.51 + 89.49 × 0.1051 = 19.92. Ngưỡng chốt = max(1, 100×0.001) = 1 → chốt sau ≈ 0.15 × ln(100) = 0.69 s. Idle CPS 58: mỗi khung target tăng ≈ 0.97, `displayed` tụt sau target xấp xỉ 58 × 0.15 = 8.7 xu (số không nhảy từng bước lớn).

### locked_shake

`offset_x(t) = lockedShakeAmplitudeRef × frame_scale × sin(2π × lockedShakeFrequencyHz × t) × (1 − t / lockedShakeSeconds)`, với 0 ≤ t ≤ `lockedShakeSeconds`; ngoài khoảng thì 0.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| lockedShakeAmplitudeRef | float (px) | 4–20 | SO field `HudConfig.lockedShakeAmplitudeRef` | biên độ tại khung 1080 |
| lockedShakeFrequencyHz | float | 3–10 | SO field `HudConfig.lockedShakeFrequencyHz` | tần số |
| lockedShakeSeconds | float (s) | 0.1–0.4 | SO field `HudConfig.lockedShakeSeconds` | thời lượng |
| 2π | float | 6.283 | constant | toán học |

**Output range:** |offset_x| ≤ `lockedShakeAmplitudeRef × frame_scale`, về 0 đúng lúc t = `lockedShakeSeconds`.
**Example:** A = 10, f = 6, T = 0.2, scale 1.0, t = 0.05: sin(2π×6×0.05) = sin(1.885) = 0.951; hệ số tắt dần 1 − 0.25 = 0.75 → offset ≈ **7.13 px**; t = 0.2 → **0**.

### modal_scrim_alpha

`modal_scrim_alpha(t) = scrimAlpha × ease_out(clamp(t / duration, 0, 1))`, `ease_out(x) = 1 − (1 − x)²`, `duration` = `modalOpenSeconds` (mở) hoặc `modalCloseSeconds` (đóng, t chạy ngược từ alpha hiện tại).

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| scrimAlpha | float | 0.3–0.7 | SO field `HudConfig.scrimAlpha` | độ đục tối đa |
| modalOpenSeconds / modalCloseSeconds | float (s) | 0.1–0.3 / 0.08–0.25 | SO field `HudConfig.modalOpenSeconds` / `HudConfig.modalCloseSeconds` | thời lượng |

**Output range:** 0 đến `scrimAlpha`, đơn điệu. Hai modal chồng nhau dùng một scrim duy nhất (không cộng alpha).
**Example:** scrimAlpha 0.45, open 0.18 s, t = 0.09 → x = 0.5 → ease = 0.75 → alpha = **0.3375**.

## Edge Cases

- **If hai nút nav nhả trong cùng khung** (hai ngón): chỉ nút đầu tiên gọi `RequestTab`; nút kia bị bỏ.
- **If người chơi nhấn nút rồi kéo ra ngoài và nhả**: hủy, không gọi `RequestTab`, nút về trạng thái trước.
- **If bấm nút của tab đang mở**: `NoChange`; không âm, không hoạt cảnh, không `TabChanged`.
- **If `RequestTab` trả `Locked`** (Gacha mở trứng, Tutorial, đang Booting): rung H3 và `Ui.Tap`; tab và trạng thái nút Active không đổi.
- **If `TabChanged` đến mà HUD chưa nhận `RequestTab` của chính nó** (Tutorial gọi `RequestTab`): HUD vẫn tô nút theo `TabChanged.current`; HUD không phân biệt nguồn.
- **If `FrameChanged` đến liên tục** (kéo cửa sổ trình duyệt): tính lại `hud_layout` một lần mỗi khung có sự kiện; không tạo mảng/chuỗi mới.
- **If `content_h_px` ≤ 0 hoặc `frame_scale` ≤ 0**: bỏ qua sự kiện, giữ bố cục trước, cảnh báo Editor.
- **If `nav_button_w` < `min_touch_px`** (khung rất hẹp): giữ `nav_button_w` tính được, nút không nhỏ hơn `min_touch_px` ở chiều cao; ghi cảnh báo Editor (khung đã chặn bởi `minAspect` nên chỉ gặp ở cấu hình sai).
- **If `WalletChanged` tăng liên tục mỗi khung** (Mưa Đồng Xu, Idle): `target` cập nhật mỗi khung, `displayed` đuổi theo; không bao giờ có hai count-up chạy riêng.
- **If `WalletChanged` giảm rồi tăng trong cùng khung** (vặn trứng rồi nhận xu): xử lý tuần tự theo thứ tự sự kiện; cuối cùng `displayed` ≤ `target` và không âm.
- **If giá trị `current` NaN, âm hoặc vô cực**: coi là 0 (Economy đã bảo đảm hữu hạn, đây là phòng thủ); text hiện "0".
- **If `displayed` và `target` rất lớn** (≥ 1e15, hậu tố `aa`): `coin_count_up` chạy bằng double; chuỗi tối đa 6 ký tự theo R13, không tràn chip.
- **If `IncomeRatesChanged` chưa tới lần nào** (đầu phiên): hiện `idleCps` đọc trực tiếp từ Economy (H10); nếu chưa có, hiện "0/s".
- **If Audio lỗi hoặc `SetUserMuted` không sẵn sàng**: nút loa không crash, icon theo `IsUserMuted` đọc được; bấm không có tác dụng và không báo lỗi.
- **If người chơi bấm nút loa liên tục**: mỗi lần bấm đổi trạng thái đúng một lần (Audio xử lý fade/`Ui.SoundOn`); HUD không đệm hàng đợi.
- **If `ModalHost.Open` khi đã đủ `modalStackMax` modal Opening/Open** (modal Closing không tính; gồm cả `Open` gọi từ `onClosed`): trả `Rejected(StackFull)`, không mở, không lấy token.
- **If hệ gọi `Close` một handle đã đóng hoặc không hợp lệ**: bỏ qua, không lỗi runtime (cảnh báo Editor); không nhả token hai lần.
- **If Back khi modal trên cùng không `dismissible`**: nuốt, không đóng, không đổi tab.
- **If Back khi không có modal**: không phải việc của HUD; Bootstrap xử lý (về `Board` hoặc không làm gì).
- **If `Paused`/`Resumed` khi modal đang mở**: modal và token giữ nguyên; HUD không đóng popup khi tạm dừng.
- **If modal đang mở và `TabChanged` đến** (Tutorial buộc đổi tab): modal giữ nguyên, nút nav chỉ đổi màu; scrim vẫn chặn nhấn nav.
- **If `Open` được gọi trong lúc Booting (rèm chưa mở)**: modal vẫn được xếp (token của rèm đã chặn input); hiện sau rèm. Tutorial/màn chào dùng cơ chế riêng của Bootstrap, không qua đây.
- **If nút Tiệm bấm ở MVP**: `RequestTab(Shop)` → `Changed`, hiện màn "Sắp mở"; đây là hành vi đúng, không phải lỗi.
- **If chip Boost ẩn và Economy báo boost** (VS, provisional): chip hiện ở vị trí đã giữ, không dịch chuyển chip khác.

## Dependencies

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Bootstrap & Scene Flow | HUD phụ thuộc | Hard | In: `ActiveTab`, `TabChanged`, kết quả `RequestTab`, `FrameChanged.safeInsetsPx/frameScale`. Out: `RequestTab`, `AcquireInputBlock`/`Release`, `PushBackHandler`/`PopBackHandler`, bước `GameplayInit`. Owner: Bootstrap (trạng thái, khóa, cổng, Back); HUD (vẽ nav, modal). Chốt hợp đồng provisional ở Bootstrap `Dependencies` hàng HUD. |
| Economy Core | HUD phụ thuộc | Hard | In: `WalletChanged`, `SpendRejected`, `IncomeRatesChanged {idleCps}`, `display_format` (R13), `maxFrameDelta`. Owner: Economy. |
| Audio | HUD phụ thuộc | Soft | Out: `SetUserMuted(bool)`, `Play(Ui.Tap / Ui.Confirm)`. In: `MuteChanged`, `IsUserMuted`. Owner: Audio. |
| Pet Catalog | HUD phụ thuộc | Soft | In: định nghĩa thú cho `PetCardView` (tên, phẩm cấp, icon). Owner: Pet Catalog. |
| Shelf & Synergy | Shelf phụ thuộc HUD | Soft | Shelf dùng `ModalHost`, `PetCardView(petId)` và khung tab; Shelf vẽ toàn bộ nội dung Tab 2. HUD không đọc dữ liệu Shelf (không chấm báo, HUD D4). |
| Gacha Machine | Gacha phụ thuộc HUD | Hard (popup) | In (cho Gacha): `ModalHost.Open(ModalRequest)` → `ModalHandle` / `Rejected(StackFull)`, `Close(handle)`, `ModalClosed`, `PetCardView(petId)`; modal id `gacha.reveal`, `gacha.rates`. Gacha tự quyết khóa tab (qua Bootstrap) và xử lý `Rejected`. |
| Feedback & Juice | Feedback phụ thuộc HUD | Soft | In: vị trí chip Xu trong header (điểm đáp số bay). Không dùng `ModalHost`. |
| Tutorial & Onboarding (VS) | Tutorial phụ thuộc HUD | Soft | In: vị trí nút nav/header để chỉ dẫn, `ModalHost` cho hộp thoại. *Provisional — GDD chưa có.* |
| Shop / Boosts / Prestige / Neighbor Pranks | Phụ thuộc HUD | Soft | In: gốc hình ảnh tab `Shop` thay màn "Sắp mở", `ModalHost`. *Provisional — GDD chưa có.* |

## Tuning Knobs

Mọi số nằm trong ScriptableObject `HudConfig` (một asset), không literal trong code.

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Cao header (tại 1920) | `HudConfig.headerHeightRef` | float (px) | 150 | 100–220 | Lấn vùng nội dung | Chữ/nút loa chật | `hud_layout` |
| Cao nav (tại 1920) | `HudConfig.navHeightRef` | float (px) | 168 | 130–220 | Lấn vùng nội dung | Nút chạm nhỏ | `navMinTouchDp` |
| Lề hai bên nav | `HudConfig.navSideMarginRef` | float (px) | 24 | 0–48 | Nút hẹp | Sát mép, khó bấm | `nav_button_w` |
| Vùng chạm tối thiểu | `HudConfig.navMinTouchDp` | float (dp) | 48 | 44–64 | Nav phình | Bấm trượt | `hud_layout` |
| Dpi dự phòng | `HudConfig.fallbackDpi` | float | 160 | 120–320 | Vùng chạm lớn ở desktop | Vùng chạm nhỏ | `dpi_eff` |
| Hằng thời gian count-up | `HudConfig.countUpTau` | float (s) | 0.15 | 0.05–0.4 | Số tụt lâu sau ví | Gần như nhảy thẳng | `coin_count_up` |
| Ngưỡng chốt tỉ lệ | `HudConfig.countUpSnapRatio` | float | 0.001 | 0.0001–0.01 | Số chốt sớm, nhảy cuối | Đếm kéo dài thừa | `coin_count_up` |
| Ngưỡng chốt tối thiểu | `HudConfig.countUpMinGap` | double (xu) | 1 | 0.5–1 | Số dư nhỏ chốt sớm | Phần lẻ nhấp nháy | `display_format` |
| Thời lượng tô nav | `HudConfig.navHighlightSeconds` | float (s) | 0.12 | 0.05–0.25 | Nút trễ so với tab | Đổi màu cộc | Bootstrap `tabSwitchMaxMs` |
| Biên độ rung Locked | `HudConfig.lockedShakeAmplitudeRef` | float (px) | 10 | 4–20 | Dễ coi là lỗi | Không thấy | `locked_shake` |
| Thời lượng rung Locked | `HudConfig.lockedShakeSeconds` | float (s) | 0.2 | 0.1–0.4 | Rườm | Không kịp nhận ra | `locked_shake` |
| Tần số rung Locked | `HudConfig.lockedShakeFrequencyHz` | float | 6 | 3–10 | Rung gắt | Như trôi | `locked_shake` |
| Thời lượng nháy chip Xu | `HudConfig.spendFlashSeconds` | float (s) | 0.25 | 0.1–0.5 | Gây khó chịu | Không thấy | Economy `SpendRejected` |
| Số modal chồng tối đa | `HudConfig.modalStackMax` | int | 3 | 1–4 | Popup chồng rối | Gacha/Tutorial bị từ chối | `ModalHost` |
| Thời lượng mở modal | `HudConfig.modalOpenSeconds` | float (s) | 0.18 | 0.1–0.3 | Thao tác chậm | Cộc | `modal_scrim_alpha` |
| Thời lượng đóng modal | `HudConfig.modalCloseSeconds` | float (s) | 0.14 | 0.08–0.25 | Chậm | Cộc | `modal_scrim_alpha` |
| Độ đục scrim | `HudConfig.scrimAlpha` | float | 0.45 | 0.3–0.7 | Tối nặng | Modal không nổi | `modal_scrim_alpha` |
| Scale đầu khi mở popup | `HudConfig.modalOpenScaleFrom` | float | 0.95 | 0.85–1 | Bật quá mạnh | Không thấy hoạt cảnh | `modalOpenSeconds` |
| Màu chữ / viền ô Xu | `HudConfig.textColor` / `HudConfig.coinChipBorderColor` | color | `#3D312A` / `#F59E0B` | palette concept | — | — | Visual |
| Màu nháy chip Xu | `HudConfig.spendFlashColor` | color | mật ong sáng (palette concept) | palette concept | — | — | `spendFlashSeconds` |
| Nhịp cập nhật chip Boost | `HudConfig.boostChipUpdateSeconds` | float (s) | 1.0 | 0.5–1 | Đếm ngược cộc | Rebuild text thừa | H6 (VS) |
| Ngân sách draw call HUD | `HudConfig.hudDrawCallBudget` | int | 12 | 8–20 | Lấn 100 draw call toàn game | Khó giữ atlas | [Perf] |
| Màu nav đang chọn | `HudConfig.navActiveColor` | color | `#7EA172` | palette concept | — | — | Visual |
| Màu nav thường | `HudConfig.navInactiveColor` | color | `#A77443` | palette concept | — | — | Visual |
| Nền header/nav | `HudConfig.barBackgroundColor` | color | `#FAF7F2` | palette concept | — | — | Visual |

Hằng số (constant): `160` (dp chuẩn), `3` (số tab, concept), `2π`; chúng không phải knob.

## Visual/Audio Requirements

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| Nav `Changed` | Nút mới tô `navActiveColor` trong `navHighlightSeconds`, nút cũ về thường; tab hiện ngay (Bootstrap) | `Ui.Tap` | MVP |
| Nav `Locked` | Rung `locked_shake`, không đổi màu | `Ui.Tap` (nhẹ; Assumed) | MVP |
| Số Xu tăng | Count-up (`coin_count_up`), chữ nâu `#3D312A`, ô 🪙 viền mật ong `#F59E0B` | Thuộc Plinko/Feedback | MVP |
| `SpendRejected` | Chip Xu nháy sáng một lần trong `spendFlashSeconds`, không đỏ/san hô | Thuộc Economy/Audio ("Tok" bịt) | MVP |
| Bấm nút loa | Icon đổi ngay (loa sóng ↔ gạch chéo), màu nâu | Audio (`Ui.SoundOn` khi bật lại) | MVP |
| Mở/đóng modal | Scrim fade + khung popup scale nhẹ từ `modalOpenScaleFrom` → 1 | Nút chính `Ui.Confirm`; nút phụ/X `Ui.Tap` | MVP |
| Màn "Sắp mở" tab Tiệm | Hình minh họa tiệm đóng cửa dễ thương + chữ "Sắp mở" (cozy, không báo lỗi) | — | MVP |
| Chip Boost hiện | Chip fade-in, màu mật ong, đếm ngược | — | VS |

Palette theo `game-concept.md` (kem `#FAF7F2`, gỗ `#D9A066` / `#A77443`, matcha, san hô, mật ong, chữ nâu). Màu san hô dành cho sự kiện (không dùng cho báo lỗi chi tiêu). Bố cục từng màn đã có trên Penpot; không tách GDD UI riêng cho từng tab.

## Game Feel

- **Feel reference:** nút bấm cozy của Animal Crossing: Pocket Camp (nảy nhẹ, tiếng gỗ mềm, không bao giờ gắt) và số tiền chảy mượt của Cookie Clicker. **Anti-reference:** idle game báo "!" đỏ liên tục lên nav để thúc giục; nút chết im không phản hồi.

| Action | Max input-to-response (ms) | Frames at 60 FPS | Notes |
|---|---|---|---|
| Nhả nút nav → tab mới hiển thị | `SceneFlowConfig.tabSwitchMaxMs` (100) | 6 | Tính từ khung xử lý cú nhả (Bootstrap AC-26); HUD không trì hoãn |
| Nhả nút nav → nút đổi màu | `navHighlightSeconds` bắt đầu cùng khung | 1 | Tô song song, tween ≤ 0.12 s |
| Bấm nút loa → icon đổi | 1 khung | 1 | Theo `MuteChanged` cùng khung |
| Bấm nút bị `Locked` → bắt đầu rung | 1 khung | 1 | |
| Back → modal bắt đầu đóng | 1 khung | 1 | Token nhả ngay |

| Animation | Startup | Active | Recovery | Feel goal |
|---|---|---|---|---|
| Rung Locked | 0 | `lockedShakeSeconds` (0.2 s ≈ 12 khung) | 0 | Lắc đầu nhẹ, không quát |
| Count-up Xu | 0 | tối đa ≈ 1.04 s | 0 | Tiền "chảy", không nhảy cộc |
| Mở/đóng modal | 0 | 0.18 s / 0.14 s | 0 | Bật nhẹ, không chặn thao tác kế tiếp |

Impact moments: HUD không có hit-stop, rung camera hay haptics ở MVP (không áp dụng — các hiệu ứng đó thuộc Feedback & Juice). Hồ sơ trọng lượng: nhẹ, phản hồi tức thì, không hoạt cảnh nào chặn input.

## UI Requirements

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Số Xu (R13) | Header, ô 🪙 bên trái | Mỗi `WalletChanged` (≤ 1 lần/khung) qua count-up | Luôn hiện |
| CPS idle ("/s", R13.2) | Header, ô +CPS cạnh Xu | Mỗi `IncomeRatesChanged` | Luôn hiện |
| Chip Boost + đếm ngược | Header, ô 🔥 | Mỗi `boostChipUpdateSeconds` | Chỉ khi có boost (VS; MVP ẩn) |
| Nút loa | Header, góc phải | Khi `MuteChanged` | Luôn hiện từ MVP |
| Ba nút nav | Dải dưới, cố định | Khi `TabChanged` | Luôn hiện, kể cả tab Tiệm ở MVP |
| Vùng nội dung | Giữa header và nav | Khi `FrameChanged` | Cao = `content_h_px`; tab tự xử lý phần cao dư |
| Màn "Sắp mở" | Vùng nội dung tab Tiệm | Tĩnh | MVP, tới khi Shop có nội dung |
| Modal (scrim + khung) | Phủ toàn khung, trên header/nav | Khi `Open`/`Close` | Khi có modal |

Ràng buộc kỹ thuật (→ ADR cho `game-adr`, không quyết ở đây): tách Canvas tĩnh (khung header/nav) khỏi Canvas động (số Xu, CPS, chip Boost) và Canvas modal để tránh rebuild toàn bộ; thứ tự vẽ: nội dung < header/nav < scrim/modal < rèm Bootstrap; bộ đệm ký tự cấp sẵn cho text số; atlas UI chung.

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| `ActiveTab`, `TabChanged`, `RequestTab`, khóa tab, `FrameChanged`, `tabSwitchMaxMs` | `Docs/design/gdd/bootstrap-and-scene-flow.md` | R tab (Boot D1), B7 cổng chặn, B8 khung, B11 Back, `tab_switch_ms` | HUD gọi/nghe; Bootstrap sở hữu |
| `AcquireInputBlock`, `PushBackHandler` | `Docs/design/gdd/bootstrap-and-scene-flow.md` | B7, B11 | Modal host dùng cơ chế của Bootstrap |
| `WalletChanged`, `SpendRejected`, `IncomeRatesChanged`, `display_format` | `Docs/design/gdd/economy-core.md` | R7, R13, R15, `display_format` | HUD nghe và gọi bộ định dạng |
| `SetUserMuted`, `IsUserMuted`, `MuteChanged`, `Ui.Tap`, `Ui.Confirm`, `Ui.SoundOn` | `Docs/design/gdd/audio.md` | A12 / D6, bảng cue Ui | HUD gọi API Audio |
| Định nghĩa thú cho `PetCardView` | `Docs/design/gdd/pet-catalog.md` | Dữ liệu thú (tên, phẩm cấp, icon) | HUD đọc |
| Bố cục 3 vùng, 3 tab, Prestige/Hàng Xóm trong 3 tab, palette | `Docs/design/gdd/game-concept.md` | Bố cục giao diện, Visual Identity Anchor | Ràng buộc thiết kế |

## Acceptance Criteria

**Bố cục và nav**
- `AC-1 [EditMode]` **GIVEN** `HudConfig` mặc định, **WHEN** tính `hud_layout` cho 1080×2400 (scale 1.0, dpi 420, safe top 66, bottom 34, ngang 0), **THEN** `min_touch_px` = 126, `nav_h_eff` = 168, `nav_button_w` = 344, `header_total_px` = 216, `nav_total_px` = 202, `content_h_px` = 1982 (sai số ≤ 0.5 px).
- `AC-2 [EditMode]` **GIVEN** `HudConfig` mặc định, **WHEN** tính `hud_layout` cho 607.5×1080 (scale 0.5625, dpi 0) và 1080×1920 (scale 1.0, safe 0), **THEN** `dpi_eff` = `fallbackDpi`, `nav_h_eff` = 94.5 và 168; `content_h_px` luôn > 0 và `nav_button_w` ≥ `min_touch_px`.
- `AC-3 [EditMode]` **GIVEN** `navMinTouchDp` ở min (44) và max (64), dpi 640, **WHEN** tính `nav_h_eff`, **THEN** bằng `max(navHeightRef × frame_scale, min_touch_px)` ở cả hai (nút không nhỏ hơn vùng chạm tối thiểu).
- `AC-4 [EditMode]` **GIVEN** `frame_h_px` rất nhỏ làm `content_h_px` ≤ 0 hoặc `frame_scale` ≤ 0, **WHEN** `FrameChanged` đến, **THEN** bố cục trước được giữ, không ngoại lệ, có đúng một cảnh báo Editor.
- `AC-5 [Play mode]` **GIVEN** Running ở `Board`, **WHEN** nhả chạm trên nút Kệ, **THEN** `RequestTab(Shelf)` được gọi đúng một lần, `ActiveTab` = `Shelf` và nút Kệ Active sau `TabChanged`, `tab_switch_ms` ≤ `SceneFlowConfig.tabSwitchMaxMs` (khớp Bootstrap AC-26).
- `AC-6 [Play mode]` **GIVEN** nút Kệ đang Pressed, **WHEN** kéo ra ngoài vùng nút rồi nhả, **THEN** không có `RequestTab`, không `TabChanged`.
- `AC-7 [EditMode]` **GIVEN** hai nút nav nhả cùng khung, **WHEN** xử lý khung đó, **THEN** đúng một lời gọi `RequestTab` (nút đầu tiên).
- `AC-8 [EditMode]` **GIVEN** `ActiveTab` = `Board`, **WHEN** nhả trên nút Bàn Chơi, **THEN** kết quả `NoChange`, không `Ui.Tap`, không hoạt cảnh, không `TabChanged`.
- `AC-9 [EditMode]` **GIVEN** stub Bootstrap có một khóa tab, **WHEN** nhả trên nút Tiệm, **THEN** kết quả `Locked`, `ActiveTab` không đổi, nút ở LockedShake và `Ui.Tap` được gọi đúng 1 lần, không popup mở.
- `AC-10 [EditMode]` **GIVEN** `locked_shake` mặc định (A = 10, f = 6, T = 0.2, scale 1.0), **WHEN** lấy mẫu t = 0.05 và t = 0.2, **THEN** offset_x = 7.13 px (±0.05) và 0; với mọi t, |offset_x| ≤ A × scale.
- `AC-11 [Screenshot]` **GIVEN** HUD ở khung 1080×2400 và 1080×1920, **WHEN** chụp Game view ở `Board`, **THEN** thấy ba nút nav 🎰/🧸/🛒 với nút Bàn Chơi tô matcha, header có ô Xu, ô +CPS, nút loa; không thấy chip Boost và không có chấm báo nào trên nav.
- `AC-12 [Play mode]` **GIVEN** khởi động mới, **WHEN** rèm kéo xong và bấm nút Tiệm, **THEN** hiện màn "Sắp mở", nút Tiệm Active, không lỗi/cảnh báo trong Console.

**Header**
- `AC-13 [EditMode]` **GIVEN** `countUpTau` 0.15, dt = 1/60, displayed 0, target 100, **WHEN** chạy hai khung `coin_count_up`, **THEN** displayed = 10.51 rồi 19.92 (±0.01); chạy tiếp thì displayed = target sau ≤ 0.69 s (±0.02) và không bao giờ vượt target.
- `AC-14 [EditMode]` **GIVEN** target 1e6 và `HudConfig` mặc định, **WHEN** chạy `coin_count_up` ở dt = 1/60 tới khi chốt, **THEN** thời gian ≤ `countUpTau × ln(1 / countUpSnapRatio)` + 1 khung (≈ 1.06 s); với target 0.5 (< `countUpMinGap`) chốt ngay trong khung đầu.
- `AC-15 [EditMode]` **GIVEN** displayed = 500, **WHEN** `WalletChanged` giảm còn 400, **THEN** displayed = 400 ngay trong khung đó (không count-down) và chuỗi hiển thị = `display_format(400)`.
- `AC-16 [EditMode]` **GIVEN** dt lớn hơn `maxFrameDelta` (khung giật), **WHEN** chạy `coin_count_up`, **THEN** dt được kẹp ở `maxFrameDelta`; displayed không nhảy thẳng tới target trong khung đó.
- `AC-17 [EditMode]` **GIVEN** `WalletChanged.current` = NaN, −1, +∞, **WHEN** HUD xử lý, **THEN** target coi là 0, chuỗi "0", không ngoại lệ.
- `AC-18 [EditMode]` **GIVEN** `WalletChanged.current` = 1234.5 và 999999.6, **WHEN** displayed = target, **THEN** chuỗi bằng `display_format` của Economy ("1.23K", "999K"); HUD không có bộ định dạng riêng (cùng instance gọi).
- `AC-19 [EditMode]` **GIVEN** `IncomeRatesChanged {idleCps = 6.58, plinkoRateAvg = 30}`, **WHEN** HUD cập nhật, **THEN** ô CPS = định dạng R13.2 của 6.58 ("6.5/s") và không có phần tử header nào hiện 30 hay tổng 36.58.
- `AC-20 [EditMode]` **GIVEN** `SpendRejected`, **WHEN** HUD xử lý, **THEN** chip Xu ở Flash đúng `spendFlashSeconds` rồi về trạng thái trước; displayed không đổi.
- `AC-21 [EditMode]` **GIVEN** MVP (không boost), **WHEN** HUD dựng, **THEN** chip Boost không hiển thị và vị trí các chip khác bằng bố cục có chip (không dịch chuyển).

**Nút loa**
- `AC-22 [EditMode]` **GIVEN** stub Audio `IsUserMuted` = false, **WHEN** bấm nút loa rồi `MuteChanged(true)` phát cùng khung, **THEN** `SetUserMuted(true)` được gọi đúng 1 lần và icon là "loa gạch chéo" ở khung đó; bấm lần nữa → `SetUserMuted(false)` đúng 1 lần.
- `AC-23 [EditMode]` **GIVEN** stub Audio không phản hồi (Audio lỗi), **WHEN** bấm nút loa, **THEN** không ngoại lệ và icon vẫn theo `IsUserMuted` đọc được.

**Modal host**
- `AC-24 [EditMode]` **GIVEN** stub Bootstrap đếm token và handler, **WHEN** `Open(modalA)` rồi `Open(modalB)`, **THEN** có đúng 2 token và 2 trình xử lý Back, `Rejected` không xảy ra; `Close(B)` rồi `Close(A)` nhả đúng cặp theo LIFO, còn 0 token và 0 handler.
- `AC-25 [EditMode]` **GIVEN** `modalStackMax` = 3 và đã mở 3 modal, **WHEN** `Open` thêm một, **THEN** trả `Rejected(StackFull)` và số token vẫn 3.
- `AC-26 [EditMode]` **GIVEN** modal trên cùng `dismissible`, **WHEN** Bootstrap gọi trình xử lý Back, **THEN** modal đóng (`result = Dismissed`) và token/handler của nó nhả ngay (trước khi hết `modalCloseSeconds`); với modal không `dismissible`, modal giữ nguyên và token không đổi.
- `AC-27 [EditMode]` **GIVEN** đã `Close(handle)`, **WHEN** `Close` cùng handle lần hai và với handle không hợp lệ, **THEN** bỏ qua, số token không đổi, có cảnh báo Editor, không ngoại lệ.
- `AC-35 [EditMode]` **GIVEN** modal có `primaryActionId` = "ok" và `secondaryActionId` = "no", **WHEN** bấm nút chính, **THEN** `ModalClosed` phát đúng 1 lần với `result` = `Primary`, `actionId` = "ok"; bấm nút phụ → `Secondary`, "no".
- `AC-36 [EditMode]` **GIVEN** modal `dismissible`, **WHEN** bấm nút X và (modal khác) Back, **THEN** mỗi modal phát đúng 1 `ModalClosed` với `result` = `Dismissed`, `actionId` rỗng.
- `AC-37 [EditMode]` **GIVEN** modal đang mở, **WHEN** hệ gọi `Close(handle)`, **THEN** `ModalClosed` phát 1 lần với `result` = `Closed`; `Open` bị `Rejected(StackFull)` không phát `ModalClosed`.
- `AC-38 [EditMode]` **GIVEN** `contentView` do hệ gọi tạo, **WHEN** modal đóng ở mọi `result`, **THEN** `contentView` còn tồn tại, đã tháo khỏi slot (không bị host hủy); `Close(handle)` sau `ModalClosed` bị bỏ qua và không phát lại `ModalClosed`.
- `AC-28 [EditMode]` **GIVEN** `scrimAlpha` 0.45, open 0.18 s, **WHEN** t = 0, 0.09, 0.18, 0.3, **THEN** alpha = 0, 0.3375, 0.45, 0.45 (±0.001); hai modal chồng dùng một scrim, alpha ≤ `scrimAlpha`.
- `AC-39 [EditMode]` **GIVEN** modal A (dưới) và B (đỉnh) đang mở, **WHEN** `Close(A)`, **THEN** B đóng trước (`Closed`) rồi A (`Closed`), còn 0 token và 0 handler.
- `AC-41 [EditMode]` **GIVEN** `modalStackMax` modal đang mở, **WHEN** đóng modal trên cùng và trong `onClosed` của nó gọi `Open` modal mới, **THEN** `Open` thành công (không `StackFull`), token/handler đã nhả trước khi `onClosed` chạy, và sau đó số token = `modalStackMax`.
- `AC-40 [Play mode]` **GIVEN** modal đang Closing, **WHEN** chạm nút nav hoặc gửi Back, **THEN** không `RequestTab`, Back không tới Bootstrap.
- `AC-29 [Play mode]` **GIVEN** modal đang mở, **WHEN** chạm nút nav và nút loa, **THEN** không `RequestTab`, không `SetUserMuted` (scrim chặn UI); `Paused`/`Resumed` giữa chừng không đóng modal.
- `AC-30 [Screenshot]` **GIVEN** một popup chuẩn với hai nút, **WHEN** chụp ở 1080×2400, **THEN** scrim phủ toàn khung kể cả header và nav, khung popup ở giữa, nút chính/phụ đều ≥ `navMinTouchDp` ở cả hai chiều.

**Hiệu năng và hợp đồng**
- `AC-31 [Perf]` **GIVEN** build WebGL/thiết bị tham chiếu (`perf_reference_device`), Idle CPS đang chạy và count-up hoạt động liên tục 30 s, **WHEN** đo bằng Profiler, **THEN** HUD cấp phát 0 byte GC mỗi khung ở trạng thái ổn định, số draw call do HUD (header, nav, không modal) ≤ `hudDrawCallBudget`, toàn game ≤ 100 draw call và ≥ 60 FPS.
- `AC-32 [Perf]` **GIVEN** `FrameChanged` phát liên tục 120 khung (kéo cửa sổ), **WHEN** đo, **THEN** HUD tính lại `hud_layout` tối đa một lần mỗi khung và không cấp phát GC mỗi lần.
- `AC-33 [EditMode]` **GIVEN** stub ghi lại mọi lời gọi Bootstrap, **WHEN** khởi động HUD tới khi rèm mở, **THEN** HUD không gọi `RequestTab`, `AcquireTabLock`, và không tạo token nào khi không có modal; nút Active = `ActiveTab` hiện có (`Board`).
- `AC-34 [EditMode]` **GIVEN** `HudConfig`, **WHEN** đặt từng knob ở min và max trong Safe range (Tuning Knobs), **THEN** `hud_layout`, `coin_count_up`, `locked_shake`, `modal_scrim_alpha` đều cho kết quả hữu hạn, không NaN, không ngoại lệ.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| **Assumed:** scrim phủ toàn khung nên nút loa không bấm được khi modal mở (đơn giản, không xung đột Back/token); nếu người chơi web cần tắt tiếng ngay trong popup thì để header nằm trên scrim. | HUD (nhìn lại sau playtest) | 2026-11-18 |
| **Assumed:** khi `Locked`, dùng `Ui.Tap` (cue có sẵn) thay vì cue riêng; có thể yêu cầu Audio thêm `Ui.Locked` nhẹ hơn sau. | Audio / HUD | 2026-11-11 |
| **Assumed:** khi chi tiêu (số Xu giảm) không đếm xuống mà hiện ngay số mới; HUD D4 chỉ nói count-up cho nhận xu. | HUD | 2026-11-18 |
| **Assumed:** HUD cấp `PetCardView` (đọc Pet Catalog) cho popup thẻ thú để Gacha/Shelf không vẽ trùng; nếu Gacha GDD muốn tự dựng màn kết quả thì HUD chỉ giữ khung. | Gacha Machine GDD | 2026-11-11 |
| **Assumed:** `Close` modal không ở đỉnh đóng dây chuyền các modal trên nó (giữ LIFO của Back Bootstrap, không bỏ rơi caller). | HUD | 2026-11-18 |
| **Assumed:** `Open` khi đủ `modalStackMax` trả `Rejected` thay vì xếp hàng; Gacha/Tutorial phải xử lý kết quả này. | Gacha / Tutorial GDD | 2026-11-11 |
| **Assumed:** header không hiện `plinkoRateAvg`; chỗ hiện (nếu có) do Plinko/Feedback & Juice quyết trong tab Bàn Chơi (Economy UI table để HUD quyết). | Feedback & Juice GDD | 2026-11-11 |
| Boost đang chạy lấy dữ liệu từ đâu (sự kiện/API "boost active + thời gian còn lại" của Economy chưa tồn tại) → chip Boost provisional tới khi Boosts có GDD (VS). | Economy Core / Boosts GDD | 2026-11-25 |
| Ranh giới chấm báo (HUD D4: không chấm ở MVP): nếu Shelf/Inventory GDD cần "thú chưa đặt" thì phải có quyết định mới, không tự thêm. | Shelf & Synergy GDD | 2026-11-11 |
| Tab Tiệm chứa Shop + Boosts + Prestige + Hàng Xóm (HUD D5) có nguy cơ quá tải; theo dõi khi các GDD đó có bố cục. | Shop / Prestige GDD | 2026-12-02 |

**Cần Sync cập nhật (không sửa ở đây):** `systems-index.md` hàng 10 — HUD sở hữu modal host, phụ thuộc Shelf & Synergy chỉ là *documentary* (không đọc dữ liệu ở MVP), thêm Gacha/Tutorial/Shop là hệ phụ thuộc vào HUD (HUD không còn là lá hoàn toàn); `bootstrap-and-scene-flow.md` bảng phụ thuộc HUD bỏ "Provisional"; `entities.yaml` thêm (nếu muốn) `HudConfig` knobs (`navMinTouchDp` 48, `modalStackMax` 3, `countUpTau` 0.15) với `source` là tài liệu này.
