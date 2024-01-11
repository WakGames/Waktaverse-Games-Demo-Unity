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
        m_numText.text = "Loading";
        LoadClickCount();

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

        if (m_num % 10 == 0)
        {
            SaveClickCount();
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

    private void LoadClickCount()
    {
        StartCoroutine(m_wakgames.GetStats((stats, resCode) =>
        {
            if (stats != null)
            {
                var stat = stats.stats.Find((s) => s.id == "click_cnt");
                int num = stat?.val ?? 0;

                m_num = num;
                m_numText.text = num.ToString();
                PlayerPrefs.SetInt("Counter", num);

                Debug.Log($"클릭 수 : {num}");
            }
            else
            {
                Debug.LogError($"알 수 없는 오류. (Code : {resCode})");

                m_num = PlayerPrefs.GetInt("Counter", 0);
                m_numText.text = m_num.ToString();
            }
        }));
    }

    private void SaveClickCount()
    {
        PlayerPrefs.SetInt("Counter", m_num);

        var stats = new Wakgames.SetStatsInput { { "click_cnt", m_num } };
        StartCoroutine(m_wakgames.SetStats(stats, (result, resCode) =>
        {
            if (result != null)
            {
                var stat = result.stats.Find((s) => s.id == "click_cnt");
                if (stat != null)
                {
                    Debug.Log($"클릭 수 기록됨 : {stat.val}");
                }
                else
                {
                    Debug.LogError($"클릭 수 기록 실패.");
                }

                foreach (var achieve in result.achieves)
                {
                    Debug.Log($"{achieve.name} 도전과제 달성!");
                }
            }
            else
            {
                Debug.LogError($"알 수 없는 오류. (Code : {resCode})");
            }
        }));
    }
}
