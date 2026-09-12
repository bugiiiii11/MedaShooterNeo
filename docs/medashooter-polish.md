# MedaShooter -- polish record

Execution record for the polish run audited in `swarm-meta/medashooter-polish-audit.md` (S305). One
section per sprint, appended in order. The audit holds the findings and the citations; this file holds
what was actually done, what was verified how, and what still needs a human.

**Constraint for every sprint:** scores are RSA-signed and validated server-side; the game is Unity
WebGL, so a C# or `.asset` change is a NEW BUILD the founder makes locally (Unity 2021.3.45f2) and
prod is pinned at build v14 (dev runs v17, next suffix **v18**, never reuse one). A level's wave COUNT
is mirrored in four places. Nothing in this run reaches prod players until the S269 MS hold is lifted.

---

## Measuring guide -- how to read the verdict instrument

`backend/scripts/ms_verdict_stats.py` (sprint 1, item F4) is the instrument for the whole PvP question
and for any decision about enforcing anti-cheat. It is strictly read-only: eight SELECTs, no writes.

```bash
cd backend
python scripts/ms_verdict_stats.py                      # last 8 ISO weeks
python scripts/ms_verdict_stats.py --since 2026-08-22   # since the shadow clock closed
python scripts/ms_verdict_stats.py --json               # machine-readable
python scripts/ms_verdict_stats.py --sql                # print the SQL, connect to nothing
```

Needs `DATABASE_URL` (or `--dsn`). **Supabase pooler port 6543, transaction mode** -- 5432 has refused
every connection for an entire session before (S145). No DSN handy, or reading PROD without putting a
prod credential on a laptop: `--sql` prints all eight queries with the window already inlined, ready to
paste into the Supabase SQL editor.

What each table answers:

| Table | The question it settles |
|---|---|
| 1. Coverage | Is there data at all, and does it span the window you think it does |
| 2. Verdict totals | The headline mix: ok / integrity failures / unanchored / legacy |
| 3. Verdict x mode x ISO week | Is the failure rate stable, rising, or concentrated in one week or one mode |
| 4. **Enforcement preview** | **What enforcement WOULD have rejected**, per mode, under two different policies |
| 5. Which checks keys fired | Whether the soft plausibility flags ever trigger at all |
| 6. Flags split by verdict | Whether a flag fires on CLEAN runs (false-positive shape) or only on already-failed ones |
| 7. Mixed wallets | The false-positive candidates: wallets with both clean and failed history |
| 8. Daily attempts by verdict | The daily board specifically -- the first enforcement candidate |

**The two policies in table 4 are different decisions, which is why both print:**

- `reject_strict` -- reject anything that is not `ok`. This kills `legacy` too, and legacy is every
  pre-Phase-2b client, i.e. what prod still runs at build v14. Strict on prod today would reject real
  players.
- `reject_integrity` -- reject only the six integrity verdicts (`bad_run_id`, `bad_token`,
  `unknown_run`, `wallet_mismatch`, `replayed`, `duration_exceeds_wall`). `legacy` and `unanchored`
  pass. `unanchored` means the client's `/run/start` failed (server down, rate limit, slow link) -- an
  ops signal, not a cheat signal.

**Reading the mode column:** `medashooter_run_validations` has no mode column. Mode comes from the
joined run row, and an unanchored submission has no run row at all -- for those it falls back to
`checks.client_mode`, which is an **unsigned client claim**. A mode that came from there is a hint.
Rows with neither show as `(none)`.

**What the instrument does NOT do:** it does not enforce anything. Turning enforcement on is a separate
human decision (`MS_RUN_VALIDATION_ENFORCE`). F4's job was only to make the numbers visible -- before
this script, nothing in any repo could read `medashooter_run_validations`.

---

## Sprint 1 (S306, 2026-09-12) -- measure, decide, spec

Picked by the founder in S305: F4, F3, G0. 2.5 points, no Unity build.

### What changed

**F4 -- the verdict readout.** New `backend/scripts/ms_verdict_stats.py` (~330 lines, read-only).
Eight queries over `medashooter_run_validations` joined to `medashooter_runs`, plus one over
`medashooter_daily_attempts`. Four output modes: default tables, `--json`, `--sql` (prints pasteable
SQL, connects to nothing), and a plain-English "Reading" paragraph that states the mix and says what it
does and does not prove. Usage and interpretation: the measuring guide above.

Chose a script over the console endpoint the audit also offered. `CONSOLE_API_KEY` is still unset on
Railway (the standing SwarmOps P0), so a `/api/admin/console/` handler would have returned 401 and
produced exactly zero numbers today -- and getting numbers is the entire point of F4. The eight queries
are written so a console endpoint can wrap them later without change (SwarmOps SB-1 queue).

