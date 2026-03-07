---
name: build
description: Build Unity project for Windows or WebGL
allowed-tools:
  - Bash(*)
  - Read
  - Grep
---

# Unity Build Skill

Build the MedaShooterNeo Unity project for testing.

## Configuration

- **Unity Version**: 2021.3.45f2
- **Unity Path**: `C:/Program Files/Unity/Hub/Editor/2021.3.45f2/Editor/Unity.exe`
- **Project Path**: Current working directory

## Available Build Targets

| Target | Command | Output Location |
|--------|---------|-----------------|
| Windows (Dev) | `BuildScript.BuildWindows` | `Builds/Windows/<timestamp>/` |
| Windows (Release) | `BuildScript.BuildWindowsRelease` | `Builds/Windows/<timestamp>/` |
| WebGL (Dev) | `BuildScript.BuildWebGL` | `WebGLBuilds/<date>/` |
| WebGL (Release) | `BuildScript.BuildWebGLRelease` | `WebGLBuilds/<date>/` |
| All Platforms | `BuildScript.BuildAll` | Both locations |

## Instructions

When the user invokes `/build`, determine which platform they want:

1. **Parse the argument**:
   - `/build` or `/build windows` → Build Windows Development
   - `/build webgl` → Build WebGL Development
   - `/build all` → Build both platforms
   - `/build release` → Build Windows Release
   - `/build webgl release` → Build WebGL Release

2. **Run the build command**:
   ```bash
   "C:/Program Files/Unity/Hub/Editor/2021.3.45f2/Editor/Unity.exe" -quit -batchmode -projectPath "." -executeMethod BuildScript.<MethodName> -logFile -
   ```

   The `-logFile -` flag outputs logs to stdout so you can see progress.

3. **Report the result**:
   - If exit code is 0: Build succeeded, tell user the output path
   - If exit code is non-zero: Build failed, show the error from logs

4. **After successful Windows build**:
   - Tell the user they can find the executable at `Builds/Windows/<timestamp>/MedaShooterNeo.exe`
   - Optionally offer to open the folder or run the build

## Example Invocations

User: `/build`
→ Run Windows Development build

User: `/build webgl`
→ Run WebGL Development build

User: `/build all`
→ Run both Windows and WebGL builds

## Important Notes

- The build runs in batch mode (no Unity GUI)
- Unity must NOT be running the same project (close Unity first, or it will fail)
- Build output includes timestamps so multiple builds don't overwrite each other
- Check the Unity console via `read_console` MCP tool if build fails for detailed errors
