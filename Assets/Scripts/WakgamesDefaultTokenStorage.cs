using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WakgamesDefaultTokenStorage : MonoBehaviour, IWakgamesTokenStorage
{
    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
    public int IdToken { get; private set; }

    private void Awake()
    {
        AccessToken = PlayerPrefs.GetString("Wakgames_Access_Token", string.Empty);
        RefreshToken = PlayerPrefs.GetString("Wakgames_Refresh_Token", string.Empty);
        IdToken = PlayerPrefs.GetInt("Wakgames_ID_Token", -1);
    }

    public void UpdateToken(string accessToken, string refreshToken, int idToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        IdToken = idToken;

        PlayerPrefs.SetString("Wakgames_Access_Token", accessToken);
        PlayerPrefs.SetString("Wakgames_Refresh_Token", refreshToken);
        PlayerPrefs.SetInt("Wakgames_ID_Token", idToken);
    }

    public void ClearToken()
    {
        UpdateToken(string.Empty, string.Empty, -1);
    }
}
