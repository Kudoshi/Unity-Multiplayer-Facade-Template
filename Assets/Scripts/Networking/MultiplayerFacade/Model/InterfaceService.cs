using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEditor;
using UnityEngine;

namespace Kudoshi.Networking
{
    // This file contains the interface for all the services and the info about them
    // There is:
    // 1. Service Controller - Handles authorization and etc setups
    // 2. Service Relay - Handles relay system
    // 3. Service Lobby - Handles lobby system


    // Every service will have Init and Shutdown to mimic the OnEnable and OnDisable feature of Monobehavior

    /// <summary>
    /// Service Controller
    /// 
    /// - Currently only handles authorization, initialization and shutdowns
    /// </summary>
    public interface IServiceController
    {
        public void Init(out IServiceRelay serviceRelay, out IServiceLobby serviceLobby);
        public void Shutdown();

        // Check whether service has already started 
        public bool IsServiceRunning { get; }
        // Get ownself player ID
        public string PlayerID { get; }

    }

    /// <summary>
    /// Server Relay
    /// 
    /// - Configures netcode to use the service's transport layer
    /// - Manages player to join and host relay
    /// </summary>
    public interface IServiceRelay
    {
        public void Init();
        public void Shutdown();

        // Start the relay. Should be called by host
        public Task<string> HostRelay();
        // Join the relay. Should be called by client 
        public Task<bool> JoinRelay(string joinCode);
    }

    /// <summary>
    /// Service Lobby
    /// 
    /// Have features for hosting lobby, managing lobby, fetch and update data to lobby service
    /// 
    /// Perks:
    ///  - The lobby service assumes that by default when host leaves -> entire lobby players should also disconnect
    ///     - Steam does not have callbacks for this while Unity does but does not seem to function properly
    ///     - Current implementation uses OnPlayerLeft to detect if it is host leaving then have all players leave themselves
    /// </summary>
    public interface IServiceLobby
    {
        public void Init();
        public void Shutdown();


        public CustomLobby Lobby { get; }
        public bool IsInLobby { get; }

        /// <summary>
        /// Called by host. Creates a lobby based on configs given
        /// - Steam has not lobby code. They use lobby ID to connect only thus returning lobby ID
        /// - Unity has both lobby code and lobby ID. They can use both to connect. It returns lobby code
        /// 
        /// When lobby is hosted, various data are pushed to lobby and lobby is set to active status
        /// </summary>
        /// <param name="lobbyData">Config for the lobby</param>
        /// <returns>Lobby Code</returns>
        public Task<string> HostLobby(CustomLobbyConfig lobbyData);

        /// <summary>
        /// Joins a lobby by lobby ID
        /// </summary>
        /// <param name="lobbyID">Lobby ID</param>
        /// <returns>Lobby Enter Result</returns>
        public Task<LobbyEnterResult> JoinLobbyByID(string lobbyID);
        /// <summary>
        /// Joins lobby by lobby code
        ///  - Steam uses lobby ID to connect
        /// </summary>
        /// <param name="lobbyCode"></param>
        /// <returns>Lobby Enter Result</returns>
        public Task<LobbyEnterResult> JoinLobbyByLobbyCode(string lobbyCode);

        /// <summary>
        /// Leaves the current lobby joined
        /// 
        /// When host leaves lobby, sets lobby to inactive status then triggers all clients to leave
        /// </summary>
        /// <returns>Leave successful</returns>
        public Task<bool> LeaveLobby();

        /// <summary>
        /// Gets a list of lobby that:
        ///     - Is public
        ///     - Is active status
        /// </summary>
        /// <returns>List of public active lobbies</returns>
        public Task<CustomLobby[]> GetAvailableLobbyList();

        /// <summary>
        /// Gets a list of lobby with server name containing the name and also:
        ///     - Is public
        ///     - Is active status
        /// </summary>
        /// <param name="name">name of server</param>
        /// <returns>List of public active lobbies with the name contained</returns>
        public Task<CustomLobby[]> GetAvailableLobbyByName(string name);

