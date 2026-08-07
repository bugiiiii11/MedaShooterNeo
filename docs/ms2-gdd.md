# MedaShooter 2.0 -- Game Design Document

> Status: DRAFT for founder review. Companion doc: `ms2-pre-coding-checklist.md` (Phase 0 runbook -- everything before coding).
> Style: key facts first, tables over prose, grep-friendly headers, no emojis.

## 1. Vision

MedaShooter was the community's most-loved game. MS 2.0 keeps its soul -- a hard, skill-first arena shooter where the best players go far regardless of NFTs -- and fixes what aged: broken wave pacing, endless-only structure, dated presentation, no reason to return daily, no way to fight other players.

### Design pillars (ordered -- when in conflict, higher wins)

| # | Pillar | Consequence |
|---|--------|-------------|
| 1 | Skill decides outcomes | Stat progression is bounded and small; NFTs and levels open OPTIONS, not power ceilings. Model: current NFT caps (max 1.55x HP, +10% speed) |
| 2 | Hard but fair | Difficulty wall stays (enemy dmg scales wave^1.485 vs player wave^1.30); remove unfair moments (dead air, off-screen deaths), not the wall |
| 3 | Every run is dense | No empty waves, no 15s dead-air. If the player is alive, something is trying to kill them |
| 4 | Reasons to return | Daily challenge, duels, progression -- all layered on the same core run |
| 5 | Cheat-resistant by construction | Deterministic spawn schedule gives the server a computable max-score bound per run |

### Non-goals (explicitly out of scope)

- Real-time networked PvP (see 4.6 -- assessed and deferred; async duels deliver the fantasy first)
- Full art replacement in early phases (juice pass first; art swap is a later, separate decision)
- New game modes beyond campaign levels + endless + daily + duels

## 2. Current State Audit

### 2.1 Assets we hold

| Asset | Location | State |
|-------|----------|-------|
| Full Unity source | `MedaShooterNeo/` (git, `dev` branch) | 563 C# scripts, Unity 2021.3.45f2 (installed locally, exact match) |
| Balance reference | `GAME_BALANCE_DOCUMENTATION.md` | Complete: stats, NFT formulas, perks, scaling, clamps |
| Build pipeline doc | `docs/dev-to-prod-merge.md` | dev->main merge + WebGL build to `fe/public/unity-builds/medashooter/Build/` |
| Live prod build | `medashooter.data.v4.gzip` | PATCHED FORK -- UnityPy binary patches (S191/S193 de-branding) NOT yet in source |
| Backend | `backend/app/routes/api_routes.py` | RSA score submit, heuristic anti-cheat + blacklist, cumulative scoring, energy spend |
| Platform plumbing | frontend + backend | NFT stats, boosts, Meda Gas (score/100, 2000/day), XP (score/100, 500/day), weekly seasons, iframe host (`medashooter-frame.html`) |

### 2.2 Known defects (with root-cause hypotheses)

| Defect | Root cause (file) | Fix sketch |
|--------|-------------------|-----------|
| Empty waves / dead air | **CONFIRMED + FIXED IN SOURCE S201 (unbuilt).** See 2.2.1 -- the hypothesis was right about `KillAllEnemies` and **wrong about Snap** | Done: `ClearEnemy()` is the single accounting path for sweeps |
| Waves silently skipped | 15s `CheckForMissingEnemies` watchdog masked the leak by force-advancing waves (`EnemySpawner.cs:113-129`) | **S201: watchdog now resyncs the counter from the live field before advancing** -- see the deviation note in 3.1 |
| Thin endless waves | **CONFIRMED WORSE THAN DESCRIBED + FIXED S202 (unbuilt).** Not merely thin -- the endless profile shipped a wave with no spawnable enemy at all. See 2.2.2 | Done: weighted selection over playable waves + density floor + spawn-site guard + the dead wave deleted from the asset |
| Projectiles never despawn | Nothing destroys a projectile that MISSES: no lifetime, no kill plane, no off-screen despawn. `Weapon.SpawnMultiProjectileDelayed` computed a lifetime from `ProjectileLifeTime` and discarded the value | **FIXED S202:** `SpriteProjectile.Awake` applies a hard ceiling, `SetMaxLifetime` narrows it to the weapon's authored 3-4s. Found while scoping the trail work in 3.2 |
| Perceived dated graphics | No hit feedback, minimal VFX, no post-processing | Juice pass (3.2) before any art decision. **Items 1-5 landed S202** |

