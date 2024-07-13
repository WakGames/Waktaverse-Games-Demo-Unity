using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Wakgames;

public class AchievePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage Icon;
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private TextMeshProUGUI Description;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private WakgamesAchieve m_wakgamesAchieve;

    private float appearedAt;

    private readonly Vector2 START = new Vector2(0, -360f);
    private readonly float HEIGHT = 120f;

    public void Setup(string name, string desc, Texture2D texture, int index = -1)
    {
        canvasGroup.alpha = 1;
        Name.text = name;
        Description.text = desc;
        if(texture != null)
            Icon.texture = texture;
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
    private void Start()
    {
        m_wakgamesAchieve = FindObjectOfType<WakgamesAchieve>();
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
        m_wakgamesAchieve.RemovePanel(this);
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
        rectTransform.anchoredPosition = START;
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
        const float DESTINATION = 1f;
        float time = 0f;
        float destinationY = rectTransform.anchoredPosition.y + HEIGHT;
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
        rectTransform.anchoredPosition = START;
        float destinationY = rectTransform.anchoredPosition.y + HEIGHT * (index + 1);
        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(rectTransform.anchoredPosition.y, destinationY, time / DESTINATION));
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(0, destinationY);
    }
}
