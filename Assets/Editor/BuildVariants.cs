using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;

public static class BuildVariants
{
    [MenuItem("Config/Desktop Profile")]
    public static void FlagDesktop()
    {
        SetXRState(false);
    }

    [MenuItem("Config/VR Profile")]
    public static void FlagVR()
    {
        SetXRState(true);
    }

    private static void SetXRState(bool enable)
    {
        // Handle Scripting Define Symbols
        string currentDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        string newDefines;

        if (enable)
        {
            if (!currentDefines.Contains("USE_XR"))
            {
                newDefines = string.IsNullOrEmpty(currentDefines) ? "USE_XR" : currentDefines + ";USE_XR";
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, newDefines);
            }
        }
        else
        {
            newDefines = string.Join(";", currentDefines.Split(';').Where(d => d != "USE_XR"));
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, newDefines);
        }

        // Handle XR Plugin Management Settings
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget buildTargetSettings);

        if (buildTargetSettings != null)
        {
            XRGeneralSettings settings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);

            // Toggle Initialize XR on Startup checkbox
            settings.InitManagerOnStart = enable;

            // This string is the internal ID for the OpenXR Loader
            string openXRLoaderType = "Unity.XR.OpenXR.OpenXRLoader";

            if (enable)
            {
                // Assign the loader to the Standalone target
                XRPackageMetadataStore.AssignLoader(settings.AssignedSettings, openXRLoaderType, BuildTargetGroup.Standalone);
            }
            else
            {
                // Remove the loader from the Standalone target
                XRPackageMetadataStore.RemoveLoader(settings.AssignedSettings, openXRLoaderType, BuildTargetGroup.Standalone);
            }

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(settings.AssignedSettings);

            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"XR State successfully set to: {enable}");
        }
        else
        {
            UnityEngine.Debug.LogError("XRGeneralSettings not found. Is XR Plugin Management installed?");
        }
    }
}