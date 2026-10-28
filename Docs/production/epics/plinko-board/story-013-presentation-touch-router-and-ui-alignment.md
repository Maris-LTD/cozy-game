# Story 013: Viewport camera, định tuyến chạm & hiển thị (Presentation, Touch Router & UI Alignment)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Core
> **Type**: Visual/Feel
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-001`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation
**ADR Decision Summary**: Camera World Space riêng biệt cho bàn Plinko; nhận lệnh chạm WorldPress từ router Bootstrap; hiển thị nhãn hũ thang ấm (x1 gỗ nhạt, x2 matcha, x5 mật ong) độ tương phản WCAG AA ≥ 4.5:1; hai khấc gỗ vùng thả và phản hồi chạm.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Nhận tọa độ chạm qua `WorldPress` từ router Bootstrap (đã lọc UI/modal); nhãn hũ đọc được rõ ràng theo thang màu ấm; camera Plinko giữ ngân sách draw call ≤ 15.
- Forbidden: Không đặt collider hoặc Rigidbody2D dưới Canvas UI; không tự ý lọc modal hay cổng chặn toàn cục.
- Guardrails: Trễ từ chạm được nhận tới khi xu xuất hiện ≤ 50 ms (≤ 3 khung ở 60 FPS); phản hồi bàn bận hiển thị trong ≤ 50 ms.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Runtime/Plinko/` (CozyGachapon.Runtime)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoPresentationTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`, `PlinkoBalanceTargets.cs`
- Scene / prefab: `Assets/CozyGachapon/Prefabs/Plinko/PlinkoBoard.prefab`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-34 [Play mode]**: GIVEN `InputTestFixture`, WHEN lần lượt: chạm trúng nút vặn trứng / header / toast; chạm trong khung bàn nhưng ngoài vùng thả; `T.multiTouchTestFingers` ngón cùng khung hình, THEN lần lượt 0 xu; 1 xu tại mép vùng thả; 1 xu.
- [ ] **AC-35 [Play mode]**: GIVEN bàn Idle, WHEN một cú chạm hợp lệ được gửi qua `InputTestFixture`, THEN xu xuất hiện ở vạch thả trong ≤ `T.tapToSpawnMaxMs` (50 ms); một cú chạm bị bỏ phát phản hồi bàn bận trong ≤ `T.tapToSpawnMaxMs`, và phản hồi bàn bận lặp lại không dày hơn `F.busyFeedbackMinInterval`.
- [ ] **AC-36 [Perf]**: GIVEN bản WebGL chạy trên thiết bị tham chiếu, số xu hoạt động = `C.maxActiveCoins` cộng `RequestBurst(C.burstQueueMax)`, WHEN đo `T.perfSampleSeconds`, THEN frame time p95 ≤ 16.7 ms (60 FPS) và thời gian Physics2D p95 ≤ `T.physicsFrameBudgetMs` (4.0 ms).
- [ ] **AC-37 [Perf]**: GIVEN cùng kịch bản và máy đo như AC-36, WHEN soi Frame Debugger ở khung có số xu hoạt động = `C.maxActiveCoins`, THEN số draw call của camera Plinko ≤ `T.boardDrawCallBudget` (15 draw calls).
- [ ] **AC-39 [Screenshot]**: GIVEN Tab 1 dọc ở độ phân giải tham chiếu `T.screenshotReferenceResolution` (1080×1920), WHEN chụp, THEN nhãn `x5 x2 x1 x2 x5` sinh từ `C.bucketMultipliers` hiện với màu thang ấm (x1 gỗ nhạt, x2 matcha, x5 mật ong) và độ tương phản giữa chữ nhãn và nền hũ ≥ `T.labelContrastMin` (4.5:1); đổi config thì nhãn đổi theo.
- [ ] **AC-40 [Screenshot]**: GIVEN Tab 1 ở độ phân giải tham chiếu, WHEN chụp, THEN thấy hai khấc gỗ đánh dấu vùng thả, tâm mỗi khấc cách `board_center_x ± C.dropZoneWidthRatio * board_inner_width / 2` không quá `T.screenshotPositionTolerancePx` (4 px).
- [ ] **AC-41 [Screenshot]**: GIVEN Tab 1 ở độ phân giải tham chiếu, một xu vào hũ x5 và một xu vào hũ x1, WHEN chụp ở khung có số bay, THEN tâm ngang mỗi số bay cách tâm ngang của đúng hũ nhận xu không quá `T.screenshotPositionTolerancePx`, hũ jackpot sáng mật ong và hiện nhãn "BONUS!" nằm trong bề rộng của hũ đó.

## Implementation Notes

- Cài đặt `PlinkoBoardController` (MonoBehaviour trong `CozyGachapon.Runtime`) kết nối với camera World Space riêng và tiếp nhận sự kiện `WorldPress` từ Bootstrap Input Router.
- Xây dựng Prefab `PlinkoBoard` hoàn chỉnh với các thành phần đinh, nửa đinh tường, thành tường, 5 hũ và 2 khấc gỗ vùng thả, áp dụng material dùng chung 1 Sprite Atlas để giữ draw call ≤ 15.
- Tạo render nhãn hũ bằng TextMeshPro với màu sắc theo đúng thang màu ấm và độ tương phản chuẩn WCAG AA ≥ 4.5:1.

## Out of Scope

- Các hiệu ứng VFX nâng cao (vòng sáng hạt, âm thanh) thuộc về Epic Feedback & Juice.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- Screenshots `Docs/production/qa/evidence/plinko-board/NN-[what].png` + user's visual sign-off, recorded by game-done in Completion Notes

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 012 must be Complete
- Unlocks: None (Epic Plinko Board Complete)

