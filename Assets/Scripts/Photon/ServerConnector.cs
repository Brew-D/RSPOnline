using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerConnector : MonoBehaviourPunCallbacks
{
    private void Awake()
    {
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

    public override void OnConnectedToMaster()
    {
        Debug.Log("마스터 서버 연결에 성공하였습니다.");
        //로비 씬을 불러옵니다.
        SceneManager.LoadScene("LobbyScene");
    }
}
