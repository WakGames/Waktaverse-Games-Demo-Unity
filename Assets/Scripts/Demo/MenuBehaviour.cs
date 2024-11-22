using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MenuBehaviour : MonoBehaviour
{
    [SerializeField] private Text descText;
    [SerializeField] private Button loginButton;
    private Wakgames _wakgames;

    private void Awake()
    {
        _wakgames = FindObjectOfType<Wakgames>();
    }

    void Start()
    {
        StartCoroutine(_wakgames.GetUserProfile((profile, _) =>
        {
            if (profile != null)
            {
                descText.text = $"{profile.name} 계정으로 로그인 되었습니다.";
                loginButton.GetComponentInChildren<Text>().text = "Logout";

                AppendAchievementMessage();
            }
            else
            {
                descText.text = "로그아웃 상태입니다.";
            }
        }));

        StartCoroutine(_wakgames.GetStatBoard("click_cnt", (result, resCode) =>
        {
            if (result != null)
            {
                int rank = result.BoardIndex + 1;
                Debug.Log($"현재 등수 : {rank}");
            }
            else
            {
                Debug.LogError($"알 수 없는 오류. (Code : {resCode})");
            }
        }));
    }

    void AppendAchievementMessage()
    {
        StartCoroutine(_wakgames.GetUnlockedAchievements((result, resCode) =>
        {
            if (result != null)
            {
                string achieveNames = string.Join(", ", result.achieves.Select((a) => a.name));
                descText.text += $"\n달성한 도전과제 : {result.size}개\n{achieveNames}";
            }
        }));
    }

    public void OnBtnStartClicked()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void OnBtnLoginClicked()
    {
        if (loginButton.GetComponentInChildren<Text>().text == "Logout")
        {
            _wakgames.Logout();

            descText.text = "로그아웃 상태입니다.";
            loginButton.GetComponentInChildren<Text>().text = "Login";
        }
        else
        {
            descText.text = "로그인 중입니다.";

            StartCoroutine(_wakgames.StartLogin((profile, resCode) =>
            {
                if (profile == null)
                {
                    descText.text = $"로그인에 실패하였습니다. (Code : {resCode})";
                }
                else
                {
                    descText.text = $"{profile.name} 계정으로 로그인 되었습니다.";
                    loginButton.GetComponentInChildren<Text>().text = "Logout";

                    AppendAchievementMessage();

                    StartCoroutine(_wakgames.UnlockAchievement("first_login", (success, resCode) =>
                    {
                        if (success != null)
                        {
                            Debug.Log("첫 로그인 도전과제 달성!");
                        }
                        else if (resCode == 404)
                        {
                            Debug.LogError("존재하지 않는 도전과제.");
                        }
                        else if (resCode == 409)
                        {
                            Debug.Log("첫 로그인 도전과제 이미 달성됨.");
                        }
                        else
                        {
                            Debug.LogError($"알 수 없는 오류. (Code : {resCode})");
                        }
                    }));
                }
            }));
        }
    }

    public void OnBtnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
