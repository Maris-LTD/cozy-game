# Bootstrap & Scene Flow

> **Status**: Approved
> **Author**: Maris-LTD + Claude (game-designer, workflow gdd-pipeline)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Hạ tầng — gián tiếp Pillar 1 (cú chạm đầu tiên nhanh, không giật) và tông cozy (rèm "Tiệm mở cửa", không chồng popup)
> **Decisions**: D1–D5 + prewarm pool của user, và quyết định "khi rewarded ad mở: Platform Services báo ad đang mở, Bootstrap không phát `Paused`" (B) (Docs/production/session-logs/decision-log.md, 2026-10-21)
> **Revision 4 — 2026-10-21** (theo review Round 3 `reviews/bootstrap-and-scene-flow-review-2026-10-21.md` và `gdd-consistency-2026-10-21-run2.json`): BSF-R3-01 dòng log `boot_to_interactive` ở mọi bản dựng (B5.3, AC-36); BSF-R3-02 sự kiện `Interactive` và luật mô phỏng chỉ chạy khi `Running` (B5.3, B6.5, AC-40); BSF-R3-03 yêu cầu build `runInBackground` và chỉ hướng dọc (B6.8, B8.4, AC-41); đóng Q1 theo quyết định B — cờ "ad đang mở" (B6.10, AC-42); BSF-R3-12 Tuning Knobs; C2[8] thứ tự `GameplayInit`; C2[16] bỏ ghi chú cross-doc lỗi thời.
> **Revision 5 — 2026-10-21**: BSF-R4-01 `AdClosed {adSeconds}` (B6.10.5, `ad_seconds`, AC-43); BSF-R4-02 `Resumed` ở Paused bất kể cờ ad (B6.10.1, AC-42); C3[12] bỏ Q6/Q8, "Hai chiều", ghi chú Cross-References cũ.
> [CD-GDD-ALIGN] skipped (lean)
> engine-programmer, game-designer (Formulas) và qa-lead (Acceptance Criteria) không được gọi: workflow không cho phép subagent; tác giả tự rà soát theo tiêu chí của các vai đó.

## Summary

