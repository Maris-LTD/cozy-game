# Story 003: Startup Curtain Controller & Interactive Milestone

> **Epic**: Bootstrap & Scene Flow
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-22
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md`
**Requirement**: `TR-boot-005`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Điều phối trạng thái rèm "Tiệm mở cửa", che toàn bộ quá trình prewarm đối tượng và phát sự kiện `Interactive` khi sẵn sàng.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Khởi tạo và điều phối rèm che khởi động, giữ cổng chặn input an toàn trước khi vào trạng thái tương tác.
- Forbidden: Tuyệt đối không để rò rỉ input chạm vào world space khi rèm chưa mở hoàn toàn.
- Guardrails: Đảm bảo thời gian từ khung đầu đến khi tương tác nằm trong ngân sách 2.0 s (`maxBootToInteractiveSeconds`).

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/StartupCurtainControllerTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `BootConfig.asset`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [x] `AC-13 [EditMode]`: Rèm che hiển thị tối thiểu `curtainMinDisplaySeconds` (0.4 s), thời gian kéo mở `curtainRevealDuration` (0.35 s); nếu `BootCompleted` phát sớm hơn 0.4 s thì rèm vẫn chờ đủ 0.4 s mới bắt đầu mở; nếu tổng thời gian vượt `maxBootToInteractiveSeconds` (2.0 s) thì ghi nhận cảnh báo vượt ngân sách.
- [x] `AC-14 [EditMode]`: Tại khung hình đầu tiên của scene, rèm ở trạng thái `Covering` và giữ 1 token chặn input; trong suốt trạng thái `Covering` và `Revealing`, mọi tương tác vào world space đều bị chặn; khi rèm mở hoàn tất (`Open`), token được nhả và vòng đời chuyển sang `Running`.
- [x] `AC-38 [EditMode]`: Khi ứng dụng bị ẩn trong lúc rèm đang kéo mở, rèm chuyển sang trạng thái đóng băng `RevealFrozen`, giữ nguyên tiến độ mở và token chặn; sau khi ứng dụng phục hồi (`Resumed`), rèm tiếp tục kéo mở phần còn lại và chỉ nhả token khi đã `Open` hoàn toàn.
- [x] `AC-40 [EditMode]`: Thuộc tính `IsSimulationRunning` trả về `false` trong suốt quá trình khởi động (cả `Booting.Steps` và `Booting.Curtain`); đúng 1 sự kiện `Interactive` được phát tại khung hình rèm đạt trạng thái `Open`, và từ khung hình này `IsSimulationRunning` chuyển thành `true`. Nếu khởi động lỗi (`BootFailed`), `Interactive` không bao giờ được phát.

## Implementation Notes

- Cài đặt máy trạng thái rèm: `Covering` -> `Revealing` (hoặc `RevealFrozen` khi bị gián đoạn) -> `Open` (hoặc `ErrorShown` khi thất bại).
- Liên kết chặt chẽ với cổng chặn input trung tâm (sử dụng token mượn/nhả).
- Lắng nghe sự kiện `BootCompleted` từ Story 001 để bắt đầu kích hoạt chuyển trạng thái sang `Revealing`.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 001]: Chuỗi thực thi các pha khởi động.
- [Story 006]: Cổng chặn input trung tâm và router tab.
- [Story 009]: Giao diện Canvas và Animation hình ảnh của rèm.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/StartupCurtainControllerTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] Created `Assets/CozyGachapon/Tests/EditMode/Bootstrap/StartupCurtainControllerTests.cs` (5 unit tests, passing in `unity-harness all`)

## Dependencies

- Depends on: Story 001, Story 002
- Unlocks: Story 004, Story 009

## Completion Notes

**Completed**: 2026-10-22
**Criteria**: 4/4 passing (0 deferred)
**Harness**: unity-harness all (rules: PASS 7 files, compile: PASS, tests: PASS CozyGachapon.Tests.EditMode 21/21)
**Run result**: N/A - pure C# curtain state controller without visual surface
**Deviations**: None
**Test Evidence**: Assets/CozyGachapon/Tests/EditMode/Bootstrap/StartupCurtainControllerTests.cs (21/21 total suite passing)
**Visual sign-off**: n/a (not Visual/Feel or UI)
**Code Review**: unity-reviewer: 0 blocker / 0 major / 0 minor (clean approval); LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
