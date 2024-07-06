using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static Wakgames;

public class AchievePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private TextMeshProUGUI Description;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private AchieveTest achieveTest;

    private float appearedAt;

    public void Setup(AchievementsResultItem achievement)
    {
        canvasGroup.alpha = 1;
        Name.text = achievement.name;
        Description.text = achievement.desc;
    }
    public void Appear()
    {
        appearedAt = Time.time;
        StartCoroutine(IEShow());
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
    private void Start()
    {
        achieveTest = FindObjectOfType<AchieveTest>();
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
        achieveTest.RemovePanel(this);
    }

    private IEnumerator IEShow()
    {
        rectTransform.anchoredPosition = new Vector2(0, -360);
        const float DESTINATION = 1f;
        float time = 0f;
        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(rectTransform.anchoredPosition.y, -240f, time / DESTINATION));
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(0, -240);
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
        float endY = rectTransform.anchoredPosition.y + 120f;
        const float DESTINATION = 1f;
        float time = 0f;
        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(rectTransform.anchoredPosition.y, endY, time / DESTINATION));
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(0, endY);
    }
}
