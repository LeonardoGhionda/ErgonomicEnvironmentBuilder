using UnityEngine;
using UnityEngine.SceneManagement;

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