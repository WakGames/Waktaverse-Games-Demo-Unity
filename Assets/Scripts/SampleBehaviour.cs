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

        UnlockAchievement("start_game", "���� ����");
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
                Debug.Log($"{name} �������� �޼�!");
            }
            else if (resCode == 404)
            {
                Debug.LogError("�������� �ʴ� ��������.");
            }
            else if (resCode == 409)
            {
                Debug.Log($"{name} �������� �̹� �޼���.");
            }
            else
            {
                Debug.LogError($"�� �� ���� ����. (Code : {resCode})");
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

                Debug.Log($"Ŭ�� �� : {num}");
            }
            else
            {
                Debug.LogError($"�� �� ���� ����. (Code : {resCode})");

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
                    Debug.Log($"Ŭ�� �� ��ϵ� : {stat.val}");
                }
                else
                {
                    Debug.LogError($"Ŭ�� �� ��� ����.");
                }

                foreach (var achieve in result.achieves)
                {
                    Debug.Log($"{achieve.name} �������� �޼�!");
                }
            }
            else
            {
                Debug.LogError($"�� �� ���� ����. (Code : {resCode})");
            }
        }));
    }
}
