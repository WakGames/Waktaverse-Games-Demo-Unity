using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Wakgames;

public class WakgamesAchieve : MonoBehaviour
{
    [SerializeField] private GameObject Alarms;
    private GameObject AchievePanel;
    private List<AchievePanel> achievePanels;

    private Wakgames m_wakgames;

    public void NewAlarm(AchievementsResultItem achievement)
    {
        // 이미지 로드 후, 알람창 띄우기
        StartCoroutine(m_wakgames.LoadImage(achievement.img, (texture, resCode) => {
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
            StartCoroutine(m_wakgames.LoadImage(achievements[i].img, (texture, resCode) => {
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
    public void RemovePanel(AchievePanel panel)
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
}
