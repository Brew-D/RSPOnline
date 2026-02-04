using Photon.Pun;
using System.Collections;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Weapon : MonoBehaviourPun
{
    [Header("공격 이펙트 관련")]
    [SerializeField] WeaponEffect weaponEffectPrefab;
    [SerializeField] WeaponType weaponType;
    [SerializeField] Transform effectSpawnPoint;

    //각각의 무기는 무기로서의 정보를 담고 있습니다.
    public WeaponData data; // 무기 정보
    public WeaponHitbox hitbox; // 무기의 범위

    private PlayerManager player;
    private bool isAttacking;

    private void Awake()
    {
        if(hitbox != null)
            hitbox.Init(this); // 히트박스에 무기를 담습니다.
        isAttacking = false;
    }
    public void SetEffectSpawnPoint(Transform spawnPoint)
    {
        effectSpawnPoint = spawnPoint;
    }

    //공격자의 PlayerManager 값을 받아옵니다.
    public void AttackerCheck(PlayerManager attacker)
    {
        player = attacker;
    }

    public void Attack(PlayerManager player)
    {
        Instantiate(weaponEffectPrefab)
            .GetComponent<WeaponEffect>()
            .Init(player, effectSpawnPoint);
    }

    /// <summary>
    /// 공격 시의 코드를 담은 코루틴입니다.
    /// </summary>
    /// <returns></returns>
    public IEnumerator AttackCoroutine(PlayerManager player)
    {
        

        //공격 이펙트를 지정 범위에 생성합니다.
        WeaponEffect effect = Instantiate(
            weaponEffectPrefab,
            effectSpawnPoint.position,
            effectSpawnPoint.rotation
            );

        //공격 이펙트에게 플레이어가 땅을 밟고 있는지 상태를 전달합니다.
        //이 코드가 공격 이펙트를 수평베기와 수직베기 중 하나를 고르도록 유도합니다.
        effect.Init(player, effectSpawnPoint);
        //effect.Play();

        yield break;
    }

    /// <summary>
    /// 피격된 플레이어에게 피격 시의 코드를 실행하도록 하는 메서드입니다.
    /// </summary>
    /// <param name="target">피격된 대상</param>
    public void ProcessHit(PlayerManager target)
    {
        target.TakeHit(weaponType); // 맞은 대상에게 피격 시의 코드를 실행하도록 합니다.
    }

    public void PlayAttackEffect(PlayerManager player)
    {
        weaponEffectPrefab.Init(player, effectSpawnPoint);
        //weaponEffectPrefab.Play();
    }
}
