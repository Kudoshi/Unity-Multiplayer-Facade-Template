
using System.Collections.Generic;
using SteamFriend = Steamworks.Friend;

/// <summary>
/// Generic representation of lobby player
/// 
/// Perks:
///     - Unity Service does not have username by default
///         - Therefore current unity service implementation uses Id as username for simplicity sake 
/// </summary>
public struct LobbyPlayer
{
    public string Id;
    public string Name;
    
    public static string KEY_PLAYER_READY = "playerready"; // Value: bool | Indicate whether this player is ready. Used by lobby manager

    public LobbyPlayer(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public LobbyPlayer(SteamFriend friend)
    {
        Id = friend.Id.ToString();
        Name = friend.Name;
    }

}