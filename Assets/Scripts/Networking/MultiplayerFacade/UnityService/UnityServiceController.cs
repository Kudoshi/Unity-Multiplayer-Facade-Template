
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Unity.Multiplayer.Playmode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Dreamonaut.Networking
{
    public class UnityServiceController : IServiceController
    {
        public bool IsServiceRunning => AuthenticationService.Instance.IsSignedIn;

        public string PlayerID => AuthenticationService.Instance.PlayerId;

        public string PlayerName => _playerName;

        private string _playerName;

        public void Init(Action<IServiceRelay, IServiceLobby> completeInitAction)
        {
            SetupServices(completeInitAction);
        }


        public void Shutdown()
        {
            MultiplayerFacade.Instance.ServiceRelay.Shutdown();
            MultiplayerFacade.Instance.ServiceLobby.Shutdown();
        }

        private async void SetupServices(Action<IServiceRelay, IServiceLobby> completeInitAction)
        {
            await Authenticate();

            // Instantiate service
            IServiceRelay serviceRelay = new UnityRelayService();
            IServiceLobby serviceLobby = new UnityLobbyService();

            // Initialize services
            serviceRelay.Init();
            serviceLobby.Init();

            completeInitAction?.Invoke(serviceRelay, serviceLobby);
        }
        private async Task Authenticate()
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

                // Using this as temporary name
                _playerName = GetRandomName();

                NetworkLog.LogNormal("[UnitySController] Unity Service initialized");
            }
        }

        private void OnSignedIn()
        {
            NetworkLog.LogNormal("[UnitySController] Character ID: " + AuthenticationService.Instance.PlayerId);
            NetworkLog.LogNormal("[UnitySController] Access Token: " + AuthenticationService.Instance.AccessToken);
        }

        #region Unity username generation

        private static readonly string[] randomNames = new string[]
{
            // OG cool names
            "Blaze", "Echo", "Shadow", "Pixel", "Nova", "Bolt", "Fizz", "Luna", "Vortex", "Jinx",
    
            // Meme / funny names
            "BigChungus", "ShrekOnFleek", "Pepega", "YeetMaster", "UgandanKnuckles", "Doggo",
            "Cheems", "SussyBaka", "Karen", "ThiccBois", "NoobSlayer69", "PogChamp", "RickRoller",
            "BoomerBoi", "UwU", "OofMaster", "BruhMoment", "Froge", "DankMemer", "LOLer"
        };

        private string GetRandomName()
        {
            int index = UnityEngine.Random.Range(0, randomNames.Length);
            int suffix = UnityEngine.Random.Range(1, 1000); // random number 1�999
            return $"{randomNames[index]}{suffix}";
        }

        #endregion
    }
}