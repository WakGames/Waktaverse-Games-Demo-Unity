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

        if (m_num == 10)
        {
            UnlockAchievement("counter_10", "10�� Ŭ��");
        }
        else if (m_num == 100)
        {
            UnlockAchievement("counter_100", "100�� Ŭ��");
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
}
