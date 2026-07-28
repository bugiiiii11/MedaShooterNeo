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
- [ ] Check in-game version label (`menu.unity` ~line 5960 `m_text:`) vs live prod label -- tells us how far source is ahead of the last build.
- [x] **Build WebGL (S200), dev backend, guard passed.** Build time **1111.5s (~18.5 min)** on the founder machine, cold `Library` rebuild before it (~15 min more). Compression `webGLCompressionFormat: 1` (Gzip) confirmed in `ProjectSettings.asset:751`; all three compressed outputs verified to carry gzip magic `1f8b`, loader verified plain text.
- [x] **Rename contract -- ANSWERED, and the old one had a latent bug.** `vercel.json` serves `medashooter.wasm.gzip` with `max-age=31536000` but **never versioned it**. Only the data file carried a version. A rebuild changing C# changes the wasm, so a returning player would have got new `data.v5` against a year-old cached wasm -- a mismatched build. **All four outputs now carry the suffix**: `medashooter.{data,wasm}.v5.gzip`, `medashooter.framework.v5.js.gzip`, `medashooter.loader.v5.js`. `BuildScript.ApplyVersionSuffix` does the renaming so it cannot be forgotten. Frame + `vercel.json` must be updated together with it.
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
- [ ] FPS on founder machine + one mid/low device, current build vs parity build.
- [ ] Load time to interactive (dev, cold + warm cache).
- [ ] Backend: current score distribution snapshot (top 50, median) -- later phases must not silently reshuffle fairness perception.
- [ ] RSA keypair: confirm the public key embedded in source matches what the dev/prod backends decrypt (a parity-build score submit passing end-to-end proves it).

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

- [ ] Colleague/CTO sync via swarm-meta: MS 2.0 track starting, ms repo active again (was archived), phases + who owns season/distribution impact (none until Phase 4-5).
- [ ] CTO ask piggyback: the `cryptomeda.tech` dead-URL cleanup (section B) overlaps CTO's open nft_service.py ask -- coordinate so both sides change together.
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

### Still owed

1. **Founder parity smoke (Section C) -- the Phase 0 pass bar.** Nothing below is claimed as
   verified: no one has played the v5 build. It is committed locally but NOT pushed, so dev has not
   redeployed yet.
2. In-game version label vs live prod label (Section C) -- still unchecked.
3. Sections E (baseline metrics: FPS, load-to-interactive, score distribution) and G (colleague/CTO
   sync). Build time + output sizes above are the first two E rows.
4. **Dead code now pointing at deleted files:** `MedaShooterPagePROD.jsx:124` and
   `MedaShooterResistancePage.jsx:28` reference the old unversioned filenames. Neither is routed in
   `App.jsx` (only `MedaShooterPage` is), so nothing breaks -- but they are actively misleading.
5. `frontend/public/medashooter-frame.html` references `streamingAssetsUrl:
   '/unity-builds/medashooter/StreamingAssets'` and **no such folder exists** -- and did not before
   v5 either. Pre-existing dead config, not a regression.

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
- **The confirm dialog's wording changes.** Live v4 says "You will be redirected to OpenSea"
  (a same-length IL2CPP patch). Source says "You will be redirected to your browser" and now shows
  the real destination on the URL line, which live v4 blanked. Links go to two different hosts now,
  so the generic wording is the more accurate one -- but it is a visible difference from what was
  signed off. Flag it during the parity playthrough.

## H. Definition of done -- Phase 0

1. Fresh `dev` source build (v5) live on dev frontend, founder-verified "identical feel".
2. All v4 binary patches ported to source; UnityPy patch pipeline retired.
3. Batchmode build script committed + documented.
4. Baseline metrics recorded in this doc (append section I).
5. Founder decisions 1-2 made; 3-7 logged with defaults.
6. `dev-to-prod-merge.md` updated: new build command, v5+ rename contract, retire note for binary patching.