        /// <summary>
        /// To get the latest update to the lobby. 
        /// - Unity: Used to force update the lobby after joining lobby. Because Unity is based on snapshot of lobby data
        /// - Steam: To manually force sync incase of de-sync after joining lobby. By default it auto updates lobby data unlike unity
        /// </summary>
        /// <returns>Current lobby</returns>
        public Task<CustomLobby> FetchLatestLobby();

        /// <summary>
        /// Create or update lobby data
        /// 
        /// Refer to the KEYs in the CustomLobby for the list of data
        /// </summary>
        /// <param name="lobbyID">Lobby ID</param>
        /// <param name="data">Dictionary of <dataKey, dataValue></param>
        /// <returns>Update success</returns>
        public Task<bool> UpdateLobbyData(string lobbyID, Dictionary<string, string> data);

        /// <summary>
        /// Gets player data based on the player ID
        /// 
        /// Refer to the KEYs in the LobbyPlayer for the list of data 
        /// </summary>
        /// <param name="playerID">Player ID</param>
        /// <param name="dataKey">Player data key</param>
        /// <returns></returns>
        public Task<string> GetPlayerData(string playerID, string dataKey);

        /// <summary>
        /// Update player data based on player ID
        /// </summary>
        /// <param name="lobbyID">Lobby ID</param>
        /// <param name="playerID">Player ID</param>
        /// <param name="data">Dictionary of <dataKey, dataValue></param>
        /// <returns>Update success</returns>
        public Task<bool> UpdatePlayerData(string lobbyID, string playerID, Dictionary<string, string> data);

        /// <summary>
        /// Used exclusively only for Steam
        /// Triggers Steam Player invite popup and also opens lobby up to be joined by steam friends
        /// 
        /// Currently Unity does not implement this
        /// </summary>
        /// <returns>Lobby open up success</returns>
        public Task<bool> OpenLobbyUpForFriendInvite();

        /// <summary>
        /// Can only be called by lobby owner to update lobby status
        /// </summary>
        /// <param name="lobbyStatus"></param>
        /// <returns></returns>
        public Task<bool> HostUpdateLobbyStatus(LobbyStatus lobbyStatus);

        /// <summary>
        /// For test use case to print lobby data
        /// </summary>
        public void PrintLobbyData();

        /// <summary>
        /// For test use case
        /// A placeholder function to easily allow you to simply test random stuff in the lobby system
        /// Put the stuff you want to test in the test function 
        /// </summary>
        /// <returns></returns>
        public Task TestFunction();

        // Events to subscribe to from outside of lobby service

        // Refer to: https://1drv.ms/x/c/2d5bfe54c619d967/EVjwhyA33c5IiTrrrDsAGxABpPokt8ZmrBfDpU5-5leleQ?e=T8nhyY
        // To get more info about the events on Steam and Unity
        // Contains a list of similar/differences in the callbacks between Steam Unity and which events is implemented

        // Steam and Unity signal lobby creation success(Steam = callback, UGS = async result).
        public event Action<CustomLobby> OnLobbyCreated;

        //      Any change in lobby metadata, e.g. name, custom data, or settings.
        public event Action<CustomLobby> OnLobbyUpdate;

        //      Player-specific key-value updates — e.g. ready states, character choice, etc.
        public event Action<CustomLobby, LobbyPlayer> OnPlayerDataChange;

        //      Steam and Unity fire when a user enters a lobby.For UGS host, the lobby creation result also means you "entered".
        public event Action<CustomLobby, LobbyPlayer> OnLobbyPlayerJoin;

        //      Unity and Steam trigger when someone leaves.
        public event Action<CustomLobby, LobbyPlayer> OnPlayerLeave;

        //     When player invites a friend to the lobby. Only in Steam
        public event Action<CustomLobby, LobbyPlayer> OnLobbyInviteFriend;

        //      When player gets a lobby invite from host. Only in Steam
        public event Action<CustomLobby, LobbyPlayer> OnLobbyGetInvited;

        //      When lobby gets closed down or for now when you get kicked as well
        //      Detects when lobby is closed by detecting when host leaves the game. Unity and Steam callbacks for lobby closed doesn't work
        public event Action OnLobbyClosed;

    }
}