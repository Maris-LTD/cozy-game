# Review — bootstrap-and-scene-flow — 2026-10-21 — Verdict: APPROVED
Round 1 (vòng review đầu của lượt pipeline này; báo cáo trước cùng tên, hash 0eb2f8c2, cũng APPROVED với 15 khuyến nghị — đã đối chiếu)
Document: Docs/design/gdd/bootstrap-and-scene-flow.md (git hash-object a4a9d436545aafcef2830e8ab41c4d733a65afbd)
Registry: Docs/design/registry/entities.yaml (git hash-object 5935a087f1d2559383f92678fef20999f0580e46)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 14
Prior verdict resolved: Yes (không có mục chặn trước đó)
Summary: Revision 5 đóng hai khuyến nghị lớn của báo cáo trước (thời gian xem ad trên mobile qua `AdClosed {adSeconds}`; kẹt Paused khi cờ ad bật lúc đang Paused qua luật "cờ chỉ chặn việc vào Paused") và Economy/Plinko đã theo `Interactive`/`IsSimulationRunning`. Công thức đúng ở min/max, không mâu thuẫn registry, không còn Open Question NEEDS-USER, không có mục chặn hiện thực MVP. Còn lại là khuyến nghị: `AdClosed` chưa vào AC-35/Summary và chưa có bên tiêu thụ trong GDD khác, các khoảng trống chi tiết mang từ vòng trước, và độ dài 588 dòng vượt mục tiêu 500.
Findings:
- [RECOMMENDED] AC-35 / Summary / B12.1: danh sách sự kiện Bootstrap được phép phát thiếu `AdClosed` (và Summary không nhắc) — AC-35 sẽ trượt khi hiện thực B6.10.5
- [RECOMMENDED] B6.10.5 / Cross-doc: `economy-core.md`, `plinko-board.md`, `audio.md` chưa có `AdClosed` (grep = 0) dù Dependencies của Bootstrap ghi Economy tiêu thụ và tự trừ phần đã mô phỏng thật; Economy (Approved) cần mở lại ở Alpha
- [RECOMMENDED] Cross-doc: `audio.md` dòng 102 vẫn chọn `BootCompleted` làm mốc nhạc với lý do cũ; `object-pooling.md` không nhắc `IsSimulationRunning` (Pooling không mô phỏng nên chấp nhận được, chỉ cần xác nhận)
- [RECOMMENDED] Độ dài: 588 dòng > mục tiêu 500; hai khối "Revision 4/5" ở header và ~20 hàng "Assumed" đã áp dụng có thể rút còn một dòng mỗi cái
- [RECOMMENDED] Edge Case "bấm hai nút nav trong cùng khung" mâu thuẫn B9.5 (router chỉ lọc world; nút nav là UI) — chuyển chống trùng vào `RequestTab` (tối đa một `Changed` mỗi khung) và thêm AC [EditMode] (mang từ vòng trước)
- [RECOMMENDED] B12.3 / AC-33: `FirstInteraction` lúc nhấn xuống bằng chạm không phải user activation trên trình duyệt mobile; Esc không cấp — chạm phát lúc nhả, thêm ca Esc
- [RECOMMENDED] AC-14 / Game Feel: chưa có luật thứ tự trong khung (vòng đời → rèm/cổng → router → tab) nên "chạm trong chính khung rèm kéo xong được giao" không xác định
- [RECOMMENDED] B1.2 / B8.2 / `frame_rect`: thiếu truy vấn `CurrentFrame` cho hệ đăng ký sau `FrameChanged` đầu; `safeInsetsPx` chưa rõ hệ tọa độ; Unity không có sự kiện đổi `Screen.safeArea`; "kích thước 0 giữ khung cũ" không có khung cũ ở khung đầu
- [RECOMMENDED] B4.4 / B12.3: "phiên" chưa định nghĩa (đề xuất = một lần nạp `MainGame`); trạng thái tĩnh sau "Thử lại"; `FirstInteraction` có phát lại ở phiên mới không
- [RECOMMENDED] B6 / States: hành vi `RequestTab`, token, `PushBackHandler`, `SetAdOpen`, hàng đợi màn chào khi Paused chưa được nêu
- [RECOMMENDED] B5.6 / Q2: dòng log release vs luật harness `noDebugLog` và CLAUDE.md — ADR phải chốt kênh ghi được miễn trừ (ví dụ lớp `BootMetricsLog` duy nhất / jslib)
- [RECOMMENDED] Acceptance Criteria: thiếu AC cho `FrameChanged` khi chỉ vùng an toàn đổi, Back ở BootFailed (B11.3), ẩn/hiện trong `Booting.Steps` trước `BootCompleted` (0 sự kiện), `RequestTab` hai lần cùng khung, độ trễ Back; 7 AC [Play mode] cần assembly `CozyGachapon.Tests.PlayMode` + `testables` cho Input System; smoke harness trỏ scene `Game` thay vì `MainGame`
- [RECOMMENDED] Tuning/AC nhỏ: mã màu hex trong Visual/Audio thay vì token palette; "5 lần" của AC-36 là literal (thêm knob `BT.perfColdRunCount`); B9.6 "cùng khung" vs `routerMaxAddedFrames` = 1; AC-37 chưa đặt tên `ProfilerMarker`
- [RECOMMENDED] Sync: header GDD "In Review" ≠ systems index "Approved" (phải cùng một từ); registry/concept (9:21), decision log Boot D5 — để bước Sync xử lý, không sửa ở đây

