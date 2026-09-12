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
