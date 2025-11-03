
using Kudoshi.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles the UI + Logic side of the lobby listing
/// It will first wait for multiplayer services to be up before fetching list of lobbies
/// 
/// Features:
///     - Search lobby by code
///     - Search lobby by name
///     - Fetch listing of lobbies
///     - Host lobby
///     
/// The manager will auto fetch new list every 5 seconds
/// </summary>
public class UILobbyManager : MonoBehaviour
{

    [SerializeField] private TMP_InputField _inputLobbyCode;
    [SerializeField] private TMP_InputField _inputSearchLobby;
    [SerializeField] private Transform _parentContent;
    [SerializeField] private UILobbyHostForm _hostForm;

    [Header("Buttons")]
    [SerializeField] private Button _btnRefresh;
    [SerializeField] private Button _btnLobbyCode;
    [SerializeField] private Button _btnSearchLobby;
    [SerializeField] private Button _btnHostLobby;
    [SerializeField] private Button _btnBack;
    [Header("Prefabs")]
    [SerializeField] private UILobbyListingRecord _recordPf;
    [Header("Configs")]
    [SerializeField] private string _backSceneName;
    [SerializeField] private string _lobbyRoomSceneName;
    [SerializeField] private float _refreshListingTime = 5f;

    private List<UILobbyListingRecord> _records = new List<UILobbyListingRecord>();
    private float _nextRefreshListingTime = 99999;

    private void Awake()
    {
        _btnRefresh.onClick.AddListener(RefreshListing);
        _btnLobbyCode.onClick.AddListener(Btn_JoinLobbyPressed);
        _btnHostLobby.onClick.AddListener(Btn_HostLobbyPressed);
        _btnSearchLobby.onClick.AddListener(Btn_SearchLobbyPressed);
        _btnBack.onClick.AddListener(Btn_BackMenu);

    }

    private void Start()
    {
        _hostForm.Initialize(this);
        Util.WaitNextFrame(this, () => StartCoroutine(FirstTimeShowListing()));
        
    }

    private void Update()
    {
        if (Time.time >= _nextRefreshListingTime)
            RefreshListing();
    }

    #region Button Events
    private async void Btn_JoinLobbyPressed()
    {
        await JoinLobbyByLobbyCode();
    }

    private void Btn_HostLobbyPressed()
    {
        _hostForm.ShowForm();
    }

    private void Btn_SearchLobbyPressed()
    {
        SearchLobby(_inputSearchLobby.text);
    }

    private void Btn_BackMenu()
    {
        SceneManager.LoadScene(_backSceneName);
    }

    #endregion

    /// <summary>
    /// Triggered by joining via lobby listings
    /// </summary>
    /// <param name="lobbyID"></param>
    public async void JoinLobby(string lobbyID)
    {
        LobbyEnterResult result = await MultiplayerFacade.Instance.ServiceLobby.JoinLobbyByID(lobbyID);

        if (result == LobbyEnterResult.Success)
        {
            Debug.Log("[UILobbyManager] Lobby Entered");
            SceneManager.LoadScene(_lobbyRoomSceneName);
        }
        else
        {
            Debug.Log("[UILobbyManager] Lobby Join Error");
        }
    }
   
    public async void RefreshListing()
    {
        _nextRefreshListingTime = Time.time + _refreshListingTime;

        CustomLobby[] lobbies = await MultiplayerFacade.Instance.ServiceLobby.GetAvailableLobbyList();

        ShowListing(lobbies);

    }

    /// <summary>
    /// Triggered via join lobby code search
    /// </summary>
    /// <returns></returns>
    public async Task JoinLobbyByLobbyCode()
    {
        string lobbyCode = _inputLobbyCode.text;
        Debug.Log("Lobby Code: " + lobbyCode);

        LobbyEnterResult result = await MultiplayerFacade.Instance.ServiceLobby.JoinLobbyByLobbyCode(lobbyCode);

        if (result == LobbyEnterResult.Success)
        {
            Debug.Log("[UILobbyManager] Lobby Entered");
            SceneManager.LoadScene(_lobbyRoomSceneName);
        }
        else
        {
            Debug.Log("[UILobbyManager] Lobby Join Error");
        }
    }

    public async void SearchLobby(string lobbyName)
    {
        CustomLobby[] lobbies = await MultiplayerFacade.Instance.ServiceLobby.GetAvailableLobbyByName(lobbyName);

        ShowListing(lobbies);
        _nextRefreshListingTime = Mathf.Infinity;
    }

    public async void HostLobby(CustomLobbyType lobbyType, string serverName)
    {
        CustomLobbyConfig config = new CustomLobbyConfig(serverName, GameConstantVariables.MAX_PLAYERS, lobbyType);

        string lobbyCode = await MultiplayerFacade.Instance.ServiceLobby.HostLobby(config);

        if (lobbyCode != null)
        {
            SceneManager.LoadScene(_lobbyRoomSceneName);
            NetworkLog.LogDev("[UILobbyManager] Lobby hosted");
        }
        else
        {
            NetworkLog.LogDev("[UILobbyManager] Error: Unable to host lobby");
            _hostForm.CloseForm();
        }
    }
    private void ShowListing(CustomLobby[] lobbies)
    {
        Debug.Log("[UILobbyManager] Show listing");

        foreach (UILobbyListingRecord record in _records)
        {
            Destroy(record.gameObject);
        }
        _records.Clear();

        if (lobbies == null || lobbies.Length == 0) return;


        foreach (CustomLobby lobby in lobbies)
        {
            UILobbyListingRecord record = Instantiate(_recordPf, _parentContent);
            record.gameObject.SetActive(true);

            string lobbyName = lobby.LobbyName;
            string playerAmt = lobby.CurrentPlayers + " / " + lobby.MaxPlayers;

            record.Initialize(this, lobby.LobbyID, lobbyName, playerAmt);
            _records.Add(record);
        }
    }

    private IEnumerator FirstTimeShowListing()
    {
        yield return new WaitUntil(() => MultiplayerFacade.Instance != null && MultiplayerFacade.Instance.ServiceController != null);
        yield return new WaitUntil(() => MultiplayerFacade.Instance.ServiceController.IsServiceRunning);

        _nextRefreshListingTime = Time.time + 0.1f;
    }

}

