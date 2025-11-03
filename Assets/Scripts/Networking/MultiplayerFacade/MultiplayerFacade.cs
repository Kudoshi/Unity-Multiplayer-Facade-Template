
using Kudoshi.Networking.Steam;
using Kudoshi.Networking.Unity;
using Kudoshi.Utilities;
using System;
using UnityEngine;

namespace Kudoshi.Networking
{
    /// <summary>
    /// Currently Multiplayer Facade does not do much other than assigning the service implementation and holds service references
    /// </summary>
    public class MultiplayerFacade : Singleton<MultiplayerFacade>
    {
        [SerializeField] private MultiplayerServiceType _serviceType = MultiplayerServiceType.STEAM;
        [SerializeField] private SO_NetworkConfig _networkConfig;

        private bool _serviceOn = false;

        // Services
        private IServiceController _serviceController;
        private IServiceRelay _serviceRelay;
        private IServiceLobby _serviceLobby;

        // Getters
        public SO_NetworkConfig NetworkConfig { get => _networkConfig; }
        public MultiplayerServiceType ServiceType { get => _serviceType; }
        public IServiceController ServiceController { get => _serviceController; }
        public IServiceRelay ServiceRelay { get => _serviceRelay; }
        public IServiceLobby ServiceLobby { get => _serviceLobby; }

        private void Awake()
        {
            if (MultiplayerFacade.Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetupMultiplayerFacade();
        }

        private void OnApplicationQuit()
        {
            if (_serviceOn)
            {
                DisconnectService();
            }
        }


        private void SetupMultiplayerFacade()
        {
            // Insert the appropriate service controller here
            if (_serviceType == MultiplayerServiceType.STEAM)
            {
                // Instantiate service
                _serviceController = new SteamServiceController();
            }
            else if (_serviceType == MultiplayerServiceType.UNITY)
            {
                // Instantiate service
                _serviceController = new UnityServiceController();
            }

            // Initialize services
            _serviceController.Init(out _serviceRelay, out _serviceLobby);

            _serviceOn = true;

        }

        private void DisconnectService()
        {
            // Initialize services
            _serviceController.Shutdown();
            _serviceOn = false;

        }
    }

}

