using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

[InitializeOnLoad]
public static class AppProfileHelper
{
    private const string BuildFolderName = "Build";
    private const string DesktopSubFolder = "Desktop";
    private const string ImmersiveSubFolder = "Immersive";
    private const string BaseName = "EEB";
    private const string DTSuffix = "_Desktop.exe";
    private const string VRSuffix = "_Immersive.exe";
    private const string XR_DEFINE = "USE_XR";

    static AppProfileHelper()
    {
        // Hook into SceneView drawing
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        string currentDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        bool isVR = currentDefines.Contains(XR_DEFINE);
        string profileName = isVR ? "Immersive" : "Desktop";
        Color profileColor = isVR ? Color.green : Color.cyan;

        Handles.BeginGUI();

        Rect rect = new(10, 10, 130, 25);
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.7f));

        GUIStyle style = new(EditorStyles.boldLabel);
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleLeft;
        style.contentOffset = new Vector2(10, 0);

        GUIStyle valueStyle = new(style);
        valueStyle.normal.textColor = profileColor;
        valueStyle.alignment = TextAnchor.MiddleRight;
        valueStyle.contentOffset = new Vector2(-10, 0);

        GUI.Label(rect, "Profile:", style);
        GUI.Label(rect, profileName, valueStyle);

        Handles.EndGUI();
    }

    [MenuItem("Build/Build All Profiles")]
    public static void BuildAllVariants()
    {
        string nextVersion = GetNextVersion();
        string rootPath = Path.Combine(Directory.GetCurrentDirectory(), BuildFolderName, nextVersion);

        string currentDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        bool isVR = currentDefines.Contains(XR_DEFINE);

        if(isVR)
        {
            // Build the current profile before (faster)
            BuildVR(rootPath);
            BuildDT(rootPath);
            // Return to the initial profile
            FlagVR();
        }
        else
        {
            BuildDT(rootPath);
            BuildVR(rootPath);
            FlagDesktop();
        }
        
        Debug.Log($"Batch build complete for version: {nextVersion}");
    }

    [MenuItem("Build/Build Immersive Profile")]
    public static void BuildVR()
    {
        string nextVersion = GetNextVersion();
        string rootPath = Path.Combine(Directory.GetCurrentDirectory(), BuildFolderName, nextVersion);
        BuildVR(rootPath);
    }

    private static void BuildVR(string rootPath)
    {
        SetXRState(true);
        ExecuteBuild(Path.Combine(rootPath, ImmersiveSubFolder, BaseName + VRSuffix));
    }

    [MenuItem("Build/Build Desktop Profile")]
    public static void BuildDT()
    {
        string nextVersion = GetNextVersion();
        string rootPath = Path.Combine(Directory.GetCurrentDirectory(), BuildFolderName, nextVersion);
        BuildDT(rootPath);
    }

    private static void BuildDT(string rootPath)
    {
        SetXRState(false);
        ExecuteBuild(Path.Combine(rootPath, DesktopSubFolder, BaseName + DTSuffix));
    }

    [MenuItem("Profile/Desktop Profile")]
    public static void FlagDesktop()
    {
        SetXRState(false);
    }

    [MenuItem("Profile/VR Profile")]
    public static void FlagVR()
    {
        SetXRState(true);
    }

    private static void SetXRState(bool enable)
    {
        string currentDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        string newDefines;

        if (enable)
        {
            if (!currentDefines.Contains(XR_DEFINE))
            {
                newDefines = string.IsNullOrEmpty(currentDefines) ? XR_DEFINE : currentDefines + ";" + XR_DEFINE;
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, newDefines);
            }
        }
        else
        {
            newDefines = string.Join(";", currentDefines.Split(';').Where(d => d != XR_DEFINE));
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, newDefines);
        }

        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget buildTargetSettings);

        if (buildTargetSettings != null)
        {
            XRGeneralSettings settings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            string openXRLoaderType = "Unity.XR.OpenXR.OpenXRLoader";

            settings.InitManagerOnStart = enable;

            if (enable)
            {
                XRPackageMetadataStore.AssignLoader(settings.AssignedSettings, openXRLoaderType, BuildTargetGroup.Standalone);
            }
            else
            {
                XRPackageMetadataStore.RemoveLoader(settings.AssignedSettings, openXRLoaderType, BuildTargetGroup.Standalone);
            }

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(settings.AssignedSettings);
            AssetDatabase.SaveAssets();

            // Forces overlay update
            SceneView.RepaintAll();
            Debug.Log($"XR State set to: {enable}");
        }
        else
        {
            Debug.LogError("XRGeneralSettings not found.");
        }
    }

    private static void ExecuteBuild(string path)
    {
        BuildPlayerOptions options = new()
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {path}");
        }
    }

    private static string GetNextVersion()
    {
        string buildRoot = Path.Combine(Directory.GetCurrentDirectory(), BuildFolderName);

        if (!Directory.Exists(buildRoot))
        {
            Directory.CreateDirectory(buildRoot);
            return "1.0.0";
        }

        string[] directories = Directory.GetDirectories(buildRoot);
        if (directories.Length == 0) return "1.0.0";

        var versions = directories
            .Select(d => Path.GetFileName(d))
            .Where(name => Version.TryParse(name, out _))
            .Select(name => new Version(name))
            .OrderByDescending(v => v)
            .ToList();

        if (versions.Count == 0) return "1.0.0";

        Version lastVersion = versions[0];
        Version nextVersion = new(lastVersion.Major, lastVersion.Minor, lastVersion.Build + 1);

        return nextVersion.ToString();
    }
}