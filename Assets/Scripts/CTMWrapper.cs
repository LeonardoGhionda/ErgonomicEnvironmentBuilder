using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static class StepToObjWrapper
{
    const string DLL = "CTM"; // omit extension

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int LoadStepAndTriangulate(string path, StringBuilder msg, int msgLen);

    public static bool Convert(string path)
    {
        var msg = new StringBuilder(512);
        int result = LoadStepAndTriangulate(path, msg, msg.Capacity);
        Debug.Log($"Native result {result}: {msg}");
        return result == 0;
    }
}

