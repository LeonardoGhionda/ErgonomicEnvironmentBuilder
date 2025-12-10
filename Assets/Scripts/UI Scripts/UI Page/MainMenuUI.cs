using UnityEngine;
using UnityEngine.UI;
using System; 

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button newRoom, loadRoom, options;

    public event Action OnNewRoomClicked;
    public event Action OnLoadRoomClicked;
    public event Action OnOptionsClicked;

    private void Start()
    {
        newRoom.onClick.AddListener(() => OnNewRoomClicked?.Invoke());
        loadRoom.onClick.AddListener(() => OnLoadRoomClicked?.Invoke());
        options.onClick.AddListener(() => OnOptionsClicked?.Invoke());
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}