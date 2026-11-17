# Validation

The game was compiled and run with Unity **6000.6.0f1** on macOS. The actual-scene validator passed **84 checks**, including a complete ten-wave win with all **338** enemies destroyed.

The campaign strategy begins with the normal $110, places towers on legal pads, buys additional towers and upgrades only when rewards make them affordable, and places exactly one tower per wave and obeys the ten-tower cap. It does not add credits, change enemy HP or speed, skip enemies, or apply test damage during the campaign. It accelerates the campaign with Unity time scaling; projectiles and enemies still use the normal fixed physics timestep. The focused tests run separately and intentionally create isolated test situations.

## What is checked

- Title state and a clean initial build phase.
- Actual tutorial button callbacks, including the new matching-type merge page: opening, Back/Next, explicit placement text, money/upgrade explanations, finishing without spending cash or a build allowance, and reopening through Help.
- Tutorial pause during combat, preserving cash and wave state on return, preserving an existing manual pause, and restoring the prior 2x speed.
- Persistent selection-and-placement text after the tutorial closes.
- Custom mesh assets present on disk and a curved, predetermined route.
- Mathematical interception of a moving target at the configured launch speed, and rejection of an unreachable target.
- Valid pad placement, rejection of road placement, occupied-pad protection, and the one-per-wave limit during both setup and combat.
- Both tower types and all three required stat differences.
- Merge rejection for different types and self-merges; safe cancellation, empty drops and wrong-type drops; pause/tutorial blocking.
- Matching drag/drop consuming exactly one source, retaining destination pad, higher upgrade level, represented units and investment, without spending cash or restoring the placement allowance.
- Larger fusion geometry, increased stats, repeated fusion, prevention of consuming the same source twice, click-to-merge button routing, and ray selection of tall tower geometry.
- Real projectile hits from a merged tower against a moving enemy.
- Upgrade prices, damage/rate/range growth, actual turret and muzzle height growth, maximum level, insufficient-funds protection, selling refunds, and blocked sell-and-rebuild loopholes.
- Distinct speed and HP values for all six enemy types, including fast motorcycles and 160-base-HP trucks.
- Different vehicle models, fitted box colliders, authored wheel mesh availability, $20/$120 payouts, and prevention of repeat truck payouts.
- Ten planned waves containing 338 enemies and more than 150 Scouts, with both new types present.
- Wave 5 unlocking wave 6 instead of ending the campaign.
- Dynamic Rigidbody projectiles with gravity and colliders.
- Damage and destruction from an actual projectile collision, with a correct cash payout.
- Exact payouts for every enemy type, prevention of double payment, and a complete accounting of campaign money.
- Physical enemy position matching route progress and its reported velocity matching its speed.
- Continuous tower fire at the expected cadence and real hits on moving enemies.
- Pause stopping simulation, stationary enemies while paused, and the 2x setting.
- Immediate defeat after an enemy reaches the route endpoint.
- Restart restoring economy, towers, waves, kill earnings and the single build allowance.
- A legal full-campaign win, with victory delayed until every scheduled and active enemy is gone.

The rendered preview was also inspected for board framing, readable controls, tower models, enemy health bars and range display. The main preview depicts the validator's legal campaign in progress. The merged-tower screenshot is an isolated mechanic demonstration, rather than a normally purchased wave-1 defense. A separate vehicle close-up poses the motorcycle and truck to inspect their graphics; that posing is outside the legal campaign run.

## Run the checks

Outside Play mode, choose **Tools → Neon Bastion → Validate assignment and campaign**. Watch the Console for `DEFENSE PASS` and the final `DEFENSE VALIDATION COMPLETE` message. The validator opens `NeonBastion.unity`; save any unrelated scene edits first. It enters and exits Play mode without saving test placements into the scene.

## Limits

The tests call the same placement, purchase, launch and merge commands used by the UI. Focused merge fixtures also create isolated towers directly; the later campaign resets all state and pays for every tower normally. They exercise drag begin/drop/cancel and merge button callbacks, but do not automate every operating-system mouse/keyboard event or test a built player on other operating systems. A successful scripted strategy proves the campaign has a winning solution; it does not replace human playtesting for feel, difficulty, accessibility or other strategies. Before submitting, play a normal run and verify the placement instructions, camera framing, pause button, upgrades, and end screens yourself.