## Design Review: Bootstrap & Scene Flow
Specialists consulted: none (lean review)
Re-review: Yes, prior verdict APPROVED on 2026-10-21 (báo cáo cùng tên hash 0eb2f8c2); báo cáo này là **Round 1** của lượt pipeline hiện tại

### Completeness: 6/6 required sections present (workflow standard)
Overview, Detailed Design (B1–B12, States and Transitions, Interactions), Formulas (6 công thức: `boot_to_interactive`, `step_timed_out`, `frame_rect`, `away_seconds`, `ad_seconds`, `world_press_routed`, `tab_switch_ms`; không rỗng), Edge Cases, Dependencies, Acceptance Criteria (43 AC, đều có tag và GIVEN/WHEN/THEN). `## Summary`, Tuning Knobs, Visual/Audio, Game Feel, UI Requirements, Cross-References, Open Questions đều có nội dung. Advisory at standard: không có heading Player Fantasy — Overview ghi hệ Foundation không có fantasy riêng; chấp nhận.

### Dependency graph
- ✓ plinko-board.md — hàng Bootstrap có `WorldPress`, `TabChanged`, `Paused`/`Resumed`, `FrameChanged`, `IsSimulationRunning` (19 lần nhắc); chưa có `AdClosed` (Alpha)
- ✓ economy-core.md — R16, `Interactive`/`IsSimulationRunning`, trạng thái Ready khớp B6.5; chưa có `AdClosed`
- ✓ audio.md — `audio.service` ServiceInit không cốt lõi, `Paused`/`Resumed`/`FirstInteraction`/`BootCompleted` khớp; lý do chọn `BootCompleted` còn cũ
- ✓ object-pooling.md — `pooling.service`/`pooling.prewarm`, `prewarmMaxSeconds` 0.1–0.5 khớp Tuning Knobs của Bootstrap
- ✓ pet-catalog.md — bước `pet.catalog` ở `DataLoad`, hai chiều khớp
- Chưa có GDD (đã ghi *provisional*): HUD & Tab Navigation, Feedback & Juice, Tutorial & Onboarding, Inventory, Gacha Machine, Shelf & Synergy, Save/Load, Time & Timers, Offline Earnings, Platform Services

### Theo dõi báo cáo trước
| Mục trước | Trạng thái |
|---|---|
| R1 thời gian xem ad mất thu nhập (NEEDS-USER) | **Đã sửa** — `AdClosed {adSeconds}` (B6.10.5, `ad_seconds`, AC-43); còn thiếu bên tiêu thụ ở GDD khác |
| R2 kẹt Paused khi cờ ad bật lúc Paused | **Đã sửa** — B6.10.1 + AC-42 ca 4 |
| R3 Cross-doc Q8 | Phần lớn đã sửa (Economy, Plinko); còn Audio dòng 102 |
| R4 log release vs `noDebugLog` | Chưa sửa (ADR) |
| R5 chống trùng nav | Chưa sửa |
| R6 `FirstInteraction` user activation | Chưa sửa |
| R7 thứ tự trong khung | Chưa sửa |
| R8 `CurrentFrame` / `safeInsetsPx` / kích thước 0 | Chưa sửa |
| R9 "phiên" và "Thử lại" | Chưa sửa |
| R10 API trong Paused | Chưa sửa |
| R11 AC còn thiếu | Chưa sửa |
| R12 hạ tầng test / tên scene | Chưa sửa |
| R13 ghi chú Unity 6000.3 | Chưa sửa |
| R14 Q6 / Sync | Q6/Q8 đã bỏ khỏi GDD; còn lệch trạng thái header vs index (Sync) |
| R15 knob/AC nhỏ | Chưa sửa |

### Required before implementation
Không có.

