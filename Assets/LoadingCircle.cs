using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LoadingCircle : MonoBehaviour
{
    private Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
        gameObject.SetActive(false);
    }

    public void SetLoadProgress(float percent)
    {
        if (percent <= 0)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.gameObject.SetActive(true);
            _image.fillAmount = percent;
        }
    }
}
