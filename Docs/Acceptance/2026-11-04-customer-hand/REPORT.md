# #37 — Player-side coin hand view and palm interaction

Source: `2415099`. The coin camera approaches from the player's actual side, uses world-up and restores the player's complete previous pose and FOV on exit. Official coin faces turn toward the player. Selected pieces lift only slightly, preserving access to the second row.

The palm has a forgiving world interaction target covering palm/fingers. It is disabled during close inspection, so each coin's own collider stays accessible; leaving the view restores the world target. Looking at the hand or its money allows E or left click to open checkout, with a matching prompt. Opening consumes the initiating action; it cannot also select a coin. Existing reach, occupied-hand and payment-state guards remain applicable; stale/missing hands cannot reopen a device.

## Verification

- Fresh Unity compile + checkout: **469 assertions**, 19 suites; 21 new hand checks. [Unity report](unity.json). Actual hand targeting, world/inspection collider switching, consumed opening input, world-up, actual approach direction, every physical coin ray at -30/0/+30 degrees and 16:9/4:3, pose/FOV restore, held-item, distance and stale-hand guards passed.
- Source fingerprint: `0f778f96a60ededb4f9b2cc9028733d1594f23b4e3f6d2ad2e1541bd640bb5b2`; identical before/after source commit. Scope includes pre-existing local rendering settings, which were preserved and excluded from the feature commit.
- Local WebGL: build completed `2026-11-04T21:21:43.482Z`, 23177130 bytes, 3 warnings, 0 errors. Build hashes are in the browser reports.
- Actual browser input: **9 hand-interaction**, **12 coin-payment**, **15 cash-register** checks, 0 console errors. E on the physical hand, Escape, direct mouse opening without selecting money, subsequent coin selection/clear, exact coin and note/coin settlement, no double charge, camera restore. [Hand](hand-interaction.json), [coins](forint-coins.json), [register](cash-register.json).

## Visual / interaction review

Reviewer: Codex, 2026-11-04. Actual native Game views (`Builds/Verification/visual-20261104-210202-888/current.png`, `visual-20261104-211456-828/current.png`) and WebGL screenshots inspected. At **1280×720 and 1920×1080**, the customer remains ahead, fingertips point toward the player, the view is upright, coin lettering is readable and all coin targets remain accessible above the payment controls. Real E and left-click interactions passed. An early browser fixture exposed settled-player aiming and a trigger-aware probe mismatch; setup now settles the real controller before aiming, and the probe ignores placement triggers consistently with the player interaction. The world hand hitbox was broadened and disabled only during close inspection. Result: **passed**.

Original character/hand geometry remains the existing placeholder; this change corrects the viewpoint and interaction. No Pages publication.

![Normal view palm interaction](1280x720-hand-world-prompt.png)

![Upright player-side close view](1280x720-hand-player-view.png)

![Large close view](1920x1080-hand-player-view-large.png)
