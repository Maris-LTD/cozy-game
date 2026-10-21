# Review — bootstrap-and-scene-flow — 2026-10-21 — Verdict: APPROVED
Round 1 (lượt pipeline này; đối chiếu báo cáo trước cùng tên: APPROVED, 0 mục chặn, 14 khuyến nghị)
Document: Docs/design/gdd/bootstrap-and-scene-flow.md (git hash-object 1830ad98dea575feeb394c0fac7d65cff8ed9b7d)
Registry: Docs/design/registry/entities.yaml (git hash-object 86c897c269e1a6a58b77974d2f52fb7e6151614c)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 13
Prior verdict resolved: Yes (không có mục chặn trước đó; Economy đã tiêu thụ `AdClosed`, Audio đã chuyển mốc nhạc sang `Interactive`)
Summary: GDD đủ 6/6 mục bắt buộc, công thức đúng ở min/max, không mâu thuẫn registry, không còn Open Question NEEDS-USER. Hợp đồng với HUD, Inventory, Gacha, Shelf, Feedback khớp (tên sự kiện, `RunsAfter`, `IsCritical`). Còn lại là khuyến nghị: nhãn *provisional* và "(chưa có)" lỗi thời, `AdClosed` thiếu ở AC-35, các khoảng trống chi tiết mang từ vòng trước, độ dài 589 dòng.
Findings:
- [RECOMMENDED] Dependencies / Cross-References: HUD, Feedback & Juice, Gacha vẫn ghi *provisional* / "(chưa có)" dù GDD đã tồn tại (HUD, Feedback, Inventory, Gacha, Shelf) — cập nhật, bỏ nhãn
- [RECOMMENDED] AC-35 / Summary / B12.1: danh sách sự kiện được phép phát thiếu `AdClosed` (B6.10.5) — AC-35 sẽ trượt khi hiện thực
- [RECOMMENDED] Edge Cases "bấm hai nút nav cùng khung": ghi theo cú nhấn và do router; HUD H-edge / AC-7 xử lý theo cú nhả bằng HUD — thống nhất chủ sở hữu (HUD gọi `RequestTab` một lần; Bootstrap chỉ bảo đảm tối đa một `Changed` mỗi khung) và thêm AC [EditMode]
- [RECOMMENDED] Interactions / Dependencies hàng Shelf, Feedback, Gacha: thiếu `Interactive`/`Paused`/`Resumed`/token chặn mà các GDD đó khai là đầu vào; hàng Plinko chưa ghi `Id` bước `plinko.board` (Shelf `RunsAfter` nó)
- [RECOMMENDED] B7.1: liệt kê Gacha là chủ token trực tiếp, trong khi Gacha/HUD đi qua `ModalHost` (HUD giữ token) — sửa danh sách chủ token
- [RECOMMENDED] B12.3 / AC-33: `FirstInteraction` phát lúc nhấn xuống bằng chạm không phải user activation trên trình duyệt di động, Esc không cấp — phát lúc nhả, thêm ca Esc
- [RECOMMENDED] AC-14 / Game Feel: chưa có luật thứ tự trong khung (vòng đời → rèm/cổng → router → tab), nên "chạm trong khung rèm kéo xong được giao" không xác định
- [RECOMMENDED] B1.2 / B8.2 / `frame_rect`: thiếu truy vấn `CurrentFrame` cho hệ đăng ký sau `FrameChanged` đầu; `safeInsetsPx` chưa rõ hệ tọa độ; Unity không có sự kiện đổi `Screen.safeArea` (phải poll); "kích thước 0 giữ khung cũ" không có khung cũ ở khung đầu
- [RECOMMENDED] B4.4 / B12.3: "phiên" chưa định nghĩa (đề xuất = một lần nạp `MainGame`); `FirstInteraction` có phát lại sau "Thử lại" không
- [RECOMMENDED] B6 / States: hành vi `RequestTab`, token, `PushBackHandler`, `SetAdOpen`, hàng đợi màn chào khi Paused chưa nêu
- [RECOMMENDED] B5.6 / Q2: dòng log release vs luật harness `noDebugLog` và CLAUDE.md — ADR phải chốt kênh ghi được miễn trừ (lớp `BootMetricsLog` duy nhất / jslib)
- [RECOMMENDED] Acceptance Criteria: thiếu AC cho `FrameChanged` chỉ khi vùng an toàn đổi, Back ở BootFailed (B11.3), ẩn/hiện trong `Booting.Steps` (0 sự kiện), độ trễ Back; 7 AC [Play mode] cần assembly PlayMode + `testables` Input System; "5 lần" AC-36 là literal (thêm `BT.perfColdRunCount`); AC-37 chưa đặt tên `ProfilerMarker`
- [RECOMMENDED] Độ dài 589 dòng > 500: rút header Revision 4–6 và ~20 hàng "Assumed" đã áp dụng (≈ 25 dòng) mà không bỏ luật nào

## Design Review: Bootstrap & Scene Flow
Specialists consulted: none (lean review)
Re-review: Yes, prior verdict APPROVED on 2026-10-21 (Round 1 của lượt pipeline này; GDD đã thêm Revision 6)