#### 2.2.1 Empty waves: the confirmed mechanism (S201)

`currentEnemyCount` is incremented on every spawn and decremented in exactly one place --
`OnEnemyKilled(BasicEnemy)`, wired to `BasicEnemy.OnSendKilledDataToSpawner`. The spawn gate in
`Update()` is `currentEnemyCount < MaxEnemyCount + increaseNumberOfEnemies`, so an over-count
closes the gate.

Both `KillAllEnemies` overloads deactivated enemies with a raw `SetActive(false)`, which never
fires that event. **And the callers are the problem: `BasicBoss.Initialize` and
`MinibossBase.Initialize` both sweep the field on spawn** (`BasicBoss.cs:30`,
`MinibossBase.cs:32`) -- minibosses arrive every 5 waves past wave 10. So the counter gained a
permanent phantom for every enemy alive at each boss arrival. Once the phantoms reached
`MaxEnemyCount`, the gate never opened again: no enemies for the **rest of the run**, while the
15s watchdog rolled the wave over and over. Not "some waves are empty" -- the run was over.

The `if (currentEnemyCount == 0) SpawnCooldown -= ...` recovery on `EnemySpawner.cs:225` cannot
help, because the stuck count is never 0.

Two corrections to the original hypothesis, both from reading the call graph:

- **Snap is NOT a leak.** `SnapAbility` calls `BasicEnemy.ExplodeOnSnap()` -> `Kill(false)`, which
  fires the event and decrements. Same for the off-screen despawn path (`BasicEnemy.cs:208-211`)
  and melee explode. Every path except the two sweeps was already correct.
