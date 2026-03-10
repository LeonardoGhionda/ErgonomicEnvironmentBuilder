using System.Linq;
using UnityEditor;
using UnityEditor.Build;


public static class BuildVariants
{
    [MenuItem("Build/Build Desktop")]
    public static void BuildDesktop()
    {
        SetOpenXRFlag(false);

        // Build desktop version
        _ = BuildPipeline.BuildPlayer(GetScenes(), "Builds/Desktop/ErgonomicEnvironmentBuilder.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    [MenuItem("Build/Build VR")]
    public static void BuildVR()
    {
        SetOpenXRFlag(true);

        // Build VR version
        _ = BuildPipeline.BuildPlayer(GetScenes(), "Builds/VR/ErgonomicEnvironmentBuilder.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    [MenuItem("Flag/Desktop")]
    public static void FlagDesktop()
    {
        SetOpenXRFlag(false);
    }

    [MenuItem("Flag/VR")]
    public static void FlagVR()
    {
        SetOpenXRFlag(true);
    }

    private static void SetOpenXRFlag(bool defined)
    {
        UnityEngine.Debug.LogWarning("WARNING!!! OpenXR must be toggled manually, don't forget >:D");
        if (defined)
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone,
                PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone) + ";USE_XR");
        }
        else
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            defines = string.Join(";", defines.Split(';').Where(d => d != "USE_XR"));
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, defines);
        }
    }

    static string[] GetScenes()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        return scenes;
    }
}

