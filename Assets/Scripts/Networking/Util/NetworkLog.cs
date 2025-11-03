using Unity.Netcode;
using UnityEngine;

namespace Kudoshi.Networking
{
    /// <summary>
    /// Allows you to turn off debugging log for network related stuff when not dealing with network stuff
    /// </summary>
    public class NetworkLog
    {

        // Logs ALL network debug log
        public static void LogDev(string msg)
        {
            if (SO_NetworkConfig.LOGGING_LEVEL <= LogLevel.Developer)
            {
                Debug.Log(msg);
            }
        }

        // Logs only important network log
        public static void LogNormal(string msg)
        {
            if (SO_NetworkConfig.LOGGING_LEVEL <= LogLevel.Normal)
            {
                Debug.Log(msg);
            }
        }

        // Logs only when there are error on the network
        public static void LogError(string msg)
        {
            if (SO_NetworkConfig.LOGGING_LEVEL <= LogLevel.Error)
            {
                Debug.Log(msg);
            }
        }
    }
}