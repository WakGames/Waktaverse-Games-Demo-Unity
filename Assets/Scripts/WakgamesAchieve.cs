using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static Wakgames;

public enum WakgamesAchievementAlarmAnimationPosition
{
    Top,
    Bottom
}

public class WakgamesAchieve : MonoBehaviour
{
    [SerializeField] private GameObject Alarms;
    private GameObject AchievePanel;
    private List<AchievePanel> achievePanels;

    public WakgamesAchievementAlarmAnimationPosition animationPosition;

    private Wakgames m_wakgames;

    public void NewAlarm(AchievementsResultItem achievement)
    {
        // 이미지 로드 후, 알람창 띄우기
        StartCoroutine(LoadImage(achievement.img, (texture, resCode) => {
            // 전 알림창 전부 올리기
            foreach(AchievePanel _panel in achievePanels)
                _panel.SlideUp();
            // 새로운 알림창 생성
            GameObject achievePanel = GetActivePanel();
            AchievePanel panel = achievePanel.GetComponent<AchievePanel>();
            panel.Setup(achievement.name, achievement.desc, texture);
            achievePanels.Add(panel);
        }));
    }
    public IEnumerator NewAlarms(SetStatsResultAchieveItem[] achievements)
    {
        Dictionary<int, Texture2D> textures = new(){};
        // 이미지 전부 로드하기
        for(int i = 0; i < achievements.Length; i++)
        {
            int index = i;
            StartCoroutine(LoadImage(achievements[i].img, (texture, resCode) => {
                textures.Add(index, texture);
            }));
        }
        // 로드 끝날 때까지 대기
        while(textures.Count < achievements.Length)
            yield return null;
        // 그전거 올리기
        foreach(AchievePanel panel in achievePanels)
        {
            panel.SlideUp();
        }
        // 알람창 띄우기
        for(int i = 0; i < achievements.Length; i++)
        {
            AchievePanel panel = GetActivePanel().GetComponent<AchievePanel>();
            panel.Setup(achievements[i].name, achievements[i].desc, textures[i], i);
            achievePanels.Add(panel);
        }
    }
    public void RemoveAlarm(AchievePanel panel)
    {
        achievePanels.Remove(panel);
    }

    private void Start()
    {
        achievePanels = new List<AchievePanel>();
        AchievePanel = Resources.Load<GameObject>("Prefabs/AchievePanel");
        m_wakgames = GetComponent<Wakgames>() ?? FindObjectOfType<Wakgames>();
        if(m_wakgames == null)
        {
            Debug.Log("Not found Wakgames");
            Destroy(gameObject);
        }
    }

    private GameObject GetActivePanel()
    {
        // 기존에 생성된 알림창 가져오기
        for(int i = 0; i < Alarms.transform.childCount; i++)
        {
            GameObject child = Alarms.transform.GetChild(i).gameObject;
            if(!child.activeSelf)
            {
                child.SetActive(true);
                return child;
            }
        }
        return Instantiate(AchievePanel, Alarms.transform, false);
    }

    private IEnumerator LoadImage(string img, CallbackDelegate<Texture2D> callback)
    {
        // 이미지 주소가 없으면 종료
        if(img == null)
        {
            callback(null, -1);
            yield break;
        }
        if(!img.StartsWith($"{HOST}/img"))
            img = $"{HOST}/img/{img}";
        UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(img);
        webRequest.SetRequestHeader("User-Agent", $"WakGames_Game/{m_wakgames.ClientId}");

        yield return webRequest.SendWebRequest();

        if(webRequest.result == UnityWebRequest.Result.Success)
        {
            callback(DownloadHandlerTexture.GetContent(webRequest), (int)webRequest.responseCode);
        } else
        {
            callback(null, (int)webRequest.responseCode);
        }
    }
}
