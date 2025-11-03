
using TMPro;
using UnityEngine;

public class UIRoomPlayerSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textPlayerName;
    [SerializeField] private TextMeshProUGUI _textReady;

    private string _playerId;
    private string _playerName;
    private bool _isReady;

    public bool IsReady { get => _isReady; }

    public void Initialize(string playerId, string playerName)
    {
        _playerId = playerId;
        _playerName = playerName;

        _textPlayerName.text = playerName;

        SetReady(false);
        gameObject.SetActive(true);
    }

    public void SetReady(bool isReady)
    {
        _isReady = isReady;
        if (_isReady)
        {
            _textReady.text = "Ready";
        }
        else
            _textReady.text = "Not Ready";
    }
}