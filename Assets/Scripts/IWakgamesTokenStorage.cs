public interface IWakgamesTokenStorage
{
    /// <summary>
    /// 접근 토큰.
    /// </summary>
    public string AccessToken { get; }
    /// <summary>
    /// 갱신 토큰.
    /// </summary>
    public string RefreshToken { get; }
    /// <summary>
    /// 사용자 ID.
    /// </summary>
    public int IdToken { get; }

    /// <summary>
    /// 토큰 저장.
    /// </summary>
    /// <param name="accessToken">접근 토큰.</param>
    /// <param name="refreshToken">갱신 토큰.</param>
    /// <param name="idToken">사용자 ID.</param>
    public void UpdateToken(string accessToken, string refreshToken, int idToken);
    /// <summary>
    /// 토큰 삭제.
    /// </summary>
    public void ClearToken();
}
