# MS 2.0 -- Phase 0 Pre-Coding Checklist

> Everything that must be TRUE before the first gameplay commit. Companion: `ms2-gdd.md`.
> Exit criterion for Phase 0: a fresh WebGL build from `dev` source runs on dev frontend, byte-different but behavior-identical to live v4, with all binary patches ported into source.

## A. Repo and branch hygiene -- DONE (S199)

- [x] `git -C MedaShooterNeo fetch` -- `dev` and `main` both level with origin; `main` has no commits missing from `dev`.
- [x] Only intentional main/dev diffs: `RestfulManager.cs`, `InventoryBackend.cs`, `menu.unity` (+ the two S198 docs).
- [x] `WebGLBuilds/` gitignored (`.gitignore:78`); builds go to `frontend/public/unity-builds/medashooter/`.
- [x] Junk sweep -- the premise was wrong, see below.

**The "junk SDKs" were not junk, and one of them was the ad bug.**

| Folder | Verdict |
|--------|---------|
| `rene-sdk-unity-1.0.1/` | Was an **active UPM package** (`com.reneverse.services` in `Packages/manifest.json`), not a stray folder. Removed together with its manifest + lockfile entries per founder decision F2. |
| `rene-sdk-unity-1.0.0/` | Unreferenced duplicate, 3.7 MB. Deleted. |
| `unity-mcp-temp/` | **Orphan gitlink** (mode 160000, no `.gitmodules`, empty on disk). Its hash equals the `com.coplaydev.unity-mcp` git package commit -- a leftover clone. Removed from the index. |
| `com.coplaydev.unity-mcp` | KEPT (founder editor tooling, not shipped). It resolves from a GitHub URL at project open -- if that ever hangs a build, this is the first suspect. |

## B. Port the v4 binary patches into source -- DONE (code S199, scene surgery S200)

Patches enumerated exactly from the S191/S193 archive notes plus the frame's own comment block
(`frontend/public/medashooter-frame.html:114-121`). No guesswork remained -- the count reconciled.

| # | Patch in live v4 | Ported to source as |
|---|------------------|---------------------|
| 1 | Dialog sentence + open-URL IL2CPP literals (S191) | Real URLs now live in `OpenLinkButton.MarketplaceUrl` / `.MedaShooterUrl` consts; the 5 scattered literals reference them |
| 2 | Two static prefab TMP URL lines blanked (S191) | `m_text` emptied in `inventory.unity:8335`, `develop_overhaul.unity:7314` (DialogBox overwrites it at runtime anyway) |
| 3 | Seven serialized `OpenLinkButton.Link` blanked (S193) | All 7 retargeted to the live OpenSea collection |
| 4 | Three ReneVerse Video Ad Surface GameObjects deactivated (S193) | 2 of 3 gone with the SDK; **the third (the `develop_overhaul.unity` instance) removed S200** via `Ms2Cleanup` |

S193 recorded the third patch as "level3 + 2 resources.assets prefabs" without identifying the
last two. They were the SDK's own `Resources/BuiltIn/Video Ad Surface.prefab` and
`Resources/URP/Video Ad Surface.prefab` -- anything under a `Resources/` folder is force-included
in the build, which is why they reached `resources.assets` despite nothing referencing them.
Deleting the package removed both at the root. Only the `develop_overhaul.unity` instance remains.

**The seven Link fields reconciled exactly** against the four build scenes
(`loading`, `menu`, `inventory`, `develop_overhaul` per `EditorBuildSettings.asset`):
`AddIcon.prefab`, `PreviewAbility.prefab`, `inventory.unity` x4, `develop_overhaul.unity` x1.
`dummy_tests.unity` and `inventory_old_save.unity` also carry dead links but are **not in the
build** and were deliberately left alone.

- [x] Enumerate patches precisely.
- [x] Port the code-side patches.
- [x] Remove the ad GameObject from `develop_overhaul.unity` (S200). `grep -c "Video Ad"` on that scene = 0.

### `grep -r cryptomeda Assets/` -- which hits are intentional

After the port, every remaining hit is deliberate. Do NOT "clean" these:

