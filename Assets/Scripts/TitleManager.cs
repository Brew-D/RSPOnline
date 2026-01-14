using UnityEngine;
using TMPro;

public enum TitleSceneStatus
{
    Start, Register
}
public class TitleManager : MonoBehaviour
{
    private TitleSceneStatus status;

    public TitleSceneStatus Status { get { return status; } }

    [Header("로그인 전용 패널")]
    [SerializeField] CanvasGroup loginPanel;

    [Header("가입 전용 패널")]
    [SerializeField] CanvasGroup registerPanel;

    [Header("안내용 패널")]
    [SerializeField] CanvasGroup noticePanel;

    //[Header("안내용 패널 내 텍스트")]
    //[SerializeField] TextMeshProUGUI title;
    //[SerializeField] TextMeshProUGUI message;

    private void Awake()
    {
        //시작하여 로그인 창이 열려있는 상태를 기본으로 설정합니다.
        status = TitleSceneStatus.Start;

        #region 인스펙터 연결 확인용 코드
        if (loginPanel == null)
            Debug.LogWarning("타이틀 화면 매니저에 로그인 패널이 연결되지 않았습니다!");
        if (registerPanel == null)
            Debug.LogWarning("타이틀 화면 매니저에 회원가입 패널이 연결되지 않았습니다!");
        if (noticePanel == null)
            Debug.LogWarning("타이틀 화면 매니저에 안내창 패널이 연결되지 않았습니다!");
        #endregion
    }

    /// <summary>
    /// 로그인 창에서 회원 가입 버튼을 눌러 회원 가입 절차를 진행하게 될 때 창 변경을 위해 실행할 메서드입니다.
    /// </summary>
    public void EnterRegister()
    {
        Debug.Log("회원 가입 창으로 이동합니다.");
        status = TitleSceneStatus.Register;
        PanelStateChange(loginPanel, false);
        PanelStateChange(registerPanel, true);
    }

    /// <summary>
    /// 회원 가입 창으로부터 로그인 창으로 이동하게 되는 모든 경우에 창 변경을 위해 실행할 메서드입니다.
    /// </summary>
    public void ExitRegister()
    {
        Debug.Log("로그인 창으로 이동합니다.");
        status = TitleSceneStatus.Start;
        PanelStateChange(loginPanel, true);
        PanelStateChange(registerPanel, false);
    }

    /// <summary>
    /// 안내문 작성이 완료되었을 경우 해당 패널을 활성화하기 위해 실행할 메서드입니다.
    /// </summary>
    public void PopUpNoticePanel()
    {
        PanelStateChange(noticePanel, true);
    }

    /// <summary>
    /// 안내문 확인 후 버튼을 눌렀을 경우 해당 패널을 비활성화하기 위해 실행할 메서드입니다.
    /// </summary>
    public void CloseNoticePanel()
    {
        PanelStateChange(noticePanel, false);
    }

    /// <summary>
    /// 캔버스 그룹과 bool값을 받아, 해당 캔버스 그룹의 활성화와 비활성화를 담당해줄 메서드입니다.
    /// </summary>
    /// <param name="panel">값을 조절할 캔버스 그룹을 가진 패널</param>
    /// <param name="state">해당 패널의 활성화 여부</param>
    public void PanelStateChange(CanvasGroup panel, bool state)
    {
        if(state == true)
        {
            panel.alpha = 1.0f; // 알파값을 1로 설정하여 온전히 화면에 보이게 합니다.
            panel.interactable = true; // 상호작용 여부를 참으로 설정하여 유저가 해당 패널과 상호작용이 가능하게 합니다.
            panel.blocksRaycasts = true; // 레이캐스트 제한을 참으로 설정하여 해당 패널 뒤에 있는 것들에 대한 작업을 방어합니다.
        }
        else if(state == false)
        {
            panel.alpha = 0f; // 알파값을 0으로 설정하여 화면에서 완전히 모습을 감추게 합니다.
            panel.interactable = false; // 상호작용 여부를 거짓으로 설정하여 해당 패널에 대해 상호작용을 하지 못하게 제한합니다.
            panel.blocksRaycasts = false; // 레이캐스트 제한을 거짓으로 설정하여 해당 패널 뒤의 오브젝트들에 대한 작업이 가능하도록 합니다.
        }    
    }
}
