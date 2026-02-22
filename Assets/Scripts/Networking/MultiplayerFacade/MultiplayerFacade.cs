
using Kudoshi.Utilities;
using UnityEngine;

namespace Dreamonaut.Networking
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
        public IServiceRelay ServiceRelay { get => _serviceRelay; set => _serviceRelay = value; }
        public IServiceLobby ServiceLobby { get => _serviceLobby; set => _serviceLobby = value; }
        public bool ServiceOn { get => _serviceOn; }

        private void Awake()
        {
            SetSingletonDontDestroyOnLoad(this);
        }

        private void Start()
        {
            SetupMultiplayerFacade();
        }

        private void OnApplicationQuit()
        {
            if (_serviceOn)
            {
                ShutdownService();
            }
        }

        public void ResetServices()
        {
            _serviceLobby.Reset();
            _serviceRelay.Reset();
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
            _serviceController.Init(FinishSetup);
        }

        private void FinishSetup(IServiceRelay serviceRellay, IServiceLobby serviceLobby)
        {
            _serviceRelay = serviceRellay;
            _serviceLobby = serviceLobby;

            _serviceOn = true;
        }

        private void ShutdownService()
        {
            // InitListeners services
            _serviceController.Shutdown();

            _serviceOn = false;

        }
    }

}

