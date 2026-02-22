using Dreamonaut.Networking;
using System;
using TMPro;
using Unity.Netcode;
using Unity.Services.Qos.V2.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Kudoshi.UI
{
    public class TestMultiplayerServiceUI : NetworkBehaviour
    {
        [Header("Relay")]
        [SerializeField] private TextMeshProUGUI _relayID;
        [SerializeField] private Button _btnRelayIDCopy;
        [SerializeField] private TMP_InputField _relayCodeInput;

        [Header("Lobby")]
        [SerializeField] private TextMeshProUGUI _lobbyID;
        [SerializeField] private Button _btnLobbyIDCopy;
        [SerializeField] private TMP_InputField _lobbyIDInput;

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI _serviceType;

        private void Start()
        {
            _serviceType.text = MultiplayerFacade.Instance.ServiceType.ToString();
        }

        private void OnEnable()
        {
            _btnRelayIDCopy.onClick.AddListener(Btn_CopyRelayID);
            _btnLobbyIDCopy.onClick.AddListener(Btn_CopyLobbyID);
        }

        public async void UI_RelayJoin()
        {
            bool joined = await MultiplayerFacade.Instance.ServiceRelay.JoinRelay(_relayCodeInput.text);
            
            if (joined)
            {
                _relayID.text = _relayCodeInput.text;
            }
        }

        public async void UI_RelayStartHost()
        {
            string lobbyID = await MultiplayerFacade.Instance.ServiceRelay.HostRelay();

            _relayID.text = lobbyID;
        }

        public void UI_RelayShutdown() 
        {
            _relayID.text = "-----";
            MultiplayerFacade.Instance.ServiceRelay.Shutdown();
        }

        public void Btn_CopyRelayID()
        {
            TextEditor textEditor = new TextEditor();
            textEditor.text = _relayID.text;
            textEditor.SelectAll();
            textEditor.Copy();
        }


        ////////////////////////////////////////////////////////////////////////
        ///  [ LOBBY ]

        public async void UI_HostLobby()
        {
            CustomLobbyConfig lobbyData = new CustomLobbyConfig("FiresOut! Game", GameConstantVariables.MAX_PLAYERS, 0, CustomLobbyType.Public);
            string lobbyID = await MultiplayerFacade.Instance.ServiceLobby.HostLobby(lobbyData);
            _lobbyID.text = lobbyID;

            await MultiplayerFacade.Instance.ServiceLobby.HostUpdateLobbyStatus(LobbyStatus.ACTIVE);
        }

        public void UI_JoinLobbyByID()
        {
            MultiplayerFacade.Instance.ServiceLobby.JoinLobbyByLobbyCode(_lobbyIDInput.text);

        }

        

        public void UI_LeaveLobby()
        {
            MultiplayerFacade.Instance.ServiceLobby.LeaveLobby();
            _lobbyID.text = "-----";
        }

        public async void UI_PrintAllLobby()
        {
            CustomLobby[] lobbies = await MultiplayerFacade.Instance.ServiceLobby.GetAvailableLobbyList();

            if (lobbies == null || lobbies.Length == 0) return;

            foreach (CustomLobby b in lobbies)
            {
                Debug.Log("Lobby: " + b.LobbyID + " | Players: " + b.CurrentPlayers);
            }
        }

        public async void UI_TestFunction()
        {
            await MultiplayerFacade.Instance.ServiceLobby.TestFunction();
        }

        private void Btn_CopyLobbyID()
        {
            TextEditor textEditor = new TextEditor();
            textEditor.text = _lobbyID.text;
            textEditor.SelectAll();
            textEditor.Copy();
        }

        ////////////////////////////////////////////////////////////////////////
        ///  [ NetCode ]
        [SerializeField] private NetworkObject cubePrefab;

        public void UI_SpawnNewCube()
        {
            Server_SpawnCubeRpc(NetworkManager.Singleton.LocalClientId);

        }

        [Rpc(SendTo.Server)]
        private void Server_SpawnCubeRpc(ulong clientID)
        {
            NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(cubePrefab, clientID);

            //SessionManager.Instance.GetPlayerData(clientID, "");
            //SessionManager.Instance.SetupNewPlayerInstance(clientID, "1234");
        }
    }
}