Bootstrap & Scene Flow là lớp hạ tầng chạy trong scene duy nhất `MainGame`: nó điều phối các pha khởi động có thứ tự (ServiceInit → DataLoad → SaveLoad → GameplayInit → Prewarm → `BootCompleted`) qua một hợp đồng bước khởi động mà các hệ tự đăng ký, che toàn bộ quá trình bằng rèm "Tiệm mở cửa" và đưa người chơi tới cú chạm đầu trong `BootConfig.maxBootToInteractiveSeconds` (2.0 s). Sau khởi động, nó sở hữu vòng đời ứng dụng (`Paused`/`Resumed` chỉ khi tab ẩn / app xuống nền, kèm thời lượng vắng), `FirstInteraction`, tab đang mở (`ActiveTab`/`TabChanged`, khóa tab), input router đã lọc UI kèm cổng chặn input dạng token, khung dọc 9:16 giãn tới 9:21 có viền trang trí, và hàng đợi màn chào (MVP rỗng). Bootstrap không biết hệ nào cụ thể: nó chỉ biết các interface của mình, nên vẫn là Foundation dù hầu hết các hệ phụ thuộc nó.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None (upstream)`; được dùng bởi Plinko Board, Economy Core, Audio, Object Pooling, Pet Catalog, Feedback & Juice, HUD & Tab Navigation, Tutorial & Onboarding, Inventory, Gacha Machine, Shelf & Synergy; từ VS/Alpha: Save/Load, Time & Timers, Offline Earnings, Platform Services

## Overview

Bootstrap & Scene Flow là hệ hạ tầng (khung dữ liệu và điều phối, không phải cơ chế người chơi chủ động dùng) quyết định game "mở cửa" thế nào và giữ cho mọi hệ khác nhìn thấy cùng một trạng thái ứng dụng. Người chơi chỉ gặp nó gián tiếp: một tấm rèm kem/gỗ kéo lên trong chưa tới hai giây, ngay sau đó chạm vào bàn Plinko là xu rơi; chuyển giữa ba tab tức thì; bấm ra ngoài khung trên trang Poki thì bàn vẫn chạy, còn ẩn tab thì mọi thứ dừng lại nguyên trạng. Không có hệ này, mỗi hệ sẽ tự đoán thứ tự khởi tạo, tự nghe sự kiện nền tảng và tự lọc chạm — dẫn tới chạm xuyên qua nút UI vào bàn, popup chồng nhau ngay phút đầu, và thu nhập Idle chạy trong khi bàn Plinko đứng hình.

Phạm vi do quyết định của user (Boot D1–D5): Bootstrap sở hữu trình tự khởi động và rèm (D2), vòng đời ứng dụng (D3), trạng thái tab và khóa tab, input router và cổng chặn input (D1), khung tỉ lệ màn hình (D4), hàng đợi màn chào (D5). HUD & Tab Navigation chỉ vẽ thanh nav và gửi yêu cầu đổi tab. Cách hiện thực (event bus, cơ chế đăng ký, API Unity cho Page Visibility, cấu trúc scene) được ghi "→ ADR" và không thuộc GDD này. Hệ không có fantasy riêng (Foundation); nó phục vụ gián tiếp Pillar 1 (cú chạm đầu nhanh, không giật) và tông cozy (không màn loading kiểu "app", không chồng popup, không quảng cáo ép buộc).

## Detailed Design

### Core Rules

Mọi tên trong `backtick` dạng `Asset.field` là knob ở `BootConfig`, `SceneFlowConfig` (runtime) hoặc `BootTestTargets` (chỉ test), xem Tuning Knobs. Không có con số nào nằm trong logic.

**B1. Một scene, một điểm vào**
1. Game có đúng một scene gameplay `MainGame`. Bootstrap là thành phần đầu tiên chạy trong scene; việc có tách thêm scene Boot nhỏ hay không → ADR (không đổi hành vi trong GDD này).
2. Trước pha đầu tiên, Bootstrap tự đọc `BootConfig` và `SceneFlowConfig`, tính khung hình (B8) và dựng rèm "Tiệm mở cửa" ở trạng thái Covering ngay từ khung hình đầu tiên. Thiếu một trong hai asset là lỗi khởi động cốt lõi (B4) — bảng "Thử lại" vẫn hiện được vì nó không phụ thuộc asset cấu hình.

**B2. Hợp đồng bước khởi động** (C1[15]) — Bootstrap chỉ biết interface `IBootStep`, không tham chiếu hệ cụ thể nào:

| Trường | Ý nghĩa |
|---|---|
| `Id` | Chuỗi bất biến, duy nhất, tiền tố theo hệ (ví dụ `pooling.service`, `pooling.prewarm`, `pet.catalog`) |
| `Phase` | Một trong `ServiceInit`, `DataLoad`, `SaveLoad`, `GameplayInit`, `Prewarm` |
| `RunsAfter` | Danh sách `Id` cùng pha phải xong trước bước này (có thể rỗng) |
| `IsCritical` | Đúng → lỗi của bước dừng khởi động (B4); sai → bước bị bỏ qua, game vẫn vào |
| `Run()` | Bootstrap gọi đúng một lần khi tới lượt |
| `IsDone` / `IsFailed` | Bootstrap đọc mỗi khung sau `Run()`; một bước được phép kéo dài nhiều khung |

1. Các hệ tự đăng ký bước của mình trong lúc scene nạp (trước khung đầu). Đăng ký đóng khi pha ServiceInit bắt đầu; đăng ký muộn bị từ chối và báo lỗi Editor.
2. Một hệ có thể đăng ký nhiều bước ở các pha khác nhau (Object Pooling: `ServiceInit` tạo dịch vụ ở trạng thái Accepting, `Prewarm` tạo đối tượng).
3. Cơ chế đăng ký (C# interface + registry, ScriptableObject, attribute) → ADR khởi động.

**B3. Thứ tự pha** (minorChoices "Thứ tự các pha", C1[0], C1[15])
1. Các pha chạy nối tiếp, không chồng nhau, đúng thứ tự:

| # | Pha | Mục đích | Bước dự kiến (do từng hệ đăng ký) |
|---|---|---|---|
| 1 | `ServiceInit` | Dịch vụ nền không phụ thuộc dữ liệu | Object Pooling (dịch vụ → Accepting), Audio (khởi tạo, không cốt lõi) |
| 2 | `DataLoad` | Dữ liệu tĩnh chỉ đọc | Pet Catalog (nạp, lập bảng tra), các config asset của hệ khác |
| 3 | `SaveLoad` | Đọc dữ liệu đã lưu (từ VS) | Save/Load (VS). MVP: pha rỗng, đi qua ngay |
| 4 | `GameplayInit` | Hệ có trạng thái và hệ trình bày; các hệ khách đăng ký pool trong pha này | Thứ tự chỉ do các cạnh `RunsAfter` mà mỗi hệ tự khai: `economy.core` không có `RunsAfter` (Economy R16.1); Inventory `RunsAfter` `economy.core`; Shelf & Synergy `RunsAfter` `economy.core` và Inventory; HUD & Tab Navigation, Feedback & Juice `RunsAfter` các bước gameplay mà chúng đọc. Plinko Board và Gacha Machine **không** có `RunsAfter` (Plinko R9.2, Economy R16.2: Plinko kéo `baseCoinValue` lúc giải quyết xu và có dự phòng) — chúng chỉ được xếp theo thứ tự chữ của `Id` (B3.2). Các cạnh của hệ chưa có GDD là *provisional* |
| 5 | `Prewarm` | Prewarm **toàn bộ** pool đã đăng ký (Pool D5, quyết định user) | Object Pooling (`pooling.prewarm`) |
| — | `BootCompleted` | Phát sự kiện, rèm bắt đầu kéo lên (B5) | — |

2. Trong một pha, các bước chạy **lần lượt từng bước một** theo thứ tự tô-pô của `RunsAfter`; các bước không ràng buộc với nhau xếp theo thứ tự chữ của `Id` (ordinal) để kết quả luôn giống nhau mọi lần chạy.
3. Một pha không có bước nào kết thúc ngay trong cùng khung.
4. `RunsAfter` chỉ được trỏ tới bước **cùng pha**; ràng buộc khác pha đã do thứ tự pha bảo đảm. Trỏ tới `Id` không tồn tại, khác pha, hoặc tạo vòng → lỗi cấu hình: validator Editor báo lỗi; lúc chạy coi như lỗi khởi động cốt lõi (B4).
5. Pha `Prewarm` luôn đứng sau `GameplayInit`, nên mọi hệ khách MVP (gameplay và trình bày) đã đăng ký pool trước khi Pooling đóng đăng ký (khớp Object Pooling R4.1).
6. Bootstrap không bao giờ phát `BootCompleted` khi còn bước cốt lõi chưa xong.

**B4. Lỗi khởi động** (fix list, minorChoices "Lỗi khởi tạo")
1. Một bước **lỗi** khi `IsFailed` đúng, khi `Run()` ném ngoại lệ, hoặc khi chưa xong sau `BootConfig.stepTimeoutSeconds` (công thức `step_timed_out`).
2. Bước không cốt lõi (`IsCritical` = sai — Audio; Feedback & Juice đề xuất là không cốt lõi, GDD của nó chốt) lỗi → Bootstrap ghi vào báo cáo khởi động, bỏ qua và chạy bước kế tiếp. Các bước có `RunsAfter` trỏ tới bước đó vẫn chạy. Audio tự vào trạng thái Failed và game chạy tiếp không tiếng (khớp `audio.md`).
3. Bước cốt lõi lỗi → khởi động dừng ngay (không chạy bước nào nữa), vòng đời vào **BootFailed**, phát `BootFailed {stepId, phase}`; rèm giữ nguyên và bảng "Thử lại" tông cozy hiện trên rèm sau `BootConfig.errorPanelFadeSeconds`. Không bao giờ để người chơi ở màn trắng hoặc rèm đứng yên vô hạn.
4. Bấm "Thử lại" → nạp lại scene `MainGame` từ đầu (mọi trạng thái trong phiên bị bỏ; từ VS, không có gì được ghi xuống save trong lúc BootFailed). Không giới hạn số lần thử lại.

**B5. Rèm "Tiệm mở cửa"** (Boot D2)
1. Rèm là lớp phủ toàn khung màu kem/gỗ, hiện từ khung đầu, giữ một token cổng chặn (B7) và chặn mọi chạm vào UI bên dưới.
2. Rèm bắt đầu kéo lên khi **cả ba** đúng: đã phát `BootCompleted`; rèm đã hiện ít nhất `BootConfig.curtainMinDisplaySeconds` (chống nháy trên máy nhanh); trang/app đang hiển thị (không ở Paused, B6.6). Kéo lên kéo dài `BootConfig.curtainRevealDuration`.
3. Kéo xong → rèm nhả token, vòng đời từ `Booting.Curtain` vào **Running**, Bootstrap phát `Interactive` (đúng một lần mỗi lần nạp scene `MainGame`, trong cùng khung, sau khi nhả token), game **tương tác được**. Mốc này là điểm cuối của ngân sách `boot_to_interactive` (Formulas).
4. **Trang ẩn khi rèm đang kéo** (Revealing): vòng đời phát `Paused` (B6.4), rèm vào **RevealFrozen** — đứng yên ở đúng tiến độ đang có, vẫn giữ token, không quay lại Covering. Khi `Resumed`, rèm kéo tiếp từ tiến độ đó trong phần thời lượng còn lại (`curtainRevealDuration` trừ phần đã kéo theo đồng hồ khởi động, B6.6). Thời gian trang ẩn không tính vào `boot_to_interactive`.
5. Không có thanh tiến độ, phần trăm hay logo động; không có âm thanh khi rèm kéo lên ở MVP (Audio trên WebGL còn khóa trước `FirstInteraction`).
6. **Dòng log `boot_to_interactive`** (BSF-R3-01): trong cùng khung phát `Interactive`, **mọi bản dựng, kể cả release**, ghi đúng một dòng log chứa tên `boot_to_interactive` và giá trị của nó (giây, đo bằng đồng hồ khởi động B6.6, độ phân giải mili-giây) ra console trình duyệt (WebGL) hoặc log thiết bị (Android/iOS). Không ghi dòng này ở BootFailed; mỗi lần nạp lại scene sau "Thử lại" ghi lại đúng một dòng của lần nạp đó. Chỉ lớp phủ debug và thời gian từng bước của báo cáo khởi động là dev-only (UI Requirements); dòng log này là nguồn đo của AC-36. Định dạng chính xác của dòng → ADR khởi động.

**B6. Vòng đời ứng dụng** (Boot D3)
1. Trạng thái: `Booting` (hai trạng thái con: `Booting.Steps` — đang chạy các pha, trước `BootCompleted`; `Booting.Curtain` — đã phát `BootCompleted`, rèm chưa kéo xong), `BootFailed`, `Running`, `Paused`, `Quitting` (States and Transitions).
2. `Paused {reason}` phát **chỉ** khi trang trình duyệt chuyển sang bị ẩn (`reason = Hidden`) hoặc app di động xuống nền (`reason = Background`), và không phát khi cờ "ad đang mở" đang bật (B6.10). Cửa sổ chỉ mất focus mà vẫn hiển thị (bấm ra ngoài canvas, mở thanh địa chỉ) **không** phát gì: mọi mô phỏng chạy tiếp.
3. `Resumed {reason, awaySeconds}` phát khi trang/ứng dụng hiển thị lại; `awaySeconds` theo công thức `away_seconds` (giờ thực, không âm). Bootstrap không cộng, không trần, không quy đổi thu nhập: Offline Earnings (VS) quyết định.
4. Mỗi lần ẩn chỉ có một `Paused` và một `Resumed` tương ứng; tín hiệu trùng (ẩn khi đang ẩn) bị bỏ. `Paused`/`Resumed` chỉ phát khi vòng đời đã qua `BootCompleted`, tức ở `Booting.Curtain` hoặc `Running`; không bao giờ phát ở `Booting.Steps`, `BootFailed` hay `Quitting` (B6.6).
5. **Mô phỏng chỉ chạy khi `Running`** (BSF-R3-02): mọi hệ mô phỏng (Plinko, Economy Idle, timer) chỉ tiến thời gian mô phỏng khi vòng đời là `Running`. Bootstrap cung cấp truy vấn `IsSimulationRunning` (đúng ⇔ vòng đời = `Running`) và các sự kiện chuyển: `Interactive` (lần đầu vào Running, B5.3), `Paused` (rời Running hoặc `Booting.Curtain`), `Resumed` (về trạng thái đã ghi, B6.9 — nếu về `Booting.Curtain` thì mô phỏng vẫn chưa chạy cho tới `Interactive`). Hệ nghe sự kiện phải đọc `IsSimulationRunning` sau `Resumed` thay vì coi `Resumed` là "chạy lại". Vì vậy không có Idle, auto-drop hay timer nào tiến dưới rèm (`Booting.Steps`, `Booting.Curtain`), ở `BootFailed` hay `Quitting`. Audio tự tắt/mở tiếng qua mixer của nó. Bootstrap không quy định `Time.timeScale` hay cách dừng → ADR (minorChoices "Cách tạm dừng").
6. **Đồng hồ khởi động** là giờ thực kể từ khung đầu của `MainGame`, **chỉ đếm khi trang/app hiển thị**; mọi mốc của rèm và `boot_to_interactive` đo bằng đồng hồ này. Theo từng trạng thái con:
   - trang bị ẩn trong `Booting.Steps`: khởi động vẫn đi tiếp khi engine còn chạy khung (trình duyệt có thể bóp khung); không phát `Paused`. Nếu trang hiện lại trước `BootCompleted` thì không có sự kiện vòng đời nào;
   - `BootCompleted` phát khi trang vẫn ẩn: vòng đời vào `Booting.Curtain` rồi phát `Paused {Hidden/Background}` ngay trong cùng khung, sau `BootCompleted`; rèm giữ Covering; `Resumed` phát khi hiện lại với `awaySeconds` tính từ lúc `Paused` đó;
   - trang bị ẩn trong `Booting.Curtain` (rèm Covering hoặc Revealing): phát `Paused` như ở Running; rèm Covering đứng chờ, rèm Revealing vào RevealFrozen (B5.4).
7. `Quitting` phát một lần khi nền tảng báo ứng dụng sắp đóng (best-effort: WebGL có thể không báo). Hệ cần lưu (Save/Load, VS) phải lưu ở `Paused`, không chỉ dựa vào `Quitting`.
8. Bật chạy nền trên WebGL và phát hiện trang ẩn (Page Visibility qua jslib), `OnApplicationPause` trên mobile → ADR vòng đời. **Yêu cầu build** (BSF-R3-03): Player Settings phải có `runInBackground` = đúng — với giá trị sai, bản WebGL ngừng chạy khung khi canvas mất focus, trái B6.2 ("chỉ mất focus vẫn chạy"). Cài đặt này là yêu cầu của GDD, được kiểm bằng AC-41; giá trị hiện tại của dự án cần sửa ở bước thiết lập (game-setup / ADR build).
9. **Trạng thái trả về**: khi vào `Paused`, Bootstrap ghi lại trạng thái trước đó (`Booting.Curtain` hoặc `Running`). `Resumed` luôn đưa vòng đời về đúng trạng thái đã ghi: từ `Booting.Curtain` thì rèm tiếp tục (B5.2, B5.4) và chỉ vào Running khi rèm kéo xong; từ `Running` thì về Running.
10. **Cờ "ad đang mở"** (quyết định user B, đóng Q1): Platform Services gọi `SetAdOpen(true)` ngay trước khi hiện một quảng cáo thưởng (rewarded ad) và `SetAdOpen(false)` khi quảng cáo đóng; Bootstrap cung cấp truy vấn `IsAdOpen`. Cờ là bool, không đếm lồng: gọi `true` lần hai không có tác dụng thêm, một lời gọi `false` tắt cờ.
    1. Khi cờ bật, tín hiệu nền tảng "ẩn" / "xuống nền" **không** phát `Paused`; vòng đời và `IsSimulationRunning` giữ nguyên, nên mô phỏng chạy tiếp ở mọi nền tảng. Cờ chỉ chặn việc **vào** Paused (BSF-R4-02): khi vòng đời đã ở `Paused`, tín hiệu "hiện lại" luôn phát `Resumed` bất kể cờ; nếu không ở Paused thì "hiện lại" không phát gì. Trên mobile engine có thể không chạy khung khi activity quảng cáo che; thời gian đó được bù bằng `AdClosed` (mục 5), không bằng `Resumed`.
    2. Khi cờ tắt mà nền tảng vẫn đang ẩn / ở nền, Bootstrap phát `Paused {reason}` theo trạng thái hiện tại ngay trong khung đó (theo luật B6.2–B6.4 như một tín hiệu ẩn mới); `awaySeconds` của `Resumed` sau đó tính từ `Paused` này. Khi cờ tắt mà trang đang hiển thị → không phát gì.
    3. Bootstrap không tự giữ token cổng chặn, không tắt tiếng và không hẹn giờ tự tắt cờ: tắt tiếng là `SetMuteReason(Ad, bool)` của Audio do Platform Services gọi; chặn chạm (nếu cần) là token B7 do Platform Services giữ; tắt cờ đúng lúc là trách nhiệm của Platform Services (bản dev hiển thị cờ trên lớp phủ debug).
    4. Cờ không ảnh hưởng `Quitting`, `FirstInteraction`, `TabChanged` hay router.
    5. **`AdClosed {adSeconds}`** (quyết định user b, đóng BSF-R4-01): khi `SetAdOpen(false)` tắt một cờ đang bật, Bootstrap phát đúng một `AdClosed` với `adSeconds` theo `ad_seconds` (dùng lại luật làm sạch của `away_seconds`); `false` khi cờ đã tắt không phát gì. Trong cùng khung, `AdClosed` phát **trước** `Paused` của mục 2. Bootstrap không cộng, không trần, không quy đổi: Economy Core (bù Idle) và Offline Earnings (VS, trần/hệ số) quyết định; phần thời gian engine đã mô phỏng thật khi cờ bật (web) do hệ tiêu thụ trừ đi để không bù hai lần.

**B7. Cổng chặn input dạng token** (Boot D1, C1[19])
1. Bootstrap là **cổng chặn toàn cục duy nhất** cho chạm vào world: `AcquireInputBlock(owner)` trả một token; `Release(token)` nhả. Còn ít nhất một token → mọi chạm world bị chặn (không tới hệ nhận nào). Chủ token điển hình: rèm (B5), bảng "Thử lại" (B4), popup/modal (HUD), bước màn chào (B10), Tutorial, Gacha lúc mở trứng.
2. Token chỉ chặn chạm world. Chạm vào UI do chính UI quyết định (popup tự có lớp chặn raycast của nó); rèm và bảng lỗi phủ toàn khung nên chặn cả UI.
3. Nhả cùng token hai lần, hoặc nhả token không hợp lệ → bỏ qua, không lỗi runtime (cảnh báo Editor).
4. Plinko **không** có cổng riêng: nó chỉ có cờ `acceptManualTaps` (bật/tắt nhận chạm `Manual`) để Tutorial dùng, và không tự lọc UI hay modal. Tutorial muốn chặn toàn bộ world dùng token của Bootstrap; muốn chỉ tắt thả xu tay thì dùng cờ của Plinko (ranh giới C1[19]).

**B8. Khung dọc cố định** (Boot D4)
1. Vùng an toàn thiết kế là 9:16 ở độ phân giải tham chiếu `SceneFlowConfig.referenceWidth` × `SceneFlowConfig.referenceHeight`. Khung nội dung được tính bằng công thức `frame_rect`:
   - màn rộng hơn `SceneFlowConfig.maxAspect` (desktop 16:9, iPad 3:4) → khung cao bằng màn, rộng = cao × `maxAspect`, đặt giữa; hai bên là **viền trang trí** kem/gỗ;
   - màn trong khoảng [`minAspect`, `maxAspect`] → khung = toàn màn; phần cao dư so với 9:16 được trao cho vùng nội dung (HUD/từng tab quyết định dùng thế nào);
   - màn hẹp hơn `minAspect` (dài hơn 9:21) → khung rộng bằng màn, cao = rộng / `minAspect`, đặt giữa; trên dưới là viền trang trí.
2. Bootstrap tính lại khung khi kích thước màn hoặc vùng an toàn (tai thỏ) đổi, và phát `FrameChanged {frameRectPx, frameScale, extraHeightRef, safeInsetsPx}` trước khung hình kế tiếp được vẽ. HUD đặt header/nav theo `safeInsetsPx`; Plinko khớp camera theo `frameRectPx`.
3. Chạm rơi vào viền (ngoài khung) bị bỏ: không tới UI, không tới world (vẫn tính cho `FirstInteraction`).
4. Màn ngang trên mobile không được hỗ trợ: khóa hướng dọc trong Player Settings (→ ADR build). Không có bố cục ngang riêng. **Yêu cầu build** (BSF-R3-03): hướng màn mặc định là `Portrait` (chỉ dọc, không `AutoRotation`, không xoay ngang trái/phải); kiểm bằng AC-41. Giá trị hiện tại của dự án cần sửa ở bước thiết lập (game-setup / ADR build).

**B9. Tab và input router** (Boot D1, minorChoices "Tab mặc định", "Cách chuyển tab", "Tab không hiển thị", "Thời điểm nhận chạm", "Đa chạm", "Chuyển tọa độ")
1. Có đúng ba tab cố định `Board` (Tab 1 — Bàn Chơi), `Shelf` (Tab 2 — Kệ), `Shop` (Tab 3 — Tiệm). Bootstrap giữ `ActiveTab`; khởi động luôn vào `SceneFlowConfig.defaultTab` (= `Board`), quay lại sau `Resumed` giữ tab đang mở.
2. Đổi tab chỉ qua `RequestTab(tab)` (HUD gọi khi bấm bottom nav; Tutorial có thể gọi). Không có chuyển tab bằng vuốt. Kết quả: `Changed` (phát `TabChanged {previous, current}`), `NoChange` (đã ở tab đó, không phát sự kiện), `Locked` (bị khóa, không phát sự kiện).
3. **Khóa tab**: `AcquireTabLock(owner)` trả token; còn token → mọi `RequestTab` sang tab khác bị `Locked`, trừ khi lời gọi mang đúng token của chủ khóa và đó là khóa duy nhất. Chính sách khi nào khóa (ví dụ lúc mở trứng) do GDD Gacha / Tutorial quyết định.
4. Tab không hiển thị **giữ sống**: Bootstrap chỉ tắt hiển thị các gốc hình ảnh mà tab đã đăng ký (Canvas của tab, camera world nếu có); logic và mô phỏng chạy tiếp (Plinko R11.1). Hiển thị tab mới và ẩn tab cũ xảy ra trong cùng một khung, trong `SceneFlowConfig.tabSwitchMaxMs` kể từ khung Input System xử lý cú nhả (click) của nút nav (công thức `tab_switch_ms`).
5. **Input router** — mỗi khung, với cú nhấn (press) **đầu tiên mới bắt đầu** trong khung đó (theo thứ tự sự kiện của Input System; các ngón khác cùng khung bị bỏ, minorChoices "Đa chạm"):
   1. ngoài khung (viền) → bỏ;
   2. trúng phần tử UI có nhận raycast → giao cho UI, **không** tới world;
   3. còn token cổng chặn (B7) → bỏ;
   4. trúng một **viewport world** đã đăng ký của `ActiveTab` (MVP: `PlinkoViewport` của Plinko ở `Board`) → đổi tọa độ màn hình sang world bằng camera mà hệ nhận cung cấp lúc đăng ký, giao `WorldPress {worldPos, screenPos}` cho hệ đó; nếu không trúng viewport nào → bỏ.
   Quy tắc gộp là công thức `world_press_routed`.
6. Router nhận ở lúc **nhấn xuống**, không chờ nhả; giao `WorldPress` trong cùng khung đọc input, tức thêm không quá `BootTestTargets.routerMaxAddedFrames` khung trễ (giữ `PlinkoBalanceTargets.tapToSpawnMaxMs` = 50 ms). Nút UI vẫn kích hoạt theo click chuẩn của UI.
7. Router không có cooldown hay trần riêng: cooldown chạm và trần xu là của Plinko (`tap_cooldown`, `max_active_coins` trong registry).
8. Cách router hit-test UI phụ thuộc framework UI (uGUI hay UI Toolkit) → cần ADR UI trước khi hiện thực router.

**B10. Hàng đợi màn chào** (Boot D5)
1. Các hệ (Offline Earnings, Tutorial, thông báo — từ VS) đăng ký bước màn chào `IWelcomeStep {Id, ShouldShow(), Show(onDone)}`. MVP không có bước nào.
2. Thứ tự chạy = danh sách `SceneFlowConfig.welcomeOrder` (dữ liệu, không phải số ưu tiên trong code); bước không có trong danh sách chạy sau các bước có tên, theo thứ tự chữ của `Id`, và validator cảnh báo.
3. Hàng đợi bắt đầu khi rèm kéo xong (B5.3). Mỗi lúc chỉ một bước hiện; bước nào `ShouldShow()` sai thì bỏ qua. Trong lúc một bước hiện, Bootstrap giữ một token cổng chặn và nhả khi bước gọi `onDone`.
4. Hết hàng đợi → phát `WelcomeQueueCompleted` (MVP: ngay khi rèm kéo xong). Tutorial (VS) bắt đầu sau sự kiện này.
5. Bước đăng ký sau khi hàng đợi đã kết thúc bị bỏ trong phiên đó (cảnh báo Editor); không có popup "chen" giữa phiên qua hàng đợi này.

**B11. Nút Back Android / Esc trên web** (minorChoices "Nút Back")
1. Bootstrap giữ một ngăn xếp trình xử lý Back (`PushBackHandler` / `PopBackHandler`); modal và bước màn chào đẩy trình xử lý của mình.
2. Bấm Back: ngăn xếp không rỗng → gọi trình xử lý trên cùng (thường là đóng modal; Tutorial có thể nuốt phím); rỗng và `ActiveTab` ≠ `Board` → `RequestTab(Board)`; rỗng và đang ở `Board` → Android đưa app xuống nền, web không làm gì. Không có popup hỏi thoát.
3. Back bị bỏ khi vòng đời là `Booting` (cả `Booting.Steps` lẫn `Booting.Curtain`); ở `BootFailed`, Back trên Android đưa app xuống nền.

**B12. Hợp đồng với cổng game** (minorChoices "Hợp đồng vòng đời")
1. Bootstrap phát `BootCompleted`, `Interactive`, `FirstInteraction`, `Paused`, `Resumed`; Platform Services (Alpha) đặt cờ "ad đang mở" (`SetAdOpen`, B6.10) và tự ánh xạ sang sự kiện loading/gameplay của Poki/CrazyGames.
2. Bootstrap **không bao giờ** phát yêu cầu quảng cáo hay tạo "điểm nghỉ" có thể bị ánh xạ thành quảng cáo ép buộc (Anti-Pillar).
3. `FirstInteraction` phát đúng một lần mỗi phiên, ở cú nhấn hoặc phím đầu tiên nhận được từ khi scene nạp, bất kể nó rơi vào UI, world, viền hay đang bị chặn bởi rèm (một cử chỉ người dùng là đủ để mở khóa âm thanh WebGL, Audio A10).

**Hệ thống không thể**: tham chiếu trực tiếp một hệ gameplay; phát `Paused` khi cửa sổ chỉ mất focus; giao hai chạm world trong cùng khung; giao chạm world khi còn token; để người chơi kẹt sau lỗi khởi động không có nút "Thử lại"; bật popup hay quảng cáo ngoài hàng đợi màn chào.

### States and Transitions

Vòng đời ứng dụng:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Booting.Steps | Scene `MainGame` nạp | Mọi bước cốt lõi xong → phát `BootCompleted` → Booting.Curtain; bước cốt lõi lỗi → BootFailed; nền tảng báo đóng → Quitting | Chạy các pha theo B3; rèm Covering giữ token; **không** phát `Paused`/`Resumed` (trang ẩn chỉ dừng đồng hồ khởi động, B6.6); Back bị bỏ; `FirstInteraction` vẫn có thể phát |
| Booting.Curtain | `BootCompleted` vừa phát; hoặc `Resumed` khi trạng thái trả về là Booting.Curtain (B6.9) | Rèm kéo xong (Open) → Running; trang ẩn / app xuống nền → phát `Paused`, vào Paused (ghi trả về = Booting.Curtain); nền tảng báo đóng → Quitting | Không chạy bước nào nữa; rèm Covering hoặc Revealing giữ token; router không giao chạm world; Back bị bỏ; `RequestTab` → `Locked` |
| BootFailed | Bước cốt lõi lỗi (B4) | Bấm "Thử lại" → nạp lại scene (về Booting.Steps); nền tảng báo đóng → Quitting | Không chạy bước nào nữa; rèm giữ nguyên, bảng "Thử lại" hiện; không phát `BootCompleted`, `Paused`, `Resumed` |
| Running | Rèm kéo xong (từ Booting.Curtain, phát `Interactive`); hoặc `Resumed` khi trạng thái trả về là Running (B6.9) | Trang ẩn / app xuống nền khi cờ "ad đang mở" tắt → phát `Paused`, vào Paused (ghi trả về = Running); cờ tắt mà nền tảng vẫn ẩn → như trên (B6.10.2); nền tảng báo đóng → Quitting | `IsSimulationRunning` = đúng, mô phỏng chạy (B6.5); router giao chạm; tab đổi được; hàng đợi màn chào chạy (B10); ẩn/hiện khi cờ "ad đang mở" bật không đổi trạng thái |
| Paused | Trang ẩn hoặc app xuống nền khi đang Booting.Curtain hoặc Running và cờ "ad đang mở" tắt (B6.10); hoặc trang vẫn ẩn lúc `BootCompleted` (vào Booting.Curtain rồi Paused trong cùng khung, B6.6) | Trang/app hiển thị lại → phát `Resumed`, về đúng trạng thái đã ghi (Booting.Curtain hoặc Running, B6.9); nền tảng báo đóng → Quitting | Đã phát `Paused` đúng một lần; mọi hệ mô phỏng dừng; đồng hồ khởi động dừng; rèm (nếu chưa Open) đứng yên ở Covering hoặc RevealFrozen; router không giao gì (không có input) |
| Quitting | Nền tảng báo sắp đóng (từ bất kỳ trạng thái nào trừ chính nó) | Không thoát | Đã phát `Quitting` một lần; không nhận chạm; không phát `Paused`/`Resumed` |

Pha khởi động (bên trong Booting.Steps):

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| ServiceInit → DataLoad → SaveLoad → GameplayInit → Prewarm | Pha trước kết thúc (pha đầu: sau khi đọc config, B1.2) | Mọi bước của pha xong hoặc (không cốt lõi) lỗi → pha kế tiếp; bước cốt lõi lỗi → BootFailed | Chạy từng bước theo thứ tự tô-pô `RunsAfter`; mỗi bước có hạn `stepTimeoutSeconds` |
| Completed | Pha Prewarm kết thúc | — | Đã phát `BootCompleted` đúng một lần; vòng đời sang Booting.Curtain |

Rèm "Tiệm mở cửa":

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Covering | Khung hình đầu tiên | `BootCompleted` đã phát **và** rèm đã hiện ≥ `curtainMinDisplaySeconds` (đồng hồ khởi động) **và** không Paused → Revealing; BootFailed → ErrorShown | Phủ toàn khung, giữ token cổng chặn, chặn UI; trong Paused đứng chờ |
| Revealing | Như trên; hoặc `Resumed` từ RevealFrozen | Đã kéo đủ `curtainRevealDuration` (cộng dồn, đồng hồ khởi động) → Open; `Paused` → RevealFrozen | Kéo lên; vẫn giữ token tới hết |
| RevealFrozen | `Paused` khi đang Revealing (B5.4) | `Resumed` → Revealing (kéo tiếp từ tiến độ đã lưu) | Đứng yên ở tiến độ hiện tại, không quay về Covering; giữ token |
| Open | Kéo xong | Không thoát trong phiên | Nhả token; vòng đời → Running; hàng đợi màn chào bắt đầu |
| ErrorShown | BootFailed | Bấm "Thử lại" (nạp lại scene) | Bảng "Thử lại" hiện trên rèm sau `errorPanelFadeSeconds` |

Tab: mỗi tab ở `Visible` (đúng một tab — `ActiveTab`) hoặc `Hidden` (gốc hình ảnh tắt, logic chạy). `RequestTab` hợp lệ đổi hai tab trong cùng một khung. Cổng chặn: `Open` (0 token) ↔ `Blocked` (≥ 1 token). Hàng đợi màn chào: `Waiting` (trước khi rèm Open) → `Running` (một bước hiện tại một lúc) → `Done` (đã phát `WelcomeQueueCompleted`).

### Interactions with Other Systems

Bootstrap không gọi API riêng của hệ nào; mọi tương tác đi qua interface và sự kiện do Bootstrap sở hữu. "Bước" = `IBootStep` mà hệ đó tự đăng ký.

| Hệ | Hệ đó cung cấp cho Bootstrap | Bootstrap cung cấp | Chủ interface |
|---|---|---|---|
| Object Pooling | Bước `ServiceInit` (dịch vụ → Accepting) và bước `Prewarm` (prewarm toàn bộ pool, xong → Ready), cả hai cốt lõi | Thứ tự pha bảo đảm mọi hệ khách đã đăng ký pool trước Prewarm; rèm che các khung prewarm | Bootstrap (hợp đồng pha); Pooling (nội dung bước prewarm, `prewarmPerFrame`, `prewarmMaxSeconds`) |
| Audio | Bước `ServiceInit` **không cốt lõi** (kết quả Ready / Failed) | `Paused` / `Resumed`, `FirstInteraction`, `Interactive` (mốc bắt đầu nhạc từ VS, B5.3) | Bootstrap (sự kiện); Audio (phản ứng, mixer) |
| Pet Catalog | Bước `DataLoad` cốt lõi (nạp, lập bảng tra → Ready) | Thứ tự: Catalog Ready trước mọi bước `GameplayInit` (Inventory, Gacha, Shelf) | Bootstrap (thứ tự); Catalog (nội dung nạp) |
| Economy Core | Bước `GameplayInit` cốt lõi (Uninitialized → Running; từ VS nhận dữ liệu từ save vì `SaveLoad` đứng trước) | `Paused` / `Resumed` (dừng/tiếp cộng Idle), `Interactive` + `IsSimulationRunning` (chỉ cộng Idle khi vòng đời `Running`, B6.5), `AdClosed {adSeconds}` (bù Idle, B6.10.5), thứ tự khởi tạo | Bootstrap |
| Inventory | Bước `GameplayInit` cốt lõi, `RunsAfter` Economy (provisional — GDD chưa có) | Thứ tự khởi tạo | Bootstrap |
| Plinko Board | Bước `GameplayInit` cốt lõi (đăng ký pool `plinko.coin`); đăng ký viewport world `PlinkoViewport` + camera Plinko vào router; đăng ký gốc hình ảnh của tab `Board` | `WorldPress {worldPos, screenPos}` đã lọc UI và cổng chặn; `ActiveTab` / `TabChanged`; `Paused` / `Resumed`; `Interactive` + `IsSimulationRunning` (mô phỏng và auto-drop chỉ tiến khi `Running`, B6.5); `FrameChanged` | Bootstrap (router, tab, vòng đời); Plinko (cờ `acceptManualTaps`, cooldown, trần xu) |
| Gacha Machine | Bước `GameplayInit` (provisional); giữ/nhả token cổng chặn và (tùy GDD Gacha) khóa tab lúc mở trứng | `AcquireInputBlock`, `AcquireTabLock`, `ActiveTab` | Bootstrap (cơ chế); Gacha (chính sách khi nào khóa) |
| Shelf & Synergy | Bước `GameplayInit`, `RunsAfter` Economy và Inventory (provisional); gốc hình ảnh của tab `Shelf` | `ActiveTab` / `TabChanged`, router cho kéo thú (nếu Shelf dùng viewport world; UI thì không cần) | Bootstrap |
| HUD & Tab Navigation | Bước `GameplayInit` (trình bày, sau gameplay); gọi `RequestTab` khi bấm nav; đẩy trình xử lý Back cho modal; giữ token cổng chặn khi modal mở | `ActiveTab` / `TabChanged` (để tô nút nav), kết quả `RequestTab` (`Locked` → phản hồi nhẹ), `FrameChanged` (vùng an toàn) | Bootstrap (trạng thái tab, khóa); HUD (vẽ nav, modal) |
| Feedback & Juice | Bước `GameplayInit` đề xuất không cốt lõi (đăng ký pool số bay) | `TabChanged` (tắt số bay khi bàn ẩn, gọi `SetBusBackground(Plinko, …)` của Audio) | Bootstrap (sự kiện); Feedback (phản ứng) |
| Tutorial & Onboarding (VS) | Bước màn chào (B10); giữ token cổng chặn và khóa tab trong các bước hướng dẫn; có thể gọi `RequestTab` | `WelcomeQueueCompleted`, `AcquireInputBlock`, `AcquireTabLock`, `ActiveTab` | Bootstrap (cơ chế); Tutorial (kịch bản) |
| Save/Load (VS) | Bước `SaveLoad` cốt lõi (đọc save) | `Paused` (mốc lưu chính), `Quitting` (best-effort) | Bootstrap |
| Time & Timers (VS) | Bước `ServiceInit` | `Paused` / `Resumed {awaySeconds}` | Bootstrap (sự kiện); Time (đồng hồ chống delta âm) |
| Offline Earnings (VS) | Bước màn chào (popup thu nhập vắng mặt) | `Resumed {awaySeconds}`, `AdClosed {adSeconds}` | Bootstrap (sự kiện); Offline (trần, hệ số) |
| Platform Services (Alpha) | Bước `ServiceInit` (khởi tạo SDK); `SetAdOpen(true/false)` quanh mỗi rewarded ad (B6.10); gọi `SetMuteReason(Ad, bool)` của Audio | `BootCompleted`, `Interactive`, `FirstInteraction`, `Paused`, `Resumed`, `BootFailed`; `IsAdOpen` | Bootstrap (sự kiện, cờ và luật không phát `Paused` khi cờ bật); Platform (đặt/tắt cờ đúng lúc, ánh xạ sang SDK cổng game) |

→ ADR khởi động & vòng đời: cơ chế đăng ký `IBootStep`, event bus, Page Visibility trên WebGL (jslib + Run In Background), `OnApplicationPause`/`OnApplicationQuit` trên mobile, cách tắt hiển thị tab, nguồn giờ thực cho `away_seconds`. → ADR UI: hit-test UI của router theo framework UI được chọn.

## Formulas

Mốc thời gian khởi động đo bằng giờ thực kể từ khung hình đầu tiên của scene `MainGame` (`t = 0`), không gồm thời gian tải HTML/loader WebGL (thuộc Platform Services/build, Boot D2). Các công thức không dùng số trần trong code; mọi hằng số đều là knob ở Tuning Knobs.

### boot_to_interactive

`reveal_start = max(t_boot_completed, curtainMinDisplaySeconds)`
`boot_to_interactive = reveal_start + curtainRevealDuration`
Kiểm tra ngân sách: `boot_to_interactive ≤ maxBootToInteractiveSeconds`; suy ra ngân sách cho các bước: `boot_work_budget = maxBootToInteractiveSeconds − curtainRevealDuration`.
Ràng buộc cấu hình (validator): `curtainMinDisplaySeconds + curtainRevealDuration < maxBootToInteractiveSeconds`.
Mọi mốc trong công thức này đo bằng **đồng hồ khởi động** (B6.6: chỉ đếm khi trang hiển thị). Thời gian Paused trong `Booting.Curtain` — kể cả lúc rèm ở RevealFrozen (B5.4) — không cộng vào `reveal_start` lẫn `curtainRevealDuration`, nên công thức giữ nguyên khi trang bị ẩn giữa chừng.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| t_boot_completed | float (s) | 0 – không trần | calculated | Lúc phát `BootCompleted`, tính từ khung đầu, **chỉ đếm thời gian trang hiển thị** (B6.6) |
| curtainMinDisplaySeconds | float (s) | 0.3–0.5 | SO field `BootConfig.curtainMinDisplaySeconds` | Thời gian rèm hiện tối thiểu, chống nháy |
| curtainRevealDuration | float (s) | 0.2–0.5 | SO field `BootConfig.curtainRevealDuration` | Thời lượng kéo rèm lên |
| maxBootToInteractiveSeconds | float (s) | 1.0–3.0 | SO field `BootConfig.maxBootToInteractiveSeconds` | Ngân sách khung đầu → tương tác được (Boot D2) |
| reveal_start | float (s) | ≥ `curtainMinDisplaySeconds` | calculated | Lúc rèm bắt đầu kéo lên |
| boot_to_interactive | float (s) | ≥ `curtainMinDisplaySeconds` + `curtainRevealDuration` | calculated | Lúc rèm kéo xong, game nhận chạm |
| boot_work_budget | float (s) | 0.5–2.8 | calculated | Thời gian tối đa cho mọi bước khởi động để còn trong ngân sách |

**Output range:** nhỏ nhất `curtainMinDisplaySeconds + curtainRevealDuration` = 0.75 s (máy nhanh, rèm luôn hiện đủ tối thiểu); trong ngân sách ≤ 2.0 s khi `t_boot_completed` ≤ `boot_work_budget` = 1.65 s. Không có trần cứng: một bước chậm có thể kéo dài tới `stepTimeoutSeconds` rồi thành lỗi (`step_timed_out`). Vượt ngân sách không đổi hành vi lúc chạy; nó làm trượt tiêu chí [Perf]. Giá trị `boot_to_interactive` của mỗi lần nạp được ghi vào dòng log `boot_to_interactive` ở mọi bản dựng, kể cả release (B5.6). Pooling tự giữ phần của nó trong ngân sách này bằng ràng buộc và validator của chính nó (`PoolingConfig.prewarmMaxSeconds`, `object-pooling.md` Tuning Knobs); Bootstrap không kiểm ràng buộc đó.
**Example:** máy tầm trung, `t_boot_completed` = 0.90 s → `reveal_start` = max(0.90, 0.4) = 0.90 → `boot_to_interactive` = 0.90 + 0.35 = **1.25 s** ≤ 2.0 ✓. Máy nhanh 0.15 s → max(0.15, 0.4) + 0.35 = **0.75 s**. Máy yếu 1.80 s → 1.80 + 0.35 = **2.15 s** > 2.0 → trượt [Perf] (cần giảm việc trong các bước hoặc tăng `prewarmPerFrame`).

### step_timed_out

`step_timed_out = (¬IsDone ∧ ¬IsFailed) ∧ (step_elapsed_visible > stepTimeoutSeconds)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| step_elapsed_visible | float (s) | 0 – không trần | calculated | Thời gian từ lúc gọi `Run()` của bước, **chỉ đếm khi trang hiển thị** (thời gian trang ẩn trong lúc Booting không tính) |
| stepTimeoutSeconds | float (s) | 5–30 | SO field `BootConfig.stepTimeoutSeconds` | Hạn tối đa của một bước |
| IsDone, IsFailed | bool | — | calculated | Do bước tự báo |