- **`Cryptomeda.*` C# namespaces** (`Cryptomeda.Minigames.BackendComs`, `Cryptomeda.NFT.Json`). Scenes serialize these by name in `m_TargetAssemblyTypeName` -- renaming silently breaks scene wiring.
- **`Assets/Prefabs/RestfulAPIManager.prefab` `@ENV.cryptomeda.tech` URLs.** Legacy fallbacks; `RestfulManager.cs:63` overrides them with the Railway base at runtime. S193 flagged these explicitly as untouchable.
- **Sprite/skin identifiers** (`Cryptomeda_Body`, `SkinName.Cryptomeda`) -- internal asset names, never displayed.
- **Non-build scenes** `dummy_tests.unity`, `inventory_old_save.unity`.

## C. Parity build (the core of Phase 0)

- [x] Open project in Unity **2021.3.45f2 exactly** (S200, batchmode -- never opened in the GUI, so no upgrade prompt and no phantom `.spriteatlas` diffs materialized).
- [x] **Compiles clean (S200).** Zero errors. Only 4 benign `CS0219`/`CS0414` unused-variable warnings (`BackgroundResolver.cs` x3, `UIGameOverScreen.cs` x1). No missing-reference warnings in any of the 4 build scenes -- the predicted drift did not exist once `VideoAdUi.cs` was gone.
- [x] **Version label checked (S201): source is NOT ahead.** `menu.unity:5960` is `v1.2.6 [DEV]` on `dev` and `v1.2.6` on `main` -- the same base version the last prod build shipped, so v5 introduced no label drift. (A second label at `menu.unity:5093`, `v0.9.5b`, is a different unrelated element -- left alone.) Worth bumping to `v1.3.0` when Phase 1 gameplay changes land, so a screenshot tells you which engine a player is on.
- [x] **Build WebGL (S200), dev backend, guard passed.** Build time **1111.5s (~18.5 min)** on the founder machine, cold `Library` rebuild before it (~15 min more). Compression `webGLCompressionFormat: 1` (Gzip) confirmed in `ProjectSettings.asset:751`; all three compressed outputs verified to carry gzip magic `1f8b`, loader verified plain text.
- [x] **Rename contract -- ANSWERED, and the old one had a latent bug.** `vercel.json` serves `medashooter.wasm.gzip` with `max-age=31536000` but **never versioned it**. Only the data file carried a version. A rebuild changing C# changes the wasm, so a returning player would have got new `data.v5` against a year-old cached wasm -- a mismatched build. **All four outputs now carry the suffix**: `medashooter.{data,wasm}.v5.gzip`, `medashooter.framework.v5.js.gzip`, `medashooter.loader.v5.js`. `BuildScript.ApplyVersionSuffix` does the renaming so it cannot be forgotten. Frame + `vercel.json` must be updated together with it.
- [x] **Deployed to dev frontend and SMOKE PASSED (S201, founder): "the game works fine, no black panel."**
  - [x] Boots in iframe, no white-screen -- so the blind `Content-Encoding: gzip` header on the new
        gzipped framework file was correct. That was the single riskiest unverified change in v5.
  - [x] **No black panel over the Esc/give-up confirm** -- the removed ReneVerse `Video Ad Surface`
        instance was indeed the last one. Section B patch #4 is closed for good.
  - [x] **Score submit hits the DEV backend, verified server-side, not just by eye (S201).** The
        public scoreboard shows a dev row at `2026-07-28T00:46:27Z` (wallet `0x4ba944fb..6e6e`,
        score 513) landed ~14 min after the founder started testing, with **`nft_boosts` populated**
        -- which also proves the wallet -> NFT hero/weapon path loaded. **No row for that wallet on
        prod**, so the `-msEnv` backend-URL guard told the truth.
  - [ ] Boost purchase (30 MG) -> `medaGasChanged` -> TopBar updates: not individually confirmed.
  - [ ] Energy 0-gate latch (S187) / spend-before-mutation (S179): not individually confirmed.
  - [ ] Quit mid-run + relaunch, no freeze (S188 iframe teardown): not individually confirmed.
  - [ ] Mobile touch controls (joystick UI): not confirmed -- Phase 1 makes mobile a perf gate anyway.
- [x] **Founder parity sign-off GIVEN (S201).** The unconfirmed rows above are pre-existing platform
      behavior that v5 did not touch, not parity risks; none of them block Phase 1.
- [ ] Note for API tests from founder machine: Avast MITMs HTTPS -- curl needs `--ssl-no-revoke`; browser testing unaffected.