**F3 -- the duel framing, into the GDD.** `MedaShooterNeo/docs/ms2-gdd.md` section 3.5. The section
opened with "Both players play the IDENTICAL seeded run ... same pattern class as OD's deterministic
matches". That premise is false, and it was load-bearing for a wagered feature. Replaced with:

- A correction block with a two-column table of what the seed fixes (which wave asset plays at each
  transition -- `ms_schedule.py:8-25` says so itself) versus what it does not (enemy type per spawn
  `EnemyWavesProfile.cs:124`, spawn position `EnemySpawner.cs:347`, cooldown `:601`, the score-driven
  extra-spawn branch `:311`, plus HP rolls, perks, drops, powerups, mines).
- Why client-side `Random.InitState(seed)` would not close it either: draw ORDER depends on frame
  timing and player input, so two runs diverge on the first spawn.
- The honest product that survives: async duels as **same level, same wave schedule, best score** --
  fair in expectation, never a replay. Enough for a wager.
- A **binding copy rule**: player-facing text says "same conditions" / "same wave schedule", never
  "identical run", "same run" or "mirror match", on any surface including marketing.
- Two stale anti-abuse rows fixed in the same table. "Cheated scores" cited the determinism envelope,
  which S206 refuted (every input is a client-chosen integer; MS RSA is encrypt-only) -- now points at
  the anchored-run verdict and carries the explicit gate that **no wager may settle on a verdict that
  has never rejected anything**. "Seed scouting" now notes the campaign wave order comes from the
  profile, not the seed, so scouting only buys anything once a run reaches endless.

**G0 -- the level identity sheet.** New `swarm-meta/medashooter-level-identity-sheet.md`. One page,
founder signs, drives Sprint 2. Holds: the four things that cannot be negotiated (wave counts 10/11/11
and their four-place mirror, shared endless, the `BasicBoss` requirement on the boss wave, minibosses
staying out of campaign waves); the 9-prefab roster table marking the **four prefabs that have never
appeared in any campaign** (`SnipingBasicEnemy`, `SnipingTripleEnemy`, `EnemyWithAllWeapons`, and the
boss variant `FlailBoss_BombrunnerAdd`); and a proposed identity per level -- name, theme line,
difficulty intent, roster, what never appears, boss, tint, particle, decals, daily label, miniboss
order. The proposal spends all four unused prefabs and gives L3 its own boss, so it needs **zero new
art**. Five open questions carry a signature block.

### Verified -- automated

- **All eight F4 queries run green against a real Postgres.** Spun a throwaway `postgres:15-alpine`
  container, applied the actual `migration_ms_run_anchoring.sql` + `migration_ms_daily_challenge.sql`
  (so the schema is the real one, including the `unity_score_id` UUID retype), seeded synthetic rows
  covering every verdict, both modes, all four soft flags, one mixed wallet and one pure-fail wallet.
  Verified correctness, not just absence of errors: the mixed wallet appears in table 7 and the
  pure-fail wallet correctly does not; an unanchored row resolves its mode from `client_mode`; a legacy
  row shows `(none)`; table 4 separates strict from integrity-only. Container removed afterwards.
- `--sql` output piped through `psql -v ON_ERROR_STOP=1`: all eight statements execute, zero errors.
  That is the path the founder uses on prod, so it is proven rather than assumed.
- `--json` parses and carries all ten top-level keys. Empty-window case prints the "no rows" reading
  instead of dividing by zero.
- Script is import-clean on the local interpreter (asyncpg 0.31.0) and imports nothing from `app/`, so
  it runs without the web3 stack -- same pattern as the other `backend/scripts/` tools.

### Checks that need a real device / a human

1. **Run F4 against DEV** -- `cd backend && python scripts/ms_verdict_stats.py --since 2026-08-22`
   with the dev `DATABASE_URL` (pooler, 6543). Confirms the queries work on live data shapes.
2. **Run F4 against PROD** -- the numbers that matter, since prod is where people actually play.
   Either set `DATABASE_URL` to the prod pooler, or run `--sql` and paste into the Supabase SQL editor.
   All eight are SELECTs; safe to paste.
3. **Read table 4 and answer one question:** enforce for `daily` (and later `duel`) only, leaving
   `normal` in shadow -- yes or no? If table 7 shows mixed wallets, enforcement would have hit real
   players and the answer is no until that is explained.
4. ~~Sign G0~~ -- **DONE 2026-09-12, same session.** Accepted as specified, no changes; Sprint 2 is
   unblocked. The four taste calls stay free to change until the v18 build exists.
