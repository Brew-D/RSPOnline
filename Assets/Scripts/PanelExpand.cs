using UnityEngine;


/// <summary>
/// 현재 패널의 상태를 정의하기 위한 열거형입니다.
/// </summary>
public enum PanelState
{
    Start, ExpandX, ExpandY, Ready
}
public class PanelExpand : MonoBehaviour
{
    //패널의 상태에 따라 다른 역할을 수행합니다.
    private PanelState state;
}
