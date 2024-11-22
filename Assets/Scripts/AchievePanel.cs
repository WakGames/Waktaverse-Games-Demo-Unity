using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class AchievePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;

    private const float Height = 80f;
    private const float MoveTime = 0.5f;
    private const float WaitTime = 2f;
    private const float FadeTime = 0.5f;
    
    private int _direction;
    private int _index = 0;
    
    private Coroutine _slideCoroutine = null;
    private UnityAction _onHide;
    
    public void SetOnHide(UnityAction onHide)
    {
        _onHide = onHide;
    }

    public void Setup(string achieveName, string achieveDesc, Texture2D texture, WakgamesAchievementAlarmPosition position)
    {
        nameText.text = achieveName;
        descriptionText.text = achieveDesc;
        _direction = (int)position;
        
        if (texture)
            icon.texture = texture;
        
        ResetIndex();
        SlideUp();
        StartCoroutine(WaitForFade());
    }

    private void ResetIndex()
    {
        _index = 0;
        _rectTransform.anchoredPosition = GetPanelPivotLocation(_index);
        _canvasGroup.alpha = 1f;
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }
    
    public void SlideUp()
    {
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }
        
        _index++;
        _slideCoroutine = StartCoroutine(SlideUpCoroutine());
    }

    private IEnumerator SlideUpCoroutine()
    {
        float time = 0f;
        Vector2 origin = _rectTransform.anchoredPosition;
        Vector2 destination = GetPanelPivotLocation(_index);
        
        while(time < MoveTime)
        {
            time += Time.deltaTime;
            _rectTransform.anchoredPosition =
                Vector2.Lerp(origin, destination, Mathf.SmoothStep(0f, 1f, time / MoveTime));
            yield return null;
        }
        
        _rectTransform.anchoredPosition = destination;
    }
    
    private IEnumerator WaitForFade()
    {
        yield return new WaitForSeconds(WaitTime);
        yield return FadeCoroutine();
    }

    private IEnumerator FadeCoroutine()
    {
        float time = 0f;
        
        while(time < FadeTime)
        {
            time += Time.deltaTime;
            _canvasGroup.alpha = Mathf.SmoothStep(1f, 0f, time / FadeTime);
            yield return null;
        }
        
        _canvasGroup.alpha = 0f;
        _onHide?.Invoke();
    }

    private Vector2 GetPanelPivotLocation(int idx)
    {
        return new Vector2(815f, (580f - idx * Height) * _direction);
    }
}