5. **Read the F3 correction** in GDD 3.5 and confirm the copy rule is acceptable, because it constrains
   marketing: duels can never be sold as an identical run.

**The only genuine blocker on this lane is item 1/2: I cannot reach either database.** No
`DATABASE_URL` is set locally and both Supabase MCP servers are unauthenticated in this session, so the
F4 numbers need a human once -- a DSN in the environment, an authorized MCP, or a paste of the `--sql`
output. Everything else in the run proceeds without it, except Sprints 4-5 (duels), which are gated on
those numbers by design.

No build in this sprint, so nothing to playtest and no new suffix.

### Tooling gotchas

- `docker cp` / `docker exec -f /tmp/x.sql` under Git Bash on Windows: paths get mangled into
  `C:/Users/...` and psql reports "No such file or directory". `export MSYS_NO_PATHCONV=1` first.
- A large Python file with mixed quoting does not survive a Bash heredoc reliably here -- it died with
  "unexpected EOF while looking for matching `''". Write the file with the Write tool instead.
- `handoff.md` is 125 lines but 51 KB: individual lines run to thousands of characters, so `sed -n`
  ranges still blow the output cap. `cut -c1-700` on the range is the way to read it.
- Postgres in a container was ready ~1s after `docker run`; no polling loop needed beyond a `pg_isready`
  check.

### Not in this sprint

Sprint 2 (G1(a)+(b), G2(a), G5, C1(a), C2 -- one build, v18) is gated on the G0 signature. Sprint 3
(C6(a), G4(a), F5 daily) has no dependency on Sprint 2. Sprints 4 and 5 (F2, the duel build, Risk H)
remain **unticked** and are additionally gated on F4's numbers coming back clean.

---

## Build v18 (S308, 2026-09-12) -- the build that makes sprints 2 and 3 testable

Dev had served **v17** since S307, so every C# item in sprints 2 and 3 existed only as source. This
is the build that runs them. Unity 2021.3.45f2, `-msEnv dev -msVersion v18`, 778 s, **0 compile
errors** -- the first result worth recording, since several scripts had never been type-checked.

Shipped as frontend `120f90d` (the four artifacts + `vercel.json` + `medashooter-frame.html`, one
commit so the tree is never half-built on the remote) and MedaShooterNeo `c018f13` (the menu label,
now `v1.3.4 [DEV] b18` -- it had drifted to v1.3.3 while dev served v17, which made it useless for
telling a playtester which build they were on).

Verified live on dev, not assumed: all four artifacts serve with the right MIME types and byte sizes
matching the local build (loader 19003, framework 87971, data 38.3 MB, wasm 8.3 MB), and the retired
v17 loader falls through to the SPA.

**Two traps worth keeping:**

- **`vercel.json` needs the version bumped in BOTH halves of each route.** The `src` half is a regex
  that escapes its dots (`medashooter\.loader\.v18\.js$`), so a plain `s/\.v17\./\.v18\./`
  updates only `dest` and leaves each route matching a filename that no longer exists. Caught here
  before commit; the symptom would have been the game silently not loading on dev.
- **A 200 does not prove an artifact is served.** The SPA `index.html` fallback is also a 200 --
  every one of the four "succeeded" at 12,279 bytes of `text/html` while the deploy was still
  building. Poll on `content_type`, never on the status code.

Unity batchmode also fails with `No valid Unity Editor license found` when the Hub sign-in token has
expired (it had, since 28 July). The guard fails before the output folder is cleared, so a failed
build leaves the previous one intact. There is no GUI fallback for this build: `BuildWebGLDeploy`
carries no `[MenuItem]`, and the two menu items that do exist skip the URL guard, the version suffix
and the output path.

## Sprint 2 (S306, 2026-09-12) -- level identity: rosters, art pass, selector, result screen

Picked in S305, unblocked by the G0 signature the same day: G1(a), G1(b), G2(a), G5, C1(a), C2.
**Build v18 now exists (S308) and is live on dev** -- every item below compiled with 0 errors
and is playable. They are `built, not yet playtested`: the human checklist at the end of this
file is what still separates them from verified.

### What changed

**G1(a) -- roster split per level.** `Data/DefaultEnemyWaveProfile.asset`,
`Data/Level2WaveProfile.asset`, `Data/Level3WaveProfile.asset`. The three levels drew from one
five-prefab roster and L2 waves 2-3 were byte copies of L1. Each level now has its own roster from the
signed sheet, and the three prefabs that had never appeared in a campaign are in play:

