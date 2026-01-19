using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("로비 씬 배치된 오브젝트 관련")]
    [SerializeField] Transform roomListPanel;

    [Header("플레이어 스탯 관련")]
    [SerializeField] TextMeshProUGUI playerNicknameText;
    [SerializeField] TextMeshProUGUI playerKillStatText;
    [SerializeField] TextMeshProUGUI playerGoldText;

    [Header("게임 생성 창 관련")]
    [SerializeField] CanvasGroup createRoomPanel;
    [SerializeField] TMP_InputField roomTitleInput;
    [SerializeField] TMP_InputField roomIdInput;
    [SerializeField] TMP_InputField roomPasswordInput;

    [Header("비밀방 입장 창 관련")]
    [SerializeField] CanvasGroup enterRoomPanel;
    [SerializeField] TMP_InputField enterRoomPasswordInput;

    [Header("방 생성용 프리팹")]
    [SerializeField] GameObject roomPrefab;

    //방 생성 시 아이디를 입력하지 않았을 경우 랜덤으로 만들기 위한 문자열 풀입니다.
    private string characterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    //내부적으로, 방 새로고침 기능이 실행 중임을 확인하기 위한 bool 매개변수입니다.
    private bool isRefreshing = false;

    //방 UI 관리를 위한 딕셔너리입니다.
    private Dictionary <string, RoomButton> roomDictionary = new Dictionary<string, RoomButton>();

    private void Awake()
    {
        if(PhotonNetwork.AutomaticallySyncScene != true)
            PhotonNetwork.AutomaticallySyncScene = true;
        #region 인스펙터 연결 확인용 코드
        if (playerKillStatText == null)
            Debug.LogWarning("LobbyManager - 플레이어 킬 수 스탯 표기를 위한 텍스트가 연결되지 않았습니다!");
        if (playerGoldText == null)
            Debug.LogWarning("LobbyManager - 플레이어 보유 골드 수 표기를 위한 텍스트가 연결되지 않았습니다!");
        if (playerNicknameText == null)
            Debug.LogWarning("LobbyManager - 플레이어 닉네임 표기를 위한 텍스트가 연결되지 않았습니다!");
        if (createRoomPanel == null)
            Debug.LogError("LobbyManager - 방 생성 패널이 연결되지 않았습니다!");
        if (roomTitleInput == null)
            Debug.LogError("LobbyManager - 방 생성 시의 제목 입력 칸이 연결되지 않았습니다!");
        if (roomIdInput == null)
            Debug.LogError("LobbyManager - 방 생성 시의 방 아이디 입력 칸이 연결되지 않았습니다!");
        if (roomPasswordInput == null)
            Debug.LogError("LobbyManager - 방 생성 시의 비밀번호 입력 칸이 연결되지 않았습니다!");
        if (enterRoomPanel == null)
            Debug.LogError("LobbyManager - 비밀방 입장 시 비밀번호 입력을 위한 패널이 연결되지 않았습니다!");
        if (enterRoomPasswordInput == null)
            Debug.LogError("LobbyManager - 비밀방 입장 시 비밀번호 입력 칸이 연결되지 않았습니다!");
        if (roomPrefab == null)
            Debug.LogError("LobbyManager - 방 생성 시 출력될 방 입장 버튼 프리팹이 연결되지 않았습니다!");
        if (roomListPanel == null)
            Debug.LogError("LobbyManager - 방 목록 출력용 패널이 연결되지 않았습니다!");
        #endregion
    }

    private void Start()
    {
        //플레이어에 대한 정보를 우선 받아온 다음 로비에 진입하겠습니다.
        FirebaseDatabaseManager.Instance.LoadPlayerData();

        Debug.Log(PhotonNetwork.NetworkClientState);


    }

    private void OnEnable()
    {
        FirebaseDatabaseManager.Instance.OnPlayerDataChanged += UpdateStats;
    }

    private void OnDisable()
    {
        FirebaseDatabaseManager.Instance.OnPlayerDataChanged -= UpdateStats;
    }

    public override void OnLeftLobby()
    {
        //새로고침을 목적으로 나온 경우에는 아래 코드를 실행합니다.
        if(isRefreshing == true)
        {
            //새로고침을 진행했다는 표시로 다시 새로고침 진행 여부를 거짓으로 돌려놓습니다.
            isRefreshing = false;

            //포톤 네트워크상으로 로비로 진입하는 것을 요청합니다.
            PhotonNetwork.JoinLobby();
        }
    }

    public void ExitLobby()
    {
        PhotonNetwork.LeaveLobby();
        SceneManager.LoadScene(0);
    }

    public void CreateRoomButtonFunction()
    {
        PanelStateChange(createRoomPanel, true);
    }
    
    public void EnterRoomButtonFunction()
    {
        PanelStateChange(enterRoomPanel, true);
    }

    /// <summary>
    /// 방 옵션을 설정하여 반환하는 메서드입니다.
    /// </summary>
    /// <returns></returns>
    private RoomOptions RoomOptionCreation()
    {
        //방 옵션을 새로 형성합니다.
        RoomOptions roomOptions = new RoomOptions();

        //방은 모두에게 공개됩니다.
        roomOptions.IsVisible = true;

        //방은 누군가가 들어갈 수 있습니다.
        roomOptions.IsOpen = true;

        //방의 최대 정원은 8명으로 제한합니다.
        roomOptions.MaxPlayers = 8;

        //방 프로퍼티는 Hashtable형 변수입니다. 새롭게 하나 만들어줍니다.
        Hashtable property = new Hashtable();

        //해당 방이 다른 이들에게 보여질 이름을 설정합니다.
        property["displayname"] = roomTitleInput.text;

        //방 생성 시 비밀번호를 적어두었다면
        if(roomPasswordInput.text != null && roomPasswordInput.text != "")
        {
            //비밀번호를 가지고 있는 것이 참이므로
            property["hasPassword"] = true;
            //패스워드를 저장해줍니다.
            property["password"] = roomPasswordInput.text;
        }

        //적어두지 않았다면
        else
        {
            //비밀번호 소지 여부는 거짓이 됩니다.
            property["hasPassword"] = false;
        }

        //방의 커스텀 프로퍼티를 방금 생성한 프로퍼티로 바꾸어줍니다.
        roomOptions.CustomRoomProperties = property;

        //이렇게 완성된 방 옵션을 반환합니다.
        return roomOptions;
    }

    /// <summary>
    /// 방 ID를 랜덤으로 생성하여 주는 메서드입니다.
    /// </summary>
    /// <returns>방의 ID값으로 들어갈 랜덤한 6자리 문자열</returns>
    private string RandomRoomIdCreation()
    {
        //랜덤한 값을 뽑아 저장할 char형 변수를 6개 담을 공간을 만듭니다.
        char[] roomId = new char[6];
        
        //해당 공간의 길이만큼 아래 코드를 실행합니다.
        for(int i = 0; i < roomId.Length; i++)
        {
            //ID값을 만들 떄 사용할 수 있는 종류를 담은 문자열로부터, 해당 문자열 길이만큼 랜덤값을 돌려
            //해당 문자열의 문자 중 하나를 값으로 저장합니다.
            roomId[i] = characterPool[Random.Range(0, characterPool.Length)];
        }
        
        //과정이 끝나 6개의 문자를 담은 배열을 문자열로 변경하여 반환합니다.
        return new string(roomId);
    }

    public void CreateRoom()
    {
        if (!PhotonNetwork.InLobby) return;
        #region 방 옵션 설정
        //방 옵션을 새롭게 생성합니다.
        RoomOptions roomOptions = RoomOptionCreation();

        //방이 로비에서 사용할 커스텀 프로퍼티는 다음과 같습니다.
        roomOptions.CustomRoomPropertiesForLobby = new string[]
        {
            "displayname", // 이름 출력
            "hasPassword" // 패스워드 소지 여부 확인
        };
        #endregion

        #region 방 아이디 설정
        string roomId; // 방을 구별할 수 있도록 각자가 고유한 방 ID를 갖도록 합니다.

        //방 생성 시에 텍스트를 입력하지 않은 경우, 방 아이디를 랜덤으로 생성합니다.
        if (roomIdInput.text == null || roomIdInput.text == "")
        {
            roomId = RandomRoomIdCreation();
        }
        //입력한 경우, 해당 아이디를 가진 채로 생성하도록 합니다.
        else
        {
            roomId = roomIdInput.text;
        }
        #endregion

        //포톤에서 해당 ID와 방 옵션을 가지도록 방을 생성합니다.
        PhotonNetwork.CreateRoom(roomId, roomOptions);

        ////방이 가지는 옵션들을 플레이어한테 보여줄, 방 입장용 버튼을 방 리스트 패널의 자식 오브젝트로 생성합니다.
        //var room = Instantiate(roomPrefab, roomListPanel);

    }
    
    public void UpdateRoomList()
    {
        //이 기능은 로비에서만 사용할 것이므로, 로비 이외의 공간일 경우 뒤 코드는 실행하지 않습니다.
        if (!PhotonNetwork.InLobby) return;

        //새로고침이 진행중입니다.
        isRefreshing = true;

        //현재까지 만들어져 있는 방 UI를 초기화합니다.
        //ClearRoomListUI();

        //로비에서 임시로 나갑니다.
        PhotonNetwork.LeaveLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        //방 리스트에 있는 각 방들의 정보로부터 다음 코드를 실행합니다.
        foreach(RoomInfo roomInfo in roomList)
        {
            //방이 리스트로부터 삭제된 경우, 즉 방 내에 사람이 존재하지 않는 경우
            if (roomInfo.RemovedFromList)
            {
                //Dictionary로부터 해당 키가 있다면
                if (roomDictionary.ContainsKey(roomInfo.Name))
                {
                    //해당 키를 파괴하고
                    Destroy(roomDictionary[roomInfo.Name]);
                    //Dictionary에서 해당 키와 값을 없앤 뒤
                    roomDictionary.Remove(roomInfo.Name);
                }
                //다음 방 정보로 이동합니다.
                continue;
            }

            //방 Dictionary가 해당 방의 이름을 Key값으로 갖고 있지 않은 경우, 즉 방이 새로 생성된 경우
            if(!roomDictionary.ContainsKey(roomInfo.Name))
            {
                //해당 방에 대한 프리팹을 생성합니다. (버튼)
                var room = Instantiate(roomPrefab, roomListPanel);

                //생성한 방 버튼으로부터, roomButton 클래스파일을 불러옵니다.
                var roomButton = room.GetComponent<RoomButton>();

                //해당 roomButton 클래스로부터, 현재 방의 정보에 대한 값을 출력하도록 하는 기능을 실행합니다.
                roomButton.SetData(roomInfo);

                //그렇게 완성된 방을 Dictionary에 추가합니다.
                roomDictionary.Add(roomInfo.Name, roomButton);
            }

            //방 Dictionary가 해당 방의 이름을 Key값으로 가지고 있으며, 삭제된 경우도 아닌 경우
            //방에 대한 정보를 다시 한번 출력시킵니다.
            else
                roomDictionary[roomInfo.Name].SetData(roomInfo);
        }
    }

    public void UpdateStats(PlayerData data)
    {
        playerNicknameText.text = data.nickname;
        playerKillStatText.text = data.totalKills.ToString();
        playerGoldText.text = data.gold.ToString();
    }
    /// <summary>
    /// 캔버스 그룹과 bool값을 받아, 해당 캔버스 그룹의 활성화와 비활성화를 담당해줄 메서드입니다.
    /// </summary>
    /// <param name="panel">값을 조절할 캔버스 그룹을 가진 패널</param>
    /// <param name="state">해당 패널의 활성화 여부</param>
    public void PanelStateChange(CanvasGroup panel, bool state)
    {
        if (state == true)
        {
            panel.alpha = 1.0f; // 알파값을 1로 설정하여 온전히 화면에 보이게 합니다.
            panel.interactable = true; // 상호작용 여부를 참으로 설정하여 유저가 해당 패널과 상호작용이 가능하게 합니다.
            panel.blocksRaycasts = true; // 레이캐스트 제한을 참으로 설정하여 해당 패널 뒤에 있는 것들에 대한 작업을 방어합니다.
        }
        else if (state == false)
        {
            panel.alpha = 0f; // 알파값을 0으로 설정하여 화면에서 완전히 모습을 감추게 합니다.
            panel.interactable = false; // 상호작용 여부를 거짓으로 설정하여 해당 패널에 대해 상호작용을 하지 못하게 제한합니다.
            panel.blocksRaycasts = false; // 레이캐스트 제한을 거짓으로 설정하여 해당 패널 뒤의 오브젝트들에 대한 작업이 가능하도록 합니다.
        }
    }
}
