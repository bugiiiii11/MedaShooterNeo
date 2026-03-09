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

1. Open `ms/` in Unity (on `main` branch)
2. Build WebGL
3. Copy build output to `fe/public/unity-builds/medashooter/Build/`
4. Commit and push `fe`:
```bash
git -C fe add public/unity-builds/
git -C fe commit -m "feat: Update WebGL build to vX.Y.Z"
git -C fe push origin main
```

Vercel auto-deploys on push.

## Verification

- Check version label in-game at cryptomeda.tech
- Open browser console, verify API calls go to `swarm-resistance-backend-production`

## Known Issues / Gotchas

- Unity regenerates `.spriteatlas` files on open -- these are phantom diffs (LF/CRLF), safe to ignore
- `WebGLBuilds/` is gitignored -- builds go to `fe/`, not committed to `ms/`
- Always push `main` source code **before** opening Unity for build, to avoid mixing temp files with the merge commit
- If merge is not fast-forward, investigate -- direct commits to main are not expected

---

> **Style rules:** Key facts first, tables over prose, Grep-friendly headers, no emojis, no "last updated" dates.
