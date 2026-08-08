# MedaShooter: Dev to Prod Merge Guide

> Step-by-step process for promoting MS changes from `dev` to `main` (production).

## Key Facts

| Item | Value |
|------|-------|
| Repo | `ms/` (MedaShooterNeo) |
| Branches | `main` = production, `dev` = development |
| WebGL builds | NOT stored in ms repo -- go to `fe/public/unity-builds/` |
| Version label | `menu.unity` line ~5960, `m_text:` field |

## Files That Differ Between Branches

These are the **only** intentional differences between `main` and `dev`:

| File | main (prod) | dev |
|------|-------------|-----|
| `Assets/RestfulManager.cs` | `swarm-resistance-backend-production` | `swarm-resistance-backend-dev-production` |
| `Assets/InventoryBackend.cs` | `swarm-resistance-backend-production` | `swarm-resistance-backend-dev-production` |
| `Assets/Scenes/menu.unity` | `vX.Y.Z` | `vX.Y.Z [DEV]` |

Everything else should be identical after a merge.

## Merge Steps

### 1. Pull latest dev

```bash
git -C ms checkout dev
git -C ms pull origin dev
```

### 2. Merge dev into main

```bash
git -C ms checkout main
git -C ms merge dev --no-edit
```

This should be a fast-forward if no one commits directly to main.

### 3. Swap API URLs to production

Two files need the URL changed from `swarm-resistance-backend-dev-production` to `swarm-resistance-backend-production`:

- `Assets/RestfulManager.cs` -- two places (line ~63 and ~135)
- `Assets/InventoryBackend.cs` -- one place (line ~194)

Verify no dev URLs remain:
```bash
grep -r "swarm-resistance-backend-dev" ms/Assets/
```

### 4. Bump version

Edit `Assets/Scenes/menu.unity`, find `m_text:` around line 5960, update version number (no `[DEV]` tag for prod).

### 5. Commit and push main

```bash
git -C ms add Assets/RestfulManager.cs Assets/InventoryBackend.cs Assets/Scenes/menu.unity
git -C ms commit -m "feat: Switch to production endpoints, bump vX.Y.Z"
git -C ms push origin main
```

### 6. Update dev version

```bash
git -C ms checkout dev
# Edit menu.unity: set to vX.Y.Z [DEV]
git -C ms add Assets/Scenes/menu.unity
git -C ms commit -m "chore: Bump dev version to vX.Y.Z [DEV]"
git -C ms push origin dev
```

### 7. Build and deploy WebGL

One command, straight into the frontend tree. `-msEnv` must match the branch (`prod` on `main`);
the build refuses to start if the committed backend URL disagrees with it.

```bash
"C:/Program Files/Unity/Hub/Editor/2021.3.45f2/Editor/Unity.exe" \
  -batchmode -quit -nographics \
  -projectPath "<repo>/MedaShooterNeo" \
  -executeMethod BuildScript.BuildWebGLDeploy \
  -msEnv prod -msVersion v6 \
  -msOut "<repo>/frontend/public/unity-builds/medashooter" \
  -logFile build.log
```

Then commit and push `fe`:
```bash
git -C fe add public/unity-builds/ public/medashooter-frame.html vercel.json
git -C fe commit -m "feat: Update WebGL build to vX.Y.Z"
git -C fe push origin main
```

Vercel auto-deploys on push. Requires a signed-in Unity licence -- batchmode fails with
"No valid Unity Editor license found" if the Hub account is not active. GUI fallback: the
same logic is on the Editor `Build/` menu.

#### Version suffix contract -- NOT optional

`vercel.json` serves the data and wasm files with `max-age=31536000`. Reusing a filename strands
returning players on a stale half of the build, and it will not reproduce on your machine because
your cache is cold. **Bump `-msVersion` on every single build that changes anything.**

`BuildWebGLDeploy` renames all four outputs for you:

