using System.IO;
using System.Linq;
using UnityEngine;

public class ModelButton : MonoBehaviour
{
    public string OBJFullpath => _path;
    private string _path;

    void Awake()
    {
        _path = Path.Combine(ImportUtils.ModelsPath, gameObject.name);
        // Get the first .obj file that doesn't contain a '#' character (scale modified models)
        _path = Directory.GetFiles(_path, "*.obj", SearchOption.TopDirectoryOnly).FirstOrDefault(s => !s.Contains("#m")); 
    }
}
