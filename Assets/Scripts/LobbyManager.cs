using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("방 입장 창 관련")]
    [SerializeField] CanvasGroup enterRoomPanel;
    [SerializeField] TMP_InputField enterRoomIdInput;

    [Header("방 생성용 프리팹")]
    [SerializeField] GameObject roomPrefab;


    //방 생성 시 아이디를 입력하지 않았을 경우 랜덤으로 만들기 위한 문자열 풀입니다.
    private string characterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

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
        if (enterRoomPanel == null)
            Debug.LogError("LobbyManager - 방 입장 시 방 아이디 입력을 위한 패널이 연결되지 않았습니다!");
        if (enterRoomIdInput == null)
            Debug.LogError("LobbyManager - 방 입장 시 방 아이디 입력 칸이 연결되지 않았습니다!");
        if (roomPrefab == null)
            Debug.LogError("LobbyManager - 방 생성 시 출력될 방 입장 버튼 프리팹이 연결되지 않았습니다!");
        if (roomListPanel == null)
            Debug.LogError("LobbyManager - 방 목록 출력용 패널이 연결되지 않았습니다!");
        #endregion
    }

    private System.Collections.IEnumerator Start()
    {
        ConnectToServer();
        while (!PhotonNetwork.IsConnectedAndReady)
            yield return null;

        if(!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            OnJoinedLobby();
        }
        //플레이어에 대한 정보를 우선 받아온 다음 로비에 진입하겠습니다.
        FirebaseDatabaseManager.Instance.LoadPlayerData();

        FirebaseDatabaseManager.Instance.OnPlayerDataChanged += OnPlayerDataLoaded;
    }

    private void OnEnable()
    {
        FirebaseDatabaseManager.Instance.OnPlayerDataChanged += UpdateStats;
    }

    private void OnDisable()
    {
        FirebaseDatabaseManager.Instance.OnPlayerDataChanged -= UpdateStats;
    }

    public void OnPlayerDataLoaded(PlayerData data)
    {
        //받아온 정보 중 다른 사람들에게 식별할 가능성을 줄 수 있는 닉네임을 포톤에 넣어주겠습니다.
        SetMyNicknameToPhoton();
    }

    public void ConnectToServer()
    {
        //PhotonNetwork.GameVersion = "1.0.0";
        ////한국으로 연결합니다.
        //PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";
        //PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("ConnectedToMaster");

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 입장에 성공했습니다. 현재 상태: " + PhotonNetwork.NetworkClientState);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"연결 끊김 발생: {cause}");
    }
    public override void OnLeftLobby()
    {

    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"방 참가 실패 : {message}");

        // 방이 없거나 입장 불가 → 패널 닫기
        PanelStateChange(enterRoomPanel, false);
    }

    public void ExitLobby()
    {
        PhotonNetwork.LeaveLobby();
        SceneManager.LoadScene(0);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    void SetMyNicknameToPhoton()
    {
        string nickname = FirebaseDatabaseManager.Instance.Data.nickname;

        Hashtable props = new Hashtable
    {
        { "nickname", nickname }
    };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    /// <summary>
    /// 방 생성 시의 버튼이 담당할 내용입니다.
    /// </summary>
    public void CreateRoomButtonFunction()
    {
        PanelStateChange(createRoomPanel, true);
    }
    
    /// <summary>
    /// 방 입장 시의 버튼이 담당할 내용입니다.
    /// </summary>
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

        //방은 공개되지 않습니다.
        roomOptions.IsVisible = false;

        //방은 누군가가 들어갈 수 있습니다.
        roomOptions.IsOpen = true;

        //방의 최대 정원은 8명으로 제한합니다.
        roomOptions.MaxPlayers = 8;

        //방 프로퍼티는 Hashtable형 변수입니다. 새롭게 하나 만들어줍니다.
        Hashtable property = new Hashtable();

        //해당 방이 다른 이들에게 보여질 이름을 설정합니다. 사실상 방 내부 인원들에게만 보여질 제목입니다.
        property["displayname"] = roomTitleInput.text;

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
        roomId = string.IsNullOrWhiteSpace(roomIdInput.text)? RandomRoomIdCreation() : roomIdInput.text;
        #endregion

        //포톤에서 해당 ID와 방 옵션을 가지도록 방을 생성합니다.
        PhotonNetwork.CreateRoom(roomId, roomOptions);

        //방이 가지는 옵션들을 플레이어한테 보여줄, 방 입장용 버튼을 방 리스트 패널의 자식 오브젝트로 생성합니다.
        //var room = Instantiate(roomPrefab, roomListPanel);

    }

    public void RoomSearchButtonFunction()
    {
        SearchRoom(enterRoomIdInput.text);
    }

    public void SearchRoom(string text)
    {
        // 입력값 방어
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.Log("방 ID가 입력되지 않았습니다.");
            PanelStateChange(enterRoomPanel, false);
            return;
        }

        // 서버에 연결되어 있지 않다면 시도 불가
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("서버에 연결되어 있지 않습니다.");
            PanelStateChange(enterRoomPanel, false);
            return;
        }

        Debug.Log($"방 참가 시도 : {text}");

        // 방 ID = RoomName 이라고 가정
        PhotonNetwork.JoinRoom(text);
    }

    public void UpdateRoomList()
    {
        
    }

    /// <summary>
    /// 플레이어의 데이터로부터 필요한 정보를 출력합니다.
    /// </summary>
    /// <param name="data">서버상에 기록되어 있는 해당 유저의 데이터</param>
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