**Output range:** bool. Đúng → bước bị coi là lỗi (B4.1): cốt lõi → BootFailed; không cốt lõi → bỏ qua. Hạn rộng hơn nhiều so với ngân sách 2.0 s có chủ đích: một máy rất yếu vẫn vào được game (chậm) thay vì gặp bảng lỗi giả.
**Example:** `stepTimeoutSeconds` = 10; bước `pet.catalog` chạy 0.3 s → sai. Một bước treo không bao giờ báo xong: tới 10.0 s vẫn sai, khung đầu tiên sau 10.0 s → đúng. Trang bị ẩn 30 s ở giây thứ 2 của bước: `step_elapsed_visible` dừng ở 2 s trong lúc ẩn, nên không hết hạn giả.

### frame_rect

`a = screen_w / screen_h`
`frame_w, frame_h` = `(screen_h × maxAspect, screen_h)` nếu `a > maxAspect`; `(screen_w, screen_w / minAspect)` nếu `a < minAspect`; ngược lại `(screen_w, screen_h)`
`frame_x = (screen_w − frame_w) / 2`, `frame_y = (screen_h − frame_h) / 2`
`frame_scale = min(frame_w / referenceWidth, frame_h / referenceHeight)`
`extra_height_ref = frame_h / frame_scale − referenceHeight`
`frame_coverage = frame_w / screen_w`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| screen_w, screen_h | int (px) | > 0 | calculated | Kích thước vùng vẽ hiện tại (canvas WebGL / màn hình) |
| minAspect | float | 0.40–0.462 | SO field `SceneFlowConfig.minAspect` | Tỉ lệ rộng/cao hẹp nhất được giãn (9:21 = 0.4286) |
| maxAspect | float | 0.5–0.5625 (≤ `referenceWidth / referenceHeight`, validator) | SO field `SceneFlowConfig.maxAspect` | Tỉ lệ rộng/cao rộng nhất (9:16 = 0.5625); rộng hơn thì thêm viền hai bên |
| referenceWidth | int (px) | constant 1080 | SO field `SceneFlowConfig.referenceWidth` (constant) | Bề rộng tham chiếu, khớp Canvas Scaler và board Penpot |
| referenceHeight | int (px) | constant 1920 | SO field `SceneFlowConfig.referenceHeight` (constant) | Bề cao tham chiếu 9:16 |
| frame_scale | float | > 0 | calculated | Hệ số từ đơn vị tham chiếu sang pixel; với khung ≤ 9:16 luôn bằng `frame_w / referenceWidth` |
| extra_height_ref | float (đơn vị tham chiếu) | `referenceWidth / maxAspect − referenceHeight` – `referenceWidth / minAspect − referenceHeight` (mặc định 0 – ≈ 600) | calculated | Phần cao dư trao cho vùng nội dung |
| frame_coverage | float | 0–1 | calculated | Tỉ lệ bề ngang màn dùng cho khung (chỉ để kiểm/thống kê) |

