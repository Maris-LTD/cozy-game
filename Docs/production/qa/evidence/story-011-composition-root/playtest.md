# Story 011 playtest note (2026-10-25)

- AC-10: MainGame, `BootstrapController._failFirstBootForTest` turned on in the open scene (not saved), Play mode 1080x1920.
  - First boot: BootFailed, retry panel shows `debug.failOnce` (`01-bootfailed-failonce-1080x1920.png`).
  - Invoked the "Thử lại" button: active scene `MainGame` reloaded, second boot succeeded, curtain Canvas disabled (Running) (`02-after-retry-running-1080x1920.png`).
  - Not measured: an explicit scene-load counter; the reload was observed through the scene state after the click.
- AC-RUN: `unity-harness smoke` on MainGame with expectTypes [CozyGachapon.BootstrapController]: PASS, 0 console errors, 0 missing scripts; capture shows only the camera background (`03-running-smoke.png`).
