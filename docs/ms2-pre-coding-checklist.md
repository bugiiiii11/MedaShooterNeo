# MS 2.0 -- Phase 0 Pre-Coding Checklist

> Everything that must be TRUE before the first gameplay commit. Companion: `ms2-gdd.md`.
> Exit criterion for Phase 0: a fresh WebGL build from `dev` source runs on dev frontend, byte-different but behavior-identical to live v4, with all binary patches ported into source.

## A. Repo and branch hygiene

- [ ] `git -C MedaShooterNeo fetch` -- confirm `dev` and `main` up to date with origin; no direct commits on `main` since last merge.
- [ ] Confirm only intentional main/dev diffs exist: `RestfulManager.cs`, `InventoryBackend.cs` (backend URLs), `menu.unity` version label (`docs/dev-to-prod-merge.md`).
- [ ] Confirm `WebGLBuilds/` still gitignored; builds go to `fe/public/unity-builds/`, never committed to ms repo.
- [ ] Junk sweep: `rene-sdk-unity-1.0.0/`, `rene-sdk-unity-1.0.1/`, `unity-mcp-temp/` at repo root -- decide keep/remove BEFORE builds (dead SDKs bloat build + confuse asset resolution). Verify nothing references them first.

## B. Port the v4 binary patches into source

The live `medashooter.data.v4.gzip` carries UnityPy patches (S191/S193) that are NOT in the Unity source. Each must be found, reproduced in source, and verified in the parity build:

| Patch (from live v4) | What to port | Verify in source |
|----------------------|--------------|------------------|
| Marketplace dialog de-brand (S191, v3) | Replace/neutralize old-brand marketplace dialog in UI prefabs/scenes | grep scenes+prefabs for marketplace strings |
| Black ad panel killed (S193, v4) | Remove/disable the ad panel object | locate in `menu.unity` / prefabs |
| URL line (S193, v4 root cause) | Fix the URL text at source | `grep -r "cryptomeda" Assets/` -- also covers CTO's dead-metadata-URL ask |
| Any remaining `cryptomeda.tech` links | Point to swarmresistance.com equivalents | same grep; frame-level link remap in `medashooter-frame.html` stays as a safety net |

- [ ] Enumerate patches precisely: session notes S191/S193 (handoff-archive) + diff v4 data vs a fresh unpatched build if notes are ambiguous.
- [ ] After porting: `grep -r "cryptomeda" MedaShooterNeo/Assets/` returns only intentional hits (NFT metadata contract references, if any).

## C. Parity build (the core of Phase 0)

- [ ] Open project in Unity **2021.3.45f2 exactly** (installed). Do NOT let Unity upgrade the project. Expect `.spriteatlas` phantom diffs on first open (LF/CRLF, safe to discard).
- [ ] Confirm project compiles clean; note any missing-reference warnings in scenes (drift indicator).
- [ ] Check in-game version label (`menu.unity` ~line 5960 `m_text:`) vs live prod label -- tells us how far source is ahead of the last build.
- [ ] Build WebGL (dev branch -> dev backend URLs). Record: build time, output size per file, compression setting (gzip -- must match `.gzip` serving contract).
- [ ] **Rename contract:** name output data file `medashooter.data.v5.gzip`; update pointer in `frontend/public/medashooter-frame.html` + the matching `vercel.json` route. Framework/loader/wasm: verify whether they are also immutably cached -- if yes, version them too.
- [ ] Deploy to dev frontend, behind the existing iframe. Full smoke on dev:
  - [ ] Boots in iframe, 16:9 intact, no white-screen; loading banner keyed on `!isLoaded` still works.
  - [ ] Login/wallet flow -> NFT heroes + weapons load with correct stats.
  - [ ] Boost purchase (30 MG) works; `medaGasChanged` fires; TopBar updates.
  - [ ] Full run -> score submit hits DEV backend (`swarm-resistance-backend-dev`), row lands in `medashooter_unity_scores`, XP + gas awarded within caps, leaderboard updates.
  - [ ] Energy: 0-energy gate latches correctly (S187); energy spent before match mutation (S179).
  - [ ] Quit mid-run + relaunch -- no freeze (the S188 iframe teardown reason).
  - [ ] Mobile touch controls still work (joystick UI).
- [ ] Founder plays 2-3 full runs on dev: "feels identical to live" sign-off. THIS is the parity bar -- not byte comparison.
- [ ] Note for API tests from founder machine: Avast MITMs HTTPS -- curl needs `--ssl-no-revoke`; browser testing unaffected.

## D. Build automation

- [ ] Write batchmode build script (Phase 0 deliverable, lives in ms repo `Editor/`):
  `Unity.exe -batchmode -quit -projectPath ... -executeMethod BuildScript.BuildWebGL -logFile build.log`
- [ ] Script parameterizes: output dir, data-file version suffix, dev/prod define or URL check.
- [ ] Add a pre-build guard: fails the build if backend URL in `RestfulManager.cs`/`InventoryBackend.cs` does not match the requested target (prevents the classic dev-URL-on-prod mistake).
- [ ] One documented command produces a deployable build; hand-clicked Editor builds become the fallback, not the norm.

## E. Baseline metrics (record BEFORE changing gameplay)

- [ ] Current live behavior video/notes: wave pacing incl. a captured empty-wave occurrence (regression reference).
- [ ] FPS on founder machine + one mid/low device, current build vs parity build.
- [ ] Load time to interactive (dev, cold + warm cache).
- [ ] Backend: current score distribution snapshot (top 50, median) -- later phases must not silently reshuffle fairness perception.
- [ ] RSA keypair: confirm the public key embedded in source matches what the dev/prod backends decrypt (a parity-build score submit passing end-to-end proves it).

## F. Founder decisions owed (blocking marked)

| # | Decision | Blocks | Default if undecided |
|---|----------|--------|---------------------|
| 1 | Mobile a first-class target for perf gates? | Phase 1 | Yes, reduced-VFX preset |
| 2 | Junk SDK folders: delete? | Phase 0 A | Delete after reference check |
| 3 | Veterans: L1 reset vs cumulative head start | Phase 4 | L1 reset (recommended in GDD 3.4) |
| 4 | Endless: gated behind campaign or always open | Phase 3 | Always open |
| 5 | Duel rake % / wager bounds / daily duel cap | Phase 5 | 10% / 10-500 gas / 10 duels/day |
| 6 | Raise MS XP cap after envelope validation? | Phase 2+ | Hold 500 until validation runs in shadow mode clean for 2 weeks |
| 7 | Pilot Level naming (ties into parked "Commander XP" rename) | Phase 4 cosmetics only | "Pilot Level" |

## G. Coordination

- [ ] Colleague/CTO sync via swarm-meta: MS 2.0 track starting, ms repo active again (was archived), phases + who owns season/distribution impact (none until Phase 4-5).
- [ ] CTO ask piggyback: the `cryptomeda.tech` dead-URL cleanup (section B) overlaps CTO's open nft_service.py ask -- coordinate so both sides change together.
- [ ] No prod DB migrations in Phase 0-1. First migration lands Phase 2 (envelope columns); follows the S133 rule: verify every object on prod via endpoint after running.

## H. Definition of done -- Phase 0

1. Fresh `dev` source build (v5) live on dev frontend, founder-verified "identical feel".
2. All v4 binary patches ported to source; UnityPy patch pipeline retired.
3. Batchmode build script committed + documented.
4. Baseline metrics recorded in this doc (append section I).
5. Founder decisions 1-2 made; 3-7 logged with defaults.
6. `dev-to-prod-merge.md` updated: new build command, v5+ rename contract, retire note for binary patching.