### Recommended revisions
Xem danh sách Findings ở đầu báo cáo. Chi tiết mới của vòng này:
1. **`AdClosed` vắng ở AC-35 và Summary.** AC-35 liệt kê đóng các sự kiện Bootstrap được phép phát (`BootCompleted`, `Interactive`, `BootFailed`, `FirstInteraction`, `Paused`, `Resumed`, `TabChanged`, `FrameChanged`, `WelcomeQueueCompleted`, `Quitting`) nhưng B6.10.5 thêm `AdClosed`. Sửa: thêm `AdClosed` vào danh sách AC-35 và một cụm trong Summary/B12.1.
2. **Bên tiêu thụ `AdClosed`.** Bootstrap giao việc "trừ phần đã mô phỏng thật" cho Economy; `economy-core.md` (Approved) chưa có. Không chặn MVP (quảng cáo là Alpha); ghi vào danh sách việc của consistency check, chốt trước GDD Platform Services (2026-11-11).
3. **Độ dài.** 588 dòng; rút header Revision 4/5 và các hàng Assumed đã áp dụng (≈ 20 dòng) để về gần mục tiêu mà không bỏ luật nào.

### Specialist disagreements
Không có (lean review).

### Nice-to-have
- Đánh số lại AC (AC-38, 40, 42, 43 nằm sau AC-21; AC-41 tách mục; AC-39 giữa AC-32 và AC-33).
- AC-31 ca 1080×2400: thêm vạch neo đáy khung để đo `extra_height_ref` trên ảnh.
- AC-15 "bảng gỗ ở giữa": thêm sai số (tâm bảng cách tâm `frame_rect` ≤ 1% bề rộng khung).

### Kiểm tra công thức (min/max, đã tính lại)
- `boot_to_interactive`: min 0.4 + 0.35 = 0.75 s; ví dụ 1.25 / 2.15 s đúng; `boot_work_budget` = 2.0 − 0.35 = 1.65 s; range 0.5–2.8 đúng (1.0 − 0.5; 3.0 − 0.2); validator loại cấu hình suy biến. AC-13, AC-38 (0.10/0.35 = 0.286; 31.25 s; 10.75 s), AC-40 khớp.
- `step_timed_out`: 10.0 s → lỗi ở khung đầu sau 10 s; ẩn 30 s ở giây 2 → ≈ 40 s giờ thực (AC-11) đúng.
- `frame_rect`: 1920×1080 → 607.5×1080, x 656.25, scale 0.5625, extra 0, coverage 0.316; 1080×2400 → scale 1, extra 480; 1080×2700 → 1080×2519.8, y ≈ 90, extra ≈ 600; 1536×2048 → 1152×2048, x 192, scale 1.0667, extra 0. Chỉ kích thước 0 suy biến (mục 8 danh sách).
- `away_seconds` / `ad_seconds`: 330 / 0 (lùi) / 0 (NaN) / 0.05; 30 / 0 — không âm, khớp AC-18, AC-42, AC-43.
- `world_press_routed`: sáu ca AC-22 khớp thứ tự B9.5. `tab_switch_ms`: 16.7 / 33.3 ms ở 60 FPS, ≈ 100 ms ở 20 FPS (chạm ngưỡng) đúng.
- Registry: `max_boot_to_interactive_seconds` 2.0, `step_timeout_seconds` 10, `tap_cooldown`, `max_active_coins`, `tap_to_spawn_max_ms` 50, `prewarm_max_seconds`, `boot_to_interactive`, `away_seconds` — không mâu thuẫn. Hai thay đổi user đã duyệt (nhãn pet scope; knob bounce uplift) không liên quan GDD này và GDD không chứa tên cũ `PetBalanceTargets.bounceUpliftAtCapAssumed`.

### Unity 6000.3
`PlayerSettings.runInBackground`, `PlayerSettings.defaultInterfaceOrientation` / `UIOrientation.Portrait` (AC-41) tồn tại và đọc được từ assembly EditMode; ProjectSettings hiện tại trái cả hai (`runInBackground: 0`, AutoRotation) — AC-41 trượt tới khi game-setup sửa (đúng ý định). Còn thiếu ghi chú cho ADR: `IsPointerOverGameObject` trong callback action, chuột giả sau chạm trên trình duyệt mobile, `moveTaskToBack` qua `AndroidJavaObject`, GameActivity/predictive back, `InputTestFixture` cần `testables`.

### Pillars và tông
Không mâu thuẫn: rèm không thanh tải/logo (cozy), popup chỉ qua hàng đợi màn chào, Bootstrap không phát yêu cầu quảng cáo hay "điểm nghỉ" (Anti-Pillar), ba tab cố định, `defaultTab` = `Board` phục vụ hook Plinko 30 s. 9:21 là mở rộng concept theo Boot D4 — chờ Sync.

### Scope signal
Rough scope signal: L (producer should verify before sprint planning)
- 6+ công thức, 15 hệ phụ thuộc, cần ít nhất 2 ADR (khởi động & vòng đời; UI framework cho router) và một assembly test Play mode.

### Verdict: APPROVED
Không có mục chặn. Nên xử lý trước khi hiện thực phần liên quan: mục thứ tự trong khung, `CurrentFrame`/`safeInsetsPx`, kênh log release, assembly Play mode trước story router/khung/khởi động; `AdClosed` ở AC-35 ngay; bên tiêu thụ `AdClosed` trước GDD Platform Services (Alpha).
