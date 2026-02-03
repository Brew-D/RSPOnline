using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class RoomPlayerSlot : MonoBehaviour
{

    //해당 슬롯의 소유자.
    public Player Owner { get; private set; }

    public Image ReadyImage { get; private set; }

    private void Awake()
    {

    }
    /// <summary>
    /// 플레이어를 인자값으로 받아, 해당 플레이어에게 슬롯 자기 자신을 할당합니다.
    /// </summary>
    /// <param name="player">나가기 전까지 해당 슬롯에 귀속될 플레이어</param>
    public void BindPlayer(Player player)
    {
        Owner = player;
    }

    /// <summary>
    /// 슬롯을 초기화합니다.
    /// </summary>
    /// <param name="player">현재 자리에 배치될 플레이어</param>
    /// <param name="slotReadyImage">해당 플레이어의 준비 여부를 보여줄 준비 이미지</param>
    public void Initialize(Player player, Image slotReadyImage)
    {
        Owner = player;
    }
}