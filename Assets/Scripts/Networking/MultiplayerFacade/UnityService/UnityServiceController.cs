
using System;
using System.Linq;
using Unity.Multiplayer.Playmode;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Kudoshi.Networking.Unity
{
    public class UnityServiceController : IServiceController
    {
        public bool IsServiceRunning => AuthenticationService.Instance.IsSignedIn;

        public string PlayerID => AuthenticationService.Instance.PlayerId;

        public void Init(out IServiceRelay serviceRelay, out IServiceLobby serviceLobby)
        {
            InitializeService();

            // Instantiate service
            serviceRelay = new UnityRelayService();
            serviceLobby = new UnityLobbyService();

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
        private void InitializeService()
        {
            Authenticate();
        }

        private async void Authenticate()
        {
            InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR

            string[] playmodeTag = CurrentPlayer.ReadOnlyTags();

            if (playmodeTag.Contains("Player1"))
            {
                options.SetProfile("Player1");
            }
            else if (playmodeTag.Contains("Player2"))
            {
                options.SetProfile("Player2");
            }
            else if (playmodeTag.Contains("Player3"))
            {
                options.SetProfile("Player3");
            }
            else if (playmodeTag.Contains("Player4"))
            {
                options.SetProfile("Player4");
            }
#endif


            await UnityServices.InitializeAsync(options);


            if (UnityServices.State == ServicesInitializationState.Initialized)
            {

                AuthenticationService.Instance.SignedIn += OnSignedIn;

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                NetworkLog.LogNormal("[UnitySController] Unity Service initialized");
            }
        }

        private void OnSignedIn()
        {
            NetworkLog.LogNormal("[UnitySController] Player ID: " + AuthenticationService.Instance.PlayerId);
            NetworkLog.LogNormal("[UnitySController] Access Token: " + AuthenticationService.Instance.AccessToken);
        }

        private void DisconnectService()
        {
        }
    }
}