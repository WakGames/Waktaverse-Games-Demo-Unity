using System.Collections;
using System.Collections.Generic;
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
        StartCoroutine(m_wakgames.GetUserProfile((profile) =>
        {
            if (profile != null)
            {
                m_descText.text = $"{profile.name} 계정으로 로그인 되었습니다.";
                m_loginButton.GetComponentInChildren<Text>().text = "Logout";
            }
            else
            {
                m_descText.text = "로그아웃 상태입니다.";
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

            StartCoroutine(m_wakgames.StartLogin((profile) =>
            {
                if (profile == null)
                {
                    m_descText.text = "로그인에 실패하였습니다.";
                }
                else
                {
                    m_descText.text = $"{profile.name} 계정으로 로그인 되었습니다.";
                    m_loginButton.GetComponentInChildren<Text>().text = "Logout";
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
