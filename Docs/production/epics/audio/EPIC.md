# Epic: Audio

> **Layer**: Foundation
> **GDD**: Docs/design/gdd/audio.md
> **Code area**: Assets/CozyGachapon/Scripts/Foundation/Audio/
> **Status**: Ready
> **Stories**: 12 — see table

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

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Audio Contracts, Config Assets, dB & Fade Math | Logic | Ready | ADR-0004, ADR-0001 |
| 002 | Default Cue Table, Placeholder Clips & Config Validator | Logic | Ready | ADR-0004 |
| 003 | Play Admission & Voice Pool Core | Logic | Ready | ADR-0004 |
| 004 | Priority Voice Stealing & Instance Caps | Logic | Ready | ADR-0004 |
| 005 | Storm Scenario & Diagnostics Counters | Logic | Ready | ADR-0004 |
| 006 | Pentatonic Pitch, Clip Selection & Pan | Logic | Ready | ADR-0004 |
| 007 | Bus Gain, Background Bus & User Volume | Logic | Ready | ADR-0004 |
| 008 | Background Suspend, User Mute & Settings Persistence | Logic | Ready | ADR-0004, ADR-0002 |
| 009 | Ad Mute Reason & Reason Stacking | Logic | Ready | ADR-0004 |
| 010 | `audio.service` Boot Step & Failed State | Logic | Ready | ADR-0004, ADR-0002 |
| 011 | WebGL Audio Unlock & Music Start | Logic | Ready | ADR-0004, ADR-0002 |
| 012 | Unity Voice Backend & Bootstrap Wiring | Integration | Ready | ADR-0004, ADR-0002 |

## Deferred acceptance criteria

Các AC dưới đây không có story trong epic này vì cần đầu vào ngoài hoặc epic khác:

- AC-20 [Play mode]: cần Feedback & Juice nối `TabChanged` và scene Plinko thật (epic Feedback & Juice, Plinko Board).
- AC-25 [Play mode]: cần bản build WebGL chạy trên Chrome và Safari cùng bàn Plinko thật (đo thiết bị/trình duyệt, Platform Services / Plinko Board).
- AC-33 [Perf]: cần scene Plinko thật và `ProfilerRecorder` 600 khung (ADR hiệu năng, Plinko Board); EditMode 0 cấp phát đã được bảo đảm bằng quy tắc trong từng story.
- AC-34 [Perf]: cần thiết bị tham chiếu tầm trung và bàn Plinko bão hòa (ADR hiệu năng, Plinko Board).
- AC-35 [Perf]: cần build report WebGL và clip âm thanh thật (ADR build/web, asset thật).
- AC-37 [Screenshot]: icon loa ở header thuộc HUD & Tab Navigation.
- AC-38 [Screenshot] (Alpha): màn Settings thuộc Settings & Accessibility.
- AC-39 [Play mode]: playtest cảm nhận "êm tai" cần người chơi và asset âm thanh thật.
- AC-40 [Play mode]: playtest Tab 2 cần người chơi, Feedback & Juice và asset thật.

Mọi AC còn lại của GDD (AC-1…AC-19, AC-21…AC-24, AC-26…AC-32, AC-36, AC-41…AC-43) có story trong bảng trên.

## Next Step

Run `game-readiness` on story 001, then `game-dev`.
