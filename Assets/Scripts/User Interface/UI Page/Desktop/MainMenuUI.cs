using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button newRoom, loadRoom, options;

    public event Action OnNewRoomClicked;
    public event Action OnLoadRoomClicked;
    public event Action OnOptionsClicked;
    [SerializeField] private Image goBackLoadUi; // The fill image for long pres
    [SerializeField] private LoadingCircle loadingCircle;

    private void Start()
    {
        newRoom.onClick.AddListener(() => OnNewRoomClicked?.Invoke());
        loadRoom.onClick.AddListener(() => OnLoadRoomClicked?.Invoke());
        options.onClick.AddListener(() => OnOptionsClicked?.Invoke());
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public void UpdateLoadingCircle(float percent) => loadingCircle.SetLoadProgress(percent);

    private void OnDisable()
    {
        UpdateLoadingCircle(0);
    }

}