using ExitGames.Client.Photon;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomButton : MonoBehaviour
{
    [Header("방 버튼 UI 관련")]
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] TextMeshProUGUI userCount;
    [SerializeField] Image locker;

    string roomId; // 방의 아이디값
    bool hasPassword; // 방의 비밀방(비밀번호 존재) 여부
    string displayname; // 표기될 방의 이름
    int currentPlayerCount; // 현재 플레이어의 수
    int maxPlayerCount; // 최대 플레이어의 수

    public string RoomId { get { return roomId; } set { roomId = value; } }
    public bool HasPassword { get { return hasPassword; } set { hasPassword = value; } }
    public string DisplayName { get { return displayname; } set { displayname = value; } }
    public int MaxPlayerCount { get { return maxPlayerCount; } set { maxPlayerCount = value; } }
    public int CurrentPlayerCount { get { return currentPlayerCount; } set { currentPlayerCount = value; } }

    /// <summary>
    /// 방의 데이터를 지정하는 메서드입니다.
    /// </summary>
    /// <param name="info">방 정보</param>
    public void SetData(RoomInfo info)
    {
        //방의 아이디값을 불러옵니다.
        roomId = info.Name;

        //출력할 방 제목의 커스텀 프로퍼티값이 있을 경우, 타이틀에 해당 내용을 기입합니다.
        if (info.CustomProperties.TryGetValue("displayname", out object name))
            title.text = name.ToString();
        //없으면 아이디값을 그대로 적습니다.
        else title.text = info.Name;

        //비밀번호의 커스텀 프로퍼티값이 있을 경우, 비밀번호 존재 여부를 변경합니다.
        if (info.CustomProperties.TryGetValue("hasPassword", out object password))
            HasPassword = (bool)password;
        //없으면 false로 지정합니다.
        else HasPassword = false;

        //유저 수는 "현재 존재하는 플레이어 수 / 최대 플레이어 수"로 고정합니다.
        userCount.text = $"{info.PlayerCount}/{info.MaxPlayers}";

        //비밀번호가 존재하는지 여부에 따라 비밀방을 나타내는 자물쇠의 출현 여부를 결정합니다.
        locker.gameObject.SetActive(HasPassword);
    }
}
