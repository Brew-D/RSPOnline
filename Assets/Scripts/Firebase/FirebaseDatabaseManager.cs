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
    public event Action<PlayerData> OnPlayerDataChanged;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(string userId)
    {
        user = FirebaseAuthenticationManager.user;
        rootRef = FirebaseDatabase.DefaultInstance.GetReference("users");
        userRef = rootRef.Child(userId);
    }
    public void UpdateNickname(string newNickname)
    {
        Data.nickname = newNickname;
        userRef.Child("nickname").SetValueAsync(newNickname);
        user.UpdateUserProfileAsync(new UserProfile
        {
            DisplayName = newNickname
        });
    }
    public void UpdateFirstCreatedData()
    {
        userRef.Child("nickname").SetValueAsync(user.DisplayName);
        userRef.Child("totalkills").SetValueAsync(0);
        userRef.Child("gold").SetValueAsync(0);
    }

    public void CreateInitialPlayerData(string name)
    {
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "nickname" , name },
            { "totalKills" , 0 },
            { "gold" , 0 }
        };

        userRef.SetValueAsync(data);
    }

    public void LoadPlayerData()
    {
        userRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if(task.Exception != null)
            {
                Debug.LogError("플레이어의 데이터를 받아오는 데 실패하였습니다." + task.Exception);
                return;
            }
            else if(!task.Result.Exists)
            {
                CreateInitialPlayerData(user.DisplayName);
                return;
            }
            else
            {
                Data = JsonUtility.FromJson<PlayerData>(task.Result.GetRawJsonValue());
                OnPlayerDataChanged?.Invoke(Data);
            }
        });
    }
}
