using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ModelButton : MonoBehaviour
{
    public string OBJFullpath => _path;
    private string _path;

    void Awake()
    {
        _path = Path.Combine(ModelButtonGenerator.ModelsFolder, gameObject.name);
        _path = Directory.GetFiles(_path, "*.obj", SearchOption.TopDirectoryOnly).FirstOrDefault();
    }
}
