using System;
using System.Collections.Generic;
using UnityLobby = Unity.Services.Lobbies.Models.Lobby;
using SteamLobby = Steamworks.Data.Lobby;
using System.Linq;

namespace Dreamonaut.Networking
{

    /// <summary>
    /// Generic representation of a lobby
    /// Supports Steam and Unity lobby
    /// </summary>
    public struct CustomLobby
    {
        public string LobbyID;
        public string LobbyName;
        public int CurrentPlayers;
        public int MaxPlayers;
        public int PlayerInstanceCount; // This player counts for player instances
        public string LobbyOwnerID;
        public CustomLobbyType LobbyType;
        public Dictionary<string, string> Data;
        public LobbyPlayer[] Players;
        public LobbyStatus LobbyStatus;


        public CustomLobby(string lobbyID, string lobbyName, int currentPlayers, int maxPlayers, int playerInstanceCount, string lobbyOwnerID, CustomLobbyType lobbyType, LobbyStatus lobbyStatus, Dictionary<string, string> data)
        {
            LobbyID = lobbyID;
            LobbyName = lobbyName;
            CurrentPlayers = currentPlayers;
            MaxPlayers = maxPlayers;
            PlayerInstanceCount = playerInstanceCount;
            LobbyOwnerID = lobbyOwnerID;
            LobbyType = lobbyType;
            LobbyStatus = lobbyStatus;

            Data = data;
            Players = new LobbyPlayer[0];
        }

        public CustomLobby(SteamLobby steamLobby)
        {
            LobbyID = steamLobby.Id.ToString();
            LobbyName = steamLobby.GetData(KEY_LOBBY_NAME);
            CurrentPlayers = steamLobby.MemberCount;
            MaxPlayers = steamLobby.MaxMembers;
            LobbyOwnerID = steamLobby.Owner.Id.ToString();

            LobbyType = Enum.Parse<CustomLobbyType>(steamLobby.GetData(KEY_CUSTOM_LOBBY_TYPE));

            LobbyStatus = Enum.Parse<LobbyStatus>(steamLobby.GetData(KEY_LOBBY_STATUS));

            Data = steamLobby.Data.ToDictionary(item => item.Key, item => item.Value);
            Players = steamLobby.Members.Select(member => new LobbyPlayer(member.Id.ToString(), member.Name)).ToArray();
            PlayerInstanceCount = int.Parse(Data[KEY_PLAYER_INSTANCE_COUNT]);
        }

        public CustomLobby(UnityLobby unityLobby)
        {
            LobbyID = unityLobby.Id;
            LobbyName = unityLobby.Data[KEY_LOBBY_NAME].Value;
            CurrentPlayers = unityLobby.Players.Count;
            MaxPlayers = unityLobby.MaxPlayers;
            LobbyOwnerID = unityLobby.HostId;

            LobbyType = Enum.Parse<CustomLobbyType>(unityLobby.Data[KEY_CUSTOM_LOBBY_TYPE].Value);

            LobbyStatus = Enum.Parse<LobbyStatus>(unityLobby.Data[KEY_LOBBY_STATUS].Value);

            Data = unityLobby.Data.ToDictionary(item => item.Key, item => item.Value.Value);
            Players = unityLobby.Players.Select(player => new LobbyPlayer(player.Id.ToString(), player.Id.ToString())).ToArray();
            PlayerInstanceCount = int.Parse(Data[KEY_PLAYER_INSTANCE_COUNT]);
        }

        public static CustomLobby[] ParseSteamLobbies(SteamLobby[] lobbies)
        {
            List<CustomLobby> cLobbies = new List<CustomLobby>();
            foreach (SteamLobby lobby in lobbies)
            {
                CustomLobby l = new CustomLobby(lobby);
                cLobbies.Add(l);
            }

            return cLobbies.ToArray();
        }

        public static CustomLobby[] ParseUnityLobbies(UnityLobby[] lobbies)
        {
            List<CustomLobby> cLobbies = new List<CustomLobby>();

            foreach (UnityLobby lobby in lobbies)
            {
                CustomLobby l = new CustomLobby(lobby);
                cLobbies.Add(l);
            }

            return cLobbies.ToArray();
        }

        // KEYs - List of lobby data keys
        public static string KEY_LOBBY_NAME = "lobbyname"; // Value: string | Defined by host
        public static string KEY_MAX_PLAYER = "maxplayer"; // Value: int | Currently value uses GameConstantVariable.MAX_PLAYERS 
        public static string KEY_GAME_NAME = "game"; // Value: string | Used by Steam service to select only our game lobby incase game uses 480 app id (Space war)
        public static string KEY_CUSTOM_LOBBY_TYPE = "customlobbytype"; // Value: CustomLobbyType
        public static string KEY_LOBBY_CODE = "lobbycode"; // Value: string | Lobby code for players to join
        public static string KEY_START_GAME = "startgame"; // Value: string | Relay code for client players to join relay
        public static string KEY_LOBBY_STATUS = "lobbystatus"; // Value: LobbyStatus | Indicate what is the current status of the lobby
        public static string KEY_PLAYER_INSTANCE_COUNT = "playerinstancecount"; // Value: int | Custom tracking of how many player in the lobby
    }

    /// <summary>
    /// Configs to hosting server
    /// </summary>
    public struct CustomLobbyConfig
    {
        public string LobbyName;
        public int MaxPlayer;
        public int PlayerInstanceCount;
        public CustomLobbyType LobbyType;

        public CustomLobbyConfig(string lobbyName, int maxPlayer, int playerInstanceCount, CustomLobbyType lobbyType)
        {
            LobbyName = lobbyName;
            MaxPlayer = maxPlayer;
            PlayerInstanceCount = playerInstanceCount;
            LobbyType = lobbyType;
        }
    }

    //
    // Lobby Type Config
    //
    public enum CustomLobbyType : int
    {
        Private = 0,
        FriendsOnly = 1,
        Public = 2,
        Invisible = 3,
        PrivateUnique = 4,
        Offline = 5,
    }

    /// <summary>
    /// Enum result for when you try to join a lobby
    /// </summary>
    public enum LobbyEnterResult : int
    {
        Success = 1,
        DoesntExist = 2,
        NotAllowed = 3,
        Full = 4,
        Error = 5,
        Banned = 6,
        Limited = 7,
        ClanDisabled = 8,
        CommunityBan = 9,
        MemberBlockedYou = 10,
        YouBlockedMember = 11,
        RatelimitExceeded = 15,
    }

    /// <summary>
    /// Status of lobby
    /// 
    /// Lobby system will only query for active lobby
    /// </summary>
    public enum LobbyStatus
    {
        SETTING_UP, ACTIVE, INGAME, INACTIVE
    }
}

