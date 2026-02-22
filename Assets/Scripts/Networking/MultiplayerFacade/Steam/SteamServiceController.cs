using Steamworks;
using System;
using UnityEngine;

namespace Dreamonaut.Networking
{ 
    public class SteamServiceController : IServiceController
    {
        public bool IsServiceRunning => SteamClient.IsValid;

        public string PlayerID => SteamClient.SteamId.Value.ToString();

        public string PlayerName => SteamClient.Name;


        public void Init(Action<IServiceRelay, IServiceLobby> completeInitAction)
        {
            InitializeSteam();

            // Instantiate service
            IServiceRelay serviceRelay = new SteamRelayService();
            IServiceLobby serviceLobby = new SteamLobbyService();


            // InitListeners services
            serviceRelay.Init();
            serviceLobby.Init();

            completeInitAction?.Invoke(serviceRelay, serviceLobby);
        }

        public void Shutdown()
        {
            MultiplayerFacade.Instance.ServiceRelay.Shutdown();
            MultiplayerFacade.Instance.ServiceLobby.Shutdown();
            Steamworks.SteamClient.Shutdown();
            NetworkLog.LogNormal("[SteamInit] Shutdown steam service");
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

                NetworkLog.LogNormal($"[SteamInit] Character: {playername} | SteamID: {playersteamid}");
            }
            catch (System.Exception e)
            {
                NetworkLog.LogError("[SteamInit] Unable to initialize steam");
                Debug.LogException(e);
            }
        }

    }

   
}
