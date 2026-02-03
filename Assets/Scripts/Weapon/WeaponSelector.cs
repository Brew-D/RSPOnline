using Photon.Pun;
using UnityEngine;

public class WeaponSelector : MonoBehaviour
{
    public WeaponType weaponType; // 열거형 무기 종류. 고르는 지점이 해당 값을 받고 있다가 생성 시에 넘겨줍니다.
    

    private void OnTriggerEnter(Collider other)
    {
        //HiddenPlayer 태그를 가진, 다른 플레이어들의 경우 해당되지 않습니다.
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        //포톤뷰를 불러오고, 없거나 내 캐릭터가 아닌 경우 반환합니다.
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv == null || pv.IsMine == false) return;

        Debug.Log("정상적으로 플레이어를 인식하였습니다.");

        //부딪힌 오브젝트로부터 웨폰매니저를 불러와, 무기를 선택하도록 지시합니다.
        other.GetComponent<WeaponManager>().SelectWeapon(weaponType);
    }
}
