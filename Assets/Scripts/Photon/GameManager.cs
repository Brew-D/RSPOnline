using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public enum GameState
{
    Select, Ready, Preparing, Fight
}

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    [Header("생성할 플레이어의 모델 프리팹")]
    [SerializeField] GameObject playerPrefab;

    [Header("무기 오브젝트 배열")]
    public GameObject[] allWeapons;

    public GameState state; //  GameState. 현재 게임 상태를 저장하기 위한 매개변수.

    List<PlayerManager> readyPlayers = new List<PlayerManager>(); // 무기를 선택 완료한 플레이어의 리스트.
    HashSet<PlayerManager> weaponSelectedPlayers = new();         // 무기를 선택한 플레이어를 찾기 위한 HashSet.
    
    //8명만 들어올 수 있으므로 8개의 스폰 포인트 지정. 플레이하는 곳의 바닥이 사각형이므로 스폰 지점도 사각형 기반으로 사용.
    private Vector3[] spawnPoints = {
    new(-60,5,0), new(60,5,0),
    new(0,5,60), new(0,5,-60),
    new(-60,5,60), new(60,5,60),
    new(-60,5,-60), new(60,5,-60)
    };

    //위의 spawnPoints 배열을 사용할 때, 해당 차례에 사용할 배열에서의 Index.
    private int nextSpawnIndex = 0;
    //무기 선택을 완료한 플레이어의 수.
    private int readyPlayerCount = 0;

    void Start()
    {
        //싱글톤 패턴을 사용합니다. 단, 하나의 게임 세션 내에서만 존재할 것이므로 파괴 방지는 하지 않습니다.
        Instance = this;
        //플레이어매니저가 조작하는 플레이어가 없을 경우, 아래 코드를 실행합니다.
        if(PlayerManager.LocalPlayerInstance == null)
        {
            //코루틴 - 연결 후 플레이어 생성.
            StartCoroutine(SpawnPlayerWhenConnected());
        }
    }

    /// <summary>
    /// 플레이어가 무기를 선택하였을 때의 행동입니다..
    /// </summary>
    /// <param name="player">무기를 선택한 플레이어</param>
    public void OnPlayerWeaponSelected(PlayerManager player)
    {
        if (player.IsReady)
            return;

        player.ChangeReadyState(true);

        //스폰 포인트를 할당하기 위해, Index를 받아옵니다.
        int spawnIndex = nextSpawnIndex;
        //할당하고 나면 다음 인원이 이동할 스폰포인트를 변경하기 위해 스폰포인트 배열의 index를 1 올립니다.
        nextSpawnIndex++;

        //스폰 포인트는 spawnPoints 배열 속, 할당받은 Index입니다.
        Vector3 spawnPos = spawnPoints[spawnIndex];

        //RPC - 플레이어를 스폰지점으로 이동시킵니다.
        player.photonView.RPC(
        "RPC_MoveToSpawn",
        player.photonView.Owner,
        spawnPos
        );

        Camera.main.cullingMask =
        LayerMask.GetMask("Player", "HiddenPlayer", "Default");

        //준비 인원 수를 높입니다.
        readyPlayerCount++;

        //전원 준비 완료 상태라면, 게임 시작 상태로 돌입합니다.
        if (readyPlayerCount == PhotonNetwork.PlayerList.Length)
        {
            StartCoroutine(StartGameSequence());
        }
    }


    /// <summary>
    /// 무기를 골랐을 때의 콜백입니다.
    /// </summary>
    public void OnSelectWeapon()
    {
        //RPC - 무기를 골랐다는 것을 방장에게 알립니다. ActorNumber을 넘김으로서 해당 ActorNumber을 가진
        //플레이어에게 방장이 무기를 고른 이후의 행동을 지시하게 합니다.
        photonView.RPC(
            "RPC_NotifyWeaponSelected",
            RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber
        );
    }

    public override void OnPlayerPropertiesUpdate(
    Player targetPlayer,
    ExitGames.Client.Photon.Hashtable changedProps)
    {
        Debug.Log($"[PropsUpdate] player={targetPlayer.ActorNumber}");

        if (!changedProps.ContainsKey("WeaponType"))
            return;

        Debug.Log($"[PropsUpdate] WeaponType changed");

        if (targetPlayer.TagObject == null)
        {
            Debug.LogError("TagObject is NULL!");
            return;
        }

        PlayerManager player = targetPlayer.TagObject as PlayerManager;
        Debug.Log($"PlayerManager found: {player.name}");

        WeaponType type =
            (WeaponType)(int)changedProps["WeaponType"];


        player.SelectWeapon(type);
        OnPlayerWeaponSelected(player);
    }

    /// <summary>
    /// 스폰 지점으로 이동하는 메서드입니다.
    /// </summary>
    /// <param name="pos">이동할 스폰 지점 위치</param>
    public void MoveToSpawnPoint(Vector3 pos)
    {
        transform.position = pos;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
    }

    /// <summary>
    /// 게임을 시작합니다.
    /// </summary>
    /// <returns></returns>
    IEnumerator StartGameSequence()
    {
        //모두가 준비(무기 선택) 완료 상태라면, 게임 시작을 기다리는 준비 상태로 바꿉니다.
        ChangeState(GameState.Preparing);

        //3초 기다립니다.
        yield return new WaitForSeconds(3f);

        //모든 플레이어를 바닥에 떨어뜨립니다.
        DropAllPlayers();

        //전투 상태로 돌입합니다.
        ChangeState(GameState.Fight);
    }

    /// <summary>
    /// 모든 플레이어를 공중에 고정 상태에서 해제하여 이동을 풀어주는 메서드입니다.
    /// </summary>
    void DropAllPlayers()
    {
        //씬에 존재하는 모든 플레이어매니저를 대상으로 아래 코드를 실행합니다.
        foreach (var player in FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
        {
            //플레이어들을 전부 떨어뜨립니다.
            player.StartDrop();
            //플레이어의 준비 상태를 해제하여 조작이 가능하게 합니다.
            //이 준비 상태는, 무기를 고르고 추가 조작이 일어나는 걸 막기 위해 넣어준 bool 매개변수입니다.
            player.ChangeReadyState(false);
        }
    }

    /// <summary>
    /// 현재의 게임 상태를 변경합니다.
    /// </summary>
    /// <param name="gs">변경할 GameState</param>
    private void ChangeState(GameState gs)
    {
        state = gs;
    }

    /// <summary>
    /// 플레이어의 캐릭터를 맵에 생성하는 코루틴입니다.
    /// </summary>
    /// <returns>완성된 캐릭터 출력. 반환값 없음.</returns>
    IEnumerator SpawnPlayerWhenConnected()
    {
        //플레이어가 방에 있는지 확인할 때까지 기다립니다. (보통 게임씬일 테니 방에 있을 것입니다)
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        //플레이어가 조작할 캐릭터는, 포톤네트워크에서 생성하는, (Resources 폴더 내의)플레이어 프리팹을, 높이 5의 공간에, 회전 없이 생성합니다.
        PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0, 2000, 2203), Quaternion.identity);
    }

    /// <summary>
    /// 콜백 함수 - 방을 떠날 때의 콜백입니다.
    /// </summary>
    public override void OnLeftRoom()
    {
        //우선 로비 씬으로 이동시킵니다.
        SceneManager.LoadScene(1);
    }

    /// <summary>
    /// 콜백 함수 - 게임이 시작했을 때의 콜백입니다.
    /// </summary>
    public void OnGameStart()
    {
        //플레이어는, 지금 기기를 조작중인 플레이어가 다룰 수 있는 캐릭터입니다.
        var player = FindLocalPlayer();
        //해당 플레이어의 캐릭터가 존재할 경우, 아래 코드를 실행합니다.
        if (player != null)
        {
            //플레이어로부터 플레이어매니저 클래스를 가져와, 중력을 활성화시켜 조작 방지를 해제합니다.
            player.GetComponent<PlayerManager>().SetGravity(true);
        }
    }

    /// <summary>
    /// 방을 떠날 때의 메서드입니다.
    /// </summary>
    public void LeaveRoom()
    {
        //만약 방에 있는 상태가 아니라면, 여기서 호출되어야 하는 코드가 아닙니다. 반환을 진행합니다.
        if (!PhotonNetwork.InRoom)
            return;
        //포톤네트워크상에서 클라이언트가 방을 나가는 도중이라면 굳이 또 시행할 필요 없으니 반환합니다.
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Leaving)
            return;
        //방을 떠나도록 지시합니다.
        PhotonNetwork.LeaveRoom();
    }



    /// <summary>
    /// 로컬 플레이어, 기기를 조작하는 플레이어가 조작하는 캐릭터를 찾아 그 게임오브젝트를 반환합니다.
    /// </summary>
    /// <returns>플레이어가 조작할 게임 오브젝트</returns>
    GameObject FindLocalPlayer()
    {
        //플레이어매니저 클래스를 가진 모든 오브젝트들을 정렬조건 없이 찾아 아래 코드를 실행합니다.
        foreach (var playerChar in FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
        {
            //그 플레이어가 내가 조작하는 캐릭터였을 경우 그 캐릭터를 반환합니다.
            if (playerChar.photonView.IsMine)
                return playerChar.gameObject;
        }
        //내가 조작하는 캐릭터가 없으면 null을 반환합니다.
        return null;
    }

    /// <summary>
    /// ActorNumber를 기반으로 하여 해당 플레이어를 찾아냅니다.
    /// </summary>
    /// <param name="actorNumber"></param>
    /// <returns></returns>
    PlayerManager FindPlayerByActorNumber(int actorNumber)
    {
        //모든 플레이어를 배열에 담습니다.
        PlayerManager[] players =
            FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        //배열 속 모든 플레이어를 대상으로 아래 코드를 실행합니다.
        foreach (var pm in players)
        {
            //각 플레이어의 PhotonView를 받아옵니다.
            PhotonView pv = pm.GetComponent<PhotonView>();

            //포톤 뷰가 존재하며, ActorNumber가 입력받은 인자값과 동일한 경우에 아래 코드를 실행합니다.
            if (pv != null && pv.Owner.ActorNumber == actorNumber)
            {
                //해당 플레이어를 반환합니다.
                return pm;
            }
        }

        //여기까지 왔다는 것은 플레이어를 찾지 못했다는 것이므로, null을 반환합니다.
        return null;
    }

    /// <summary>
    /// PunRPC - 무기를 선택한 이후의 RPC입니다.
    /// </summary>
    /// <param name="actorNumber">찾으려는 대상의 ActorNumber</param>
    [PunRPC]
    public void RPC_NotifyWeaponSelected(int actorNumber)
    {
        //방장만이 이 RPC를 행사하도록 제한합니다.
        if (!PhotonNetwork.IsMasterClient)
            return;

        //인자값으로 받은 ActorNumber을 갖는 플레이어를 찾습니다.
        PlayerManager player =
            FindPlayerByActorNumber(actorNumber);

        //찾은 대상에게 무기 선택 이후의 행동을 시행하도록 합니다.
        OnPlayerWeaponSelected(player);
    }

    
}
