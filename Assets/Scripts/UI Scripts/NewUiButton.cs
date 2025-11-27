using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NewUiButton: MonoBehaviour
{
    UiManager uiManager;
    [SerializeField] private Canvas nextPage;

    private void Start()
    {
        uiManager = UiManager.Instance;
        var button = GetComponent<Button>();

        button.onClick.AddListener(() =>
        {
            uiManager.ChangeScreen(nextPage);
        });
    }
}
