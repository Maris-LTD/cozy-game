# Story 007: World Press Input Routing & Modal Back Handler

> **Epic**: Bootstrap & Scene Flow
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: 1-2 days
> **Last Updated**: [set by game-dev when implementation begins]
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md`
**Requirement**: `TR-boot-003`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Tích hợp luồng input Unity New Input System, lọc chạm hợp lệ thành `WorldPress` đưa vào bàn Plinko, và điều phối nút Back theo ngăn xếp modal.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Lọc input an toàn tránh chạm xuyên UI, phát hiện cử chỉ người dùng đầu tiên (`FirstInteraction`).
- Forbidden: Tuyệt đối không để chạm trúng UI sinh ra sự kiện rơi xu vào bàn Plinko.
- Guardrails: Trễ xử lý input router không vượt quá 1 khung hình.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/WorldPressRouterTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `SceneFlowConfig.asset`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-22 [EditMode]`: Khi đánh giá điều kiện phát `WorldPress` vào bàn Plinko: chỉ chấp nhận cú nhấn đầu tiên trong khung hình, nằm trong khung hiển thị `frame_rect`, không trúng phần tử UI, cổng chặn đang mở (0 token), và `ActiveTab` đang là `Board`; nếu là ngón thứ hai, trúng UI, có token chặn, khác tab, hoặc chạm ngoài khung hiển thị thì đều bị bỏ qua (không phát `WorldPress`).
- [ ] `AC-29 [EditMode]`: Khi người chơi nhấn nút Back (hoặc phím Escape): nếu có modal đang mở trong ngăn xếp thì gọi trình xử lý của modal để đóng modal; nếu không có modal mà đang ở tab `Shelf` hoặc `Shop` thì chuyển về tab `Board`; nếu đã ở `Board` trên Android thì gửi yêu cầu đưa app xuống nền (không mở popup hỏi thoát); trên WebGL thì không làm gì. Nhấn Back trong lúc đang khởi động (`Booting`) bị bỏ qua.
- [ ] `AC-33 [EditMode]`: Cú chạm hoặc nhấn phím hợp lệ đầu tiên của người chơi vào phiên (kể cả khi chạm vào rèm che hoặc viền) phát đúng 1 sự kiện `FirstInteraction` để tầng Audio mở khóa Web Audio context trên WebGL; các cú chạm tiếp theo không phát lại sự kiện này.

## Implementation Notes

- Cài đặt `InputRouter` kết nối với hệ thống New Input System của Unity.
- Tích hợp kiểm tra raycast UI (`EventSystem.current.IsPointerOverGameObject`) hoặc qua layer mask để lọc chạm UI.
- Quản lý ngăn xếp `Stack<IModalBackHandler>` cho các popup/modal để xử lý nút Back theo đúng thứ tự LIFO.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 006]: Cổng chặn input trung tâm và router tab.
- [Core/Plinko]: Mô phỏng vật lý và sinh xu sau khi nhận sự kiện `WorldPress`.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Integration
**Required evidence**: An EditMode/PlayMode test in `Assets/CozyGachapon/Tests/EditMode/Bootstrap/WorldPressRouterTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 006
- Unlocks: Story 008
