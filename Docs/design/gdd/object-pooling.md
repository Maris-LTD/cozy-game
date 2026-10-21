# Object Pooling

> **Status**: Approved
> **Author**: Maris-LTD + Claude (game-designer)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Gián tiếp Pillar 1 — Kinetic ASMR (giữ 60 FPS và 0 B GC để xu, số bay không giật)
> **Consulted**: không spawn specialist (workflow này cấm subagent); Formulas và Acceptance Criteria do game-designer tự viết theo chuẩn engine-programmer / qa-lead
> [CD-GDD-ALIGN] skipped (lean)
> **Revision 1 — 2026-10-21** (khớp GDD Foundation vừa viết): Pooling đăng ký bước `pooling.service` (pha `ServiceInit`) và `pooling.prewarm` (pha `Prewarm`) vào hợp đồng pha của Bootstrap — Pooling phụ thuộc Bootstrap, không ngược lại (C1[15]); headroom xu gắn với cách Plinko trả xu sau Resolved, *provisional* chờ NEEDS-USER, hành vi khi pool trả rỗng theo Plinko R4.5 (C1[16]); Cross-References cập nhật file đã có (C1[17]). Pool D1–D5 giữ nguyên.
> **Revision 2 — 2026-10-21** (fix round 1, review `reviews/object-pooling-review-2026-10-21.md`, mục chặn R3.3/R8): thêm R8.5 — "Thử lại" của Bootstrap (B4.4) nạp lại scene kết thúc phiên Pooling, dịch vụ mới bắt đầu từ Accepting với sổ đăng ký rỗng, không có trạng thái static sống qua scene; cập nhật R3.3, R8.4, bảng trạng thái, hàng Bootstrap, Edge Case và thêm AC-31 – AC-33. Pool D1–D5 giữ nguyên.
> **Revision 3 — 2026-10-21** (fix round 2, mục chặn OP2-B1): viết lại AC-32 — phạm vi reflection là namespace `CozyGachapon.Pooling` thay vì cả assembly runtime, bỏ qua kiểu/field do trình biên dịch sinh (trừ backing field auto-property/event static), danh sách kiểu static bất biến cho phép thêm `bool`, `char`, `ProfilerMarker`; ghi namespace vào R8.5. Pool D1–D5 giữ nguyên.
> **Revision 4 — 2026-10-21** (consistency run 2, C2[3] + C2[13]; quyết định user trong `Docs/production/session-logs/decision-log.md`): đóng NEEDS-USER hiệu ứng tan theo phương án **B** — hiệu ứng tan dùng pool trang trí riêng `feedback.coinFizzle` (RecycleOldest, Feedback & Juice đăng ký), xu về pool ngay khi Resolved (Plinko R9.1, không có `Fizzling`), `pools[plinko.coin].headroom` = 0 (hết *provisional*); bỏ mọi câu nhắc phương án A; viết lại AC-26, thêm AC-34; ghi rõ nguồn `client_declared_max` của các pool trang trí là knob của hệ sở hữu. Pool D1–D5 giữ nguyên.

## Summary

Object Pooling là dịch vụ nền tạo sẵn và tái sử dụng mọi đối tượng hình ảnh xuất hiện rồi biến mất liên tục — xu Plinko, số bay, toast, hiệu ứng trang trí — để game giữ 60 FPS và không sinh rác bộ nhớ trên WebGL. Người chơi không thấy hệ này; họ chỉ cảm nhận được rằng cú chạm đầu, jackpot hay Mưa Đồng Xu không bao giờ giật (Pillar 1). Mỗi hệ khách tự khai báo số đối tượng tối đa, Pooling thêm phần dư, prewarm toàn bộ dưới rèm khởi động và khai báo chính sách khi cạn theo từng pool (xu: Fixed; trang trí: RecycleOldest).

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `Bootstrap & Scene Flow (hợp đồng pha khởi động)`; được dùng bởi Plinko Board, Feedback & Juice

## Overview

Object Pooling là dịch vụ hạ tầng dùng chung, tạo sẵn và tái sử dụng mọi **đối tượng hình ảnh sinh/hủy lặp lại** của game: xu Plinko, số bay "+n", toast và các hiệu ứng trang trí (trứng, kệ) từ các hệ sau này. Mỗi hệ khách đăng ký một pool, khai báo số đối tượng tối đa nó dùng cùng lúc; Pooling cộng thêm phần dư (headroom) do chính nó sở hữu, tạo sẵn toàn bộ trong lúc khởi động (dưới rèm "Tiệm mở cửa"), rồi suốt phiên chỉ cho mượn và nhận lại, không bao giờ tạo mới hay hủy. Khi một pool cạn, hành vi do từng pool khai báo: pool xu là **Fixed** (cạn là lỗi lập trình, trả rỗng kèm cảnh báo trong Editor), pool trang trí là **RecycleOldest** (lấy lại đối tượng mượn lâu nhất để thứ mới nhất luôn hiện).

Người chơi không tương tác với hệ này. Thứ game mất nếu thiếu nó: Instantiate/Destroy liên tục làm rác GC và giật khung trên WebGL ngay đúng lúc jackpot hay Mưa Đồng Xu — phá cảm giác "chạm là có xu" của Pillar 1 và AC-38 của Plinko (0 B GC mỗi khung). Âm thanh nằm ngoài phạm vi: Audio tự quản voice pool với luật ưu tiên riêng (quyết định D2). Hạt (particle) cũng nằm ngoài: dùng ParticleSystem chung với `Emit()` theo ràng buộc hiệu năng của Plinko.

GDD này chỉ mô tả hành vi, dung lượng và tiêu chí đo được; API cụ thể (kiểu handle, cơ chế ẩn đối tượng, có dùng `UnityEngine.Pool` hay không) → **ADR riêng** (quyết định D1, xem Open Questions).

## Detailed Design

### Core Rules

Mọi con số trong các luật dưới đây là knob ở `PoolingConfig` / `PoolingTestTargets` (xem Tuning Knobs) hoặc knob của hệ khách được nêu rõ asset; không có con số nào nằm trong logic. Tên khác trong `backtick` là `id` pool, biến công thức hoặc bộ đếm chẩn đoán.

**R1. Phạm vi khách hàng** (quyết định D2)
1. Pooling phục vụ mọi đối tượng hình ảnh sinh/hủy lặp lại trong một phiên: xu Plinko, số bay, toast, hiệu ứng trang trí của Gacha/Kệ/Feedback.
2. Không thuộc Pooling: voice âm thanh (Audio tự quản); hạt (ParticleSystem dùng chung + `Emit()`); phần tử UI tĩnh tồn tại suốt phiên (header, nav, khung tab).
3. Validator trong Editor cảnh báo khi prefab của một `PoolDefinition` chứa AudioSource (nhắc ranh giới D2), không chặn chạy.

**R2. Định nghĩa pool** — mỗi pool là một mục `PoolDefinition` trong `PoolingConfig` gồm: `id` (chuỗi bất biến, dạng `<hệ>.<đối tượng>`), `prefab`, `policy` (Fixed | RecycleOldest), `headroom`. Dung lượng gốc **không** nằm ở đây: hệ khách khai báo lúc đăng ký từ knob của chính nó (quyết định D4), để khi ADR hiệu năng đổi `PlinkoConfig.maxActiveCoins` thì pool xu tự theo.

**R3. Đăng ký** (quyết định D4)
1. Hệ khách đăng ký với: `id`, `client_declared_max` (số đối tượng tối đa nó giữ cùng lúc, đọc từ asset của nó), cờ `forbid_recycle` (true khi hệ khách cần mỗi đối tượng có đúng một chủ — Plinko R9.1).
2. Pooling tính `pool_capacity` (Formulas) và chấp nhận khi `registration_valid` đúng. Không đúng → đăng ký bị từ chối: không tạo đối tượng nào, ghi lỗi trong Editor, trả kết quả thất bại cho hệ khách; hệ khách báo lỗi qua bước khởi động của chính nó (`IsFailed` của bước `GameplayInit`, Bootstrap B2) và Bootstrap quyết định: bước cốt lõi → bảng "Thử lại".
3. Chỉ được đăng ký khi dịch vụ đang ở **Accepting**: từ khi bước `pooling.service` (pha `ServiceInit`) chạy xong đến trước bước `pooling.prewarm` (pha `Prewarm`). Hệ khách đăng ký pool trong bước `GameplayInit` của chúng (Bootstrap B3). Mỗi `id` được đăng ký đúng một lần mỗi **phiên Pooling** — một phiên bắt đầu ở `Run()` của `pooling.service` và kết thúc khi scene bị dỡ, kể cả khi Bootstrap nạp lại scene sau "Thử lại" (R8.5), nên lần khởi động lại đăng ký lại mọi `id` từ sổ rỗng.

**R4. Prewarm** (quyết định D5)
1. Pooling tự đăng ký hai bước cốt lõi (`IsCritical` = true) vào hợp đồng pha của Bootstrap & Scene Flow (`IBootStep`, Bootstrap B2): `pooling.service` ở pha `ServiceInit` (tạo dịch vụ → Accepting) và `pooling.prewarm` ở pha `Prewarm`. Pha `Prewarm` luôn đứng sau `GameplayInit` — nơi mọi hệ khách MVP đăng ký pool — và trước khi Bootstrap phát `BootCompleted` (Bootstrap B3; rèm "Tiệm mở cửa" che bước này). Bootstrap chỉ biết interface bước, không tham chiếu Pooling. Khi Bootstrap gọi `Run()` của `pooling.prewarm`, dịch vụ đóng đăng ký.
2. Pooling tạo đủ `pool_capacity` đối tượng cho **mọi** pool đã đăng ký, tối đa `prewarmPerFrame` đối tượng mỗi khung, theo thứ tự đăng ký. Số khung cần = `prewarm_frames` (Formulas).
3. Xong toàn bộ → dịch vụ chuyển **Ready** và bước `pooling.prewarm` báo `IsDone` (Bootstrap đọc mỗi khung). Từ đó đến hết phiên Pooling **không bao giờ tạo hay hủy** đối tượng (không Instantiate, không Destroy).

