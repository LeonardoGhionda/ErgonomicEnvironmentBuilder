using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.XR.Management;

public class SceneLoader : MonoBehaviour
{

    void Start()
    {
#if USE_XR
        SceneManager.LoadScene("VR");
#else
        SceneManager.LoadScene("Desktop");
#endif
    }
}