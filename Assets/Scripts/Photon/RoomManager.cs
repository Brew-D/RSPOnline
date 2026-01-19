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
    [SerializeField] Image locked;
    [SerializeField] Transform[] spawnPoints;

    [Header("준비 / 시작 버튼")]
    [SerializeField] Button readyButton;
    [SerializeField] Button startButton;
    [SerializeField] TextMeshProUGUI readyText;

    [Header("퇴장 버튼 관련")]
    [SerializeField] Button exitButton;

    [Header("플레이어 캐릭터 오브젝트")]
    [SerializeField] GameObject playerPrefab;

    [Header("각 슬롯별 이미지 연결")]
    [SerializeField] List<RoomPlayerSlot> slots;
    [SerializeField] List<Image> slotReadyImages;

    private bool isReady = false;
    Dictionary<Player, RoomPlayerSlot> slotMap = new Dictionary<Player, RoomPlayerSlot>();

    void Awake()
    {
        #region 인스펙터 연결 확인용 코드
        if (roomIdText == null)
            Debug.LogError("RoomManager - 방 아이디 텍스트가 연결되지 않았습니다!");
        if (roomTitleText == null)
            Debug.LogError("RoomManager - 방 제목 텍스트가 연결되지 않았습니다!");
        if (roomPlayerCountText == null)
            Debug.LogError("RoomManager - 방 인원 수 텍스트가 연결되지 않았습니다!");
        if (locked == null)
            Debug.LogError("RoomManager - 비밀방 여부를 나타낼 이미지가 연결되지 않았습니다!");
        if(readyButton == null)
            Debug.LogError("RoomManager - 방장 이외의 플레이어에게 보여질 준비 버튼이 연결되지 않았습니다!");
        if(startButton == null)
            Debug.LogError("RoomManager - 방장 플레이어에게 보여질 시작 버튼이 연결되지 않았습니다!");
        if(readyText == null)
            Debug.LogError("RoomManager - 방장 플레이어에게 보여질 준비 인원 수 텍스트가 연결되지 않았습니다!");
        if (spawnPoints == null)
            Debug.LogError("RoomManager - 플레이어 생성 장소가 연결되지 않았습니다!");
        if (playerPrefab == null)
            Debug.LogError("RoomManager - 생성할 플레이어 캐릭터 프리팹이 연결되지 않았습니다!");
        if (exitButton == null)
            Debug.LogError("RoomManager - 방에서 퇴장하기 위한 버튼이 연결되지 않았습니다!");
        #endregion
    }

    //플레이어에게 슬롯을 지정해 줍니다.
    void AssignSlot(Player player, RoomPlayerSlot slot)
    {
        //해당 슬롯에 플레이어의 정보를 담도록 합니다.
        slot.BindPlayer(player);

        //슬롯맵의 플레이어를 키로서 받으면 해당 슬롯을 값으로 반환하도록 합니다. 
        slotMap[player] = slot;
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
        
        //해당 방의 커스텀 프로퍼티로, "비밀번호 소지 여부"가 참인 경우, 자물쇠 표시를 활성화하여 비밀번호가 있다는 것을 표기해줍니다.
        if ((bool)room.CustomProperties["hasPassword"] == true)
        {
            locked.gameObject.SetActive(true);
        }

        //해당 방의 커스텀 프로퍼티로, "비밀번호 소지 여부"가 거짓인 경우, 자물쇠 표시를 출력해줄 필요는 없습니다..
        else
        {
            locked.gameObject.SetActive(false);
        }

        //방 내에 플레이어의 캐릭터를 생성합니다.
        SpawnPlayer(PhotonNetwork.LocalPlayer);
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
    /// 플레이어를 인자값으로 받아, 해당 플레이어의 캐릭터를 생성합니다.
    /// </summary>
    /// <param name="targetPlayer">캐릭터를 생성할 플레이어</param>
    private void SpawnPlayer(Player targetPlayer)
    {
        //액터 넘버는 플레이어가 해당 방에 입장한 순서대로 매겨지는 숫자입니다.
        //원하는 로직은 로컬 플레이어의 전용 공간에 해당 플레이어의 캐릭터를 소환하는 것이므로
        //로컬 플레이어의 숫자만 빼내면 됩니다.
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        //플레이어는 상시 전용 공간에 위치해야 합니다. 배열의 가장 앞 부분으로 빼놓을 것이니 해당 위치로 지정합시다.
        Vector3 spawnPos = spawnPoints[0].position;

        //각 플레이어들에게 내 캐릭터가 확인될 수 있도록, 플레이어 프리팹을 생성합니다.
        GameObject player = PhotonNetwork.Instantiate(
            playerPrefab.name, spawnPos, Quaternion.identity
            );
        //플레이어 조작 등과 관련된 기능인, 플레이어매니저를 우선 가져옵니다.
        PlayerManager playerCtrl = player.GetComponent<PlayerManager>();
        //중력을 비활성화하여 떨어지지 않도록 합니다.
        playerCtrl.SetGravity(false);
    }

    /// <summary>
    /// 플레이어의 위치를 갱신합니다.
    /// </summary>
    public void RefreshPlayerPos()
    {
        //자신의 슬롯 위치를 확인합니다.
        Transform mySlot = spawnPoints[0];

        //자신의 캐릭터의 모델컨트롤러를 반환받습니다.
        RoomPlayerModelController mine = FindLocalModel();
        //위치를 확인한 슬롯으로 이동시킵니다.
        mine.MoveSlot(mySlot);

        //방 내의 모든 플레이어 정보를 받아옵니다.
        Player[] others = PhotonNetwork.PlayerListOthers;

        //그 길이만큼 아래의 코드를 실행합니다.
        for(int i = 0; i< others.Length; i++)
        {
            //각각의 플레이어를 받아옵니다.
            RoomPlayerModelController players = FindPlayerModel(others[i]);
            //해당 플레이어의 전용 슬롯으로 이동합니다.
            players.MoveSlot(spawnPoints[i + 1]);
        }
    }

    /// <summary>
    /// 방의 플레이어 등에 변동이 일어날 경우, 플레이어마다 버튼을 다시 갱신합니다.
    /// </summary>
    public void RefreshButton()
    {
        //마스터 클라이언트(방장) 여부를 확인합니다.
        bool isMaster = PhotonNetwork.IsMasterClient;

        //방장이면 시작, 방장이 아니면 준비 버튼을 사용하도록 합니다.
        startButton.gameObject.SetActive(isMaster);
        readyButton.gameObject.SetActive(!isMaster);
    }

    /// <summary>
    /// 방 내에서의 권환을 확인합니다.
    /// </summary>
    void HandleJoinedRoom()
    {
        //방장인지 확인합니다.
        bool isMaster = PhotonNetwork.IsMasterClient;

        //방장이면 시작, 방장이 아니면 준비 버튼을 사용하도록 합니다.
        readyButton.gameObject.SetActive(!isMaster);
        startButton.gameObject.SetActive(isMaster);

        //방장이 아닌 경우, 준비 여부를 거짓으로 설정합니다.
        if (!isMaster)
            SetReady(false);

        //준비 인원을 갱신합니다.
        UpdateReadyCountUI();
    }

    /// <summary>
    /// 준비 상태에 변화가 있을 경우 해당 변화를 적용합니다.
    /// </summary>
    /// <param name="ready">준비 여부</param>
    public void SetReady(bool ready)
    {
        //준비 값을 가지는 해시테이블에, 준비 bool값을 넣습니다.
        Hashtable props = new Hashtable
        {
            { "Ready", ready }
        };

        //로컬 플레이어 기준, 커스텀프로퍼티에 준비 상태 변화를 적용합니다.
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    /// <summary>
    /// 준비한 유저의 수를 확인하는 UI를 업데이트합니다.
    /// </summary>
    void UpdateReadyCountUI()
    {
        //준비 인원과 방 내 총 인원을 확인합니다.
        int ready = GetReadyPlayerCount();
        int total = GetTargetPlayerCount();

        //준비 인원 텍스트는, 준비 인원과 총 인원을 출력하도록 합니다.
        readyText.text = $"준비 : {ready} / {total}";

        //방장 한정으로, 시작 버튼은 준비 인원과 총 인원이 동일할 때만 상호 작용이 가능하도록 합니다.
        if(PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = (ready == total);
        }
    }

    /// <summary>
    /// 준비 버튼 관련입니다.
    /// </summary>
    public void ReadyButton()
    {
        //현재 준비 여부는 거짓입니다.
        bool currentReady = false;

        //해당 플레이어의 커스텀 프로퍼티를 확인하여, 준비 상태의 값을 확인합니다.
        if(PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Ready", out object value))
        {
            //그 값을 일단 가져옵니다.
            currentReady = (bool)value;
        }

        //준비를 하지 않았다면 준비 상태로, 준비 상태였다면 미 준비 상태로 전환합니다.
        SetReady(!currentReady);
    }

    /// <summary>
    /// 시작 버튼 관련입니다.
    /// </summary>
    public void StartButton()
    {
        //방장이 아니면 반환합니다.
        if (!PhotonNetwork.IsMasterClient) return;

        //모든 플레이어가 준비를 마치지 않았을 경우 반환합니다.
        if (!AreAllPlayersReady()) return;

        //방에 대한 접근을 틀어막습니다.
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        //인게임 세션으로 들어갑니다.
        PhotonNetwork.LoadLevel("InGameScene");
    }

    /// <summary>
    /// 모든 플레이어가 준비했는지 여부를 반환합니다.
    /// </summary>
    /// <returns>모든 플레이어가 준비했을 경우 참, 한 명이라도 준비 상태가 아니면 거짓</returns>
    bool AreAllPlayersReady()
    {
        //준비 인원과 총 인원이 같은지 여부를 반환합니다.
        return GetReadyPlayerCount() == GetTargetPlayerCount();
    }

    /// <summary>
    /// 콜백 함수 - 새 인원이 들어왔을 때의 콜백입니다.
    /// </summary>
    /// <param name="newPlayer">방에 새로 들어온 인원</param>
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        UpdatePlayerCount();
        UpdateReadyCountUI();
    }

    /// <summary>
    /// 콜백 함수 - 방에서 나간 인원이 발생했을 때의 콜백입니다.
    /// </summary>
    /// <param name="otherPlayer">나간 인원</param>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCount();
        UpdateReadyCountUI();
    }

    /// <summary>
    /// 콜백 함수 - 방장이 바뀌었을 때 발생하는 콜백입니다.
    /// </summary>
    /// <param name="newMasterClient">새로 임명된 방장</param>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdateButtonText();
    }

    /// <summary>
    /// 콜백 함수 - 방에 들어왔을 때의 콜백입니다.
    /// </summary>
    public override void OnJoinedRoom()
    {
        AssignAllPlayersToSlots();
    }

    /// <summary>
    /// 콜백 함수 - 플레이어의 프로퍼티가 업데이트되었을 때 호출되는 콜백입니다.
    /// </summary>
    /// <param name="targetPlayer">대상 플레이어</param>
    /// <param name="changedProps">변경 사항이 존재하는 프로퍼티</param>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        //변경된 사항이 "Ready"를 포함하지 않은 경우 반환합니다.
        if (!changedProps.ContainsKey("Ready")) return;

        //슬롯맵에서 해당 플레이어를 키값으로 갖는 슬롯 값을 받아옵니다.
        if (slotMap.TryGetValue(targetPlayer, out RoomPlayerSlot slot))
        {
            //해당 슬롯의 준비 상태를 갱신합니다.
            slot.UpdateReadyState();
        }
    }

    /// <summary>
    /// 플레이어 수를 갱신합니다.
    /// </summary>
    private void UpdatePlayerCount()
    {
        //방 플레이어의 텍스트는, 현재 인원 / 최대 인원으로 갱신됩니다.
        roomPlayerCountText.text = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
    }

    /// <summary>
    /// 버튼의 글씨를 변경합니다. (변경이 필요한 건 현재 씬에서는 준비 버튼 뿐입니다.)
    /// </summary>
    private void UpdateButtonText()
    {
        //준비 상태가 아닌 경우 준비
        if (isReady != true)
            readyText.text = "준비";

        //준비 상태인 경우 취소
        else
            readyText.text = "준비 취소";
    }

    /// <summary>
    /// 모든 슬롯에 플레이어를 등록합니다.
    /// </summary>
    private void AssignAllPlayersToSlots()
    {
        //슬롯맵 딕셔너리를 우선 초기화합니다.
        slotMap.Clear();

        //플레이어 리스트를 받아옵니다.
        Player[] players = PhotonNetwork.PlayerList;

        //그 길이만큼 아래 코드를 실행합니다.
        for(int i = 0; i < players.Length; i++)
        {
            //슬롯의 총 수보다 플레이어 수가 많으면 오류가 발생한 것으로 간주, 그만두고 나옵니다.
            if (i >= slots.Count) break;

            //각 숫자별 슬롯과 준비 상태 이미지를 저장합니다.
            RoomPlayerSlot slot = slots[i];
            Image readyImage = slotReadyImages[i];

            //슬롯에 플레이어 및 준비 이미지를 지정합니다.
            slot.Initialize(players[i], readyImage);
        }
    }

    /// <summary>
    /// 지정받은 플레이어를 찾아 해당 플레이어의 크기 조절 클래스를 반환합니다.
    /// </summary>
    /// <param name="player">찾고자 하는 플레이어</param>
    /// <returns></returns>
    private RoomPlayerModelController FindPlayerModel(Player player)
    {
        //방 내의 플레이어 모델 컨트롤러 클래스 배열 플레이어 - 종류 구분 없이 일단 싹 가져옵니다.
        RoomPlayerModelController[] players =
            FindObjectsByType<RoomPlayerModelController>(FindObjectsSortMode.None);

        //해당 배열 내의 모든 플레이어 모델에 대해 아래 코드를 실행합니다.
        foreach(var pModel in players)
        {
            //포톤 뷰를 찾아옵니다.
            PhotonView pv = pModel.GetComponent<PhotonView>();
            
            //포톤 뷰가 존재하며, 해당 포톤 뷰의 소유자가 찾고자 하는 플레이어와 동일할 경우 해당 플레이어 모델을 반환합니다.
            if (pv != null && pv.Owner == player)
            {
                return pModel;
            }
        }
        //여기까지 온 거면 실패한 것이므로 null을 반환합니다.
        return null;
    }

    /// <summary>
    /// 플레이어가 직접 조작하는 플레이어 모델을 반환합니다.
    /// </summary>
    /// <returns>현재 기기에서 게임을 하는 플레이어를 나타내는 모델.</returns>
    private RoomPlayerModelController FindLocalModel()
    {
        //방 내의 플레이어 모델 컨트롤러 클래스 배열 플레이어 - 종류 구분 없이 일단 싹 가져옵니다.
        RoomPlayerModelController[] players =
            FindObjectsByType<RoomPlayerModelController>(FindObjectsSortMode.None);
        
        //해당 배열 내의 모든 플레이어 모델에 대해 아래 코드를 실행합니다.
        foreach (var pModel in players)
        {
            //포톤 뷰를 찾아옵니다.
            PhotonView pv = pModel.GetComponent<PhotonView>();

            //포톤 뷰가 존재하며, 해당 포톤 뷰가 자신의 것인 경우 해당 플레이어 모델을 반환합니다.
            if (pv != null && pv.IsMine == true)
            {
                return pModel;
            }
        }
        //여기까지 온 거면 실패한 것이므로 null을 반환합니다.
        return null;
    }

    /// <summary>
    /// 준비 상태의 플레이어 수를 반환합니다.
    /// </summary>
    /// <returns>준비 상태의 플레이어 수</returns>
    private int GetReadyPlayerCount()
    {
        //초기 값을 0으로 잡습니다.
        int count = 0;
        //준비 여부 또한 우선 기본값인 거짓으로 둡니다.
        bool isReady = false;

        //플레이어 리스트에 잡히는 플레이어마다 아래 코드를 시행합니다.
        foreach(var player in PhotonNetwork.PlayerList)
        {
            //마스터 클라이언트는 준비 버튼이 존재하지 않으므로 체크할 대상에서 제외합니다.
            if (player.IsMasterClient)
                continue;

            //플레이어의 커스텀 프로퍼티에서 준비 값을 받아옵니다.
            if(player.CustomProperties.TryGetValue("Ready", out object value))
            {
                isReady = (bool)value;
            }

            //준비 값이 참이라면, 준비 인원 수를 1 올립니다.
            if(isReady == true)
                count++;
        }
        //모든 플레이어에게 해당 과정이 끝났다면 총 수를 반환합니다.
        return count;
    }

    /// <summary>
    /// 방 내에서 준비를 할 수 있는 총 인원 수를 반환합니다.
    /// </summary>
    /// <returns>방장 이외의 플레이어 수</returns>
    private int GetTargetPlayerCount()
    {
        //초기 값을 0으로 지정합니다.
        int count = 0;

        //플레이어 리스트에 잡히는 플레이어마다 아래 코드를 시행합니다.
        foreach (var player in PhotonNetwork.PlayerList)
        {
            //플레이어가 방장이 아닌 경우, 인원 수를 1 올립니다.
            if (!player.IsMasterClient)
                count++;
        }
        //모든 플레이어에게 해당 과정이 끝났다면 총 수를 반환합니다.
        return count;
    }
}
