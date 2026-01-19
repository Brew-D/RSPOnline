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
        Instance = this;
        if(PlayerManager.LocalPlayerInstance == null)
        {
            StartCoroutine(SpawnPlayerWhenConnected());
        }
    }

    IEnumerator SpawnPlayerWhenConnected()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        PlayerManager.LocalPlayerInstance = PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0, 5, 0), Quaternion.identity);
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(1);
    }

    public void OnGameStart()
    {
        var player = FindLocalPlayer();
        if (player != null)
            player.GetComponent<PlayerManager>().SetGravity(true);
    }

    public void LeaveRoom()
    {
        if (!PhotonNetwork.InRoom)
            return;
        if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Leaving)
            return;
        PhotonNetwork.LeaveRoom();
    }
    GameObject FindLocalPlayer()
    {
        foreach (var playerChar in FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
        {
            if (playerChar.photonView.IsMine)
                return playerChar.gameObject;
        }
        return null;
    }
}
