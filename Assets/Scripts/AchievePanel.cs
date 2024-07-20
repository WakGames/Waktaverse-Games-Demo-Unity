using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage Icon;
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private TextMeshProUGUI Description;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private WakgamesAchieve m_wakgamesAchieve;

    private int direction;

    private float appearedAt;

    private readonly float HEIGHT = 120f;

    public void Setup(string name, string desc, Texture2D texture, int index = -1)
    {
        // Init info
        canvasGroup.alpha = 1;
        Name.text = name;
        Description.text = desc;
        if(texture != null)
            Icon.texture = texture;
        // Get Direction
        m_wakgamesAchieve = FindObjectOfType<WakgamesAchieve>();
        direction = m_wakgamesAchieve.animationPosition == WakgamesAchievementAlarmAnimationPosition.Top ? -1 : 1;
        // 순서대로 추가하는지 여부 확인
        if(index == -1)
            Appear();
        else
            SlideUp(index);
    }
    public void SlideUp()
    {
        // 사라지고 있다면 IEHide를 종료시키지 않는다.
        if(canvasGroup.alpha == 1)
            StopAllCoroutines();
        StartCoroutine(IESlideUp());
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    private void Update()
    {
        if(Time.time - appearedAt > 3f)
        {
            StartCoroutine(IEHide());
            appearedAt = 9999;
        }
    }
    private void OnDisable()
    {
        m_wakgamesAchieve.RemoveAlarm(this);
    }

    private void Appear()
    {
        appearedAt = Time.time;
        StartCoroutine(IEShow());
    }
    private void SlideUp(int index)
    {
        appearedAt = Time.time;
        StartCoroutine(IESlideUp(index));
    }

    private IEnumerator IEShow()
    {
        const float DESTINATION = 1f;
        float time = 0f;

        rectTransform.anchoredPosition = GetInitalPosition();
        float destinationY = rectTransform.anchoredPosition.y + HEIGHT * direction;

        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(rectTransform.anchoredPosition.y, destinationY, time / DESTINATION));
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(0, destinationY);
    }
    private IEnumerator IEHide()
    {
        const float DESTINATION = 0.6f;
        float time = 0f;
        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, time / DESTINATION);
            yield return null;
        }

        gameObject.SetActive(false);
    }
    private IEnumerator IESlideUp()
    {
        const float DESTINATION = 1f;
        float time = 0f;

        float destinationY = rectTransform.anchoredPosition.y + HEIGHT * direction;

        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(rectTransform.anchoredPosition.y, destinationY, time / DESTINATION));
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(0, destinationY);
    }
    private IEnumerator IESlideUp(int index)
    {
        const float DESTINATION = 0.8f;
        float time = 0f;
        rectTransform.anchoredPosition = GetInitalPosition();
        float destinationY = rectTransform.anchoredPosition.y + HEIGHT * (index + 1) * direction;
        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(rectTransform.anchoredPosition.y, destinationY, time / DESTINATION));
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(0, destinationY);
    }

    private Vector2 GetInitalPosition() => new Vector2(0, -600f) * direction;
}
