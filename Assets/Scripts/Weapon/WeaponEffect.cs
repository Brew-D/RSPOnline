using System.Collections;
using Photon.Pun;
using UnityEngine;

public class WeaponEffect : MonoBehaviour
{
    [Header("소환할 이펙트 담당용 게임오브젝트")]
    [SerializeField] GameObject effectPrefab;

    [Header("히트박스")]
    [SerializeField] WeaponHitbox hitbox;

    [SerializeField] float duration = 0.15f;
    [SerializeField] float moveDistance = 1.2f;
    [SerializeField] float rightOffset = 0.4f;

    BoxCollider col;
    PlayerManager owner;

    public void Init(PlayerManager player, Transform spawnPoint)
    {
        owner = player;
        col = GetComponentInChildren<BoxCollider>();

        // 플레이어 기준 위치 설정
        Vector3 right = spawnPoint.right * rightOffset;
        transform.position = spawnPoint.position + right;
        transform.forward = spawnPoint.forward;

        col.enabled = false;

        StartCoroutine(Swing());
    }

    public void Play()
    {
        StopAllCoroutines();

        StartCoroutine(Swing());
    }

    public void SpawnAttack(Transform attacker)
    {
        Vector3 spawnPos =
            attacker.position +
            attacker.forward * 1;

        Quaternion spawnRot =
            Quaternion.LookRotation(attacker.forward);

        PhotonNetwork.Instantiate("effectPrefab", spawnPos, spawnRot);
    }

    IEnumerator Swing()
    {
        float elapsed = 0f;

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos - transform.right * (rightOffset * 3f);

        // 기존 피격 대상 초기화
        GetComponent<WeaponHitbox>()?.ResetHitbox();

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);

            // 공격 판정 구간
            col.enabled = (t >= 0.2f && t <= 0.7f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        col.enabled = false;
        Destroy(gameObject);
    }

}