## D. Build automation -- WRITTEN (S199), not yet executed

`Assets/Editor/BuildScript.cs` already existed with Windows/WebGL entry points; `BuildWebGLDeploy`
was added rather than replacing it. The pre-existing CLI entry `BuildWebGL()` defaults to a
**Development** build -- do not use it for deploys.

- [x] Batchmode entry point, parameterized on output dir, version suffix and target env.
- [x] Pre-build backend-URL guard: refuses to build when `RestfulManager.cs` / `InventoryBackend.cs`
      point at the wrong Railway host for the requested `-msEnv`. Neither host is a substring of the
      other, so a wrong-host hit is decisive.
- [x] Post-build version rename for all four outputs (see Section C).
- [x] Clears `Build/` first, so a stale file from a previous version cannot masquerade as deployed.
- [x] **Actually run it (S200) -- worked first try, exit 0.**

**One gap the script does NOT cover, found S200.** Unity also emits its stock template page
(`index.html` + `TemplateData/`) into `-msOut`. Neither was in the repo before, nothing references
them, and the emitted `index.html` points at the *unversioned* `medashooter.loader.js` -- a dead
page shipped into `public/`. Deleted by hand after the build. **Delete them after every build**, or
teach `BuildWebGLDeploy` to remove them.

```
"C:/Program Files/Unity/Hub/Editor/2021.3.45f2/Editor/Unity.exe" \
  -batchmode -quit -nographics \
  -projectPath "<repo>/MedaShooterNeo" \
  -executeMethod BuildScript.BuildWebGLDeploy \
  -msEnv dev -msVersion v5 \
  -msOut "<repo>/frontend/public/unity-builds/medashooter" \
  -logFile build.log
```

Both new entry points are also on the Editor `Build/` menu, so a licensed GUI session can run them
without the CLI: **Build > Phase 0 - Remove ReneVerse Ad Objects**, then **Build > WebGL Release**.

## E. Baseline metrics (record BEFORE changing gameplay)

- [ ] Current live behavior video/notes: wave pacing incl. a captured empty-wave occurrence (regression reference).
- [ ] FPS on founder machine + one mid/low device, current build vs parity build. **Founder-owned -- needs a play session with a frame counter; nothing else in Phase 1 is blocked on it.**
- [ ] Load time to interactive (dev, cold + warm cache). Build sizes are already recorded in Section I (data 38.91 -> 36.47 MB, wasm 8.82 -> 7.93 MB), so v5 should be *faster* than v4, not slower.
- [x] **Score distribution snapshot taken S201, 2026-07-28 ~01:00 UTC** (`/api/game/medashooter/scoreboard?limit=50`, current season, one row per wallet):

| | PROD | DEV |
|---|---|---|
| Players on the board | 12 | 3 |
| Top 5 | 54923, 11641, 2005, 1999, 1985 | 1309, 513, 0 |
| Median | 1973 | 513 |
| Min / max | 20 / 54923 | 0 / 1309 |

  **Read this before Phase 2 tuning.** Eight of the twelve prod scores sit in a tight 1841-2005
  band -- that is the natural ceiling of current wave pacing, and it is the number Phase 1 will move.
  The two outliers above it (54923 = 27x the median, 11641 = 6x) are exactly what MEMORY's "MS 500
  XP cap has no replay validation, watch leaderboard outliers" warns about; they are the case for
  Phase 2 envelope validation, and they must NOT be used as the balance reference.
- [x] **RSA keypair confirmed working end-to-end (S201)** -- the v5 dev submit above decrypted and
      validated server-side, which is the proof this row asked for. The embedded public key survived
      the source rebuild.

## F. Founder decisions owed (blocking marked)

