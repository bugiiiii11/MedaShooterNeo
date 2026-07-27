using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build automation script for MedaShooterNeo.
/// Can be called from command line via: Unity.exe -batchmode -executeMethod BuildScript.BuildWindows
/// </summary>
public static class BuildScript
{
    private const string BuildRootFolder = "Builds";
    private const string ProductName = "MedaShooterNeo";

    /// <summary>
    /// Build for Windows 64-bit (Development build with debugging)
    /// </summary>
    [MenuItem("Build/Windows Development")]
    public static void BuildWindowsDevelopment()
    {
        BuildWindows(BuildOptions.Development | BuildOptions.AllowDebugging);
    }

    /// <summary>
    /// Build for Windows 64-bit (Release build)
    /// </summary>
    [MenuItem("Build/Windows Release")]
    public static void BuildWindowsRelease()
    {
        BuildWindows(BuildOptions.None);
    }

    /// <summary>
    /// Build for Windows - called from command line for development builds
    /// </summary>
    public static void BuildWindows()
    {
        BuildWindows(BuildOptions.Development | BuildOptions.AllowDebugging);
    }

    private static void BuildWindows(BuildOptions options)
    {
        string timestamp = DateTime.Now.ToString("MMddyy_HHmm");
        string buildPath = Path.Combine(BuildRootFolder, "Windows", timestamp);
        string executablePath = Path.Combine(buildPath, $"{ProductName}.exe");

        // Ensure directory exists
        Directory.CreateDirectory(buildPath);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = options
        };

