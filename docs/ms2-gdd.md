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
| Empty waves / dead air | `currentEnemyCount` leaks: `KillAllEnemies()` + Snap deactivate enemies without decrementing (`EnemySpawner.cs:82-107`); spawner thinks field is full | Route ALL enemy removal through one accounting path |
| Waves silently skipped | 15s `CheckForMissingEnemies` watchdog masks the leak by force-advancing waves (`EnemySpawner.cs:113-129`) | Watchdog becomes telemetry-only once the leak is fixed |
| Thin endless waves | Endless picks next wave uniformly at random incl. silent/low-content waves (`EnemySpawner.cs:476`) | Weighted selection; silent waves excluded or budgeted |
| Perceived dated graphics | No hit feedback, minimal VFX, no post-processing | Juice pass (3.2) before any art decision |

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
- Single enemy-removal accounting path: every despawn (killed, sniped, escaped, Snap, KillAllEnemies, boss-clear) decrements through `OnEnemyKilled`-equivalent. Add an assertion counter in dev builds.
- Endless wave selection: weighted-random over non-silent waves with a density floor (min enemies/minute); silent waves only as scripted breathers, never random.
- Watchdog stays as telemetry (log + metric), no longer gameplay-corrective.

**Campaign levels (Phase 3):** the engine already supports this -- `EnemySpawner` has profile swapping (`Level2Profile` hook exists) and `EnemyWavesProfile` assets are data-driven. Structure mirrors OD's biome model:

| Level | Theme | Content lever |
|-------|-------|---------------|
| 1-3 | Existing backdrop variants | Curated wave profiles, distinct enemy mixes, one miniboss identity each |
| Boss levels | Every 3rd | Existing bosses (Flail, Missiler, Sniper, AdvancedShooter) get arena-specific patterns |
| Endless | Unlocked after campaign clear (or always -- founder decision) | Current mode, fixed pacing |

Levels = new `EnemyWavesProfile` assets + backdrop swaps, NOT engine work. Score multiplier per level can mirror OD's biome multiplier pattern if we want to steer play.

**Daily challenge (Phase 3):** one fixed server-issued seed per UTC day, one attempt, own leaderboard tab, normal XP/gas rules. Cheapest high-retention feature once determinism exists.

### 3.2 Juice pass (graphics, without new art)

Priority-ordered; all in-source, no new art assets:

| # | Item | Notes |
|---|------|-------|
| 1 | Hit feedback: enemy flash + hit-stop (2-3 frames) + damage-number pop already exists (`DamageTextSpawner`) -- tune | Biggest perceived-quality lever |
| 2 | Screen shake on kills/explosions/ability casts (amplitude-capped, toggleable) | |
| 3 | Kill VFX upgrade: reuse FORGE3D effects already in project (`Assets/FORGE3D/`) | Zero new assets |
| 4 | Post-processing: bloom + vignette + subtle chromatic aberration on damage | URP/built-in PP stack, check WebGL perf budget |
| 5 | Muzzle flash + projectile trails per weapon type | |
| 6 | Parallax background layers + ambient particles per level | Pairs with campaign backdrops |
| 7 | UI polish: score ticker easing, perk-pickup toast, ability-ready pulse | |

Perf gate: WebGL build must hold 60fps on a mid laptop; every PP effect individually toggleable. Mobile (touch UI exists) gets a reduced preset.

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
| 1 | Fix + Juice | 3.1 fixes (counter leak, weighted endless), 3.2 juice pass items 1-5 | Founder playtest sign-off on dev |
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
| 6 | WebGL post-processing perf on low-end -- may need preset tiers | Phase 1 perf gate | Phase 1 |
| 7 | Endless unlock: gated behind campaign clear or always open | Founder | Phase 3 |
| 8 | Mobile: touch UI exists -- is mobile a first-class target for juice/perf gates? | Founder | Phase 1 |
