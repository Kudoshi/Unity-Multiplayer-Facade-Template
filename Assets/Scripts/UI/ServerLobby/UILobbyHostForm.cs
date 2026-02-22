

using Dreamonaut.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup form used to host a lobby in the lobby selection scene
/// </summary>
public class UILobbyHostForm : MonoBehaviour
{
    [SerializeField] private GameObject _container;
    [SerializeField] private TMP_InputField _inputServerName;
    [SerializeField] private Button _btnPrivateLobby;
    [SerializeField] private Button _btnPublicLobby;
    [SerializeField] private Button _btnHostLobby;
    [SerializeField] private Button _btnCancelLobby;

    private CustomLobbyType _lobbyType;
    private string _defaultServerName;
    private UILobbyManager _manager;

    private void Awake()
    {
        _btnPrivateLobby.onClick.AddListener(OnBtnPrivateLobbyPress);
        _btnPublicLobby.onClick.AddListener(OnBtnPublicLobbyPress);
        _btnHostLobby.onClick.AddListener(OnHostLobbyPress);
        _btnCancelLobby.onClick.AddListener(OnCancelLobby);

        _defaultServerName = _inputServerName.text;
    }

    private void Start()
    {
        _container.SetActive(false);
        ToggleLobbyType(CustomLobbyType.Public);
    }

    public void Initialize(UILobbyManager lobbyManager)
    {
        _manager = lobbyManager;
    }

    public void ShowForm()
    {
        ToggleLobbyType(CustomLobbyType.Public);
        _inputServerName.text = _defaultServerName;
        _container.SetActive(true);
    }

    public void CloseForm()
    {
        _container.SetActive(false);
    }

    private void OnCancelLobby()
    {
        CloseForm();
    }

    

    private void OnBtnPublicLobbyPress()
    {
        ToggleLobbyType(CustomLobbyType.Public);
    }

    private void OnBtnPrivateLobbyPress()
    {
        ToggleLobbyType(CustomLobbyType.Private);
    }

    private void OnHostLobbyPress()
    {
        _manager.HostLobby(_lobbyType, _inputServerName.text);
    }
    private void ToggleLobbyType(CustomLobbyType type)
    {
        if (type == CustomLobbyType.Public)
        {
            _btnPrivateLobby.interactable = true;
            _btnPublicLobby.interactable = false;
        }
        else if (type == CustomLobbyType.Private)
        {
            _btnPrivateLobby.interactable = false;
            _btnPublicLobby.interactable = true;
        }

        _lobbyType = type;
    }

}