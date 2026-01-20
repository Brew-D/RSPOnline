using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("카메라 특성 관련")]
    public Transform target;        // 쫒아갈 대상
    public Vector3 offset;          // 카메라가 대상으로부터 떨어져있을 거리
    public float followSpeed = 5f;  // 대상을 추적할 속도

    private void Awake()
    {
        if (target == null)
            Debug.Log("CameraMovement - 카메라가 쫒아갈 대상이 할당되지 않았습니다!");
    }

    //이동을 마친 후 따라가야 하므로 LateUpdate로 작성합니다.
    private void LateUpdate()
    {
        //타겟이 존재하는 경우에는 대상을 추적합니다.
        if (target != null)
        FollowTarget();
    }

    /// <summary>
    /// 대상의 위치에 오프셋을 적용하여 이동할 위치를 확인, 해당 위치로 실시간 이동합니다.
    /// </summary>
    private void FollowTarget()
    {
        //목표거리는 플레이어로부터 오프셋만큼 떨어진 거리. 플레이어의 이동을 따라갑니다.
        Vector3 desiredPos = target.position + offset;
        
        //카메라는 현재 위치에서 목표 위치까지 부드럽게 이동합니다.
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followSpeed * Time.deltaTime
        );
    }
}