| # | Decision | Blocks | Default if undecided |
|---|----------|--------|---------------------|
| 1 | Mobile a first-class target for perf gates? | Phase 1 | **DECIDED S199: YES, reduced-VFX preset + its own FPS gate** |
| 2 | Junk SDK folders: delete? | Phase 0 A | **DECIDED S199: ReneVerse ripped out entirely** (package, both folders, `VideoAdUi.cs`, ad prefab, `UIEscMenu.ServingAd`). Also decided: the dead `cryptomeda.tech/staking` button is **removed**, not retargeted -- Shield is land-gated now, so there was no destination to send anyone to |
| 3 | Veterans: L1 reset vs cumulative head start | Phase 4 | L1 reset (recommended in GDD 3.4) |
| 4 | Endless: gated behind campaign or always open | Phase 3 | Always open |
| 5 | Duel rake % / wager bounds / daily duel cap | Phase 5 | 10% / 10-500 gas / 10 duels/day |
| 6 | Raise MS XP cap after envelope validation? | Phase 2+ | Hold 500 until validation runs in shadow mode clean for 2 weeks |
| 7 | Pilot Level naming (ties into parked "Commander XP" rename) | Phase 4 cosmetics only | "Pilot Level" |

## G. Coordination

- [x] **Colleague/CTO sync written S201: `swarm-meta/ms2-track-note.md`** -- ms repo active again, patch pipeline retired, the two build gotchas (licence sign-in, framework gzip header), dev-on-v5 vs prod-on-v4, and the impact table (no season/distribution impact until Phase 4-5, no migrations until Phase 2). **Needs a push to reach them.**
- [x] CTO ask piggyback included in that note: the `cryptomeda.tech` dead-URL cleanup (Section B) overlaps CTO's open `nft_service.py` ask -- flagged so both sides change together.
- [ ] No prod DB migrations in Phase 0-1. First migration lands Phase 2 (envelope columns); follows the S133 rule: verify every object on prod via endpoint after running.

## I. Execution log (S199 code, S200 build)

### The S199 licence blocker -- RESOLVED S200

S199 could not build: batchmode died at `Found 0 entitlement groups` / `No valid Unity Editor
license found`. Root cause was simply a stale token -- **the founder signing into Unity Hub
interactively fixed it**, exactly as S199 predicted. Headless Hub launches never will.

Verify before blaming anything else (instant, no project load):

```
"C:\Program Files\Unity\Hub\Editor\2021.3.45f2\Editor\Data\Resources\Licensing\Client\Unity.Licensing.Client.exe" --showEntitlements
```