| | Roster after | Gone from this level |
|---|---|---|
| L1 Dust Run | BasicEnemy, TripleShoot, SpeedrunnerBasic | RoundShoot, SniperEnemy |
| L2 Cold Front | BasicEnemy, RoundShoot, SniperEnemy, **SnipingBasicEnemy** | TripleShoot, both speedrunners |
| L3 Scorch | BasicEnemy, RoundShoot, SniperEnemy, SpeedrunnerQuick, **SnipingTripleEnemy**, **EnemyWithAllWeapons** (w8-w10 only) | TripleShoot, SpeedrunnerBasic |

Wave COUNTS untouched (10/11/11), so the four-place mirror needed no edit. Stat ranges were carried
across each prefab swap rather than re-rolled, with two deliberate shape changes: L1 w4 trades a
500-HP round-shooter for a speedrunner (the tutorial should teach movement, not tanking), and L2 w8
stops being a 30-enemy speedrunner rush -- it is a ranged set-piece now at Qty 14 / Max 6, because 30
snipers is a different game. L3 w8 keeps a rush but mixes in `EnemyWithAllWeapons` at Qty 20 / Max 10.

**G1(b) -- BLOCKED, and the audit row was factually wrong.** The audit said
`FlailBoss_BombrunnerAdd.prefab` was "still a `BasicBoss`, so the win path holds". It is not. Its root
script is `FlailBombRunnerEnemy : SpeedrunnerEnemy`, and the prefab contains **zero** references to
`BasicBoss` or `FlailBoss` -- it is an ADD the FlailBoss fight spawns. `EnemySpawner.cs:299` does
`obj.GetComponentInChildren<BasicBoss>()` and immediately `bossEnemy.Initialize(this)`, so the swap
would have thrown an NRE and left a boss wave with no exit (`OnEnemyKilled(BasicBoss)` is the only
one). The swap was authored, caught in verification, and reverted; L3 ends on `FlailBoss` like the
others. **`FlailBoss.prefab` is the only `BasicBoss` prefab in the project** -- the one other subclass,
`MinibossBase`, is barred from campaign waves by the audit's own Do-not list -- so a distinct per-level
boss is new content, not a data edit. Audit row, sheet row and taste call 2 all corrected.

**G2(a) -- per-level backdrop, zero new art.** `Resources/Backdrops/Level{1,2,3}.asset`:
`OverrideDecals` flipped to 1 on all three, each with its own `MainPlaneAdditions` /
`ForegroundAdditions`. L1 gets ground litter only at the lowest density (0-3 at p0.7 / 0-2 at p0.4),
L2 takes the crystals, L3 takes all four rocks at the highest density (2-6 at p1.0 / 2-4 at p0.85).
L1 also finally has an ambient particle: `Sniper_Barrel_Smoke_01`, the thinnest of the four FORGE3D
smokes the other two levels already repurpose, tinted to dust at alpha 0.15. Tints left exactly as the
signed sheet records them.

An earlier correction of mine was wrong and is fixed in both docs: the Rock and Crystal sprites are
NOT unused. The scene already feeds Ground-stuff to the main plane and Rock+Crystal to the foreground
(`develop_overhaul.unity:11404-11427`) -- identically for every level. The real gap was never missing
art, it was ONE shared decal set applied to all three levels. G2(a) splits it.

The trap here: `BackgroundResolver.CreateDecals` calls `Instantiate(additions.Prefab)` and
`additions.Sprites.Random()` unguarded. A profile with `OverrideDecals: 1`, a null Prefab and an empty
Sprites list would throw the moment a decal rolled -- which is exactly what the existing all-zero
blocks would have become if the flag alone were flipped. Every block written carries the scene's real
decal holder prefab and a non-empty sprite list.

**G5 -- stale indices on L2/L3.** Reproduced `CalculateIndices()` exactly
(`EnemyWavesProfile.cs:34-51`): `WaveDifficulty` = array position, `Index` = running count of
non-silent waves, silent waves take `index - 1`. L2/L3 had two waves both at `Index 1 / Diff 3` and
then `Diff 6` before `Diff 5`; both are monotone now (Idx 0,0,0,1,2,3,4,4,4,5,6 / Diff 0-10). L1 was
already correct. **The row's caveat about the last wave's `WaveDifficulty` is moot**: both fields are
editor-only bookkeeping -- nothing reads either at runtime (`GetIndexForWave` walks the list live and
its only caller sits inside `#if UNITY_EDITOR`), and the endless chain seeds off `PreviousProfile` =
`DefaultEnemyWaveProfile`, which was already right. The last `WaveDifficulty` moved 9 to 10 and shifts
no difficulty.

