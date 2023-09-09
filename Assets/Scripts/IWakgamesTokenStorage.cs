public interface IWakgamesTokenStorage
{
    public string AccessToken { get; }
    public string RefreshToken { get; }
    public int IdToken { get; }

    public void UpdateToken(string accessToken, string refreshToken, int idToken);
    public void ClearToken();
}
