using UnityEngine;

public enum WeaponType
{
    Rock, Paper, Scissors
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    public WeaponType weaponType;     // 무기의 종류. 각 타입은 상성이 존재하며, 상성인 상태로 상대를 공격하면 이로운 효과를 얻습니다.
    public float damage;              // 무기별 기본 데미지.
    public float attackRange;         // 공격의 사거리.
    public float attackDuration;      // 공격 지속시간.
}
