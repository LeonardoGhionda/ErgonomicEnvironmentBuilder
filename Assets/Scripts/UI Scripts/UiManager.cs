using UnityEngine;
using UnityEngine.Assertions;

public class UiManager : MonoBehaviour
{

    //singleton beheviour
    public static UiManager Instance { get; private set; }
    public string roomName = "";

    [SerializeField] private Canvas startingScreen;
    private System.Collections.Generic.Stack<Canvas> previousScreen;
    private Canvas currentScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // prevent duplicates
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        previousScreen = new();
        currentScreen = startingScreen;
        currentScreen.gameObject.SetActive(true);
    }

    public void ChangeScreen(Canvas next)
    {
        previousScreen.Push(currentScreen);
        currentScreen.gameObject.SetActive(false);
        currentScreen = next;
        currentScreen.gameObject.SetActive(true);
    }

    public void GoToPreviousScreen()
    {
        currentScreen.gameObject.SetActive(false);
        try
        {
            currentScreen = previousScreen.Pop();
        }
        catch
        {
            currentScreen = startingScreen;
        }
        currentScreen.gameObject.SetActive(true);
    }

    public void TurnOff()
    {
        currentScreen.gameObject.SetActive(false);
    }

    /// <summary>
    /// Get the name of the previous screen without changing screens
    /// </summary>
    public string PreviousScreenName()
    {
        try
        {
            return previousScreen.Peek().name;
        }
        catch
        {
            return startingScreen.name;
        }
    }
}


