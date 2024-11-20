using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class AchievePanel : MonoBehaviour
{
    [FormerlySerializedAs("Icon")]
    [Header("UI")]
    [SerializeField] private RawImage icon;
    [FormerlySerializedAs("Name")] [SerializeField] private TextMeshProUGUI nameText;
    [FormerlySerializedAs("Description")] [SerializeField] private TextMeshProUGUI descriptionText;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private AudioSource _audioSource;

    private WakgamesAchieve _wakgamesAchieve;

    private int _direction;

    private float _appearedAt;

    private const float Height = 120f;

    public void Setup(string name, string desc, Texture2D texture, int index = -1)
    {
        // Init info
        _canvasGroup.alpha = 1;
        nameText.text = name;
        descriptionText.text = desc;
        _audioSource.Play();
        if(texture != null)
            icon.texture = texture;
        // Get Direction
        _wakgamesAchieve = FindObjectOfType<WakgamesAchieve>();
        _direction = _wakgamesAchieve.animationPosition == WakgamesAchievementAlarmAnimationPosition.Top ? -1 : 1;
        // 순서대로 추가하는지 여부 확인
        if(index == -1)
            Appear();
        else
            SlideUp(index);
    }
    public void SlideUp()
    {
        // 사라지고 있다면 IEHide를 종료시키지 않는다.
        if(_canvasGroup.alpha == 1)
            StopAllCoroutines();
        StartCoroutine(IESlideUp());
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _audioSource = GetComponentInChildren<AudioSource>();
    }
    private void Update()
    {
        if(Time.time - _appearedAt > 3f)
        {
            StartCoroutine(IEHide());
            _appearedAt = 9999;
        }
    }
    private void OnDisable()
    {
        _wakgamesAchieve.RemoveAlarm(this);
    }

    private void Appear()
    {
        _appearedAt = Time.time;
        StartCoroutine(IEShow());
    }
    private void SlideUp(int index)
    {
        _appearedAt = Time.time;
        StartCoroutine(IESlideUp(index));
    }

    private IEnumerator IEShow()
    {
        const float DESTINATION = 1f;
        float time = 0f;

        _rectTransform.anchoredPosition = GetInitalPosition();
        float destinationY = _rectTransform.anchoredPosition.y + Height * _direction;

        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            _rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(_rectTransform.anchoredPosition.y, destinationY, time / DESTINATION));
            yield return null;
        }
        _rectTransform.anchoredPosition = new Vector2(0, destinationY);
    }
    private IEnumerator IEHide()
    {
        const float DESTINATION = 0.6f;
        float time = 0f;
        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 0f, time / DESTINATION);
            yield return null;
        }

        gameObject.SetActive(false);
    }
    private IEnumerator IESlideUp()
    {
        const float DESTINATION = 1f;
        float time = 0f;

        float destinationY = _rectTransform.anchoredPosition.y + Height * _direction;

        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            _rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(_rectTransform.anchoredPosition.y, destinationY, time / DESTINATION));
            yield return null;
        }
        _rectTransform.anchoredPosition = new Vector2(0, destinationY);
    }
    private IEnumerator IESlideUp(int index)
    {
        const float DESTINATION = 0.8f;
        float time = 0f;
        _rectTransform.anchoredPosition = GetInitalPosition();
        float destinationY = _rectTransform.anchoredPosition.y + Height * (index + 1) * _direction;
        while(time < DESTINATION)
        {
            time += Time.deltaTime;
            _rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(_rectTransform.anchoredPosition.y, destinationY, time / DESTINATION));
            yield return null;
        }
        _rectTransform.anchoredPosition = new Vector2(0, destinationY);
    }

    private Vector2 GetInitalPosition() => new Vector2(0, -600f) * _direction;
}
