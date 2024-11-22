using UnityEngine;

public class WakgamesTokenStorage : MonoBehaviour
{
    public string AccessToken => _token.accessToken;
    public string RefreshToken => _token.refreshToken;
    public int IdToken => _token.idToken;

    private readonly WakgamesToken _token = new();

    private void Awake()
    {
        _token.accessToken = PlayerPrefs.GetString("Wakgames_Access_Token", string.Empty);
        _token.refreshToken = PlayerPrefs.GetString("Wakgames_Refresh_Token", string.Empty);
        _token.idToken = PlayerPrefs.GetInt("Wakgames_ID_Token", -1);
    }

    public void UpdateToken(string accessToken, string refreshToken, int idToken)
    {
        _token.accessToken = accessToken;
        _token.refreshToken = refreshToken;
        _token.idToken = idToken;

        PlayerPrefs.SetString("Wakgames_Access_Token", accessToken);
        PlayerPrefs.SetString("Wakgames_Refresh_Token", refreshToken);
        PlayerPrefs.SetInt("Wakgames_ID_Token", idToken);
    }

    public void ClearToken()
    {
        UpdateToken(string.Empty, string.Empty, -1);
    }
}
