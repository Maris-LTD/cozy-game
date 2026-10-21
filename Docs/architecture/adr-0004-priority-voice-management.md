# ADR-0004: Priority Voice Management & Audio Subsystem

> **Status**: Accepted
> **Date**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Decision Makers**: User, Lead Programmer, Technical Director
> **Review Note**: TD-ADR skipped (lean)

## Summary
The game uses a lightweight, zero-allocation audio architecture in `CozyGachapon.Foundation` that manages a fixed pool of 16 `AudioSource` voices with priority-based voice stealing, pentatonic pitch shifting (+/- 4 semitones marimba), and browser WebGL audio unlock. This ensures that high-volume Plinko peg hits never muddy sound output or cause mobile audio starvation.

## Engine Compatibility

| Field | Value |
|---|---|
| **Engine** | Unity 6000.3.16f1 (URP 2D) |
| **Packages** | `UnityEngine.AudioModule` |
| **Domain** | Audio |
| **Knowledge Risk** | LOW |
| **References Consulted** | `references/VERSION.md`, `references/breaking-changes.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | WebGL audio context unlock on first touch and voice stealing under peg storm |

## ADR Dependencies

| Field | Value |
|---|---|
| **Depends On** | ADR-0001, ADR-0002 |
| **Enables** | ADR-0006, ADR-0007 |
| **Blocks** | Feedback & Juice audio translation stories |
| **Ordering Note** | Audio service initializes in ServiceInit phase |

## Context

### Problem Statement
When tens of coins bounce through hundreds of Plinko pegs simultaneously, up to 134 collision events occur every second. Without voice budgeting and rate limiting, this flood crashes WebGL audio contexts, causes harsh digital clipping, and drowns out rare reward sounds (Jackpots, Gacha reveals).

### Constraints
- Must run reliably within mobile browser WebGL memory and audio thread limits.
- Must honor WebGL browser autoplay policy requiring user interaction to unmute audio.
- Must not use heavy Native AudioMixers that have inconsistent WebGL platform support.

### Requirements
- Central `AudioService` managing exactly 16 pooled `AudioSource` components.
- Hierarchical bus gain calculation (Master, Music, Sfx/Gameplay/Plinko/Gacha/Ui).
- Data-driven `SoundCue` assets specifying volume, pitch variation, priority, and instance caps.
- Strict voice stealing rules: high priority cues (Jackpot, UI click) steal oldest low-priority voices (peg hits).

## Decision
We implement `AudioService` in `CozyGachapon.Foundation`:
1. **Voice Pool & Stealing**:
   - 16 pre-allocated `AudioSource` components created at startup (`ServiceInit`).
   - When a cue plays:
     - Check `minInterval` rate-limiting (ignore if fired too rapidly).
     - Check `instanceCap` (drop new or steal oldest instance of the same cue).
     - If voice pool is full: steal the voice playing the lowest priority sound older than a minimum age threshold.
2. **ASMR Pitch & Tone Shaping**:
   - Peg hits support pentatonic scale pitch shifting (+/- 4 semitones) to ensure coin streams sound musical rather than chaotic.
3. **WebGL Autoplay Unlock**:
   - Audio starts muted or suspended until the `FirstInteraction` event (pointer down/up) is detected by `BootstrapController`, unlocking the Web Audio context cleanly.
4. **Mute Reason Stack**:
   - Muting is tracked as a bitmask/stack: `UserMute`, `AdPlaying`, `AppBackgrounded`. Audio is unmuted only when all active reasons are cleared.

### Architecture Diagram

```
[ Feedback / UI / Core ]
           │
           │ PlayCue("Plinko.PegHit", pitch)
           ▼
   [ AudioService ]
           │
           ├── 1. Rate Limit & Instance Cap Check
           ├── 2. Calculate Gain (Cue dB + Bus dB + Master dB)
           ├── 3. Select Voice (Free voice OR Steal Lowest Priority)
           ▼
[ Voice Pool (16 AudioSources) ] ─── Output ───► Speakers / Headphones
```

### Key Interfaces
```csharp
namespace CozyGachapon.Contracts
{
    public enum AudioBus
    {
        Master,
        Music,
        Sfx,
        Gameplay,
        Plinko,
        Gacha,
        Ui
    }

    [Flags]
    public enum MuteReason
    {
        None = 0,
        User = 1 << 0,
        Ad = 1 << 1,
        Background = 1 << 2
    }

    public interface IAudioService
    {
        void PlayCue(string cueId, float pitchOffset = 0f, float volumeMultiplier = 1f);
        void SetBusVolume(AudioBus bus, float volumeLinear);
        void SetMuteReason(MuteReason reason, bool isActive);
        void UnlockAudioContext();
    }
}
```

### Implementation Guidelines
- Subsystems **must never** instantiate their own `AudioSource` components in prefabs.
- All sound playback **must** go through `IAudioService.PlayCue()`.
- Sound effects **must** be configured as `SoundCue` ScriptableObject assets.

## Alternatives Considered

### Alternative 1: Unity AudioMixer Snapshots
- **Pros**: Standard visual tool for ducking and effects.
- **Cons**: Poor and buggy WebGL support; high memory overhead on mobile browsers.
- **Rejection Reason**: Direct software volume scaling across hierarchical buses is lightweight, 100% portable, and zero-allocation.

### Alternative 2: Third-party FMOD / Wwise Middleware
- **Pros**: Advanced audio authoring.
- **Cons**: Substantial build size footprint and licensing cost for a casual HTML5/mobile game.
- **Rejection Reason**: Disproportionate complexity for our scope.

## Consequences

### Positive
- Crisp, pleasant ASMR marimba sounds during heavy Plinko cascades.
- Guaranteed protection against audio glitching and sound context crashes on WebGL.
- Clean coordination with ads and app lifecycle.

### Negative
- Peg hit sounds are intentionally dropped or stolen during heavy coin storms (by design).

## Risks
- **Browser blocking audio permanently if interaction missed**:
  - *Mitigation*: The startup curtain explicitly requires a tap to dismiss or continue, guaranteeing an interaction event.

## GDD Requirements Addressed

| GDD System | Requirement (TR-ID) | How This ADR Addresses It |
|---|---|---|
| audio.md | TR-audio-001 | Central SoundCue repository with bus hierarchy |
| audio.md | TR-audio-002 | 16-voice pool with priority stealing and pitch shifting |
| audio.md | TR-audio-003 | WebGL context unlock on first user pointer event |
| audio.md | TR-audio-004 | Mute reason tracking for ads and background |

## Performance Implications
- **CPU**: Negligible CPU load; software gain calculation is a single float multiplication per voice trigger.
- **Memory**: Fixed memory footprint for 16 audio sources; zero per-frame garbage.

## Validation Criteria
- Continuous spamming of 100 peg hit requests/sec never exceeds 16 active voices.
- High-priority Jackpot sounds always play immediately and steal low-priority peg voices.

## Related
- `Docs/architecture/adr-0001-layered-assembly-architecture.md`
- `Docs/architecture/adr-0002-bootstrap-composition-root.md`
