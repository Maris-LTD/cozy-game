# Review — plinko-board — 2026-10-21 — Verdict: APPROVED
Round: 1 (đợt review độc lập của workflow này; ghi đè báo cáo cùng tên của đợt trước)
Document: Docs/design/gdd/plinko-board.md (git hash-object d79c572dc256811c5b8385587f9d559908850e19)
Registry: Docs/design/registry/entities.yaml (git hash-object 5935a087f1d2559383f92678fef20999f0580e46)
Scope signal: L (sát XL: 9 phụ thuộc, 10 công thức, nhiều ADR)
Specialists: none (lean review)
Blocking items: 0 | Recommended: 17
Prior verdict resolved: Yes — báo cáo trước (APPROVED, 0 chặn) không có mục chặn; trong 22 khuyến nghị của nó, Revision 7 đã xử lý hai: tên knob bounce uplift (nay `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed`, không còn tên cũ trong GDD) và trần bước mô phỏng `maxSimStep`. Các khuyến nghị còn lại chưa xử lý, được gộp lại dưới đây.
Summary: GDD đủ các mục bắt buộc ở tầng standard, công thức khớp ví dụ khi thay số (sigma, EV, EV hiệu dụng, trần thu nhập 19.7 / 27.1 / 30.7 / 42.2), sự kiện và API khớp Economy Core, Object Pooling, Bootstrap, Audio, và không mâu thuẫn registry. Hai thay đổi user đã duyệt (bỏ "(provisional)" ở scope pet; knob bounce uplift thuộc Economy Core) đã áp đúng ở phía Plinko. Không còn câu NEEDS-USER mở. Tồn đọng là các khoảng trống triển khai và cỡ tài liệu (746 dòng so với mục tiêu 500).

## Design Review: Plinko Board
Specialists consulted: none (lean review)
Re-review: Có làm lại đầy đủ; báo cáo trước APPROVED cùng ngày, tài liệu đã đổi (Revision 7).

### Completeness: 5/5 required sections present (standard: Overview, Detailed Rules, Edge Cases, Dependencies, Acceptance Criteria; Formulas có và không rỗng)
Advisory at standard: thiếu heading Player Fantasy (Summary/Overview thay một phần); Tuning Knobs có đủ nhưng thiếu knob hình học/vật lý (xem khuyến nghị 4). Summary có.

### Dependency graph
- ✓ economy-core.md, object-pooling.md, bootstrap-and-scene-flow.md, audio.md, pet-catalog.md tồn tại; các GDD này liệt kê Plinko với interface khớp (`CoinResolved` + `reason`, `IBaseCoinValueSource`, pool `plinko.coin`, `WorldPress`/`Paused`/`Resumed`/`Interactive`/`IsSimulationRunning`, cue `Plinko.*`).
- Chưa có (hệ chưa thiết kế, không phải tham chiếu hỏng): shelf-and-synergy.md, boosts.md, rewarded-ads.md, feedback-and-juice.md, tutorial-and-onboarding.md. Khi viết, mỗi GDD phải liệt kê Plinko.

### Required before implementation
- none

