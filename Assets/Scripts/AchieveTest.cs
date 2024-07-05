using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchieveTest : MonoBehaviour
{
    [SerializeField] private GameObject Alarms;
    [SerializeField] private GameObject AchievePanel;
    private List<AchievePanel> achievePanels;

    public void OnTestClick()
    {
        // 전 알림창 전부 올리기
        foreach(AchievePanel _panel in achievePanels)
            _panel.SlideUp();
        // 새로운 알림창 생성
        GameObject achievePanel = GetActivePanel();
        AchievePanel panel = achievePanel.GetComponent<AchievePanel>();
        panel.Setup(new()
        {
            name = "게임 시작",
            desc = "게임을 시작합니다."
        });
        panel.Appear();
        achievePanels.Add(panel);
    }
    public void RemovePanel(AchievePanel panel)
    {
        achievePanels.Remove(panel);
    }

    private void Awake()
    {
        achievePanels = new List<AchievePanel>(){};
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
