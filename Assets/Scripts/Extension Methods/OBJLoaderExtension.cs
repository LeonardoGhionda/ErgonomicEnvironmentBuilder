using Dummiesman;
using System.IO;
using UnityEngine;

public static class OBJLoaderExtension
{
    public static GameObject FindMTLAndLoad(this OBJLoader self, string OBJFilepath)
    {
        string[] lines = File.ReadAllLines(OBJFilepath);
        string mtlFileName = string.Empty;

        // Find the mtllib line
        foreach (string line in lines)
        {
            if (line.StartsWith("mtllib"))
            {
                // Extract filename (mtllib filename.mtl)
                mtlFileName = line.Replace("mtllib", "").Trim();
                break;
            }
        }

        if (!string.IsNullOrEmpty(mtlFileName))
        {
            string dir = Path.GetDirectoryName(OBJFilepath);
            string MTLFilepath = Path.Combine(dir, mtlFileName);
            if (File.Exists(MTLFilepath))
            {
                return self.Load(OBJFilepath, MTLFilepath);
            }
        }

        // Load the OBJ using Dummiesman
        return self.Load(OBJFilepath);
    }
}