**C1(a) -- the selector has names.** `Scripts/UI/MsModeSelectBootstrap.cs`. The buttons are 140x70
canvas units, too small for a name and a blurb, so the name goes on the button (`L2` / `COLD FRONT`,
two lines) and the theme line into a new display-only caption in the 36-unit gap between the selector
row and the Daily button. The Daily button reads `DAILY L2 COLD FRONT` at all three places that write
it. Level identity moved to `MsLevelSelect.LevelName` / `LevelBlurb` because three surfaces name a
level and three copies is how one goes stale. The web Daily card
(`frontend/src/pages/MedaShooterPage.jsx`) shows the name too -- without it the names would exist only
inside the Unity build, which is held.

**C2 -- the result screen says which run ended.** `Scripts/UI/UIGameOverScreen.cs`. A summary line
under the score (`L2 COLD FRONT - WAVE 17 - DAILY`), built by cloning the score label at runtime --
the same zero-scene-YAML pattern the rest of Phase 3 uses -- with the inherited typewriter component
disabled so it does not animate the text away. After a daily run the Retry button relabels to
`PLAY AGAIN (NORMAL RUN)`, because Retry has always silently played a normal run of the sticky level
(the daily attempt is burned at `/run/start`) and nothing said so. The button is found by its
serialized call to `OnClickRetryButton`, not by GameObject name, so a rename cannot break it silently.
Both are wrapped in a try/catch: a summary line must never be what stops a game-over screen appearing.

### Verified -- automated

- 45 MS backend tests pass (`test_ms_schedule_parity.py`, `test_ms_run_guard.py`,
  `test_ms_wrap_guard.py`). Parity is the one that matters: it proves the wave-count mirror is intact.
- Wave and backdrop data re-parsed after writing and asserted against the signed sheet: counts
  10/11/11, rosters exactly as specified, `EnemyWithAllWeapons` only in L3's last three waves, indices
  monotone, one boss prefab guid per level.
- Frontend `npm run build` clean. ESLint on the touched file went 20 to 14 errors: the edit added two
  `react/prop-types` errors, so `DailyChallengeCard.propTypes` was added, which also cleared six
  pre-existing ones.
- C# brace/paren/bracket balance unchanged against the HEAD versions; cross-file members
  (`MsLevelSelect.LevelName` / `LevelBlurb` / `IsDaily` / `EffectiveLevel`) confirmed present.
- **Not verified: anything that needs Unity.** No compile, no play. See the checklist.

### Checks that need a real device / a human

1. **Build v18** (Unity 2021.3.45f2, never reuse a suffix) and push it to dev. The C# compiles or it
   does not -- that is the first thing v18 tells us, and three C# files changed.
2. **Play L1, L2, L3 to the boss and 5 waves into endless each.** Each level should feel like a
   different roster (no triple-shooters or speedrunners in L2, no snipers in L1), and three
   never-shipped prefabs are on screen for the first time: `SnipingBasicEnemy` in L2,
   `SnipingTripleEnemy` + `EnemyWithAllWeapons` in L3. **Balance is the open question**: L1 should feel
   easier than before, L2 w8 is a new kind of wave, and L3 w8-w10 have an enemy nobody has ever fought.
3. **Look at the backdrops.** L1 sparser than before, L2 with crystals, L3 cluttered with rock. L1
   should have a faint dust drift where it previously had nothing. If L3 reads as too busy, the fix is
   one `AmountRange` / `SpawnProbability` edit.
4. **The level-select screen.** Names on the three buttons, the theme line under them. The caption
   position is the single most likely thing to need a nudge -- it was placed by arithmetic against the
   other two rows, not by eye. It cannot eat a click (raycasts stripped), so worst case is cosmetic.
5. **Finish a daily run.** The result line should end in `- DAILY` and Retry should read
   `PLAY AGAIN (NORMAL RUN)`. Then check the web Daily card names today's level.
6. `medashooter_blacklist` unchanged after the playtest.

### Tooling gotchas

- Unity prefab references use the ROOT GameObject fileID plus the prefab's own guid, and **prefab
  variants share their parent's root fileID** (`SniperEnemy`, `SnipingBasicEnemy` and
  `SnipingTripleEnemy` are all `8966015197849417581`). Deriving it from the file -- the Transform whose
  `m_Father` is `{fileID: 0}`, then its `m_GameObject` -- reproduced the existing references exactly,
  which is what made authoring new prefab references safe without Unity.
- A prefab's name is not its type. The only reliable check is grepping the prefab for the component
  guid the consuming code actually requires.
- `git stash` / `git stash pop` around a lint run is the cheapest way to prove an error count is
  pre-existing rather than newly introduced.
- A large Python file with mixed quoting still does not survive a Bash heredoc here (second time) --
  write it to the scratchpad with the Write tool and run it.

### Not in this sprint

