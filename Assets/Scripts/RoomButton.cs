using ExitGames.Client.Photon;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] TextMeshProUGUI userCount;
    [SerializeField] Image locker;
    string roomId;
    bool hasPassword;
    string displayname;
    int currentPlayerCount;
    int maxPlayerCount;

    public string RoomId { get { return roomId; } set { roomId = value; } }
    public bool HasPassword { get { return hasPassword; } set { hasPassword = value; } }
    public string DisplayName { get { return displayname; } set { displayname = value; } }
    public int MaxPlayerCount { get { return maxPlayerCount; } set { maxPlayerCount = value; } }
    public int CurrentPlayerCount { get { return currentPlayerCount; } set { currentPlayerCount = value; } }

    public void SetData(RoomInfo info)
    {
        roomId = info.Name;

        if (info.CustomProperties.TryGetValue("displayname", out object name))
            title.text = name.ToString();
        else title.text = info.Name;

        if (info.CustomProperties.TryGetValue("hasPassword", out object password))
            HasPassword = (bool)password;
        else HasPassword = false;

        userCount.text = $"{info.PlayerCount}/{info.MaxPlayers}";

        locker.gameObject.SetActive(HasPassword);
    }
}
