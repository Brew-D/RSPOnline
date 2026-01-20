using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public enum PlayerControlMode
{
    Room,Game
}

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
    public float coyoteTime = 0.15f;

    [Header("중력 제어 확인용 변수")]
    [SerializeField] bool useGravity = false;

    [Header("바닥 판정 확인용 변수")]
    [SerializeField] float groundCheckDistance = 0.3f;
    [SerializeField] LayerMask ground;

    public static GameObject LocalPlayerInstance;

    private PlayerControlMode controlMode = PlayerControlMode.Room; // 조작 모드입니다. Room일 경우 조작방지, Game일 경우 해제 상태입니다.
    private PlayerInput playerInput;                                // 플레이어의 조작은 InputSystem을 이용해 받을 예정입니다.
    private Vector2 moveInput;                                      // 바닥에서의 움직임을 제어하기 위한 Vector2 입력값입니다.
    private Vector3 velocity;                                       // Rigidbody 이동 제어를 위한 velocity 입니다.

    private bool isJumpPressed;           // 점프를 눌렀는지 여부를 확인합니다.
    private bool isAttacking;             // 공격 여부를 확인합니다.
    private bool isGrounded = false;      // 땅에 닿고 있는지 여부를 확인합니다. 공중에서 시작하므로 시작값은 false입니다.
    private bool raycastGrounded = false; // 바닥에 레이캐스트가 닿았는지 여부를 확인합니다. 공중에서 시작하므로 시작값은 false입니다.

    private float coyoteTimeChecker = 0;  // 코요테 타임(바닥을 밟고 있다가 점프 기능을 사용하지 않고 체공한 경우 점프가 가능한 시간을 제공) 확인용 변수입니다.
    private void Awake()
    {
        //포톤 뷰 기준으로 캐릭터가 생성될 텐데, 플레이어의 포톤 뷰일 경우에만 작동하게 설계합니다.
        if(photonView.IsMine == true)
        {
            LocalPlayerInstance = gameObject;
        }
        playerInput = GetComponent<PlayerInput>();
        //할당받은 플레이어를 조작하기 위한 컨트롤러를 받아옵시다.
        if(controller == null)
            controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        //"내가 조작하는 캐릭터에 한하여" 플레이어의 입력을 받도록 합니다.
        if (photonView.IsMine == true)
        {
            playerInput.enabled = true;
        }
    }

    private void Start()
    {
        //내가 조작하는 캐릭터일 경우, 카메라가 따라올 대상을 해당 캐릭터로 설정합니다.
        if(photonView.IsMine == true)
            Camera.main.GetComponent<CameraMovement>().target = transform;
    }

    private void OnDisable()
    {
        //사라질 때, 가지고 있던 "플레이어 입력" 받는 기능을 제거합니다.
        playerInput.enabled = false;
    }

    void Update()
    {
        //내 캐릭터가 아닌 경우 반환합니다.
        if(photonView.IsMine != true)
        {
            return;
        }
        //게임이 시작되어 플레이중인 상태일 경우 아래 코드를 실행합니다.
        if(controlMode == PlayerControlMode.Game)
        {
            //중력 활성화를 통해 조작 방지 상태를 해제합니다.
            SetGravity(true);
            //조작 관련은 이 메서드로 전부 받아옵니다.
            //중력이 비활성화되면 이 기능들이 전부 동작하지 않습니다.
            HandleMovement();
        }
        else
        {
            //조작을 하지 못하도록 중력을 비활성화합니다.
            SetGravity(false);
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
    /// 움직입을 진행할 경우의 인풋액션 콜백입니다.
    /// </summary>
    /// <param name="ctx"></param>
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if(isGrounded == true && ctx.started)
            isJumpPressed = true;
    }

    /// <summary>
    /// 플레이어가 현재 어떤 씬에 있는지 판단하여, 조작 모드를 선택합니다.
    /// </summary>
    /// <param name="mode">현재 씬의 종류</param>
    public void SetControlMode(PlayerControlMode mode)
    {
        //내 캐릭터가 아니면 반환합니다.
        if (!photonView.IsMine) return;
        //컨트롤 모드를 받아와, 해당 상태를 적용시킵니다.
        controlMode = mode;

        //이때, 받아온 상태의 정보를 확인하고,
        //방 씬이라면 다음과 같은 코드를 실행합니다.
        if(mode == PlayerControlMode.Room)
        {
            //방 안에 있으면 움직임을 만들어서는 안 됩니다. 이동량을 0으로 지정합니다.
            velocity = Vector3.zero;
            //조작을 통해 플레이어가 이동을 해서는 안 되므로 캐릭터컨트롤러를 비활성화합니다.
            controller.enabled = false;
        }
        //게임 씬이라면 아래 코드를 실행합니다.
        else if(mode == PlayerControlMode.Game)
        {
            //게임에 들어왔다면 조작을 해야 하므로 캐릭터컨트롤러를 활성화합니다.
            controller.enabled = true;
            //게임에 들어왔으니 중력을 활성화하여 조작 방지 상태를 해제합니다.
            useGravity = true;
        }
    }

    /// <summary>
    /// 움직임을 제어하는 메서드의 총 집합체입니다.
    /// </summary>
    private void HandleMovement()
    {
        CheckGround(); // 바닥에 붙어있는가?
        UpdateGroundState(); // 그 정보를 업데이트했는가?

        Jump(); // 점프 동작이 들어왔는가?
        Gravity(); // 중력을 적용한다
        Move(); // 이동 동작이 들어왔는가?
    }

    /// <summary>
    /// 이동을 담당하는 메서드입니다.
    /// </summary>
    private void Move()
    {
        //중력을 적용하지 않을 경우 해당 메서드의 동작을 진행하지 않습니다.
        if (!useGravity) return;
        
        // 바닥에서의 이동 방향을 정의합니다.
        Vector3 groundMovement = transform.right * moveInput.x + transform.forward * moveInput.y;
        //이동 방향을 지정합니다. 전/후방 이동, 좌/우측 이동값에 상하 이동값을 더해줍니다.
        Vector3 move = groundMovement * moveSpeed + Vector3.up * velocity.y;

        //캐릭터컨트롤러에 1초간 움직이는 방향 * 지정된 이동 속도만큼 나아갈 정도의 속도로 이동하는 것을 명령합니다.
        controller.Move(move * Time.deltaTime);
    }

    /// <summary>
    /// 점프를 담당하는 메서드입니다.
    /// </summary>
    private void Jump()
    {
        //중력을 적용하지 않을 경우 해당 메서드의 동작을 진행하지 않습니다.
        if (!useGravity) return;

        //캐릭터 컨트롤러에서 지면에 닿아있다는 신호가 있다면 아래 코드를 실행합니다.
        if (isGrounded == true)
        {
            //바닥에 닿아있을 경우 코요테 타임은 기본값을 유지합니다.
            coyoteTimeChecker = coyoteTime;
            //y축 기준 높이가 0보다 낮다면
            if (isGrounded == true && velocity.y < 0)
            {
                //-2로 고정시켜 바닥과 접촉하고 있는 상태를 강제합니다.
                velocity.y = -2f;
            }

            //점프 버튼이 눌러져 있을 경우 아래 코드를 실행합니다.
            if (isJumpPressed == true)
            {
                //y축 기준 벨로시티를 점프력만큼으로 바꿉니다.
                velocity.y = jumpForce;
                //점프가 눌러져 있는 것을 비활성화합니다.
                isJumpPressed = false;
                //점프를 진행했으므로 바로 공중에 띄웁니다.
                isGrounded = false;
                //코요테 타임은 점프를 하는 순간 0이 됩니다.
                coyoteTimeChecker = 0;
            }
        }
        else
        {
            //점프 버튼 없이 공중에 뜬 거라면, 코요테 타임이 지나기 전까지 여전히 점프가 가능합니다.
            coyoteTimeChecker -= Time.deltaTime;
            //코요테 타임이 0이 되기 전에, 점프 버튼이 눌러졌을 경우 점프와 동일한 기능을 수행합니다.
            if(coyoteTimeChecker > 0 && isJumpPressed == true)
            {
                //y축 기준 벨로시티를 점프력만큼으로 바꿉니다.
                velocity.y = jumpForce;
                //점프가 눌러져 있는 것을 비활성화합니다.
                isJumpPressed = false;
                //점프를 진행했으므로 바로 공중에 띄웁니다.
                isGrounded = false;
                //코요테 타임은 점프를 하는 순간 0이 됩니다.
                coyoteTimeChecker = 0;
            }
        }
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
    
    private void UpdateGroundState()
    {
        //땅에 붙어있는지의 조건은, 캐릭터컨트롤러의 바닥 판정을 기반으로 합니다.
        //다만 바닥에 붙어있음에도 붙어있지 않은 판정이 가끔 발생하므로, 레이캐스트를 통해 판정을 완화합니다.
        isGrounded = controller.isGrounded || raycastGrounded;
    }

    /// <summary>
    /// 바닥에 붙어있는지 여부를 확인하기 위해, 레이캐스트를 사용합니다.
    /// </summary>
    private void CheckGround()
    {
        //중력을 적용하지 않을 경우 해당 메서드의 동작을 진행하지 않습니다.
        if (!useGravity) return;

        //레이캐스트의 기준점은 캐릭터 오브젝트의 위치에서 살짝 위입니다.
        Vector3 origin = transform.position + Vector3.up * 0.05f;

        //땅을 향한 레이캐스트는, 기준점에서 아래 방향으로, 약한 수치를 주어 바닥 레이어를 확인하도록 합니다.
        //주의사항으로, ground에 항상 바닥 레이어가 설정되어있는지 확인해야 합니다.
        raycastGrounded = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            ground
        );
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

        //만약 그 값이 false였을 경우, 아래 코드를 실행합니다.
        if(useGravity == false)
        {
            //y축의 움직임이 생겨서는 안 됩니다.
            velocity.y = 0;
            //플레이어의 Rigidbody를 가져와, 중력을 비활성화합니다.
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
        }

    }

    
}
