using Unity.Netcode;
using Facepunch;
using Steamworks;
using System.Threading.Tasks;
using System;

namespace Dreamonaut.Networking
{
    public class SteamRelayService : IServiceRelay
    {
        private FacepunchTransport _transport;
        private string _joinCode;
        private bool _isInSession = false;
        public bool IsInSession => _isInSession;

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

            NetworkLog.LogDev("[UnityLobbyService] Shutdown service");
        }

        public void Reset()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();

            _transport.targetSteamId = 0;
            _isInSession = false;
            _joinCode = null;
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

                _isInSession = true;
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
                _joinCode = joinCode;
                ulong steamID = ulong.Parse(joinCode);

                _transport.targetSteamId = steamID;
                NetworkManager.Singleton.StartClient();

                NetworkLog.LogDev("[SteamRelay] Joining relay steam ID: " + joinCode);

                _isInSession = true;
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                NetworkLog.LogDev("[SteamRelay] Unable to join relay: " + e.Message);

                return Task.FromResult(false);
            }
        }

        public Task<(bool success, bool shouldTryAgain)> ReconnectRelay()
        {
            try
            {
                if (!_isInSession)
                    return Task.FromResult((false, false));

                ulong steamID = ulong.Parse(_joinCode);

                _transport.targetSteamId = steamID;
                NetworkManager.Singleton.StartClient();

                NetworkLog.LogDev("[SteamRelay] Rejoining relay steam ID: " + _joinCode);

                _isInSession = true;

                return Task.FromResult((true,false));
            }
            catch (Exception e)
            {
                NetworkLog.LogDev("[SteamRelay] Unable to rejoin relay: " + e.Message);

                return Task.FromResult((false,true));
            }
        }

    }
}
