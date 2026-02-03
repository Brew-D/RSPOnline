using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private Transform weaponSocket; // 무기를 장착할 공간입니다. 무기 장착을 진행할 플레이어의 위치를 넣어 주십시오.
    public Transform WeaponSocket => weaponSocket;
}
