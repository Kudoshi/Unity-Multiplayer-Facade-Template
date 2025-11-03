using Steamworks;
using System.Collections;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Kudoshi.Networking.Steam
{
    public class SteamServiceController : IServiceController
    {
        public bool IsServiceRunning => SteamClient.IsValid;

        public string PlayerID => SteamClient.SteamId.Value.ToString();

        public void Init(out IServiceRelay serviceRelay, out IServiceLobby serviceLobby)
        {
            InitializeSteam();

            // Instantiate service
            serviceRelay = new SteamRelayService();
            serviceLobby = new SteamLobbyService();


            // Initialize services
            serviceRelay.Init();
            serviceLobby.Init();

        }

        public void Shutdown()
        {
            MultiplayerFacade.Instance.ServiceRelay.Shutdown();
            MultiplayerFacade.Instance.ServiceLobby.Shutdown();

            DisconnectService();
        }

        public void InitializeSteam()
        {
            try
            {
                uint appID = MultiplayerFacade.Instance.NetworkConfig.SteamAppID;

                Steamworks.SteamClient.Init(appID, false);

                NetworkLog.LogNormal("[SteamInit] Successfully initialize steam client app id: " + appID);

                var playername = SteamClient.Name;
                var playersteamid = SteamClient.SteamId;

                NetworkLog.LogNormal($"[SteamInit] Player: {playername} | SteamID: {playersteamid}");
            }
            catch (System.Exception e)
            {
                NetworkLog.LogError("[SteamInit] Unable to initialize steam");
                Debug.LogException(e);
            }
        }

        public void DisconnectService()
        {
            Steamworks.SteamClient.Shutdown();
            NetworkLog.LogNormal("[SteamInit] Shutdown steam service");
        }

    }

   
}
