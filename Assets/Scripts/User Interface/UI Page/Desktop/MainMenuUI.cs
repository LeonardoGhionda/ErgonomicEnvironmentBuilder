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

    [SerializeField] private Image goBackLoadUi;
    [SerializeField] private LoadingCircle loadingCircle;
    [SerializeField] private RectTransform sessionInvitation;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button joinButton;

    private SpectatorNetworkManager _sessionListener;

    private float _notificationTimer = 0f;
    private readonly float _notificationDuration = 20f;
    private readonly float _fadeInDuration = 1f;
    private bool _notificationWasOn = false;

    private void Start()
    {
        newRoom.onClick.AddListener(() => OnNewRoomClicked?.Invoke());
        loadRoom.onClick.AddListener(() => OnLoadRoomClicked?.Invoke());
        options.onClick.AddListener(() => OnOptionsClicked?.Invoke());
        joinButton.onClick.AddListener(() => OnJoinClicked?.Invoke());
    }

    private void OnEnable()
    {
        if (_sessionListener == null) _sessionListener = FindAnyObjectByType<SpectatorNetworkManager>();
        _sessionListener.InvitationRecevied += ShowInvitation;
    }

    private void Update()
    {
        if (sessionInvitation.gameObject.activeSelf)
        {
            _notificationTimer += Time.deltaTime;

            float fadeIn = _notificationWasOn ? 1.0f : Mathf.Clamp01(_notificationTimer / _fadeInDuration);

            float fadeOut = Mathf.Clamp01(1 - ((_notificationTimer / _notificationDuration - .5f) * 2f));

            canvasGroup.alpha = Mathf.Min(fadeIn, fadeOut);

            if (canvasGroup.alpha <= 0) sessionInvitation.gameObject.SetActive(false);
        }
    }

    public void Show() => gameObject.SetActive(true);

    public void Hide() => gameObject.SetActive(false);

    public void UpdateLoadingCircle(float percent) => loadingCircle.SetLoadProgress(percent);

    private void OnDisable()
    {
        UpdateLoadingCircle(0);

        if (_sessionListener != null)
        {
            _sessionListener.InvitationRecevied -= ShowInvitation;
        }
    }

    private void ShowInvitation(string roomName)
    {
        _notificationWasOn = _sessionListener.gameObject.activeSelf;
        sessionInvitation.gameObject.SetActive(true);
        roomNameText.text = roomName;
        _notificationTimer = 0;
    }
}