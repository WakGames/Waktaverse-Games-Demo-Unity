using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuBehaviour : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button quitButton;
    private Wakgames _wakgames;
    private TextMeshProUGUI _loginButtonText;

    private void Awake()
    {
        _wakgames = FindObjectOfType<Wakgames>();
        
        startButton.onClick.AddListener(OnStartButtonClicked);
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
    }

    private void Start()
    {
        StartCoroutine(_wakgames.GetUserProfile((profile, _) =>
        {
            if (profile != null)
            {
                descText.text = $"{profile.name} 계정으로 로그인 되었습니다.";
                _loginButtonText.text = "Logout";

                AppendAchievementMessage();
            }
            else
            {
                descText.text = "로그아웃 상태입니다.";
            }
        }));

        StartCoroutine(_wakgames.GetStatBoard("click_cnt", (result, resCode) =>
        {
            if (result != null)
            {
                int rank = result.BoardIndex + 1;
                Debug.Log($"현재 등수 : {rank}");
            }
            else
            {
                Debug.LogError($"알 수 없는 오류. (Code : {resCode})");
            }
        }));
    }

    private void AppendAchievementMessage()
    {
        StartCoroutine(_wakgames.GetUnlockedAchievements((result, resCode) =>
        {
            if (result != null)
            {
                string achieveNames = string.Join(", ", result.achieves.Select((a) => a.name));
                descText.text += $"\n달성한 도전과제 : {result.size}개\n{achieveNames}";
            }
        }));
    }

    private void OnStartButtonClicked()
    {
        SceneManager.LoadScene("SampleScene");
    }

    private void OnLoginButtonClicked()
    {
        if (_loginButtonText.text == "Logout")
        {
            _wakgames.Logout();

            descText.text = "로그아웃 상태입니다.";
            _loginButtonText.text = "Login";
        }
        else
        {
            descText.text = "로그인 중입니다.";

            StartCoroutine(_wakgames.StartLogin((profile, resCode) =>
            {
                if (profile == null)
                {
                    descText.text = $"로그인에 실패하였습니다. (Code : {resCode})";
                }
                else
                {
                    descText.text = $"{profile.name} 계정으로 로그인 되었습니다.";
                    _loginButtonText.text = "Logout";

                    AppendAchievementMessage();

                    StartCoroutine(_wakgames.UnlockAchievement("first_login", (success, resCode) =>
                    {
                        if (success != null)
                        {
                            Debug.Log("첫 로그인 도전과제 달성!");
                        }
                        else if (resCode == 404)
                        {
                            Debug.LogError("존재하지 않는 도전과제.");
                        }
                        else if (resCode == 409)
                        {
                            Debug.Log("첫 로그인 도전과제 이미 달성됨.");
                        }
                        else
                        {
                            Debug.LogError($"알 수 없는 오류. (Code : {resCode})");
                        }
                    }));
                }
            }));
        }
    }

    private void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
