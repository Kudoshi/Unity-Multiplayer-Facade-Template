using Dreamonaut.Networking;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dreamonaut.Networking
{

    /// <summary>a
    /// Steam uses event based callbacks and has auto live update to the lobby
    /// 
    /// Perks:
    ///     - One machine can only have 1 instance of game as it uses the PC's Steam and steam account.
    ///     - You need to use different machine to test out the lobby
    /// </summary>
    public class SteamLobbyService : IServiceLobby
    {
        public CustomLobby Lobby => new CustomLobby(_currentLobby.Value);
        public bool IsInLobby => _isInLobby;

        private Lobby? _currentLobby = null;
        private bool _isInLobby = false;

        public event Action<CustomLobby> OnLobbyCreated;
        public event Action<CustomLobby> OnLobbyUpdate;
        public event Action<CustomLobby, LobbyPlayer> OnPlayerDataChange;
        public event Action<CustomLobby, LobbyPlayer> OnLobbyPlayerJoin;
        public event Action<CustomLobby, LobbyPlayer> OnPlayerLeave;
        public event Action<CustomLobby, LobbyPlayer> OnLobbyInviteFriend;
        public event Action<CustomLobby, LobbyPlayer> OnLobbyGetInvited;
        public event Action OnLobbyClosed;

        private string _hostID; // Cache host ID because lobby can auto change owner when host leaves

        #region Lifecycle
        public void Init()
        {
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedHandler;
            SteamMatchmaking.OnLobbyDataChanged += OnLobbyUpdateHandler;
            SteamMatchmaking.OnLobbyMemberDataChanged += OnPlayerDataChangeHandler;

            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyPlayerJoinHandler;
            SteamMatchmaking.OnLobbyMemberLeave += OnPlayerLeaveHandler;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnPlayerLeaveHandler;
            SteamMatchmaking.OnLobbyInvite += OnLobbyInviteHandler;

            SteamFriends.OnGameLobbyJoinRequested += OnFriendLobbyJoinRequestHandler;
        }
        public void Shutdown()
        {
            if (_currentLobby.HasValue)
            {
                _currentLobby.Value.Leave();
            }

            NetworkLog.LogDev("[SteamLobbyService] Shutdown");

        }

        public void Reset()
        {
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreatedHandler;
            SteamMatchmaking.OnLobbyDataChanged -= OnLobbyUpdateHandler;
            SteamMatchmaking.OnLobbyMemberDataChanged -= OnPlayerDataChangeHandler;

            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyPlayerJoinHandler;
            SteamMatchmaking.OnLobbyMemberLeave -= OnPlayerLeaveHandler;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnPlayerLeaveHandler;
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInviteHandler;

            SteamFriends.OnGameLobbyJoinRequested -= OnFriendLobbyJoinRequestHandler;

            if (_currentLobby.HasValue)
            {
                _currentLobby.Value.Leave();
                Debug.Log("[SteamLobbyService] Left current lobby during reset");
            }

            _currentLobby = null;
            _isInLobby = false;

            NetworkLog.LogDev("[SteamLobbyService] Reset service");

        }


        #endregion

        #region Events
        private void OnLobbyCreatedHandler(Result result, Lobby lobby)
        {
            if (result == Result.OK)
            {
                if (lobby.GetData(CustomLobby.KEY_CUSTOM_LOBBY_TYPE) == CustomLobbyType.Public.ToString())
                {
                    lobby.SetPublic();
                }

                lobby.SetJoinable(true);
                _currentLobby = lobby;
                NetworkLog.LogDev("[SteamLobby] On Lobby Created: " + lobby.Id.ToString());
                OnLobbyCreated?.Invoke(new CustomLobby(_currentLobby.Value));
            }
            else
            {
                NetworkLog.LogDev("[SteamLobby] <ERROR> On Lobby Created: " + lobby.Id.ToString() + " | Error: " + result.ToString());
            }
        }

        private void OnLobbyUpdateHandler(Lobby lobby)
        {
            _currentLobby = lobby;

            OnLobbyUpdate?.Invoke(new CustomLobby(_currentLobby.Value));
            NetworkLog.LogDev("[SteamLobby] Lobby data changed!");

        }

        private void OnPlayerDataChangeHandler(Lobby lobby, Friend friend)
        {
            _currentLobby = lobby;
            OnPlayerDataChange?.Invoke(new CustomLobby(_currentLobby.Value), new LobbyPlayer(friend));

            NetworkLog.LogDev("[SteamLobby] Character data changed | Character: " + friend.Name);

        }
        private void OnLobbyPlayerJoinHandler(Lobby lobby, Friend friend)
        {
            _currentLobby = lobby;
            OnLobbyPlayerJoin?.Invoke(new CustomLobby(_currentLobby.Value), new LobbyPlayer(friend));
            NetworkLog.LogDev("[SteamLobby] Character Join lobby | Character: " + friend.Name);
        }

        private async void OnPlayerLeaveHandler(Lobby lobby, Friend friend)
        {
            _currentLobby = lobby;

            OnPlayerLeave?.Invoke(new CustomLobby(_currentLobby.Value), new LobbyPlayer(friend));

            // Host leaves the game -> Boots everyone off
            if (_hostID == friend.Id.ToString())
            {
                await OnLobbyClosedHandler();
            }

            NetworkLog.LogDev($"[SteamLobby] Character left lobby | Character: {friend.Name}");
        }

        private async Task OnLobbyClosedHandler()
        {
            NetworkLog.LogDev("[SteamLobby] Lobby closed");

            // Not host will have to leave game manually. Host will already have left the game to trigger this event
            if (_currentLobby.Value.Owner.Id != SteamClient.SteamId)
            {
                await LeaveLobby();
            }

            OnLobbyClosed?.Invoke();
        }

        /// <summary>
        /// Triggers when inviting friend to join lobby
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="lobby"></param>
        private void OnLobbyInviteHandler(Friend friend, Lobby lobby)
        {
            _currentLobby = lobby;

            OnLobbyInviteFriend?.Invoke(new CustomLobby(_currentLobby.Value), new LobbyPlayer(friend));
            NetworkLog.LogDev($"[SteamLobby] Character invited to lobby | Character: {friend.Name}");

        }

        /// <summary>
        /// Triggers when asking friend to join their lobby
        /// </summary>
        /// <param name="lobby"></param>
        /// <param name="id"></param>
        private async void OnFriendLobbyJoinRequestHandler(Lobby lobby, SteamId id)
        {
            NetworkLog.LogDev("[SteamLobby] On Friend request join " + lobby.Id.ToString() + " By SteamID: " + id);

            await lobby.Join();
            SceneManager.LoadScene(GameConstantVariables.LOBBY_SCENE_NAME);
            OnLobbyGetInvited?.Invoke(new CustomLobby(lobby), new LobbyPlayer(SteamClient.SteamId.ToString(), SteamClient.Name));

            NetworkLog.LogDev("[SteamLobby] Lobby Joined");

        }


        #endregion



        public async Task<string> HostLobby(CustomLobbyConfig lobbyData)
        {
            Lobby? lobby = (Lobby)await SteamMatchmaking.CreateLobbyAsync(lobbyData.MaxPlayer);

            if (lobby == null)
            {
                NetworkLog.LogDev("[SteamLobby] Lobby could not be created");
                return null;
            }
            else
            {
                lobby.Value.SetData(CustomLobby.KEY_LOBBY_NAME, lobbyData.LobbyName);
                lobby.Value.SetData(CustomLobby.KEY_MAX_PLAYER, lobbyData.MaxPlayer.ToString());
                lobby.Value.SetData(CustomLobby.KEY_PLAYER_INSTANCE_COUNT, lobbyData.PlayerInstanceCount.ToString());
                lobby.Value.SetData(CustomLobby.KEY_GAME_NAME, Application.productName);
                lobby.Value.SetData(CustomLobby.KEY_CUSTOM_LOBBY_TYPE, lobbyData.LobbyType.ToString());
                lobby.Value.SetData(CustomLobby.KEY_LOBBY_CODE, lobby.Value.Id.ToString());
                lobby.Value.SetData(CustomLobby.KEY_LOBBY_STATUS, LobbyStatus.ACTIVE.ToString());

                NetworkLog.LogDev("[SteamLobby] Lobby hosted");

                _currentLobby = lobby;
                _hostID = lobby.Value.Owner.Id.ToString();
                _isInLobby = true;

                return lobby.Value.Id.ToString();
            }
        }

        public async Task<LobbyEnterResult> JoinLobbyByID(string lobbyID)
        {
            //string gameName = GameName

            Lobby[] lobbies = await
                SteamMatchmaking.LobbyList
                    .WithKeyValue(CustomLobby.KEY_LOBBY_CODE, lobbyID)
                    .FilterDistanceWorldwide()
                    .WithSlotsAvailable(1).RequestAsync();

            if (lobbies == null || lobbies.Length == 0)
            {
                NetworkLog.LogDev("[SteamLobby] Unable to find lobby: " + lobbyID);
                return LobbyEnterResult.DoesntExist;
            }

            if (lobbies.Length > 2)
            {
                NetworkLog.LogDev("[SteamLobby] <WARNING> More than 1 lobby found with the ID: " + lobbyID);
            }

            RoomEnter roomResult = await lobbies[0].Join();
            _hostID = lobbies[0].Owner.Id.ToString();

            if (roomResult == RoomEnter.Success)
            {
                _isInLobby = true;
                _currentLobby = lobbies[0];
                NetworkLog.LogDev("[SteamLobby] Lobby Joined");

            }
            else
                NetworkLog.LogDev("[SteamLobby] <ERROR> Join Lobby Error: " + roomResult.ToString());


            return Enum.Parse<LobbyEnterResult>(roomResult.ToString());

        }

        /// <summary>
        /// Steam uses Lobby ID to join. So for steam, lobby code uses lobby ID
        /// </summary>
        /// <param name="lobbyCode"></param>
        /// <returns></returns>
        public async Task<LobbyEnterResult> JoinLobbyByLobbyCode(string lobbyCode)
        {
            return await JoinLobbyByID(lobbyCode);
        }
        public Task<bool> LeaveLobby()
        {
            if (_currentLobby == null)
            {
                NetworkLog.LogDev("[SteamLobby] No lobby joined. Unable to leave");
                _currentLobby = null;
                return Task.FromResult(false);
            }

            _currentLobby.Value.SetData(CustomLobby.KEY_LOBBY_STATUS, LobbyStatus.INACTIVE.ToString());
            _currentLobby.Value.Leave();
            NetworkLog.LogDev("[SteamLobby] Left lobby");
            _currentLobby = null;
            _isInLobby = false;

            return Task.FromResult(true);
        }
        public async Task<CustomLobby[]> GetAvailableLobbyByName(string name)
        {
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithKeyValue(CustomLobby.KEY_GAME_NAME, Application.productName)
              .WithKeyValue(CustomLobby.KEY_CUSTOM_LOBBY_TYPE, CustomLobbyType.Public.ToString())
              .WithKeyValue(CustomLobby.KEY_LOBBY_STATUS, LobbyStatus.ACTIVE.ToString())
              .FilterDistanceWorldwide()
              .RequestAsync();

            if (lobbies == null || lobbies.Length == 0) return null;

            List<Lobby> searchLobbies = new List<Lobby>();
            foreach (Lobby lobby in lobbies)
            {
                string lobbyName = lobby.GetData(CustomLobby.KEY_LOBBY_NAME).ToUpper();
                if (lobbyName.Contains(name.ToUpper()))
                {
                    searchLobbies.Add(lobby);
                }
            }

            return CustomLobby.ParseSteamLobbies(searchLobbies.ToArray());
        }
        public async Task<CustomLobby[]> GetAvailableLobbyList()
        {
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithKeyValue(CustomLobby.KEY_GAME_NAME, Application.productName)
                .WithKeyValue(CustomLobby.KEY_CUSTOM_LOBBY_TYPE, CustomLobbyType.Public.ToString())
                .WithKeyValue(CustomLobby.KEY_LOBBY_STATUS, LobbyStatus.ACTIVE.ToString())
                .FilterDistanceWorldwide()
                .RequestAsync();

            if (lobbies == null || lobbies.Length == 0) return null;

            return CustomLobby.ParseSteamLobbies(lobbies);
        }

        public Task<CustomLobby> FetchLatestLobby()
        {
            return Task.FromResult(new CustomLobby(_currentLobby.Value));
        }


        public Task<bool> UpdateLobbyData(string lobbyID, Dictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> kvp in data)
            {
                _currentLobby.Value.SetData(kvp.Key, kvp.Value);
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets player data
        /// </summary>
        /// <param name="playerID">Character ID</param>
        /// <param name="dataKey">Data Key</param>
        /// <returns>Data Value OR null if no data found</returns>
        public Task<string> GetPlayerData(string playerID, string dataKey)
        {
            Friend player = _currentLobby.Value.Members.FirstOrDefault(p => p.Id.Value.ToString() == playerID);

            string data = _currentLobby.Value.GetMemberData(player, dataKey);

            if (data == "")
            {
                return Task.FromResult<string>(null);
            }
            else
            {
                return Task.FromResult<string>(data);
            }

        }



        public Task<bool> UpdatePlayerData(string lobbyID, string playerID, Dictionary<string, string> data)
        {
            try
            {
                if (_currentLobby.Value.Id.ToString() != lobbyID)
                {
                    throw new Exception("Given lobby to update does not match current lobby");
                }

                foreach (KeyValuePair<string, string> kvp in data)
                {
                    _currentLobby.Value.SetMemberData(kvp.Key, kvp.Value);

                }

                NetworkLog.LogDev("[UnityLobby] Updated data for player: " + playerID);

                return Task.FromResult(true); ;
            }
            catch (Exception e)
            {
                NetworkLog.LogError("[SteamLobby] ERROR: " + e.Message);
                return Task.FromResult(false);
            }
        }

        public Task<bool> OpenLobbyUpForFriendInvite()
        {
            if (_currentLobby.HasValue)
            {
                NetworkLog.LogDev("[UnityLobby] Opening lobby up for friend invite");
                SteamFriends.OpenGameInviteOverlay(_currentLobby.Value.Id);
                return Task.FromResult(true);
            }
            else return Task.FromResult(false);
        }

        public async Task<bool> HostUpdateLobbyStatus(LobbyStatus lobbyStatus)
        {
            if (_hostID == SteamClient.SteamId.ToString())
            {
                NetworkLog.LogDev("[SteamLobby] Updating lobby status to: " + lobbyStatus);
                Dictionary<string, string> data = new Dictionary<string, string> { { CustomLobby.KEY_LOBBY_STATUS, lobbyStatus.ToString() } };
                await UpdateLobbyData(_currentLobby.Value.Id.ToString(), data);

                return true;
            }
            else return false;
        }



        public Task TestFunction()
        {
            //NetPingLocation? pingLoc = SteamNetworkingUtils.LocalPingLocation;
            ////SteamNetworkingUtils

            //if (pingLoc != null)
            //{
            //    //string region = GetRegionFromPingLocation(pingLoc.Value);
            //    int ping = SteamNetworkingUtils.EstimatePingTo(pingLoc.Value);
            //    Debug.Log("Ping: " + ping);
            //}

            //Debug.Log("Local Ping LocatioN: " +  SteamNetworkingUtils.LocalPingLocation);

            Debug.Log(SteamClient.SteamId);
            return Task.CompletedTask;
        }

        public void PrintLobbyData()
        {
            if (_currentLobby == null)
            {
                NetworkLog.LogDev("[SteamLobby] No lobby joined. No data");
            }
            else
            {
                NetworkLog.LogDev("-------- [STEAM LOBBY DATA] ---------");
                NetworkLog.LogDev($"Member Count: {_currentLobby.Value.MemberCount}");

                foreach (KeyValuePair<string, string> data in _currentLobby.Value.Data)
                {
                    NetworkLog.LogDev($"{data.Key} | {data.Value}");
                }
                NetworkLog.LogDev("------------------------------------");
            }


        }


    }
}
