using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using static Wakgames;

public enum WakgamesAchievementAlarmPosition
{
    Top,
    Bottom
}

public class WakgamesAchieve : MonoBehaviour
{
    private readonly List<AchievePanel> _achievePanels = new();
    private readonly Queue<AchievePanel> _achievePanelPool = new();
    
    private AchievePanel _achievePanel;
    private AudioSource _audioSource;
    private Wakgames _wakgames;
    
    [SerializeField] private WakgamesAchievementAlarmPosition position;

    public void TestAlarm()
    {
        PopupAlarm("Test", "Test", null);
    }

    public void PopupAlarm(string achieveName, string achieveDesc, Texture2D texture)
    {
        foreach (AchievePanel panel in _achievePanels)
            panel.SlideUp();
        
        // 새로운 알림창 생성
        AchievePanel achievePanel = GetActivePanel();
        achievePanel.SetOnHide(() => RemoveAlarm(achievePanel));
        achievePanel.Setup(achieveName, achieveDesc, texture, position);
        _achievePanels.Add(achievePanel);
        _audioSource.Play();
    }

    public void AddAlarm(AchievementsResultItem achievement)
    {
        // 이미지 로드 후, 알람창 띄우기
        StartCoroutine(LoadImage(achievement.img,
            (texture, resCode) =>
                PopupAlarm(achievement.name, achievement.desc, texture)));
    }
    
    public IEnumerator AddAlarms(SetStatsResultAchieveItem[] achievements)
    {
        Dictionary<int, Texture2D> textures = new();
        
        // 이미지 전부 로드하기
        for(int i = 0; i < achievements.Length; i++)
        {
            int index = i;
            StartCoroutine(LoadImage(achievements[i].img,
                (texture, resCode) => textures.Add(index, texture)));
        }
        
        // 로드 끝날 때까지 대기
        yield return new WaitUntil(() => textures.Count == achievements.Length);
        
        // 알람창 띄우기
        for(int i = 0; i < achievements.Length; i++)
        {
            PopupAlarm(achievements[i].name, achievements[i].desc, textures[i]);
        }
    }
    
    private void RemoveAlarm(AchievePanel panel)
    {
        panel.gameObject.SetActive(false);
        _achievePanels.Remove(panel);
        _achievePanelPool.Enqueue(panel);
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _achievePanel = Resources.Load<AchievePanel>("Prefabs/AchievePanel");
        _wakgames = GetComponent<Wakgames>() ?? FindObjectOfType<Wakgames>();
        
        if (!_wakgames)
        {
            Debug.Log("Not found Wakgames");
            Destroy(gameObject);
        }
    }

    private AchievePanel GetActivePanel()
    {
        if (_achievePanelPool.Count > 0)
        {
            AchievePanel panel = _achievePanelPool.Dequeue();
            panel.gameObject.SetActive(true);
            return panel;
        }
        
        return Instantiate(_achievePanel, transform, true);
    }

    private IEnumerator LoadImage(string img, CallbackDelegate<Texture2D> callback)
    {
        // 이미지 주소가 없으면 종료
        if (img == null)
        {
            callback(null, -1);
            yield break;
        }

        if (!img.StartsWith($"{Host}/img"))
            img = $"{Host}/img/{img}";
        
        UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(img);
        webRequest.SetRequestHeader("User-Agent", $"WakGames_Game/{_wakgames.ClientId}");

        yield return webRequest.SendWebRequest();

        callback(
            webRequest.result == UnityWebRequest.Result.Success 
                ? DownloadHandlerTexture.GetContent(webRequest) : null,
            (int)webRequest.responseCode);
    }
}