**Output range:** với giá trị mặc định (`minAspect` = 0.4286, `maxAspect` = 0.5625), `extra_height_ref` từ 0 (9:16 hoặc rộng hơn) tới ≈ 600 (9:21 hoặc dài hơn); `frame_coverage` = 1 trên điện thoại dọc, ≈ 0.316 trên desktop 16:9, 0.75 trên iPad 3:4. Tổng quát, `extra_height_ref` nhỏ nhất = `referenceWidth / maxAspect − referenceHeight` (ví dụ `maxAspect` = 0.5 → 240) và lớn nhất = `referenceWidth / minAspect − referenceHeight`. Kích thước 0 (cửa sổ thu nhỏ) → không tính lại, giữ khung cũ, không phát `FrameChanged`.
**Example:**
- Desktop 1920×1080: a = 1.778 > 0.5625 → khung 607.5×1080, x = 656.25 (viền hai bên 656.25 px), `frame_scale` = 607.5/1080 = 0.5625, `extra_height_ref` = 1080/0.5625 − 1920 = **0**, `frame_coverage` = **0.316**.
- Điện thoại 1080×2400 (9:20, a = 0.45): trong khoảng → khung toàn màn, `frame_scale` = 1.0, `extra_height_ref` = **480**.
- Điện thoại 1080×2700 (a = 0.40 < 0.4286): khung 1080×(1080/0.4286 = 2519.8), y ≈ 90 px viền trên và dưới, `extra_height_ref` ≈ **600**.
- iPad 1536×2048 (a = 0.75): khung 1152×2048, viền hai bên 192 px, `frame_scale` = 1.0667, `extra_height_ref` = 2048/1.0667 − 1920 = **0**.

### away_seconds

`away_seconds = sanitize(t_resume_utc − t_pause_utc)`, với `sanitize(v)` = v nếu v hữu hạn và ≥ 0, ngược lại 0

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| t_pause_utc | double (s, giờ thực UTC) | — | calculated | Lúc phát `Paused` |
| t_resume_utc | double (s, giờ thực UTC) | — | calculated | Lúc phát `Resumed` |
| away_seconds | double (s) | 0 – không trần | calculated | Trường `awaySeconds` của `Resumed` |

**Output range:** 0 tới không trần. Bootstrap không đặt trần: trần giờ offline thuộc Offline Earnings; chống chỉnh đồng hồ thuộc Time & Timers (VS), hệ này có thể thay nguồn giờ (→ ADR). Đồng hồ bị lùi → 0, không bao giờ âm.
**Example:** ẩn tab lúc 10:00:00, hiện lại 10:05:30 → **330 s**. Người chơi chỉnh đồng hồ lùi 1 giờ trong lúc ẩn → −3270 → **0**.

### ad_seconds

`ad_seconds = sanitize(t_ad_close_utc − t_ad_open_utc)`, `sanitize` như `away_seconds`; `t_ad_open_utc` lấy ở lời gọi `SetAdOpen(true)` làm cờ bật (lời gọi `true` thứ hai không đổi mốc), `t_ad_close_utc` ở lời gọi `SetAdOpen(false)` tương ứng. Output: 0 tới không trần (trần thuộc Economy / Offline). **Example:** mở 10:00:00, đóng 10:00:30 → **30 s**; đồng hồ lùi → **0**.

### world_press_routed

