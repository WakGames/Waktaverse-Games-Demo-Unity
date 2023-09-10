using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class Wakgames : MonoBehaviour
{
    public enum LoginState
    {
        Running,
        Fail,
        Success,
    }

    public delegate void CallbackDelegate<T>(T result, int responseCode) where T : class;

    [SerializeField]
    private string ClientId;
    [SerializeField]
    private int CallbackServerPort;

    public const string HOST = "https://wakgames-test.neurowhai.cf";

    private IWakgamesTokenStorage m_tokenStorage;
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

    public void Logout()
    {
        TokenStorage.ClearToken();
    }

    #endregion

    #region Wakgames API

    [Serializable]
    public class SuccessResult
    {
        public bool success;
    }

    [Serializable]
    public class RefreshTokenResult
    {
        public string accessToken;
        public string refreshToken;
        public int idToken;
    }

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

    [Serializable]
    public class UserProfileResult
    {
        public int id;
        public string name;
        public string profileImg;
    }

    public IEnumerator GetUserProfile(CallbackDelegate<UserProfileResult> callback)
    {
        yield return StartCoroutine(GetMethod("api/game-link/user/profile", callback));
    }

    [Serializable]
    public class AchievementsResultItem
    {
        public string id;
        public string name;
        public string desc;
        public string img;
        public long regDate;
    }

    [Serializable]
    public class AchievementsResult
    {
        public int size;
        public List<AchievementsResultItem> achieves;
    }

    public IEnumerator GetUnlockedAchievements(CallbackDelegate<AchievementsResult> callback)
    {
        yield return StartCoroutine(GetMethod("api/game-link/achieve", callback));
    }

    public IEnumerator UnlockAchievement(string achieveId, CallbackDelegate<SuccessResult> callback)
    {
        achieveId = Uri.EscapeDataString(achieveId);
        yield return StartCoroutine(PostMethod($"api/game-link/achieve?id={achieveId}", callback));
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

    private IEnumerator ApiMethod<T>(Func<UnityWebRequest> webRequestFactory, CallbackDelegate<T> callback, int maxRetry = 1) where T : class
    {
        if (string.IsNullOrEmpty(TokenStorage.AccessToken))
        {
            callback(null, 401);
            yield break;
        }

        using var webRequest = webRequestFactory();
        webRequest.SetRequestHeader("Authorization", "Bearer " + TokenStorage.AccessToken);

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
