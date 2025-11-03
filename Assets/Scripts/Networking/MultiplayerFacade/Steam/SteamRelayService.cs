using Unity.Netcode;
using UnityEngine;
using Netcode;
using Facepunch;
using Steamworks;
using System.Threading.Tasks;
using System;

namespace Kudoshi.Networking
{
    public class SteamRelayService : IServiceRelay
    {
        private FacepunchTransport _transport;

        

        public void Init()
        {
            NetworkLog.LogDev("[SteamRelay] Setting up relay steam");

            _transport = NetworkManager.Singleton.gameObject.AddComponent<FacepunchTransport>();
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = _transport;
            _transport.InitRelay();
            NetworkLog.LogNormal("[SteamRelay] Logged in as: " + Steamworks.SteamClient.SteamId);
            
        }

        public void Shutdown()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();
            NetworkLog.LogDev("[SteamRelay] Shutdown");
        }

        public Task<string> HostRelay()
        {
            try
            {
                NetworkManager.Singleton.StartHost();
                string steamID = SteamClient.SteamId.Value.ToString();

                if (MultiplayerFacade.Instance.ServiceLobby.IsInLobby)
                {
                    MultiplayerFacade.Instance.ServiceLobby.HostUpdateLobbyStatus(LobbyStatus.INGAME);
                }

                return Task.FromResult(steamID);

            }
            catch (Exception e)
            {
                NetworkLog.LogDev("[SteamRelay] Unable to host relay: " + e.Message);

                return Task.FromResult<string>(null);
            }
        }

        /// <summary>
        /// Join Code refers to SteamID that you want to connect to
        /// </summary>
        /// <param name="joinCode"></param>
        /// <returns></returns>
        public Task<bool> JoinRelay(string joinCode)
        {
            try
            {
                ulong steamID = ulong.Parse(joinCode);

                _transport.targetSteamId = steamID;
                NetworkManager.Singleton.StartClient();

                NetworkLog.LogDev("[SteamRelay] Joining relay steam ID: " + joinCode);

                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                NetworkLog.LogDev("[SteamRelay] Unable to join relay: " + e.Message);

                return Task.FromResult(false);
            }
        }
    }
}