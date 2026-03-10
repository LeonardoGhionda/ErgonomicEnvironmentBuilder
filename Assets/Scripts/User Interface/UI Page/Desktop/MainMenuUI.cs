using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button newRoom, loadRoom, options;

    public event Action OnNewRoomClicked;
    public event Action OnLoadRoomClicked;
    public event Action OnOptionsClicked;
    public event Action OnJoinClicked;


    [SerializeField] private Image goBackLoadUi; // The fill image for long pres
    [SerializeField] private LoadingCircle loadingCircle;
    [SerializeField] private RectTransform sessionInvitation;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button joinButton;

    private DesktopSessionListener _sessionListener;

    // !!! keep _notificationDuration > _fadeInDuration * 2
    private float _notificationTimer = 0f;
    private readonly float _notificationDuration = 20f;
    private readonly float _fadeInDuration = 1f;
    private bool _notificationWasOn = false;
    

    private void Start()
    {
        newRoom.onClick.AddListener(() => OnNewRoomClicked?.Invoke());
        loadRoom.onClick.AddListener(() => OnLoadRoomClicked?.Invoke());
        options.onClick.AddListener(() => OnOptionsClicked?.Invoke());

        _sessionListener = FindAnyObjectByType<DesktopSessionListener>();
        _sessionListener.InvitationRecevied += ShowInvitation;

        joinButton.onClick.AddListener(() =>{ OnJoinClicked?.Invoke(); });
    }

    private void Update()
    {
        // Notification life handling
        if (sessionInvitation.gameObject.activeSelf)
        {
            _notificationTimer += Time.deltaTime;

            // Notification happer gradually (completely visible at fadeInDuration seconds)
            float fadeIn = 
            _notificationWasOn?
                1.0f :
                Mathf.Clamp01(_notificationTimer / _fadeInDuration);
            
            // After half of the lifetime passed start to fade out
            float fadeOut =  
            Mathf.Clamp01(1 - ((_notificationTimer / _notificationDuration - .5f) * 2f));

            canvasGroup.alpha = Mathf.Min(fadeIn, fadeOut);

            // When complitely fades out, set inactive
            if (canvasGroup.alpha <= 0)  sessionInvitation.gameObject.SetActive(false);
        }
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public void UpdateLoadingCircle(float percent) => loadingCircle.SetLoadProgress(percent);

    private void OnDisable()
    {
        UpdateLoadingCircle(0);
        if(_sessionListener != null) _sessionListener.InvitationRecevied -= ShowInvitation;
    }

    private void ShowInvitation(string roomName)
    {
        _notificationWasOn = _sessionListener.gameObject.activeSelf;
        sessionInvitation.gameObject.SetActive(true);
        roomNameText.text = roomName;
        _notificationTimer = 0;
    }
}