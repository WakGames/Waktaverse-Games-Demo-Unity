using System;
using System.Collections;
using System.Collections.Generic;
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
        set => m_tokenStorage = value;
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

    public IEnumerator StartLogin(Action<UserProfileResult> callback)
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

                StartCoroutine(GetUserProfile(callback));

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

    [Serializable]
    public class RefreshTokenResult
    {
        public string accessToken;
        public string refreshToken;
        public int idToken;
    }

    public IEnumerator RefreshToken(Action<RefreshTokenResult> callback)
    {
        if (string.IsNullOrEmpty(TokenStorage.RefreshToken))
        {
            callback(null);
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

            callback(result);
        }
        else
        {
            callback(null);
        }
    }

    [Serializable]
    public class UserProfileResult
    {
        public int id;
        public string name;
        public string profileImg;
    }

    public IEnumerator GetUserProfile(Action<UserProfileResult> callback, int maxRetry = 1)
    {
        if (string.IsNullOrEmpty(TokenStorage.AccessToken))
        {
            callback(null);
            yield break;
        }

        string url = $"{HOST}/api/game-link/user/profile";
        using var webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("Authorization", "Bearer " + TokenStorage.AccessToken);

        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseJson = webRequest.downloadHandler.text;
            var result = JsonUtility.FromJson<UserProfileResult>(responseJson);
            callback(result);
        }
        else
        {
            if (webRequest.responseCode == 401 && maxRetry > 0)
            {
                StartCoroutine(RefreshToken((token) =>
                {
                    if (token != null)
                    {
                        StartCoroutine(GetUserProfile(callback, maxRetry - 1));
                    }
                    else
                    {
                        callback(null);
                    }
                }));
            }
            else
            {
                callback(null);
            }
        }
    }
}