Sprint 3 (C6(a), G4(a), F5 daily) has no dependency on this one. Sprints 4-5 (F2 duels, Risk H) are
still unticked and still gated on the F4 numbers, which remain unread. **New for the Later pool: a
distinct per-level boss needs new content** -- it was the one signed item that turned out to be
impossible, and it is now the largest remaining gap in level identity.

---

## Sprint 3 (S307, 2026-09-12) -- per-level boards, daily runs that mean it, and the phone gate

Picked rows: **G4(a)**, **F5 (daily half)**, **C6(a)**. 3 pt. No dependency on Sprint 2, so this ran
without waiting for build v18. Mobile-visible item last, per the audit's ordering.

Commits: BE `f91c459`, MSNeo `33adba8`, FE `3a1fbfc` -- one per item, all on `dev` and pushed.

### What changed

**G4(a) -- level + mode on the score rows.** `backend/migration_ms_score_level_mode.sql` (new) adds
two nullable columns to `medashooter_scores` and `medashooter_scores_all`, backfills them from two
independent sources, and adds a partial index for the board read. The write path fills them going
forward: `_ms_shadow_validate` now returns a third value, an `anchor` dict from the server-issued run
row (`api_routes.py:1074-1082`), and all three score writes carry it -- the `scores_all` insert
(`api_routes.py:1382-1420`), the PB update (`:1466-1500`) and the first-timer insert (`:1503-1536`).

The design decision worth keeping: **NULL means "we do not know", never "Level 1".** Only a
server-anchored run row sets these columns. The client's `level` field is an unsigned claim
(`JsonBuilder.cs:68-76`) and stays where its provenance is unambiguous -- `checks.client_level` on
`medashooter_run_validations`. The alternative, writing the claim and adding a third provenance
column, would have put an unverified number on a board row and then asked every future reader to
remember to filter it. G4(b), the per-level multiplier, must read these columns and never the claim;
the audit's "Do not apply a level multiplier for unanchored runs" is what this shape enforces
structurally rather than by convention.

Backfill has two sources because the two link paths cover different rows: the verdict row
(`medashooter_run_validations.unity_score_id` -> `run_id` -> `medashooter_runs`) covers anchored
normal runs, and `medashooter_daily_attempts` covers daily runs whose score id never reached a
verdict row. Both join on `unity_score_id::text` on both sides -- there is no DDL for
`medashooter_scores_all` anywhere in the repo (it predates the migrations), so the cast makes the
join work whether that column is UUID or TEXT on a given environment.

**The read path: `GET /api/game/medashooter/scoreboard/by-level`** (`api_routes.py:1871-1985`).
`level` (validated against `MS_PROFILES`, not a literal -- the level count already has four mirrors
and this is not becoming a fifth), optional `mode`, `limit`, optional `player_address` for the
outside-the-top-N self row. Reads `scores_all`, so it spans seasons unlike the season board.

It returns a **`coverage` block** (`rows_total`, `rows_with_level`, `rows_this_level`). That is the
instrument for this item: without it an empty board is unreadable -- nobody has played Level 3, or no
rows carry a level yet? On an environment where no anchored run has submitted since the
daily-challenge migration, `rows_with_level` is legitimately 0 and climbs from the next submission.

**F5 (daily) -- a daily run is anchored or not played.** The daily challenge is everyone playing the
SAME server-issued seed. An unanchored daily run was a private schedule wearing the daily's name: it
could never reach the daily board (that write is keyed on `run_id`) and nobody else played its waves
-- but it still cost the player a whole run, silently. `MsRunAnchor.cs` now aborts the run back to
inventory on every anchor failure when `mode == "daily"`: non-200, the 409 already-attempted case,
unparseable body, missing fields, schedule-version mismatch, unparseable seed. Normal runs are
untouched and stay fail-open on their local seed -- the fail-open contract in the class docstring now
says exactly which runs it covers.

Three details that make it safe. **Nothing is burned by an abort**: `/run/start` writes the attempt
reservation and the run row in one transaction, on a 200 only, so the player still holds today's
attempt. **The abort is generation-guarded** exactly like `TryApplyServerSeed` -- a late response from
an abandoned daily must never yank the run the player has since started. **The reason is carried, not
dropped**: `MsRunAnchor.ConsumeAbortNotice()` is read once by `MsModeSelectBootstrap` on the next
inventory load and rendered in the level caption in amber. That reuses the one text surface the scene
already owns rather than inventing a toast system in a scene that has none; clicking any level
selector calls `RefreshSelectorTints` and replaces the notice with the blurb, which is the dismissal.

The server half of F5 ("settlement requires `verdict = 'ok'`") is duel-specific and belongs to
Sprint 5. It is deliberately NOT here: for daily, refusing to record a score on a non-`ok` verdict
would be flipping enforcement, which the audit gates on the F4 numbers that are still unread.

