using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for each individual record lines in the lobby selection scene
/// </summary>
public class UILobbyListingRecord : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _serverName;
    [SerializeField] private TextMeshProUGUI _playerAmt;
    [SerializeField] private Button _joinBtn;

    private string _serverID;
    private UILobbyManager _manager;    

    private void Awake()
    {
        _joinBtn.onClick.AddListener(JoinLobby);
    }

    public void Initialize(UILobbyManager manager, string serverID, string serverName, string playerAmt)
    {
        _serverID = serverID;
        _manager = manager;

        _serverName.text = serverName;
        _playerAmt.text = playerAmt;
    }

    private void JoinLobby()
    {
        _manager.JoinLobby(_serverID);
    }
}