        Build(buildPlayerOptions, "Windows");
    }

    /// <summary>
    /// Build for WebGL (Development build)
    /// </summary>
    [MenuItem("Build/WebGL Development")]
    public static void BuildWebGLDevelopment()
    {
        BuildWebGL(BuildOptions.Development);
    }

    /// <summary>
    /// Build for WebGL (Release build)
    /// </summary>
    [MenuItem("Build/WebGL Release")]
    public static void BuildWebGLRelease()
    {
        BuildWebGL(BuildOptions.None);
    }

    /// <summary>
    /// Build for WebGL - called from command line for development builds
    /// </summary>
    public static void BuildWebGL()
    {
        BuildWebGL(BuildOptions.Development);
    }

    private static void BuildWebGL(BuildOptions options)
    {
        string timestamp = DateTime.Now.ToString("MMddyy");
        string buildPath = Path.Combine("WebGLBuilds", timestamp);

        // Ensure directory exists
        Directory.CreateDirectory(buildPath);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = options
        };

        Build(buildPlayerOptions, "WebGL");
    }

    /// <summary>
    /// Build both Windows and WebGL (Development builds)
    /// </summary>
    [MenuItem("Build/All Platforms (Development)")]
    public static void BuildAll()
    {
        BuildWindows();
        BuildWebGL();
    }

    // ---------------------------------------------------------------------
    // Deployable WebGL build (MS 2.0 Phase 0, S199)
    //
    // The one documented way to produce a build the frontend can serve. Replaces
    // hand-clicked Editor builds plus the retired UnityPy binary-patch pipeline.
    //
    //   Unity.exe -batchmode -quit -projectPath <repo> -logFile build.log \
    //     -executeMethod BuildScript.BuildWebGLDeploy \
    //     -msEnv dev -msVersion v5 -msOut <frontend>/public/unity-builds/medashooter
    //
    // Every compressed output carries the version suffix because Vercel serves the
    // data and wasm files with a 1-year immutable cache: a rebuild that reuses a
    // filename strands returning players on a stale half of the build.
    // ---------------------------------------------------------------------

    private const string DevBackendHost = "swarm-resistance-backend-dev-production.up.railway.app";
    private const string ProdBackendHost = "swarm-resistance-backend-production.up.railway.app";

    private static readonly string[] BackendUrlSources =
    {
        "Assets/RestfulManager.cs",
        "Assets/InventoryBackend.cs"
    };

    public static void BuildWebGLDeploy()
    {
        var env = GetArg("-msEnv", "dev").ToLowerInvariant();
        var version = GetArg("-msVersion", null);
        var outDir = GetArg("-msOut", null);

        if (string.IsNullOrEmpty(version))
        {
            Fail("-msVersion is required (e.g. -msVersion v5). It becomes the cache-busting suffix.");
            return;
        }

        if (string.IsNullOrEmpty(outDir))
        {
            Fail("-msOut is required: the medashooter folder under the frontend's public/unity-builds/.");
            return;
        }

        if (env != "dev" && env != "prod")
        {
            Fail($"-msEnv must be 'dev' or 'prod', got '{env}'.");
            return;
        }

        if (!VerifyBackendUrls(env))
        {
            return;
        }

        var buildDir = Path.Combine(outDir, "Build");

        // A stale file from a previous version would keep being served: the frame
        // names each file explicitly, but leftovers make it impossible to tell which
        // build is actually deployed.
        if (Directory.Exists(buildDir))
        {
            Debug.Log($"[BuildScript] Clearing previous build output at {buildDir}");
            Directory.Delete(buildDir, true);
        }

        Directory.CreateDirectory(outDir);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildScript] Deploy build -- env={env} version={version} out={outDir}");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if (report.summary.result != BuildResult.Succeeded)
        {
            LogBuildFailure(report, "WebGL deploy");
            Fail($"WebGL deploy build failed with {report.summary.totalErrors} error(s).");
            return;
        }

        Debug.Log($"[BuildScript] Build succeeded in {report.summary.totalTime.TotalSeconds:F1}s, applying version suffix '{version}'.");

        if (!ApplyVersionSuffix(buildDir, version))
        {
            return;
        }

        Debug.Log("[BuildScript] Deploy build COMPLETE. Update medashooter-frame.html and vercel.json to the new filenames.");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    /// <summary>
    /// Fails the build when the committed backend URL does not match the requested
    /// target -- the classic "shipped a dev-URL build to prod" mistake, which is
    /// silent at build time and only shows up as dead API calls in the browser.
    /// </summary>
    private static bool VerifyBackendUrls(string env)
    {
        var expected = env == "prod" ? ProdBackendHost : DevBackendHost;
        var forbidden = env == "prod" ? DevBackendHost : ProdBackendHost;

        // Application.dataPath is <project>/Assets; batchmode does not guarantee that
        // the working directory is the project root.
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;

        foreach (var relativePath in BackendUrlSources)
        {
            var fullPath = Path.Combine(projectRoot, relativePath);

            if (!File.Exists(fullPath))
            {
                Fail($"Backend URL guard: expected source file is missing: {relativePath}");
                return false;
            }

            var source = File.ReadAllText(fullPath);

            // Neither host is a substring of the other, so a hit on the wrong one is decisive.
            if (source.Contains(forbidden))
            {
                Fail($"Backend URL guard: {relativePath} still points at the {(env == "prod" ? "DEV" : "PROD")} backend ({forbidden}) but the requested target is {env}. Refusing to build.");
                return false;
            }

            if (!source.Contains(expected))
            {
                Fail($"Backend URL guard: {relativePath} does not reference the expected {env} backend ({expected}). Refusing to build.");
                return false;
            }
        }

        Debug.Log($"[BuildScript] Backend URL guard passed for env={env} ({expected}).");
        return true;
    }

    /// <summary>
    /// Unity emits medashooter.data.gz / .wasm.gz / .framework.js.gz / .loader.js.
    /// The frontend serves the compressed ones with Content-Encoding: gzip under a
    /// .gzip extension, and every file gets the version suffix so a rebuild can never
    /// collide with a 1-year-cached predecessor.
    /// </summary>
    private static bool ApplyVersionSuffix(string buildDir, string version)
    {
        var renames = new (string From, string To)[]
        {
            ("medashooter.data.gz",         $"medashooter.data.{version}.gzip"),
            ("medashooter.wasm.gz",         $"medashooter.wasm.{version}.gzip"),
            ("medashooter.framework.js.gz", $"medashooter.framework.{version}.js.gzip"),
            ("medashooter.loader.js",       $"medashooter.loader.{version}.js")
        };

        foreach (var (from, to) in renames)
        {
            var fromPath = Path.Combine(buildDir, from);
            var toPath = Path.Combine(buildDir, to);

            if (!File.Exists(fromPath))
            {
                Fail($"Expected build output '{from}' not found in {buildDir}. " +
                     "Check that WebGL compression format is still Gzip in Player Settings.");
                return false;
            }

            File.Move(fromPath, toPath);
            var sizeMb = new FileInfo(toPath).Length / (1024f * 1024f);
            Debug.Log($"[BuildScript]   {from} -> {to} ({sizeMb:F2} MB)");
        }

        return true;
    }

    private static string GetArg(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }
        return fallback;
    }

    private static void LogBuildFailure(BuildReport report, string label)
    {
        Debug.LogError($"[BuildScript] {label} build FAILED! errors={report.summary.totalErrors} warnings={report.summary.totalWarnings}");

        foreach (var step in report.steps)
        {
            foreach (var message in step.messages)
            {
                if (message.type == LogType.Error || message.type == LogType.Exception)
                {
                    Debug.LogError($"[BuildScript] {message.content}");
                }
            }
        }
    }

    private static void Fail(string message)
    {
        Debug.LogError($"[BuildScript] {message}");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void Build(BuildPlayerOptions options, string platformName)
    {
        Debug.Log($"[BuildScript] Starting {platformName} build...");
        Debug.Log($"[BuildScript] Output: {options.locationPathName}");
        Debug.Log($"[BuildScript] Scenes: {string.Join(", ", options.scenes)}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] {platformName} build SUCCEEDED!");
            Debug.Log($"[BuildScript] Size: {summary.totalSize / (1024 * 1024):F2} MB");
            Debug.Log($"[BuildScript] Time: {summary.totalTime.TotalSeconds:F1} seconds");
            Debug.Log($"[BuildScript] Output: {options.locationPathName}");

            // For batch mode, exit with success
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        else
        {
            Debug.LogError($"[BuildScript] {platformName} build FAILED!");
            Debug.LogError($"[BuildScript] Errors: {summary.totalErrors}");
            Debug.LogError($"[BuildScript] Warnings: {summary.totalWarnings}");

            // Log each error
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        Debug.LogError($"[BuildScript] {message.content}");
                    }
                }
            }

            // For batch mode, exit with failure
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }

    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }
}
