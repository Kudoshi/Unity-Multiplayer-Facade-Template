
using Unity.Netcode;
using UnityEngine;

namespace Dreamonaut.Networking
{
    [CreateAssetMenu(fileName = "SO_NetworkConfig", menuName = "Scriptable Objects/Networking/SO_NetworkConfig")]
    public class SO_NetworkConfig : ScriptableObject
    {
        [Tooltip("Insert your steam app id here. Can be found on the steam page")]
        public uint SteamAppID = 480;


        /// <summary>
        /// Contains compiled time constant network configs
        /// Has to be changed in script
        /// </summary>
        #region Compile Time Configs

        // Specify what levels of network debug is to be logged down
        public const LogLevel LOGGING_LEVEL = LogLevel.Developer;
        #endregion
    }
}