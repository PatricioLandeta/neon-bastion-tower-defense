# Validation

The game was compiled and run with Unity **6000.6.0f1** on macOS. The actual-scene validator passed **153 checks**, including a complete ten-wave win with all **522** enemies destroyed.

The campaign strategy begins with the normal $110, places towers on legal pads, buys additional towers and upgrades only when rewards make them affordable, and makes eight purchases and two paid merges across ten waves. It does not add credits, change enemy HP or speed, skip enemies, or apply test damage during the campaign. It accelerates the campaign with Unity time scaling; projectiles and enemies still use the normal fixed physics timestep. The focused tests run separately and intentionally create isolated test situations.

## What is checked

- Title state and a clean initial build phase.
- Actual tutorial button callbacks, including the new matching-type merge page: opening, Back/Next, explicit placement text, money/upgrade explanations, finishing without spending cash, and reopening through Help.
- Tutorial pause during combat, preserving cash and wave state on return, preserving an existing manual pause, and restoring the prior 2x speed.
- Persistent selection-and-placement text after the tutorial closes.
- Live tutorial example cycles base → green → purple → red and deducts the correct example merge fees, replays, keeps animating while the battle is paused, and never changes real towers, money.
- Custom mesh assets present on disk and a curved, predetermined route.
- Mathematical interception of a moving target at the configured launch speed, and rejection of an unreachable target.
- Valid pad placement, rejection of road placement, occupied-pad protection, and repeated affordable placements in one wave during combat.
- Both tower types and all three required stat differences.
- Merge rejection for different types and self-merges; safe cancellation, empty drops and wrong-type drops; pause/tutorial blocking.
- Matching drag/drop charges exactly $50/$100/$200, consumes the source, retains the destination, paid upgrades and total investment, and produces the correct green/purple/red tier and color.
- Buying after merging, merging after buying, and multiple merges in the same wave; cash regulates all purchases.
- Rejection of unequal tiers, insufficient funds, self-merges, wrong types and any merge beyond red, with no money or tower loss.
- Wider geometry with unchanged height/muzzle elevation, exactly 5% compounded range and 20% compounded shot rate per merge tier, height growth only from paid upgrades, increased stats, eight represented units at red, scaled paid upgrade costs, click-to-merge routing and ray selection of tall red geometry.
- Real projectile hits from a merged tower against a moving enemy.
- Upgrade prices, damage/rate/range growth, actual turret and muzzle height growth, maximum level, insufficient-funds protection, selling refunds, and blocked sell-and-rebuild loopholes.
- Distinct speed and HP values for all six enemy types, including fast motorcycles and 160-base-HP trucks.
- Different vehicle models, fitted box colliders, authored wheel mesh availability, $6/$40 payouts, and prevention of repeat truck payouts.
- Ten planned waves containing 522 enemies and more than 240 Scouts, with both new types present.
- Wave 5 unlocking wave 6 instead of ending the campaign.
- Dynamic Rigidbody projectiles with gravity and colliders.
- Damage and destruction from an actual projectile collision, with a correct cash payout.
- Exact payouts for every enemy type, prevention of double payment, and a complete accounting of the $3,652 total campaign kill income. A single Scout cannot fund an immediate upgrade; two-unit merged towers charge double for upgrades.
- Physical enemy position matching route progress and its reported velocity matching its speed.
- Continuous tower fire at the expected cadence and real hits on moving enemies.
- Pause stopping simulation, stationary enemies while paused, and the 2x setting.
- Immediate defeat after an enemy reaches the route endpoint.
- Restart restoring economy, towers, waves, kill earnings and merge history.
- A legal full-campaign win, with victory delayed until every scheduled and active enemy is gone.

Audio checks load distinct original laser assets, verify increasing pitch across all paid/merge levels, require actual tower fire to call the sound system, and check that resets keep the same looping music source. The sound-source count remains bounded. These automated checks do not judge perceived loudness or confirm a music recording that has not been supplied. Scout checks require its new vehicle model and preserve training-wave stats.

The rendered preview was also inspected for board framing, readable controls, tower models, enemy health bars and range display. The main preview depicts the validator's legal campaign in progress. The merged-tower screenshot is an isolated mechanic demonstration, rather than a normally purchased wave-1 defense. A separate vehicle close-up poses the motorcycle and truck to inspect their graphics; that posing is outside the legal campaign run.

## Run the checks

Outside Play mode, choose **Tools → Neon Bastion → Validate assignment and campaign**. Watch the Console for `DEFENSE PASS` and the final `DEFENSE VALIDATION COMPLETE` message. The validator opens `NeonBastion.unity`; save any unrelated scene edits first. It enters and exits Play mode without saving test placements into the scene.

## Limits

The tests call the same placement, purchase, launch and merge commands used by the UI. Focused merge fixtures also create isolated towers directly; the later campaign resets all state and pays for every tower normally. They exercise drag begin/drop/cancel and merge button callbacks, but do not automate every operating-system mouse/keyboard event or test a built player on other operating systems. A successful scripted strategy proves the campaign has a winning solution; it does not replace human playtesting for feel, difficulty, accessibility or other strategies. Before submitting, play a normal run and verify the placement instructions, camera framing, pause button, upgrades, and end screens yourself.

## No-upgrade opening checks

**6 / 6 opening scenarios passed** with graphics enabled: Pulse on pads 0, 1 and 3, and Siege on pads 0, 1 and 3. Each run starts with the normal $110, buys exactly one level-1 tower, launches wave 1, and performs no upgrades, additional purchases, manual damage, or extra-money grants. It must reach the wave-2 build phase without an escape. The earlier version failed the pad-0 and pad-3 Pulse cases, reproducing the reported difficulty problem.

Run **Tools → Neon Bastion → Validate opening without upgrades** outside Play mode to repeat the six cases. This establishes several forgiving openings; it does not promise every pad is equally effective. The gold START HERE marker identifies the recommended opening.