**R5. Mượn (Get)**
1. Chỉ hợp lệ khi dịch vụ Ready. Trả về một đối tượng rảnh của pool kèm một **phiếu mượn** (lease) mang số thứ tự mượn `acquire_seq` tăng dần trong pool.
2. Trước khi trả về: đối tượng được bật (hiển thị và mô phỏng theo mặc định của prefab) và hook *lấy ra* của hệ khách được gọi đúng một lần. Hệ khách đặt vị trí, biến thể (ví dụ sprite viền mật ong của xu `JackpotBurst`) và dữ liệu của nó trong cùng lời gọi, trước khung render kế tiếp.
3. Pool không còn đối tượng rảnh → áp chính sách cạn (R7).

**R6. Trả (Release)**
1. Hệ khách trả bằng phiếu mượn. Đối tượng về trạng thái rảnh: **không hiển thị, không mô phỏng** (không render, không tham gia vật lý), hook *trả về* của hệ khách được gọi đúng một lần với lý do `Released`.
2. Pooling **không** reset dữ liệu riêng của hệ khách (điểm số, tween, trạng thái gameplay); hệ khách tự reset trong hook. Pooling chỉ bảo đảm trạng thái ẩn và không mô phỏng.
3. Trả không hợp lệ bị **bỏ qua**, không ném exception trong build, ghi lỗi trong Editor và tăng bộ đếm chẩn đoán: trả lần hai cùng phiếu (`doubleRelease`), phiếu đã hết hiệu lực vì đối tượng bị tái chế cho chủ khác (`staleRelease`), đối tượng không thuộc pool hoặc rỗng (`foreignRelease`).

**R7. Chính sách khi cạn** (quyết định D3; khai báo theo từng pool)
1. **Fixed**: Get trả **rỗng**, không cấp phát, không tạo đối tượng; tăng `exhausted`; ghi cảnh báo Editor ở lần cạn đầu tiên của pool đó trong phiên (các lần sau chỉ tăng bộ đếm — để log không sinh GC trong đo đạc). Hệ khách phải xử lý kết quả rỗng mà không sập. Dùng cho pool mà cạn nghĩa là hệ khách đã vượt khai báo của nó (lỗi lập trình) — xu Plinko, vì Plinko đã chặn trước bằng `maxActiveCoins`.
2. **RecycleOldest**: lấy đối tượng đang mượn có `acquire_seq` nhỏ nhất (`recycle_victim`), gọi hook *trả về* của chủ cũ với lý do `Recycled` (chủ cũ dừng tween, gỡ tham chiếu), vô hiệu phiếu cũ, rồi giao cho yêu cầu mới như một lần Get thường (hook *lấy ra*, `acquire_seq` mới); tăng `recycled`. Không cấp phát. Dùng cho đối tượng trang trí: thứ mới nhất luôn hiện, đúng khoảnh khắc (Pillar 1).
3. Không có chính sách tự lớn (Grow) trong MVP: mọi lần lớn thêm là cấp phát, trái AC "không cấp phát khi cạn".
4. Pool có `forbid_recycle = true` không bao giờ được mang chính sách RecycleOldest (R3, `registration_valid`).

**R8. Vòng đời phiên**
1. Pool tồn tại suốt phiên trong scene duy nhất; **không bao giờ thu nhỏ** (tổng bộ nhớ nhỏ, thu rồi lớn lại chỉ sinh GC).
2. Pooling không dùng thời gian (không đồng hồ, không timer), nên tạm dừng/mất focus và đổi tab không ảnh hưởng pool. Thứ tự tái chế dựa trên `acquire_seq`, không dựa trên giờ.
3. Pooling không có trạng thái cần lưu; không đăng ký với Save/Load. Pool được dựng lại mỗi phiên.
4. Khi scene bị dỡ (thoát game, hoặc nạp lại scene theo R8.5), mọi đối tượng bị hủy cùng scene; hook *trả về* không được gọi. Đối tượng pool thuộc scene `MainGame`, không bao giờ được đánh dấu sống qua scene.
5. **Nạp lại scene kết thúc phiên Pooling.** Khi Bootstrap vào BootFailed và người chơi bấm "Thử lại" (Bootstrap B4.4), scene `MainGame` được nạp lại và khởi động chạy lại từ `ServiceInit` (Bootstrap AC-10). Dù dịch vụ đang ở Accepting, Prewarming hay Ready lúc lỗi xảy ra, việc dỡ scene bỏ toàn bộ phiên Pooling: mọi pool và đối tượng của chúng, mọi bộ đếm chẩn đoán (R9), mọi `acquire_seq`, mọi phiếu mượn, cờ "đã cảnh báo cạn" (R7.1) và sổ đăng ký `id`. Lần `Run()` kế tiếp của bước `pooling.service` tạo một dịch vụ **mới** ở Accepting với sổ đăng ký rỗng, nên hệ khách đăng ký lại `plinko.coin` (và mọi pool khác) như lần khởi động đầu. Không có trạng thái Pooling nào là `static` hay nằm ngoài scene: phiên mới không phụ thuộc việc Unity có reload domain hay không (kể cả khi Enter Play Mode Options tắt domain reload trong Editor). Mọi kiểu của Pooling nằm trong namespace `CozyGachapon.Pooling` (và namespace con) — đây là phạm vi cố định của AC-32; chỉ được có field static bất biến (`const`, hoặc `static readonly` kiểu số, `bool`, `char`, `string`, enum, `ProfilerMarker`). Prewarm đang dở bị bỏ, không tiếp tục ở phiên mới; phiên mới prewarm lại từ đầu theo R4.

**R9. Chẩn đoán** — mỗi pool có bộ đếm chỉ đọc: `capacity`, `created`, `inUse`, `peakInUse`, `exhausted`, `recycled`, `doubleRelease`, `staleRelease`, `foreignRelease`, `lost`. Đọc bộ đếm không cấp phát. Test và [Perf] dùng các bộ đếm này thay vì đọc log.

**R10. Không cấp phát ở trạng thái ổn định** — sau khi Ready, mọi lời gọi Get, Release, tái chế và Get rỗng (Fixed cạn, sau cảnh báo đầu) cấp phát 0 B bộ nhớ quản lý.

**Pool MVP**

| Pool `id` | Hệ khách | `client_declared_max` lấy từ | `policy` | `headroom` | `forbid_recycle` |
|---|---|---|---|---|---|
| `plinko.coin` | Plinko Board | `PlinkoConfig.maxActiveCoins` (24, registry `max_active_coins`) | Fixed | `pools[plinko.coin].headroom` (0) | true |
| `feedback.floatingText` | Feedback & Juice (*provisional* cho tới khi có GDD Feedback & Juice) | Knob của hệ sở hữu số bay — hiện `PlinkoFeedbackConfig.floatingTextMaxOnScreen` (10), asset tạm của Plinko; có thể chuyển sang asset của Feedback & Juice | RecycleOldest | `pools[feedback.floatingText].headroom` (2) | false |
| `feedback.coinFizzle` | Feedback & Juice (*provisional* cho tới khi có GDD Feedback & Juice) | Knob của hệ sở hữu hiệu ứng tan — số hiệu ứng tan tối đa cùng lúc; *provisional* `PlinkoFeedbackConfig.fizzleMaxOnScreen` (đề xuất ở C2[13], chưa có trong `plinko-board.md`, chưa có giá trị), ký hiệu `F` trong ví dụ | RecycleOldest | `pools[feedback.coinFizzle].headroom` (0) | false |

Ghi chú: xu `JackpotBurst` dùng chung pool `plinko.coin` (biến thể sprite đặt ở hook lấy ra); xu `Queued` của Plinko không chiếm đối tượng pool nên `jackpot_burst_count` và `burstQueueMax` không cộng vào dung lượng. **Hiệu ứng tan của xu kẹt** (phương án B, quyết định user — `decision-log.md`): Plinko trả đối tượng xu ngay ở Resolved → Pooled, cùng bước mô phỏng (Plinko R9.1, không có trạng thái `Fizzling`), nên `pools[plinko.coin].headroom` = 0 và pool xu Fixed không bao giờ cạn vì hiệu ứng. Hiệu ứng "xu mờ + co lại" tại `CoinStuck.worldPosition` do Feedback & Juice vẽ bằng đối tượng của pool trang trí `feedback.coinFizzle` (prefab riêng, không phải prefab xu, không có Rigidbody2D), RecycleOldest: khi nhiều xu kẹt cùng lúc, hiệu ứng cũ nhất bị cắt sớm thay vì bỏ chạm. Feedback & Juice đăng ký pool này trong bước `GameplayInit` của nó. **Nguồn `client_declared_max` của pool trang trí** là knob của hệ sở hữu đối tượng đó, không phải `PoolingConfig` (D4): hiện cả hai pool `feedback.*` đọc từ `PlinkoFeedbackConfig` — asset tạm do Plinko giữ; khi GDD Feedback & Juice chuyển knob sang asset của nó, chỉ nguồn đọc lúc đăng ký đổi, `PoolingConfig` không đổi. Pool của toast, hiệu ứng trứng/kệ sẽ được thêm khi GDD HUD, Gacha, Shelf, Feedback được viết (mặc định RecycleOldest).

**Hệ khách không thể**: tạo đối tượng ngoài pool đã đăng ký trong phiên, đăng ký sau khi khởi động, làm pool lớn lên, tái chế đối tượng của pool `forbid_recycle`, trả một đối tượng hai lần mà gây tác dụng.

### States and Transitions

Trạng thái của dịch vụ:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Accepting | Bootstrap gọi `Run()` của bước `pooling.service` (pha `ServiceInit`) — mỗi lần scene `MainGame` nạp, kể cả sau "Thử lại"; luôn là dịch vụ mới với sổ đăng ký rỗng (R8.5) | Bootstrap gọi `Run()` của bước `pooling.prewarm` (pha `Prewarm`) → Prewarming; scene bị dỡ (BootFailed → "Thử lại") → phiên bị bỏ (R8.5) | Nhận đăng ký (R3); Get trả rỗng + lỗi Editor |
| Prewarming | Bootstrap gọi `Run()` của bước `pooling.prewarm` | Đã tạo đủ `pool_capacity` cho mọi pool → Ready; scene bị dỡ → phiên bị bỏ, prewarm dở không tiếp tục (R8.5) | Tạo ≤ `prewarmPerFrame` đối tượng/khung; từ chối đăng ký mới; Get trả rỗng + lỗi Editor; bước báo chưa xong |
| Ready | Prewarm xong (bước `pooling.prewarm` báo `IsDone`, trước `BootCompleted`) | Scene bị dỡ (thoát game, hoặc nạp lại sau "Thử lại" — R8.5) → phiên bị bỏ | Get/Release/tái chế; không tạo, không hủy; từ chối đăng ký mới |

