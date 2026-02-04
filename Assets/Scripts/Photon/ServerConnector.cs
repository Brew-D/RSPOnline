using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerConnector : MonoBehaviourPunCallbacks
{
    public static ServerConnector Instance;

    public static event Action OnJoinedRoomEvent;
    public static event Action OnPlayerListChanged;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //호스트와 클라이언트를 동기화합니다.
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    

    public void LeaveRoom()
    {
        if(PhotonNetwork.InRoom == true)
        {
            PhotonNetwork.LeaveRoom();
        }
    }


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        OnPlayerListChanged?.Invoke();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        OnPlayerListChanged?.Invoke();
    }


    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
        Debug.Log($"현재 방 이름: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");
        PhotonNetwork.LoadLevel("RoomScene");
        
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"CreateRoom Failed | {returnCode} | {message}");
    }
}
