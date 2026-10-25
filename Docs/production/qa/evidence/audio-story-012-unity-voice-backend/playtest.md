# Audio Story 012 - Play mode check (AC-36)

- Date: 2026-10-25, Unity 6000.3.16f1 Editor, scene `Assets/CozyGachapon/Scenes/MainGame.unity`
- Steps: enter Play mode, wait for `CozyGachapon.BootstrapController` + 4 s settle (boot to Running, editor platform so no WebGL unlock), then in one eval call `BootstrapController.Audio.Play("Plinko.CoinSpawn", AudioPlayParams.None)` and read the started voice's `AudioSource.isPlaying` in the same frame.
- Result: `played=True reason=None voice=1 isPlayingSameFrame=True clip=tok voiceCount=16 sourcesOnContainer=16 frame=3225`
- Request-to-start latency: 0 frames (<= `T.requestToStartMaxFrames` = 0).
- `AudioVoices` holds exactly 16 `AudioSource` components created at `audio.service` Run; none exist in the saved scene.
- Console: 0 errors, 0 warnings. `unity-harness smoke` PASS (MainGame, 0 console errors).
- Scene left saved and clean before and after Play mode.