Trạng thái của một đối tượng trong pool:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Free | Vừa được tạo lúc prewarm; hoặc được trả hợp lệ | Get → InUse | Không hiển thị, không mô phỏng |
| InUse | Get thành công (kể cả Get qua tái chế) | Release hợp lệ → Free; bị chọn làm `recycle_victim` → Recycling | Thuộc đúng một phiếu mượn còn hiệu lực |
| Recycling | Bị chọn làm `recycle_victim` (chỉ pool RecycleOldest) | Ngay trong cùng lời gọi → InUse (chủ mới) | Gọi hook trả về (`Recycled`) của chủ cũ, vô hiệu phiếu cũ, gọi hook lấy ra của chủ mới |
| Lost | Đối tượng bị hủy từ bên ngoài (không qua Pooling) | Không bao giờ (hết phiên) | Bị bỏ khỏi pool; `lost` tăng; dung lượng thực giảm 1; không tạo bù |

### Interactions with Other Systems

| Hệ | Pooling nhận vào | Pooling trả ra | Chủ interface |
|---|---|---|---|
| Plinko Board | Đăng ký `plinko.coin` (`client_declared_max` = `PlinkoConfig.maxActiveCoins`, `forbid_recycle` = true); Get khi một xu được nhận lên bàn (Plinko R4.5); Release ngay khi xu Resolved → Pooled, cùng bước mô phỏng (Plinko R9.1, không có `Fizzling`; hiệu ứng tan thuộc `feedback.coinFizzle`) | Đối tượng xu + phiếu mượn; rỗng nếu cạn (Fixed) — Plinko coi như xu không được nhận (R4.5) | Object Pooling |
| Feedback & Juice | Đăng ký `feedback.floatingText` (`client_declared_max` = `PlinkoFeedbackConfig.floatingTextMaxOnScreen`), `feedback.coinFizzle` (`client_declared_max` = knob số hiệu ứng tan tối đa, *provisional* `PlinkoFeedbackConfig.fizzleMaxOnScreen`) và các pool trang trí sau này; Get/Release số bay và hiệu ứng tan (Get khi nhận `CoinStuck`); hook trả về xử lý lý do `Recycled` | Đối tượng số bay / hiệu ứng tan + phiếu; đối tượng mượn lâu nhất khi cạn (RecycleOldest) | Object Pooling |
| Bootstrap & Scene Flow | Hợp đồng pha `IBootStep` (B2) và thứ tự pha (B3): `Run()` của bước `pooling.service` (pha `ServiceInit`) và `pooling.prewarm` (pha `Prewarm`, sau `GameplayInit`, trước `BootCompleted`); nạp lại scene sau "Thử lại" (B4.4) → phiên Pooling bị bỏ, khởi động mới tạo dịch vụ mới (R8.5) | Pooling đăng ký hai bước cốt lõi; `IsDone` của `pooling.prewarm` khi Ready, `IsFailed` khi bước lỗi (Bootstrap → BootFailed). Lỗi đăng ký pool do bước của hệ khách báo, không qua Pooling | Bootstrap sở hữu hợp đồng và thứ tự pha; Pooling sở hữu nội dung hai bước (`prewarmPerFrame`, `prewarmMaxSeconds`) |
| Gacha Machine, Shelf & Synergy, HUD & Tab Navigation (sau này) | Đăng ký pool hiệu ứng trứng/kệ, toast theo cùng hợp đồng R3 | Như Feedback | Object Pooling |
| Audio | — (Audio tự quản voice pool, D2) | — | Audio |

→ Thành ADR (D1): hình dạng API (đăng ký, Get, Release, phiếu mượn có generation), cơ chế ẩn/không mô phỏng (`SetActive` hay tắt renderer + `Rigidbody2D.simulated`), có dùng `UnityEngine.Pool.ObjectPool<T>` không, giao diện hook lấy ra/trả về, cách đọc bộ đếm không cấp phát.

## Formulas

Bốn công thức; mọi biến là knob (`SO field`), giá trị khai báo của hệ khách, hoặc hằng số có lý do. Không có số cân bằng gameplay ở đây — chỉ dung lượng và ngân sách khởi động.

### pool_capacity

`pool_capacity = client_declared_max + headroom`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| client_declared_max | int | 1 – `maxCapacityPerPool` | SO field của hệ khách, truyền lúc đăng ký (`plinko.coin`: `PlinkoConfig.maxActiveCoins`, 12–40; `feedback.floatingText`: `PlinkoFeedbackConfig.floatingTextMaxOnScreen`, 6–16; `feedback.coinFizzle`: knob số hiệu ứng tan tối đa của hệ sở hữu, *provisional* `PlinkoFeedbackConfig.fizzleMaxOnScreen`, chưa có giá trị) | Số đối tượng tối đa hệ khách giữ cùng lúc theo luật của chính nó |
| headroom | int | 0–8 | SO field `PoolingConfig.pools[id].headroom` | Phần dư cho đối tượng đã xong về logic nhưng còn chạy hiệu ứng biến mất trước khi được trả |
| pool_capacity | int | 1 – `maxCapacityPerPool` | calculated | Số đối tượng tạo sẵn và giữ suốt phiên |

**Output range:** 1–48 với các knob hiện có trong safe range (40 + 8); vượt `maxCapacityPerPool` → đăng ký bị từ chối (`registration_valid`), không kẹp ngầm.
**Example:** `plinko.coin` = 24 + 0 = **24**; `feedback.floatingText` = 10 + 2 = **12**; `feedback.coinFizzle` = `F` + 0 = **`F`** (`F` = số hiệu ứng tan tối đa do Feedback & Juice khai báo). Biên: `maxActiveCoins` = 40 (trần safe range) + headroom 8 = 48 ≤ 64 → chấp nhận; `client_declared_max` = 1, headroom 0 → 1.

### registration_valid

`registration_valid = phase_is_accepting ∧ id_defined ∧ prefab_present ∧ id_not_registered ∧ (client_declared_max ≥ 1) ∧ (pool_capacity ≤ maxCapacityPerPool) ∧ (registered_capacity_total + pool_capacity ≤ maxTotalPooledObjects) ∧ ¬(policy = RecycleOldest ∧ forbid_recycle)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| phase_is_accepting | bool | — | calculated | Dịch vụ đang ở Accepting |
| id_defined / prefab_present | bool | — | calculated từ `PoolingConfig.pools` | `id` có trong config và có prefab |
| id_not_registered | bool | — | calculated | `id` chưa được đăng ký trong phiên |
| client_declared_max | int | bất kỳ (kể cả ≤ 0 do lỗi) | đầu vào của hệ khách | Xem `pool_capacity` |
| pool_capacity | int | — | calculated (`pool_capacity`) | |
| maxCapacityPerPool | int | 48–128 | SO field `PoolingConfig.maxCapacityPerPool` | Trần một pool, chặn khai báo nhầm (ví dụ nhập 2400 thay vì 24) |
| registered_capacity_total | int | 0 – `maxTotalPooledObjects` | calculated | Tổng `pool_capacity` của các pool đã chấp nhận |
| maxTotalPooledObjects | int | 96–256 | SO field `PoolingConfig.maxTotalPooledObjects` | Trần tổng số đối tượng pool, giữ prewarm và bộ nhớ trong tầm |
| policy | enum | Fixed, RecycleOldest | SO field `PoolingConfig.pools[id].policy` | Chính sách khi cạn |
| forbid_recycle | bool | — | đầu vào của hệ khách | Hệ khách cấm tái chế (Plinko R9.1) |

**Output range:** true / false. False → không tạo đối tượng nào, lỗi Editor, thất bại trả về hệ khách.
**Example:** `plinko.coin` đăng ký trước: Accepting, có trong config, 24 ≤ 64, 0 + 24 ≤ 128, Fixed → **true**. `feedback.floatingText`: 12 ≤ 64, 24 + 12 = 36 ≤ 128, RecycleOldest nhưng `forbid_recycle` = false → **true**. `feedback.coinFizzle`: `F` ≤ 64, 36 + `F` ≤ 128 (đúng khi `F` ≤ 64), RecycleOldest, `forbid_recycle` = false → **true**. Nếu ai đó đổi `pools[plinko.coin].policy` thành RecycleOldest → ¬(true ∧ true) = false → **false**. Nếu `maxActiveCoins` bị nhập 240 → 244 > 64 → **false**.

### prewarm_frames

`total_prewarm = Σ pool_capacity (mọi pool đã chấp nhận)`
`prewarm_frames = ceil(total_prewarm / prewarmPerFrame)`
`prewarm_seconds_est = prewarm_frames / targetFps`, kiểm tra `prewarm_seconds_est ≤ prewarmMaxSeconds`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| total_prewarm | int | 1 – `maxTotalPooledObjects` | calculated | Mọi pool được prewarm đầy (D5) |
| prewarmPerFrame | int | 4–40 | SO field `PoolingConfig.prewarmPerFrame` | Số đối tượng tạo tối đa mỗi khung |
| targetFps | int | 60 | constant (studio config `targetFps`) | Ngân sách khung của dự án, không thuộc Pooling |
| prewarmMaxSeconds | float (s) | 0.1–0.5 | SO field `PoolingConfig.prewarmMaxSeconds` | Phần của Pooling trong ngân sách khởi động của Bootstrap (`BootConfig.maxBootToInteractiveSeconds` = 2.0 s, `bootstrap-and-scene-flow.md`) |
| prewarm_frames | int | 1–64 | calculated | Số khung prewarm |

**Output range:** MVP 4 khung (~0.067 s) khi `F` ≤ 4; 5 khung khi 5 ≤ `F` ≤ 14. Cực trị: 128 đối tượng với `prewarmPerFrame` = 4 → 32 khung ≈ 0.53 s > 0.25 s → validator cảnh báo (tăng `prewarmPerFrame` hoặc giảm dung lượng). `prewarmPerFrame` < 1 là cấu hình sai: validator báo lỗi, runtime dùng 1 để không chia cho 0 và không treo khởi động.
**Example:** total_prewarm = 24 + 12 + `F` = 36 + `F`; `prewarmPerFrame` = 10 → với `F` = 4: ceil(40/10) = **4 khung** → 4/60 ≈ **0.067 s** ≤ 0.25 s. Với 41 đối tượng (`F` = 5) → 5 khung ≈ 0.083 s. Ước tính chi phí Instantiate trên WebGL 0.05–0.2 ms/đối tượng (chưa đo) → 0.5–2 ms mỗi khung prewarm, dưới `PoolingTestTargets.prewarmFrameBudgetMs` (8 ms).

### recycle_victim

`recycle_victim = argmin over o ∈ InUse(pool) of acquire_seq(o)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| InUse(pool) | tập đối tượng | 1 – `pool_capacity` phần tử | calculated | Các đối tượng đang mượn của pool (pool cạn ⇒ = toàn bộ pool trừ `lost`) |
| acquire_seq | long (64-bit) | 1 – 2^63−1 | calculated | Số thứ tự mượn, tăng 1 mỗi lần Get thành công trong pool (kể cả Get qua tái chế) |

