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
