using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Dreamonaut.Networking
{

    /// <summary>
    /// Unity has two methods for lobby callbacks which is via polling or event callback
    /// Current implementation uses event callbacks
    /// 
    /// Unity does snapshot for their lobby data where you get data of the lobby and have to apply manually
    /// Events can sometime be delayed and then pushed all at one call. E.g. player leave event can be triggered once for 2 players leaving
    /// </summary>
    public class UnityLobbyService : IServiceLobby
    {
        public CustomLobby Lobby => new CustomLobby(_currentLobby);
        public bool IsInLobby => _isInLobby;


        private Lobby _currentLobby = null;
        private bool _isInLobby = false;
        private Coroutine _heartBeatCoroutine;

        public event Action<CustomLobby> OnLobbyCreated;
        public event Action<CustomLobby> OnLobbyUpdate;
        public event Action<CustomLobby, LobbyPlayer> OnPlayerDataChange;
        public event Action<CustomLobby, LobbyPlayer> OnLobbyPlayerJoin;
        public event Action<CustomLobby, LobbyPlayer> OnPlayerLeave;
        public event Action<CustomLobby, LobbyPlayer> OnLobbyGetInvited; //  Not used in unity service
        public event Action<CustomLobby, LobbyPlayer> OnLobbyInviteFriend; //  Not used in unity service
        public event Action OnLobbyClosed;

        private ILobbyEvents _lobbyEvents;
        private LobbyEventCallbacks _eventCallbacks;

        // Making it an array such that list values can be copied into the array. Rather than reference
        // Done this because of callbacks will tell indicies of player leaving but not have a copy of the player info in the lobby data
        private Player[] _playersCache;
        private string _hostID;  // Cache host ID because lobby can auto change owner when host leaves

        // When a lobby is joined, it will take a while before callbacks are hookedup
        private async void SubscribeToCallbacks()
        {
            // TODO: To remove if event gets called in future already
            _ = OnLobbyGetInvited;
            _ = OnLobbyInviteFriend;
            //-----

            _eventCallbacks = new LobbyEventCallbacks();
            _eventCallbacks.LobbyChanged += OnLobbyUpdateHandler;
            _eventCallbacks.PlayerDataChanged += OnPlayerDataChangeHandler;
            _eventCallbacks.PlayerJoined += OnLobbyPlayerJoinHandler;
            _eventCallbacks.PlayerDataAdded += OnPlayerDataChangeHandler;

            // For troubleshooting purposes. Not hooked to external event triggers
            _eventCallbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChangedHandler;

            try
            {
                _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_currentLobby.Id, _eventCallbacks);
                await FetchLatestLobby();
            }
            catch (LobbyServiceException ex)
            {
                switch (ex.Reason)
                {
                    case LobbyExceptionReason.AlreadySubscribedToLobby: NetworkLog.LogDev($"[UnityLobby] ERROR: Already subscribed to lobby[{_currentLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                    case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy:  NetworkLog.LogDev($"[UnityLobby] ERROR: Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                    case LobbyExceptionReason.LobbyEventServiceConnectionError: NetworkLog.LogDev($"[UnityLobby] ERROR: Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                    default: throw;
                }
            }
        }

        // Ensure all events are unsubscribed otherwise it will cause error
        private void UnsubscribeToCallbacks()
        {
            if (_eventCallbacks == null) return;

            _eventCallbacks.LobbyChanged -= OnLobbyUpdateHandler;
            _eventCallbacks.PlayerDataChanged -= OnPlayerDataChangeHandler;
            _eventCallbacks.PlayerJoined -= OnLobbyPlayerJoinHandler;
            _eventCallbacks.PlayerDataAdded -= OnPlayerDataChangeHandler;
            _eventCallbacks.LobbyEventConnectionStateChanged -= OnLobbyEventConnectionStateChangedHandler;

            _eventCallbacks = null;
            _lobbyEvents.UnsubscribeAsync();
        }

        #region Lifecycle
        public void Init()
        {
        }


        public void Shutdown()
        {
            // In the case of force shutdown
            if (_currentLobby != null && _currentLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            }
            else if (_currentLobby != null)
            {
                LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }

            NetworkLog.LogDev("[UnityLobbyService] Shutdown");
        }

        public void Reset()
        {
            UnsubscribeToCallbacks();
            
            if (_currentLobby != null && _currentLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            }
            else if (_currentLobby != null)
            {
                LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }

            _currentLobby = null;
            _isInLobby = false;
            if (_heartBeatCoroutine != null)
            {
                MultiplayerFacade.Instance.StopCoroutine(_heartBeatCoroutine);
                _heartBeatCoroutine = null;
            }
            _lobbyEvents = null;
            _eventCallbacks = null;
            _playersCache = null;
            _hostID = null;

            NetworkLog.LogDev("[UnityLobbyService] Reset service");
        }

        #endregion

        #region Events

        private void OnLobbyCreatedHandler(Lobby lobby)
        {
            NetworkLog.LogDev("[UnityLobby] On Lobby Created: " + lobby.LobbyCode.ToString());

            _currentLobby = lobby;

            OnLobbyCreated?.Invoke(new CustomLobby(_currentLobby));


        }
        /// <summary>
        /// Everything that changes in the lobby such as player leave, lobby data change and etc will trigger this event first before other events
        /// 
        /// Change is applied first to lobby 
        /// </summary>
        /// <param name="changes"></param>
        private void OnLobbyUpdateHandler(ILobbyChanges changes)
        {
            NetworkLog.LogDev("[UnityLobby] Lobby update check!");
         
            changes.ApplyToLobby(_currentLobby);

            // For some odd reason player leave event is abit buggy when dealing with host leaving and so on. So manually called it here
            if (changes.PlayerLeft.Changed)
            {
                OnPlayerLeaveHandler(changes.PlayerLeft.Value);
            }

            //if (changes.PlayerData.Changed)
            //{
            //    Debug.Log($"----- changed data player");
            //    foreach(var change in changes.PlayerData.Value)
            //    {
            //        Debug.Log($"Key: {change.Key} Value: {change.Value} Value's Key: {change.Value.ChangedData.Value}");
            //    }
            //    Debug.Log($"---------------------------");

            //}

            // So far in testing lobby deleted was never called. But put it in just for safety measure
            else if (changes.LobbyDeleted)
            {
                OnLobbyClosedHandler();
            }
            else
            {
                NetworkLog.LogDev("[UnityLobby] Lobby Updated!");

                OnLobbyUpdate?.Invoke(new CustomLobby(_currentLobby));
            }

            _playersCache = _currentLobby.Players.ToArray();
        }

        /// <summary>
        /// Event called when player data changes. The outer dictionary is indexed on player indices. 
        /// The inner dictionary is indexed on the changed data key.
        /// </summary>
        /// <param name="playerChanges">Character Data Changes</param>
        private void OnPlayerDataChangeHandler(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerChanges)
        {
            NetworkLog.LogDev("[UnityLobby] Character Data Changed");

            // Update lobby cache with latest player data update
            foreach (var playerChange in playerChanges)
            {
                int playerIndex = playerChange.Key;
                var dataChanges = playerChange.Value;
                string playerID = _currentLobby.Players[playerIndex].Id;

                // Trigger a per-player event call 
                // Setting username as ID for simplicity sake. Refer to Model_LobbyPlayer header for more info on this

                OnPlayerDataChange?.Invoke(new CustomLobby(_currentLobby), new LobbyPlayer(playerID, playerID));
            }

        }

        /// <summary>
        /// Event called when a player join has occurred to a lobby on the server.
        /// </summary>
        /// <param name="playersJoined"></param>
        private void OnLobbyPlayerJoinHandler(List<LobbyPlayerJoined> playersJoined)
        {
            NetworkLog.LogDev("[UnityLobby] Players Join | " + playersJoined.Count);

            foreach (LobbyPlayerJoined player in playersJoined)
            {
                OnLobbyPlayerJoin?.Invoke(new CustomLobby(_currentLobby), new LobbyPlayer(player.Player.Id, player.Player.Id));
            }

        }

        /// <summary>
        /// Event called when a player leave has occurred to a lobby on the server. 
        /// it is called before ApplyChangesToLobby is done so can fetch player info that left
        /// 
        /// The indices of the players who left.
        /// </summary>
        /// <param name="playersLeft"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnPlayerLeaveHandler(List<int> playersLeft)
        {
            NetworkLog.LogDev("[UnityLobby] Players left | " + playersLeft.Count);

            foreach (int playerIndex in playersLeft)
            {
                Player player = _playersCache[playerIndex];

                if (_hostID == player.Id)
                {
                    OnPlayerLeave?.Invoke(new CustomLobby(_currentLobby), new LobbyPlayer(player.Id, player.Id));
                    OnLobbyClosedHandler();

                    return;
                }

                // Remove this player from the cached lobby
                OnPlayerLeave?.Invoke(new CustomLobby(_currentLobby), new LobbyPlayer(player.Id, player.Id));

            }

        }

        private async void OnLobbyClosedHandler()
        {
            NetworkLog.LogDev("[UnityLobby] Lobby closed");

            // Not host will have to leave game manually. Host will already have left the game to trigger this event
            if (_hostID != AuthenticationService.Instance.PlayerId)
            {
                await LeaveLobby();
            }

            OnLobbyClosed?.Invoke();
        }

        private void OnLobbyEventConnectionStateChangedHandler(LobbyEventConnectionState state)
        {
            NetworkLog.LogDev("[UnityLobby] Lobby Event CurrentState Changed: " + state);
        }
        #endregion

        public async Task<string> HostLobby(CustomLobbyConfig lobbyData)
        {
            Player player = new Player(AuthenticationService.Instance.PlayerId, null);

            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = lobbyData.LobbyType == CustomLobbyType.Private,
                Player = player,

                Data = new Dictionary<string, DataObject>
                {
                    { CustomLobby.KEY_LOBBY_NAME, new DataObject(DataObject.VisibilityOptions.Public, lobbyData.LobbyName, DataObject.IndexOptions.S1) },
                    { CustomLobby.KEY_GAME_NAME, new DataObject(DataObject.VisibilityOptions.Public, Application.productName, DataObject.IndexOptions.S2) },
                    { CustomLobby.KEY_CUSTOM_LOBBY_TYPE, new DataObject(DataObject.VisibilityOptions.Public, lobbyData.LobbyType.ToString(), DataObject.IndexOptions.S3) },
                    { CustomLobby.KEY_LOBBY_STATUS, new DataObject(DataObject.VisibilityOptions.Public, LobbyStatus.SETTING_UP.ToString(), DataObject.IndexOptions.S4) },
                    { CustomLobby.KEY_PLAYER_INSTANCE_COUNT, new DataObject(DataObject.VisibilityOptions.Public, lobbyData.PlayerInstanceCount.ToString(), DataObject.IndexOptions.N1) },
                    { CustomLobby.KEY_MAX_PLAYER, new DataObject(DataObject.VisibilityOptions.Public, lobbyData.MaxPlayer.ToString()) },

                }
            };

            try
            {
                _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyData.LobbyName.ToUpper(), lobbyData.MaxPlayer, options);
                _playersCache = _currentLobby.Players.ToArray();
                _hostID = _currentLobby.HostId;
                _isInLobby = true;

                SubscribeToCallbacks();

                // Uses lobby code as the ID for joining lobby
                UpdateLobbyOptions updateOptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {
                            CustomLobby.KEY_LOBBY_CODE, new DataObject(DataObject.VisibilityOptions.Public, _currentLobby.LobbyCode)
                        },
                        {
                            CustomLobby.KEY_PLAYER_INSTANCE_COUNT, new DataObject(DataObject.VisibilityOptions.Public,  _currentLobby.Data[CustomLobby.KEY_PLAYER_INSTANCE_COUNT].Value)

                        }
                    }
                };
                _currentLobby = await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, updateOptions);

                //_currentLobby.Data.Add(CustomLobby.KEY_LOBBY_CODE, new DataObject(DataObject.VisibilityOptions.Public, _currentLobby.LobbyCode));
            }
            catch (Exception e)
            {
                NetworkLog.LogDev($"[UnityLobby] Lobby failed to create: {e.Message}");
                return null;
            }

            NetworkLog.LogDev($"[UnityLobby] Lobby created with lobby code: {_currentLobby.LobbyCode}");

            _heartBeatCoroutine = MultiplayerFacade.Instance.StartCoroutine(HeartBeatLobbyCoroutine(_currentLobby.Id, 6f));
            OnLobbyCreatedHandler(_currentLobby);
            return _currentLobby.LobbyCode;
        }

        public async Task<LobbyEnterResult> JoinLobbyByID(string lobbyID)
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions();
            //Character player = new Character(AuthenticationService.Instance.PlayerId, null, SerializePlayerData(playerData));
            Player player = new Player(AuthenticationService.Instance.PlayerId, null);
            options.Player = player;

            try
            {

                _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID, options);
                _playersCache = _currentLobby.Players.ToArray();
                _hostID = _currentLobby.HostId;
                _isInLobby = true;

                NetworkLog.LogDev($"[UnityLobby] Lobby join : {lobbyID}");

                SubscribeToCallbacks();


            }
            catch (Exception e)
            {
                NetworkLog.LogDev($"[UnityLobby] Lobby join failed: {e.Message}");


                return LobbyEnterResult.Error;
            }

            return LobbyEnterResult.Success;
        }

        public async Task<LobbyEnterResult> JoinLobbyByLobbyCode(string lobbyCode)
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions();
            //Character player = new Character(AuthenticationService.Instance.PlayerId, null, SerializePlayerData(playerData));
            Player player = new Player(AuthenticationService.Instance.PlayerId, null);
            options.Player = player;

            try
            {

                _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
                _playersCache = _currentLobby.Players.ToArray();
                _hostID = _currentLobby.HostId;
                _isInLobby = true;

                NetworkLog.LogDev($"[UnityLobby] Lobby join : {lobbyCode}");

                SubscribeToCallbacks();
            }
            catch (Exception e)
            {
                NetworkLog.LogDev($"[UnityLobby] Lobby join failed: {e.Message}");


                return LobbyEnterResult.Error;
            }

            return LobbyEnterResult.Success;
        }

        public async Task<bool> LeaveLobby()
        {
            if (_currentLobby == null)
            {
                NetworkLog.LogDev("[UnityLobby] Unable to leave. Not in any lobby currently");

                return false;
            }

            try
            {
                UnsubscribeToCallbacks();

                // Check if host
                if (_currentLobby.Id == AuthenticationService.Instance.PlayerId)
                {
                    // If you are host, stop heartbeat. Only host has the coroutine up
                    if (_heartBeatCoroutine != null)
                        MultiplayerFacade.Instance.StopCoroutine(_heartBeatCoroutine);

                    await UpdateLobbyData(_currentLobby.Id,
                        new Dictionary<string, string>() { { CustomLobby.KEY_LOBBY_STATUS, LobbyStatus.INACTIVE.ToString() } });

                    await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }

                NetworkLog.LogDev($"[UnityLobby] Lobby left");

                _isInLobby = false;
                _currentLobby = null;

                return true;
            }
            catch (Exception e)
            {
                NetworkLog.LogDev("[UnityLobby] Unable to leave lobby: " + e.Message);
                return false;
            }
        }

        public async Task<CustomLobby[]> GetAvailableLobbyByName(string name)
        {
            try
            {
                var queryOptions = new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter>
                    {
                        // (Optional) only public lobbies
                        new QueryFilter(
                            field: QueryFilter.FieldOptions.IsLocked,
                            op: QueryFilter.OpOptions.EQ,
                            value: "false"
                        ),
                        //new QueryFilter(
                        //    field: QueryFilter.FieldOptions.S4,
                        //    op: QueryFilter.OpOptions.EQ,
                        //    value: LobbyStatus.ACTIVE.ToString()
                        //),
                        new QueryFilter(
                            field: QueryFilter.FieldOptions.Name, // S1 corresponds to your first string data field
                            op: QueryFilter.OpOptions.CONTAINS, // Partial match allowed
                            value: name.ToUpper()
                        ),
                    },
                    Count = 20
                };

                QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);


                return CustomLobby.ParseUnityLobbies(response.Results.ToArray());
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Lobby query failed: {e}");
                return null;
            }
        }

        public async Task<CustomLobby[]> GetAvailableLobbyList()
        {
            try
            {
                var queryOptions = new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter>
                    {
                        // (Optional) only public lobbies
                        new QueryFilter(
                            field: QueryFilter.FieldOptions.IsLocked,
                            op: QueryFilter.OpOptions.EQ,
                            value: "false"
                        ),

                    },

                };

                QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

                var lobbies = response.Results.Where(l => l.Data[CustomLobby.KEY_LOBBY_STATUS].Value == LobbyStatus.ACTIVE.ToString()).ToArray();

                return CustomLobby.ParseUnityLobbies(lobbies);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Lobby query failed: {e}");
                return null;
            }
        }

        /// <summary>
        /// Unity lobby data is snapshot. So need to force update first
        /// </summary>
        /// <returns></returns>
        public async Task<CustomLobby> FetchLatestLobby()
        {
            try
            {
                NetworkLog.LogDev("[UnityLobby] Fetch latest lobby");
                _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);

                return new CustomLobby(_currentLobby);
            }
            catch (LobbyServiceException e)
            {
                NetworkLog.LogError("[UnityLobby] ERROR: Fetching latest lobby failed: " + e.Message);
                return new CustomLobby(_currentLobby);
            }

        }


        public async Task<bool> UpdateLobbyData(string lobbyID, Dictionary<string, string> data)
        {
            Dictionary<string, DataObject> updateDict = new Dictionary<string, DataObject>();

            foreach (KeyValuePair<string,string> kvp in data)
            {
                if (data.ContainsKey(kvp.Key))
                {
                    updateDict.Add(kvp.Key, new DataObject(DataObject.VisibilityOptions.Public, kvp.Value));
                }

            }

            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions
            {
                Data = updateDict
            };

            try
            {
                _currentLobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyID, updateOptions);
                return true;
            }
            catch (LobbyServiceException e)
            {
                NetworkLog.LogError("[UnityLobby] ERROR: Unable to update lobby data: " + e.Message);
                return false;
            }
        }


        public Task<string> GetPlayerData(string playerID, string dataKey)
        {
            Player player =  _currentLobby.Players.FirstOrDefault(p => p.Id == playerID);

            if (player.Data == null || player.Data[dataKey] == null)
                return Task.FromResult<string>(null);
            else
                return Task.FromResult(player.Data[dataKey].Value);
        }

        /// <summary>
        /// Warning: Callback for updating player data for ownself will not be triggered on the sender
        /// </summary>
        /// <param name="lobbyID"></param>
        /// <param name="playerID"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> UpdatePlayerData(string lobbyID, string playerID, Dictionary<string, string> data)
        {
            try
            {
                // Create the new player data you want to send
                Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>();
                foreach (KeyValuePair<string, string> kvp in data)
                {
                    playerData.Add(kvp.Key, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, kvp.Value));
                }

                // Apply the update to the player
                await LobbyService.Instance.UpdatePlayerAsync(lobbyID, playerID, new UpdatePlayerOptions
                {
                    Data = playerData,
                });


                NetworkLog.LogDev("[UnityLobby] Updated data for player: " + playerID);

                return true;
            }
            catch (Exception e)
            {
                NetworkLog.LogError("[UnityLobby] ERROR: " + e.Message);
                return false;
            }
        }

        public Task<bool> OpenLobbyUpForFriendInvite()
        {
            return Task.FromResult(false);
        }

        public async Task<bool> HostUpdateLobbyStatus(LobbyStatus lobbyStatus)
        {
            if (_hostID == AuthenticationService.Instance.PlayerId)
            {
                NetworkLog.LogDev("[SteamLobby] Updating lobby status to: " + lobbyStatus);
                Dictionary<string, string> data = new Dictionary<string, string> { { CustomLobby.KEY_LOBBY_STATUS, lobbyStatus.ToString() } };
                await UpdateLobbyData(_currentLobby.Id.ToString(), data);

                return true;
            }
            else return false;
        }


        public Task TestFunction()
        {
            PrintLobbyData();

            //Debug.Log("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
            //Debug.Log("Version: " + _currentLobby.Version);

            //foreach (var player in _currentLobby.Players)
            //{
            //    Debug.Log("-----------------------");
            //    Debug.Log("Character: " + player.Id);

            //    if (player.Data == null)
            //    {
            //        Debug.Log("Data None");
            //        continue;
            //    }
            //    foreach(var data in player.Data)
            //    {
            //        Debug.Log(data.Key + " | " + data.Value.Value);
            //    }
            //    Debug.Log("-----------------------");
            //}
            return Task.CompletedTask;
        }


        public void PrintLobbyData()
        {
            if (_currentLobby == null)
            {
                NetworkLog.LogDev("[UnityLobby] No lobby joined. No data");
            }
            else
            {
                NetworkLog.LogDev("-------- [UNITY LOBBY DATA] ---------");
                NetworkLog.LogDev($"Member Count: {_currentLobby.Players.Count}");

                foreach (KeyValuePair<string, DataObject> data in _currentLobby.Data)
                {
                    NetworkLog.LogDev($"{data.Key} | {data.Value.Value} | {data.Value.Index}");
                }
                NetworkLog.LogDev("------------------------------------");
            }
        }

        

        private IEnumerator HeartBeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
        {
            while (_currentLobby != null && _currentLobby.Id == lobbyId)
            {
                LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);

                yield return new WaitForSecondsRealtime(waitTimeSeconds);

            }

        }

    }
}