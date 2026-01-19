using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class RoomPlayerSlot : MonoBehaviour
{
    [Header("각 슬롯의 준비 이미지 관련")]
    [SerializeField] private Image readyImage;
    [SerializeField] private Sprite readySprite;
    [SerializeField] private Sprite notReadySprite;

    //해당 슬롯의 소유자.
    public Player Owner { get; private set; }

    public Image ReadyImage { get; private set; }

    private void Awake()
    {
        if (readyImage == null)
            Debug.LogError("RoomPlayerSlot - 플레이어의 준비 상태를 나타낼 이미지 오브젝트가 연결되지 않았습니다!");
        if (notReadySprite == null)
            Debug.LogError("RoomPlayerSlot - 미 준비 상태를 나타낼 스프라이트가 연결되지 않았습니다!");
        if (readySprite == null)
            Debug.LogError("RoomPlayerSlot - 준비 상태를 나타낼 스프라이트가 연결되지 않았습니다!");
    }
    /// <summary>
    /// 플레이어를 인자값으로 받아, 해당 플레이어에게 슬롯 자기 자신을 할당합니다.
    /// </summary>
    /// <param name="player">나가기 전까지 해당 슬롯에 귀속될 플레이어</param>
    public void BindPlayer(Player player)
    {
        Owner = player;
        SetReady(false);
    }

    /// <summary>
    /// 슬롯을 초기화합니다.
    /// </summary>
    /// <param name="player">현재 자리에 배치될 플레이어</param>
    /// <param name="slotReadyImage">해당 플레이어의 준비 여부를 보여줄 준비 이미지</param>
    public void Initialize(Player player, Image slotReadyImage)
    {
        Owner = player;
        readyImage = slotReadyImage;
        UpdateReadyState();
    }

    /// <summary>
    /// 플레이어의 준비 상태를 갱신합니다.
    /// </summary>
    public void UpdateReadyState()
    {
        //슬롯의 소유자가 없을 경우 반환합니다.
        if (Owner == null) return;

        //준비 상태의 기본값은 거짓입니다. 들어오자마자 준비가 되면 불상사가 일어날 수 있으므로...
        bool isReady = false;

        //슬롯의 소유자의 준비 상태를 불러와, 해당 값을 준비 상태에 저장합니다.
        if (Owner.CustomProperties.TryGetValue("Ready", out object value))
        {
            isReady = (bool)value;
        }

        //그 값을 기준으로, 준비 이미지를 세팅합니다.
        SetReady(isReady);
    }

    /// <summary>
    /// 인자값으로 받아온 준비 여부 bool값에 따라, 준비 이미지 칸의 스프라이트를 수정합니다.
    /// </summary>
    /// <param name="isReady">준비 여부</param>
    public void SetReady(bool isReady)
    {
        //준비 스프라이트를 띄울 이미지 오브젝트가 없으면 반환합니다.
        if (readyImage == null) return;

        //해당 이미지의 스프라이트는 인자값으로 받은 준비 여부 값에 따라, 참이면 준비 상태, 거짓이면 미 준비 상태의 스프라이트를 적용합니다.
        readyImage.sprite = isReady ? readySprite : notReadySprite;
    }
}