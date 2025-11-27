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
        BuildPipeline.BuildPlayer(GetScenes(), "Builds/Multy-build Test/Desktop/App.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    [MenuItem("Build/Build VR")]
    public static void BuildVR()
    {
        SetOpenXRFlag(true);

        // Build VR version
        BuildPipeline.BuildPlayer(GetScenes(), "Builds/Multy-build Test/VR/App.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    private static void SetOpenXRFlag(bool defined)
    {
        if(defined)
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone,
                PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone) + ";USE_XR");
        }
        else
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            defines = string.Join(";", defines.Split(';').Where(d => d != "USE_XR"));
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, defines);
        }
    }

    static string[] GetScenes()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        return scenes;
    }
}

