using System;

[Serializable] // 이 코드는 다른 코드에 넣어줄 수 있습니다.
public class PlayerData
{
    //닉네임을 담을 매개변수
    public string nickname;
    //총 킬 수를 담을 매개변수
    public int totalKills;
    //총 골드 보유 수를 담을 매개변수
    public int gold;
}