### Recommended revisions
1. Detailed Rules R11.4.1: `min(dt, maxSimStep)` chưa nói tiến mô phỏng được chia thành các bước cố định `fixed_delta_time` (0.02 s); gọi `Simulate(0.25)` một lần phá `anti_tunnel_constraint`. Cần chia bước và trần số bước mỗi khung (≈ 13 bước, ảnh hưởng ngân sách WebGL, AC-36); ghi ADR cho physics scene riêng hay `SimulationMode2D.Script` toàn cục.
2. R10 / R12.5: chưa định nghĩa bộ hẹn giờ auto-drop khi tổng tốc độ đổi hoặc nguồn đăng ký lại cùng giá trị (giữ phần thời gian đã tích lũy hay đặt lại).
3. R4 / `tap_admitted`: biến `t` thiếu trong bảng biến; cooldown 0.15 s lượng tử theo bước 0.02 s (thực tế 0.16 s); chưa nói `WorldPress` được đệm theo bước thế nào.
4. R1 / Tuning Knobs / `min_passage_gap`: thiếu knob hình học và vật lý (độ cao vạch thả, tầng cuối → miệng hũ, chiều cao vách và độ sâu cảm biến, ma sát, nảy tường/vách, damping, khóa xoay); khe tầng cuối ↔ đỉnh vách không nằm trong `min_passage_gap`. Quy tắc "mọi số là knob" chưa đủ.
5. `coin_rate_ceiling` / AC-13…13d: `T.maxRawPayoutPerSecondPerBaseCoin*` là số tuyệt đối, trượt khi `tapCooldown` xuống 0.10 trong safe range (10 × 2.96 ≈ 29.6 > 24). Biểu diễn bằng tỉ lệ theo `1 / tapCooldown` hoặc ràng buộc safe range.
6. `coin_payout` / R11.3 / Edge Cases: safe range `instantPayoutMultiplier` 1.0–2.0 vượt `evMeanMin` 1.85 và "≤ EV_mean" không có validator; câu "nhận đủ phần thưởng" sai với xu Rain (1.9 so với ≈ 2.96 giá trị mô phỏng). Thu hẹp safe range hoặc thêm validator; sửa câu chữ.
7. R9.2: không làm sạch `BaseCoinValue` từ nguồn runtime không hữu hạn hoặc < 1 (AC-45 khẳng định validator không cảnh báo giá trị runtime); thêm quy tắc kẹp hoặc nêu rõ Economy Core bảo đảm.
8. R12.1 / States: `coinId` cho xu Overflow không qua hàng đợi (tràn trực tiếp, `RequestBurst` lúc Paused) chưa có chỗ cấp; lối ra "tràn `burst_queue_max`" của trạng thái Queued trái R4.2 (xu tràn được trả ngay, không vào hàng đợi).
9. Acceptance Criteria thiếu: R4.5 (pool rỗng), validator `manualReservedSlots` < `maxActiveCoins`, cảnh báo hệ số hũ 0, lỗi dự phòng `baseCoinValue` < 1.
10. AC-15, AC-19, AC-29, AC-39: THEN gộp nhiều nhóm GIVEN (AC-29 gộp ba kịch bản, AC-19 gộp ba cấu hình sai + một ca sim); "sim ở tốc độ tối đa" (AC-19) không có số xu/knob; màu x1/x2/x5 (AC-39) không có giá trị đo. Tách thành AC riêng, thêm giá trị đo.
11. AC-37: soi Frame Debugger là thủ công; thay bằng số đo tự động (`UnityStats.drawCalls` hoặc ProfilerRecorder) ở Play mode/thiết bị đo.
12. AC-35, AC-38, AC-39…41: kiểm phần trình bày của Feedback & Juice (chưa có GDD); gắn nhãn tích hợp hoặc đánh dấu chờ GDD đó.
13. Unity 6000.3 / hạ tầng test: nhóm "Sim" EditMode dựa `SceneManager.CreateScene` + `LocalPhysicsMode.Physics2D` (đã nêu là ADR cần xác minh); chưa có PlayMode test asmdef (chỉ có `CozyGachapon.Tests.EditMode`) cho AC-21, AC-28, AC-34, AC-35; `InputTestFixture` cần package test của Input System. Đặt hạn chốt ADR trước story đầu tiên.
14. Tuning Knobs `tapCooldown`: ràng buộc `cue_throughput` của Audio vỡ khi `tapCooldown` < 0.15 trong safe range; ghi vào "Interacts with" hoặc thu hẹp safe range.
15. Formulas `drop_x`: `coin_radius` 0.12–0.20 khác 0.12–0.19 ở `min_passage_gap` và Tuning Knobs; output range neo cứng vào ±5 s (n = 10), nên viết theo `pegs_per_row`.
16. States (bàn): lối ra "Chờ mở cửa" chỉ theo sự kiện `Interactive`, trong khi R11.4.1 lấy lần đọc `IsSimulationRunning` mỗi khung làm nguồn sự thật; thống nhất một nguồn.
17. Độ dài và cách viết: GDD 746 dòng (mục tiêu ≤ 500), bảy đoạn revision ở đầu file dài (mỗi revision nên một dòng), nhiều câu lặp giữa R11, Edge Cases, States và AC. Cắt revision về một dòng mỗi cái, gộp phần lặp; thiếu Player Fantasy (advisory).

### Specialist disagreements
- none (lean review)

### Nice-to-have
- Open Questions: gộp các dòng "Assumed" cũ vào Detailed Rules hoặc xóa; chuyển các dòng đã đóng sang decision-log.
- Cross-References: thêm báo cáo review này; dòng "Review Round 3" nên trỏ tới mục đã xử lý.

### Senior verdict [creative-director]
Không chạy (lean review). Đánh giá chính: thiết kế giữ đúng Pillar 1 (khoảnh khắc xu gỗ ASMR) và Pillar 3 (kênh chủ động có trần mềm, cân theo cấu hình); không có vi phạm tone. Không cần quyết định mới từ user.

### Scope signal
Rough scope signal: L (producer should verify before sprint planning)

### Sync cần làm (không sửa ở bước review này)
- `Docs/design/gdd/systems-index.md` và dòng `> **Status**:` của GDD: đặt cả hai thành `Approved` theo verdict này.
- `Docs/design/registry/entities.yaml`: bỏ "(provisional)" ở scope của `pet.cozy_fox` (dòng 20) và `pet.chubby_king_cat` (dòng 62) theo quyết định Pet scope (a); `plinko_bounce_uplift_at_cap_assumed` đã có `referenced_by` plinko-board.md, pet-catalog.md (không cần sửa).

### Verdict: APPROVED
