# Epic: Audio

> **Layer**: Foundation
> **GDD**: Docs/design/gdd/audio.md
> **Code area**: Assets/CozyGachapon/Scripts/Foundation/Audio/
> **Status**: Ready
> **Stories**: Not yet created — run `game-tasks audio`

## Overview

Audio là hệ thống hạ tầng âm thanh chịu trách nhiệm phát toàn bộ âm thanh phản hồi và nhạc nền của trò chơi, hiện thực hóa trải nghiệm Kinetic ASMR ấm cúng và thư giãn (Pillar 1). Hệ thống sở hữu kho cấu hình `SoundCue` dạng ScriptableObject (định nghĩa clip, âm lượng dB, biến thiên cao độ ngũ cung, mức ưu tiên, giới hạn số phiên bản đồng thời và khoảng thời gian kích hoạt tối thiểu), cấu trúc cây bus phân cấp (Master, Music, SFX/Gameplay, SFX/UI), cùng nhóm 16 voice AudioSource cố định với cơ chế cướp kênh âm thanh theo độ ưu tiên (priority stealing). Audio không lắng nghe trực tiếp sự kiện gameplay mà được gọi thông qua interface `IAudioService` từ tầng phản hồi (`Feedback & Juice`). Ngoài ra, hệ thống tự động xử lý mở khóa Web Audio context sau thao tác chạm đầu tiên trên WebGL và duy trì ngăn xếp lý do tắt tiếng (`MuteReason`) độc lập khi hiển thị quảng cáo hoặc người chơi bật/tắt âm thanh.

## Governing ADRs

| ADR | Decision summary | Engine risk |
|-----|------------------|-------------|
| ADR-0001: Layered Assembly Architecture | Định nghĩa interface `IAudioService` và DTO tại Contracts, cài đặt hạ tầng tại Foundation/Presentation | LOW |
| ADR-0004: Priority Voice Management | Nhóm 16 voice cố định, kho SoundCue, thuật toán cướp voice theo ưu tiên, mở khóa WebGL | LOW |

## Requirements

| ID | Requirement | ADR coverage |
|----|-------------|--------------|
| TR-audio-001 | Central SoundCue repository with hierarchical bus gain routing | ADR-0004 ✅ |
| TR-audio-002 | Fixed 16-voice pool with priority stealing and pitch randomization | ADR-0004 ✅ |
| TR-audio-003 | WebGL audio context unlock on first user pointer interaction | ADR-0004 ✅ |
| TR-audio-004 | Mute reason stack tracking for ad presentation and user preference | ADR-0004 ✅ |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed and closed with `game-done`.
- All acceptance criteria from `Docs/design/gdd/audio.md` are verified.
- Logic and Integration stories have passing EditMode tests in the asmdef listed in `testAssemblies` (`unity-harness config`), conventionally `Assets/CozyGachapon/Tests/EditMode/`.
- Visual/Feel and UI stories have screenshots kept in `Docs/production/qa/evidence/audio/` (Visual/Feel cần có sự xác nhận của người dùng trong `game-done`).
- `unity-harness all` passes.

## Next Step

Run `game-tasks audio` to break this epic into implementable stories.
