using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SampleBehaviour : MonoBehaviour
{
    [SerializeField] private Wakgames wakgames;
    [SerializeField] private Text numText;
    private int _num;

    void Start()
    {
        numText.text = "Loading";
        LoadClickCount();

        UnlockAchievement("start_game", "게임 시작");
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("Counter", _num);
    }

    public void OnBtnAddClicked()
    {
        _num += 1;
        numText.text = $"{_num}";
        
        if (_num % 10 == 0)
        {
            SaveClickCount();
        }
    }
    public void OnBtnResetClicked()
    {
        _num = 0;
        numText.text = "0";
        SaveClickCount();
        UnlockAchievement("reset", "큰 결심");
    }
    public void OnHiddenClicked()
    {
        UnlockAchievement("follow_ine", "쉿.");
    }

    private void UnlockAchievement(string id, string name)
    {
        StartCoroutine(wakgames.UnlockAchievement(id, (success, resCode) =>
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
        StartCoroutine(wakgames.GetStats((stats, resCode) =>
        {
            if (stats != null)
            {
                var stat = stats.stats.Find((s) => s.id == "click_cnt");
                int num = stat?.val ?? 0;

                _num = num;
                numText.text = num.ToString();
                PlayerPrefs.SetInt("Counter", num);

                Debug.Log($"클릭 수 : {num}");
            }
            else
            {
                Debug.LogError($"알 수 없는 오류. (Code : {resCode})");

                _num = PlayerPrefs.GetInt("Counter", 0);
                numText.text = _num.ToString();
            }
        }));
    }

    private void SaveClickCount()
    {
        PlayerPrefs.SetInt("Counter", _num);

        var stats = new SetStatsInput { { "click_cnt", _num } };
        StartCoroutine(wakgames.SetStats(stats, (result, resCode) =>
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
