using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SampleBehaviour : MonoBehaviour
{
    [SerializeField]
    private Wakgames m_wakgames;
    [SerializeField]
    private Text m_numText;

    private int m_num;

    void Start()
    {
        m_num = PlayerPrefs.GetInt("Counter", 0);
        m_numText.text = m_num.ToString();

        UnlockAchievement("start_game", "게임 시작");
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("Counter", m_num);
    }

    public void OnBtnAddClicked()
    {
        m_num += 1;
        m_numText.text = m_num.ToString();

        if (m_num == 10)
        {
            UnlockAchievement("counter_10", "10번 클릭");
        }
        else if (m_num == 100)
        {
            UnlockAchievement("counter_100", "100번 클릭");
        }
    }

    private void UnlockAchievement(string id, string name)
    {
        StartCoroutine(m_wakgames.UnlockAchievement(id, (success, resCode) =>
        {
            if (success != null)
            {
                Debug.Log($"{name} 도전과제 달성!");
            }
            else if (resCode == 404)
            {
                Debug.LogError("존재하지 않는 도전과제.");
            }
            else if (resCode == 409)
            {
                Debug.Log($"{name} 도전과제 이미 달성됨.");
            }
            else
            {
                Debug.LogError($"알 수 없는 오류. (Code : {resCode})");
            }
        }));
    }
}
