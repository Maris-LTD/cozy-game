# Epic: Object Pooling

> **Layer**: Foundation
> **GDD**: Docs/design/gdd/object-pooling.md
> **Code area**: Assets/CozyGachapon/Scripts/Foundation/Pooling/
> **Status**: In Progress
> **Stories**: 6 — see table

## Overview

Object Pooling là dịch vụ hạ tầng nền dùng chung nhằm tạo sẵn và tái sử dụng toàn bộ các đối tượng hình ảnh sinh/hủy ngắn hạn (xu Plinko, số bay hiển thị tiền/thu nhập, toast và hiệu ứng trang trí). Hệ thống giúp đảm bảo game vận hành mượt mà ở mức 60 FPS và đạt chỉ tiêu 0 B GC/frame trên môi trường WebGL và thiết bị di động. Các hệ thống khách hàng đăng ký dung lượng nhu cầu tối đa trong pha `GameplayInit`, dịch vụ tự động tính toán dung lượng kèm hệ số đệm (headroom) và tiến hành prewarm toàn bộ đối tượng dưới rèm che khởi động (pha `Prewarm`). Trong suốt phiên chơi, dịch vụ chỉ cho mượn và thu hồi đối tượng qua cơ chế token lease bảo vệ; áp dụng chính sách xử lý cạn pool linh hoạt theo từng loại đối tượng (`Fixed` cho xu Plinko, `RecycleOldest` cho số bay và hiệu ứng trang trí).

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0001: Layered Assembly Architecture | Cô lập module Pooling trong `CozyGachapon.Foundation` kế thừa interface từ `CozyGachapon.Contracts` | LOW |
| ADR-0003: Zero-Allocation Object Pooling | Pool dùng chung typed, token lease an toàn, chính sách Fixed/RecycleOldest, prewarm theo định mức | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-pooling-001 | Zero-allocation object pool service with prewarming for transient visual objects | ADR-0003 ✅ |
| TR-pooling-002 | Per-pool exhaustion policies supporting Fixed and RecycleOldest behaviours | ADR-0003 ✅ |
| TR-pooling-003 | Lease-based borrow and release guards preventing double or stale releases | ADR-0003 ✅ |
| TR-pooling-004 | Diagnostic tracking for active instances, peaks, and leak detection | ADR-0003 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria from `Docs/design/gdd/object-pooling.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/object-pooling/` (nếu có; Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Pool Registration & Capacity | Logic | Complete | ADR-0003 |
| 002 | Prewarm & Boot Steps | Logic | Complete | ADR-0003, ADR-0002 |
| 003 | Lease Get / Release Guards | Logic | Complete | ADR-0003 |
| 004 | Exhaustion Policies (Fixed / RecycleOldest) | Logic | Complete | ADR-0003 |
| 005 | Diagnostics, Zero-GC & Robustness | Logic | Ready | ADR-0003 |
| 006 | Session Reset & Namespace Boundary | Logic | Ready | ADR-0003 |

Deferred to other epics: AC-10, AC-25 [Perf] (device profiling); AC-26, AC-29, AC-33 (Plinko Board); AC-27, AC-34 (Feedback & Juice); AC-28 (real MVP pool prefabs).

## Next Step

Run `game-readiness` on story 001, then `game-dev`.
