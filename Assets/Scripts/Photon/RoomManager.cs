using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("인터페이스 관련")]
    [SerializeField] TextMeshProUGUI roomIdText;
    [SerializeField] TextMeshProUGUI roomTitleText;
    [SerializeField] TextMeshProUGUI roomPlayerCountText;

    [Header("시작 버튼")]
    [SerializeField] Button startButton;

    [Header("퇴장 버튼 관련")]
    [SerializeField] Button exitButton;

    [Header("각 슬롯별 이미지 연결")]
    [SerializeField] List<RoomPlayerSlot> slots;

    void Awake()
    {
        #region 인스펙터 연결 확인용 코드
        if (roomIdText == null)
            Debug.LogError("RoomManager - 방 아이디 텍스트가 연결되지 않았습니다!");
        if (roomTitleText == null)
            Debug.LogError("RoomManager - 방 제목 텍스트가 연결되지 않았습니다!");
        if (roomPlayerCountText == null)
            Debug.LogError("RoomManager - 방 인원 수 텍스트가 연결되지 않았습니다!");
        if(startButton == null)
            Debug.LogError("RoomManager - 방장 플레이어에게 보여질 시작 버튼이 연결되지 않았습니다!");
        if (exitButton == null)
            Debug.LogError("RoomManager - 방에서 퇴장하기 위한 버튼이 연결되지 않았습니다!");
        #endregion
    }

    void OnEnable()
    {
        //서버 커넥터 기준, 방에 입장했을 경우 해당 방에서의 권한을 확인합니다.
        ServerConnector.OnJoinedRoomEvent += HandleJoinedRoom;

        //퇴장 버튼에 방을 떠나도록 하는 코드를 추가합니다.
        exitButton.onClick.AddListener(LeaveRoom);
    }
    void OnDisable()
    {
        //서버 커넥터 기준, 방 입장 후 권한을 확인하는 기능을 제거합니다.
        ServerConnector.OnJoinedRoomEvent -= HandleJoinedRoom;

        //퇴장 버튼을 눌러 씬을 이탈했을 것이므로 해당 코드 또한 제거합니다.
        exitButton.onClick.RemoveListener(LeaveRoom);
    }
    void Start()
    {
        //포톤 네트워크 기준으로 방 안에 있어야 해당 씬이 불러와지므로, 비정상적인 접근을 대비해 우선 예외 처리를 해둡니다.
        if(!PhotonNetwork.InRoom)
        {
            return;
        }

        //지금 이 방의 정보를 포톤 네트워크로부터 받아옵니다.
        var room = PhotonNetwork.CurrentRoom;

        //현재 방의 정보를 받아온 정보와 일치화시킵니다. (UI 업데이트)
        roomIdText.text = room.Name; // 방의 아이디값
        roomTitleText.text = room.CustomProperties["displayname"].ToString(); // 방의 제목
        roomPlayerCountText.text = $"{room.PlayerCount}/{room.MaxPlayers}"; // 방의 플레이어 수
        

    }

    /// <summary>
    /// 버튼에 추가하기 위한 방 나가기 메서드입니다.
    /// 서버 커넥터로부터 방으로 나가는 기능을 수행하도록 합니다.
    /// </summary>
    private void LeaveRoom()
    {
        ServerConnector.Instance.LeaveRoom();
    }

    /// <summary>
    /// 방의 플레이어 등에 변동이 일어날 경우, 플레이어마다 버튼을 다시 갱신합니다.
    /// </summary>
    public void RefreshButton()
    {
        //마스터 클라이언트(방장) 여부를 확인합니다.
        bool isMaster = PhotonNetwork.IsMasterClient;

        //방장이면 시작 버튼을 활성화 합니다.
        startButton.gameObject.SetActive(isMaster);
    }

    /// <summary>
    /// 방 내에서의 권환을 확인합니다.
    /// </summary>
    void HandleJoinedRoom()
    {
        //방장인지 확인합니다.
        bool isMaster = PhotonNetwork.IsMasterClient;

        //방장이면 시작버튼을 활성화 합니다.
        startButton.gameObject.SetActive(isMaster);
    }

    /// <summary>
    /// 슬롯에 플레이어 관련 정보가 갱신될 때 호출할 메서드입니다.
    /// </summary>
    void RefreshPlayerSlots()
    {
        //포톤네트워크의 플레이어 리스트로부터 플레이어 배열을 받아옵니다.
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < slots.Count; i++)
        {
            //현재 존재하는 플레이어의 수보다 i값이 낮으면 존재한다는 것이므로
            if (i < players.Length)
            {
                // 닉네임 표시 코드 작성

                //해당 슬롯 보이도록 변경
                slots[i].gameObject.SetActive(true);
            }

            //현재 존재하는 플레이어 수보다 i값이 높으면 존재하지 않는 것이므로
            else
            {
                // 이미 존재하던 기존 데이터 비우기

                //해당 슬롯 비활성화
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 시작 버튼 관련입니다.
    /// </summary>
    public void StartButton()
    {
        //방장이 아니면 반환합니다.
        if (!PhotonNetwork.IsMasterClient) return;

        //방에 대한 접근을 틀어막습니다.
        PhotonNetwork.CurrentRoom.IsOpen = false;

        
        //인게임 세션으로 들어갑니다.
        PhotonNetwork.LoadLevel("InGameScene");
    }
    /// <summary>
    /// 콜백 함수 - 새 인원이 들어왔을 때의 콜백입니다.
    /// </summary>
    /// <param name="newPlayer">방에 새로 들어온 인원</param>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerCount();
        //BindPlayerToSlot();
        RefreshPlayerSlots();
    }

    /// <summary>
    /// 콜백 함수 - 방에서 나간 인원이 발생했을 때의 콜백입니다.
    /// </summary>
    /// <param name="otherPlayer">나간 인원</param>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCount();
        RefreshPlayerSlots();
    }

    /// <summary>
    /// 콜백 함수 - 방장이 바뀌었을 때 발생하는 콜백입니다.
    /// </summary>
    /// <param name="newMasterClient">새로 임명된 방장</param>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        RefreshButton();
    }

    /// <summary>
    /// 콜백 함수 - 방에 들어왔을 때의 콜백입니다.
    /// </summary>
    public override void OnJoinedRoom()
    {
        RefreshPlayerSlots();
        RefreshButton();
    }

    /// <summary>
    /// 플레이어 수를 갱신합니다.
    /// </summary>
    private void UpdatePlayerCount()
    {
        //방 플레이어의 텍스트는, 현재 인원 / 최대 인원으로 갱신됩니다.
        roomPlayerCountText.text = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
    }

    void BindPlayerToSlot(Player player, TextMeshProUGUI nameText)
    {
        if (player.CustomProperties.TryGetValue("nickname", out object value))
        {
            nameText.text = value.ToString();
        }
        else
        {
            nameText.text = "Loading...";
        }
    }

}
