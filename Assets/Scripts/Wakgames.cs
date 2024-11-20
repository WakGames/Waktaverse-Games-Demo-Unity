using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class Wakgames : MonoBehaviour
{
    private enum LoginState
    {
        Running,
        Fail,
        Success,
    }

    /// <summary>
    /// API 응답을 받을 콜백.
    /// </summary>
    /// <typeparam name="T">응답 형식.</typeparam>
    /// <param name="result">응답 데이터. 없으면 null.</param>
    /// <param name="responseCode">HTTP 응답 코드.</param>
    public delegate void CallbackDelegate<T>(T result, int responseCode) where T : class;

    [field:SerializeField]
    public string ClientId
    {
        get;
        private set;
    }
    
    [SerializeField] private int callbackServerPort;

    public const string Host = "https://waktaverse.games";

    private WakgamesCallbackServer _callbackServer = null;

    /// <summary>
    /// 토큰 저장소.
    /// 별도로 설정하지 않으면 기본 저장소를 사용합니다.
    /// </summary>
    public WakgamesTokenStorage TokenStorage
    {
        get
        {
            if (_tokenStorage == null)
            {
                _tokenStorage = this.AddComponent<WakgamesTokenStorage>();
            }
            return _tokenStorage;
        }
        set
        {
            if (_tokenStorage != null && _tokenStorage != value)
            {
                value.UpdateToken(_tokenStorage.AccessToken, _tokenStorage.RefreshToken, _tokenStorage.IdToken);
            }
            _tokenStorage = value;
        }
    }
    private WakgamesTokenStorage _tokenStorage;

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            Debug.LogError($"유효한 Client ID를 설정해야 합니다.");
            return;
        }
        
        if (callbackServerPort <= 0)
        {
            Debug.LogError($"유효한 콜백 서버 포트를 설정해야 합니다.");
            return;
        }
    }

    #region Wakgames Login

    /// <summary>
    /// 로그인 절차를 시작합니다.
    /// </summary>
    /// <param name="callback">로그인 완료 후 사용자 정보를 받을 콜백. 실패하면 null.</param>
    /// <returns></returns>
    public IEnumerator StartLogin(CallbackDelegate<UserProfileResult> callback)
    {
        string csrfState = WakgamesAuth.GenerateCsrfState();
        string codeVerifier = WakgamesAuth.GenerateCodeVerifier();
        string codeChallenge = WakgamesAuth.GenerateCodeChallenge(codeVerifier);

        if (!TryGetComponent(out _callbackServer))
        {
            _callbackServer = this.AddComponent<WakgamesCallbackServer>();
        }

        _callbackServer.StartServer(callbackServerPort);

        _callbackServer.ClientId = ClientId;
        _callbackServer.CsrfState = csrfState;
        _callbackServer.CodeVerifier = codeVerifier;

        string callbackUri = Uri.EscapeDataString($"http://localhost:{callbackServerPort}/callback");
        string authUri = $"{Host}/oauth/authorize?responseType=code&clientId={ClientId}&state={csrfState}&callbackUri={callbackUri}&challengeMethod=S256&challenge={codeChallenge}";
        Application.OpenURL(authUri);

        yield return new WaitUntil(() => GetLoginState() != LoginState.Running);
        
        EndLogin();

        yield return GetUserProfile(callback);
    }

    private LoginState GetLoginState()
    {
        if (_callbackServer.IsRunning)
        {
            return LoginState.Running;
        }

        return _callbackServer.UserWakgamesToken == null
            ? LoginState.Fail
            : LoginState.Success;
    }

    private void EndLogin()
    {
        if (_callbackServer == null || TokenStorage == null)
        {
            Debug.LogError("로그인 절차가 시작되지 않았거나 토큰 저장소가 설정되지 않았습니다.");
            return;
        }

        var token = _callbackServer.UserWakgamesToken;
        if (token != null)
        {
            TokenStorage.UpdateToken(token.accessToken, token.refreshToken, token.idToken);
        }

        DestroyImmediate(_callbackServer);
    }

    /// <summary>
    /// 저장된 토큰을 삭제하여 로그아웃합니다.
    /// </summary>
    public void Logout()
    {
        TokenStorage.ClearToken();
    }

    #endregion

    #region Wakgames API

    /// <summary>
    /// 단순 성공 응답.
    /// </summary>
    [Serializable]
    public class SuccessResult
    {
        /// <summary>
        /// 성공 여부.
        /// </summary>
        public bool success;
    }

    /// <summary>
    /// 토큰을 갱신하고 성공시 저장합니다.
    /// </summary>
    /// <param name="callback">새로 발급된 토큰 정보를 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator RefreshToken(CallbackDelegate<WakgamesToken> callback)
    {
        if (string.IsNullOrEmpty(TokenStorage.RefreshToken))
        {
            callback(null, 401);
            yield break;
        }

        string url = $"{Host}/api/oauth/refresh";
        using var webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("User-Agent", $"WakGames_Game/{ClientId}");
        webRequest.SetRequestHeader("Authorization", "Bearer " + TokenStorage.RefreshToken);

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseJson = webRequest.downloadHandler.text;
            var result = JsonUtility.FromJson<WakgamesToken>(responseJson);

            Debug.Log("Token refreshed");

            TokenStorage?.UpdateToken(result.accessToken, result.refreshToken, result.idToken);

            callback(result, (int)webRequest.responseCode);
        }
        else
        {
            callback(null, (int)webRequest.responseCode);
        }
    }

    /// <summary>
    /// 사용자 프로필.
    /// </summary>
    [Serializable]
    public class UserProfileResult
    {
        /// <summary>
        /// 사용자 ID.
        /// </summary>
        public int id;
        /// <summary>
        /// 닉네임.
        /// </summary>
        public string name;
        /// <summary>
        /// 프로필 이미지 URL.
        /// </summary>
        public string profileImg;
    }

    /// <summary>
    /// 사용자 프로필을 조회합니다.
    /// </summary>
    /// <param name="callback">사용자 프로필 정보를 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator GetUserProfile(CallbackDelegate<UserProfileResult> callback)
    {
        yield return GetMethod("api/game-link/user/profile", callback);
    }


    /// <summary>
    /// 사용자가 달성한 도전과제 목록을 얻습니다.
    /// </summary>
    /// <param name="callback">달성 도전과제 목록을 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator GetUnlockedAchievements(CallbackDelegate<AchievementsResult> callback)
    {
        yield return GetMethod("api/game-link/achieve", callback);
    }

    /// <summary>
    /// 특정 도전과제가 달성되었음을 기록합니다.
    /// </summary>
    /// <param name="achieveId">도전과제 ID.</param>
    /// <param name="callback">달성 결과를 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator UnlockAchievement(string achieveId, CallbackDelegate<SuccessResult> callback)
    {
        // 업적 달성 시, 알람 띄우기
        achieveId = Uri.EscapeDataString(achieveId);
        callback += (success, responseCode) =>
        {
            if (success != null)
            {
                StartCoroutine(GetUnlockedAchievements((result, resCode) =>
                {
                    AchievementsResultItem achieve = result.achieves.Find(item => item.id == achieveId);
                    FindObjectOfType<WakgamesAchieve>().NewAlarm(achieve);
                }));
            }
            else
            {
                Debug.Log($"Error: {responseCode}");
            }
        };
        yield return PostMethod($"api/game-link/achieve?id={achieveId}", callback);
    }

    /// <summary>
    /// 사용자의 누적 통계 값들을 얻습니다.
    /// </summary>
    /// <param name="callback">통계 목록을 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator GetStats(CallbackDelegate<GetStatsResult> callback)
    {
        yield return GetMethod("api/game-link/stat", callback);
    }
    
    /// <summary>
    /// 사용자의 대상 통계 값들을 입력합니다.
    /// </summary>
    /// <param name="stats">입력할 통계들.</param>
    /// <param name="callback">통계 입력 결과를 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator SetStats(SetStatsInput stats, CallbackDelegate<SetStatsResult> callback)
    {
        // 업적 달성 시, 알람 띄우기
        callback += (result, responseCode) =>
        {
            StartCoroutine(FindObjectOfType<WakgamesAchieve>().NewAlarms(result.achieves.ToArray()));
        };
        
        yield return PutMethod("api/game-link/stat", JsonUtility.ToJson(stats), callback);
    }
    
    /// <summary>
    /// 대상 통계의 전체 사용자 값을 조회합니다. (값 내림차순 정렬됨.)
    /// </summary>
    /// <param name="statId">대상 통계 ID.</param>
    /// <param name="callback">전체 사용자 통계를 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator GetStatBoard(string statId, CallbackDelegate<GetStatBoardResult> callback)
    {
        yield return GetMethod($"api/game-link/stat-board?id={statId}", callback);
    }

    #endregion

    #region HTTP API 기본 메서드

    private IEnumerator GetMethod<T>(string api, CallbackDelegate<T> callback) where T : class
    {
        yield return ApiMethod(() => UnityWebRequest.Get($"{Host}/{api}"), callback);
    }

    private IEnumerator PostMethod<T>(string api, CallbackDelegate<T> callback) where T : class
    {
        yield return ApiMethod(() => UnityWebRequest.PostWwwForm($"{Host}/{api}", null), callback);
    }

    private IEnumerator PutMethod<T>(string api, string body, CallbackDelegate<T> callback) where T : class
    {
        yield return ApiMethod(() => UnityWebRequest.Put($"{Host}/{api}", Encoding.UTF8.GetBytes(body)), callback);
    }

    private IEnumerator ApiMethod<T>(Func<UnityWebRequest> webRequestFactory, CallbackDelegate<T> callback, int maxRetry = 1) where T : class
    {
        if (string.IsNullOrEmpty(TokenStorage.AccessToken))
        {
            callback(null, 401);
            yield break;
        }

        using var webRequest = webRequestFactory();
        webRequest.SetRequestHeader("User-Agent", $"WakGames_Game/{ClientId}");
        webRequest.SetRequestHeader("Authorization", "Bearer " + TokenStorage.AccessToken);
        webRequest.SetRequestHeader("Content-Type", "application/json");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseJson = webRequest.downloadHandler.text;
            var result = JsonUtility.FromJson<T>(responseJson);
            callback(result, (int)webRequest.responseCode);
            yield break;
        }
        
        if (webRequest.responseCode == 401 && maxRetry > 0)
        {
            WakgamesToken token = null;
            yield return RefreshToken((t, _) => token = t);

            if (token != null)
            {
                yield return ApiMethod(webRequestFactory, callback, maxRetry - 1);
            }
            else
            {
                callback(null, (int)webRequest.responseCode);
            }
        }
        else
        {
            callback(null, (int)webRequest.responseCode);
        }
    }

    #endregion
}
