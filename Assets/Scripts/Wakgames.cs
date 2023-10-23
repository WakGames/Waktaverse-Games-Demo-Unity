using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

    [SerializeField]
    private string ClientId;
    [SerializeField]
    private int CallbackServerPort;

#if WAKGAMES_DEMO
    public const string HOST = "https://wakgames-test.neurowhai.cf";
#else
    public const string HOST = "https://waktaverse.games";
#endif

    /// <summary>
    /// 토큰 저장소.
    /// 별도로 설정하지 않으면 기본 저장소를 사용합니다.
    /// </summary>
    public IWakgamesTokenStorage TokenStorage
    {
        get
        {
            if (m_tokenStorage == null)
            {
                m_tokenStorage = this.AddComponent<WakgamesDefaultTokenStorage>();
            }
            return m_tokenStorage;
        }
        set
        {
            if (m_tokenStorage != null && m_tokenStorage != value)
            {
                value.UpdateToken(m_tokenStorage.AccessToken, m_tokenStorage.RefreshToken, m_tokenStorage.IdToken);
            }
            m_tokenStorage = value;
        }
    }
    private IWakgamesTokenStorage m_tokenStorage;

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new Exception($"유효한 Client ID를 설정해야 합니다.");
        }
        if (CallbackServerPort <= 0)
        {
            throw new Exception($"유효한 콜백 서버 포트를 설정해야 합니다.");
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

        var callbackServer = this.GetComponent<WakgamesCallbackServer>();
        if (callbackServer == null)
        {
            callbackServer = this.AddComponent<WakgamesCallbackServer>();
        }

        callbackServer.StartServer(CallbackServerPort);

        callbackServer.ClientId = ClientId;
        callbackServer.CsrfState = csrfState;
        callbackServer.CodeVerifier = codeVerifier;

        string callbackUri = Uri.EscapeDataString($"http://localhost:{CallbackServerPort}/callback");
        string authUri = $"{HOST}/oauth/authorize?responseType=code&clientId={ClientId}&state={csrfState}&callbackUri={callbackUri}&challengeMethod=S256&challenge={codeChallenge}";
        Application.OpenURL(authUri);

        while (true)
        {
            var state = GetLoginState();
            if (state != LoginState.Running)
            {
                EndLogin();

                yield return StartCoroutine(GetUserProfile(callback));

                yield break;
            }

            yield return null;
        }
    }

    private LoginState GetLoginState()
    {
        var callbackServer = this.GetComponent<WakgamesCallbackServer>();
        if (callbackServer == null)
        {
            throw new InvalidOperationException();
        }

        if (callbackServer.IsRunning)
        {
            return LoginState.Running;
        }

        var token = callbackServer.UserToken;
        if (token == null)
        {
            return LoginState.Fail;
        }

        return LoginState.Success;
    }

    private void EndLogin()
    {
        var callbackServer = this.GetComponent<WakgamesCallbackServer>();
        if (callbackServer == null || TokenStorage == null)
        {
            throw new InvalidOperationException();
        }

        var token = callbackServer.UserToken;
        if (token != null)
        {
            TokenStorage.UpdateToken(token.accessToken, token.refreshToken, token.idToken);
        }

        Destroy(callbackServer);
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
    /// 토큰 갱신 응답.
    /// </summary>
    [Serializable]
    public class RefreshTokenResult
    {
        /// <summary>
        /// 접근 토큰.
        /// </summary>
        public string accessToken;
        /// <summary>
        /// 갱신 토큰.
        /// </summary>
        public string refreshToken;
        /// <summary>
        /// 사용자 ID.
        /// </summary>
        public int idToken;
    }

    /// <summary>
    /// 토큰을 갱신하고 성공시 저장합니다.
    /// </summary>
    /// <param name="callback">새로 발급된 토큰 정보를 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator RefreshToken(CallbackDelegate<RefreshTokenResult> callback)
    {
        if (string.IsNullOrEmpty(TokenStorage.RefreshToken))
        {
            callback(null, 401);
            yield break;
        }

        string url = $"{HOST}/api/oauth/refresh";
        using var webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("Authorization", "Bearer " + TokenStorage.RefreshToken);

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseJson = webRequest.downloadHandler.text;
            var result = JsonUtility.FromJson<RefreshTokenResult>(responseJson);

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
        yield return StartCoroutine(GetMethod("api/game-link/user/profile", callback));
    }

    /// <summary>
    /// 한 도전과제 정보.
    /// </summary>
    [Serializable]
    public class AchievementsResultItem
    {
        /// <summary>
        /// 도전과제 ID.
        /// </summary>
        public string id;
        /// <summary>
        /// 도전과제 이름.
        /// </summary>
        public string name;
        /// <summary>
        /// 도전과제 설명.
        /// </summary>
        public string desc;
        /// <summary>
        /// 도전과제 아이콘 이미지 URL.
        /// </summary>
        public string img;
        /// <summary>
        /// 도전과제 달성 시간. (UNIX 시간(ms))
        /// </summary>
        public long regDate;
        /// <summary>
        /// 연동된 통계 ID. (없으면 공백.)
        /// </summary>
        public string statId;
        /// <summary>
        ///  연동된 통계 목푯값. (없으면 0.)
        /// </summary>
        public int targetStatVal;
    }

    /// <summary>
    /// 도전과제 목록.
    /// </summary>
    [Serializable]
    public class AchievementsResult
    {
        /// <summary>
        /// 개수.
        /// </summary>
        public int size;
        /// <summary>
        /// 도전과제 목록.
        /// </summary>
        public List<AchievementsResultItem> achieves;
    }

    /// <summary>
    /// 사용자가 달성한 도전과제 목록을 얻습니다.
    /// </summary>
    /// <param name="callback">달성 도전과제 목록을 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator GetUnlockedAchievements(CallbackDelegate<AchievementsResult> callback)
    {
        yield return StartCoroutine(GetMethod("api/game-link/achieve", callback));
    }

    /// <summary>
    /// 특정 도전과제가 달성되었음을 기록합니다.
    /// </summary>
    /// <param name="achieveId">도전과제 ID.</param>
    /// <param name="callback">달성 결과를 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator UnlockAchievement(string achieveId, CallbackDelegate<SuccessResult> callback)
    {
        achieveId = Uri.EscapeDataString(achieveId);
        yield return StartCoroutine(PostMethod($"api/game-link/achieve?id={achieveId}", callback));
    }

    /// <summary>
    /// 한 통계 정보.
    /// </summary>
    [Serializable]
    public class GetStatsResultItem
    {
        /// <summary>
        /// 통계 ID.
        /// </summary>
        public string id;
        /// <summary>
        /// 통계 이름.
        /// </summary>
        public string name;
        /// <summary>
        /// 현재 통계 값.
        /// </summary>
        public int val;
        /// <summary>
        /// 최대 통계 값.
        /// </summary>
        public int? max;
        /// <summary>
        /// 최초 누적일. (UNIX 시간(ms))
        /// </summary>
        public long regDate;
        /// <summary>
        /// 마지막 누적일. (UNIX 시간(ms))
        /// </summary>
        public long chgDate;
    }

    /// <summary>
    /// 통계 목록.
    /// </summary>
    [Serializable]
    public class GetStatsResult
    {
        /// <summary>
        /// 개수.
        /// </summary>
        public int size;
        /// <summary>
        /// 통계 목록.
        /// </summary>
        public List<GetStatsResultItem> stats;
    }

    /// <summary>
    /// 사용자의 누적 통계 값들을 얻습니다.
    /// </summary>
    /// <param name="callback">통계 목록을 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator GetStats(CallbackDelegate<GetStatsResult> callback)
    {
        yield return StartCoroutine(GetMethod("api/game-link/stat", callback));
    }

    /// <summary>
    /// 한 통계 입력 정보.
    /// </summary>
    [Serializable]
    public class SetStatsInputItem
    {
        /// <summary>
        /// 통계 ID.
        /// </summary>
        public string id;
        /// <summary>
        /// 입력할 통계 값.
        /// </summary>
        public int val;
    }

    /// <summary>
    /// 통계 입력 목록.
    /// </summary>
    [Serializable]
    public class SetStatsInput : IEnumerable<SetStatsInputItem>
    {
        /// <summary>
        /// 입력할 통계들.
        /// </summary>
        public List<SetStatsInputItem> stats;

        public void Add(string id, int val)
        {
            stats ??= new List<SetStatsInputItem>();
            stats.Add(new SetStatsInputItem { id = id, val = val });
        }

        public IEnumerator<SetStatsInputItem> GetEnumerator()
        {
            return stats.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return stats.GetEnumerator();
        }
    }

    /// <summary>
    /// 한 통계 입력 결과.
    /// </summary>
    [Serializable]
    public class SetStatsResultStatItem
    {
        /// <summary>
        /// 통계 ID.
        /// </summary>
        public string id;
        /// <summary>
        /// 입력된 통계 값.
        /// </summary>
        public int val;
    }

    /// <summary>
    /// 한 도전과제 달성 결과.
    /// </summary>
    [Serializable]
    public class SetStatsResultAchieveItem
    {
        /// <summary>
        /// 도전과제 ID.
        /// </summary>
        public string id;
        /// <summary>
        /// 도전과제 이름.
        /// </summary>
        public string name;
        /// <summary>
        /// 도전과제 설명.
        /// </summary>
        public string desc;
        /// <summary>
        /// 도전과제 아이콘 이미지 URL.
        /// </summary>
        public string img;
        /// <summary>
        /// 도전과제 달성 시간. (UNIX 시간(ms))
        /// </summary>
        public long regDate;
        /// <summary>
        /// 연동된 통계 ID. (없으면 공백.)
        /// </summary>
        public string statId;
        /// <summary>
        ///  연동된 통계 목푯값. (없으면 0.)
        /// </summary>
        public int targetStatVal;
    }

    /// <summary>
    /// 통계 입력 결과 목록.
    /// </summary>
    [Serializable]
    public class SetStatsResult
    {
        /// <summary>
        /// 입력된 통계들.
        /// </summary>
        public List<SetStatsResultStatItem> stats;
        /// <summary>
        /// 새로 달성된 도전과제들.
        /// </summary>
        public List<SetStatsResultAchieveItem> achieves;
    }

    /// <summary>
    /// 사용자의 대상 통계 값들을 입력합니다.
    /// </summary>
    /// <param name="stats">입력할 통계들.</param>
    /// <param name="callback">통계 입력 결과를 받을 콜백.</param>
    /// <returns></returns>
    public IEnumerator SetStats(SetStatsInput stats, CallbackDelegate<SetStatsResult> callback)
    {
        yield return StartCoroutine(PutMethod("api/game-link/stat", JsonUtility.ToJson(stats), callback));
    }

    #endregion

    #region HTTP API 기본 메서드

    private IEnumerator GetMethod<T>(string api, CallbackDelegate<T> callback) where T : class
    {
        yield return ApiMethod(() => UnityWebRequest.Get($"{HOST}/{api}"), callback);
    }

    private IEnumerator PostMethod<T>(string api, CallbackDelegate<T> callback) where T : class
    {
        yield return ApiMethod(() => UnityWebRequest.PostWwwForm($"{HOST}/{api}", null), callback);
    }

    private IEnumerator PutMethod<T>(string api, string body, CallbackDelegate<T> callback) where T : class
    {
        yield return ApiMethod(() => UnityWebRequest.Put($"{HOST}/{api}", Encoding.UTF8.GetBytes(body)), callback);
    }

    private IEnumerator ApiMethod<T>(Func<UnityWebRequest> webRequestFactory, CallbackDelegate<T> callback, int maxRetry = 1) where T : class
    {
        if (string.IsNullOrEmpty(TokenStorage.AccessToken))
        {
            callback(null, 401);
            yield break;
        }

        using var webRequest = webRequestFactory();
        webRequest.SetRequestHeader("Authorization", "Bearer " + TokenStorage.AccessToken);
        webRequest.SetRequestHeader("Content-Type", "application/json");

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseJson = webRequest.downloadHandler.text;
            var result = JsonUtility.FromJson<T>(responseJson);
            callback(result, (int)webRequest.responseCode);
        }
        else
        {
            if (webRequest.responseCode == 401 && maxRetry > 0)
            {
                RefreshTokenResult token = null;
                yield return StartCoroutine(RefreshToken((t, _) => token = t));

                if (token != null)
                {
                    yield return StartCoroutine(ApiMethod(webRequestFactory, callback, maxRetry - 1));
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
    }

    #endregion
}
