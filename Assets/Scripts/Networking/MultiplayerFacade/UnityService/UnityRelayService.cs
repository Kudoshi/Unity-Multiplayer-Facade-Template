using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Dreamonaut.Networking
{
    public class UnityRelayService : IServiceRelay
    {
        private UnityTransport _transport;

        private Allocation _allocation;
        private JoinAllocation _joinAllocation;
        private string _joinCode;
        private bool _isInSession = false;

        public bool IsInSession => _isInSession;

        public void Init()
        {
            NetworkLog.LogDev("[UnityRelay] Setting up unity relay");

            _transport = NetworkManager.Singleton.gameObject.AddComponent<UnityTransport>();
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = _transport;
            //_transport.InitListeners();
            //NetworkLog.LogNormal("[SteamRelay] Logged in as: " + Steamworks.SteamClient.SteamId);
        }

        public void Reset()
        {
            _isInSession = false;
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();
            _allocation = null;
            _joinAllocation = null;
            _joinCode = null;
            NetworkLog.LogDev("[UnityRelay] Reset");

        }



        public void Shutdown()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();
            NetworkLog.LogDev("[UnityRelay] Shutdown");

            _isInSession = false;
        }

        public async Task<string> HostRelay()
        {
            try
            {
                _allocation = await RelayService.Instance.CreateAllocationAsync(GameConstantVariables.MAX_PLAYERS);

                _joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);
                NetworkLog.LogDev("[UnityRelay] Created Relay : " + _joinCode);

                _transport.SetRelayServerData(AllocationUtils.ToRelayServerData(_allocation, "dtls"));

                NetworkManager.Singleton.StartHost();

                //if (MultiplayerFacade.Instance.ServiceLobby.IsInLobby)
                //{
                //    await MultiplayerFacade.Instance.ServiceLobby.HostUpdateLobbyStatus(LobbyStatus.INGAME);
                //}

                _isInSession = true;
                return _joinCode;
            }
            catch (RelayServiceException e)
            {
                NetworkLog.LogDev("[UnityRelay] Unable to create relay: " + e.Message);
                return null;
            }
        }

        public async Task<bool> JoinRelay(string joinCode)
        {
            try
            {
                _joinCode = joinCode;
                _joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                NetworkLog.LogDev("[UnityRelay] Joining relay with : " + joinCode);

                _transport.SetRelayServerData(AllocationUtils.ToRelayServerData(_joinAllocation, "dtls"));

                NetworkManager.Singleton.StartClient();
                _isInSession = true;

                return true;
            }
            catch (Exception e)
            {
                NetworkLog.LogDev("[UnityRelay] Unable to join relay: " + e.Message);

                return false;
            }
        }

        public async Task<(bool success, bool shouldTryAgain)> ReconnectRelay()
        {
            try
            {
                if (!_isInSession)
                {
                    NetworkLog.LogDev("[UnityRelay] Not in session, unablle to rejoin relay");
                    return (false, false);
                }

                _joinAllocation = await RelayService.Instance.JoinAllocationAsync(_joinCode);

                NetworkLog.LogDev("[UnityRelay] Rejoining relay with : " + _joinCode);

                _transport.SetRelayServerData(AllocationUtils.ToRelayServerData(_joinAllocation, "dtls"));

                NetworkManager.Singleton.StartClient();
                _isInSession = true;

                return (true, false);
            }
            catch (Exception e)
            {
                NetworkLog.LogDev("[UnityRelay] Unable to rejoin relay: " + e.Message);

                return (false, true);
            }
        }

        

    }
}