`world_press_routed = is_first_new_press_in_frame ∧ inside_frame ∧ ¬hits_ui ∧ (input_block_tokens = 0) ∧ hits_viewport(ActiveTab)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| is_first_new_press_in_frame | bool | — | calculated | Cú nhấn mới bắt đầu đầu tiên trong khung (theo thứ tự sự kiện); ngón thứ hai trở đi cùng khung = sai |
| inside_frame | bool | — | calculated | Điểm nhấn nằm trong `frame_rect` |
| hits_ui | bool | — | calculated | Raycast UI trúng phần tử nhận raycast |
| input_block_tokens | int | 0 – không trần | calculated | Số token cổng chặn đang giữ (B7) |
| hits_viewport(ActiveTab) | bool | — | calculated | Điểm nằm trong một viewport world đã đăng ký của tab đang mở |

**Output range:** bool. Đúng → giao `WorldPress` trong cùng khung (trễ thêm ≤ `BootTestTargets.routerMaxAddedFrames`); sai → không hệ world nào nhận. Plinko sau đó tự áp `acceptManualTaps` và `tap_admitted` (registry); router không lặp các luật đó. Độ trễ đổi tab có công thức riêng `tab_switch_ms` (bên dưới), vì nút nav là UI và kích hoạt lúc nhả, không lúc nhấn.
**Example:** 3 ngón chạm cùng khung trên bàn, không token, không trúng UI → chỉ ngón đầu cho `true` → **1** `WorldPress`. Chạm trúng nút vặn trứng nằm trên bàn → `hits_ui` đúng → **0**. Chạm bàn khi popup giữ 1 token → **0**. Chạm bàn khi đang ở `Shelf` → `hits_viewport` sai → **0**.

### tab_switch_ms

`tab_switch_ms = t_new_tab_visible − t_nav_press`
Kiểm tra ngân sách: `tab_switch_ms ≤ SceneFlowConfig.tabSwitchMaxMs`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| t_nav_press | double (ms, giờ thực) | ≥ 0 | calculated | Giờ thực ở **đầu khung** mà Input System xử lý sự kiện **nhả** (click) của nút nav — mốc kích hoạt chuẩn của nút UI. Thời gian giữ ngón trước khi nhả không tính |
| t_new_tab_visible | double (ms, giờ thực) | ≥ `t_nav_press` | calculated | Giờ thực ở **cuối khung** đầu tiên vẽ tab mới (tab cũ đã ẩn trong cùng khung đó, B9.4) |
| tabSwitchMaxMs | int (ms) | 50–150 | SO field `SceneFlowConfig.tabSwitchMaxMs` | Ngân sách đổi tab |
| tab_switch_ms | double (ms) | 0 – không trần | calculated | Độ trễ đổi tab người chơi cảm nhận, tính từ lúc nhả nút |

**Output range:** 0 tới không trần; đạt khi ≤ `tabSwitchMaxMs`. Nếu tab mới được vẽ ngay trong khung xử lý cú nhả, `tab_switch_ms` bằng đúng một thời lượng khung. Trong test, cú nhấn và cú nhả được gửi ở hai khung liên tiếp (nhấn ở khung N, nhả ở khung N+1) để mốc đo không phụ thuộc thời gian giữ ngón.
**Example:** 60 FPS (`targetFps`), nhả được xử lý ở khung N+1 bắt đầu lúc 1000.0 ms; `RequestTab(Shelf)` → `Changed` trong cùng khung, khung N+1 vẽ xong lúc 1016.7 ms → `tab_switch_ms` = **16.7 ms** ≤ 100 ✓. HUD hoãn việc hiện tab sang khung N+2 (vẽ xong 1033.3 ms) → **33.3 ms** ≤ 100 ✓. Máy chậm 20 FPS, tab mới vẽ ở khung thứ hai sau cú nhả → ≈ **100 ms**, chạm ngưỡng.

## Edge Cases

**Khởi động**
- **If không có bước nào đăng ký trong một pha** (MVP: `SaveLoad`): pha kết thúc trong cùng khung, đi tiếp.
- **If hai bước trùng `Id`**: validator Editor báo lỗi; lúc chạy bước đăng ký sau bị từ chối và coi là lỗi khởi động cốt lõi (thứ tự không còn xác định được).
- **If `RunsAfter` trỏ tới `Id` không tồn tại, khác pha, hoặc tạo vòng**: validator Editor báo lỗi; lúc chạy → BootFailed với `stepId` của bước vi phạm (B3.4). Không bao giờ treo vô hạn chờ một bước không có.
- **If một hệ đăng ký bước sau khi ServiceInit đã bắt đầu**: từ chối, lỗi Editor; bước không chạy. Nếu hệ đó cần pool, Pooling cũng sẽ từ chối đăng ký muộn (Pooling R4) — lỗi trình tự phải sửa trong code.
- **If bước `Run()` ném ngoại lệ**: coi như `IsFailed` (B4.1), xử lý theo `IsCritical`.
- **If Audio lỗi khởi tạo** (thiếu `AudioConfig`, không tạo được voice): bước không cốt lõi → bỏ qua; game vào bình thường, không tiếng; `BootCompleted` vẫn phát đúng hạn.
- **If Pet Catalog, Economy Core, Plinko Board, Object Pooling (dịch vụ hoặc prewarm) lỗi**: BootFailed, bảng "Thử lại"; không phát `BootCompleted`, không cộng tiền, không mô phỏng.
- **If một bước không cốt lõi bị trễ tới `stepTimeoutSeconds`**: bị bỏ qua, khởi động đi tiếp; `boot_to_interactive` vượt ngân sách (trượt [Perf]) nhưng người chơi vẫn vào game.
- **If người chơi bấm "Thử lại" nhiều lần liên tiếp**: mỗi lần nạp lại scene đúng một lần; bấm trong lúc scene đang nạp bị bỏ (bảng đã biến mất cùng scene cũ).
- **If thiếu `BootConfig` hoặc `SceneFlowConfig`**: BootFailed ngay trước ServiceInit (B1.2); bảng "Thử lại" dùng giá trị dựng sẵn trong prefab của chính nó, không đọc config.
- **If khởi động xong nhanh hơn `curtainMinDisplaySeconds`**: rèm chờ đủ thời gian tối thiểu rồi mới kéo (`boot_to_interactive` = 0.75 s), không nháy.

**Vòng đời**
- **If cửa sổ trình duyệt chỉ mất focus nhưng vẫn hiển thị** (bấm ra ngoài canvas trên Poki): không có `Paused`; Plinko, cộng Idle và âm thanh chạy tiếp (Boot D3).
- **If trang bị ẩn trong lúc `Booting.Steps`**: không phát `Paused`; khởi động tiếp khi engine còn chạy khung; thời gian ẩn không tính vào `step_elapsed_visible` lẫn `t_boot_completed`. Nếu `BootCompleted` khi trang vẫn ẩn → vòng đời vào `Booting.Curtain` rồi phát `Paused` ngay trong cùng khung; rèm chỉ kéo khi trang hiện lại và đã có `Resumed`, sau đó mới vào Running.
- **If trang bị ẩn khi đã `BootCompleted` nhưng rèm còn Covering** (đang chờ đủ `curtainMinDisplaySeconds`): phát `Paused`; đồng hồ khởi động dừng nên thời gian ẩn không tính vào thời gian rèm đã hiện; sau `Resumed` vòng đời về `Booting.Curtain` và rèm kéo khi đủ thời gian tối thiểu.
- **If trang bị ẩn khi rèm đang kéo lên (Revealing)**: phát đúng 1 `Paused`; rèm vào RevealFrozen, đứng yên ở tiến độ đang có và vẫn giữ token (không chạm world nào lọt qua); khi trang hiện lại phát `Resumed`, rèm kéo tiếp phần còn lại rồi mới vào Running và nhả token. Ẩn/hiện nhiều lần trong lúc kéo → mỗi lần một cặp `Paused`/`Resumed`, rèm vẫn chỉ kéo đủ đúng `curtainRevealDuration` cộng dồn.
- **If trang ẩn rồi hiện lại nhiều lần thật nhanh**: mỗi chu kỳ ẩn/hiện cho đúng một cặp `Paused`/`Resumed`; `awaySeconds` có thể rất nhỏ (ví dụ 0.05 s) và vẫn hợp lệ.
- **If tín hiệu "ẩn" đến khi đã Paused** (ví dụ app nền trên mobile trong lúc tab đã ẩn): bỏ qua, không phát `Paused` lần hai.
- **If đồng hồ thiết bị bị chỉnh lùi trong lúc ẩn**: `awaySeconds` = 0 (`away_seconds`), không âm.
- **If ở MVP (chưa có Offline Earnings) tab bị ẩn 1 giờ**: CPS Idle và Plinko dừng suốt 1 giờ và **không được bù** khi quay lại. Đây là hành vi đúng của MVP, playtest không ghi là lỗi.
- **If ứng dụng bị đóng trong lúc Paused** (mobile kill từ nền): có thể không có `Quitting`; từ VS Save/Load đã lưu ở `Paused` nên không mất dữ liệu.
- **If WebGL không báo đóng trang**: không có `Quitting`; không hệ nào được dựa duy nhất vào `Quitting`.
- **If đang xem rewarded ad** (Alpha; quyết định user B, B6.10): Platform Services bật cờ `SetAdOpen(true)` trước khi hiện quảng cáo; trong lúc cờ bật, tín hiệu ẩn/nền do SDK quảng cáo gây ra (activity toàn màn trên Android/iOS) **không** phát `Paused`/`Resumed`, nên mô phỏng chạy tiếp; phần thời gian engine không chạy khung (mobile, activity che) được bù qua `AdClosed {adSeconds}` khi quảng cáo đóng (B6.10.5) — người chơi chọn xem quảng cáo không mất thu nhập. Tắt tiếng lúc ad là `SetMuteReason(Ad, bool)` của Audio do Platform Services gọi. Quảng cáo đóng (`SetAdOpen(false)`) mà app/trang vẫn ẩn → phát `AdClosed` rồi `Paused` ngay khung đó; đang hiển thị → không phát gì.
- **If Platform Services quên tắt cờ "ad đang mở"**: ẩn tab / xuống nền không còn tạm dừng tới hết phiên (Idle không dừng, Offline Earnings không nhận `Resumed`). Đây là lỗi của Platform Services; Bootstrap không tự tắt cờ theo thời gian, bản dev hiển thị trạng thái cờ trên lớp phủ debug.

**Cổng chặn và tab**
- **If hai hệ cùng giữ token cổng chặn** (popup mở trong lúc Tutorial giữ token): world bị chặn tới khi **cả hai** nhả.
- **If một hệ quên nhả token**: world bị chặn tới hết phiên. Bản dev hiển thị danh sách chủ token đang giữ để tìm lỗi; không có tự nhả theo thời gian (tránh mở cổng giữa một bước Tutorial dài).
- **If `RequestTab` tới chính tab đang mở**: `NoChange`, không phát `TabChanged`.
- **If `RequestTab` khi đang khóa tab**: `Locked`, không phát `TabChanged`; HUD phát phản hồi nhẹ (UI Requirements).
- **If `RequestTab` trong lúc Booting**: `Locked` (rèm che, tab luôn là `defaultTab` khi rèm kéo xong).
- **If bấm hai nút nav trong cùng khung**: chỉ cú nhấn đầu tiên trong khung được xử lý (cùng quy tắc một chạm mỗi khung).
- **If người chơi đang chạm giữ trên bàn rồi chuyển tab bằng ngón khác**: ngón thứ hai vào khung sau (ngón đầu không tạo cú nhấn mới) → nếu trúng nav thì đổi tab; chạm cũ trên bàn không giao thêm gì.

**Input và khung**
- **If chạm trúng UI nằm trên `PlinkoViewport`** (nút vặn trứng, header, toast): giao UI, 0 xu (khớp Plinko AC-34).
- **If 3 ngón chạm cùng khung trong bàn**: đúng 1 `WorldPress` (khớp Plinko AC-34).
- **If chạm vào viền trang trí ngoài khung**: bỏ; vẫn tính là cử chỉ đầu tiên cho `FirstInteraction`.
- **If cú chạm đầu tiên của phiên rơi vào bàn trên WebGL**: `FirstInteraction` phát và xu vẫn được thả; tiếng "tok" của chính cú đó có thể mất (Audio A10).
- **If cửa sổ trình duyệt bị đổi kích thước liên tục** (kéo mép cửa sổ): tính lại `frame_rect` mỗi khung có thay đổi, phát `FrameChanged` tối đa một lần mỗi khung; xu đang rơi không bị ảnh hưởng (Plinko world-space chỉ khớp camera).
- **If cửa sổ thu nhỏ còn kích thước 0**: giữ khung cũ, không phát `FrameChanged` (`frame_rect`).
- **If màn hình dài hơn 9:21 hoặc rộng hơn 9:16**: thêm viền trang trí (trên-dưới hoặc hai bên) thay vì kéo méo; bố cục bên trong không đổi.
- **If thiết bị xoay ngang** (mobile): hướng bị khóa dọc ở build nên không xảy ra; trên web, canvas ngang được xử lý như desktop (viền hai bên).

**Hàng đợi màn chào và Back**
- **If một bước màn chào không bao giờ gọi `onDone`**: hàng đợi đứng ở bước đó và token cổng chặn còn giữ — là lỗi của hệ sở hữu bước; bước đó phải có nút đóng (tông cozy, không ép).
- **If `ShouldShow()` của mọi bước đều sai**: `WelcomeQueueCompleted` phát ngay khi rèm kéo xong.
- **If bấm Back khi đang ở `Board` trên web**: không làm gì, không popup hỏi thoát.
- **If bấm Back trong lúc một bước màn chào hiện**: trình xử lý Back của bước đó chạy (thường là đóng = `onDone`).

## Dependencies

Bootstrap không phụ thuộc hệ nào (upstream: None). Mọi dòng dưới đây là hệ **phụ thuộc Bootstrap**; cột "Hard or soft" là mức phụ thuộc của hệ đó vào Bootstrap. Bootstrap chỉ biết các interface của chính nó (`IBootStep`, `IWelcomeStep`, viewport world, gốc hình ảnh tab, token), nên các mũi tên chỉ đi một chiều vào Foundation (C1[15]).

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Plinko Board | Plinko phụ thuộc Bootstrap | Hard | In (cho Plinko): `WorldPress {worldPos, screenPos}` đã lọc UI và cổng chặn; `ActiveTab` / `TabChanged` (R11.1, AC-28); `Paused` / `Resumed` (R11.2, Boot D3); `Interactive` + `IsSimulationRunning` (B6.5); `FrameChanged`. Out: bước `GameplayInit`, đăng ký `PlinkoViewport` + camera, gốc hình ảnh tab `Board`. Owner: Bootstrap (router, tab, vòng đời); Plinko (`acceptManualTaps`, cooldown, trần). |
| Economy Core | Economy phụ thuộc Bootstrap | Hard | In: thứ tự khởi tạo (sau `SaveLoad` từ VS), `Paused` / `Resumed` (dừng/tiếp cộng Idle), `Interactive` + `IsSimulationRunning` (B6.5), `AdClosed {adSeconds}` (tiêu thụ để bù Idle, B6.10.5). Out: bước `GameplayInit` cốt lõi `economy.core`, không `RunsAfter` (R16). Owner: Bootstrap. |
| Audio | Audio phụ thuộc Bootstrap | Soft (Audio chạy được khi không có sự kiện vòng đời; Bootstrap coi bước Audio là không cốt lõi) | In: `Paused` / `Resumed`, `FirstInteraction`, `Interactive` (mốc nhạc VS). Out: bước `ServiceInit` không cốt lõi (Ready / Failed). Owner: Bootstrap (sự kiện); Audio (phản ứng). |
| Object Pooling | Pooling phụ thuộc Bootstrap (hợp đồng pha) | Hard | In: thứ tự pha (mọi hệ khách đăng ký trong `GameplayInit`, trước `Prewarm`). Out: bước `ServiceInit` (Accepting) và bước `Prewarm` (prewarm toàn bộ pool, Pool D5), cả hai cốt lõi. Owner: Bootstrap (thứ tự pha); Pooling (nội dung prewarm). |
| Pet Catalog | Catalog phụ thuộc Bootstrap | Hard | In: pha `DataLoad` (trước mọi bước `GameplayInit`). Out: bước nạp cốt lõi → Ready. Owner: Bootstrap (thứ tự); Catalog (nạp). |
| Feedback & Juice | Feedback phụ thuộc Bootstrap | Hard (với `TabChanged`) | In: `ActiveTab` / `TabChanged`. Out: bước `GameplayInit` đề xuất không cốt lõi (đăng ký pool số bay). Owner: Bootstrap (sự kiện); Feedback (phản ứng, gọi Audio `SetBusBackground`). *Provisional* — GDD chưa có. |
| HUD & Tab Navigation | HUD phụ thuộc Bootstrap | Hard | In: `ActiveTab` / `TabChanged`, kết quả `RequestTab`, `FrameChanged` (vùng an toàn). Out: `RequestTab(tab)`, token cổng chặn và trình xử lý Back cho modal, bước `GameplayInit`. Owner: Bootstrap (trạng thái tab, khóa, cổng); HUD (vẽ nav, modal). *Provisional* — GDD chưa có. |
| Tutorial & Onboarding | Tutorial phụ thuộc Bootstrap (VS) | Hard | In: `WelcomeQueueCompleted`, `AcquireInputBlock`, `AcquireTabLock`, `RequestTab`, `ActiveTab`. Out: bước màn chào. Owner: Bootstrap (cơ chế); Tutorial (kịch bản). Muốn chỉ tắt thả xu tay → cờ `acceptManualTaps` của Plinko. *Provisional* — GDD chưa có. |
| Inventory | Inventory phụ thuộc Bootstrap | Hard | Out: bước `GameplayInit` cốt lõi (`RunsAfter` Economy). *Provisional*. |
| Gacha Machine | Gacha phụ thuộc Bootstrap | Hard | In: `AcquireInputBlock`, `AcquireTabLock`, `ActiveTab`. Out: bước `GameplayInit`. Owner: Bootstrap (cơ chế); Gacha (khi nào khóa). *Provisional*. |
| Shelf & Synergy | Shelf phụ thuộc Bootstrap | Hard | In: `ActiveTab` / `TabChanged`. Out: bước `GameplayInit` (`RunsAfter` Economy, Inventory), gốc hình ảnh tab `Shelf`. *Provisional*. |
| Save/Load (VS) | Save/Load phụ thuộc Bootstrap | Hard | In: pha `SaveLoad`, `Paused` (mốc lưu), `Quitting`. Out: bước `SaveLoad` cốt lõi. *Provisional*. |
| Time & Timers (VS) | Time phụ thuộc Bootstrap | Soft | In: `Paused` / `Resumed {awaySeconds}`. Out: bước `ServiceInit`. *Provisional*. |
| Offline Earnings (VS) | Offline phụ thuộc Bootstrap | Hard | In: `Resumed {awaySeconds}`, `AdClosed {adSeconds}` (bù thời gian xem rewarded ad, B6.10.5). Out: bước màn chào. *Provisional*. |
| Platform Services (Alpha) | Platform phụ thuộc Bootstrap | Hard | In: `BootCompleted`, `Interactive`, `FirstInteraction`, `Paused`, `Resumed`, `BootFailed`, `IsAdOpen`. Out: bước `ServiceInit`; `SetAdOpen(true/false)` quanh mỗi rewarded ad (B6.10, quyết định user B). Bootstrap không phát yêu cầu quảng cáo. *Provisional* (hợp đồng cờ đã chốt; chi tiết SDK chờ GDD). |

## Tuning Knobs

Ba asset: `BootConfig` (runtime: khởi động, rèm, lỗi), `SceneFlowConfig` (runtime: khung, tab, hàng đợi màn chào) và `BootTestTargets` (chỉ test/validator, không ảnh hưởng gameplay). Không có số nào trong code; validator Editor kiểm safe range và các ràng buộc ghi ở cột "Interacts with". Knob của hệ khác không lặp ở đây: `PoolingConfig.prewarmPerFrame` / `prewarmMaxSeconds` (Object Pooling), `PlinkoBalanceTargets.tapToSpawnMaxMs` (Plinko), `PlinkoConfig.tapCooldown` (Plinko, registry `tap_cooldown`).

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Ngân sách khung đầu → tương tác | `BootConfig.maxBootToInteractiveSeconds` | float (s) | 2.0 | 1.0–3.0 | Che giấu khởi động chậm, mất hook 30 s trên Poki | Tiêu chí [Perf] trượt khi thêm Save/Load và SDK nền tảng | curtainMinDisplaySeconds, curtainRevealDuration, `PoolingConfig.prewarmMaxSeconds` (phần của Pooling trong ngân sách này; ràng buộc và validator do Pooling sở hữu) |
| Rèm hiện tối thiểu | `BootConfig.curtainMinDisplaySeconds` | float (s) | 0.4 | 0.3–0.5 | Máy nhanh chờ vô ích | Rèm nháy trên máy nhanh | curtainRevealDuration; tổng hai knob rèm < maxBootToInteractiveSeconds |
| Thời lượng kéo rèm | `BootConfig.curtainRevealDuration` | float (s) | 0.35 | 0.2–0.5 | Mở tiệm lề mề, ăn vào ngân sách | Rèm giật lên, mất cảm giác "mở cửa" | maxBootToInteractiveSeconds |
| Hạn một bước khởi động | `BootConfig.stepTimeoutSeconds` | float (s) | 10 | 5–30 | Bước treo giữ người chơi trên rèm lâu trước khi có "Thử lại" | Máy yếu gặp bảng lỗi giả | Mọi `IBootStep` |
| Thời gian hiện bảng "Thử lại" | `BootConfig.errorPanelFadeSeconds` | float (s) | 0.2 | 0.1–0.4 | Người chơi tưởng game treo | Bảng bật gắt, lệch tông cozy | — |
| Tỉ lệ hẹp nhất được giãn | `SceneFlowConfig.minAspect` | float | 0.4286 (9:21) | 0.40–0.462 | Điện thoại dài bị viền trên/dưới sớm | Bố cục phải chịu vùng cao dư quá lớn | referenceWidth, referenceHeight; < maxAspect |
| Tỉ lệ rộng nhất | `SceneFlowConfig.maxAspect` | float | 0.5625 (9:16) | 0.5–0.5625 | > 0.5625 cần bố cục co giãn ngang (ngoài phạm vi, Boot D4) | Viền hai bên xuất hiện cả trên điện thoại 9:16 | minAspect; validator: ≤ referenceWidth / referenceHeight |
| Bề rộng tham chiếu | `SceneFlowConfig.referenceWidth` | int (px) | 1080 | constant — khớp Canvas Scaler và mọi board Penpot; đổi = làm lại toàn bộ bố cục, không phải tuning | — | — | referenceHeight; validator: `maxAspect` ≤ referenceWidth / referenceHeight |
| Bề cao tham chiếu | `SceneFlowConfig.referenceHeight` | int (px) | 1920 | constant — như trên (khung thiết kế 9:16) | — | — | referenceWidth |
| Ngân sách đổi tab | `SceneFlowConfig.tabSwitchMaxMs` | int (ms) | 100 | 50–150 | Đổi tab cảm giác lì | Không đạt được trên WebGL tầm trung (test trượt giả) | HUD (hoạt cảnh nav không được chặn việc hiện tab) |
| Tab mặc định khi khởi động | `SceneFlowConfig.defaultTab` | enum Tab | `Board` | constant — hook Plinko trong 30 s đầu (concept); giữ ở asset để test, không phải tuning | — | — | Tutorial |
| Thứ tự màn chào | `SceneFlowConfig.welcomeOrder` | list<string> | rỗng (MVP) | — (VS đề xuất: `offline.summary` → `tutorial.*` → thông báo) | Quá nhiều bước: phút đầu đầy popup | — | Offline Earnings, Tutorial, Shop |
| Số khung trễ router thêm vào | `BootTestTargets.routerMaxAddedFrames` | int (khung) | 1 | 0–1 | Vỡ `tapToSpawnMaxMs` (50 ms) của Plinko | — | `PlinkoBalanceTargets.tapToSpawnMaxMs` |
| Thiết bị đo [Perf] khởi động | `BootTestTargets.perfReferenceDevice` | string (hồ sơ máy đo) | "Laptop Intel Core i5 thế hệ 8, 8 GB RAM, Windows; Chrome bản ổn định mới nhất; DevTools CPU throttling 4×; cache và bộ nhớ trang trống; tab hiển thị suốt lần đo" (tạm — ADR hiệu năng có thể thay) | Chỉ thay bằng hồ sơ cụ thể tương đương hoặc yếu hơn do ADR hiệu năng chốt (đủ: CPU, RAM, trình duyệt, throttling, trạng thái cache) | Máy mạnh hơn che giấu khởi động chậm trên máy người chơi thật | Máy quá yếu làm [Perf] trượt giả, ép cắt việc khởi động không cần thiết | `BC.maxBootToInteractiveSeconds`, Object Pooling AC-10, Plinko [Perf] |
| Sai số tỉ lệ ảnh chụp | `BootTestTargets.screenshotAspectTolerance` | float | 0.01 | 0.005–0.02 | Kéo méo nhỏ lọt qua AC-31 | Sai số làm tròn pixel và khử răng cưa gây trượt giả | AC-31 (vòng tròn tham chiếu) |

Hằng số không phải knob: thứ tự năm pha (B3) — là cấu trúc của hợp đồng, đổi thứ tự là đổi GDD này; ba tab cố định (concept: không có tab thứ tư); `SF.referenceWidth` / `SF.referenceHeight` / `SF.defaultTab` (ghi "constant" ở bảng trên, lý do kèm theo); dòng log `boot_to_interactive` đúng một dòng mỗi lần nạp, độ phân giải mili-giây (B5.6 — định dạng đo của AC-36, không phải gameplay); `requiresUnlock` của Audio theo nền tảng (thuộc Audio). Cờ "ad đang mở" (B6.10) không có hẹn giờ nên không có knob.

## Visual/Audio Requirements

Tông "Tiệm Gỗ Kem Bơ", Zero Art: mọi thứ dưới đây dựng được bằng vector phẳng + màu palette + emoji, không cần art riêng.

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| Khung đầu / Booting | Rèm toàn khung nền kem `#FAF7F2`, dải gỗ sồi `#D9A066` ở mép dưới như mép rèm, bảng gỗ nhỏ treo giữa ghi "Tiệm mở cửa" chữ nâu `#3D312A`; không thanh tiến độ, không phần trăm | Không (WebGL còn khóa âm thanh; Audio có thể chưa sẵn sàng) | MVP |
| Rèm kéo lên (Revealing) | Rèm trượt lên hết khung trong `curtainRevealDuration`, ease-out mềm, bảng gỗ đi theo rèm; bàn Plinko hiện dần bên dưới | Không ở MVP. VS có thể thêm tiếng chuông cửa gỗ nhỏ (do Feedback & Juice map cue, chỉ phát khi Audio đã mở khóa) | MVP (hình) / VS (tiếng) |
| BootFailed | Bảng "Thử lại" hiện trên rèm sau `errorPanelFadeSeconds`: thẻ gỗ bo tròn, câu ngắn tông cozy ("Tiệm gặp chút trục trặc khi mở cửa"), nút matcha `#7EA172` "Thử lại", dòng chữ nhỏ mờ ghi mã bước lỗi (`stepId`) để báo lỗi | Không | MVP |
| Viền ngoài khung (màn rộng hơn 9:16 / dài hơn 9:21) | Nền kem có hoa văn ván gỗ nhạt hoặc kệ gỗ trang trí, độ tương phản thấp, không chứa nội dung tương tác | Không | MVP (màu phẳng) / VS (hoa văn — cần art-director xác nhận trên Poki desktop) |
| `TabChanged` | Tab mới hiện, tab cũ ẩn trong cùng khung; hoạt cảnh nút nav thuộc HUD | Tiếng bấm nav thuộc HUD / Feedback & Juice | MVP |
| `Paused` / `Resumed` | Không có lớp phủ "Tạm dừng" (người chơi không thấy khi tab ẩn); khi quay lại mọi thứ tiếp tục nguyên trạng | Audio tự tắt/mở tiếng có fade (A9 của Audio) | MVP |

