using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("현재 장착 중인 무기")]
    [SerializeField] GameObject weapon;

    [Header("캐릭터 컨트롤러")]
    [SerializeField] CharacterController controller;

    [Header("플레이어 인게임 정보")]
    public float health = 20;

    [Header("움직임 제어")]
    public float moveSpeed = 5f;
    public float gravity = -9.8f;
    public float jumpForce = 5f;

    [Header("중력 제어 확인용 변수")]
    [SerializeField] bool useGravity = false;

    public static GameObject LocalPlayerInstance;
    private Vector3 velocity;
    bool isJumpPressed;

    private Vector2 moveInput;

    bool isAttacking;
    private void Awake()
    {
        //포톤 뷰 기준으로 캐릭터가 생성될 텐데, 플레이어의 포톤 뷰일 경우에만 작동하게 설계합니다.
        if(photonView.IsMine == true)
        {
            LocalPlayerInstance = gameObject;
        }

        //할당받은 플레이어를 조작하기 위한 컨트롤러를 받아옵시다.
        if(controller == null)
            controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        //내 캐릭터가 아닌 경우 반환합니다.
        if(photonView.IsMine != true)
        {
            return;
        }
        if(PhotonNetwork.InRoom)
        {
            controller.enabled = false;
        }
        else
        {
            //조작 관련은 이 메서드로 전부 받아옵니다.
            HandleMovement();
        }
    }

    /// <summary>
    /// 움직입을 진행할 경우의 인풋액션 콜백입니다.
    /// </summary>
    /// <param name="ctx"></param>
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    /// <summary>
    /// 움직임을 제어하는 메서드의 총 집합체입니다.
    /// </summary>
    private void HandleMovement()
    {
        Move();
        Jump();
        Gravity();
    }

    /// <summary>
    /// 이동을 담당하는 메서드입니다.
    /// </summary>
    private void Move()
    {
        //중력을 적용하지 않을 경우 해당 메서드의 동작을 진행하지 않습니다.
        if (!useGravity) return;
        
        //이동 방향을 지정합니다. 전/후방 이동, 좌/우측 이동을 지원합니다.
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        //캐릭터컨트롤러에 1초간 움직이는 방향 * 지정된 이동 속도만큼 나아갈 정도의 속도로 이동하는 것을 명령합니다.
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 점프를 담당하는 메서드입니다.
    /// </summary>
    private void Jump()
    {
        //중력을 적용하지 않을 경우 해당 메서드의 동작을 진행하지 않습니다.
        if (!useGravity) return;

        //캐릭터 컨트롤러에서 지면에 닿아있다는 신호가 있다면 아래 코드를 실행합니다.
        if (controller.isGrounded == true)
        {
            //y축 기준 높이가 0보다 낮다면
            if (velocity.y < 0)
                //-2로 고정시켜 바닥과 접촉하고 있는 상태를 강제합니다.
                velocity.y = -2f;

            //점프 버튼이 눌러져 있을 경우 아래 코드를 실행합니다.
            if (isJumpPressed == true)
            {
                //y축 기준 벨로시티를 점프력만큼으로 바꿉니다.
                velocity.y = jumpForce;
                //점프가 눌러져 있는 것을 비활성화합니다.
                isJumpPressed = false;
            }
        }
        //지면에 닿아있지 않을 경우 점프가 눌려도 무시합니다.
        else
            isJumpPressed = false;
    }

    /// <summary>
    /// 중력을 담당하는 메서드입니다.
    /// </summary>
    private void Gravity()
    {
        //중력을 적용하지 않을 경우 해당 메서드의 동작을 진행하지 않습니다.
        if (!useGravity) return;

        //캐릭터의 y축 벨로시티는 시간의 흐름에 따라 점점 감소합니다.
        //중력이 음수이므로 더해줍니다.
        velocity.y += gravity * Time.deltaTime;

        //컨트롤러의 velocity는 이 중력에 영향을 받습니다.
        controller.Move(velocity * Time.deltaTime);
    }
    

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// 중력을 적용할지 여부를 결정합니다.
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    public void SetGravity(bool enabled)
    {
        //인자값으로 넣은 bool값을 중력 사용 여부에 적용합니다.
        useGravity = enabled;

        //만약 그 값이 false였을 경우, y축 velocity를 0으로 고정합니다.
        if(useGravity == false)
            velocity.y = 0;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    
}