Wants `Product Name: Unity Personal` with `com.unity.editor.headless` listed. The entitlement file
is at `%LOCALAPPDATA%\Unity\licenses\UnityEntitlementLicense.xml` (**not** `C:\ProgramData\Unity\`,
which does not exist on this machine) and carries an `UpdateDate` roughly a month out -- so expect
to re-sign-in periodically. Two log lines are benign noise even on a healthy run: a licensing-client
signature `Code 10` warning and `Access token is unavailable; failed to update`.

### S200: Phase 0 build DONE

Both Section D commands ran in batchmode, first try, exit 0. No GUI session was needed.

- **Scene surgery:** `Ms2Cleanup` removed exactly 1 object across the 4 build scenes --
  `UI/PlayerControls/EscMenu/LeftPart (1)/Video Ad Surface(Clone)`. Script + `.meta` deleted after.
- **The diff was far smaller than S199 predicted:** 516 deletions / 1 insertion, not a whole-file
  re-serialize. Surgical and correct -- prefab-instance block, the RawImage/VideoPlayer/AudioSource
  objects, the parent `m_Children` entry, and the dangling `ServingAd` field on `UIEscMenu`.
  The single insertion is a float wobble: a RectTransform `m_AnchoredPosition.y` `-8` ->
  `-7.9999695`. Invisible; left alone rather than fought.
- **Build:** 1111.5s, guard passed for `env=dev`, all four outputs suffixed `v5`.

| File | v4 (live) | v5 (source) |
|------|-----------|-------------|
| `data` | 38.91 MB | **36.47 MB** |
| `wasm` | 8.82 MB | **7.93 MB** |
| `framework` | 431 KB (served *uncompressed*) | **88 KB** (gzipped) |
| `loader` | 19 KB | 19 KB |

**The framework file changed transport, not just name.** Live v4 served
`medashooter.framework.js` as plain JS; v5 is `medashooter.framework.v5.js.gzip`, so its
`vercel.json` route needed a `Content-Encoding: gzip` header it never had. Renaming the paths alone
would have fed gzip bytes to the browser as JavaScript and white-screened dev. Header added.

### S201: Phase 0 CLOSED

Founder played v5 on dev: **"the game works fine, no black panel."** With the version label and the
score-distribution baseline also settled (Sections C and E above), every Phase 0 exit condition in
Section H is met except the two founder-owned FPS/load-time measurements, which gate nothing.

The one code change S201 made is the dialog wording revert (Section "Two things to watch"). It is
**source-only and unbuilt** -- deliberately not worth a 20-minute rebuild on its own, so it rides
along with the first Phase 1 build.

### Still owed

1. FPS on founder machine + one mid/low device, and cold/warm load-to-interactive (Section E).
   Founder-owned, blocks nothing.
2. Section G colleague/CTO sync -- **written S201** to `swarm-meta/ms2-track-note.md`. Committed
   locally; **not pushed**, so the colleague lane cannot see it yet.
3. ~~Dead frontend code pointing at deleted build files~~ **DONE S201:** `MedaShooterPagePROD.jsx`
   and `MedaShooterResistancePage.jsx` were unreferenced anywhere in the tree (only
   `MedaShooterPage.jsx` is routed) and both still named the pre-v5 unversioned filenames. Deleted
   outright, 1565 lines; git has them if ever needed.
4. `frontend/public/medashooter-frame.html` references `streamingAssetsUrl:
   '/unity-builds/medashooter/StreamingAssets'` and **no such folder exists** -- and did not before
   v5 either. Pre-existing dead config, not a regression; left alone rather than touch the live boot
   path for zero gain.
5. ~~The next build is a prod promote~~ **SUPERSEDED S202: the next build is Phase 1 (v7) on dev.**
   The prod promote still needs TWO builds when it comes (`dev-to-prod-merge.md`): the dev build
   that exists, then a separate `-msEnv prod` build from `main` after the URL swap. Budget ~20 min
   each, and bump `-msVersion` every time -- v5 and v6 are both spent.

## J. Phase 1 (S202) -- what shipped, and what it means for the build

**Status: live on dev as `v7` (label `v1.3.0 [DEV]`). Founder played it the same session and
reported it "working nicely."**

That clears the obvious regressions -- the build boots, the juice pass does not break anything
visible, and nothing about the wave rewrite made the game worse to play. It does **not** yet close
Phase 1's definition of done, because the two things most worth proving both need a long run and a
short one cannot show either:

1. **The empty-wave fix needs depth.** The S201 half only manifests after a miniboss sweeps the
   field (every 5 waves past wave 10), and the S202 half was a 1-in-7 draw per wave transition in
   endless. A short run can pass while both bugs are still there. Watch for the opposite failure
   too: over-spawning would mean the counter now under-counts.
2. **The blacklist crossover sits deep.** See the founder decision in `ms2-gdd.md` 3.1 -- the
   estimate is around wave 84. Checking `medashooter_blacklist` after a wave-85+ run is the only
   way to find out whether the estimate is right before real players do.

Phase 0 is closed; this section only records the build-facing consequences. Design detail lives in
`ms2-gdd.md` 2.2.2, 3.1 and 3.2.

- **Nineteen files changed, five new scripts, one asset edited. Zero scene edits, zero prefab
  edits.** That was a hard constraint, not an accident: `develop_overhaul.unity` is 62k lines of
  YAML and every hand-edit to it is merge-hostile. Everything that would normally want an
  Inspector reference goes through `Resources.Load` (an established pattern here) or through
  components attached at runtime (the pattern `DamageReceiver.ActivateDot` already used).
- **The FORGE3D effects cost nothing to add.** `Assets/FORGE3D/2D Sci-Fi Platformer/Resources/` is
  a Resources root, so its 38 effect prefabs were already inside every WebGL build we have ever
  shipped, referenced by nothing that runs. Kill bursts and muzzle flashes just start using them.
  Expect no meaningful `medashooter.data` size change from the juice pass.
- **Two pre-existing leaks were closed while in the area**, both of which grow over a run and
  therefore hit long sessions hardest: projectiles that missed were never destroyed (unbounded
  Rigidbody2D + looping particles + a per-frame trail-material recolour, forever), and every
  pooled explosion allocated a coroutine plus a `WaitForSeconds` and waited in *scaled* time, so
  anything spawned near a pause was stranded in the pool permanently.
- **Anti-cheat coupling, deliberate and worth re-reading before touching hit-stop.** Hit-stop
  slows `Time.timeScale`, and `RealtimeDurationChecker` measures the run in scaled `Time.time` and
  POSTs that next to a server-measured duration. Stolen time is now accumulated in
  `JuiceRuntime.StolenSeconds` and added back at game over, mirroring what `additiveDuration`
  already did for pauses. If anyone adds a second timeScale consumer, it must do the same or long
  runs will start looking like speed hacks to the backend.
- **Still owed:** the in-game toggle UI for the juice settings. It is the only part of the perf
  gate that needs a scene edit (rows in the EscMenu settings panel), so it was left out.

### Durable WebGL gotchas learned in the S202 review

Recorded because all three would have shipped, none would have thrown, and none would have shown
up in the Editor.

- **`SystemInfo.systemMemorySize` on WebGL is the WASM HEAP, not device RAM.** Unity implements it
  as `JS_SystemInfo_GetMemory: return HEAPU8.length/(1024*1024)`
  (`<editor>/PlaybackEngines/WebGLSupport/BuildTools/lib/SystemInfo.js`). This project ships
  `webGLMemorySize: 482`, and a wasm32 heap can never reach 4096 MB, so any `memorySize < 4096`
  device check is true on **100% of clients**. `SystemInfo.processorCount` is equally untrustworthy
  with `webGLThreadsSupport: 0`. **`Application.isMobilePlatform` IS implemented properly** on
  WebGL (a user-agent check) and is the only one of the three worth branching on. The first draft
  of `JuiceSettings.DetectQuality` used the memory probe and silently disabled three of the five
  juice features on every shipped client while looking perfect in the Editor, which reports real
  RAM.
- **`GameManager` is NOT gameplay-exclusive.** `inventory.unity` carries a second active instance
  on its "Settings" object. Anything hung off `GameManager.Start()` runs there too. Gate on
  something that only exists in `develop_overhaul.unity` -- `EnemySpawner != null` is the cheap
  test, since `GameManager.Awake` resolves it with `FindObjectOfType`.
- **A `DontDestroyOnLoad` component bootstrapped at `AfterSceneLoad` anchors its timers to APP
  start, not scene start.** A frame-time watchdog written that way measures the synchronous load
  of the gameplay scene as if it were a gameplay frame. Subscribe to `SceneManager.sceneLoaded`
  and re-arm, and discard any sample window containing a frame longer than ~0.25s -- that is a
  load or a backgrounded tab (`runInBackground` is 0), not a frame-budget miss.

### Done and committed

- Section A junk sweep, with corrected findings.
- ReneVerse SDK removed at source: manifest + lockfile entries, `rene-sdk-unity-1.0.0/`,
  `rene-sdk-unity-1.0.1/`, `Assets/VideoAdUi.cs`, `Assets/Prefabs/Ads/`, and the `ServingAd`
  field plus its `SetVideoActive` coroutine in `UIEscMenu.cs`.
- All code-side and serialized link de-branding (Section B table).
- `BuildScript.BuildWebGLDeploy` + backend-URL guard + version-suffix rename.
- `Assets/Editor/Ms2Cleanup.cs` -- one-shot scene surgery. **Executed S200, then deleted.**
- **S200:** scene surgery run, v5 build produced, frame + `vercel.json` moved to v5 names.

### Two things to watch on the first build

- ~~**`VideoAdUi.cs` had an unguarded `using UnityEditor;`**~~ **RESOLVED S200 -- and it was the
  only drift.** Removing that file was sufficient; the first real compile produced zero errors and
  no missing-reference warnings. The worry that source "had not produced a player build in some
  time" turned out to cost nothing.
- ~~**The confirm dialog's wording changes.**~~ **DECIDED S201 (founder): keep the live prod
  wording, "You will be redirected to OpenSea."** Because links now go to two hosts, the sentence is
  no longer a hardcoded literal -- `OpenLinkButton.RedirectMessageFor(url)` derives it from the
  destination (`MarketplaceUrl` -> "OpenSea", `MedaShooterUrl` -> "Swarm Resistance", anything else
  -> the generic "your browser"). Both call sites use it (`OpenLinkButton.OpenLink`,
  `UICardPreview.Buy`). The URL line **stays visible** -- v4 only blanked it because the URL it
  showed was the dead `cryptomeda.tech` one; the dialog was designed with a `UrlText` field.
  **Ships in the next build -- v5 on dev still shows the old sentence.**

## H. Definition of done -- Phase 0

1. Fresh `dev` source build (v5) live on dev frontend, founder-verified "identical feel".
2. All v4 binary patches ported to source; UnityPy patch pipeline retired.
3. Batchmode build script committed + documented.
4. Baseline metrics recorded in this doc (append section I).
5. Founder decisions 1-2 made; 3-7 logged with defaults.
6. `dev-to-prod-merge.md` updated: new build command, v5+ rename contract, retire note for binary patching.

## K. Phase 1 tuning (S203) -- first playtest feedback, build v8

Founder played the v7 dev build to **8032 points** (dev scoreboard rank 1, 2026-07-28 04:17 UTC,
loadout with `score_multiplier: 20` and `fire_rate_bonus: 30`). Verdict: plays well, **no empty
waves seen**, weapon effects good -- with one complaint and it was a real defect.

- **The pistol's muzzle halo strobed.** Only the glow child, not the flash sprite. FORGE3D authored
  `MuzzleFlashGlow_Pistol` at **3-6 world units** against a ~13-unit tall camera, on a renderer
  whose `m_MaxParticleSize: 1` lets one particle cover the full viewport height, with a 0.05-0.08s
  lifetime. The pistol is the starting weapon, auto-fires, and its 0.65s cooldown is driven toward
  0.1s by per-wave upgrades and NFT fire-rate bonuses -- so the halo repeats faster the longer the
  run lasts, which is exactly how the founder described it ("distracting after a while"). Now
  scaled to 0.4x size / 0.45x alpha, **pistol only**: the 6-7 unit machinegun and 5-7 unit shotgun
  halos belong to weapons slow enough for a heavy flash to read as impact.
  Tuning lives in `JuiceSettings.PistolGlowSizeScale` / `PistolGlowAlphaScale` -- two numbers.
- **Applied per instance, never on the prefab.** `Resources.Load` returns the shared asset; mutating
  it would dirty the real file in the Editor. `Weapon.DampenGlow` matches children by name (only the
  ROOT gets renamed "(Clone)"), handles every `MinMaxCurve`/`MinMaxGradient` mode, and uses the
  list overload of `GetComponentsInChildren` so the fire path still allocates nothing. Safe right
  after `Instantiate` because `playOnAwake` starts the system but emission waits for the particle
  simulation step, which runs after `Update`.
- **`KillBurstSeconds` 1.1s -> 1.5s.** The S202 note said 1.1s clipped `Enemy_Explode`'s "1.81s
  tail"; the 1.81s was the system's `lengthInSec` and was never the visible duration. Every system
  in `Enemy_Explode`, `Pistol_Hit_01`, `AssaultLaser_Hit` and `Sniper_Barrel_Smoke_01` has
  `rateOverTime` 0 with a single burst at t=0, so what matters is the longest particle LIFETIME:
  1.41s, then 1.16s, then 0.4-1.0s. 1.1s released the two biggest mid-fade (particles vanished
  instead of fading). 1.5s clears all of them.
- **HUD wave number is now the run counter.** `UINumbersHandler` displayed `NextWave.Index + 1` --
  the wave ASSET's slot in the profile. In the campaign that coincides with the wave number; in
  endless, waves are drawn at random, so the counter jittered between 1 and 6 forever and a run at
  wave 40 could read "2". New `RunWaveChangedEvent` carries `EnemySpawner.waveNumber`, which is the
  same number the miniboss ladder, difficulty curve and spawn-gap floor already use. Deliberately
  NOT reusing `NextWaveEvent`: that one drives perk rolls, stat upgrades and powerup spawns, and it
  is not dispatched at all on a miniboss wave -- precisely when the HUD needs to move.

**Blacklist watch (open item 2) -- one data point, not an all-clear.** The 8032 run submitted and
ranked, so `game_duration * 100 < calculated_score` did not trip: it would have needed the run to
last under 80s. The ratio only becomes dangerous where the quadratic miniboss ladder outruns the
clock, around w84 with the Phase 1 density floor. Still needs a deep run to actually exercise.

Build v8: batchmode, exit 0, 537.5s, zero compile errors. Stock `index.html` + `TemplateData/`
deleted per Section I. Frame + `vercel.json` moved to v8 (JSON re-parsed after editing, per the
S202 sed hazard). Label `v1.3.0 [DEV]` -> `v1.3.1 [DEV]`.
