using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(WeaponHolder))]
[RequireComponent(typeof(PlayerManager))]
public class WeaponManager : MonoBehaviourPun
{
    public Weapon currentWeapon;
    private GameObject Weapon; // 해당 매니저가 담당할 무기 게임오브젝트
    private WeaponHolder holder; // 장착할 위치를 전담하는 WeaponHolder 매개변수
    private PlayerManager playerManager;

    public void Awake()
    {
        playerManager = GetComponent<PlayerManager>();
        //해당 오브젝트의 컴포넌트로서 WeaponHolder를 받아옵니다.
        holder = GetComponent<WeaponHolder>();
    }

    /// <summary>
    /// WeaponType 열거형에 따라, 해당 값에 맞는 무기를 리소스 폴더에서 불러옵니다.
    /// </summary>
    /// <param name="type">열거형의 값인 가위, 바위, 보 중 하나.</param>
    public void SelectWeapon(WeaponType type)
    {
        //자신이 조작하는 캐릭터가 아닌 경우 영향을 끼치지 않도록 합니다.
        if (photonView.IsMine == false)
        {
            Debug.Log("자신의 플레이어가 아니므로 무기 선택을 미진행하였습니다.");
            return;
        }
        //이미 선택을 완료했을 경우 선택이 불가능하도록 합니다.
        if(playerManager.IsReady)
        {
            Debug.Log("이미 무기를 골라 준비가 완료되었으므로 무기 선택이 불가능합니다.");
            return;
        }
        //만약 이미 무기가 있다면 기존 무기를 파괴합니다. ( 만에 하나 IsReady가 작동하지 않을 경우의 방어용 코드 )
        if (currentWeapon != null)
        {
            Debug.Log("기존에 가지고 있던 무기를 파괴하였습니다.");
            Destroy(Weapon);
            currentWeapon = null;
        }

        //생성할 무기는 리소스 폴더의 Weapons 폴더 안에 있는, 열거형의 값과 동일한 이름을 가진 프리팹입니다.
        GameObject weaponPrefab = Resources.Load<GameObject>($"Weapons/{type}");

        //현재의 무기는 무기를 장착하는 위치에, 해당 회전값을 받아서, 그 하위 오브젝트로서 생성합니다.
        Weapon = Instantiate(
            weaponPrefab,
            holder.WeaponSocket.position,
            holder.WeaponSocket.rotation,
            holder.WeaponSocket
            );
        Debug.Log("무기를 생성합니다. Resource 폴더에 있는 무기가 정상 제작되었습니다.");

        currentWeapon = Weapon.GetComponent<Weapon>();
        playerManager.weapon = currentWeapon;
        playerManager.weaponType = type;
        playerManager.weapon.SetEffectSpawnPoint(playerManager.effectTransform);

        GameManager.Instance.OnSelectWeapon();
    }

    
}
