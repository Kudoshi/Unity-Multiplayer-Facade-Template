using Facepunch;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Kudoshi.Networking.Unity
{
    public class UnityRelayService : IServiceRelay
    {
        private UnityTransport _transport;

        private Allocation _allocation;
        private JoinAllocation _joinAllocation;
        private string _joinCode;

        public void Init()
        {
            NetworkLog.LogDev("[UnityRelay] Setting up unity relay");

            _transport = NetworkManager.Singleton.gameObject.AddComponent<UnityTransport>();
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = _transport;
            //_transport.Initialize();
            //NetworkLog.LogNormal("[SteamRelay] Logged in as: " + Steamworks.SteamClient.SteamId);
        }

        //public void ResetRelay()
        //{
        //    _allocation = null;
        //    _joinAllocation = null;
        //    _joinCode = null;
        //}

        public async Task<string> HostRelay()
        {
            try
            {
                _allocation = await RelayService.Instance.CreateAllocationAsync(GameConstantVariables.MAX_PLAYERS);

                _joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);
                NetworkLog.LogDev("[UnityRelay] Created Relay : " + _joinCode);

                _transport.SetRelayServerData(AllocationUtils.ToRelayServerData(_allocation, "dtls"));

                NetworkManager.Singleton.StartHost();

                if (MultiplayerFacade.Instance.ServiceLobby.IsInLobby)
                {
                    await MultiplayerFacade.Instance.ServiceLobby.HostUpdateLobbyStatus(LobbyStatus.INGAME);
                }

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
                _joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                NetworkLog.LogDev("[UnityRelay] Joining relay with : " + joinCode);

                _transport.SetRelayServerData(AllocationUtils.ToRelayServerData(_joinAllocation, "dtls"));

                NetworkManager.Singleton.StartClient();

                return true;
            }
            catch (Exception e)
            {
                NetworkLog.LogDev("[UnityRelay] Unable to join relay: " + e.Message);

                return false;
            }
        }

        public void Shutdown()
        {
            _transport.Shutdown();
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();
            NetworkLog.LogDev("[UnityRelay] Shutdown");
        }
    }
}