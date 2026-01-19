using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class RoomPlayerModelController : MonoBehaviourPun
{
    private void Start()
    {
        Initialize(photonView.IsMine);
    }

    /// <summary>
    /// 자신의 캐릭터인지 여부에 따라, 해당 캐릭터의 스케일을 조정합니다.
    /// 방 초안 디자인에 따른 코드이므로, 추후 방 디자인의 변경에 따라 사라질 수 있습니다.
    /// </summary>
    /// <param name="isLocal">Local 여부 확인용.</param>
    public void Initialize(bool isLocal)
    {
        //자신의 캐릭터일 경우 캐릭터의 크기를 2배로, 그렇지 않을 경우 1배로 적용합니다.
        transform.localScale = isLocal ? new Vector3(2, 2, 2) : new Vector3(1, 1, 1);
    }

    /// <summary>
    /// 캐릭터의 위치를 해당 슬롯의 플레이어 전용 위치로 옮깁니다.
    /// </summary>
    /// <param name="slot">플레이어를 배치할 슬롯의 번호</param>
    public void MoveSlot(Transform slot)
    {
        transform.position = slot.position;
        transform.rotation = slot.rotation;
    }

}
