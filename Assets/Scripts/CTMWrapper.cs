using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class StepToObjWrapper
{
    const string DLL = "CTM"; // omit extension

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int LoadStepAndTriangulate(string path, StringBuilder msg, int msgLen);

    public static bool Convert(string path, float scale = 1)
    {
        var msg = new StringBuilder(512);
        int result = LoadStepAndTriangulate(path, msg, msg.Capacity);
        if (result == 0 && scale != 1)
        {
            //readline 
            path = Path.ChangeExtension(path, "obj");
            if (File.Exists(path))
            {
                string tempPath = path + ".tmp";

                using var reader = new StreamReader(path);
                using var writer = new StreamWriter(tempPath);

                string line;
                var inv = System.Globalization.CultureInfo.InvariantCulture;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("v "))
                    {
                        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        float x = float.Parse(parts[1], inv) * scale;
                        float y = float.Parse(parts[2], inv) * scale;
                        float z = float.Parse(parts[3], inv) * scale;

                        writer.WriteLine(
                            $"v {x.ToString("0.######", inv)} {y.ToString("0.######", inv)} {z.ToString("0.######", inv)}"
                        );
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }

                writer.Flush();
                writer.Close();
                reader.Close();

                File.Delete(path);
                File.Move(tempPath, path);
            }
        }

        return result == 0;
    }
}