**C6(a) -- keyboard gate.** MedaShooter has no touch input: movement and fire read
`SimpleInput.GetAxis` (keyboard/gamepad only) and `WebGLInputMobile.jslib` is a text-field helper. A
phone that mounted the iframe downloaded ~46.6 MB to reach a ship it could not move.
`MedaShooterPage.jsx` gains `useHasFinePointer()` and `MSKeyboardGate`.

The test is **"is there no fine pointer anywhere"** (`any-pointer: fine`), not "is this a touch
device": a touchscreen laptop, or a tablet with a trackpad or mouse attached, reports a fine pointer
and stays playable. It listens for changes, so pairing a mouse opens the game without a reload, and
it fails OPEN where `matchMedia` is missing.

Two placements, deliberately. The gate replaces the **SIGN IN & PLAY** button, so a phone visitor is
told before connecting a wallet -- making someone log in and press a play button only to learn the
game cannot be flown here is the worse half of the same problem. It stands in front of the **iframe**
again as the hard stop, which is what actually guarantees the build is never fetched.

Two additions beyond the audit row, both flagged rather than assumed. (1) An **override link** ("I
have a keyboard -- load it anyway"): a false negative in pointer detection would otherwise lock a
real player out entirely, while being wrong the other way costs only a download they chose to make.
(2) The row said "render keyboard required **+ the leaderboard**"; the per-game leaderboard tab is
hidden on purpose (S283 -- `/rankings` is the one board that pays), so the panel links to the **War
Rankings** instead of reviving a surface the founder retired.

### Verified -- automated

- **G4(a), schema and backfill: 10/10 SQL assertions green** against a throwaway `postgres:15-alpine`
  built from the REAL `migration_ms_run_anchoring.sql` + `migration_ms_daily_challenge.sql` (so the
  `unity_score_id` UUID retype is in play), seeded with five score rows covering every path. Verified
  correctness, not just absence of errors: the verdict-link backfill fills the three anchored rows;
  the daily-attempt backfill fills the row no verdict row could reach; **the legacy row is left NULL**
  (the contract); the PB table gets the same treatment; the board dedupes per wallet and ranks
  9500-before-9000 with the codename joined; the mode filter separates normal from daily; coverage
  reports 4 of 5; re-running the backfill changes nothing (idempotent); the partial index exists.
- **G4(a), bound parameters: 11/11 asyncpg checks green.** Run separately and on purpose -- literal
  SQL cannot catch the S156/S285 failure where a parameter with no typed use resolves to TEXT at
  prepare and asyncpg then refuses ints. Every query was executed through `asyncpg.connect` with the
  same argument shapes the route passes, including `mode=None` (the branch where `$2` appears only
  inside its own cast), an empty level, the self-rank lookup, and both write shapes with and without
  an anchor.
- **79 backend tests pass** (whole suite, includes the 45 MS ones; schedule parity intact).
- **C6(a): 11/11 headless checks green** across three Playwright profiles -- desktop, phone
  (iPhone UA + touch + no fine pointer), and phone-then-override. The assertion that matters is the
  network one: on a phone `/medashooter-frame.html` is **never requested**, not merely hidden. Also
  asserted: gate absent on desktop, play CTA withheld on the phone and restored after the override,
  the Rankings link present, zero console errors on every profile.
- **FE build clean** (56.7 s, 20 prerendered routes). Lint on the changed page: 14 errors, identical
  to the pre-existing baseline recorded in sprint 2 -- all in components this sprint did not touch.
- **C# brace/paren balance checked** on both edited files. That is the ceiling here: Unity is the only
  thing that can compile them.

### Checks that need a real device / a human

1. **Run the G4(a) migration on dev** (`backend/migration_ms_score_level_mode.sql`, no BEGIN block),
   then `GET /api/game/medashooter/scoreboard/by-level?level=1` and read the `coverage` block.
   `rows_with_level: 0` right after the migration is expected, not a failure -- it means no anchored
   run has submitted since the daily-challenge migration. Play one anchored run and it becomes 1.
2. **Then the same on prod**, whenever the MS hold lifts. The migration is additive and the backfill
   is idempotent, so it is safe to run before the code promote; the columns simply stay NULL until
   the new submit path is live.
3. **Build v18** (Unity 2021.3.45f2, never reuse a suffix) -- it now carries Sprint 2 AND F5.
   Sprint 2's playtest checklist still applies. New for F5, on the dev build:
   - Play the Daily normally: it should behave exactly as before (anchoring succeeds).
   - Play the Daily **twice in one UTC day**. The second attempt must bounce back to inventory with
     "Today's Daily Challenge has already been played. It resets at 00:00 UTC." in amber under the
     level buttons -- today it silently plays a run that counts for nothing.
   - Click any level button afterwards: the notice must be replaced by that level's blurb.
   - Play a **normal** run with the backend unreachable (throttle or block `/run/start` in devtools):
     it must still play through to the end, unanchored. If a normal run ever bounces to inventory,
     that is the bug -- fail-open for normal runs is the whole contract.
4. **The phone gate on a real phone** (the emulated profile is not the matrix): open `/meda-shooter`
   on iOS Safari and on Android Chrome. Expect the KEYBOARD REQUIRED panel where SIGN IN & PLAY
   normally sits, and no long download. Then on a **touchscreen laptop**, where the panel must NOT
   appear. Screenshot of the emulated phone view is in the sprint report.
5. **Still open from sprint 1, unchanged:** the F4 verdict numbers are unread, and Sprints 4-5 (duels)
   stay gated on them.

### Tooling gotchas

- The MedaShooter route is `/meda-shooter`, not `/medashooter`. A wrong path does not 404 here -- the
  SPA renders the shell with an empty body, so the smoke test failed with a confusing "content
  missing" rather than a clear 404.
- The play area is behind a **SIGN IN & PLAY** click, so a headless run without a wallet can never
  reach the iframe. The honest headless assertion is therefore "the frame URL was never requested",
  which is also the assertion that actually matters.
- Third time for the same one: a Python file with regex or mixed quoting does not survive a Bash
  heredoc here -- `re.PatternError: unterminated character set` on a pattern that is fine on disk.
  Write it to the scratchpad with the Write tool and run it.
- `postgres:15-alpine` reports `pg_isready` OK before it accepts connections during first-time
  initialisation; polling `psql -c "select 1"` instead is the reliable readiness check.
- `docker run` without `-p` cannot be reached by a host-side asyncpg client, and there is no way to
  add a port mapping to a running container -- decide up front whether the test needs a host
  connection or only `docker exec psql`.

### Not in this sprint

Sprints 4-5 (F2 duels, Risk H) remain UNTICKED and gated on the F4 numbers. The Later pool is
unchanged: G3, G4(b), G9, C3, C6(b), F6, H1. **C6(b)** -- real touch controls -- is now the obvious
follow-up to this sprint: the gate tells mobile visitors the truth, but the truth is still "not for
you". **G4(b)**, the per-level multiplier, is newly unblocked in the sense that the columns it needs
now exist; it stays unpicked because it reshuffles the cumulative board.

---

## Do not (cumulative)

- Do not settle a wager on a shadow verdict. As of S306 the verdict has never rejected a submission;
  `pending_validation` means nothing until enforcement is on for that mode.
- Do not market duels as an "identical run" / "same run" / "mirror match" -- GDD 3.5, F3. "Same
  conditions" or "same wave schedule" is the accurate phrasing.
- Do not cite the "determinism envelope" as anti-cheat. S206 refuted it; the anchored-run token plus
  the wall-clock check is what exists.
- Do not change a level's wave COUNT without updating all four mirrors in the same commit.
- Do not put a `MinibossBase` prefab in a campaign wave (zero or negative reward below wave 10, random
  spawn position), and do not set a boss wave's prefab to anything that is not a `BasicBoss`.
- Do not add a write to `ms_verdict_stats.py`. It is read-only by design and is run against prod.
- Do not `git merge dev` on frontend `main` -- it silently keeps the MS hold. Do not reuse a build suffix.
- Do not set a backdrop profile `OverrideDecals: 1` without filling BOTH Additions blocks with a real
  holder Prefab and a non-empty Sprites list -- `BackgroundResolver.CreateDecals` instantiates and
  indexes them unguarded.
- Do not trust a prefab NAME for its type. `FlailBoss_BombrunnerAdd` is an add, not a boss; grep the
  prefab for the component guid the consuming code requires.
- Do not treat a wave `Index` / `WaveDifficulty` as runtime data -- both are editor bookkeeping, and
  the only consumer of the stored values is `UnendingWavesProfile.CalculateIndices`, an editor button.
- Do not write the client's `level` / `mode` claim to a score row. `level_id` NULL means "not known";
  only a server-anchored run row fills it. G4(b) and any future per-level multiplier read the columns,
  never `checks.client_level`.
- Do not make a NORMAL run fail closed when `/run/start` errors. Only daily (and later duel) runs
  abort; a determinism feature must never be able to stop someone playing a solo run.
- Do not assume `medashooter_scores_all` column types from the repo -- it has no DDL here. Cast both
  sides of any join on `unity_score_id`.