**Output range:** đúng một đối tượng; không có hòa vì `acquire_seq` duy nhất trong pool (kể cả nhiều Get trong cùng khung). Chỉ áp dụng cho pool RecycleOldest. Tràn 64-bit không xảy ra trong thực tế (1 000 lần mượn/giây cần ~2.9 × 10^8 năm) — `long` là hằng số kiểu dữ liệu, có chủ đích.
**Example:** pool `feedback.floatingText` (12) đang mượn đủ 12 số bay với seq 1…12. Yêu cầu thứ 13 → victim là seq 1, đối tượng đó nhận seq 13. Yêu cầu tiếp → victim seq 2. Nếu số seq 3 đã được trả rồi mượn lại (seq 14) trước đó, thì sau seq 2 victim kế tiếp là seq 4.

## Edge Cases

**Đăng ký và cấu hình**
- **If `client_declared_max` ≤ 0**: đăng ký bị từ chối (`registration_valid` = false), lỗi Editor, không tạo đối tượng. Hệ khách báo lỗi khởi tạo cho Bootstrap.
- **If `pool_capacity` > `maxCapacityPerPool`** (ví dụ gõ nhầm `maxActiveCoins` = 240): từ chối, lỗi Editor nêu `id`, giá trị khai báo và trần. Không kẹp ngầm — kẹp sẽ khiến pool Fixed cạn khó hiểu lúc chạy.
- **If tổng dung lượng vượt `maxTotalPooledObjects`**: pool đăng ký sau cùng (pool làm vượt) bị từ chối; các pool đã chấp nhận giữ nguyên.
- **If `id` không có trong `PoolingConfig` hoặc thiếu prefab**: từ chối; validator Editor đã báo trước khi chạy.
- **If cùng `id` đăng ký lần hai**: lần hai bị từ chối, lỗi Editor; pool đầu giữ nguyên.
- **If pool có `forbid_recycle` = true mà config đặt RecycleOldest**: từ chối (bảo vệ Plinko R9.1: mỗi xu giải quyết đúng một lần).
- **If đăng ký sau khi bước `pooling.prewarm` đã bắt đầu** (Prewarming hoặc Ready): từ chối, lỗi Editor. Hệ cần pool phải đăng ký trong bước `GameplayInit` của nó; không có prewarm muộn trong phiên (sẽ cấp phát khi đang chơi). Bootstrap cũng từ chối bước khởi động đăng ký muộn (Bootstrap B2.1).
- **If prefab của pool chứa AudioSource**: chạy bình thường, validator Editor cảnh báo (ranh giới D2: âm thanh thuộc voice pool của Audio).
- **If `prewarmPerFrame` < 1**: validator báo lỗi; runtime dùng 1 (không chia cho 0, khởi động không treo).
- **If `prewarm_seconds_est` > `prewarmMaxSeconds`**: validator cảnh báo, không chặn; [Perf] AC đo giá trị thật.

**Mượn và trả**
- **If Get khi dịch vụ chưa Ready**: trả rỗng, lỗi Editor, không tạo đối tượng. Theo Bootstrap, input bị rèm chặn nên trường hợp này là lỗi trình tự khởi động.
- **If pool Fixed cạn** (`plinko.coin` có 24 đối tượng đang mượn — chỉ xảy ra khi Plinko vượt `maxActiveCoins` của chính nó, vì headroom 0 và xu trả ngay ở Resolved): Get trả rỗng, `exhausted` +1, cảnh báo Editor ở lần cạn đầu, 0 B cấp phát. Plinko xử lý kết quả rỗng như xu không được nhận (Plinko R4.5: `Manual` → bỏ, phát `BoardBusy`; `Auto` → bỏ nhịp; xu loạt → ở lại đầu hàng đợi); không xu nào bị mất vì xu chưa từng được nhận.
- **If pool RecycleOldest cạn khi mọi đối tượng được mượn trong cùng một khung** (ví dụ jackpot + Bùng Xu sinh nhiều số bay): victim là đối tượng có `acquire_seq` nhỏ nhất — xác định, không phụ thuộc thời gian.
- **If pool RecycleOldest có dung lượng 1**: mỗi Get mới tái chế chính đối tượng đó; hợp lệ.
- **If hệ khách trả cùng phiếu hai lần**: lần hai bị bỏ qua, `doubleRelease` +1, lỗi Editor; đối tượng không bị đưa vào danh sách rảnh hai lần, nên hai lần Get sau đó luôn nhận hai đối tượng khác nhau.
- **If chủ cũ trả một phiếu đã bị tái chế** (tween số bay kết thúc sau khi đối tượng đã được giao cho số mới): bị bỏ qua, `staleRelease` +1; đối tượng vẫn InUse cho chủ mới. Đây là lý do phiếu mượn có hiệu lực theo lần mượn, không theo đối tượng.
- **If trả đối tượng không thuộc pool, hoặc rỗng**: bỏ qua, `foreignRelease` +1, lỗi Editor.
- **If hệ khách bỏ qua hook trả về lý do `Recycled`** (không dừng tween cũ): đối tượng có thể chạy hai hành vi chồng nhau — lỗi của hệ khách; Pooling chỉ bảo đảm hook được gọi trước hook lấy ra (AC).
- **If một đối tượng pool bị Destroy từ bên ngoài**: lần chạm tới kế tiếp (Get chọn trúng hoặc Release) bỏ nó khỏi pool, `lost` +1, lỗi Editor; **không tạo bù** (sẽ cấp phát). Pool Fixed có thể cạn sớm hơn — lỗi lộ ra qua `exhausted` thay vì bị che.
- **If hệ khách không bao giờ trả đối tượng (rò rỉ)**: Fixed → cạn dần, `exhausted` và `inUse` lộ lỗi; RecycleOldest → đối tượng rò rỉ cuối cùng bị tái chế. Không có cơ chế thu hồi theo thời gian (Pooling không dùng thời gian).

**Phiên và nền tảng**
- **If ứng dụng tạm dừng / tab trình duyệt bị ẩn / người chơi đổi sang Tab 2–3**: pool không đổi gì. Plinko vẫn mượn/trả xu khi mô phỏng chạy ở tab ẩn (Plinko R11); việc trả sớm số bay khi Tab 1 bị ẩn do Feedback & Juice quyết định.
- **If phiên kết thúc khi còn đối tượng đang mượn**: đối tượng bị hủy cùng scene, hook không được gọi; không có gì cần lưu.
- **If BootFailed khi Pooling đang Accepting, Prewarming hoặc Ready rồi người chơi bấm "Thử lại"** (ví dụ bước `GameplayInit` cốt lõi của một hệ khác lỗi sau khi Plinko đã đăng ký `plinko.coin`; hoặc `pooling.prewarm` quá `BootConfig.stepTimeoutSeconds`): scene nạp lại bỏ toàn bộ phiên Pooling (R8.5); `Run()` mới của `pooling.service` tạo dịch vụ mới ở Accepting, đăng ký lại `plinko.coin` thành công (không bị coi là trùng `id`), bộ đếm bắt đầu từ 0, prewarm chạy lại đủ từ đầu. Không có vòng BootFailed do trạng thái cũ. Hook *trả về* của các đối tượng đang mượn ở phiên cũ không được gọi (R8.4).
- **If người chơi bấm "Thử lại" nhiều lần liên tiếp**: mỗi lần nạp scene là một phiên Pooling mới, độc lập; số đối tượng tồn tại sau mỗi lần khởi động thành công = `registered_capacity_total` của phiên đó, không cộng dồn qua các lần thử.
- **If ADR hiệu năng hạ `maxActiveCoins`** (ví dụ 24 → 16): pool xu tự theo ở phiên kế tiếp (16 + 0 = 16), không sửa `PoolingConfig`.
- **If nhiều xu kẹt cùng lúc hơn `F`** (dung lượng `feedback.coinFizzle`): hiệu ứng tan cũ nhất bị tái chế (RecycleOldest), Feedback & Juice cắt nó trong hook `Recycled`; pool xu không bị ảnh hưởng vì xu đã về pool ở Resolved, không chạm nào bị bỏ.
- **If bộ nhớ**: khoảng 36 + `F` đối tượng 2D nhỏ trong MVP, ước tính dưới 1 MB so với ngân sách 1024 MB; `maxTotalPooledObjects` là chốt chặn khi thêm pool sau này.

## Dependencies