Bootstrap không phát âm thanh nào trực tiếp và không sở hữu `SoundCue` (Audio D1).

## Game Feel

Bootstrap là hạ tầng nên không có cảm giác riêng, nhưng nó đặt trần độ trễ cho mọi cú chạm và cú đổi tab — các con số dưới đây là ràng buộc mà các hệ trình bày phải giữ.

- **Feel reference**: cảm giác "lật biển mở cửa" của một tiệm nhỏ — rèm kéo lên một lần, gọn, rồi mọi thứ đã sẵn sàng (gần với màn vào vườn của Neko Atsume: không thanh tải, vào là thấy ngay). **Anti-reference**: màn loading có logo + thanh phần trăm và popup chồng nhau ngay khi vào của nhiều game idle mobile.
- **Input responsiveness**:

| Action | Max input-to-response (ms) | Frames at target fps | Notes |
|---|---|---|---|
| Chạm bàn → `WorldPress` tới Plinko | ≤ 1 khung thêm vào (`routerMaxAddedFrames`) | 1 | Phần còn lại của `tapToSpawnMaxMs` (50 ms, 3 khung) thuộc Plinko |
| Bấm nút nav → tab mới hiển thị | `tabSwitchMaxMs` (100) | 6 | Hiện/ẩn trong cùng khung; hoạt cảnh nút nav chạy song song, không chặn |
| Bấm Back → đóng modal / về Bàn | `tabSwitchMaxMs` (100) | 6 | Cùng ngân sách |
| Rèm kéo xong → chạm đầu được nhận | 0 (token nhả cùng khung kéo xong) | 0 | Không có khoảng "chết" sau rèm |

- **Animation feel targets**:

| Animation | Startup | Active | Recovery frames | Feel goal |
|---|---|---|---|---|
| Rèm kéo lên | 0 (bắt đầu ngay khi đủ điều kiện) | `curtainRevealDuration` (0.35 s ≈ 21 khung) | 0 | Gọn, ấm, như kéo rèm cửa tiệm |
| Bảng "Thử lại" hiện | 0 | `errorPanelFadeSeconds` (0.2 s ≈ 12 khung) | 0 | Nhẹ, không báo động |

- **Impact moments**: không có (Bootstrap không dùng hit-stop, rung màn hình hay đổi time scale; Plinko cấm hit-stop toàn cục).
- **Weight and responsiveness profile**: vô hình và tức thì — người chơi không bao giờ nhận ra router hay tab manager tồn tại. Tiêu chí cảm giác: AC-24 (trễ router), AC-26 (đổi tab), AC-14 (không có khoảng chết sau rèm).

## UI Requirements

Bootstrap sở hữu ba thành phần UI toàn khung; thanh nav, header và modal thuộc HUD & Tab Navigation.

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Rèm "Tiệm mở cửa" (bảng gỗ tên tiệm) | Phủ toàn `frame_rect`, trên mọi Canvas khác | Một lần mỗi phiên (Covering → Revealing → Open) | Từ khung đầu tới khi rèm kéo xong |
| Bảng "Thử lại" (câu tông cozy, nút "Thử lại", mã bước lỗi chữ nhỏ) | Giữa khung, trên rèm | Một lần khi vào BootFailed | Chỉ khi bước cốt lõi lỗi |
| Viền trang trí | Ngoài `frame_rect` (hai bên hoặc trên-dưới) | Khi `FrameChanged` | Khi màn rộng hơn `maxAspect` hoặc hẹp hơn `minAspect` |
| Danh sách chủ token cổng chặn, cờ "ad đang mở" và báo cáo khởi động (thời gian từng bước) | Lớp phủ debug | Mỗi khung khi bật | Chỉ bản dev, không bao giờ trong bản phát hành. Riêng dòng log `boot_to_interactive` (B5.6) không phải lớp phủ: ghi ra console/log ở mọi bản dựng, kể cả release |

Yêu cầu bố cục: rèm, bảng lỗi và viền co giãn theo `frame_rect`, đúng ở mọi tỉ lệ từ 9:21 tới 16:9 ngang; nút "Thử lại" có vùng chạm ≥ quy chuẩn nút của HUD; chữ trên rèm và bảng lỗi đạt tương phản ≥ `PlinkoBalanceTargets.labelContrastMin` (4.5:1, dùng chung chuẩn của Plinko, không tạo knob riêng).

UX flag: hệ này có UI (rèm, bảng "Thử lại", viền). UX authoring chưa có trong plugin — cần board Penpot cho rèm, bảng lỗi và viền desktop 16:9 (art-director xác nhận viền trên Poki desktop, Boot D4); ghi chú cho hàng Bootstrap trong systems index (bước Sync).

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Router: lọc UI, một chạm mỗi khung, trễ ≤ 1 khung | `Docs/design/gdd/plinko-board.md` | AC-34, AC-35, `tapToSpawnMaxMs`, `tap_cooldown`, `max_active_coins` (registry) | Bootstrap giữ ngân sách của Plinko; không lặp cooldown/trần |
| Tab ẩn vẫn mô phỏng; tạm dừng chỉ khi ẩn/nền | `Docs/design/gdd/plinko-board.md` | R11.1, R11.2, AC-28, AC-29 | Plinko nghe `TabChanged`, `Paused`/`Resumed`, `Interactive` / `IsSimulationRunning` (B6.5) |
| Ranh giới cổng chặn | `Docs/design/gdd/plinko-board.md` | Cờ `acceptManualTaps` (R4.6) | Plinko sở hữu cờ; Bootstrap sở hữu cổng toàn cục |
| Dừng cộng Idle theo tạm dừng; thứ tự sau Save/Load | `Docs/design/gdd/economy-core.md` | R6.3, States (Uninitialized/Running/Paused), AC-22, `maxFrameDelta` | Economy nghe `Paused`/`Resumed`, `Interactive` / `IsSimulationRunning`; tự kẹp delta khung đầu sau `Resumed`; bù Idle theo `AdClosed {adSeconds}` (B6.10.5) |
| Khởi tạo dịch vụ nền, phụ thuộc mềm | `Docs/design/gdd/audio.md` | A9 (`Background`), A10 (`FirstInteraction`), trạng thái Failed, `SetBusBackground` khi `TabChanged` (qua Feedback) | Audio nghe sự kiện; bước Audio không cốt lõi |
| Prewarm toàn bộ pool trước `BootCompleted` | `Docs/design/gdd/object-pooling.md` | R3.3 (Accepting), R4 (Prewarm), `prewarm_frames`, `PoolingConfig.prewarmMaxSeconds` (phần của Pooling trong `maxBootToInteractiveSeconds`) | Pooling đăng ký hai bước; Bootstrap bảo đảm thứ tự pha |
| Nạp Catalog ở pha dữ liệu | `Docs/design/gdd/pet-catalog.md` | R12.1, vòng đời Catalog (Unloaded → Ready) | Catalog đăng ký bước `DataLoad` |
| Tab, nav, modal | `Docs/design/gdd/hud-and-tab-navigation.md` *(chưa có)* | `RequestTab`, `TabChanged`, token và trình xử lý Back cho modal | HUD gọi Bootstrap |
| Số bay và bus nền khi bàn ẩn | `Docs/design/gdd/feedback-and-juice.md` *(chưa có)* | `TabChanged` | Feedback nghe |
| Khóa tab / chặn input khi hướng dẫn | `Docs/design/gdd/tutorial-and-onboarding.md` *(chưa có)* | Token, khóa tab, `WelcomeQueueCompleted` | Tutorial gọi Bootstrap |
| Mở trứng | `Docs/design/gdd/gacha-machine.md` *(chưa có)* | Token cổng chặn, chính sách khóa tab | Gacha quyết định chính sách |
| Thu nhập vắng mặt | `Docs/design/gdd/offline-earnings.md` *(chưa có)* | `Resumed.awaySeconds`, `AdClosed.adSeconds`, bước màn chào | Offline nghe |
| Lưu dữ liệu | `Docs/design/gdd/save-load.md` *(chưa có)* | Pha `SaveLoad`, `Paused`, `Quitting` | Save/Load đăng ký bước |
| Cổng game | `Docs/design/gdd/platform-services.md` *(chưa có)* | `BootCompleted`, `Interactive`, `FirstInteraction`, `Paused`, `Resumed`, `BootFailed`; `SetAdOpen` / `IsAdOpen` (B6.10) | Platform ánh xạ sang SDK; đặt/tắt cờ "ad đang mở" |
| Khung 9:16 / 9:19.5, 3 tab, không cuộn, Anti-Pillar quảng cáo ép buộc | `Docs/design/gdd/game-concept.md` | Bố cục giao diện, Anti-Pillars, rủi ro "hook trong 30 giây" | Ràng buộc concept; 9:21 (Boot D4) là phần mở rộng, concept nên ghi thêm (Sync) |

## Acceptance Criteria

Ký hiệu: `BC` = `BootConfig`, `SF` = `SceneFlowConfig`, `BT` = `BootTestTargets`. Test EditMode dùng bước khởi động giả (`IBootStep` stub), đồng hồ giả và nguồn tín hiệu nền tảng giả (hiển thị/ẩn, nền, focus, đóng) — không cần nền tảng thật. Assembly test: `CozyGachapon.Tests.EditMode`.

**Hợp đồng bước và thứ tự pha (B2, B3)**
- `AC-1 [EditMode]` **GIVEN** 6 bước giả đăng ký theo thứ tự xáo trộn, mỗi pha một bước (`ServiceInit`, `DataLoad`, `SaveLoad`, `GameplayInit`, `Prewarm`) và thêm một bước `ServiceInit` thứ hai, **WHEN** chạy khởi động tới hết, **THEN** thứ tự gọi `Run()` theo đúng thứ tự pha, mỗi bước được gọi đúng 1 lần, và `BootCompleted` phát đúng 1 lần, sau khi bước `Prewarm` báo xong.
- `AC-2 [EditMode]` **GIVEN** trong `GameplayInit` ba bước `economy.core`, `inventory.core` (`RunsAfter` = [`economy.core`]) và `gacha.machine` (không ràng buộc), **WHEN** chạy khởi động 10 lần, **THEN** cả 10 lần thứ tự đều là `economy.core` → `gacha.machine` → `inventory.core` (tô-pô, hòa thì theo thứ tự chữ).
- `AC-3 [EditMode]` **GIVEN** bước A báo `IsDone` sau 5 khung và bước B cùng pha xếp sau A, **WHEN** chạy từng khung, **THEN** `Run()` của B không được gọi trước khung A báo xong, và được gọi ở chính khung đó hoặc khung kế tiếp.
- `AC-4 [EditMode]` **GIVEN** pha `SaveLoad` không có bước nào (cấu hình MVP), **WHEN** khởi động đi tới pha đó, **THEN** pha kết thúc trong cùng khung và bước đầu của `GameplayInit` được gọi không trễ thêm khung nào vì pha rỗng.
- `AC-5 [EditMode]` **GIVEN** lần lượt ba cấu hình sai: `RunsAfter` trỏ `Id` không tồn tại; trỏ một bước khác pha; hai bước trỏ nhau thành vòng, **WHEN** chạy validator rồi chạy khởi động, **THEN** validator báo lỗi cho cả ba, và mỗi lần chạy kết thúc ở BootFailed với `stepId` của bước vi phạm trong ≤ 1 khung sau khi tới pha đó (không treo).
- `AC-6 [EditMode]` **GIVEN** khởi động đã vào `ServiceInit`, **WHEN** một hệ đăng ký thêm bước, **THEN** đăng ký bị từ chối, có lỗi Editor, `Run()` của bước đó không bao giờ được gọi; hai bước trùng `Id` cũng bị từ chối và dẫn tới BootFailed.
- `AC-7 [EditMode]` **GIVEN** bước Pooling giả ở `Prewarm` ghi lại số đăng ký pool nhận được và các bước `GameplayInit` giả mỗi bước đăng ký 1 pool, **WHEN** chạy khởi động, **THEN** mọi đăng ký pool xảy ra trước `Run()` của bước Prewarm, Pooling giả ở Ready trước `BootCompleted`, và `BootCompleted` không phát khi bước Prewarm chưa xong (Pool D5).

