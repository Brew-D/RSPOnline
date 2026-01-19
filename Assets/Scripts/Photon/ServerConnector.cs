using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerConnector : MonoBehaviourPunCallbacks
{
    public static ServerConnector Instance;

    public static event Action OnJoinedRoomEvent;

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

    public void ConnectToServer()
    {
        //한국으로 연결합니다.
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";
        PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;

        PhotonNetwork.ConnectUsingSettings();
    }

    public void LeaveRoom()
    {
        if(PhotonNetwork.InRoom == true)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버 연결에 성공하였습니다.");
        PhotonNetwork.AutomaticallySyncScene = true;

        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby()
    {
        Debug.Log("로비에 입장하였습니다.");
        //로비 씬을 불러옵니다.
        SceneManager.LoadScene("LobbyScene");
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
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