### Completeness: 6/6 required sections present (workflow standard)
Overview, Detailed Design (B1–B12, States and Transitions, Interactions), Formulas (7 công thức, không rỗng), Edge Cases, Dependencies, Acceptance Criteria (43 AC, đều có tag và GIVEN/WHEN/THEN). Summary, Tuning Knobs, Visual/Audio, Game Feel, UI Requirements, Cross-References, Open Questions có nội dung. Advisory: không có heading Player Fantasy (hệ Foundation, Overview ghi rõ) — chấp nhận.

### Dependency graph
- ✓ plinko-board.md, economy-core.md (`AdClosed`, `IsSimulationRunning`, R16), audio.md (`Interactive` làm mốc nhạc), object-pooling.md, pet-catalog.md
- ✓ hud-and-tab-navigation.md — `RequestTab`, `AcquireInputBlock`, `PushBackHandler`, `FrameChanged.safeInsetsPx` khớp
- ✓ inventory.md — `inventory.core` `RunsAfter` [`economy.core`], cốt lõi, khớp B3/AC-2
- ✓ gacha-machine.md — `gacha.machine`, `RunsAfter` rỗng, `AcquireTabLock`, `ModalHost`, khớp
- ✓ shelf-and-synergy.md — `shelf.core` `RunsAfter` [`economy.core`, `inventory.core`, `plinko.board`], khớp; Plinko đặt `plinko.board` (cốt lõi, `RunsAfter` rỗng)
- ✓ feedback-and-juice.md — `feedback.init` không cốt lõi, nghe `TabChanged`/`Paused`
- Chưa có GDD (đúng ghi *provisional*): Tutorial & Onboarding, Save/Load, Time & Timers, Offline Earnings, Platform Services

### Required before implementation
Không có.

### Recommended revisions
Xem Findings ở đầu báo cáo.

### Specialist disagreements
Không có (lean review).

### Nice-to-have
- Đánh số lại AC (AC-38, 40, 42, 43 nằm sau AC-21; AC-41, AC-39 lạc mục).
- AC-15 "bảng gỗ ở giữa": thêm sai số; AC-31 ca 1080×2400 thêm vạch neo đáy khung.
- Mã màu hex trong Visual/Audio nên là token palette.

### Kiểm tra công thức (min/max)
- `boot_to_interactive`: min 0.4 + 0.35 = 0.75 s; 0.90 → 1.25 s; 1.80 → 2.15 s (trượt); `boot_work_budget` = 1.65 s; validator loại cấu hình suy biến (0.5 + 0.5 < 1.0 vẫn hợp lệ). Đúng.
- `step_timed_out`: 10 s; ẩn 30 s ở giây 2 → ≈ 40 s giờ thực. Đúng.
- `frame_rect`: 4 ví dụ đúng (607.5×1080 x 656.25; scale 1 extra 480; 1080×2519.8 y ≈ 90; 1152×2048 x 192); kích thước 0 xử lý bằng "giữ khung cũ" (thiếu khung cũ ở khung đầu — xem Findings).
- `away_seconds` / `ad_seconds`: 330 / 0 (lùi) / 0 (NaN) / 0.05; 30 / 0. Không âm.
- `world_press_routed`, `tab_switch_ms` (16.7 / 33.3 ms ở 60 FPS, ≈ 100 ms ở 20 FPS) đúng.
- Registry: `max_boot_to_interactive_seconds` 2.0, `step_timeout_seconds` 10, `default_tab` Board, `boot_to_interactive`, `away_seconds` — không mâu thuẫn. Thay đổi user đã duyệt (nguồn `floating_text_merge_window` / `busy_feedback_min_interval`, `gacha_rarity_rates`, giá trị đợt 2) không liên quan GDD này; GDD không chứa tên cũ.

### Unity 6000.3
`PlayerSettings.runInBackground`, `PlayerSettings.defaultInterfaceOrientation` / `UIOrientation.Portrait` (AC-41) dùng được từ assembly EditMode; ProjectSettings hiện tại trái cả hai (đã ghi ở Q2). Ghi chú cho ADR: `visibilitychange` qua jslib, `IsPointerOverGameObject` trong callback action, chuột giả sau chạm trên trình duyệt di động, `moveTaskToBack` qua `AndroidJavaObject`, `InputTestFixture` cần `testables`.

### Pillars và tông
Không mâu thuẫn: rèm không thanh tải/logo, popup chỉ qua hàng đợi màn chào, không phát yêu cầu quảng cáo (Anti-Pillar), ba tab cố định, `defaultTab` = `Board` phục vụ hook Plinko 30 s. 9:21 là mở rộng concept (Boot D4) — Sync xử lý.

### Scope signal
Rough scope signal: L (producer should verify before sprint planning)
- 7 công thức, 15 hệ phụ thuộc, cần ít nhất 2 ADR (khởi động & vòng đời; UI framework cho router) và assembly test PlayMode.

### Verdict: APPROVED
Không có mục chặn. Nên xử lý trước khi hiện thực: `AdClosed` ở AC-35, thứ tự trong khung, `CurrentFrame`/`safeInsetsPx`, kênh log release, assembly PlayMode trước story router/khung/khởi động. Sync: header "In Review" cần khớp trạng thái systems index.