| Unity emits | Deployed as |
|-------------|-------------|
| `medashooter.data.gz` | `medashooter.data.<ver>.gzip` |
| `medashooter.wasm.gz` | `medashooter.wasm.<ver>.gzip` |
| `medashooter.framework.js.gz` | `medashooter.framework.<ver>.js.gzip` |
| `medashooter.loader.js` | `medashooter.loader.<ver>.js` |

Then update, in the same commit:
- `fe/public/medashooter-frame.html` -- the four `*Url` fields **and** the hardcoded `script.src`
  that loads the loader.
- `fe/vercel.json` -- the four matching routes. Compressed files need
  `Content-Encoding: gzip`; the loader does not.

#### Binary patching is retired (S199)

Builds before v5 were hand-patched with UnityPy (`ms_data_v4_patch.py`) to blank dead
`cryptomeda.tech` links and deactivate a ReneVerse ad panel. **All of that now lives in source** --
see `ms2-pre-coding-checklist.md` Section B. Do not resurrect the patch pipeline; if a dead link
reappears, fix it in the scene or in `OpenLinkButton`'s URL constants and rebuild.

## The frontend merge always conflicts -- resolve it this way (S225)

`fe` `main` and `dev` each carry a *different version* of the same four build artifacts, so
`git merge dev` on `fe` reports rename/rename conflicts on every one plus content conflicts on
`medashooter-frame.html` and `vercel.json`. This is expected, not a sign anything is wrong.

Do NOT hand-resolve the binaries. They are all about to be deleted anyway -- `BuildWebGLDeploy`
clears `Build/` before it writes:

```bash
git -C fe checkout dev -- public/medashooter-frame.html vercel.json   # dev is a strict superset
git -C fe rm -f --ignore-unmatch "public/unity-builds/medashooter/Build/*"
# bump v<N> -> v<N+1> in both text files, THEN run the build, THEN commit
```

**Commit the merge and the new build together, as one commit.** Committing the merge first leaves
a revision on `main` with no game assets at all; push that by accident and Vercel deploys a broken
MedaShooter.

Two traps worth naming:

- **Never `sed 's/\.v13\./.v14./g'` over `vercel.json`.** The `src` fields hold JSON-escaped regex
  (`medashooter\\.data\\.v13\\.gzip$`), and that pattern eats the escapes, silently yielding
  `\.v14..gzip` -- a route that still matches enough to look fine in review but is wrong. Use
  `sed 's/v13/v14/g'`; the bare version token appears nowhere else in the file.
- **Dev and prod must never share a version suffix.** They are different bytes (different baked
  backend host) under a one-year immutable cache. v13 = dev, v14 = prod.

Sanity check after building: the prod data file should differ from the dev one by only a handful of
bytes, and `wasm`/`framework`/`loader` should be identical in size. Same game, different hostname.
A large divergence means the build picked up something else.

## Verification

- Check version label in-game at cryptomeda.tech
- Open browser console, verify API calls go to `swarm-resistance-backend-production`
- Black-box probe needing no auth: `POST /api/v1/minigames/medashooter/run/start/` with `{}`.
  `400 Invalid address` = route deployed AND `MS_RUN_TOKEN_SECRET` set (the secret gate runs
  first). `503` = secret missing. `404` = deploy has not landed.
- Confirm the served assets are byte-identical to the local build, not merely HTTP 200 -- compare
  `curl -w %{size_download}` against `ls -l`. A 200 of the right size can still be the SPA
  `index.html` fallback if the file did not deploy; check `Content-Type` too.

## Known Issues / Gotchas

- Unity regenerates `.spriteatlas` files on open -- these are phantom diffs (LF/CRLF), safe to ignore
- `WebGLBuilds/` is gitignored -- builds go to `fe/`, not committed to `ms/`
- Always push `main` source code **before** opening Unity for build, to avoid mixing temp files with the merge commit
- If merge is not fast-forward, investigate -- direct commits to main are not expected

---

> **Style rules:** Key facts first, tables over prose, Grep-friendly headers, no emojis, no "last updated" dates.
