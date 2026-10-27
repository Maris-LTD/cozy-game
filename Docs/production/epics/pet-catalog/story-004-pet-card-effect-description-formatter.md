# Story 004: Pet Card Effect Description Formatter

> **Epic**: [Pet Catalog](file:///C:/Users/PC/CodingProgram/cozy-game/Docs/production/epics/pet-catalog/EPIC.md)  
> **Status**: Complete (Verified 1046/1046 tests)
> **Type**: Logic
> **Granularity**: Balanced

---

## 1. Mục tiêu
Hiện thực service / helper định dạng mô tả hiệu ứng Pet cho UI thẻ bài (`PetCardDescriptionFormatter`), hỗ trợ localization string format và định dạng số chính xác theo quy chuẩn UX (giá trị < 1000 dùng 3 chữ số có nghĩa, round half away from zero, loại bỏ số 0 thừa sau dấu phẩy; >= 1000 dùng K/M/B).

## 2. Tiêu chuẩn chấp nhận (Acceptance Criteria)
- [x] **AC-39**: `PetCardDescriptionFormatter.FormatEffect(PetEffect effect)` trả về chuỗi mô tả thân thiện:
  - Hiệu ứng `FlatGoldPerDrop`: `+X Vàng mỗi lượt bóng rơi`
  - Hiệu ứng `PercentGoldBoost`: `+X% Vàng toàn bàn` (hoặc theo scope)
  - Hiệu ứng `ScoreMultiplier`: `xX Điểm thưởng`
  - Hiệu ứng `SpawnRateBoost`: `+X% Tỉ lệ xuất hiện`
  - Hiệu ứng `SynergyCountBonus`: `+X Kích hoạt liên kết`
- [x] Quy tắc format số:
  - Giá trị < 1000: làm tròn đến tối đa 3 chữ số có nghĩa (significant digits), cắt số 0 thừa (`1` thay vì `1.00`, `2.5` thay vì `2.50`).
  - Giá trị >= 1000: hiển thị ký hiệu rút gọn K, M, B (ví dụ: `1.5K`, `2M`).

## 3. Bằng chứng kiểm thử (Evidence)
- EditMode tests trong `CozyGachapon.Tests.EditMode`:
  - `PetCardDescriptionFormatterTests.cs`: Kiểm tra định dạng cho từng loại hiệu ứng, từng scope, các trường hợp số nguyên, số lẻ, số lớn >= 1000.
