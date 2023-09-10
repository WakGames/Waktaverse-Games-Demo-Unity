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

    void Start()
    {
        StartCoroutine(m_wakgames.UnlockAchievement("start_game", (success, resCode) =>
        {
            if (success != null)
            {
                Debug.Log("���� ���� �������� �޼�!");
            }
            else if (resCode == 404)
            {
                Debug.LogError("�������� �ʴ� ��������.");
            }
            else if (resCode == 409)
            {
                Debug.Log("���� ���� �������� �̹� �޼���.");
            }
            else
            {
                Debug.LogError($"�� �� ���� ����. (Code : {resCode})");
            }
        }));
    }
}
