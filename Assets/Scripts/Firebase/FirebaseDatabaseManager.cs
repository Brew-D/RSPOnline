using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FirebaseDatabaseManager : MonoBehaviour
{
    private DatabaseReference rootRef;
    private DatabaseReference userRef;
    private FirebaseUser user;

    //이 클래스는 싱글톤 패턴을 사용하여 게임 플레이가 유지되는 동안 파괴되지 않고 남아있습니다.
    public static FirebaseDatabaseManager Instance { get; private set; }
    //플레이어의 정보를 저장할 공간입니다.
    public PlayerData Data { get; private set; }
    //플레이어의 정보가 변하게 되었을 때 수행할 이벤트입니다.
    public event Action<PlayerData> OnPlayerDataChanged;

    private void Awake()
    {
        //인스턴스에 무언가가 이미 존재할 경우
        if(Instance != null)
        {
            //중복 생성을 방지하기 위해 자기 자신을 파괴합니다.
            Destroy(gameObject);

            //이후 코드를 실행하지 않도록 반환합니다.
            return;
        }

        //인스턴스에 아무것도 없으면 자신을 인스턴스에 넣습니다.
        Instance = this;
        //이 게임 오브젝트는 씬 로드 등의 이유로 파괴되지 않도록 합니다.
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 유저의 아이디값을 받아 초기화합니다.
    /// </summary>
    /// <param name="userId">해당 유저의 고유 아이디 값</param>
    public void Initialize(string userId)
    {
        //인증 매니저로부터 해당 유저의 정보를 받아옵니다.
        user = FirebaseAuthenticationManager.user;

        //가장 기본적인 경로를 지정해줍니다.
        rootRef = FirebaseDatabase.DefaultInstance.GetReference("users");
        //여기서 유저의 아이디값 경로를 추가로 작성합니다.
        userRef = rootRef.Child(userId);
    }

    /// <summary>
    /// 유저의 닉네임을 업데이트합니다.
    /// </summary>
    /// <param name="newNickname">변경할 닉네임</param>
    public void UpdateNickname(string newNickname)
    {
        //데이터에서 해당 유저의 nickname값을 인자값으로 변경합니다.
        Data.nickname = newNickname;

        //유저 레퍼런스에서, 닉네임 부분의 동기화 값 또한 인자값으로 변경합니다.
        userRef.Child("nickname").SetValueAsync(newNickname);

        //유저의 프로필을 업데이트하고 동기화합니다.
        user.UpdateUserProfileAsync(new UserProfile
        {
            //출력되는 이름은 인자값으로 받아온 값입니다.
            DisplayName = newNickname
        });
    }

    /// <summary>
    /// 유저의 데이터에서 갱신할 것이 있을 경우, 처음 생성되는 데이터를 업데이트합니다.
    /// </summary>
    public void UpdateFirstCreatedData()
    {
        //유저의 닉네임 쪽에 유저가 입력한 닉네임을 넣어줍니다.
        userRef.Child("nickname").SetValueAsync(user.DisplayName);
        //유저의 총 킬 수를 0으로 설정합니다.
        userRef.Child("totalkills").SetValueAsync(0);
        //유저의 총 골드 보유 수를 0으로 설정합니다.
        userRef.Child("gold").SetValueAsync(0);
    }

    /// <summary>
    /// 이제 막 생성을 마친 플레이어의 기본적인 데이터들을 형성합니다.
    /// </summary>
    /// <param name="name">플레이어의 이름</param>
    public void CreateInitialPlayerData(string name)
    {
        //닉네임, 총 킬 수, 골드 수를 새롭게 데이터에 넣습니다.
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "nickname" , name },
            { "totalKills" , 0 },
            { "gold" , 0 }
        };

        //해당 데이터로, 유저의 레퍼런스에 해당 값을 동기화합니다.
        userRef.SetValueAsync(data);
    }

    /// <summary>
    /// 플레이어의 데이터를 불러옵니다.
    /// </summary>
    public void LoadPlayerData()
    {
        //유저의 경로로부터, 값을 받아옵니다.
        userRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            //해당 동작에서 예외가 존재할 경우
            if(task.Exception != null)
            {
                Debug.LogError("플레이어의 데이터를 받아오는 데 실패하였습니다." + task.Exception);
                return;
            }
            //해당 동작으로 예외가 발생하지 않았으나 값이 아예 없었을 경우
            else if(!task.Result.Exists)
            {
                //초기값을 형성합니다.
                CreateInitialPlayerData(user.DisplayName);
                return;
            }
            //예외 또한 발생하지 않았고 값 또한 존재하고 있었을 경우
            else
            {
                //존재하는 값들을 불러옵니다.
                Data = JsonUtility.FromJson<PlayerData>(task.Result.GetRawJsonValue());
                OnPlayerDataChanged?.Invoke(Data);
            }
        });
    }
}