**Lỗi khởi động (B4, step_timed_out)**
- `AC-8 [EditMode]` **GIVEN** bước `audio.service` (`IsCritical` = sai) báo `IsFailed` và, ở một ca khác, ném ngoại lệ trong `Run()`, **WHEN** chạy khởi động, **THEN** cả hai ca đều phát `BootCompleted`, mọi bước sau vẫn chạy, báo cáo khởi động ghi `audio.service` lỗi, không có `BootFailed`.
- `AC-9 [EditMode]` **GIVEN** bước `pet.catalog` (`IsCritical` = đúng) báo `IsFailed`, **WHEN** chạy khởi động, **THEN** phát đúng 1 `BootFailed {stepId = pet.catalog, phase = DataLoad}`, không bước nào sau đó được gọi `Run()`, không có `BootCompleted`, rèm ở ErrorShown và bảng "Thử lại" hiện đủ sau `BC.errorPanelFadeSeconds`.
- `AC-10 [Play mode]` **GIVEN** scene `MainGame` với một bước cốt lõi giả lỗi ở lần chạy đầu và thành công ở lần sau, **WHEN** bấm "Thử lại", **THEN** scene được nạp lại đúng 1 lần, khởi động chạy lại từ `ServiceInit` và phát `BootCompleted`.
- `AC-11 [EditMode]` **GIVEN** `BC.stepTimeoutSeconds` = 10 và một bước cốt lõi không bao giờ báo xong, **WHEN** đồng hồ giả chạy, **THEN** ở 9.9 s bước chưa lỗi, ở khung đầu tiên sau 10.0 s phát `BootFailed`; với cùng bước nhưng trang bị ẩn 30 s từ giây thứ 2, bước chỉ lỗi khi `step_elapsed_visible` > 10 s (tức ≈ 40 s giờ thực); với bước không cốt lõi thì hết hạn dẫn tới `BootCompleted` thay vì `BootFailed`.
- `AC-12 [EditMode]` **GIVEN** thiếu `BootConfig` (và ở ca khác thiếu `SceneFlowConfig`), **WHEN** khởi động, **THEN** vào BootFailed trước khi bất kỳ `Run()` nào được gọi, và bảng "Thử lại" vẫn hiện.

**Rèm và ngân sách (B5, boot_to_interactive)**
- `AC-13 [EditMode]` **GIVEN** `BC` mặc định (0.4, 0.35, 2.0) và đồng hồ giả, **WHEN** `BootCompleted` phát lúc 0.15 s, 0.90 s và 1.80 s, **THEN** rèm bắt đầu kéo lúc 0.40 / 0.90 / 1.80 s, kéo xong lúc 0.75 / 1.25 / 2.15 s, và kiểm tra ngân sách chỉ báo vượt ở ca 2.15 s; validator báo lỗi khi `BC.curtainMinDisplaySeconds` + `BC.curtainRevealDuration` ≥ `BC.maxBootToInteractiveSeconds`.
- `AC-14 [EditMode]` **GIVEN** khung đầu tiên của scene, **WHEN** đọc trạng thái trước khi bất kỳ `Run()` nào được gọi, **THEN** rèm ở Covering và giữ 1 token; trong Covering và Revealing mọi chạm world bị chặn; ở khung rèm kéo xong token được nhả, vòng đời là Running và một chạm vào bàn trong chính khung đó được giao.
- `AC-15 [Screenshot]` **GIVEN** scene ở trạng thái Covering, **WHEN** chụp ở 1080×1920 và 1920×1080, **THEN** thấy rèm kem phủ đúng `frame_rect`, bảng gỗ "Tiệm mở cửa" ở giữa, không có thanh tiến độ hay số phần trăm, chữ đạt tương phản ≥ `PlinkoBalanceTargets.labelContrastMin`.
- `AC-16 [Screenshot]` **GIVEN** BootFailed do bước giả `pet.catalog`, **WHEN** chụp ở 1080×1920, **THEN** thấy bảng gỗ có câu thông báo, nút "Thử lại" màu matcha và dòng chữ nhỏ chứa `pet.catalog`; không có màn trắng.

**Vòng đời (B6, away_seconds)**
- `AC-17 [EditMode]` **GIVEN** vòng đời Running, **WHEN** lần lượt: cửa sổ mất focus (vẫn hiển thị); trang ẩn; trang ẩn lần nữa; app xuống nền; trang hiện lại, **THEN** mất focus không phát gì; phát đúng 1 `Paused {reason = Hidden}`; hai tín hiệu ẩn/nền sau bị bỏ; phát đúng 1 `Resumed` kèm `awaySeconds`.
- `AC-18 [EditMode]` **GIVEN** đồng hồ UTC giả, **WHEN** `Paused` lúc 10:00:00 rồi `Resumed` lúc 10:05:30; ca thứ hai đồng hồ lùi 1 giờ trong lúc ẩn; ca thứ ba hiệu là NaN; ca thứ tư ẩn 0.05 s, **THEN** `awaySeconds` lần lượt = 330, 0, 0, 0.05.
- `AC-19 [EditMode]` **GIVEN** trang bị ẩn lúc 0.5 s trong lúc Booting, **WHEN** các bước xong trong lúc ẩn, **THEN** không có `Paused` trước `BootCompleted` (vòng đời ở `Booting.Steps` suốt lúc đó); trong cùng khung `BootCompleted` vòng đời qua `Booting.Curtain` rồi phát đúng 1 `Paused` và ở Paused với trạng thái trả về = `Booting.Curtain`; rèm vẫn Covering; khi trang hiện lại phát `Resumed`, vòng đời về `Booting.Curtain` (chưa phải Running) rồi rèm mới bắt đầu kéo; vòng đời chỉ vào Running ở khung rèm Open.
- `AC-20 [Play mode]` **GIVEN** scene đầy đủ với `idle_cps` > 0 và 10 xu đang rơi, **WHEN** giả lập trang ẩn 60 s rồi hiện lại; và ở ca khác chỉ mất focus 60 s, **THEN** ca ẩn: số dư và vị trí xu không đổi trong lúc ẩn, tiếp tục sau `Resumed` (khớp Plinko AC-29, Economy AC-22); ca mất focus: số dư tăng và xu tiếp tục rơi.
- `AC-21 [EditMode]` **GIVEN** vòng đời Running, **WHEN** nền tảng báo sắp đóng hai lần, **THEN** `Quitting` phát đúng 1 lần, sau đó không có `Paused`/`Resumed` nào nữa.
- `AC-38 [EditMode]` **GIVEN** `BC` mặc định (0.4, 0.35, 2.0), đồng hồ giả, `BootCompleted` lúc 0.90 s (đồng hồ khởi động) và một viewport world giả cho `Board`, **WHEN** trang bị ẩn lúc 1.00 s (rèm đã kéo 0.10 s), giữ ẩn 30 s giờ thực, gửi một cú nhấn vào bàn trong lúc ẩn, rồi hiện lại, **THEN** phát đúng 1 `Paused {reason = Hidden}` lúc ẩn và đúng 1 `Resumed` với `awaySeconds` = 30 (sai số ≤ 0.01); suốt lúc ẩn rèm ở RevealFrozen với tiến độ không đổi (0.10 / 0.35 ≈ 0.286, sai số ≤ 0.01), cổng giữ token của rèm và 0 `WorldPress`; sau `Resumed` vòng đời là `Booting.Curtain` (không phải Running), rèm Revealing tiếp từ ≈ 0.286, kéo xong lúc 1.25 s đồng hồ khởi động (≈ 31.25 s giờ thực), và chỉ ở khung đó vòng đời vào Running, token được nhả. **GIVEN** ca thứ hai: `BootCompleted` lúc 0.15 s, trang ẩn lúc 0.20 s trong 10 s, **WHEN** hiện lại, **THEN** rèm vẫn Covering suốt lúc ẩn, bắt đầu kéo lúc 0.40 s và kéo xong lúc 0.75 s đồng hồ khởi động (≈ 10.75 s giờ thực).
- `AC-40 [EditMode]` **GIVEN** `BC` mặc định (0.4, 0.35, 2.0), đồng hồ giả và một hệ mô phỏng giả chỉ đếm bước khi `IsSimulationRunning` đúng, `BootCompleted` lúc 0.90 s, **WHEN** chạy tới khi rèm Open (1.25 s), rồi trang ẩn, rồi hiện lại, **THEN** `IsSimulationRunning` sai và hệ giả đếm 0 bước ở mọi khung trước khung rèm Open (cả `Booting.Steps` lẫn `Booting.Curtain`); ở khung rèm Open phát đúng 1 `Interactive`, sau khi token của rèm đã nhả, và `IsSimulationRunning` đúng từ khung đó; khi `Paused` thì sai, sau `Resumed` thì đúng lại, và không có `Interactive` thứ hai. **GIVEN** ca thứ hai: trang ẩn lúc 1.00 s khi rèm đang kéo (như AC-38), **WHEN** hiện lại, **THEN** sau `Resumed` `IsSimulationRunning` vẫn sai (vòng đời `Booting.Curtain`) cho tới khung rèm Open, nơi `Interactive` phát đúng 1 lần. **GIVEN** ca thứ ba: bước cốt lõi lỗi (BootFailed), **THEN** không có `Interactive` và `IsSimulationRunning` luôn sai.
- `AC-42 [EditMode]` **GIVEN** vòng đời Running, nguồn tín hiệu nền tảng giả và đồng hồ UTC giả, **WHEN** Platform Services giả gọi `SetAdOpen(true)` hai lần, rồi lần lượt: app xuống nền, trở lại, trang ẩn, hiện lại, rồi `SetAdOpen(false)` một lần, **THEN** `IsAdOpen` đúng tới lời gọi `false` rồi sai; 0 `Paused`, 0 `Resumed`; vòng đời luôn Running và `IsSimulationRunning` luôn đúng. **GIVEN** ca thứ hai: `SetAdOpen(true)`, trang ẩn lúc 10:00:00, `SetAdOpen(false)` lúc 10:00:30 khi trang vẫn ẩn, hiện lại lúc 10:01:00, **THEN** phát đúng 1 `Paused {reason = Hidden}` trong khung gọi `SetAdOpen(false)` và đúng 1 `Resumed` với `awaySeconds` = 30. **GIVEN** ca thứ ba: cờ tắt từ đầu, **WHEN** trang ẩn rồi hiện, **THEN** hành vi như AC-17 (1 `Paused`, 1 `Resumed`). **GIVEN** ca thứ tư (BSF-R4-02): trang ẩn khi cờ tắt (vào Paused), **WHEN** `SetAdOpen(true)` rồi trang hiện lại khi cờ vẫn bật, **THEN** phát đúng 1 `Resumed` và vòng đời về Running.
- `AC-43 [EditMode]` **GIVEN** vòng đời Running, đồng hồ UTC giả, **WHEN** `SetAdOpen(true)` lúc 10:00:00 (gọi lần hai lúc 10:00:10), rồi `SetAdOpen(false)` lúc 10:00:30; ca thứ hai: đồng hồ lùi 1 giờ giữa mở và đóng; ca thứ ba: `SetAdOpen(false)` khi cờ đã tắt; ca thứ tư: như ca một nhưng trang ẩn từ 10:00:05 tới 10:01:00, **THEN** phát đúng 1 `AdClosed` với `adSeconds` = 30, 0, không phát gì; ca thứ tư phát `AdClosed {30}` rồi `Paused` trong cùng khung đóng cờ, theo đúng thứ tự đó.

**Cài đặt build (B6.8, B8.4)**
- `AC-41 [EditMode]` **GIVEN** Player Settings của dự án, **WHEN** test đọc `PlayerSettings.runInBackground` và `PlayerSettings.defaultInterfaceOrientation`, **THEN** `runInBackground` = đúng và `defaultInterfaceOrientation` = `UIOrientation.Portrait` (không phải `AutoRotation`, nên không có hướng ngang nào được cho phép).

**Cổng chặn, tab và router (B7, B9, world_press_routed)**
- `AC-22 [EditMode]` **GIVEN** một viewport world giả đăng ký cho `Board`, `ActiveTab` = `Board`, **WHEN** đánh giá `world_press_routed` cho: nhấn đầu tiên trong khung, trong khung hình, không trúng UI, 0 token; cùng điểm nhưng là ngón thứ hai cùng khung; trúng UI; 1 token; `ActiveTab` = `Shelf`; điểm ngoài `frame_rect`, **THEN** kết quả lần lượt đúng, sai, sai, sai, sai, sai, và chỉ ca đầu sinh `WorldPress` với `worldPos` = điểm màn hình đổi qua camera giả.
- `AC-23 [Play mode]` **GIVEN** `InputTestFixture` và scene thật, **WHEN** chạm trúng nút vặn trứng / header nằm trên `PlinkoViewport`; chạm 3 ngón cùng khung trong bàn, **THEN** lần lượt 0 và 1 `WorldPress` (khớp Plinko AC-34).
- `AC-24 [Play mode]` **GIVEN** bàn Idle, **WHEN** gửi một cú nhấn hợp lệ qua `InputTestFixture`, **THEN** `WorldPress` tới Plinko trễ ≤ `BT.routerMaxAddedFrames` khung so với khung Input System xử lý sự kiện, và xu xuất hiện trong ≤ `PlinkoBalanceTargets.tapToSpawnMaxMs`.
- `AC-25 [EditMode]` **GIVEN** cổng mở, **WHEN** hệ X lấy token, hệ Y lấy token, X nhả, Y nhả, X nhả lại lần nữa, nhả một token không hợp lệ, **THEN** trạng thái cổng lần lượt Blocked, Blocked, Blocked, Open, Open, Open; hai lần nhả cuối không ném exception (cảnh báo Editor).
- `AC-26 [Play mode]` **GIVEN** Running ở `Board`, `InputTestFixture`, **WHEN** gửi cú nhấn xuống trên nút nav "Kệ" ở khung N rồi cú nhả ở khung N+1 (cố định trong test), **THEN** ở khung N không có `TabChanged` và `ActiveTab` vẫn là `Board`; tab `Shelf` hiển thị và `Board` ẩn trong cùng một khung; `tab_switch_ms` (từ đầu khung N+1 — khung Input System xử lý cú nhả — tới cuối khung đầu tiên vẽ `Shelf`) ≤ `SF.tabSwitchMaxMs`; phát đúng 1 `TabChanged {previous = Board, current = Shelf}`.
- `AC-27 [EditMode]` **GIVEN** `ActiveTab` = `Board`, **WHEN** `RequestTab(Board)`; `RequestTab(Shop)` khi có 1 khóa; `RequestTab(Shop, token của chủ khóa)` khi đó là khóa duy nhất; `RequestTab(Shelf, token A)` khi có hai khóa A và B; `RequestTab(Shelf)` trong lúc Booting, **THEN** kết quả lần lượt `NoChange`, `Locked`, `Changed`, `Locked`, `Locked`, và chỉ ca `Changed` phát `TabChanged`.
- `AC-28 [Play mode]` **GIVEN** khởi động mới, **WHEN** rèm kéo xong, **THEN** `ActiveTab` = `SF.defaultTab` (`Board`); chuyển sang `Shelf` với 10 xu đang rơi thì renderer/camera của bàn tắt nhưng số `CoinResolved` vẫn tăng (khớp Plinko AC-28); sau một chu kỳ `Paused`/`Resumed` ở `Shop`, `ActiveTab` vẫn là `Shop`.
- `AC-29 [EditMode]` **GIVEN** ngăn xếp Back có 1 trình xử lý modal, **WHEN** bấm Back bốn lần liên tiếp bắt đầu từ `ActiveTab` = `Shop`, trên Android giả rồi trên web giả, **THEN** lần 1 gọi trình xử lý modal (modal tự pop); lần 2 → `RequestTab(Board)`; lần 3 → Android: yêu cầu đưa app xuống nền, web: không làm gì; lần 4 (ngăn xếp vẫn rỗng, vẫn ở `Board`) → Android: lại yêu cầu đưa app xuống nền (tổng 2 yêu cầu), web: không làm gì (tổng 0 yêu cầu, 0 `TabChanged` ở lần 3 và 4); không lần nào mở popup hỏi thoát; Back trong lúc Booting bị bỏ.

