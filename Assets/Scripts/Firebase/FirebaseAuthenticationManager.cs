using Firebase; // 파이어베이스의 기본 기능 사용을 위해 필요합니다.
using Firebase.Auth; // 파이어베이스의 인증 기능 사용을 위해 필요합니다.
using Firebase.Database; // 파이어베이스의 데이터베이스 기능 사용을 위해 필요합니다.
using Firebase.Extensions; // 파이어베이스의 확장 기능 사용을 위해 필요합니다.
using System.Collections;
using System.Threading.Tasks; // 비동기 작업 관련입니다.
using TMPro; // TextMeshPro계열 관련 기능을 위해 필요합니다.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // UnityEngine의 UI 관련된 코드를 작성 시 필요합니다.

public class FirebaseAuthenticationManager : MonoBehaviour
{
    public FirebaseAuth auth; // 인증 진행용 객체입니다.
    static public FirebaseUser user; // 인증 이후의 유저 정보를 들고 있도록 하기 위함입니다.
    public static DatabaseReference dbRef; // 데이터베이스에 대한 정보를 여러 씬에서 쓰기 위한 정적 변수입니다.

    [Header("게임 시작 버튼")]
    [SerializeField] Button startButton; // 안내창 버튼 연결 바랍니다.
    [SerializeField] TextMeshProUGUI startButtonText; // 안내창 버튼 TMP 연결 바랍니다.

    [Header("로그인 입력 필드")]
    [SerializeField] TMP_InputField loginEmailField;
    [SerializeField] TMP_InputField loginPasswordField;

    [Header("회원가입 입력 필드")]
    [SerializeField] TMP_InputField registerEmailField;
    [SerializeField] TMP_InputField registerPasswordField;
    [SerializeField] TMP_InputField nicknameField;

    [Header("안내용 메세지")]
    [SerializeField] TextMeshProUGUI noticeTitle;
    [SerializeField] TextMeshProUGUI noticeMessage;

    [Header("타이틀 화면 매니저")]
    [SerializeField] TitleManager titleMgr;

    [Header("서버 연결용 오브젝트")]
    [SerializeField] ServerConnector connector;

    //이 매니저만 접근 가능한, 에러 발생 시 해당 에러에 대해 전달하기 위한 문자열입니다.
    private string message;
    //안내창에 작성할 위 메세지에 대해, 해당 메세지가 나타내는 큰 틀을 적어줄 문자열입니다.
    private string title;

    void Awake()
    {
        //프로젝트와 맞지 않는 코드를 수정하기 위함입니다.
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            //비동기 작업 결과를 기억시킵니다.
            var dependencyStatus = task.Result;

            //비동기 작업이 가능하다는 결과를 받았을 경우 아래 내용을 실행합니다.
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                //인증 진행용 객체에 해당 인증 정보를 기억시킵니다.
                auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

                //데이터베이스 정보를 초기화합니다.
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;

                //인게임 버튼을 활성화시킵니다.
                startButton.interactable = true;
            }

            //비동기 작업이 가능하다는 결과 이외의 결과를 받았을 경우 아래 내용을 실행합니다.
            else
            {
                //유니티엔진 기준으로, 에러가 발생했다는 문구를 출력합니다.
                UnityEngine.Debug.LogError(System.String.Format("오류가 발생했습니다." + dependencyStatus));
            }