Object Pooling chỉ phụ thuộc Bootstrap & Scene Flow (hợp đồng pha khởi động, cùng lớp Foundation); Bootstrap không phụ thuộc Pooling vì chỉ biết interface `IBootStep`. Các hệ còn lại dưới đây phụ thuộc Pooling.

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Plinko Board | Plinko phụ thuộc Pooling | Hard (với Plinko) | In: đăng ký `plinko.coin` với `PlinkoConfig.maxActiveCoins`, `forbid_recycle` = true (trong bước `GameplayInit` của Plinko); Get khi xu được nhận lên bàn (R4.5), Release ngay khi xu Resolved → Pooled (R9.1, không có `Fizzling`). Out: đối tượng xu + phiếu, hoặc rỗng (Fixed cạn → Plinko R4.5). Owner: Object Pooling. Khớp hàng "Object Pooling" trong Dependencies của Plinko ("Release ngay khi xu Resolved", "kích thước pool ≥ `max_active_coins`"): `pool_capacity` = 24 + 0 = 24 ≥ 24. |
| Feedback & Juice | Feedback phụ thuộc Pooling | Hard (với Feedback) | In: đăng ký `feedback.floatingText` (`client_declared_max` = `PlinkoFeedbackConfig.floatingTextMaxOnScreen`), `feedback.coinFizzle` (`client_declared_max` = knob số hiệu ứng tan tối đa, *provisional* `PlinkoFeedbackConfig.fizzleMaxOnScreen`) và pool trang trí sau này — knob do hệ sở hữu đối tượng giữ, hiện ở asset tạm `PlinkoFeedbackConfig`; Get/Release; xử lý hook `Recycled`. Out: đối tượng số bay, hiệu ứng tan. Owner: Object Pooling. *Provisional* — Feedback chưa có GDD; GDD đó phải liệt kê Object Pooling. |
| Bootstrap & Scene Flow | Pooling phụ thuộc Bootstrap (hợp đồng pha) | Hard | In: hợp đồng `IBootStep` (B2) và thứ tự pha `ServiceInit` → `DataLoad` → `SaveLoad` → `GameplayInit` → `Prewarm` → `BootCompleted` (B3; mọi hệ khách đăng ký pool trong `GameplayInit`). Out: Pooling đăng ký bước `pooling.service` (pha `ServiceInit`, → Accepting) và bước `pooling.prewarm` (pha `Prewarm`, prewarm toàn bộ pool — Pool D5, → Ready), cả hai cốt lõi. Owner: Bootstrap (hợp đồng và thứ tự pha); Pooling (nội dung hai bước). Khớp hàng "Object Pooling" trong Dependencies của `bootstrap-and-scene-flow.md`. |
| Gacha Machine, Shelf & Synergy, HUD & Tab Navigation | Sẽ phụ thuộc Pooling nếu có đối tượng sinh/hủy lặp lại (hiệu ứng trứng, kệ, toast) | Soft (tùy thiết kế của hệ đó) | Cùng hợp đồng R3; mỗi GDD khai báo `client_declared_max` từ knob của nó và thêm `PoolDefinition`. |
| Audio | Không phụ thuộc theo hướng nào | — | Audio tự quản voice pool (D2). Systems index giữ nguyên: Audio không phụ thuộc Object Pooling. |
| Time & Timers, Save/Load | Không phụ thuộc | — | Pooling không dùng thời gian, không lưu trạng thái. |

**Hai chiều**: Plinko Board đã liệt kê Object Pooling (Hard) — khớp. Feedback & Juice đã có Object Pooling trong cột Depends On của systems index — khớp, GDD chưa có. Bootstrap & Scene Flow đã liệt kê Object Pooling ("Pooling phụ thuộc Bootstrap (hợp đồng pha)", Hard) — khớp; systems index chưa có Bootstrap trong cột Depends On của Object Pooling (báo cho Sync).

## Tuning Knobs

Hai asset: `PoolingConfig` (runtime) và `PoolingTestTargets` (chỉ dùng cho test/đo hiệu năng, không ảnh hưởng gameplay). Không có số nào trong code; validator Editor cảnh báo khi giá trị ra ngoài safe range. Dung lượng gốc của từng pool là knob của hệ khách và **không** lặp lại ở đây: `PlinkoConfig.maxActiveCoins` (24, safe 12–40; registry `max_active_coins`), `PlinkoFeedbackConfig.floatingTextMaxOnScreen` (10, safe 6–16; knob của hệ sở hữu số bay, asset tạm do Plinko giữ, có thể chuyển sang Feedback & Juice), knob số hiệu ứng tan tối đa cho `feedback.coinFizzle` (*provisional* `PlinkoFeedbackConfig.fizzleMaxOnScreen`, chưa có trong `plinko-board.md`; hệ sở hữu đặt giá trị và safe range).

**Runtime (`PoolingConfig`)**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Đối tượng prewarm mỗi khung | `PoolingConfig.prewarmPerFrame` | int | 10 | 4–40 | Một khung khởi động dài, giật dưới rèm, vượt `prewarmFrameBudgetMs` | Prewarm kéo dài nhiều khung, vượt `prewarmMaxSeconds` | prewarmMaxSeconds, maxTotalPooledObjects |
| Ngân sách thời gian prewarm | `PoolingConfig.prewarmMaxSeconds` | float (s) | 0.25 | 0.1–0.5 (≤ ¼ `BootConfig.maxBootToInteractiveSeconds`) | Prewarm ăn vào ngân sách khởi động của hệ khác | Cảnh báo giả với số pool bình thường | prewarmPerFrame, Bootstrap |
| Trần dung lượng một pool | `PoolingConfig.maxCapacityPerPool` | int | 64 | 48–128 (≥ 40 + 8 của pool xu ở trần) | Không bắt được khai báo nhầm | Từ chối pool xu hợp lệ khi `maxActiveCoins` tăng | PlinkoConfig.maxActiveCoins, headroom |
| Trần tổng đối tượng pool | `PoolingConfig.maxTotalPooledObjects` | int | 128 | 96–256 | Prewarm và bộ nhớ phình không ai thấy | Từ chối pool mới của Gacha/Kệ/HUD | prewarmPerFrame, mọi pool |
| Headroom pool xu | `PoolingConfig.pools[plinko.coin].headroom` | int | 0 | 0–8 (0 đủ vì xu về pool ngay ở Resolved — Plinko R9.1; hiệu ứng tan ở `feedback.coinFizzle`) | Thừa đối tượng (rẻ), che lỗi Plinko vượt `maxActiveCoins` | — (không thể thấp hơn 0) | Plinko R9.1, maxActiveCoins |
| Chính sách pool xu | `PoolingConfig.pools[plinko.coin].policy` | enum | Fixed | Chỉ Fixed (`forbid_recycle`) | — | — | Plinko R9.1 |
| Headroom pool số bay | `PoolingConfig.pools[feedback.floatingText].headroom` | int | 2 | 0–6 | Thừa đối tượng (rẻ) | Số đang mờ dần bị tái chế sớm, số bay "nháy mất" | floatingTextMaxOnScreen, floatingTextMergeWindow |
| Chính sách pool số bay | `PoolingConfig.pools[feedback.floatingText].policy` | enum | RecycleOldest | RecycleOldest hoặc Fixed | — | Fixed: số mới bị mất ngay lúc jackpot | Feedback & Juice |
| Headroom pool hiệu ứng tan | `PoolingConfig.pools[feedback.coinFizzle].headroom` | int | 0 | 0–4 | Thừa đối tượng (rẻ) | — (không thể thấp hơn 0; khai báo `F` của hệ sở hữu đã gồm cả hiệu ứng đang mờ) | fizzleMaxOnScreen (*provisional*), fizzleDotCount |
| Chính sách pool hiệu ứng tan | `PoolingConfig.pools[feedback.coinFizzle].policy` | enum | RecycleOldest | RecycleOldest hoặc Fixed | — | Fixed: hiệu ứng tan mới không hiện khi nhiều xu kẹt cùng lúc | Feedback & Juice |

**Mục tiêu đo (`PoolingTestTargets`)**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Số chu kỳ trong test GC | `PoolingTestTargets.gcTestCycles` | int | 1000 | 500–10000 | Test chậm | Bỏ sót cấp phát hiếm | — |
| Số khung đo [Perf] | `PoolingTestTargets.perfSampleFrames` | int | 600 | 300–3600 | Phiên đo dài | Không phủ được jackpot/Mưa Xu | Plinko AC-38 (cùng kịch bản) |
| Ngân sách một khung prewarm | `PoolingTestTargets.prewarmFrameBudgetMs` | float (ms) | 8 | 4–12 (< 16.7 ms của `targetFps`) | Không bắt được khung giật dưới rèm | Test trượt trên thiết bị yếu | prewarmPerFrame |

**Hằng số (không phải knob, có lý do)**
- Cấp phát cho phép ở trạng thái ổn định = **0 B**: yêu cầu của quyết định D1 và Plinko AC-38, không tinh chỉnh.
- `acquire_seq` là `long` 64-bit: kiểu dữ liệu, không tràn trong thực tế (`recycle_victim`).
- `targetFps` = 60: thuộc studio config, Pooling chỉ đọc.

## Visual/Audio Requirements

Không áp dụng — hệ hạ tầng không có phản hồi người chơi. Một hệ quả nhìn thấy được thuộc về hệ khách: khi pool RecycleOldest cạn, số bay cũ nhất biến mất ngay để nhường chỗ cho số mới; cách nó biến mất (cắt ngay hay mờ nhanh trong hook `Recycled`) do Feedback & Juice quyết định. Pooling chỉ bảo đảm đối tượng trong pool không hiển thị.

## Game Feel

Không áp dụng — hệ hạ tầng. Đóng góp gián tiếp: không có khung giật do Instantiate/GC ở cú chạm đầu (Plinko `tapToSpawnMaxMs` = 50 ms) và lúc jackpot / Mưa Đồng Xu, được chứng minh bằng các AC [Perf].

## UI Requirements

