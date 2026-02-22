
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Dreamonaut.Networking;
using Dreamonaut.Networking.Sample;

/// <summary>
/// UI side that controls the lobby room
/// 
/// Most of the logic are done on the LobbyRoommanager. This script merely handles the frontend
/// </summary>
public class UILobbyRoomManager : MonoBehaviour
{

    [SerializeField] private LobbyRoomManager _lobbyRoomManager;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI _textServer;
    [SerializeField] private TextMeshProUGUI _textLobbyCode;
    [SerializeField] private TextMeshProUGUI _textStartGame;
    [SerializeField] private Transform _playerSlotsContainer;
    [SerializeField] private UIRoomPlayerSlot _pfPlayerSlot;
    [SerializeField] private GameObject _container;
    [SerializeField] private Button _btnLeave;
    [SerializeField] private Button _btnCopyLobbyCode;
    [SerializeField] private Button _btnReady;
    [SerializeField] private Button _btnInviteFriends;

    private Dictionary<string, UIRoomPlayerSlot> _playerSlots = new Dictionary<string, UIRoomPlayerSlot>();
    private bool _hostReady = false;
    private TextMeshProUGUI _textReadyBtn;
    private Coroutine _crCountdownStartGame;

    private void Awake()
    {
        _btnLeave.onClick.AddListener(Btn_Leave);
        _btnCopyLobbyCode.onClick.AddListener(Btn_CopyLobbyCode);
        _btnReady.onClick.AddListener(Btn_Ready);
        _btnInviteFriends.onClick.AddListener(Btn_InviteFriend);

        _textReadyBtn = _btnReady.GetComponentInChildren<TextMeshProUGUI>();
        _textStartGame.text = "Ready up to start the game";
    }

    #region Buttons
    private void Btn_Leave()
    {
        _lobbyRoomManager.LeaveLobby();
    }

    private void Btn_CopyLobbyCode()
    {
        UnityEngine.TextEditor textEditor = new UnityEngine.TextEditor();
        textEditor.text = _textLobbyCode.text;
        textEditor.SelectAll();
        textEditor.Copy();
    }

    private void Btn_Ready()
    {
        string ownPlayerID = MultiplayerFacade.Instance.ServiceController.PlayerID;

        _hostReady = !_hostReady;

        _lobbyRoomManager.PushReadyUp(_hostReady);

        if (_hostReady)
        {
            _textReadyBtn.text = "Unready";
        }
        else
        {
            _textReadyBtn.text = "Ready Up";
            StopCrStartGameCountdown();
        }
    }

    private void Btn_InviteFriend()
    {
        _lobbyRoomManager.InviteFriend();
    }
    #endregion

    #region Events
    

    #endregion

    public void InitializeLobbyDisplay(string lobbyName, string lobbyCode, LobbyPlayer[] players)
    {
        _textServer.text = lobbyName;
        _textLobbyCode.text = lobbyCode;

        // Create new player slots
        foreach (LobbyPlayer player in players)
        {
            if (_playerSlots.ContainsKey(player.Id)) continue;

            AddPlayerSlot(player);
        }
    }

    public void AddPlayerSlot(LobbyPlayer player)
    {
        if (_playerSlots.ContainsKey(player.Id))
        {
            Debug.Log("[UILRoomManager] Error: Unable to add player slot. Already exist");
            return;
        }

        UIRoomPlayerSlot slot = Instantiate(_pfPlayerSlot, _playerSlotsContainer);
        slot.Initialize(player.Id, player.Name);
        _playerSlots.Add(player.Id, slot);
    }

    public void RemovePlayerSlot(LobbyPlayer player)
    {
        if (!_playerSlots.ContainsKey(player.Id))
        {
            Debug.Log("[UILRoomManager] Error: Unable to remove player slot. Does not exist");
            return;
        }

        Destroy(_playerSlots[player.Id].gameObject);
        _playerSlots.Remove(player.Id);
    }

    public void UpdateSlotReadyState(string playerID, bool isReady)
    {
        StopCrStartGameCountdown();

        _playerSlots[playerID].SetReady(isReady);

        if (playerID == MultiplayerFacade.Instance.ServiceController.PlayerID)
        {
            _hostReady = isReady;
            if (_hostReady)
            {
                _textReadyBtn.text = "Unready";
            }
            else _textReadyBtn.text = "Ready Up";
        }
    }

    public void TriggerStartGameCountdown()
    {
        if (_crCountdownStartGame != null)
        {
            StopCoroutine(_crCountdownStartGame);
            _crCountdownStartGame = null;
        }

        _crCountdownStartGame = StartCoroutine(Cr_StartGameCountdown());
    }

    public void EnableInviteFriendsButton(bool enable)
    {
        _btnInviteFriends.gameObject.SetActive(enable);
    }
    public void StopCrStartGameCountdown()
    {
        // If there is changes in ready state - Stop start game Coroutine
        if (_crCountdownStartGame != null)
        {
            StopCoroutine(_crCountdownStartGame);
            _crCountdownStartGame = null;
            _textStartGame.text = "Ready up to start the game";
        }

        //foreach(UIRoomPlayerSlot slot in _playerSlots.Values)
        //{
        //    slot.SetReady(false);
        //}
    }

    private IEnumerator Cr_StartGameCountdown()
    {
        _textStartGame.text = "Game starting in 3..";

        yield return new WaitForSeconds(1);

        _textStartGame.text = "Game starting in 2..";

        yield return new WaitForSeconds(1);

        _textStartGame.text = "Game starting in 1..";

        yield return new WaitForSeconds(1);

        _textStartGame.text = "Game starting in 0..";
        _lobbyRoomManager.Server_StartGame();
    }
}