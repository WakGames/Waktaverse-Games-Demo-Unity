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
    /// API ������ ���� �ݹ�.
    /// </summary>
    /// <typeparam name="T">���� ����.</typeparam>
    /// <param name="result">���� ������. ������ null.</param>
    /// <param name="responseCode">HTTP ���� �ڵ�.</param>
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
    /// ��ū �����.
    /// ������ �������� ������ �⺻ ����Ҹ� ����մϴ�.
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
            throw new Exception($"��ȿ�� Client ID�� �����ؾ� �մϴ�.");
        }
        if (CallbackServerPort <= 0)
        {
            throw new Exception($"��ȿ�� �ݹ� ���� ��Ʈ�� �����ؾ� �մϴ�.");
        }
    }

    #region Wakgames Login

    /// <summary>
    /// �α��� ������ �����մϴ�.
    /// </summary>
    /// <param name="callback">�α��� �Ϸ� �� ����� ������ ���� �ݹ�. �����ϸ� null.</param>
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
    /// ����� ��ū�� �����Ͽ� �α׾ƿ��մϴ�.
    /// </summary>
    public void Logout()
    {
        TokenStorage.ClearToken();
    }

    #endregion

    #region Wakgames API

    /// <summary>
    /// �ܼ� ���� ����.
    /// </summary>
    [Serializable]
    public class SuccessResult
    {
        /// <summary>
        /// ���� ����.
        /// </summary>
        public bool success;
    }

    /// <summary>
    /// ��ū ���� ����.
    /// </summary>
    [Serializable]
    public class RefreshTokenResult
    {
        /// <summary>
        /// ���� ��ū.
        /// </summary>
        public string accessToken;
        /// <summary>
        /// ���� ��ū.
        /// </summary>
        public string refreshToken;
        /// <summary>
        /// ����� ID.
        /// </summary>
        public int idToken;
    }

    /// <summary>
    /// ��ū�� �����ϰ� ������ �����մϴ�.
    /// </summary>
    /// <param name="callback">���� �߱޵� ��ū ������ ���� �ݹ�.</param>
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
    /// ����� ������.
    /// </summary>
    [Serializable]
    public class UserProfileResult
    {
        /// <summary>
        /// ����� ID.
        /// </summary>
        public int id;
        /// <summary>
        /// �г���.
        /// </summary>
        public string name;
        /// <summary>
        /// ������ �̹��� URL.
        /// </summary>
        public string profileImg;
    }

    /// <summary>
    /// ����� �������� ��ȸ�մϴ�.
    /// </summary>
    /// <param name="callback">����� ������ ������ ���� �ݹ�.</param>
    /// <returns></returns>
    public IEnumerator GetUserProfile(CallbackDelegate<UserProfileResult> callback)
    {
        yield return StartCoroutine(GetMethod("api/game-link/user/profile", callback));
    }

    /// <summary>
    /// �� �������� ����.
    /// </summary>
    [Serializable]
    public class AchievementsResultItem
    {
        /// <summary>
        /// �������� ID.
        /// </summary>
        public string id;
        /// <summary>
        /// �������� �̸�.
        /// </summary>
        public string name;
        /// <summary>
        /// �������� ����.
        /// </summary>
        public string desc;
        /// <summary>
        /// �������� ������ �̹��� URL.
        /// </summary>
        public string img;
        /// <summary>
        /// �������� �޼� �ð�. (UNIX �ð�(ms))
        /// </summary>
        public long regDate;
        /// <summary>
        /// ������ ��� ID. (������ ����.)
        /// </summary>
        public string statId;
        /// <summary>
        ///  ������ ��� ��ǩ��. (������ 0.)
        /// </summary>
        public int targetStatVal;
    }

    /// <summary>
    /// �������� ���.
    /// </summary>
    [Serializable]
    public class AchievementsResult
    {
        /// <summary>
        /// ����.
        /// </summary>
        public int size;
        /// <summary>
        /// �������� ���.
        /// </summary>
        public List<AchievementsResultItem> achieves;
    }

    /// <summary>
    /// ����ڰ� �޼��� �������� ����� ����ϴ�.
    /// </summary>
    /// <param name="callback">�޼� �������� ����� ���� �ݹ�.</param>
    /// <returns></returns>
    public IEnumerator GetUnlockedAchievements(CallbackDelegate<AchievementsResult> callback)
    {
        yield return StartCoroutine(GetMethod("api/game-link/achieve", callback));
    }

    /// <summary>
    /// Ư�� ���������� �޼��Ǿ����� ����մϴ�.
    /// </summary>
    /// <param name="achieveId">�������� ID.</param>
    /// <param name="callback">�޼� ����� ���� �ݹ�.</param>
    /// <returns></returns>
    public IEnumerator UnlockAchievement(string achieveId, CallbackDelegate<SuccessResult> callback)
    {
        achieveId = Uri.EscapeDataString(achieveId);
        yield return StartCoroutine(PostMethod($"api/game-link/achieve?id={achieveId}", callback));
    }

    /// <summary>
    /// �� ��� ����.
    /// </summary>
    [Serializable]
    public class GetStatsResultItem
    {
        /// <summary>
        /// ��� ID.
        /// </summary>
        public string id;
        /// <summary>
        /// ��� �̸�.
        /// </summary>
        public string name;
        /// <summary>
        /// ���� ��� ��.
        /// </summary>
        public int val;
        /// <summary>
        /// �ִ� ��� ��.
        /// </summary>
        public int? max;
        /// <summary>
        /// ���� ������. (UNIX �ð�(ms))
        /// </summary>
        public long regDate;
        /// <summary>
        /// ������ ������. (UNIX �ð�(ms))
        /// </summary>
        public long chgDate;
    }

    /// <summary>
    /// ��� ���.
    /// </summary>
    [Serializable]
    public class GetStatsResult
    {
        /// <summary>
        /// ����.
        /// </summary>
        public int size;
        /// <summary>
        /// ��� ���.
        /// </summary>
        public List<GetStatsResultItem> stats;
    }

    /// <summary>
    /// ������� ���� ��� ������ ����ϴ�.
    /// </summary>
    /// <param name="callback">��� ����� ���� �ݹ�.</param>
    /// <returns></returns>
    public IEnumerator GetStats(CallbackDelegate<GetStatsResult> callback)
    {
        yield return StartCoroutine(GetMethod("api/game-link/stat", callback));
    }

    /// <summary>
    /// �� ��� �Է� ����.
    /// </summary>
    [Serializable]
    public class SetStatsInputItem
    {
        /// <summary>
        /// ��� ID.
        /// </summary>
        public string id;
        /// <summary>
        /// �Է��� ��� ��.
        /// </summary>
        public int val;
    }

    /// <summary>
    /// ��� �Է� ���.
    /// </summary>
    [Serializable]
    public class SetStatsInput : IEnumerable<SetStatsInputItem>
    {
        /// <summary>
        /// �Է��� ����.
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
    /// �� ��� �Է� ���.
    /// </summary>
    [Serializable]
    public class SetStatsResultStatItem
    {
        /// <summary>
        /// ��� ID.
        /// </summary>
        public string id;
        /// <summary>
        /// �Էµ� ��� ��.
        /// </summary>
        public int val;
    }

    /// <summary>
    /// �� �������� �޼� ���.
    /// </summary>
    [Serializable]
    public class SetStatsResultAchieveItem
    {
        /// <summary>
        /// �������� ID.
        /// </summary>
        public string id;
        /// <summary>
        /// �������� �̸�.
        /// </summary>
        public string name;
        /// <summary>
        /// �������� ����.
        /// </summary>
        public string desc;
        /// <summary>
        /// �������� ������ �̹��� URL.
        /// </summary>
        public string img;
        /// <summary>
        /// �������� �޼� �ð�. (UNIX �ð�(ms))
        /// </summary>
        public long regDate;
        /// <summary>
        /// ������ ��� ID. (������ ����.)
        /// </summary>
        public string statId;
        /// <summary>
        ///  ������ ��� ��ǩ��. (������ 0.)
        /// </summary>
        public int targetStatVal;
    }

    /// <summary>
    /// ��� �Է� ��� ���.
    /// </summary>
    [Serializable]
    public class SetStatsResult
    {
        /// <summary>
        /// �Էµ� ����.
        /// </summary>
        public List<SetStatsResultStatItem> stats;
        /// <summary>
        /// ���� �޼��� ����������.
        /// </summary>
        public List<SetStatsResultAchieveItem> achieves;
    }

    /// <summary>
    /// ������� ��� ��� ������ �Է��մϴ�.
    /// </summary>
    /// <param name="stats">�Է��� ����.</param>
    /// <param name="callback">��� �Է� ����� ���� �ݹ�.</param>
    /// <returns></returns>
    public IEnumerator SetStats(SetStatsInput stats, CallbackDelegate<SetStatsResult> callback)
    {
        yield return StartCoroutine(PutMethod("api/game-link/stat", JsonUtility.ToJson(stats), callback));
    }

    #endregion

    #region HTTP API �⺻ �޼���

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