- **Boss accounting is asymmetric**, and it is a second, much slower leak: a full boss increments
  the counter (Update's `IsBoss` branch falls through to the `++`) but `OnEnemyKilled(BasicBoss)`
  never decrements, while a miniboss does neither. A blanket decrement there would therefore
  under-count minibosses and over-spawn -- there is a comment in the file warning against exactly
  that. The residual +1 per full boss is absorbed by the new resync instead.

#### 2.2.2 The endless dead wave (S202)

`UnendingWavesProfile.asset` held **seven** waves. The seventh (`Index: 0`, `EnemyQuantity: 30`,
`SpawnCooldownRange {0,0}`) had **both** of its `Enemies` entries at `Prefab: {fileID: 0}` with
`ProbabilityInWave: 0` -- an empty inspector row someone left behind. Endless picked uniformly with
`Random.Range(0, Waves.Count)`, so it came up **once every seven wave transitions**.

When it did: `GetEnemyRandomByProbability` took its `sum == 0` branch and returned `Enemies[0]`, an
`Enemy` whose `Prefab` is null, so `EnemySpawner` reached `Instantiate(null)` and **threw**. The
throw happens *before* `currentEnemyCount++`, *before* `spawnedEnemiesCount++` and *before*
`lastSpawnTime` is refreshed, which has three consequences:

- the wave-advance test `spawnedEnemiesCount >= EnemyQuantity` is permanently `0 >= 30`, so the
  wave can never end itself;
- the spawn gate reopens every frame (`SpawnCooldownRange {0,0}` means a cooldown of 0), so the
  exception repeats at frame rate -- of the order of a **thousand IL2CPP stack traces** per
  occurrence, in the browser console, each with real reconstruction cost;
- only the 15s `CheckForMissingEnemies` watchdog breaks the deadlock, so the player gets **15-20
  seconds of an empty screen**, and can draw the same wave again immediately (1-in-49 for
  back-to-back).

The wave still fired a non-silent `NextWaveEvent`, so it granted the per-wave stat upgrade and
ratcheted `DifficultyScaling` while delivering nothing. And because the HUD renders
`NextWave.Index + 1` and this wave's `Index` was 0, **the on-screen wave counter dropped to 1** for
the duration.

Fixed at three layers, deliberately redundant because the data is hand-authored and a stray '+'
click in the inspector will happen again: the asset entry is deleted; `IsWavePlayable` excludes any
wave with no spawnable enemy from selection; and a guard at the spawn site rerolls the wave instead
of throwing.

Separately, the HUD wave number in endless is *still* meaningless -- endless waves carry `Index`
6-11 and are now drawn at random, so the display cycles in 7-12 forever instead of climbing. Not
fixed here; it wants a run-scoped counter, which belongs with the Phase 3 level work.

### 2.3 Anti-cheat today

| Layer | Mechanism | Gap |
|-------|-----------|-----|
| Transport | RSA-encrypted score payload | Protects transit, not gameplay honesty |
| Server heuristics | duration/score ratio, enemies/score ratio, blacklist table | Coarse; XP cap 500/day is the backstop because runs are unverifiable |
| Client | CheatDetector, AntiCheatToolkit, `SpawnEnemyForHackers` (score>17k spawn pressure) | Client-side = bypassable |

Determinism (3.3) is the structural fix: a seeded spawn schedule the server can recompute gives a hard max-possible-score bound per run.

## 3. System Designs

### 3.1 Wave and level system

**Fixes (Phase 1):**
- ~~Single enemy-removal accounting path~~ **DONE S201 (source, unbuilt).** Every path already
  decremented except the two `KillAllEnemies` sweeps; both now go through `ClearEnemy()`, which
  skips already-dead enemies so it cannot double-subtract and clamps at 0. Root cause: 2.2.1.
- ~~Endless wave selection: weighted-random over non-silent waves with a density floor.~~
  **DONE S202 (source, unbuilt).** Three parts:
  1. **Eligibility.** A wave enters the endless rotation only if it is not silent and holds at
     least one enemy with a real prefab *and* a probability above zero. Checking either condition
     alone misses a case, which is how the dead wave survived.
  2. **Weighting.** Selection is proportional to expected enemies-per-minute
     (`60 / mean(SpawnCooldownRange)`, clamped to 1-60 so a `{0,0}` range cannot compute to
     infinity and win every draw). Baselines: five endless waves sit at 20-25/min, the Index-9
     wave at 10.8/min, so it now comes up about half as often instead of equally often. One
     redraw prevents back-to-back repeats.
  3. **Density floor.** The endless spawn gap is clamped to at most 3.0s at wave 10, tightening
     to 1.8s by wave 30. Deliberately endless-only: the campaign profile's pacing is hand-authored
     and its long early gaps are intentional, whereas endless just replays six waves forever and
     the Index-9 wave's range topped out at 9.16s.
  **Balance note for the founder:** the floor raises endless enemy throughput, and score is
  `RewardPoints` per kill, so late-endless scores will run higher than historical ones on the
  cumulative leaderboard. That is the intended direction (pillar 3) but it is a real comparability
  break -- worth watching the first week of leaderboard entries.

  **FOUNDER DECISION OWED -- the backend auto-blacklist gets closer, and it is permanent.**
  `submit_medashooter_score` (`backend/app/routes/api_routes.py:858`) inserts the wallet into
  `medashooter_blacklist` whenever `game_duration * 100 < calculated_score`, returns HTTP success
  anyway, and silently rejects every future submission from that address. The player sees a normal
  game-over and never learns.

  The real driver is pre-existing and quadratic, not this change: `MinibossBase.Kill` awards
  `((waveNumber + 1) - 10) / 5 * 1000`, a reward that grows linearly on a fixed five-wave cadence,
  so cumulative score grows quadratically against a linear duration and eventually crosses any
  fixed score-per-second ceiling. Raising endless density moves that crossing **earlier** -- a
  review estimate puts it around wave 84 instead of wave 90, i.e. deep runs by strong players.
  Note the other heuristic, `enemies_spawned * 250 < calculated_score`, is unaffected: more
  density raises both sides. Hit-stop is also safe here, because its stolen time is added back to
  the duration, which only makes the ratio safer.

  Three options, none of which should be taken unilaterally because they touch the live anti-cheat
  path: (a) accept and watch `medashooter_blacklist` after the playtest; (b) clamp `minibossIndex`
  so the ladder stops compounding; (c) replace the ratio heuristic with the Phase 2 envelope bound,
  which is what it is there to do. **Whatever is chosen, the founder playtest should run past wave
  85 and the blacklist table should be checked afterwards** -- a false positive is permanent and
  invisible.
- ~~Watchdog stays as telemetry, no longer gameplay-corrective.~~ **DEVIATION, founder call owed.**
  It now calls `ResyncEnemyCount()` -- recomputing the count from enemies actually on the field --
  and then still advances the wave. Rationale: demoting it to telemetry-only means any *future*
  leak stalls the run permanently again with nothing to catch it, whereas a resync heals the whole
  bug class within 15s. The wave-advance was kept as a belt-and-braces net; with the counter
  honest it should never fire, and if it does, that is a real stall worth recovering from. If you
  want strict pacing instead (no force-advance ever), that is a one-line change during the pacing
  pass -- but keep the resync.

**Campaign levels (Phase 3):** the engine already supports this -- `EnemySpawner` has profile swapping (`Level2Profile` hook exists) and `EnemyWavesProfile` assets are data-driven. Structure mirrors OD's biome model:

| Level | Theme | Content lever |
|-------|-------|---------------|
| 1-3 | Existing backdrop variants | Curated wave profiles, distinct enemy mixes, one miniboss identity each |
| Boss levels | Every 3rd | Existing bosses (Flail, Missiler, Sniper, AdvancedShooter) get arena-specific patterns |
| Endless | Unlocked after campaign clear (or always -- founder decision) | Current mode, fixed pacing |

Levels = new `EnemyWavesProfile` assets + backdrop swaps, NOT engine work. Score multiplier per level can mirror OD's biome multiplier pattern if we want to steer play.

**Daily challenge (Phase 3):** one fixed server-issued seed per UTC day, one attempt, own leaderboard tab, normal XP/gas rules. Cheapest high-retention feature once determinism exists.

### 3.2 Juice pass (graphics, without new art)

Priority-ordered; all in-source, no new art assets. **Items 1-5 shipped S202 (source, unbuilt).**

| # | Item | Status |
|---|------|--------|
| 1 | Hit feedback: enemy flash + hit-stop + damage-number pop | **DONE.** New `HitFlash` component; hit-stop in `JuiceRuntime`; crit damage numbers finally scale |
| 2 | Screen shake on kills/explosions/ability casts (amplitude-capped, toggleable) | **DONE.** `CameraShake` rewritten in place -- it already existed, wired to exactly one call site |
| 3 | Kill VFX upgrade: reuse FORGE3D effects already in project | **DONE.** `GameEffectsPool.SpawnKillBurst` / `SpawnBossKillBurst`, zero new assets |
| 4 | Post-processing: bloom + vignette + chromatic aberration | **DEVIATION -- delivered differently. See below.** |
| 5 | Muzzle flash + projectile trails per weapon type | **DONE.** FORGE3D muzzle prefabs by `Resources.Load`; trails tinted by `WeaponType`. **Tuned S203:** the halo is scaled per weapon (`JuiceSettings.Pistol*Scale`) -- FORGE3D sized these for a slow-firing shooter and the auto-firing starting pistol strobed |
| 6 | Parallax background layers + ambient particles per level | Phase 3, pairs with campaign backdrops |
| 7 | UI polish: score ticker easing, perk-pickup toast, ability-ready pulse | Open |

**What was already there, contrary to this document's assumptions.** Item 1's flash and item 2's
shake both existed. The flash (`DamageReceiver` -> `F3DCharacterAvatar.TweenColor`) had a real bug:
it captured the rest colour *after* a flash had already started, so a second bullet landing mid-flash
made the pink permanent -- constant under auto-fire -- and it cost 36 AddComponent/Destroy calls per
hit across nine renderers. The shake existed on the camera but was fired from exactly one line in the
whole game (the player surviving damage), decayed in scaled time so a pause left the camera
juddering forever, and shook on Z, which does nothing on an orthographic camera.

#### Item 4: why there is no post-processing stack

Rejected after assessment, and the substitute is `Assets/Scripts/UI/ScreenFxOverlay.cs`. Three
independent reasons, any one of which would be enough:

| Obstacle | Detail |
|----------|--------|
| Gamma colour space | `m_ActiveColorSpace: 0`. Bloom thresholding is non-physical here -- the dead PPv2 profile still in the repo needed `intensity: 9` at `threshold: 0.98` to look acceptable, which is the signature of fighting the colour space. Switching to Linear would re-tone all four shipped scenes and 93 materials |
| WebGL 1.0 still live | `m_BuildTargetGraphicsAPIs` has no WebGL entry, so API selection is Automatic and WebGL 1 remains a fallback. A bloom mip chain degrades silently there |
| Zero fill cost today | The gameplay camera renders straight to the backbuffer (`m_ForceIntoRT: 0`, `m_TargetTexture: 0`, `m_AllowMSAA: 0`). Any post effect forces an intermediate render target plus a blit chain -- a pure fill-rate tax on a 2D side-scroller whose first-class perf target is mobile WebGL |

What ships instead: a runtime-built overlay canvas with a **procedural vignette** (128x128 gradient
generated in code, so no new asset, no meta file, no import settings) and a **damage tint** that
washes red when the player is hit, at half strength when armour absorbed it. "Bloom" is delivered
where it belongs in a 2D game -- as additive glow on the emitters themselves, which is what the
FORGE3D kill-burst and muzzle-flash particles are. Chromatic aberration is dropped; there is no
honest shader-free version and it was the weakest item on the list.

If the founder wants real post-processing later, the upgrade path is `com.unity.postprocessing`
3.4.0 plus a runtime `PostProcessLayer` -- still no scene edit and no pipeline migration -- but pin
WebGL to 2.0 only first and re-tune from scratch rather than trusting the resurrected profile.

**Perf gate.** Every effect is individually toggleable through `JuiceSettings` (PlayerPrefs keys,
lowerCamelCase + `Enabled`, matching the existing `UISettings` convention). Quality is a code-side
two-rung preset, **not** Unity quality levels -- the project has exactly one level ("Fantastic")
shared by every platform, so `SetQualityLevel` can never do anything and adding levels would change
shadows/AA/textures everywhere. `JuiceRuntime` also runs a frame-time watchdog that demotes High to
Low after 3 sustained seconds above a 40fps budget.

**Tuning knob to revisit after the playtest, deliberately not churned into this build:**
`JuiceSettings.KillBurstSeconds` is 1.1s, but the longest ParticleSystem in `Enemy_Explode` is
authored at 1.81s, so the tail of the debris fade is cut when the effect returns to the pool.
Everything else finishes inside the window (FORGE3D sparks 0.56s, energy burst 1.0s). Whether that
reads as "snappy" or "clipped" is a judgement call the playtest should settle -- raising the
constant costs pool residency at high kill rates, so it is not free either. Per-effect hold times
would be the proper fix if the flat constant proves wrong.

**Still owed for item 4 and the perf gate:** there is no in-game UI for the toggles. Adding rows to
the EscMenu settings panel is the one part of this that genuinely needs a scene edit, so it was left
out rather than bundled into an already large source-only change. Until then the toggles are
reachable only via PlayerPrefs and the automatic preset.

### 3.3 Determinism (the keystone)

**Honest scope statement:** full byte-identical replay (OD-style) is NOT achievable here -- OD uses fixed-point Q16.16 specifically because float physics diverges across devices, and retrofitting 563 scripts to fixed-point is a rewrite. What IS achievable and sufficient:

**Spawn-schedule determinism.** One seeded PRNG stream (xorshift/PCG wrapper, NOT `UnityEngine.Random`) drives everything that defines a run's OPPORTUNITY:

| Seeded | Stays unseeded (acceptable variance) |
|--------|--------------------------------------|
| Wave sequence + timing | Crit/dodge/instakill rolls |
| Enemy type choice per spawn | VFX/cosmetic randomness |
| Spawn positions | Enemy micro-movement noise |
| Perk offer rolls | |
| Drop/powerup/mine spawns | |

**Mirror rule (same discipline as MW_XP_TABLE):** the schedule generator is a pure function `(seed, elapsed_time) -> ordered spawn list`, written dependency-free in one C# file with a line-for-line Python mirror in the backend. Server can then compute per run: total spawnable enemies, max reward points, max perk count -> a hard **max-score bound** and expected-stats envelope for validation. This replaces heuristic ratios with computed bounds.

**Two RNG streams minimum:** `scheduleRng` (seeded, mirrored) and `combatRng` (client-only). Never let a combat roll consume from the schedule stream or the mirror desyncs.

Server issues the seed at `/match/start` (duels, daily) or accepts a client seed it records (casual runs). Score submits carry `seed + stats`; server validates against the mirror's envelope.

**S206 corrections to the above (the review that demolished half of this section):** (a) the pure
function is indexed by the WAVE-TRANSITION ordinal, not `(seed, elapsed_time)` -- spawning is
player-reactive, elapsed time is not reconstructible; (b) scope is the wave SEQUENCE only -- spawn
ordinals are ambiguous in principle (boss emits 1 spawn against authored quantity 2; the hacker
branch keys off live score); (c) **the max-score envelope is REFUTED** -- every input to the bound
is a client-chosen integer and MS RSA is encrypt-only, so the formula constrains nobody. Full
mechanism: checklist section on S206.

#### 3.3b Phase 2b -- server-anchored runs (SHIPPED S223, shadow mode)

What replaces the envelope is not a tighter formula but a COST. `POST /run/start` issues
`{run_id, seed, token}` per run; the token is an HMAC only the server can mint; the seed anchors
the (already shipped) schedule. Submits echo `run_id + run_token + seed` as plain fields next to
the legacy RSA params. The server then has, for the first time, facts the client cannot choose:

| Check | Verdict on failure | Class |
|-------|--------------------|-------|
| token is OUR mint, for THIS wallet | `bad_token` / `bad_run_id` | integrity |
| run exists / not already submitted | `unknown_run` / `replayed` | integrity |
| wallet matches the issued run | `wallet_mismatch` | integrity |
| claimed duration fits inside server wall-clock (issue -> submit) | `duration_exceeds_wall` | integrity |
| waves/perks per minute plausible | flags in `checks` JSON only | soft signal |

The wall-clock check is the load-bearing one: forging a 30-minute-grade score now costs 30 real
minutes per attempt per single-use token, instead of zero. Every failure is FAIL-OPEN for the
player: no anchor -> the run plays on its local seed and submits as `unanchored`; legacy builds
(prod data.v4) submit as `legacy` forever-until-promote.

**SHADOW MODE: no verdict rejects anything.** Verdicts accumulate in
`medashooter_run_validations`; enforcement (`MS_RUN_VALIDATION_ENFORCE`) is a separate later
decision taken from that data (target: ~zero false positives over 2 weeks, per founder decision
F6). The ratio heuristics + wrap guard stay until then. Env: `MS_RUN_TOKEN_SECRET` (absent =
anchoring off, everything degrades to pre-2b), `MS_RUN_START_HOURLY_CAP` (default 40).

This endpoint is also Phase 3's daily-challenge seed issuer and Phase 5's duel fairness anchor --
one mechanism, three phases.

### 3.4 Progression: Pilot Level

Bounded-power progression per pillar 1. MW ladder pattern, MS-specific.

| Element | Design |
|---------|--------|
| XP source | Validated score / 100 (same basis as platform XP), own daily cap (proposal: 1500, MW-tier since determinism validation is stronger than MS-classic) |
| Ladder | Fresh ladder from L1, MW-style rungs (first rungs small for early dopamine, founder-tuned like MW 30/40). Cap L40 |
| Mirrors | `PilotLevel` table in ONE C# file + Python mirror -- same two-mirror discipline as MW_XP_TABLE |
| Storage | Backend-owned (new table, see 5) -- client displays, never computes authoritatively |

**Unlocks are options-first (power strictly bounded):**

| Level band | Unlock type | Examples |
|-----------|-------------|----------|
| Early (2-10) | Loadout choice | Pick starting weapon variant; pick 1-of-2 starting perk offer |
| Mid (11-25) | Tactical depth | Ability loadout slots (choose 2 of 4: OhShit/DeepWound/Chain/Snap); 1 perk reroll per run; perk ban list (1 slot) |
| High (26-40) | Identity + small stats | Titles, skins (`SkinManager` exists), trail colors; capped stat bumps totaling at most +10% HP and +5% cooldown reduction across ALL 40 levels |
| Prestige (post-40) | Cosmetic only | Border/badge on leaderboard + duel card |

Rationale: a L40 vs L1 pilot differs mainly in choices available, not in raw numbers -- leaderboards and duels stay skill contests. The total level-based stat delta (<=10% HP) is smaller than the existing NFT delta (55% HP), which the community already accepts as fair.

Open founder decision: veterans' `medashooter_scores_cumulative` stockpile -- start everyone at L1 (clean ladder, recommended: shared "new game" moment, mirrors MW launch) OR grant starting levels from historical cumulative score (rewards loyalty, but day-one maxed veterans kill the ladder as a retention loop).

### 3.5 Async PvP: Duels

Both players play the IDENTICAL seeded run; higher score wins. Same pattern class as OD's deterministic matches -- proven infra thinking, no netcode.

**Flow:**

| Step | Behavior |
|------|----------|
| Create | Player A challenges a specific wallet OR posts an open challenge. Optional Meda Gas wager (equal stake both sides, escrowed) |
| Seed | Server generates seed at duel creation; revealed to each player only at their `/match/start` |
| Play | ONE attempt each. Normal fight-energy spend (existing S179/S187 rules). A's score hidden from B until resolution |
| Resolve | Both played -> higher validated score wins pot minus rake; tie -> stakes returned. Opponent no-show after 48h -> challenger refunded (no forfeit win -- prevents farming alts by spamming challenges) |
| Rewards | Runs earn normal XP/gas within existing caps; wager settlement is a separate transfer OUTSIDE earn caps (it is redistribution, not emission) |

**Anti-abuse:**

| Threat | Mitigation |
|--------|-----------|
| Seed scouting (A tells alt-B the schedule) | Seed revealed only at match start; one attempt; hidden opponent score |
| Wager wash-trading for rake farming | Rake makes wash-trading strictly lossy; per-day duel count cap |
| Cheated scores | Determinism envelope validation (3.3) + existing blacklist; duels flagged `pending_validation` until server check passes |
| Griefing low-levels | Optional: matchmaking bracket by pilot level for OPEN challenges; direct challenges unrestricted |

**Economy:** rake (proposal 10%) is a real Meda Gas sink -- first structural sink beyond upgrades; feeds the gas-sink re-audit owed from MW M0.3. Wager bounds: min 10 / max 500 gas initially.

**Phase 5b -- weekly bracket:** opt-in 8/16-player single-elim tournament, one seed per round, entry fee to pot, settles alongside the weekly season cron. Only after 1v1 duels prove out.

### 3.6 Real-time PvP (assessed, deferred)

| Requirement | Reality |
|-------------|---------|
| Transport | Browser WebGL has no UDP; WebSocket only (head-of-line blocking) |
| Netcode | Server-authoritative sim + client prediction (Photon Fusion 2 / Colyseus / custom) |
| Infra | Hosted game servers + matchmaking + regions; ongoing cost |
| Anti-cheat | Full server sim required -- larger than the entire current MS backend |

Verdict: a bigger project than the whole MW revival, for one mode. Revisit only if duels prove sustained demand for direct competition. Duels' hidden-score + same-seed format delivers the "fight other players" fantasy at a fraction of the cost.

## 4. Phase Plan

> No time estimates (house rule). Each phase ships independently behind the existing dev->prod promote flow. Order is dependency-driven.

| Phase | Name | Contents | Ships when |
|-------|------|----------|-----------|
| 0 | Pre-coding | Parity rebuild from source, port v4 binary patches into source, baseline metrics, founder decisions. Full runbook: `ms2-pre-coding-checklist.md` | Fresh source build == live game on dev |
| 1 | Fix + Juice | 3.1 fixes (counter leak, weighted endless), 3.2 juice pass items 1-5 | Founder playtest sign-off on dev. **Shipped to dev as v7 (S202). Founder played it: "working nicely" -- a positive first read, not yet the deep-run check (see below)** |
| 2 | Determinism | Seeded schedule RNG + Python mirror + envelope validation on submit (shadow mode first: log-only, no rejects) | Mirror matches client schedule on N test seeds; shadow-mode false-positive rate ~0 |
| 3 | Levels + Daily | Campaign profiles + backdrops + juice item 6; daily challenge (needs Phase 2) | Founder playtest; daily runs on dev for a full week |
| 4 | Pilot Level | Ladder + unlocks + backend tables + Vault/profile surface | Founder signs unlock table + ladder tuning |
| 5 | Duels | 1v1 flow + wager escrow + validation gating; 5b bracket later | Duel loop tested with 2 wallets on dev incl. no-show + tie paths |

Phase 2 is the keystone: 3 (daily), 5 (duels), and the anti-cheat upgrade all depend on it. Phase 1 has no dependencies -- start immediately after Phase 0.

## 5. Backend and Data (planned -- no migrations until phases land)

| Table (new) | Purpose | Phase |
|-------------|---------|-------|
| `medashooter_pilot_progress` | wallet, xp, level cache, unlock flags, prestige | 4 |
| `medashooter_duels` | id, challenger, opponent, seed, wager, rake, status, per-side score+validation, expires_at, winner | 5 |
| `medashooter_daily_seeds` | date, seed, per-wallet attempt tracking | 3 |
| `medashooter_match_envelope` (or columns on `medashooter_unity_scores`) | seed, computed max-score bound, validation verdict | 2 |

Existing tables reused: `medashooter_unity_scores`, `medashooter_scores_all`/`_cumulative` (triggers), `medashooter_blacklist`, `seasons`, energy tables. Standing rules apply: gas events dispatch `medaGasChanged`; new gas surfaces join both cap-read queries; migrations verified object-by-object on prod (S133 lesson); seasons stay on the shared weekly cron.

## 6. Build and Deploy Pipeline

| Fact | Value |
|------|-------|
| Editor | Unity 2021.3.45f2 -- installed on founder machine (exact version). Do NOT upgrade the project's Unity version during MS 2.0 (parity risk); revisit after Phase 5 |
| Build target | WebGL -> `fe/public/unity-builds/medashooter/Build/` |
| Host | Same-origin iframe `frontend/public/medashooter-frame.html` (S188 freeze-proof teardown) -- 16:9 contract, opaque `#0F0F23` |
| Cache contract | Build files are immutably cached -- every new build RENAMES the data file (`medashooter.data.v5.gzip`, v6, ...) + updates frame pointer + vercel.json route |
| Automation | Unity batchmode CLI build script (Phase 0 deliverable) so builds are reproducible and not hand-clicked |
| Branch rules | ms repo: `dev` -> `main` per `docs/dev-to-prod-merge.md`; only intentional main/dev diffs are the 2 backend-URL files + version label |

The UnityPy binary-patch pipeline (`data.v4`) RETIRES after Phase 0 -- all changes flow from source. Never touch the `@ENV.cryptomeda.tech` API-template MonoBehaviours contract note becomes irrelevant once URLs live in source constants (they already do: `RestfulManager.cs`, `InventoryBackend.cs`).

## 7. Risks and Open Questions

| # | Item | Owner | Blocking |
|---|------|-------|----------|
| 1 | Source/live drift: does a fresh `dev` build behave identically to live v4? (BoostPackages etc. committed after last build?) | Phase 0 parity build answers | Phase 1 |
| 2 | Veterans: L1 reset vs cumulative-score head start (3.4) | Founder | Phase 4 |
| 3 | XP/gas caps for MS 2.0 (raise 500 XP once envelope validation lands?) | Founder | Phase 2+ |
| 4 | Duel rake % + wager bounds + per-day duel cap | Founder | Phase 5 |
| 5 | Ability loadout (choose 2 of 4) -- confirm it does not gut Snap-dependent high-wave play | Playtest | Phase 4 |
| 6 | ~~WebGL post-processing perf on low-end~~ **CLOSED S202: no post-processing stack is being added.** Rationale + substitute in 3.2 item 4. Preset tiers exist, code-side, in `JuiceSettings` | Decided | -- |
| 7 | Endless unlock: gated behind campaign clear or always open | Founder | Phase 3 |
| 8 | Mobile juice/perf gate. **Correction S202: the premise was wrong -- "touch UI exists" is not true of the shipped scene.** There is no SimpleInput joystick or any touch component in any scene or prefab; game code only calls `SimpleInput.GetAxis`, which falls through to keyboard/gamepad. Mobile is currently a *rendering* target, not a playable one. Phase 1 therefore treats mobile as a perf tier only (auto-demote + watchdog); making it playable is a separate piece of work | Founder | Not Phase 1 |
