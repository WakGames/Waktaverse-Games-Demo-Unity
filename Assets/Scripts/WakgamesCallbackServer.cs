using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class WakgamesCallbackServer : MonoBehaviour
{
    [Serializable]
    public class TokenResult
    {
        public string accessToken;
        public string refreshToken;
        public int idToken;
    }

    private HttpListener m_listener;

    public string ClientId { get; set; }
    public string CsrfState { get; set; }
    public string CodeVerifier { get; set; }

    public bool IsRunning { get; private set; } = false;
    public TokenResult UserToken { get; private set; }

    public void StartServer(int listenPort)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException();
        }

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{listenPort}/");
        listener.Start();
        m_listener = listener;

        IsRunning = true;

        ThreadPool.QueueUserWorkItem(HandleRequests);
    }

    private void HandleRequests(object _)
    {
        while (m_listener.IsListening)
        {
            try
            {
                var context = m_listener.GetContext();
                var request = context.Request;
                var response = context.Response;

                // callback 쪽 요청이 아니면 404 응답 주고 무시.
                if (request.Url.LocalPath != "/callback")
                {
                    response.StatusCode = 404;

                    byte[] buffer = Encoding.UTF8.GetBytes("not found");
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                    continue;
                }

                var queryParams = ParseQueryString(request.Url.Query);
                queryParams.TryGetValue("code", out string code);
                queryParams.TryGetValue("state", out string state);
                queryParams.TryGetValue("error", out string error);
                queryParams.TryGetValue("message", out string message);

                bool success = string.IsNullOrEmpty(error) && state == CsrfState;

                if (success)
                {
                    try
                    {
                        UserToken = GetToken(code);

                        response.Redirect($"{Wakgames.HOST}/oauth/authorize?success=1");
                        response.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);

                        error = e.Message;
                        message = e.ToString();

                        success = false;
                    }
                }

                if (!success)
                {
                    string responseText;
                    if (string.IsNullOrEmpty(error) && string.IsNullOrEmpty(message))
                    {
                        responseText = "error: invalid state";
                    }
                    else
                    {
                        responseText = $"error: {error}\n{message}";
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }

                break;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                break;
            }
        }

        m_listener.Stop();
        m_listener.Close();

        IsRunning = false;
    }

    private TokenResult GetToken(string code)
    {
        string callbackUri = Uri.EscapeDataString(m_listener.Prefixes.First() + "callback");
        string getTokenUri = $"{Wakgames.HOST}/api/oauth/token?grantType=authorization_code&clientId={ClientId}&code={code}&verifier={CodeVerifier}&callbackUri={callbackUri}";

        var request = (HttpWebRequest)WebRequest.Create(getTokenUri);
        request.Method = "POST";
        request.ContentLength = 0;

        string responseContent;

        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream))
        {
            responseContent = reader.ReadToEnd();
        }

        return JsonUtility.FromJson<TokenResult>(responseContent);
    }

    private Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>();
        if (queryString.StartsWith("?"))
        {
            queryString = queryString.Substring(1);
        }
        foreach (string segment in queryString.Split('&'))
        {
            int index = segment.IndexOf('=');
            if (index >= 0)
            {
                string key = Uri.UnescapeDataString(segment.Substring(0, index));
                string value = Uri.UnescapeDataString(segment.Substring(index + 1));
                result[key] = value;
            }
        }
        return result;
    }

    private void OnDestroy()
    {
        if (m_listener != null && m_listener.IsListening)
        {
            m_listener.Stop();
            m_listener.Close();
        }
    }
}
