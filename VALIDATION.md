# Validation

The game was compiled and run with Unity **6000.6.0f1** on macOS. The actual-scene validator passed **47 checks**, including a complete five-wave win with all **68** enemies destroyed.

The campaign strategy begins with the normal $110, places towers on legal pads, buys additional towers and upgrades only when rewards make them affordable, and places exactly one tower per wave and obeys the five-tower cap. It does not add credits, change enemy HP or speed, skip enemies, or apply test damage during the campaign. It accelerates the campaign with Unity time scaling; projectiles and enemies still use the normal fixed physics timestep. The focused tests run separately and intentionally create isolated test situations.

## What is checked

- Title state and a clean initial build phase.
- Custom mesh assets present on disk and a curved, predetermined route.
- Mathematical interception of a moving target at the configured launch speed, and rejection of an unreachable target.
- Valid pad placement, rejection of road placement, occupied-pad protection, and the one-per-wave limit during both setup and combat.
- Both tower types and all three required stat differences.
- Upgrade prices, damage/rate/range growth, actual turret and muzzle height growth, maximum level, insufficient-funds protection, selling refunds, and blocked sell-and-rebuild loopholes.
- Distinct enemy speed and HP values.
- Dynamic Rigidbody projectiles with gravity and colliders.
- Damage and destruction from an actual projectile collision, with a correct cash payout.
- Exact payouts for every enemy type, prevention of double payment, and a complete accounting of campaign money.
- Physical enemy position matching route progress and its reported velocity matching its speed.
- Continuous tower fire at the expected cadence and real hits on moving enemies.
- Pause stopping simulation, stationary enemies while paused, and the 2x setting.
- Immediate defeat after an enemy reaches the route endpoint.
- Restart restoring economy, towers, waves, kill earnings and the single build allowance.
- A legal full-campaign win, with victory delayed until every scheduled and active enemy is gone.

The rendered preview was also inspected for board framing, readable controls, tower models, enemy health bars and range display. The preview depicts the validator's legal campaign in progress.

## Run the checks

Outside Play mode, choose **Tools → Neon Bastion → Validate assignment and campaign**. Watch the Console for `DEFENSE PASS` and the final `DEFENSE VALIDATION COMPLETE` message. The validator opens `NeonBastion.unity`; save any unrelated scene edits first. It enters and exits Play mode without saving test placements into the scene.

## Limits

The tests call the same placement, purchase and launch methods used by the UI. They do not automate every mouse/keyboard event or test a built player on other operating systems. A successful scripted strategy proves the campaign has a winning solution; it does not replace human playtesting for feel, difficulty, accessibility or other strategies. Before submitting, play a normal run and verify the placement instructions, camera framing, pause button, upgrades, and end screens yourself.
