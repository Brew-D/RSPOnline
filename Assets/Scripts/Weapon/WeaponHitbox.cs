using System.Collections.Generic;
using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    private Weapon weapon; // 무기 정보
    private HashSet<PlayerManager> hitTargets = new(); // 히트박스가 활성화되었을 때의 공격 대상

    private BoxCollider attackRange;

    private void Start()
    {
        attackRange = GetComponent<BoxCollider>();
        attackRange.enabled = false;
    }

    public void HitboxOnOff(bool isOn)
    {
        if(isOn == true) attackRange.enabled = true;
        else attackRange.enabled = false;
    }

    /// <summary>
    /// 무기를 인자값으로 받으면 그 무기를 히트박스 클래스에 저장합니다.
    /// </summary>
    /// <param name="weapon">지정할 무기</param>
    public void Init(Weapon weapon)
    {
        this.weapon = weapon;
    }

    private void OnTriggerEnter(Collider other)
    {
        //부딪힌 대상으로부터 PlayerManager을 받아오도록 시도합니다.
        if (!other.TryGetComponent(out PlayerManager target)) return;

        //피격 대상에 포함되어 있다면 반환합니다. ( 다단히트 방지용 )
        if(hitTargets.Contains(target)) return;

        //부딪힌 대상을 공격 대상에 추가합니다.
        hitTargets.Add(target);
        //무기가 대상을 공격했다는 정보를 전달합니다.
        weapon.ProcessHit(target);
    }

    /// <summary>
    /// 공격 대상에 포함되어있던 모든 플레이어를 대상에서 제외합니다.
    /// </summary>
    public void ResetHitbox()
    {
        //공격 대상을 초기화합니다.
        hitTargets.Clear();
    }
}
