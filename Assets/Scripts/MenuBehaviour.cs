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
                m_descText.text = $"{profile.name} �������� �α��� �Ǿ����ϴ�.";
                m_loginButton.GetComponentInChildren<Text>().text = "Logout";

                AppendAchievementMessage();
            }
            else
            {
                m_descText.text = "�α׾ƿ� �����Դϴ�.";
            }
        }));

        StartCoroutine(m_wakgames.GetStatBoard("click_cnt", (result, resCode) =>
        {
            if (result != null)
            {
                int rank = result.BoardIndex + 1;
                Debug.Log($"���� ��� : {rank}");
            }
            else
            {
                Debug.LogError($"�� �� ���� ����. (Code : {resCode})");
            }
        }));
    }

    void AppendAchievementMessage()
    {
        StartCoroutine(m_wakgames.GetUnlockedAchievements((achieves, resCode) =>
        {
            if (achieves != null)
            {
                string achieveNames = string.Join(", ", achieves.achieves.Select((a) => a.name));
                m_descText.text += $"\n�޼��� �������� : {achieves.size}��\n{achieveNames}";
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

            m_descText.text = "�α׾ƿ� �����Դϴ�.";
            m_loginButton.GetComponentInChildren<Text>().text = "Login";
        }
        else
        {
            m_descText.text = "�α��� ���Դϴ�.";

            StartCoroutine(m_wakgames.StartLogin((profile, resCode) =>
            {
                if (profile == null)
                {
                    m_descText.text = $"�α��ο� �����Ͽ����ϴ�. (Code : {resCode})";
                }
                else
                {
                    m_descText.text = $"{profile.name} �������� �α��� �Ǿ����ϴ�.";
                    m_loginButton.GetComponentInChildren<Text>().text = "Logout";

                    AppendAchievementMessage();

                    StartCoroutine(m_wakgames.UnlockAchievement("first_login", (success, resCode) =>
                    {
                        if (success != null)
                        {
                            Debug.Log("ù �α��� �������� �޼�!");
                        }
                        else if (resCode == 404)
                        {
                            Debug.LogError("�������� �ʴ� ��������.");
                        }
                        else if (resCode == 409)
                        {
                            Debug.Log("ù �α��� �������� �̹� �޼���.");
                        }
                        else
                        {
                            Debug.LogError($"�� �� ���� ����. (Code : {resCode})");
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