Không áp dụng — không có thông tin hiển thị cho người chơi. Bộ đếm chẩn đoán (R9) chỉ dành cho test và công cụ debug.

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Pillar 1, ràng buộc WebGL 60 FPS | `Docs/design/gdd/game-concept.md` | Pillar 1 Kinetic ASMR, nền tảng WebGL/mobile | Lý do tồn tại của hệ |
| Pool xu | `Docs/design/gdd/plinko-board.md` | `PlinkoConfig.maxActiveCoins` (registry `max_active_coins` = 24), R4.5 (mượn xu, xử lý khi pool trả rỗng), R9.1 (giải quyết đúng một lần, xu về pool), trạng thái Queued không chiếm đối tượng, trạng thái Resolved → Pooled, biến thể xu `JackpotBurst`, hiệu ứng tan xu kẹt, AC-38 (0 B GC) | Plinko là hệ khách; Pooling tôn trọng giá trị đã khóa |
| Pool số bay | `Docs/design/gdd/plinko-board.md` → `Docs/design/gdd/feedback-and-juice.md` *(chưa có)* | `PlinkoFeedbackConfig.floatingTextMaxOnScreen` (10), `floatingTextMergeWindow` | Feedback là hệ khách (*provisional*) |
| Pool hiệu ứng tan | `Docs/design/gdd/plinko-board.md` → `Docs/design/gdd/feedback-and-juice.md` *(chưa có)* | R9.1 + bảng trạng thái (Resolved → Pooled, không có `Fizzling`), hàng "Xu kẹt (fizzle, theo `CoinStuck`)" của Visual/Audio, `fizzleDotCount`, `fizzleTextAlpha`, knob đề xuất `fizzleMaxOnScreen` (C2[13]) | Feedback là hệ khách (*provisional*); quyết định B trong `Docs/production/session-logs/decision-log.md` |
| Bước prewarm | `Docs/design/gdd/bootstrap-and-scene-flow.md` | B2 (hợp đồng `IBootStep`: `Id`, `Phase`, `IsCritical`, `Run()`, `IsDone`/`IsFailed`), B3 (pha `ServiceInit` … `GameplayInit` → `Prewarm` → `BootCompleted`), rèm "Tiệm mở cửa", `BootConfig.maxBootToInteractiveSeconds` (2.0 s), B4.4 ("Thử lại" nạp lại scene `MainGame`, mọi trạng thái phiên bị bỏ), AC-10 (kịch bản thử lại), `BootConfig.stepTimeoutSeconds` | Pooling phụ thuộc Bootstrap; đăng ký bước `pooling.service` và `pooling.prewarm`; vòng đời phiên Pooling theo lần nạp scene (R8.5, AC-31 – AC-33) |
| Voice pool | `Docs/design/gdd/audio.md` | A4 (voice tự quản, Pool D2) | Ranh giới phạm vi |
| Pool toast / hiệu ứng | `Docs/design/gdd/gacha-machine.md`, `shelf-and-synergy.md`, `hud-and-tab-navigation.md` *(chưa có)* | Khai báo pool trang trí | Khách tương lai |
| Hệ trong index | `Docs/design/gdd/systems-index.md` | Hàng 4 (Object Pooling), hàng 6 và 11 (Depends On) | Truy vết |

## Acceptance Criteria

Ký hiệu `P.` = `PoolingConfig`, `T.` = `PoolingTestTargets`. Test EditMode thuộc asmdef `CozyGachapon.Tests.EditMode`, dùng prefab test nhẹ (SpriteRenderer + Rigidbody2D) và hook giả ghi lại thứ tự lời gọi; lỗi/cảnh báo Editor được kiểm bằng `LogAssert`. "Bộ đếm" là bộ đếm chẩn đoán R9. Con số trong ngoặc là giá trị hiện tại của knob, không phải literal của test.

**Đăng ký (R2, R3, pool_capacity, registration_valid)**
- `AC-1 [EditMode]` **GIVEN** dịch vụ Accepting, `P.pools[plinko.coin]` = Fixed, headroom `H`, **WHEN** đăng ký `plinko.coin` với `client_declared_max` = `PlinkoConfig.maxActiveCoins` và `forbid_recycle` = true, **THEN** đăng ký thành công và bộ đếm `capacity` = `PlinkoConfig.maxActiveCoins` + `H` (24).
- `AC-2 [EditMode]` **GIVEN** cặp (declared, headroom) = (1, 0), (24, 4), (40, 8), (10, 2), **WHEN** đăng ký từng pool, **THEN** `capacity` lần lượt = 1, 28, 48, 12.
- `AC-3 [EditMode]` **GIVEN** dịch vụ Accepting, **WHEN** đăng ký lần lượt các ca sai: `id` không có trong config; prefab rỗng; `client_declared_max` = 0 và = −1; `pool_capacity` = `P.maxCapacityPerPool` + 1; pool làm tổng vượt `P.maxTotalPooledObjects`; `id` đã đăng ký; policy RecycleOldest với `forbid_recycle` = true, **THEN** mỗi ca trả thất bại, ghi đúng một lỗi Editor, không tạo đối tượng nào (bộ đếm `created` của mọi pool không đổi) và các pool đã chấp nhận trước đó giữ nguyên `capacity`.
- `AC-4 [EditMode]` **GIVEN** dịch vụ đã ở Prewarming và một lần khác ở Ready, **WHEN** đăng ký một `id` hợp lệ, **THEN** cả hai lần bị từ chối với lỗi Editor và không tạo đối tượng.
- `AC-5 [EditMode]` **GIVEN** `PoolDefinition` có prefab chứa AudioSource, **WHEN** chạy validator, **THEN** có đúng một cảnh báo nêu `id` đó; đăng ký vẫn thành công.

**Prewarm (R4, prewarm_frames)**
- `AC-6 [EditMode]` **GIVEN** các pool đã đăng ký với tổng dung lượng `N` và `P.prewarmPerFrame` = `k`, **WHEN** chạy từng bước prewarm (mỗi bước = một khung), **THEN** mỗi bước tạo ≤ `k` đối tượng, số bước = ceil(`N` / `k`), và sau bước cuối dịch vụ Ready với `created` = `capacity` ở mọi pool; kiểm với (`N`, `k`) = (40, 10) → 4 bước, (41, 10) → 5 bước, (40, 40) → 1 bước, (40, 4) → 10 bước.
- `AC-7 [EditMode]` **GIVEN** `P.prewarmPerFrame` = 0, **WHEN** chạy validator và prewarm, **THEN** validator báo lỗi, prewarm vẫn kết thúc sau `N` bước (dùng 1) và không ném exception.
- `AC-8 [EditMode]` **GIVEN** cấu hình có ceil(`N` / `P.prewarmPerFrame`) / `targetFps` > `P.prewarmMaxSeconds` (ví dụ `N` = 128, `k` = 4 → 0.53 s > 0.25 s), **WHEN** chạy validator, **THEN** có cảnh báo; với cấu hình MVP thật của project (`N` = `registered_capacity_total` = 36 + `F`, `k` = `P.prewarmPerFrame`; ví dụ `F` = 4 → (40, 10) → 0.067 s) thì không có cảnh báo.
- `AC-9 [EditMode]` **GIVEN** prewarm xong, **WHEN** đọc mọi đối tượng ở trạng thái Free, **THEN** không đối tượng nào có renderer đang hiển thị hay Rigidbody2D đang mô phỏng.
- `AC-10 [Perf]` **GIVEN** bản WebGL trên thiết bị tham chiếu tầm trung của ADR hiệu năng, cấu hình MVP, **WHEN** đo các khung prewarm bằng Profiler, **THEN** mỗi khung prewarm ≤ `T.prewarmFrameBudgetMs` (8 ms) và tổng thời gian prewarm ≤ `P.prewarmMaxSeconds` (0.25 s).

**Mượn và trả (R5, R6)**
- `AC-11 [EditMode]` **GIVEN** dịch vụ Ready, **WHEN** Get một đối tượng, **THEN** đối tượng đang hiển thị/mô phỏng, hook lấy ra được gọi đúng 1 lần trước khi Get trả về, `inUse` +1 và `peakInUse` ≥ `inUse`.
- `AC-12 [EditMode]` **GIVEN** một đối tượng đang mượn, **WHEN** Release bằng phiếu của nó, **THEN** đối tượng không hiển thị, không mô phỏng, hook trả về được gọi đúng 1 lần với lý do `Released`, `inUse` −1; dữ liệu do hệ khách gắn trên đối tượng không bị Pooling đổi.
- `AC-13 [EditMode]` **GIVEN** một phiếu đã Release, **WHEN** Release lại cùng phiếu, **THEN** `doubleRelease` +1, một lỗi Editor, `inUse` không đổi, và hai lần Get kế tiếp trả về hai đối tượng khác nhau.
- `AC-14 [EditMode]` **GIVEN** đối tượng nằm trong pool khác, một GameObject không thuộc pool nào, và một phiếu rỗng, **WHEN** Release từng cái vào pool đang test, **THEN** cả ba bị bỏ qua, `foreignRelease` +3, không exception.
- `AC-15 [EditMode]` **GIVEN** dịch vụ ở Accepting hoặc Prewarming, **WHEN** Get, **THEN** trả rỗng, một lỗi Editor, `created` không đổi.

**Chính sách khi cạn (R7, recycle_victim)**
- `AC-16 [EditMode]` **GIVEN** pool Fixed có `capacity` = `C` và `C` đối tượng đang mượn, **WHEN** Get, **THEN** trả rỗng, `exhausted` +1, `created` vẫn = `C`, lần cạn đầu ghi đúng một cảnh báo Editor và các lần cạn sau trong phiên không ghi thêm.
- `AC-17 [EditMode]` **GIVEN** pool RecycleOldest `capacity` = `C` đã mượn đủ với seq 1…`C`, **WHEN** Get hai lần, **THEN** lần 1 trả đối tượng seq 1 và lần 2 trả đối tượng seq 2; với mỗi lần, hook trả về (lý do `Recycled`) của chủ cũ được gọi trước hook lấy ra của chủ mới; `recycled` +2; `created` vẫn = `C`.
- `AC-18 [EditMode]` **GIVEN** pool RecycleOldest `capacity` = `C` mượn đủ, rồi trả đối tượng seq 3 và mượn lại (seq `C`+1), **WHEN** Get ba lần, **THEN** victim lần lượt là seq 1, 2, 4.
- `AC-19 [EditMode]` **GIVEN** pool RecycleOldest `capacity` = 1 đang mượn, **WHEN** Get 3 lần, **THEN** cả ba trả cùng một đối tượng với seq tăng dần, `recycled` +3.
- `AC-20 [EditMode]` **GIVEN** đối tượng A bị tái chế cho chủ mới, **WHEN** chủ cũ Release bằng phiếu cũ của A, **THEN** bị bỏ qua, `staleRelease` +1, A vẫn đang mượn và vẫn hiển thị cho chủ mới.

