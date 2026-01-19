using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    [SerializeField] GameObject playerPrefab;
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
    /// 플레이어의 캐릭터를 맵에 생성하는 코루틴입니다.
    /// </summary>
    /// <returns>완성된 캐릭터 출력. 반환값 없음.</returns>
    IEnumerator SpawnPlayerWhenConnected()
    {
        //플레이어가 방에 있는지 확인할 때까지 기다립니다. (보통 게임씬일 테니 방에 있을 것입니다)
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        //플레이어가 조작할 캐릭터는, 포톤네트워크에서 생성하는, (Resources 폴더 내의)플레이어 프리팹을, 높이 5의 공간에, 회전 없이 생성합니다.
        PlayerManager.LocalPlayerInstance = PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0, 5, 0), Quaternion.identity);
        //플레이어가 조작할 캐릭터의 플레이어매니저 클래스를 가져와, 현재 조작 모드를 게임으로 설정하여 게임 씬 전용 조작이 가능하게 합니다.
        PlayerManager.LocalPlayerInstance.GetComponent<PlayerManager>().SetControlMode(PlayerControlMode.Game);
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
}
