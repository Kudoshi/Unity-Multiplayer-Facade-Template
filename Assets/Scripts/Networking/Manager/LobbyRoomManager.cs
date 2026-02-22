
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dreamonaut.Networking.Sample
{

    /// <summary>
    /// This is part of the sample. Not in use in actual one
    /// 
    /// The logic side of the room manager. Connects to the multiplayer service system and pings the UI to display stuff
    /// 
    /// Current flow for starting game:
    /// 1. Players join lobby
    /// 2. All players ready up
    /// 3. Countdown timer for game start begins
    ///     3a. If new player joins - Countdown stops and all player unready
    ///     3b. If player disconnects - Countdown stops and all player unready
    /// 3. When countdown timer ends, host will start the relay service and broadcast the relay code to others
    /// 4. Other players will get the code and join the relay
    /// 5. Netcode will change scene of the players to the game scene
    /// </summary>
    public class LobbyRoomManager : MonoBehaviour
    {
        [SerializeField] private UILobbyRoomManager _uiManager;
        [SerializeField] private string _gameScene;
        [SerializeField] private string _lobbyListScene;


        private CustomLobby _lobby;
        private Dictionary<string, bool> _playerReadyDict = new Dictionary<string, bool>();


        private void OnEnable()
        {
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyCreated += OnLobbyCreatedHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyUpdate += OnLobbyUpdateHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnPlayerDataChange += OnPlayerDataChangeHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyPlayerJoin += OnPlayerJoinHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnPlayerLeave += OnPlayerLeaveHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyGetInvited += OnLobbyInviteHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyClosed += OnLobbyClosedHandler;

        }

        private void OnDisable()
        {
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyCreated -= OnLobbyCreatedHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyUpdate -= OnLobbyUpdateHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnPlayerDataChange -= OnPlayerDataChangeHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyPlayerJoin -= OnPlayerJoinHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnPlayerLeave -= OnPlayerLeaveHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyGetInvited -= OnLobbyInviteHandler;
            MultiplayerFacade.Instance.ServiceLobby.OnLobbyClosed -= OnLobbyClosedHandler;
        }

        //private void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.C)) MultiplayerFacade.Instance.ServiceLobby.TestFunction();
        //}

        #region Events
        private void OnLobbyCreatedHandler(CustomLobby lobby)
        {
            _lobby = lobby;
            //Debug.Log("[LobbyRoomManager] Lobby Created");
        }

        private void OnLobbyUpdateHandler(CustomLobby lobby)
        {
            _lobby = lobby;
            Client_CheckStartGameData();
            //Debug.Log("[LobbyRoomManager] Lobby Updated");
        }

        private void OnPlayerDataChangeHandler(CustomLobby lobby, LobbyPlayer player)
        {
            _lobby = lobby;
            UpdatePlayerReadyState(player.Id);
            CheckShouldStartGame();
            //Debug.Log("[LobbyRoomManager] Character data changed: " + player.Name);
        }

        private void OnPlayerJoinHandler(CustomLobby lobby, LobbyPlayer player)
        {
            _lobby = lobby;
            AddPlayer(player);
            //Debug.Log("[LobbyRoomManager] Character joined: " + player.Name);
        }

        private void OnPlayerLeaveHandler(CustomLobby lobby, LobbyPlayer player)
        {
            _lobby = lobby;
            RemovePlayer(player);
            //Debug.Log("[LobbyRoomManager] Character left: " + player.Name);
        }

        // Technically not in use for unity services
        private void OnLobbyInviteHandler(CustomLobby lobby, LobbyPlayer player)
        {
            _lobby = lobby;
            //Debug.Log("[LobbyRoomManager] Character invited: " + player.Name);
        }

        private void OnLobbyClosedHandler()
        {
            SceneManager.LoadScene(_lobbyListScene);
        }

        #endregion

        private void Start()
        {
            InitializeLobbyData();
        }

        public void LeaveLobby()
        {
            MultiplayerFacade.Instance.ServiceLobby.LeaveLobby();
            SceneManager.LoadScene(_lobbyListScene);
            //Debug.Log("[LRoomManager] Leave");
        }

        public async void PushReadyUp(bool isReady)
        {
            string ownPlayerID = MultiplayerFacade.Instance.ServiceController.PlayerID;

            // Client update ready button text and update data
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add(LobbyPlayer.KEY_PLAYER_READY, isReady.ToString());
            bool success = await MultiplayerFacade.Instance.ServiceLobby.UpdatePlayerData(_lobby.LobbyID, ownPlayerID, data);

            if (!success) Debug.LogError("[LobbyRoomManager] Unable to ready up");
        }

        public void InviteFriend()
        {
            MultiplayerFacade.Instance.ServiceLobby.OpenLobbyUpForFriendInvite();
        }

        public async void Server_StartGame()
        {
            // If is owner of the lobby - Start relay and broadcast
            if (_lobby.LobbyOwnerID == MultiplayerFacade.Instance.ServiceController.PlayerID)
            {
                string relayID = await MultiplayerFacade.Instance.ServiceRelay.HostRelay();

                bool connected = await MultiplayerFacade.Instance.ServiceLobby.UpdateLobbyData(_lobby.LobbyID,
                    new Dictionary<string, string>() { { CustomLobby.KEY_START_GAME, relayID } });

                if (connected)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(_gameScene, LoadSceneMode.Single);
                }
            }
        }

        private void InitializeLobbyData()
        {
            _lobby = MultiplayerFacade.Instance.ServiceLobby.Lobby;

            _uiManager.InitializeLobbyDisplay(_lobby.LobbyName, _lobby.Data[CustomLobby.KEY_LOBBY_CODE], _lobby.Players);

            // If is host -> update lobby status to active
            if (_lobby.LobbyOwnerID == MultiplayerFacade.Instance.ServiceController.PlayerID)
                MultiplayerFacade.Instance.ServiceLobby.HostUpdateLobbyStatus(LobbyStatus.ACTIVE);


            if (MultiplayerFacade.Instance.ServiceType == MultiplayerServiceType.STEAM && _lobby.LobbyOwnerID == MultiplayerFacade.Instance.ServiceController.PlayerID)
            {
                _uiManager.EnableInviteFriendsButton(true);
            }
            else
            {
                _uiManager.EnableInviteFriendsButton(false);
            }

            foreach (LobbyPlayer player in _lobby.Players)
            {
                _uiManager.UpdateSlotReadyState(player.Id, false);
                _playerReadyDict.Add(player.Id, false);
            }
        }

        private void AddPlayer(LobbyPlayer player)
        {
            _uiManager.AddPlayerSlot(player);
            _playerReadyDict.Add(player.Id, false);

            UpdatePlayerReadyState(player.Id);
            ResetOwnReady();
        }

        private void ResetOwnReady()
        {
            UpdatePlayerReadyState(MultiplayerFacade.Instance.ServiceController.PlayerID);
            PushReadyUp(false);
            _uiManager.StopCrStartGameCountdown();

        }

        private void RemovePlayer(LobbyPlayer player)
        {
            _uiManager.RemovePlayerSlot(player);

            _playerReadyDict.Remove(player.Id);
            ResetOwnReady();
        }

        private async void UpdatePlayerReadyState(string playerID)
        {
            string readyString = await MultiplayerFacade.Instance.ServiceLobby.GetPlayerData(playerID, LobbyPlayer.KEY_PLAYER_READY);

            bool ready;

            if (readyString == null) ready = false;
            else ready = bool.Parse(readyString);

            _playerReadyDict[playerID] = ready;

            _uiManager.UpdateSlotReadyState(playerID, ready);
        }

        private void CheckShouldStartGame()
        {
            bool shouldStartGame = true;

            foreach (bool playerReady in _playerReadyDict.Values)
            {
                if (!playerReady)
                {
                    shouldStartGame = false;
                    break;
                }
            }

            if (shouldStartGame)
            {
                _uiManager.TriggerStartGameCountdown();
            }

        }

        private async void Client_CheckStartGameData()
        {

            if (_lobby.Data == null || !_lobby.Data.ContainsKey(CustomLobby.KEY_START_GAME) || _lobby.LobbyOwnerID == MultiplayerFacade.Instance.ServiceController.PlayerID)
            {
                return;
            }

            string relayCode = _lobby.Data[CustomLobby.KEY_START_GAME];

            if (relayCode != null)
            {
                await MultiplayerFacade.Instance.ServiceRelay.JoinRelay(relayCode);
            }
        }
    }
}