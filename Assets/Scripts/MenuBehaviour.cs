using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuBehaviour : MonoBehaviour
{
    [SerializeField]
    private Wakgames m_wakgames;
    [SerializeField]
    private Text m_descText;
    [SerializeField]
    private Button m_loginButton;

    void Start()
    {
        StartCoroutine(m_wakgames.GetUserProfile((profile, _) =>
        {
            if (profile != null)
            {
                m_descText.text = $"{profile.name} 계정으로 로그인 되었습니다.";
                m_loginButton.GetComponentInChildren<Text>().text = "Logout";

                AppendAchievementMessage();
            }
            else
            {
                m_descText.text = "로그아웃 상태입니다.";
            }
        }));

        StartCoroutine(m_wakgames.GetStatBoard("click_cnt", (result, resCode) =>
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
        StartCoroutine(m_wakgames.GetUnlockedAchievements((result, resCode) =>
        {
            if (result != null)
            {
                string achieveNames = string.Join(", ", result.achieves.Select((a) => a.name));
                m_descText.text += $"\n달성한 도전과제 : {result.size}개\n{achieveNames}";
            }
        }));
    }

    public void OnBtnStartClicked()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void OnBtnLoginClicked()
    {
        if (m_loginButton.GetComponentInChildren<Text>().text == "Logout")
        {
            m_wakgames.Logout();

            m_descText.text = "로그아웃 상태입니다.";
            m_loginButton.GetComponentInChildren<Text>().text = "Login";
        }
        else
        {
            m_descText.text = "로그인 중입니다.";

            StartCoroutine(m_wakgames.StartLogin((profile, resCode) =>
            {
                if (profile == null)
                {
                    m_descText.text = $"로그인에 실패하였습니다. (Code : {resCode})";
                }
                else
                {
                    m_descText.text = $"{profile.name} 계정으로 로그인 되었습니다.";
                    m_loginButton.GetComponentInChildren<Text>().text = "Logout";

                    AppendAchievementMessage();

                    StartCoroutine(m_wakgames.UnlockAchievement("first_login", (success, resCode) =>
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