**Vòng đời và độ bền (R8, Lost)**
- `AC-21 [EditMode]` **GIVEN** pool đã từng đạt `peakInUse` = `capacity`, **WHEN** trả hết rồi chạy thêm `T.gcTestCycles` chu kỳ Get/Release, **THEN** `created` = `capacity` suốt quá trình (không thu nhỏ, không tạo mới, không hủy).
- `AC-22 [EditMode]` **GIVEN** một đối tượng Free bị Destroy từ bên ngoài, **WHEN** Get đủ `capacity` lần, **THEN** đối tượng bị hủy không bao giờ được trả ra, `lost` = 1, một lỗi Editor, Get thứ `capacity` áp chính sách cạn của pool, `created` không đổi.
- `AC-23 [EditMode]` **GIVEN** đồng hồ test đứng yên (giả lập Paused) hoặc chạy nhanh 1 giờ, **WHEN** thực hiện cùng chuỗi Get/Release/tái chế, **THEN** kết quả (đối tượng trả ra, victim, bộ đếm) giống hệt nhau — Pooling không phụ thuộc thời gian.
- `AC-31 [EditMode]` **GIVEN** ba ca của phiên cũ — dịch vụ ở Accepting với `plinko.coin` đã đăng ký; ở Prewarming sau một bước prewarm; ở Ready với `plinko.coin` có đối tượng đang mượn và `exhausted` > 0 — **WHEN** mô phỏng nạp lại scene (huỷ dịch vụ cùng mọi đối tượng pool, rồi chạy lại `Run()` của `pooling.service`) và đăng ký lại `plinko.coin` với cùng tham số, **THEN** ở cả ba ca: dịch vụ mới ở Accepting, đăng ký trả thành công, không có lỗi Editor "trùng `id`", trước prewarm `created` = 0 và mọi bộ đếm R9 khác (`inUse`, `peakInUse`, `exhausted`, `recycled`, `doubleRelease`, `staleRelease`, `foreignRelease`, `lost`) = 0; sau prewarm `created` = `capacity` và Get đầu tiên mang `acquire_seq` = 1; hook *trả về* của phiên cũ được gọi 0 lần.
- `AC-32 [EditMode]` **GIVEN** tập kiểu Pooling theo R8.5 — mọi kiểu (kể cả kiểu lồng, kiểu generic) có `Type.Namespace` bằng `CozyGachapon.Pooling` hoặc bắt đầu bằng `CozyGachapon.Pooling.`, lấy từ assembly chứa kiểu dịch vụ Pooling (hiện `CozyGachapon.Runtime`; asmdef Pooling riêng nếu ADR D1 tách), bỏ qua kiểu mang `[CompilerGenerated]` (lớp cache lambda `<>c` với field `<>9`, `<>9__N_M`; lớp closure `<>c__DisplayClass…`) và field mang `[CompilerGenerated]`, **trừ** backing field của auto-property static (`<…>k__BackingField`) và của event static dạng field — hai loại này vẫn bị kiểm; tập có ≥ 1 kiểu, **WHEN** duyệt bằng reflection field của từng kiểu với `BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly`, **THEN** mọi field là `const` (`FieldInfo.IsLiteral`) hoặc `static readonly` (`FieldInfo.IsInitOnly`) có `FieldType` thuộc danh sách cho phép — kiểu số nguyên thủy, `bool`, `char`, `string`, enum, `Unity.Profiling.ProfilerMarker` — và số field còn lại = 0; test in tên kiểu + tên field của từng vi phạm. Không có trạng thái Pooling nào sống qua lần nạp lại scene khi domain không reload (R8.5).
- `AC-33 [Play mode]` **GIVEN** kịch bản Bootstrap AC-10 trên scene `MainGame` với `PoolingConfig` và `PlinkoConfig` thật, trong đó một bước `GameplayInit` cốt lõi giả chạy sau bước đăng ký pool của Plinko, lỗi ở lần chạy đầu và thành công ở lần sau, **WHEN** khởi động vào BootFailed rồi bấm "Thử lại", **THEN** scene được nạp lại đúng 1 lần, Bootstrap phát `BootCompleted` đúng 1 lần, bước `pooling.prewarm` báo `IsDone`, `plinko.coin` có `capacity` = `PlinkoConfig.maxActiveCoins` + `P.pools[plinko.coin].headroom` và `created` = `capacity` (không cộng dồn với phiên lỗi), số GameObject đối tượng pool đang tồn tại = `registered_capacity_total` của phiên mới.

**Không cấp phát (R10, quyết định D1)**
- `AC-24 [EditMode]` **GIVEN** dịch vụ Ready, một pool Fixed và một pool RecycleOldest, cảnh báo cạn đầu tiên của pool Fixed đã được ghi, **WHEN** chạy `T.gcTestCycles` chu kỳ gồm Get, Release, Get qua tái chế, Get rỗng khi Fixed cạn và đọc mọi bộ đếm, đo bằng `GC.GetAllocatedBytesForCurrentThread()` trước và sau, **THEN** chênh lệch = 0 B. (Các đường lỗi luôn ghi log Editor — trả kép, trả lạ — nằm ngoài phép đo vì log Editor tự cấp phát; trong build chúng chỉ tăng bộ đếm.)
- `AC-25 [Perf]` **GIVEN** bản WebGL trên thiết bị tham chiếu, kịch bản của Plinko AC-36/AC-38 (`maxActiveCoins` xu hoạt động + `RequestBurst(burstQueueMax)`, có jackpot và số bay), **WHEN** ghi `T.perfSampleFrames` (600) khung bằng `ProfilerRecorder` "GC Allocated In Frame", **THEN** 0 B mỗi khung, đồng thời bộ đếm cho thấy có Get/Release trong cửa sổ đo (`peakInUse` của `plinko.coin` > 0) và `created` không đổi so với lúc Ready.

**Tích hợp với hệ khác**
- `AC-26 [EditMode]` **GIVEN** `PlinkoConfig` và `PoolingConfig` thật của project, **WHEN** Plinko đăng ký `plinko.coin` và sim bàn ở trạng thái Saturated có hàng đợi Bùng Xu đầy, **THEN** `P.pools[plinko.coin].headroom` = 0, `peakInUse` ≤ `PlinkoConfig.maxActiveCoins`, `exhausted` = 0, và ở mọi bước mô phỏng số đối tượng đang mượn = số xu Falling (không tính xu Queued, không tính xu đã Resolved — kể cả xu kẹt, vì xu được Release trong cùng bước phát `CoinResolved`); khớp Plinko AC-46.
- `AC-27 [EditMode]` **GIVEN** `PlinkoFeedbackConfig` và `PoolingConfig` thật, **WHEN** Feedback yêu cầu `floatingTextMaxOnScreen` + `P.pools[feedback.floatingText].headroom` + 5 số bay liên tiếp không trả, **THEN** `recycled` = 5, `inUse` = `capacity` (12) và không có số bay nào vượt `capacity` cùng hiển thị.
- `AC-28 [EditMode]` **GIVEN** `PoolingConfig` thật, **WHEN** đăng ký các pool MVP, **THEN** pool `plinko.coin` có policy Fixed (đăng ký với `forbid_recycle` = true thành công) và chỉ có đúng một pool chứa prefab xu (xu `JackpotBurst` dùng chung pool).
- `AC-30 [EditMode]` **GIVEN** registry bước khởi động giả của Bootstrap và dịch vụ Pooling vừa được nạp, **WHEN** đọc các bước Pooling đã đăng ký rồi chạy khởi động với các bước `GameplayInit` giả mỗi bước đăng ký 1 pool, **THEN** Pooling đăng ký đúng hai bước — `pooling.service` (pha `ServiceInit`, `IsCritical` = true) và `pooling.prewarm` (pha `Prewarm`, `IsCritical` = true); mọi đăng ký pool được chấp nhận trước `Run()` của `pooling.prewarm`; `pooling.prewarm` báo `IsDone` = false cho tới khi dịch vụ Ready và = true ngay khi Ready; Pooling không gọi API nào của Bootstrap ngoài hợp đồng `IBootStep` (khớp Bootstrap AC-7).
- `AC-34 [EditMode]` *(provisional — đóng cùng Feedback & Juice)* **GIVEN** `PoolingConfig` thật và `client_declared_max` của `feedback.coinFizzle` = `F` đọc từ knob của hệ sở hữu, **WHEN** đăng ký `feedback.coinFizzle` với `forbid_recycle` = false rồi, sau prewarm, Get `F` + `P.pools[feedback.coinFizzle].headroom` + 3 hiệu ứng tan liên tiếp không trả trong khi `plinko.coin` có `C` đối tượng đang mượn, **THEN** đăng ký thành công với policy RecycleOldest, `capacity` = `F` + `P.pools[feedback.coinFizzle].headroom`, prefab của pool khác prefab của `plinko.coin` và không có Rigidbody2D, `recycled` của `feedback.coinFizzle` = 3, và `inUse` / `exhausted` / `created` của `plinko.coin` không đổi (= `C`, 0, `capacity`).