            #region 인스펙터 연결 확인용 코드
            if (startButton == null)
                Debug.LogWarning("시작 버튼이 연결되지 않았습니다!");    
            if(loginEmailField == null)
                Debug.LogWarning("이메일 입력을 위한 로그인 창 입력 공간이 연결되지 않았습니다!");    
            if(registerEmailField == null)
                Debug.LogWarning("이메일 입력을 위한 회원가입 창 입력 공간이 연결되지 않았습니다!");    
            if(loginPasswordField == null)
                Debug.LogWarning("비밀번호 입력을 위한 로그인 창 입력 공간이 연결되지 않았습니다!");    
            if(registerPasswordField == null)
                Debug.LogWarning("비밀번호 입력을 위한 회원가입 창 입력 공간이 연결되지 않았습니다!");    
            if(nicknameField == null)
                Debug.LogWarning("가입 시 게임 내에서 사용할 이름의 입력을 위한 입력 공간이 연결되지 않았습니다!");    
            if(noticeTitle == null)
                Debug.LogWarning("안내 창의 제목 텍스트가 연결되지 않았습니다!");
            if(noticeMessage == null)
                Debug.LogWarning("안내 창의 메세지 텍스트가 연결되지 않았습니다!");
            if (titleMgr == null)
                Debug.LogWarning("타이틀 화면 매니저가 연결되지 않았습니다!");
            if (connector == null)
                Debug.LogWarning("서버 연결용 오브젝트가 연결되지 않았습니다!");
            #endregion
        }
        );
    }

    void Start()
    {
        //startButton.interactable = false;
        noticeTitle.text = "";
        noticeMessage.text = "";
    }

    public void Login()
    {
        StartCoroutine(LoginCor(loginEmailField.text, loginPasswordField.text));
    }

    IEnumerator LoginCor(string email, string password)
    {
        //이메일과 비밀번호를 통해 인증 과정으로부터 로그인을 실행합니다.
        Task<AuthResult> LoginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(predicate : () => LoginTask.IsCompleted);

        //로그인 과정에서 문제가 발생했다면, 아래 코드를 실행합니다.
        if(LoginTask.Exception != null)
        {
            Debug.LogWarning("로그인 실패!" + LoginTask.Exception);

            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError) firebaseEx.ErrorCode;

            noticeTitle.text = "로그인 실패";
            noticeMessage.text = "";
            switch(errorCode)
            {
                case AuthError.MissingEmail:
                    noticeMessage.text = "이메일 누락\n(MissingEmail)";
                    break;
                case AuthError.MissingPassword:
                    noticeMessage.text = "패스워드 누락\n(MissingPassword)";
                    break;
                case AuthError.WrongPassword:
                    noticeMessage.text = "패스워드 미일치\n(WrongPassword)";
                    break;
                case AuthError.InvalidEmail:
                    noticeMessage.text = "유효하지 않은 이메일 형식\n(InvalidEmail)";
                    break;
                case AuthError.UserNotFound:
                    noticeMessage.text = "존재하지 않는 아이디\n(UserNotFound)";
                    break;
                default:
                    noticeMessage.text = "관리자에게 문의 바랍니다";
                    break;
            }
        }
        //로그인 과정에서 문제가 발생하지 않은 경우, 로그인 성공을 안내합니다.
        else
        {
            user = LoginTask.Result.User;

            PlayerSession.UserId = user.UserId;
            FirebaseDatabaseManager.Instance.Initialize(user.UserId);

            noticeTitle.text = "로그인 성공";
            nicknameField.text = user.DisplayName;
            noticeMessage.text = "반갑습니다," + user.DisplayName + "님!";

            startButtonText.text = "게임 시작";
            startButton.onClick.AddListener(connector.ConnectToServer);
        }
        //타이틀매니저를 통해 안내창을 팝업시킵니다.
        titleMgr.PopUpNoticePanel();
    }
    public void Register()
    {
        StartCoroutine(RegisterCor(registerEmailField.text, registerPasswordField.text, nicknameField.text));
    }

    IEnumerator RegisterCor(string email, string password, string userName)
    {
        //이메일과 비밀번호를 받아 인증 과정을 거쳐 유저를 생성합니다.
        Task<AuthResult> RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

        //가입 절차에서 오류가 발생했다면 아래 코드를 실행합니다.
        if (RegisterTask.Exception != null)
        {
            Debug.LogWarning(message: "가입 실패!" + RegisterTask.Exception);

            FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            noticeTitle.text = "가입 실패";
            noticeMessage.text = "";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    noticeMessage.text = "이메일이 누락되었습니다.\n(MissingEmail)";
                    break;
                case AuthError.MissingPassword:
                    noticeMessage.text = "비밀번호가 누락되었습니다.\n(MissingPassword)";
                    break;
                case AuthError.WeakPassword:
                    noticeMessage.text = "비밀번호가 너무 짧습니다.\n(WeakPassword)";
                    break;
                case AuthError.EmailAlreadyInUse:
                    noticeMessage.text = "중복된 이메일입니다.\n(EmailAlreadyInUse)";
                    break;
                default:
                    noticeMessage.text = "관리자에게 문의 바랍니다.";
                    break;
            }
            //타이틀매니저를 통해 안내창을 팝업시킵니다.
            titleMgr.PopUpNoticePanel();
        }
        else
        {
            //가입 절차에 따른 유저 생성의 결과를 담습니다.
            user = RegisterTask.Result.User;

            //담은 결과가 무언가를 생성해냈다면 아래 코드를 실행합니다.
            if (user != null)
            {
                //로컬에서 유저의 프로필을 생성합니다.
                UserProfile profile = new UserProfile { DisplayName = userName };

                //유저의 프로필을 업데이트합니다.
                Task profileTask = user.UpdateUserProfileAsync(profile);
                yield return new WaitUntil(predicate: () => profileTask.IsCompleted);

                //오류가 발생했을 경우, 해당 안내를 작성합니다.
                if (profileTask.Exception != null)
                {
                    Debug.LogWarning("닉네임 설정 실패" + profileTask.Exception);
                    FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                    noticeTitle.text = "닉네임 설정 실패";
                    noticeMessage.text = "닉네임 설정에 실패하였습니다.";
                }

                //발생하지 않은 경우, 생성 완료에 대한 안내를 작성합니다.
                else
                {
                    noticeTitle.text = "생성 완료";
                    noticeMessage.text = "생성 완료! 반갑습니다, " + user.DisplayName + "님!";


                    FirebaseDatabaseManager.Instance.Initialize(user.UserId);
                    FirebaseDatabaseManager.Instance.CreateInitialPlayerData(userName);

                    //startButton.interactable = true;
                }
                //타이틀매니저를 통해 안내창을 팝업시킵니다.
                titleMgr.PopUpNoticePanel();
            }
        }
    }

    /// <summary>
    /// 안내 창의 내용을 초기화하는 메서드입니다.
    /// </summary>
    public void ResetNotice()
    {
        noticeTitle.text = "";
        noticeMessage.text = "";
    }
}