**Khung hình (B8, frame_rect)**
- `AC-30 [EditMode]` **GIVEN** `SF` mặc định, **WHEN** tính `frame_rect` cho 1920×1080, 1080×2400, 1080×2700, 1536×2048 và 0×0, **THEN** lần lượt: khung 607.5×1080 ở x = 656.25, `frame_scale` 0.5625, `extra_height_ref` 0, `frame_coverage` ≈ 0.316; khung toàn màn, scale 1.0, extra 480; khung 1080×2519.8 ở y ≈ 90, extra ≈ 600; khung 1152×2048 ở x = 192, scale ≈ 1.0667, extra 0; 0×0 giữ khung trước, không phát `FrameChanged` (sai số ≤ 0.5 px / 0.001).
- `AC-31 [Screenshot]` **GIVEN** scene Running có thêm một vòng tròn tham chiếu (chỉ trong scene test, vẽ trong khung bằng đơn vị tham chiếu), **WHEN** chụp ở 1920×1080, 1080×2400 và 1080×2700, **THEN** 1920×1080 có viền trang trí hai bên và biên khung đo trên ảnh khớp `frame_rect` của AC-30 (607.5×1080 ở x = 656.25, sai số ≤ 1 px do làm tròn pixel); 1080×2400 không có viền và vùng nội dung cao hơn bản 1080×1920 đúng `extra_height_ref` = 480 đơn vị tham chiếu (= 480 px vì `frame_scale` = 1.0, sai số ≤ 1 px); 1080×2700 có viền trên và dưới khớp `frame_rect` (y ≈ 90 px, sai số ≤ 1 px); ở cả ba ảnh, tỉ lệ rộng/cao của vòng tròn tham chiếu đo trên ảnh = 1.00 ± `BT.screenshotAspectTolerance` (không kéo méo).
- `AC-32 [EditMode]` **GIVEN** nguồn kích thước màn giả (cùng kiểu nguồn tín hiệu nền tảng giả của AC-17) và `SF` mặc định, **WHEN** trong cùng một khung đặt kích thước 3 lần (1920×1080 → 1080×2400 → 1080×2700) rồi chạy một bước cập nhật khung; sau đó đặt 1536×2048 và chạy bước cập nhật khung kế tiếp, **THEN** `FrameChanged` phát đúng 1 lần ở mỗi khung (2 lần tổng), lần đầu mang `frameRectPx` của kích thước cuối trong khung (1080×2700 → khung 1080×2519.8 ở y ≈ 90), lần hai mang khung 1152×2048 ở x = 192 (sai số như AC-30); cả hai lần phát trong chính bước cập nhật đọc kích thước mới, trước lời gọi vẽ giả của khung đó.
- `AC-39 [Play mode]` **GIVEN** scene Running ở 1920×1080 (viền hai bên), `InputTestFixture`, một nút UI giả đặt sát mép trái khung và bộ đếm `WorldPress`, **WHEN** gửi một cú nhấn rồi nhả tại giữa viền trái (x = `frame_x` / 2, y = giữa màn), **THEN** 0 `WorldPress`, 0 lần `onClick` của mọi nút, `EventSystem` không chọn đối tượng nào trong khung đó và khung kế tiếp.

**Cổng game và màn chào (B10, B12)**
- `AC-33 [EditMode]` **GIVEN** phiên mới, **WHEN** cú nhấn đầu tiên rơi vào rèm đang Covering (hoặc vào viền, hoặc là một phím), rồi thêm 5 cú nhấn nữa, **THEN** `FirstInteraction` phát đúng 1 lần, ở cú nhấn đầu tiên.
- `AC-34 [EditMode]` **GIVEN** `SF.welcomeOrder` rỗng (MVP), **WHEN** rèm kéo xong, **THEN** `WelcomeQueueCompleted` phát trong cùng khung. **GIVEN** ba bước giả `a`, `b`, `c` với `welcomeOrder` = [`b`, `a`] và `ShouldShow()` của `a` = sai, **WHEN** rèm kéo xong và mỗi bước gọi `onDone`, **THEN** thứ tự hiện là `b` rồi `c`, `a` bị bỏ qua, validator cảnh báo `c` không có trong danh sách, trong lúc mỗi bước hiện cổng giữ đúng 1 token của hàng đợi, và một bước đăng ký sau `WelcomeQueueCompleted` không bao giờ hiện.
- `AC-35 [EditMode]` **GIVEN** stub Platform Services ghi lại mọi lời gọi, **WHEN** chạy khởi động, 3 chu kỳ `Paused`/`Resumed`, 10 lần đổi tab và một lần BootFailed + "Thử lại", **THEN** Bootstrap chỉ phát các sự kiện `BootCompleted`, `Interactive`, `BootFailed`, `FirstInteraction`, `Paused`, `Resumed`, `TabChanged`, `FrameChanged`, `WelcomeQueueCompleted`, `Quitting`, và số yêu cầu quảng cáo do Bootstrap gây ra = 0.

**Hiệu năng**
- `AC-36 [Perf]` **GIVEN** bản WebGL release cấu hình MVP chạy trên đúng hồ sơ `BT.perfReferenceDevice` (Intel Core i5 thế hệ 8, 8 GB RAM, Chrome ổn định mới nhất, DevTools CPU throttling 4×, cache trống trước mỗi lần), trang hiển thị suốt lần đo — không ẩn, không thu nhỏ, không chuyển tab lần nào (lần nào có ẩn thì hủy và đo lại), **WHEN** mở game nguội 5 lần và mỗi lần đọc giá trị trên dòng log `boot_to_interactive` trong console trình duyệt (B5.6; đồng hồ khởi động, từ khung đầu của `MainGame` tới khung rèm Open), **THEN** mỗi lần có đúng 1 dòng log đó và cả 5 giá trị ≤ `BC.maxBootToInteractiveSeconds` (2.0 s).
- `AC-37 [Perf]` **GIVEN** bản phát triển, Running, **WHEN** đo 600 khung có chạm bàn liên tục mỗi `PlinkoConfig.tapCooldown` và 10 lần đổi tab bằng Profiler, **THEN** GC Alloc do router, cổng chặn và đổi tab = 0 B mỗi khung.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| ~~Q1 — NEEDS-USER: mô phỏng khi đang xem rewarded ad (Alpha).~~ **Đã đóng** — user chọn **B** (decision-log 2026-10-21): Platform Services đặt cờ "ad đang mở" (`SetAdOpen`, `IsAdOpen`) và Bootstrap không phát `Paused`/`Resumed` trong khoảng đó; mô phỏng chạy tiếp ở mọi nền tảng, Audio tắt tiếng qua `SetMuteReason(Ad)`. Áp dụng ở B6.2, B6.10, States, Edge Cases, AC-42. Phần bù thu nhập đóng bằng `AdClosed {adSeconds}` (B6.10.5, AC-43). | User | 2026-10-21 (đã đóng) |
| Q2 — ADR khởi động & vòng đời: cơ chế đăng ký `IBootStep`/`IWelcomeStep`, event bus, Run In Background + Page Visibility (jslib) trên WebGL 6000.3, `OnApplicationPause`/`OnApplicationQuit` trên mobile, cách dừng thời gian mô phỏng (không quy định `Time.timeScale`), nguồn giờ thực cho `away_seconds`, cách tắt hiển thị tab, có tách scene Boot hay không. Yêu cầu build đã ghi vào GDD (B6.8 `runInBackground` = đúng, B8.4 hướng `Portrait`, kiểm bằng AC-41); ProjectSettings hiện tại của dự án (`runInBackground: 0`, `defaultScreenOrientation: 4` AutoRotation) trái hai yêu cầu này — game-setup / ADR build sửa. Ghi chú cho ADR: tab ẩn làm `requestAnimationFrame` ngừng nên `Paused`/`Resumed` và `t_pause_utc`/`t_resume_utc` phải lấy trong callback `visibilitychange` của jslib. Feasibility chưa đối chiếu Unity 6000.3 (unity-reference không có module scene flow/core; rủi ro MEDIUM cho Page Visibility). | technical-director → `game-adr` | 2026-11-04 |
| Q3 — ADR UI (uGUI hay UI Toolkit) phải chốt trước khi hiện thực input router, vì hit-test UI khác nhau giữa hai framework. | technical-director → `game-architecture` | 2026-11-04 |
| Q7 — Art-director xác nhận viền trang trí trên Poki desktop 16:9 (khung chỉ chiếm ≈ 32% bề ngang) và thêm board Penpot cho rèm, bảng "Thử lại", viền. | art-director | 2026-11-04 |
| Assumed: phân rã pha theo fix list (ServiceInit → DataLoad → SaveLoad → GameplayInit → Prewarm); dữ liệu tĩnh ở `DataLoad`, mọi hệ có trạng thái kể cả Economy và Inventory ở `GameplayInit` (vì từ VS chúng cần dữ liệu save, đọc ở `SaveLoad`), hệ trình bày (HUD, Feedback) cũng ở `GameplayInit` với `RunsAfter` thay vì thêm pha riêng — giữ đúng tên pha mà các GDD khác đang dùng. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: thứ tự trong pha bằng `RunsAfter` (đồ thị) + hòa theo thứ tự chữ của `Id`, không dùng số ưu tiên — để không có con số nào trong code và kết quả luôn xác định. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: `BC.stepTimeoutSeconds` = 10 s và chỉ đếm thời gian trang hiển thị — rộng hơn nhiều so với ngân sách 2.0 s để máy yếu không gặp lỗi giả; tab ẩn lúc khởi động không gây hết hạn giả. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: "Thử lại" nạp lại scene `MainGame` (không tải lại trang web), không giới hạn số lần — nhanh hơn tải lại trang và giữ được SDK nền tảng đã nạp. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: Feedback & Juice đăng ký bước không cốt lõi (thiếu số bay không chặn game), các hệ gameplay còn lại cốt lõi; GDD của từng hệ có thể đổi cờ `IsCritical` của mình. | game-designer + GDD Feedback & Juice | 2026-10-28 |
| Assumed: `Paused`/`Resumed` không phát trong `Booting.Steps`, chỉ phát từ `Booting.Curtain` trở đi; nếu trang vẫn ẩn lúc `BootCompleted` thì vào `Booting.Curtain` và phát `Paused` ngay trong cùng khung, rèm chỉ kéo khi trang hiện lại; `Resumed` trả vòng đời về đúng trạng thái trước `Paused` — để mọi hệ chỉ nhận sự kiện vòng đời khi đã khởi tạo xong và người chơi không bỏ lỡ cảnh "mở cửa" (review 2026-10-21, B1). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: trang ẩn khi rèm đang kéo → phát `Paused`, rèm đứng yên ở tiến độ hiện tại (RevealFrozen, vẫn giữ token) và kéo tiếp phần còn lại sau `Resumed`, không kéo lại từ đầu; mọi mốc rèm đo bằng đồng hồ khởi động (chỉ đếm khi trang hiển thị) — theo đề xuất của review B1, người chơi luôn thấy trọn cảnh "mở cửa" và `boot_to_interactive` không bị thời gian ẩn làm sai. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: `awaySeconds` theo giờ thực UTC, kẹp ≥ 0, không trần — trần và chống chỉnh giờ thuộc Offline Earnings / Time & Timers (VS). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: tên tab `Board` / `Shelf` / `Shop` (Tab 1/2/3) — chưa GDD nào dùng định danh khác và tab không được lưu (khởi động luôn vào `Board`), nên đổi tên sau này rẻ. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: khóa tab dạng token; chủ khóa duy nhất được đổi tab bằng token của mình — để Tutorial dẫn người chơi qua các tab mà vẫn chặn được người chơi tự đổi. | game-designer + GDD Tutorial | 2026-10-28 |
| Assumed: màn dài hơn 9:21 thì thêm viền trên/dưới (không giãn thêm), màn ngang trên mobile bị khóa ở build — một bố cục duy nhất (Boot D4). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: `FirstInteraction` tính cả cú nhấn vào rèm, viền hoặc phím — một cử chỉ người dùng bất kỳ đủ để trình duyệt cho phép mở khóa âm thanh. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: không tự nhả token theo thời gian; bản dev hiển thị chủ token để bắt lỗi quên nhả — tránh mở cổng giữa một bước hướng dẫn dài. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: hồ sơ máy đo [Perf] tạm thời (`BT.perfReferenceDevice` = laptop Intel Core i5 thế hệ 8 / 8 GB, Chrome ổn định mới nhất, CPU throttling 4×, cache trống) để AC-36 đo được ngay; mốc `tab_switch_ms` tính từ khung xử lý cú nhả nút nav (nút UI kích hoạt lúc nhả); AC-32 chuyển sang [EditMode] với nguồn kích thước màn giả, phần chạm viền tách thành AC-39 [Play mode] (review Round 2, BSF-R2-01). ADR hiệu năng có thể thay hồ sơ máy đo. | game-designer + technical-director (ADR hiệu năng) | 2026-10-21 (đã áp dụng) |
| Assumed: hàng đợi màn chào chạy sau khi rèm kéo xong và dùng danh sách `SF.welcomeOrder` thay cho số ưu tiên; bước đến muộn bị bỏ trong phiên — để popup không bao giờ chen giữa phiên (tông cozy). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: `Interactive` phát đúng một lần mỗi lần nạp scene `MainGame` (cả sau "Thử lại"), ở khung rèm Open sau khi nhả token, và `IsSimulationRunning` ⇔ vòng đời `Running` — tên theo review BSF-R3-02; một truy vấn kèm sự kiện để hệ nào đăng ký muộn vẫn đọc được trạng thái. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: dòng log `boot_to_interactive` ghi ở mọi bản dựng, độ phân giải mili-giây, định dạng chính xác để ADR khởi động chốt — đủ để AC-36 đọc trên bản release mà không cần lớp phủ dev (BSF-R3-01). | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: cờ "ad đang mở" là bool (`SetAdOpen(bool)` / `IsAdOpen`), không đếm lồng và không hẹn giờ tự tắt — mỗi lúc chỉ có một quảng cáo; hẹn giờ cần một con số tùy SDK, và tắt cờ đúng lúc là trách nhiệm của Platform Services (B6.10). | game-designer + GDD Platform Services | 2026-11-11 |
| Assumed: `adSeconds` là thời lượng thô giữa `SetAdOpen(true)` và `SetAdOpen(false)`; hệ tiêu thụ (Economy) tự trừ phần đã mô phỏng thật (web) — Bootstrap không biết thời gian mô phỏng của hệ khác. | game-designer + GDD Economy Core | 2026-10-28 |
| Assumed: hướng màn chỉ `Portrait` (không cho `PortraitUpsideDown`) — đơn giản nhất cho một bố cục dọc duy nhất (Boot D4, BSF-R3-03); GDD Platform Services có thể nới nếu cửa hàng yêu cầu. | game-designer | 2026-10-21 (đã áp dụng) |
| Assumed: bỏ hệ số "¼" khỏi GDD này — ràng buộc `prewarmMaxSeconds` với ngân sách khởi động do Pooling sở hữu và kiểm (BSF-R3-12), để Bootstrap không chứa literal không phải knob của mình. | game-designer | 2026-10-21 (đã áp dụng) |