**Tiêu chí trải nghiệm (playtest)**
- `AC-29 [Play mode]` **GIVEN** bản build mới mở, **WHEN** người chơi chạm bàn Plinko lần đầu ngay khi rèm kéo lên, **THEN** xu xuất hiện trong ≤ `PlinkoBalanceTargets.tapToSpawnMaxMs` (50 ms) và Profiler không ghi Instantiate nào của pool trong khung đó.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| **ADR cần làm (D1)** — API của Object Pooling: hình dạng đăng ký/Get/Release, phiếu mượn có generation để chặn `staleRelease`, cơ chế ẩn và không mô phỏng (`SetActive` hay tắt renderer + `Rigidbody2D.simulated`), có dùng `UnityEngine.Pool.ObjectPool<T>` không, giao diện hook lấy ra/trả về (lý do `Released`/`Recycled`), đọc bộ đếm không cấp phát, đo chi phí Instantiate trên WebGL để xác nhận `prewarmPerFrame`. Làm cùng đợt ADR hiệu năng của Plinko. Feasibility chưa đối chiếu Unity 6000.3.16f1 (unity-reference không có module scripting/core). | technical-director → `game-adr` | 2026-11-04 |
| ~~**NEEDS-USER** — Hiệu ứng "tan" của xu kẹt dùng đối tượng nào, và Plinko trả xu lúc nào sau Resolved?~~ **Đã đóng (Revision 4) — user chọn B** (`Docs/production/session-logs/decision-log.md`): hiệu ứng tan dùng pool trang trí riêng `feedback.coinFizzle` của Feedback & Juice (RecycleOldest), xu về pool ngay khi Resolved, `pools[plinko.coin].headroom` = 0. Câu phụ "xu vào hũ có rơi tiếp trong hũ" cùng cách xử lý: hình ảnh sau Resolved thuộc Feedback & Juice, không giữ đối tượng xu. Nội dung câu hỏi gốc giữ lại để truy vết: (Plinko R9.1 + bảng trạng thái ghi Resolved → Pooled ngay, Visual/Audio ghi "xu mờ + co lại".) **A.** Plinko thêm trạng thái `Resolved → Fizzling → Pooled`, hiệu ứng chạy trên chính đồng xu ngoài trần `maxActiveCoins`; Plinko khai báo số xu `Fizzling` tối đa cùng lúc, `pools[plinko.coin].headroom` ≥ số đó (giữ 4 nếu ≤ 4). **B. (khuyến nghị)** Hiệu ứng tan dùng đối tượng của một pool trang trí riêng (ví dụ `feedback.coinFizzle`, RecycleOldest, do hệ trình bày hiệu ứng đăng ký); xu trả ngay ở Resolved → Pooled, `pools[plinko.coin].headroom` = 0 — giữ nguyên Plinko R9.1, pool Fixed không bao giờ cạn vì hiệu ứng, burst hiệu ứng chỉ bị cắt sớm (RecycleOldest) thay vì bỏ chạm. **C.** Hiệu ứng chạy trên đồng xu nhưng xu `Fizzling` vẫn tính vào `maxActiveCoins`; headroom 0, đổi lại bàn "bận" lâu hơn sau mỗi xu kẹt. Text phụ thuộc (ghi chú Pool MVP, knob headroom xu, ví dụ `pool_capacity`, hàng Plinko) đang *provisional*, giá trị tạm 4. Kèm câu hỏi: xu vào hũ có tiếp tục rơi trong hũ vài khung không (nếu có, cùng cách xử lý). | User → Plinko Board revision + Feedback & Juice (art-director) | 2026-10-21 (đã đóng) |
| Knob số hiệu ứng tan tối đa (`client_declared_max` của `feedback.coinFizzle`): tên *provisional* `PlinkoFeedbackConfig.fizzleMaxOnScreen` theo C2[13], giá trị và safe range do hệ sở hữu đặt (Plinko thêm vào asset tạm `PlinkoFeedbackConfig`, hoặc Feedback & Juice khi viết GDD). Tới lúc đó ví dụ dùng ký hiệu `F`, AC-34 *provisional*. | Plinko Board revision / Feedback & Juice GDD | 2026-10-28 |
| ~~Plinko cần ghi rõ hành vi khi `plinko.coin` trả rỗng (Fixed cạn).~~ **Đã đóng** — Plinko R4.5: xu coi như không được nhận (`Manual` → bỏ, phát `BoardBusy`, không dời mốc cooldown; `Auto` → bỏ nhịp; xu loạt → ở lại đầu hàng đợi), không mất xu, không phát `CoinResolved`. | Plinko Board | 2026-10-21 (đã đóng) |
| ~~Bootstrap & Scene Flow phải có pha "Prewarm pools" sau khi mọi hệ khách MVP đăng ký và trước `BootCompleted`.~~ **Đã đóng** — Bootstrap B2/B3: hợp đồng `IBootStep`, pha `Prewarm` sau `GameplayInit` và trước `BootCompleted`; Pooling đăng ký `pooling.service` (`ServiceInit`) và `pooling.prewarm` (`Prewarm`), C1[15] / Bootstrap Q5. | Bootstrap & Scene Flow | 2026-10-21 (đã đóng) |
| `floatingTextMaxOnScreen` (và knob hiệu ứng tan) chuyển sở hữu từ `PlinkoFeedbackConfig` sang asset của Feedback & Juice? Nguồn `client_declared_max` của `feedback.floatingText` / `feedback.coinFizzle` luôn là knob của hệ sở hữu đối tượng (hiện `PlinkoFeedbackConfig`, asset tạm do Plinko giữ — C2[13]); khi chuyển, chỉ nguồn đọc lúc đăng ký đổi, `PoolingConfig` không đổi. | Feedback & Juice GDD | 2026-10-28 |
| Pool trang trí của Gacha (hiệu ứng trứng), Shelf (hiệu ứng kệ), HUD (toast): `client_declared_max` và chính sách của từng pool; tổng phải ≤ `maxTotalPooledObjects`. | GDD Gacha Machine, Shelf & Synergy, HUD & Tab Navigation | Khi viết các GDD đó |
| Assumed: chỉ hai chính sách Fixed và RecycleOldest, không có Grow (Grow cấp phát khi cạn, trái AC "không cấp phát khi cạn" của D1). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: tên asset `PoolingConfig` chứa các `PoolDefinition` (id, prefab, policy, headroom) + `PoolingTestTargets` cho mục tiêu đo (brief: một nguồn data cho phần Pooling sở hữu; cùng mẫu PlinkoConfig/PlinkoBalanceTargets). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: framing chỉ hạ tầng, không Player Fantasy; Visual/Audio, Game Feel, UI ghi "Không áp dụng"; Cross-References đầy đủ (brief: Core/Foundation là infrastructure). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: trả kép / trả phiếu cũ / trả lạ bị bỏ qua, lỗi Editor, đếm vào chẩn đoán, không ném exception trong build (brief: bảo vệ "giải quyết đúng một lần" của Plinko mà không làm sập game). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: Pooling chỉ bảo đảm đối tượng ẩn, không mô phỏng và gọi hook lấy ra/trả về; hệ khách tự reset dữ liệu (brief: SRP; cơ chế → ADR). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: không bao giờ thu nhỏ pool trong phiên (brief: bộ nhớ < 1 MB, thu rồi lớn lại sinh GC). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: xu `JackpotBurst` dùng chung pool `plinko.coin`, biến thể sprite đặt ở hook lấy ra; xu Queued không chiếm đối tượng; hạt dùng ParticleSystem chung với `Emit()`, không thuộc Pooling (brief + Plinko). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: Pooling không dùng thời gian, không lưu trạng thái, không phụ thuộc Time & Timers / Save/Load; pool sống suốt phiên, trả sớm số bay khi tab ẩn do Feedback quyết định (brief). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: bộ đếm chẩn đoán active/peak/cạn/trả kép (mở rộng thêm `recycled`, `staleRelease`, `foreignRelease`, `lost`, `created`) đọc được từ test (brief). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: công thức `pool_capacity = client_declared_max + headroom`, prewarm đầy mọi pool, `prewarm_frames = ceil(total / prewarmPerFrame)` (brief); thêm `registration_valid` và `recycle_victim` để mọi luật có AC. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: test GC EditMode dùng `GC.GetAllocatedBytesForCurrentThread`, [Perf] WebGL dùng chung kịch bản với Plinko AC-38 (brief). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: headroom xu = 4 (không phải 0) vì Plinko Visual/Audio mô tả hiệu ứng tan chạy trên chính đồng xu; chi phí 4 đối tượng không đáng kể. Revision 1: giá trị này thành *provisional* — giữ tạm vì đúng với cả phương án A (≤ 4 xu tan cùng lúc) và B (thừa, vô hại); chốt theo câu NEEDS-USER về hiệu ứng tan. **Revision 4: thay thế** — user chọn B, headroom xu = 0. | game-designer | 2026-10-21 (đã thay thế) |
| Assumed: Pooling đăng ký **hai** bước cốt lõi (`pooling.service` ở `ServiceInit`, `pooling.prewarm` ở `Prewarm`) thay vì chỉ bước Prewarm như gợi ý C1[15], vì Bootstrap B2.2/B3 đã ghi đúng hai bước này và R3 cần trạng thái Accepting trước `GameplayInit`. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: cảnh báo Fixed cạn chỉ ghi ở lần cạn đầu mỗi pool mỗi phiên (các lần sau chỉ đếm) để phép đo 0 B không bị log làm nhiễu; đăng ký chỉ được phép trước bước prewarm (không prewarm muộn trong phiên, tránh cấp phát khi đang chơi). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed (Revision 2): "phiên Pooling" = một lần nạp scene `MainGame` (từ `Run()` của `pooling.service` tới khi scene bị dỡ), không phải một lần mở ứng dụng; "Thử lại" bỏ cả phiên kể cả khi Pooling đã Ready, prewarm lại từ đầu thay vì giữ pool cũ (Bootstrap B4.4: mọi trạng thái trong phiên bị bỏ; chi phí prewarm MVP ~4 khung). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed (Revision 2): cấm trạng thái `static` có thể ghi trong assembly Pooling, kiểm bằng reflection (AC-32) thay vì dựa vào domain reload — vì Enter Play Mode Options có thể tắt domain reload trong Editor; hình dạng cài đặt cụ thể (dịch vụ là component trong scene hay đối tượng do bước tạo) để ADR D1 chọn. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed (Revision 3): phạm vi AC-32 = namespace `CozyGachapon.Pooling` (dự án hiện chỉ có một asmdef runtime `CozyGachapon.Runtime` chứa mọi hệ); backing field của auto-property và event static vẫn bị kiểm dù mang `[CompilerGenerated]`, để `static int X { get; set; }` không lọt qua; `char` thêm vào danh sách cho phép cùng `bool`. Nếu ADR D1 tách asmdef Pooling, test đổi nguồn assembly, namespace giữ nguyên. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed (Revision 4): `pools[feedback.coinFizzle].headroom` = 0 (safe 0–4) và chưa gán số cho `client_declared_max` (ký hiệu `F`) — vì knob số hiệu ứng tan thuộc hệ sở hữu (D4), chưa có trong `plinko-board.md`; hiệu ứng tan tự là phần mờ dần nên khai báo `F` đã gồm hiệu ứng đang chạy, không cần phần dư. Ví dụ `F` = 4 chỉ để minh họa công thức. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed (Revision 4): prefab `feedback.coinFizzle` là prefab trang trí riêng (bản sao sprite xu, không Rigidbody2D), không phải prefab xu — để AC-28 "đúng một pool chứa prefab xu" vẫn đúng và hiệu ứng không tham gia vật lý. | game-designer | 2026-10-21 (đã áp dụng) |
